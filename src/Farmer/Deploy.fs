module Farmer.Deploy

open Farmer.ArmBuilder
open Newtonsoft.Json
open System
open System.Diagnostics
open System.IO

let deployFolder = ".farmer"
let prepareDeploymentFolder() =
    if Directory.Exists deployFolder then Directory.Delete(deployFolder, true)
    Directory.CreateDirectory deployFolder |> ignore
let generateDeployNumber =
    let r = Random()
    fun () -> r.Next 10000

/// Provides strongly-typed access to the Azure CLI
module Az =
    open System.Runtime.InteropServices
    open System.Text

    [<AutoOpen>]
    module AzHelpers =
        let outputFile = Path.Combine(deployFolder, "output.txt")
        let (|OperatingSystem|_|) platform () =
            if RuntimeInformation.IsOSPlatform platform then Some() else None

        let executeAzWindows arguments =
            let azProcess =
                ProcessStartInfo(
                    FileName = "az",
                    Arguments = arguments + " 1> " + outputFile + " 2>&1",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden)
                |> Process.Start
            azProcess.WaitForExit()
            let response = File.ReadAllText outputFile
            File.Delete outputFile
            azProcess, response
        let executeAzLinux arguments =
            let azProcess =
                ProcessStartInfo(
                    FileName = "az",
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    WindowStyle = ProcessWindowStyle.Hidden)
                |> Process.Start
            azProcess.WaitForExit()
            let sb = StringBuilder()
            while not azProcess.StandardOutput.EndOfStream do
                sb.AppendLine(azProcess.StandardOutput.ReadLine()) |> ignore
            azProcess, sb.ToString()

        let processToResult (p:Process, response) =
            match p.ExitCode with
            | 0 -> Ok response
            | _ -> Error response

    /// Executes a generic AZ CLI command.
    let executeAz arguments =
        match () with
        | OperatingSystem OSPlatform.Windows ->
            executeAzWindows arguments
        | OperatingSystem OSPlatform.Linux
        | OperatingSystem OSPlatform.OSX ->
            executeAzLinux arguments
        | _ ->
            failwithf "OSPlatform: %s not supported" RuntimeInformation.OSDescription
        |> processToResult
    /// Tests if the Az CLI has logged in credentials.
    let isLoggedIn() = executeAz "account show" |> function Ok _ -> true | Error _ -> false
    /// Logs you into Az CLI interactively.
    let login() = executeAz "login" |> Result.ignore
    /// Logs you into the Az CLI using the supplied service principal credentials.
    let loginWithCredentials appId secret tenantId = executeAz (sprintf "login --service-principal --username %s --password %s --tenant %s" appId secret tenantId)
    /// Creates a resource group.
    let createResourceGroup location resourceGroup =
        executeAz (sprintf "group create -l %s -n %s" location resourceGroup) |> Result.ignore
    /// Deploys an ARM template to an existing resource group.
    let deploy resourceGroup deploymentName templateFilename parameters =
        let parametersArgument =
            match parameters with
            | [] -> ""
            | parameters -> sprintf "--parameters %s" (parameters |> List.map(fun (a,b) -> sprintf "%s=%s" a b) |> String.concat " ")
        executeAz (sprintf "group deployment create -g %s -n %s --template-file %s %s" resourceGroup deploymentName templateFilename parametersArgument)
    /// Deploys a zip file to a web app using the Zip Deploy mechanism.
    let zipDeploy webAppName (zipDeployKind:ZipDeployKind) resourceGroup =
        let packageFilename = zipDeployKind.GetZipPath deployFolder
        executeAz (sprintf """webapp deployment source config-zip --resource-group "%s" --name "%s" --src %s""" resourceGroup webAppName packageFilename)

type OutputKey = string
type OutputValue = string
type OutputMap = Map<OutputKey, OutputValue>
type Subscription = { ID : Guid; Name : string; IsDefault : bool }

/// Authenticates the Az CLI using the supplied ApplicationId, Client Secret and Tenant Id.
/// Returns the list of subscriptions, including which one the default is.
let authenticate appId secret tenantId =
    Az.loginWithCredentials appId secret tenantId
    |> Result.map (JsonConvert.DeserializeObject<Subscription []>)

let validateParameters suppliedParameters deployment =
    let expected = deployment.Template.Parameters |> List.map(fun (SecureParameter p) -> p) |> Set
    match (expected - (suppliedParameters |> List.map fst |> Set)) |> Seq.toList with
    | [] -> Ok ()
    | missingParameters -> Error (sprintf "The following parameters are missing: %s." (missingParameters |> String.concat ", "))

let NoParameters : (string * string) list= []

/// Executes the supplied Deployment against a resource group using the Azure CLI.
/// If successful, returns a Map of the output keys and values.
let execute resourceGroupName parameters deployment : Result<OutputMap, _> = result {
    prepareDeploymentFolder()
    do! deployment |> validateParameters parameters
    do!
        printf "Checking Azure CLI logged in status... "
        if Az.isLoggedIn() then printfn "you are already logged in, nothing to do."; Ok()
        else printfn "logging you in."; Az.login()

    printfn "Creating resource group %s..." resourceGroupName
    do! Az.createResourceGroup deployment.Location.Value resourceGroupName

    printfn "Deploying ARM template (please be patient, this can take a while)..."
    let! response =
        let deploymentName = sprintf "farmer-deploy-%d" (generateDeployNumber())
        let templateFilename = deployment.Template |> Writer.toJson |> Writer.toFile deployFolder "farmer-deploy"
        Az.deploy resourceGroupName deploymentName templateFilename parameters

    do!
        [ for (RunFromZip wd) in deployment.PostDeployTasks do
            printfn "Running ZIP deploy for %s" wd.Path.Value
            Az.zipDeploy wd.WebApp.Value wd.Path resourceGroupName ]
        |> Result.sequence
        |> Result.ignore

    printfn "All done, now parsing ARM response to get any outputs..."
    let response = response |> JsonConvert.DeserializeObject<{| properties : {| outputs : Map<string, {| value : string |}> |} |}>
    return response.properties.outputs |> Map.map (fun _ value -> value.value)
}
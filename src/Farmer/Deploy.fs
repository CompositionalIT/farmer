module Farmer.Deploy

open Farmer.ArmBuilder
open Newtonsoft.Json
open System
open System.Diagnostics
open System.IO

let private DeployFolder = ".farmer"
let private prepareDeploymentFolder() =
    if Directory.Exists DeployFolder then Directory.Delete(DeployFolder, true)
    Directory.CreateDirectory DeployFolder |> ignore
let private generateDeployNumber =
    let r = Random()
    fun () -> r.Next 10000

/// Provides strongly-typed access to the Azure CLI
module Az =
    open System.Runtime.InteropServices
    open System.Text

    let MinimumVersion = Version "2.3.1"

    [<AutoOpen>]
    module AzHelpers =
        let (|OperatingSystem|_|) platform () =
            if RuntimeInformation.IsOSPlatform platform then Some() else None
        let azCliPath =
            lazy
                match () with
                | OperatingSystem OSPlatform.Windows ->
                    Environment.GetEnvironmentVariable("PATH").Split Path.PathSeparator
                    |> Seq.map (fun s -> Path.Combine(s, "az.cmd"))
                    |> Seq.tryFind File.Exists
                    |> Option.defaultWith (fun () -> invalidOp "Can't find Azure CLI")
                | OperatingSystem OSPlatform.Linux
                | OperatingSystem OSPlatform.OSX ->
                    "az"
                | _ ->
                    failwithf "OSPlatform: %s not supported" RuntimeInformation.OSDescription
        let executeAz arguments =
            let azProcess =
                ProcessStartInfo(
                    FileName = azCliPath.Value,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true)
                |> Process.Start
            let sb = StringBuilder()
            let flushContents() =
                let flushStream (stream:StreamReader) =
                    while not stream.EndOfStream do sb.AppendLine(stream.ReadLine()) |> ignore
                [ azProcess.StandardOutput; azProcess.StandardError ] |> List.iter flushStream

            flushContents() // For some reason if we don't try flushing before waiting for exit, sometimes stdout crashes.
            azProcess.WaitForExit()
            flushContents()

            azProcess, sb.ToString()
        let processToResult (p:Process, response) =
            match p.ExitCode with
            | 0 -> Ok response
            | _ -> Error response

    /// Executes a generic AZ CLI command.
    let az = AzHelpers.executeAz >> processToResult
    /// Tests if the Az CLI has logged in credentials.
    let isLoggedIn() = az "account show" |> function Ok _ -> true | Error _ -> false
    /// Logs you into Az CLI interactively.
    let login() = az "login" |> Result.ignore
    /// Logs you into the Az CLI using the supplied service principal credentials.
    let loginWithCredentials appId secret tenantId = az (sprintf "login --service-principal --username %s --password %s --tenant %s" appId secret tenantId)
    let version() = az "--version"
    /// Lists all subscriptions
    let listSubscriptions() = az "account list"
    let setSubscription subscriptionId = az (sprintf "account set --subscription %s" subscriptionId)
    /// Creates a resource group.
    let createResourceGroup location resourceGroup =
        az (sprintf "group create -l %s -n %s" location resourceGroup) |> Result.ignore
    /// Deploys an ARM template to an existing resource group.
    let deploy resourceGroup deploymentName templateFilename parameters =
        let parametersArgument =
            match parameters with
            | [] -> ""
            | parameters -> sprintf "--parameters %s" (parameters |> List.map(fun (a,b) -> sprintf "%s=%s" a b) |> String.concat " ")
        az (sprintf "deployment group create -g %s -n %s --template-file %s %s" resourceGroup deploymentName templateFilename parametersArgument)
    /// Deploys a zip file to a web app using the Zip Deploy mechanism.
    let zipDeploy webAppName (zipDeployKind:ZipDeployKind) resourceGroup =
        let packageFilename = zipDeployKind.GetZipPath DeployFolder
        az (sprintf """webapp deployment source config-zip --resource-group "%s" --name "%s" --src %s""" resourceGroup webAppName packageFilename)

/// Represents an Azure subscription
type Subscription = { ID : Guid; Name : string; IsDefault : bool }

/// Authenticates the Az CLI using the supplied ApplicationId, Client Secret and Tenant Id.
/// Returns the list of subscriptions, including which one the default is.
let authenticate appId secret tenantId =
    Az.loginWithCredentials appId secret tenantId
    |> Result.map (JsonConvert.DeserializeObject<Subscription []>)

/// Lists all subscriptions that the logged in identity has access to.
let listSubscriptions() = result {
    let! response = Az.listSubscriptions()
    return response |> JsonConvert.DeserializeObject<Subscription array>
}

let checkVersion minimum = result {
    let! version = Az.version()
    let! version =
        version.Split([| "\r\n" |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.tryHead
        |> Option.bind(fun text -> text.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries) |> Array.tryLast)
        |> Option.map Version
        |> Result.ofOption "Unable to determine Azure CLI version."
    return!
        if version < minimum then Error (sprintf "Minimum version of Azure CLI is %O. You have installed %O" minimum version)
        else Ok version
}

/// Sets the currently active (default) subscription.
let setSubscription (subscriptionId:Guid) =
    Az.setSubscription (subscriptionId.ToString())

let validateParameters suppliedParameters deployment =
    let expected = deployment.Template.Parameters |> List.map(fun (SecureParameter p) -> p) |> Set
    match (expected - (suppliedParameters |> List.map fst |> Set)) |> Seq.toList with
    | [] -> Ok ()
    | missingParameters -> Error (sprintf "The following parameters are missing: %s." (missingParameters |> String.concat ", "))

let NoParameters : (string * string) list = []

/// Executes the supplied Deployment against a resource group using the Azure CLI.
/// If successful, returns a Map of the output keys and values.
let execute resourceGroupName parameters deployment = result {
    let! version = checkVersion Az.MinimumVersion
    printfn "Compatible version of Azure CLI %O detected" version
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
        let templateFilename = deployment.Template |> Writer.toJson |> Writer.toFile DeployFolder "farmer-deploy"
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
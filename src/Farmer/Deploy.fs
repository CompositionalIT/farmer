module Farmer.Deploy

open Farmer.ArmBuilder
open Newtonsoft.Json
open System
open System.Diagnostics
open System.IO

let private deployFolder = ".farmer"
let private prepareDeploymentFolder() =
    if Directory.Exists deployFolder then Directory.Delete(deployFolder, true)
    Directory.CreateDirectory deployFolder |> ignore
let private getDeployNumber =
    let r = Random()
    fun () -> r.Next 10000



module ParameterFile =
    module Passwords =
        open System
        let lowerCaseLetters = String [|'a'..'z'|]
        let upperCaseLetters = String [|'A'..'Z'|]
        let digits = String [|'0' .. '9'|]
        let special = "!�$%^&*()_-+="
        let allCharacters = lowerCaseLetters + upperCaseLetters + digits + special

        let isValid (s:string) =
            let isInString (src:string) = s |> Seq.exists (string >> src.Contains)
            isInString lowerCaseLetters && isInString upperCaseLetters && isInString digits && isInString special

        let generatePassword randomNumber length =
            Seq.init length (fun _ -> allCharacters.[randomNumber allCharacters.Length])
            |> Seq.toArray
            |> String

        /// Creates a password that is known to conform to lower, upper and numeric constraints.
        let generateConformingPassword length template =
            let rnd = Random (template.GetHashCode())

            Seq.initInfinite (fun _ -> generatePassword rnd.Next length)
            |> Seq.take 100
            |> Seq.filter isValid
            |> Seq.tryHead
            |> function
            | None -> failwith "Unable to generate a valid password that meet the requested requirements!"
            | Some password -> password

    let toParameters parameters =
        {| ``$schema`` = "https://schema.management.azure.com/schemas/2015-01-01/deploymentParameters.json#"
           contentVersion = "1.0.0.0"
           parameters =
                parameters
                |> List.map(fun (name, value) -> name, {| value = value |})
                |> Map.ofList
        |}

    let generateParametersFile folder (armTemplate:ArmTemplate) =
        armTemplate.Parameters
        |> List.map(fun (SecureParameter p) -> p, Passwords.generateConformingPassword 24 armTemplate)
        |> toParameters
        |> Writer.TemplateGeneration.serialize
        |> Writer.toFile folder "farmer-deploy-parameters"

/// Provides strongly-typed access to the Azure CLI
module Az =
    open System.Runtime.InteropServices
    let (|OperatingSystem|_|) platform () =
        if RuntimeInformation.IsOSPlatform platform then Some() else None

    let executeAzWindows arguments =
        let outputFile = Path.Combine(deployFolder, "output.txt")
        let p =
            ProcessStartInfo(
                FileName = "az",
                Arguments = arguments + " > " + outputFile,
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden)
            |> Process.Start
        p.WaitForExit()
        let response = File.ReadAllText outputFile
        File.Delete outputFile
        p, response
    let executeAzLinux arguments =
        let p =
            ProcessStartInfo(
                FileName = "az",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden)
            |> Process.Start
        let sb = Text.StringBuilder()
        while not p.StandardOutput.EndOfStream do
            sb.AppendLine(p.StandardOutput.ReadLine()) |> ignore
        p, sb.ToString()

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
    let isLoggedIn() = executeAz "account show" |> function Ok _ -> true | Error _ -> false
    let login() = executeAz "login" |> Result.ignore
    let createResourceGroup location resourceGroup = executeAz (sprintf "group create -l %s -n %s" location resourceGroup) |> Result.ignore
    let deploy resourceGroup deploymentName templateFilename parametersFilename =
        sprintf "group deployment create -g %s -n%s --template-file %s --parameters @%s"
            resourceGroup
            deploymentName
            templateFilename
            parametersFilename
        |> executeAz
    let zipDeploy webAppName (zipDeployKind:ZipDeployKind) resourceGroup =
        let packageFilename = zipDeployKind.GetZipPath deployFolder
        executeAz (sprintf """webapp deployment source config-zip --resource-group "%s" --name "%s" --src %s""" resourceGroup webAppName packageFilename)

type OutputKey = string
type OutputValue = string
type OutputMap = Map<OutputKey, OutputValue>
/// Executes the supplied Deployment against a resource group using the Azure CLI.
/// If successful, returns a Map of the output keys and values.
let execute resourceGroupName deployment : Result<OutputMap, _> = result {
    prepareDeploymentFolder()
    do!
        printf "Checking Azure CLI logged in status... "
        if Az.isLoggedIn() then printfn "you are already logged in, nothing to do."; Ok()
        else printfn "logging you in."; Az.login()

    printfn "Creating resource group %s..." resourceGroupName
    do! Az.createResourceGroup deployment.Location.Value resourceGroupName

    printfn "Deploying ARM template (please be patient, this can take a while)..."
    let templateFilename = deployment.Template |> Writer.toJson |> Writer.toFile deployFolder "farmer-deploy"
    let parameterFilename = ParameterFile.generateParametersFile deployFolder deployment.Template
    let deploymentName = sprintf "farmer-deploy-%d" (getDeployNumber())
    let! response = Az.deploy resourceGroupName deploymentName templateFilename parameterFilename

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
﻿module Farmer.Deploy

open Farmer.CoreTypes
open Newtonsoft.Json
open System
open System.Diagnostics
open System.IO

let private deployFolder = ".farmer"
let private prepareDeploymentFolder() =
    if Directory.Exists deployFolder then Directory.Delete(deployFolder, true)
    Directory.CreateDirectory deployFolder |> ignore
let private generateDeployNumber =
    let r = Random()
    fun () -> r.Next 10000

/// Provides strongly-typed access to the Azure CLI
module Az =
    open System.Runtime.InteropServices
    open System.Text

    let MinimumVersion = Version "2.5.0"

    type AzureCLIToolsNotFound (message:string, innerException : exn) =
        inherit System.Exception (message, innerException)

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
            try
                let argumentsWithOutputSet =
                    if System.Text.RegularExpressions.Regex.IsMatch(arguments, "(-o|--output) (json|table|jsonc|yaml|tsv|none)") then
                        arguments
                    else arguments + " -o json"
                let azProcess =
                    ProcessStartInfo(
                        FileName = azCliPath.Value,
                        Arguments = arguments,
                        UseShellExecute = false,
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
            with
            | :? System.ComponentModel.Win32Exception as e when e.Message.Contains("No such file or directory") ->
                let message = sprintf "Could not find Azure CLI tools on %s. Make sure you've setup the Azure CLI tools.  Go to https://compositionalit.github.io/farmer/quickstarts/quickstart-3/#install-the-azure-cli for more information." azCliPath.Value
                AzureCLIToolsNotFound(message, e) |> raise
            | _ ->
                reraise()


        let processToResult (p:Process, response) =
            match p.ExitCode with
            | 0 -> Ok response
            | _ -> Error response

    /// Executes a generic AZ CLI command.
    let az = AzHelpers.executeAz >> processToResult
    /// Tests if the Az CLI has logged in credentials.
    let showAccount() = az "account show"
    /// Logs you into Az CLI interactively.
    let login() = az "login" |> Result.ignore
    /// Logs you into the Az CLI using the supplied service principal credentials.
    let loginWithCredentials appId secret tenantId = az (sprintf "login --service-principal --username %s --password %s --tenant %s" appId secret tenantId)
    let version() = az "--version"
    /// Lists all subscriptions
    let listSubscriptions() = az "account list"
    let setSubscription subscriptionId = az (sprintf "account set --subscription %s" subscriptionId)
    /// Creates a resource group.
    let createResourceGroup location resourceGroup = az (sprintf "group create -l %s -n %s" location resourceGroup) |> Result.ignore
    /// Searches for users in AD using the supplied filter.
    let searchUsers filter = az ("ad user list --filter " + filter)
    /// Searches for groups in AD using the supplied filter.
    let searchGroups filter = az ("ad group list --filter " + filter)


    type DeploymentCommand =
    | Create
    | WhatIf
    | Validate
        member this.Description =
            match this with
            | Create -> "create"
            | WhatIf -> "what-if"
            | Validate -> "validate"

    let private deployOrValidate (deploymentCommand:DeploymentCommand) resourceGroup deploymentName templateFilename parameters =
        let parametersArgument =
            match parameters with
            | [] -> ""
            | parameters -> sprintf "--parameters %s" (parameters |> List.map(fun (a,b) -> sprintf "%s=%s" a b) |> String.concat " ")
        let cmd = sprintf """deployment group %s -g %s -n %s --template-file %s %s""" deploymentCommand.Description resourceGroup deploymentName templateFilename parametersArgument
        az cmd
    /// Deploys an ARM template to an existing resource group.
    let deploy resourceGroup deploymentName templateFilename parameters = deployOrValidate Create resourceGroup deploymentName templateFilename parameters
    /// Validates whether the specified template is syntactically correct and will be accepted by Azure Resource Manager.
    let validate resourceGroup deploymentName templateFilename parameters = deployOrValidate Validate resourceGroup deploymentName templateFilename parameters |> Result.ignore
    // The what-if operation doesn't make any changes to existing resources. Instead, it predicts the changes if the specified template is deployed.
    let whatIf resourceGroup deploymentName templateFilename parameters = deployOrValidate WhatIf resourceGroup deploymentName templateFilename parameters
    /// Generic function for ZipDeploy using custom command (based on application type)
    let private zipDeploy command appName getZipPath resourceGroup =
        let packageFilename = getZipPath deployFolder
        az (sprintf """%s deployment source config-zip --resource-group "%s" --name "%s" --src %s""" command resourceGroup appName packageFilename)
    /// Deploys a zip file to a web app using the Zip Deploy mechanism.
    let zipDeployWebApp = zipDeploy "webapp"
    /// Deploys a zip file to a function app using the Zip Deploy mechanism.
    let zipDeployFunctionApp = zipDeploy "functionapp"
    let delete resourceGroup =
        az (sprintf "group delete --name %s --yes --no-wait" resourceGroup)
    let enableStaticWebsite name indexDoc errorDoc =
        [ sprintf "storage blob service-properties update --account-name %s --static-website --index-document %s" name indexDoc
          match errorDoc with
          | Some errorDoc -> sprintf "--404-document %s" errorDoc
          | None -> () ]
        |> String.concat " "
        |> az
    let batchUploadStaticWebsite name path =
        az (sprintf "storage blob upload-batch --account-name %s --destination $web --source %s" name path)

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


/// Checks that the version of the Azure CLI meets minimum version.
let checkVersion minimum = result {
    let! versionOutput = Az.version()
    let! version =
        versionOutput.Replace("\r\n","\n").Replace("\r","\n").Split([| "\n" |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.tryHead
        |> Option.bind(fun text ->
            match text.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries) with
            | [| _; version |]
            | [| _; version; _ |] ->
                Some version
            | _ ->
                None)
        |> Option.bind(fun versionText ->
            try Some(Version versionText)
            with _ -> None)
        |> Result.ofOption (sprintf "Unable to determine Azure CLI version. You need to have at least %O installed. Output was: %s" minimum versionOutput)
    return!
        if version < minimum then Error (sprintf "You have %O of the Azure CLI installed, but the minimum version is %O. Please upgrade." version minimum)
        else Ok version
}

/// Sets the currently active (default) subscription.
let setSubscription (subscriptionId:Guid) =
    Az.setSubscription (subscriptionId.ToString())

/// Validates that the parameters supplied meet the deployment requirements.
let validateParameters suppliedParameters deployment =
    let expected = deployment.Template.Parameters |> List.map(fun (SecureParameter p) -> p) |> Set
    let supplied = suppliedParameters |> List.map fst |> Set
    let missing = Set.toList (expected - supplied)
    let extra = Set.toList (supplied - expected)
    match missing, extra with
    | [], [] -> Ok ()
    | (_ :: _), _ -> Error (sprintf "The following parameters are missing: %s. Please add them." (missing |> String.concat ", "))
    | [], (_ :: _) -> Error (sprintf "The following parameters are not required: %s. Please remove them." (extra |> String.concat ", "))

let NoParameters : (string * string) list = []

let private prepareForDeployment parameters resourceGroupName deployment = result {
    do! deployment |> validateParameters parameters

    let! version = checkVersion Az.MinimumVersion
    printfn "Compatible version of Azure CLI %O detected" version

    prepareDeploymentFolder()

    let! subscriptionDetails =
        printf "Checking Azure CLI logged in status... "
        match Az.showAccount() with
        | Ok response ->
            printfn "you are already logged in, nothing to do."
            Ok response
        | Error _ ->
            printfn "logging you in."
            Az.login()
            |> Result.bind(fun _ -> Az.showAccount())

    let subscriptionDetails = subscriptionDetails |> JsonConvert.DeserializeObject<{| id : Guid; name : string |}>
    printfn "Using subscription '%s' (%O)." subscriptionDetails.name subscriptionDetails.id

    printfn "Creating resource group %s..." resourceGroupName
    do! Az.createResourceGroup deployment.Location.ArmValue resourceGroupName

    return
        {| DeploymentName = sprintf "farmer-deploy-%d" (generateDeployNumber())
           TemplateFilename = deployment.Template |> Writer.toJson |> Writer.toFile deployFolder "farmer-deploy" |}
}

/// Validates a deployment against a resource group. If the resource group does not exist, it will be created automatically.
let tryValidate resourceGroupName parameters deployment = result {
    let! deploymentParameters = deployment |> prepareForDeployment parameters resourceGroupName
    return! Az.validate resourceGroupName deploymentParameters.DeploymentName deploymentParameters.TemplateFilename parameters
}

/// Validates a deployment against a resource group. If the resource group does not exist, it will be created automatically.
let tryWhatIf resourceGroupName parameters deployment = result {
    let! deploymentParameters = deployment |> prepareForDeployment parameters resourceGroupName
    return! Az.whatIf resourceGroupName deploymentParameters.DeploymentName deploymentParameters.TemplateFilename parameters
}

/// Executes the supplied Deployment against a resource group using the Azure CLI.
/// If successful, returns a Map of the output keys and values.
let tryExecute resourceGroupName parameters deployment = result {
    let! deploymentParameters = deployment |> prepareForDeployment parameters resourceGroupName

    printfn "Deploying ARM template (please be patient, this can take a while)..."
    let! response = Az.deploy resourceGroupName deploymentParameters.DeploymentName deploymentParameters.TemplateFilename parameters

    do!
        [ for task in deployment.PostDeployTasks do task.Run resourceGroupName ]
        |> List.choose id
        |> Result.sequence
        |> Result.ignore

    printfn "All done, now parsing ARM response to get any outputs..."
    let! response =
        response
        |> Result.ofExn JsonConvert.DeserializeObject<{| properties : {| outputs : Map<string, {| value : string |}> |} |}>
        |> Result.mapError(fun _ -> response)
    return response.properties.outputs |> Map.map (fun _ value -> value.value)
}

/// Executes the supplied Deployment against a resource group using the Azure CLI.
/// If successful, returns a Map of the output keys and values, otherwise returns any error as an exception.
let execute resourceGroupName parameters deployment =
    match tryExecute resourceGroupName parameters deployment with
    | Ok output -> output
    | Error message -> failwith message

let whatIf resourceGroupName parameters deployment =
    match tryWhatIf resourceGroupName parameters deployment with
    | Ok output -> output
    | Error message -> failwith message
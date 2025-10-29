module Farmer.Deploy

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO

let private deployFolder = ".farmer"

let private prepareDeploymentFolder () =
    if Directory.Exists deployFolder then
        Directory.Delete(deployFolder, true)

    Directory.CreateDirectory deployFolder |> ignore

let private generateDeployNumber =
    let r = Random()
    fun () -> r.Next 10000

/// Provides strongly-typed access to the Azure CLI
module Az =
    open System.Runtime.InteropServices
    open System.Text

    let MinimumVersion = Version "2.5.0"

    type AzureCLIToolsNotFound(message: string, innerException: exn) =
        inherit System.Exception(message, innerException)

    [<AutoOpen>]
    module AzHelpers =
        let (|OperatingSystem|_|) platform () =
            if RuntimeInformation.IsOSPlatform platform then
                Some()
            else
                None

        let azCliPath =
            lazy
                match () with
                | OperatingSystem OSPlatform.Windows ->
                    Environment.GetEnvironmentVariable("PATH").Split Path.PathSeparator
                    |> Seq.map (fun s -> Path.Combine(s, "az.cmd"))
                    |> Seq.tryFind File.Exists
                    |> Option.defaultWith (fun () -> invalidOp "Can't find Azure CLI")
                | OperatingSystem OSPlatform.Linux
                | OperatingSystem OSPlatform.OSX -> "az"
                | _ -> raiseFarmer $"OSPlatform: {RuntimeInformation.OSDescription} not supported"

        let executeAz arguments =
            try
                let azProcess =
                    ProcessStartInfo(
                        FileName = azCliPath.Value,
                        Arguments = $"%s{arguments} --output json --only-show-errors",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    )
                    |> Process.Start

                let sb = StringBuilder()

                let flushContents () =
                    let flushStream (stream: StreamReader) =
                        while not stream.EndOfStream do
                            sb.AppendLine(stream.ReadLine()) |> ignore

                    [ azProcess.StandardOutput; azProcess.StandardError ] |> List.iter flushStream

                flushContents () // For some reason if we don't try flushing before waiting for exit, sometimes stdout crashes.
                azProcess.WaitForExit()
                flushContents ()
                azProcess, sb.ToString()
            with
            | :? System.ComponentModel.Win32Exception as e when e.Message.Contains("No such file or directory") ->
                let message =
                    $"Could not find Azure CLI tools on {azCliPath.Value}. Make sure you've setup the Azure CLI tools. Go to https://compositionalit.github.io/farmer/quickstarts/quickstart-3/#install-the-azure-cli for more information."

                AzureCLIToolsNotFound(message, e) |> raise
            | _ -> reraise ()

        let processToResult (p: Process, response) =
            match p.ExitCode with
            | 0 -> Ok response
            | _ -> Error response

    /// Executes a generic AZ CLI command.
    let az = AzHelpers.executeAz >> processToResult
    /// Tests if the Az CLI has logged in credentials.
    let showAccount () = az "account show"
    /// Logs you into Az CLI interactively.
    let login () = az "login" |> Result.ignore

    /// Logs you into the Az CLI using the supplied service principal credentials.
    let loginWithCredentials appId secret tenantId =
        try
            // Set credentials in environment variables (process-local, not inherited)
            Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", appId, EnvironmentVariableTarget.Process)
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", secret, EnvironmentVariableTarget.Process)
            Environment.SetEnvironmentVariable("AZURE_TENANT_ID", tenantId, EnvironmentVariableTarget.Process)

            // Login without password in command line
            let result =
                az $"login --service-principal --username %s{appId} --tenant %s{tenantId}"

            // Clean up immediately after use
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", null, EnvironmentVariableTarget.Process)

            result
        with ex ->
            // Ensure cleanup on error
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", null, EnvironmentVariableTarget.Process)
            reraise ()

    /// Gets the version of Az CLI
    let version () = az "--version"

    /// Checks that the version of the Azure CLI meets minimum version.
    let checkVersion minimum = result {
        let! versionOutput = version ()

        let! version =
            versionOutput
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Split([| "\n" |], StringSplitOptions.RemoveEmptyEntries)
            |> Array.tryHead
            |> Option.bind (fun text ->
                match text.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries) with
                | [| _; version |]
                | [| _; version; _ |] -> Some version
                | _ -> None)
            |> Option.bind (fun versionText ->
                try
                    Some(Version versionText)
                with _ ->
                    None)
            |> Result.ofOption
                $"Unable to determine Azure CLI version. You need to have at least {minimum} installed. Output was: %s{versionOutput}"

        return!
            if version < minimum then
                Error
                    $"You have {version} of the Azure CLI installed, but the minimum version is {minimum}. Please upgrade."
            else
                Ok version
    }

    /// Lists all subscriptions
    let listSubscriptions () = az "account list --all"

    let setSubscription subscriptionId =
        az $"account set --subscription %s{subscriptionId}"

    /// Creates a resource group.
    let createResourceGroup location tags resourceGroup =
        let tagString =
            match Map.toList tags with
            | [] -> ""
            | tagList ->
                tagList
                |> Seq.map (fun (key, value) -> $"{key}={value}")
                |> String.concat " "
                |> sprintf " --tags %s"

        az $"group create -l %s{location} -n %s{resourceGroup}%s{tagString}"
        |> Result.ignore

    /// Searches for users in AD using the supplied filter.
    let searchUsers filter = az $"ad user list --filter %s{filter}"
    /// Searches for groups in AD using the supplied filter.
    let searchGroups filter = az $"ad group list --filter %s{filter}"

    type DeploymentCommand =
        | Create
        | WhatIf
        | Validate

        member this.Description =
            match this with
            | Create -> "create"
            | WhatIf -> "what-if"
            | Validate -> "validate"

    let private deployOrValidate
        (deploymentCommand: DeploymentCommand)
        resourceGroup
        deploymentName
        templateFilename
        parameters
        =
        let escapeParameterArguments =
            (fun (a, (b: string)) ->
                let escapedValue = b.Replace("\"", "\\\"")
                $"\"%s{a}\"=\"%s{escapedValue}\"")

        let parametersArgument =
            match parameters with
            | [] -> ""
            | parameters ->
                sprintf "--parameters %s" (parameters |> List.map (escapeParameterArguments) |> String.concat " ")

        az
            $"""deployment group {deploymentCommand.Description} -g {resourceGroup} -n {deploymentName} --template-file {templateFilename} {parametersArgument}"""

    /// Deploys an ARM template to an existing resource group.
    let deploy resourceGroup deploymentName templateFilename parameters =
        deployOrValidate Create resourceGroup deploymentName templateFilename parameters

    /// Validates whether the specified template is syntactically correct and will be accepted by Azure Resource Manager.
    let validate resourceGroup deploymentName templateFilename parameters =
        deployOrValidate Validate resourceGroup deploymentName templateFilename parameters
        |> Result.ignore
    // The what-if operation doesn't make any changes to existing resources. Instead, it predicts the changes if the specified template is deployed.
    let whatIf resourceGroup deploymentName templateFilename parameters =
        deployOrValidate WhatIf resourceGroup deploymentName templateFilename parameters

    /// Generic function for ZipDeploy using custom command (based on application type)
    let private zipDeploy command appName getZipPath resourceGroup slotName =
        let packageFilename = getZipPath deployFolder |> sprintf "\"%s\""

        let slotArg =
            match slotName with
            | None -> ""
            | Some n -> $"--slot %s{n} "

        az
            $"""%s{command} deployment source config-zip %s{slotArg}--resource-group "%s{resourceGroup}" --name "%s{appName}" --src %s{packageFilename}"""

    /// Deploys a zip file to a web app using the Zip Deploy mechanism.
    let zipDeployWebApp = zipDeploy "webapp"
    /// Deploys a zip file to a function app using the Zip Deploy mechanism.
    let zipDeployFunctionApp = zipDeploy "functionapp"

    let delete resourceGroup =
        az $"group delete --name %s{resourceGroup} --yes --no-wait"

    let enableStaticWebsite name indexDoc errorDoc =
        [
            $"storage blob service-properties update --account-name %s{name} --static-website --index-document %s{indexDoc}"
            yield! errorDoc |> Option.mapList (sprintf "--404-document %s")
        ]
        |> String.concat " "
        |> az

    /// The overwrite parameter was introduced in Azure CLI v2.34.0 with a breaking change to the default behaviour
    let private cliVersionWithOverwriteParameter = Version "2.34.0"

    let batchUploadStaticWebsite name path =
        let additionalParameters =
            match checkVersion cliVersionWithOverwriteParameter with
            | Ok _ -> "--overwrite true"
            | _ -> ""

        az
            $"storage blob upload-batch --account-name %s{name} --destination $web --source %s{path} {additionalParameters}"

    type AzureErrorCode = { Code: string; Message: string }
    type AzureError = { Error: AzureErrorCode }

    let tryGetError (error: string) =
        try
            let skip =
                "Deployment failed. Correlation ID: 3c51a527-c6e2-42a9-acee-7d9c796a626f. ".Length

            match Serialization.ofJson<AzureError> error[skip..] with
            | {
                  Error = {
                              Code = "RoleAssignmentExists"
                              Message = "The role assignment already exists."
                          }
              } ->
                "A role assignment defined in this template already exists in Azure, but with a different GUID. If you have recently upgraded to Farmer 1.5, please be aware of a breaking change in the generation of role assignment GUIDs. To resolve this, locate the resource group in the Azure portal, remove the existing role assignment from IAM and then redeploy your Farmer template."
            | _ -> error
        with _ ->
            error

/// Represents an Azure subscription
type Subscription = {
    ID: Guid
    Name: string
    IsDefault: bool
}

/// Authenticates the Az CLI using the supplied ApplicationId, Client Secret and Tenant Id.
/// Returns the list of subscriptions, including which one the default is.
let authenticate appId secret tenantId =
    Az.loginWithCredentials appId secret tenantId
    |> Result.map (Serialization.ofJson<Subscription[]>)

/// Lists all subscriptions that the logged in identity has access to.
let listSubscriptions () = result {
    let! response = Az.listSubscriptions ()
    return response |> Serialization.ofJson<Subscription array>
}

/// Sets the currently active (default) subscription.
let setSubscription (subscriptionId: Guid) =
    Az.setSubscription (subscriptionId.ToString())

/// Validates that the parameters supplied meet the deployment requirements.
let validateParameters suppliedParameters (deployment: IDeploymentSource) =
    let expected =
        deployment.Deployment.Template.Parameters
        |> List.map (fun (SecureParameter p) -> p)
        |> Set

    let supplied = suppliedParameters |> List.map fst |> Set
    let missing = Set.toList (expected - supplied)
    let extra = Set.toList (supplied - expected)

    match missing, extra with
    | [], [] -> Ok()
    | (_ :: _), _ ->
        Error(sprintf "The following parameters are missing: %s. Please add them." (missing |> String.concat ", "))
    | [], (_ :: _) ->
        Error(
            sprintf "The following parameters are not required: %s. Please remove them." (extra |> String.concat ", ")
        )

let NoParameters: (string * string) list = []

let private prepareForDeployment parameters resourceGroupName (deployment: IDeploymentSource) = result {
    do! deployment |> validateParameters parameters

    let! version = Az.checkVersion Az.MinimumVersion
    stdout.WriteLine $"Compatible version of Azure CLI {version} detected"

    prepareDeploymentFolder ()

    let! subscriptionDetails =
        stdout.Write "Checking Azure CLI logged in status... "

        match Az.showAccount () with
        | Ok response ->
            stdout.WriteLine "you are already logged in, nothing to do."
            Ok response
        | Error _ ->
            stdout.WriteLine "logging you in."
            Az.login () |> Result.bind (fun _ -> Az.showAccount ())

    let subscriptionDetails =
        subscriptionDetails |> Serialization.ofJson<{| id: Guid; name: string |}>

    stdout.WriteLine $"Using subscription '%s{subscriptionDetails.name}' ({subscriptionDetails.id})."

    match deployment.Deployment.Location with
    | Location _ ->
        let resourceGroups =
            (resourceGroupName :: deployment.Deployment.RequiredResourceGroups)
            |> List.distinct
            // Filter out any resource groups that are an ARM expression calculated at deploy-time
            |> List.filter (fun resGroupName -> not (resGroupName.StartsWith("[")))
            |> List.mapi (fun i x -> i, x)

        for (i, rg) in resourceGroups do
            stdout.WriteLine $"Creating resource group {rg} ({i + 1}/{resourceGroups.Length})..."
            do! Az.createResourceGroup deployment.Deployment.Location.ArmValue deployment.Deployment.Tags rg
    | LocationExpression _ ->
        stdout.WriteLine
            "Deployment location is an ARM expression that cannot be evaluated by the CLI. Skipping resource group creation."

        return () // Cannot evaluate an ARM expression in Az CLI.

    return {|
        DeploymentName = $"farmer-deploy-{generateDeployNumber ()}"
        TemplateFilename =
            deployment.Deployment.Template
            |> Writer.toJson
            |> Writer.toFile deployFolder "farmer-deploy"
    |}
}

/// Validates a deployment against a resource group. If the resource group does not exist, it will be created automatically.
let tryValidate resourceGroupName parameters (deployment: IDeploymentSource) = result {
    let! deploymentParameters = deployment |> prepareForDeployment parameters resourceGroupName

    return!
        Az.validate
            resourceGroupName
            deploymentParameters.DeploymentName
            deploymentParameters.TemplateFilename
            parameters
}

/// Validates a deployment against a resource group. If the resource group does not exist, it will be created automatically.
let tryWhatIf resourceGroupName parameters (deployment: IDeploymentSource) = result {
    let! deploymentParameters = deployment |> prepareForDeployment parameters resourceGroupName

    return!
        Az.whatIf resourceGroupName deploymentParameters.DeploymentName deploymentParameters.TemplateFilename parameters
}

/// Executes the supplied Deployment against a resource group using the Azure CLI.
/// If successful, returns a Map of the output keys and values.
let tryExecute resourceGroupName parameters (deployment: IDeploymentSource) = result {
    let! deploymentParameters = deployment |> prepareForDeployment parameters resourceGroupName

    stdout.WriteLine "Deploying ARM template (please be patient, this can take a while)..."

    let! response =
        Az.deploy resourceGroupName deploymentParameters.DeploymentName deploymentParameters.TemplateFilename parameters

    do!
        [
            for task in deployment.Deployment.PostDeployTasks do
                task.Run resourceGroupName
        ]
        |> List.choose id
        |> Result.sequence
        |> Result.ignore

    stdout.WriteLine "All done, now parsing ARM response to get any outputs..."

    let! response =
        response
        |> Result.ofExn
            Serialization.ofJson<
                {|
                    properties:
                        {|
                            outputs: IDictionary<string, {| value: string |}>
                        |}
                |}
             >
        |> Result.mapError (fun _ -> response)

    return
        response.properties.outputs
        |> Seq.map (fun r -> r.Key, r.Value.value)
        |> Map.ofSeq
}

/// Executes the supplied Deployment against a resource group using the Azure CLI.
/// If successful, returns a Map of the output keys and values, otherwise returns any error as an exception.
let execute resourceGroupName parameters (deployment: IDeploymentSource) =
    match tryExecute resourceGroupName parameters deployment with
    | Ok output -> output
    | Error message -> raiseFarmer (Az.tryGetError message)

let whatIf resourceGroupName parameters (deployment: IDeploymentSource) =
    match tryWhatIf resourceGroupName parameters deployment with
    | Ok output -> output
    | Error message -> raiseFarmer message
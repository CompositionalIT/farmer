[<AutoOpen>]
module Farmer.Arm.DeploymentScript

open Farmer
open Farmer.Identity
open System

let deploymentScripts =
    ResourceType("Microsoft.Resources/deploymentScripts", "2019-10-01-preview")

type CliVersion =
    | AzCli of string // current is 2.9.1 - supported versions listed here: https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/deployment-script-template?tabs=CLI#prerequisites
    | AzPowerShell of string // current is 4.7

// The contents or location of the primary deployment script
type ScriptSource =
    | Content of string
    | Remote of Uri

[<RequireQualifiedAccess>]
type Cleanup =
    | Always
    | OnSuccess
    | OnExpiration of TimeSpan

type DeploymentScript = {
    Name: ResourceName
    Location: Location
    Dependencies: ResourceId Set
    Arguments: string list
    CleanupPreference: Cleanup
    Cli: CliVersion
    EnvironmentVariables: Map<string, EnvVar>
    ForceUpdateTag: Guid option
    Identity: UserAssignedIdentity
    ScriptSource: ScriptSource
    SupportingScriptUris: Uri list
    Timeout: TimeSpan option
    Tags: Map<string, string>
} with

    interface IParameters with
        member this.SecureParameters = [
            for envVar in this.EnvironmentVariables do
                match envVar.Value with
                | SecureEnvValue p -> p
                | _ -> ()
        ]

    interface IArmResource with
        member this.ResourceId = deploymentScripts.resourceId this.Name

        member this.JsonModel =
            let cliKind, azCliVersion, azPowerShellVersion =
                match this.Cli with
                | AzCli version -> "AzureCLI", version, null
                | AzPowerShell version -> "AzurePowerShell", null, version

            let dependencies = this.Dependencies.Add this.Identity.ResourceId

            {|
                deploymentScripts.Create(this.Name, this.Location, dependencies, this.Tags) with
                    kind = cliKind
                    identity =
                        {
                            SystemAssigned = Disabled
                            UserAssigned = [ this.Identity ]
                        }
                            .ToArmJson
                    properties = {|
                        arguments =
                            match this.Arguments with
                            | [] -> null
                            | args -> String.concat " " args
                        azPowerShellVersion = azPowerShellVersion
                        azCliVersion = azCliVersion
                        cleanupPreference =
                            match this.CleanupPreference with
                            | Cleanup.OnSuccess -> "OnSuccess"
                            | Cleanup.OnExpiration _ -> "OnExpiration"
                            | Cleanup.Always -> "Always"
                        environmentVariables = [
                            for key, value in Map.toSeq this.EnvironmentVariables do
                                match value with
                                | EnvValue v -> {|
                                    name = key
                                    value = v
                                    secureValue = null
                                  |}
                                | SecureEnvExpression armExpression -> {|
                                    name = key
                                    value = null
                                    secureValue = armExpression.Eval()
                                  |}
                                | SecureEnvValue v -> {|
                                    name = key
                                    value = null
                                    secureValue = v.ArmExpression.Eval()
                                  |}
                        ]
                        forceUpdateTag = this.ForceUpdateTag |> Option.toNullable
                        scriptContent =
                            match this.ScriptSource with
                            | Content content -> content
                            | Remote _ -> null
                        primaryScriptUri =
                            match this.ScriptSource with
                            | Content _ -> null
                            | Remote uri -> uri
                        retentionInterval =
                            match this.CleanupPreference with
                            | Cleanup.OnSuccess
                            | Cleanup.Always -> TimeSpan.FromDays 1.
                            | Cleanup.OnExpiration retention -> retention
                            |> Xml.XmlConvert.ToString
                        supportingScriptUris =
                            match this.SupportingScriptUris with
                            | [] -> null
                            | uris -> uris |> Seq.map string |> ResizeArray
                        timeout = this.Timeout |> Option.map Xml.XmlConvert.ToString |> Option.toObj
                    |}
            |}
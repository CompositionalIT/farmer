[<AutoOpen>]
module Farmer.Arm.DeploymentScript

open Farmer
open Farmer.Identity
open System

let deploymentScripts = ResourceType ("Microsoft.Resources/deploymentScripts", "2019-10-01-preview")

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

type DeploymentScript =
    { Name : ResourceName
      Location : Location
      AdditionalDependencies : ResourceId list
      Arguments : string list
      CleanupPreference : Cleanup option
      Cli : CliVersion
      EnvironmentVariables: Map<string, EnvVar>
      ForceUpdateTag : Guid option
      Identity : UserAssignedIdentity
      ScriptSource : ScriptSource
      SupportingScriptUris : Uri list
      Timeout : TimeSpan option
      Tags: Map<string,string> }
    member private this.Dependencies = this.Identity.ResourceId :: this.AdditionalDependencies
    interface IArmResource with
        member this.ResourceId = deploymentScripts.resourceId this.Name
        member this.JsonModel =
            let cliKind, azCliVersion, azPowerShellVersion =
                match this.Cli with
                | AzCli version -> "AzureCLI", version, null
                | AzPowerShell version -> "AzurePowerShell", null, version
            let cleanup, retention =
                let defaultRetentionInterval = TimeSpan.FromDays 1.
                match this.CleanupPreference with
                | Some Cleanup.OnSuccess -> "OnSuccess", defaultRetentionInterval
                | Some (Cleanup.OnExpiration retention) -> "OnExpiration", retention
                | Some Cleanup.Always
                | None -> "Always", defaultRetentionInterval
            {| deploymentScripts.Create(this.Name, this.Location, this.Dependencies, this.Tags) with
                kind = cliKind
                identity = { SystemAssigned = Disabled; UserAssigned = [ this.Identity ] } |> ManagedIdentity.toArmJson
                properties =
                    {| arguments = match this.Arguments with [] -> null | args -> String.concat " " args
                       azPowerShellVersion = azPowerShellVersion
                       azCliVersion = azCliVersion
                       cleanupPreference = cleanup
                       environmentVariables = [
                         for (key, value) in Map.toSeq this.EnvironmentVariables do
                             match value with
                             | EnvValue v -> {| name = key; value = v; secureValue = null |}
                             | SecureEnvValue v -> {| name = key; value = null; secureValue = v |}
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
                       retentionInterval = retention |> Xml.XmlConvert.ToString
                       supportingScriptUris =
                           match this.SupportingScriptUris with
                           | [] -> null
                           | uris -> uris |> Seq.map string |> ResizeArray
                       timeout = this.Timeout |> Option.map Xml.XmlConvert.ToString |> Option.toObj |}
            |} :> _
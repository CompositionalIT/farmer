[<AutoOpen>]
module Farmer.Arm.DeploymentScript

open Farmer
open Farmer.CoreTypes
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

type DeploymentScript =
    { Name : ResourceName
      Location : Location
      Arguments : string list
      Cli : CliVersion
      EnvironmentVariables: Map<string, EnvVar>
      ForceUpdateTag : Guid option
      Identity : UserAssignedIdentity
      ScriptSource : ScriptSource
      RetentionInterval : TimeSpan option
      SupportingScriptUris : Uri list
      Timeout : TimeSpan option
      Tags: Map<string,string> }
    member private this.Dependencies = [ this.Identity.ResourceId ]
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            let cliKind, azCliVersion, azPowerShellVersion =
                match this.Cli with
                | AzCli version -> "AzureCLI", version, null
                | AzPowerShell version -> "AzurePowerShell", null, version
            {| deploymentScripts.Create(this.Name, this.Location, this.Dependencies, this.Tags) with
                kind = cliKind
                identity = { SystemAssigned = Disabled; UserAssigned = [ this.Identity ] } |> ManagedIdentity.toArmJson
                properties =
                    {| arguments = match this.Arguments with [] -> null | args -> String.concat " " args
                       azPowerShellVersion = azPowerShellVersion
                       azCliVersion = azCliVersion
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
                       retentionInterval =
                           this.RetentionInterval
                           |> Option.defaultValue (TimeSpan.FromDays 1.)
                           |> Xml.XmlConvert.ToString
                       supportingScriptUris =
                           match this.SupportingScriptUris with
                           | [] -> null
                           | uris -> uris |> Seq.map string |> ResizeArray
                       timeout = this.Timeout |> Option.map Xml.XmlConvert.ToString |> Option.toObj |}
            |} :> _
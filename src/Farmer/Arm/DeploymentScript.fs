[<AutoOpen>]
module Farmer.Arm.DeploymentScript

open Farmer
open Farmer.CoreTypes
open Farmer.Identity

let deploymentScripts = ResourceType ("Microsoft.Resources/deploymentScripts", "2019-10-01-preview")

type CliVersion =
    | AzCli of string // current is 2.9.1 - supported versions listed here: https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/deployment-script-template?tabs=CLI#prerequisites
    | AzPowerShell of string // current is 4.7

type DeploymentScript =
    { Name : ResourceName
      Location : Location
      Arguments : string list
      Cli : CliVersion
      EnvironmentVariables: Map<string, EnvVarValue>
      ForceUpdateTag : string option
      Identity : ManagedIdentity
      PrimaryScriptUri : System.Uri option
      RetentionInterval : System.TimeSpan option
      ScriptContent : string option
      SupportingScriptUris : System.Uri list
      Timeout : System.TimeSpan option
      Tags: Map<string,string> }
    member private this.Dependencies = this.Identity.Dependencies
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            let cliKind, azCliVersion, azPowerShellVersion =
                match this.Cli with
                | AzCli version -> "AzureCLI", version, null
                | AzPowerShell version -> "AzurePowerShell", null, version
            {| deploymentScripts.Create(this.Name, this.Location, this.Dependencies, this.Tags) with
                   kind = cliKind
                   identity = this.Identity |> ManagedIdentity.toArmJson
                   properties =
                       {| arguments = match this.Arguments with | [] -> null | args -> String.concat " " args
                          azPowerShellVersion = azPowerShellVersion
                          azCliVersion = azCliVersion
                          environmentVariables = [
                              for (key, value) in Map.toSeq this.EnvironmentVariables do
                                  match value with
                                  | EnvValue v -> {| name = key; value = v; secureValue = null |}
                                  | EnvSecureValue v -> {| name = key; value = null; secureValue = v |}
                              ]
                          forceUpdateTag = this.ForceUpdateTag |> Option.toObj
                          primaryScriptUri = this.PrimaryScriptUri |> Option.map string |> Option.toObj
                          retentionInterval =
                              this.RetentionInterval
                              |> Option.defaultValue (System.TimeSpan.FromDays 1.)
                              |> System.Xml.XmlConvert.ToString
                          scriptContent = this.ScriptContent |> Option.toObj
                          supportingScriptUris =
                              match this.SupportingScriptUris with
                              | [] -> null
                              | uris -> uris |> Seq.map string |> ResizeArray
                          timeout = this.Timeout |> Option.map System.Xml.XmlConvert.ToString |> Option.toObj |}
            |} :> _
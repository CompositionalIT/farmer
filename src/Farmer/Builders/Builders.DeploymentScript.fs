[<AutoOpen>]
module Farmer.Builders.DeploymentScript

open Farmer
open Farmer.Arm
open Farmer.CoreTypes
open Farmer.Identity

type DeploymentScriptConfig =
    { Name : ResourceName
      Arguments : string list
      Cli : CliVersion
      EnvironmentVariables: Map<string, EnvVarValue>
      ForceUpdateTag : string option
      Identity : ManagedIdentity
      PrimaryScriptUri : System.Uri option
      RetentionInterval : System.TimeSpan option
      ScriptContent : string option
      SupportingScriptUris : System.Uri list
      Tags : Map<string,string>
      Timeout : System.TimeSpan option }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            { Location = location
              Name = this.Name
              Arguments = this.Arguments
              Cli = this. Cli
              EnvironmentVariables = this.EnvironmentVariables
              ForceUpdateTag = this.ForceUpdateTag
              Identity = this.Identity
              PrimaryScriptUri = this.PrimaryScriptUri
              RetentionInterval = this.RetentionInterval
              ScriptContent = this.ScriptContent
              SupportingScriptUris = this.SupportingScriptUris
              Tags = this.Tags
              Timeout = this.Timeout }
        ]

type DeploymentScriptBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Arguments = []
          Cli = AzCli "2.9.1"
          EnvironmentVariables = Map.empty
          ForceUpdateTag = None
          Identity = ManagedIdentity.Empty
          PrimaryScriptUri = None
          RetentionInterval = None
          ScriptContent = None
          SupportingScriptUris = []
          Tags = Map.empty
          Timeout = None
        }
    /// Sets the name of the container instance.
    [<CustomOperation "name">]
    member __.Name(state:DeploymentScriptConfig, name) = { state with Name = name }
    member this.Name(state:DeploymentScriptConfig, name) = this.Name(state, ResourceName name)
    /// Arguments which will become a space separated string of arguments passed to the script.
    [<CustomOperation "arguments">]
    member this.Arguments(state:DeploymentScriptConfig, arguments:string list) =
        { state with Arguments = state.Arguments @ arguments }
    /// Specify the CLI type and version to use - defaults to the 'az cli' version 2.12.1.
    [<CustomOperation "cli">]
    member this.Cli(state:DeploymentScriptConfig, cliVersion:CliVersion) = { state with Cli = cliVersion }
    /// Content to run - either a script or a URL where a script can be downloaded.
    [<CustomOperation "content">]
    /// The script to execute.
    member this.Content(state:DeploymentScriptConfig, content:string) = { state with ScriptContent = Some content }
    /// A URI to download a script to execute.
    member this.Content(state:DeploymentScriptConfig, scriptUri:System.Uri) = { state with PrimaryScriptUri = Some scriptUri }
    /// Environment variables for the script.
    [<CustomOperation "env_vars">]
    member __.EnvironmentVariables(state:DeploymentScriptConfig, envVars) =
        { state with EnvironmentVariables=Map.ofList envVars }
    /// A tag that can be changed on deployment to ensure it will be run again even if there are no script changes.
    [<CustomOperation "force_update_tag">]
    member this.ForceUpdateTag(state:DeploymentScriptConfig, forceUpdateTag:string) =
        { state with ForceUpdateTag = Some forceUpdateTag }
    /// Sets the user assigned managed identity under which this deployment script runs.
    [<CustomOperation "identity">]
    member _.Identity(state:DeploymentScriptConfig, identity:UserAssignedIdentity) = { state with Identity = state.Identity + identity }
    member this.Identity(state, identity:UserAssignedIdentityConfig) = this.Identity(state, identity.UserAssignedIdentity)
    /// URI to download the primary script to execute.
    [<CustomOperation "primary_script_uri">]
    member this.PrimaryScriptUri(state:DeploymentScriptConfig, primaryScriptUri:System.Uri) =
        { state with PrimaryScriptUri = Some primaryScriptUri }
    member this.PrimaryScriptUri(state:DeploymentScriptConfig, primaryScriptUri:string) =
        { state with PrimaryScriptUri = Some (System.Uri primaryScriptUri) }
    /// Time to retain the container instance that runs the script - 1 to 30 days.
    [<CustomOperation "retention_interval_days">]
    member this.RetentionInterval(state:DeploymentScriptConfig, retentionInterval:int) =
        let maxRetention = min retentionInterval 30 // Max retention is 30 days
        { state with RetentionInterval = Some (System.TimeSpan.FromDays (float maxRetention)) }
    [<CustomOperation "script_content">]
    member this.ScriptContent(state:DeploymentScriptConfig, scriptContent:string) =
        { state with ScriptContent = Some scriptContent }
    /// Additional URI's to download scripts that the primary script relies on.
    [<CustomOperation "supporting_script_uris">]
    member this.SupportingScriptUris(state:DeploymentScriptConfig, supportingScriptUris:System.Uri list) =
        { state with SupportingScriptUris = state.SupportingScriptUris @ supportingScriptUris }
    /// Timeout for script execution.
    [<CustomOperation "timeout">]
    member this.Timeout(state:DeploymentScriptConfig, timeout:System.TimeSpan) =
        { state with Timeout = Some timeout }
    /// Timeout for script execution in ISO 8601 format, e.g. PT30M.
    member this.Timeout(state:DeploymentScriptConfig, timeout:string) =
        { state with Timeout = Some (System.Xml.XmlConvert.ToTimeSpan timeout) }
    [<CustomOperation "add_tags">]
    member _.Tags(state:DeploymentScriptConfig, pairs) =
        { state with
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
    member this.Tag(state:DeploymentScriptConfig, key, value) = this.Tags(state, [ (key,value) ])

let deploymentScript = DeploymentScriptBuilder()

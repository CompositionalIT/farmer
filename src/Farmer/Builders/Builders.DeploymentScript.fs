[<AutoOpen>]
module Farmer.Builders.DeploymentScript

open Farmer
open Farmer.Arm.DeploymentScript
open Farmer.Arm.ManagedIdentity
open Farmer.Arm.RoleAssignment
open Farmer.CoreTypes
open Farmer.Identity
open System

type OutputCollection(owner) =
    member this.Item
        with get key =
            ArmExpression
                .reference(deploymentScripts, ResourceId.create(deploymentScripts, owner))
                .Map(fun v -> v + ".outputs." + key)

type DeploymentScriptConfig =
    { Name : ResourceName
      Arguments : string list
      Cli : CliVersion
      EnvironmentVariables: Map<string, EnvVar>
      ForceUpdate : bool
      CustomIdentity : UserAssignedIdentity option
      ScriptSource : ScriptSource
      RetentionInterval : TimeSpan option
      SupportingScriptUris : Uri list
      Tags : Map<string,string>
      Timeout : TimeSpan option }

    member this.Outputs = OutputCollection this.Name

    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            let generatedIdentityId =
                let generatedIdentityName = sprintf "%s-identity" this.Name.Value |> ResourceName
                ResourceId.create (userAssignedIdentities, generatedIdentityName)

            // User Assigned Identity - create one if none was supplied.
            if this.CustomIdentity.IsNone then
                { Name = generatedIdentityId.Name
                  Location = location
                  Tags = Map.empty }

            let identity =
                this.CustomIdentity
                |> Option.defaultValue (UserAssignedIdentity generatedIdentityId)

            // Assignment
            { Name =
                (sprintf "guid(concat(resourceGroup().id, '%O'))" Roles.Contributor.Id
                |> ArmExpression.create).Eval()
                |> ResourceName
              RoleDefinitionId = Roles.Contributor
              PrincipalId = identity.PrincipalId
              PrincipalType = PrincipalType.ServicePrincipal
              Scope = AssignmentScope.ResourceGroup }

            // Deployment Script
            { Location = location
              Name = this.Name
              Arguments = this.Arguments
              Cli = this.Cli
              EnvironmentVariables = this.EnvironmentVariables
              ForceUpdateTag = if this.ForceUpdate then Some (Guid.NewGuid()) else None
              Identity = identity
              ScriptSource = this.ScriptSource
              RetentionInterval = this.RetentionInterval
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
          ForceUpdate = false
          CustomIdentity = None
          ScriptSource = Content ""
          RetentionInterval = None
          SupportingScriptUris = []
          Tags = Map.empty
          Timeout = None }

    /// Sets the name of the container instance.
    [<CustomOperation "name">]
    member _.Name(state:DeploymentScriptConfig, name) = { state with Name = name }
    member this.Name(state:DeploymentScriptConfig, name) = this.Name(state, ResourceName name)
    /// Arguments which will become a space separated string of arguments passed to the script.
    [<CustomOperation "arguments">]
    member _.Arguments(state:DeploymentScriptConfig, arguments) =
        { state with Arguments = state.Arguments @ arguments }
    /// Specify the CLI type and version to use - defaults to the 'az cli' version 2.12.1.
    [<CustomOperation "cli">]
    member _.Cli(state:DeploymentScriptConfig, cliVersion) = { state with Cli = cliVersion }
    /// The contents of the script to execute.
    [<CustomOperation "script_content">]
    member _.Content(state:DeploymentScriptConfig, content) = { state with ScriptSource = Content content }
    /// URI to download the primary script to execute.
    [<CustomOperation "script_uri">]
    member _.PrimaryScriptUri(state:DeploymentScriptConfig, primaryScriptUri) =
        { state with ScriptSource = Remote primaryScriptUri }
    member this.PrimaryScriptUri(state, primaryScriptUri) =
        this.PrimaryScriptUri(state, Uri primaryScriptUri)
    /// Environment variables for the script.
    [<CustomOperation "env_vars">]
    member _.EnvironmentVariables(state:DeploymentScriptConfig, envVars) =
        { state with EnvironmentVariables = Map envVars }
    member this.EnvironmentVariables(state, envVars) =
        this.EnvironmentVariables(state, envVars |> List.map(fun (k, v) -> k, EnvValue v))
    /// Ensures that your script be run on deployment even if there are no changes since the last deployment.
    [<CustomOperation "force_update">]
    member _.ForceUpdate(state:DeploymentScriptConfig) =
        { state with ForceUpdate = true }
    member _.ForceUpdate(state:DeploymentScriptConfig, value) =
        { state with ForceUpdate = value }
    /// Sets the user assigned managed identity under which this deployment script runs. If none is supplied, a new identity will be automatically created.
    [<CustomOperation "identity">]
    member _.Identity(state:DeploymentScriptConfig, identity) = { state with CustomIdentity = Some identity }
    member this.Identity(state, identity:UserAssignedIdentityConfig) = this.Identity(state, identity.UserAssignedIdentity)
    /// Time to retain the container instance that runs the script - 1 to 26 hours.
    [<CustomOperation "retention_interval">]
    member _.RetentionInterval(state:DeploymentScriptConfig, retentionInterval) =
        let maxRetention = min retentionInterval 30<Hours>
        { state with RetentionInterval = Some (TimeSpan.FromHours (float maxRetention)) }
    /// Additional URIs to download scripts that the primary script relies on.
    [<CustomOperation "supporting_script_uris">]
    member _.SupportingScriptUris(state:DeploymentScriptConfig, supportingScriptUris) =
        { state with SupportingScriptUris = supportingScriptUris }
    /// Timeout for script execution.
    [<CustomOperation "timeout">]
    member _.Timeout(state:DeploymentScriptConfig, timeout) =
        { state with Timeout = Some timeout }
    /// Timeout for script execution in ISO 8601 format, e.g. PT30M.
    member _.Timeout(state:DeploymentScriptConfig, timeout) =
        { state with Timeout = Some (Xml.XmlConvert.ToTimeSpan timeout) }
    [<CustomOperation "add_tags">]
    member _.Tags(state:DeploymentScriptConfig, pairs) =
        { state with
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
    member this.Tag(state:DeploymentScriptConfig, key, value) = this.Tags(state, [ (key,value) ])

let deploymentScript = DeploymentScriptBuilder()

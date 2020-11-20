[<AutoOpen>]
module Farmer.Builders.ResourceGroup

open Farmer
open Farmer.CoreTypes
open Farmer.Arm.ResourceGroup

/// Represents all configuration information to generate an ARM template.
type ResourceGroupConfig =
    { Name: Option<ResourceName>
      Parameters : string Set
      Outputs : Map<string, string>
      Location : Location 
      Resources : IArmResource list
      Tags: Map<string,string>
      DeploymentMode: DeploymentMode}
    member private this.ResourceName = this.Name |> Option.defaultValue (ResourceName "farmer-resource-group")
    interface IBuilder with
        member this.DependencyName = this.ResourceName
        member this.BuildResources location = 
            match this.Resources with
            | [] -> []
            | _ ->
                [{ ResourceGroupName = this.ResourceName
                   Location = this.Location
                   Resources = this.Resources
                   Tags = this.Tags
                   DeploymentMode = this.DeploymentMode}]
    interface IParameters with
        member this.SecureParameters =
            let nestedParams = 
                this.Resources
                |> List.choose 
                    (function 
                     | :? IParameters as p -> Some p.SecureParameters
                     | _ -> None)
            let thisParams = 
                this.Parameters 
                |> Set.map SecureParameter 
                |> Set.toList
            thisParams::nestedParams
            |> List.collect id
        
    interface ISubscriptionResourceBuilder with
        member this.Outputs = this.Outputs
        member this.BuildResources () = 
            [
                { Name = this.ResourceName
                  Location = this.Location
                  Tags = this.Tags }
                match this.Resources with
                | [] -> ()
                | _ ->
                    { ResourceGroupName = this.ResourceName
                      Location = this.Location
                      Resources = this.Resources
                      Tags = this.Tags
                      DeploymentMode = this.DeploymentMode}
            ]
        member this.RunPostDeployTasks () =
            this.Resources
            |> List.choose 
                (function
                | :? IPostDeploy as pd -> pd.Run this.ResourceName.Value
                | _ -> None)   
            
    interface IDeploymentBuilder with
        member this.BuildDeployment name =
            let resolvedName = Option.orElse (Some (ResourceName name)) this.Name
            subscriptionDeployment {
                location this.Location
                add_resource_group {this with Name = resolvedName }
            }
            |>  Deployment.build name

type ResourceGroupBuilder() =
    member __.Yield _ =
        { Name = Option.None
          Parameters = Set.empty
          Outputs = Map.empty
          Resources = List.empty
          Location = Location.WestEurope
          Tags = Map.empty
          DeploymentMode = DeploymentMode.Incremental}

    /// Sets the name of the resource group
    [<CustomOperation "name">]
    member __.SetName (state, name) : ResourceGroupConfig = { state with Name = ResourceName name |> Some }

    /// Creates an output value that will be returned by the ARM template.
    [<CustomOperation "output">]
    member __.Output (state, outputName, outputValue) : ResourceGroupConfig = { state with Outputs = state.Outputs.Add(outputName, outputValue) }
    member this.Output (state:ResourceGroupConfig, outputName:string, (ResourceName outputValue)) = this.Output(state, outputName, outputValue)
    member this.Output (state:ResourceGroupConfig, outputName:string, outputValue:ArmExpression) = this.Output(state, outputName, outputValue.Eval())
    member this.Output (state:ResourceGroupConfig, outputName:string, outputValue:string option) =
        match outputValue with
        | Some outputValue -> this.Output(state, outputName, outputValue)
        | None -> state
    member this.Output (state:ResourceGroupConfig, outputName:string, outputValue:ArmExpression option) =
        match outputValue with
        | Some outputValue -> this.Output(state, outputName, outputValue)
        | None -> state

    /// Sets the default location of all resources.
    [<CustomOperation "location">]
    member __.Location (state, location) : ResourceGroupConfig = { state with Location = location }

    static member private AddResources(state:ResourceGroupConfig, resources:IArmResource list) =
        { state with
            Resources =
                state.Resources
                @ resources
                |> List.distinctBy(fun r -> r.ResourceName, r.GetType().Name) }

    /// Adds a builder's ARM resources to the ARM template.
    [<CustomOperation "add_resource">]
    member _.AddResource (state:ResourceGroupConfig, input:IBuilder) = ResourceGroupBuilder.AddResources(state, input.BuildResources state.Location)
    member _.AddResource (state:ResourceGroupConfig, input:Builder) = ResourceGroupBuilder.AddResources(state, input state.Location)
    member _.AddResource (state:ResourceGroupConfig, input:IArmResource) = ResourceGroupBuilder.AddResources(state, [ input ])

    [<CustomOperation "add_resources">]
    member this.AddResources(state:ResourceGroupConfig, input:IBuilder list) =
        let resources = input |> List.collect(fun i -> i.BuildResources state.Location)
        ResourceGroupBuilder.AddResources(state, resources)

let resourceGroup = ResourceGroupBuilder()

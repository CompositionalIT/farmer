[<AutoOpen>]
module Farmer.Builders.ResourceGroup

open Farmer
open Farmer.Arm
open Farmer.CoreTypes
open Farmer.Arm.ResourceGroup
open System

type ResourceGroupConfig =
    { Name: ResourceName
      Location: Location
      Outputs : Map<string, string>
      Resources : IArmResource list }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            { ResourceGroup.Name = this.Name
              Location = this.Location
              PostDeployTasks = List.empty }
            if not this.Resources.IsEmpty then
                { ResourceGroupDeployment.Name = this.Name
                  Location = this.Location
                  Template = 
                    { Parameters = [
                          for resource in this.Resources do
                              match resource with
                              | :? IParameters as p -> yield! p.SecureParameters
                              | _ -> ()
                      ] |> List.distinct
                      Resources = this.Resources
                      Outputs = this.Outputs |> Map.toList
                      Schema = DeploymentScope.ResourceGroup.Schema }
                  }
        ]

type ResourceGroupBuilder() =
    member _.Yield _ =
        { ResourceGroupConfig.Name = ResourceName "my-resource-group"
          Location = Location.WestEurope
          Outputs = Map.empty
          Resources = List.empty }
    
    /// Sets the name of the resource group
    [<CustomOperation "name">]
    member _.SetName(state:ResourceGroupConfig, name) =
        { state with Name = ResourceName name }
    member _.SetName(state:ResourceGroupConfig, name) =
        { state with Name = name }
    static member private AddResources(state:ResourceGroupConfig, resources:IArmResource list) =
        { state with
            Resources =
                state.Resources
                @ resources
                |> List.distinctBy(fun r -> r.ResourceName, r.GetType().Name) }
                
    /// Sets the name of the resource group
    [<CustomOperation "location">]
    member _.SetLocation (state:ResourceGroupConfig, input:Location) =
        { state with Location = input }
    /// Adds a builder's ARM resources to the Resource Group.
    [<CustomOperation "add_resource">]
    member _.AddResource (state:ResourceGroupConfig, input:IBuilder) = ResourceGroupBuilder.AddResources(state, input.BuildResources state.Location)
    member _.AddResource (state:ResourceGroupConfig, input:Builder) = ResourceGroupBuilder.AddResources(state, input state.Location)
    member _.AddResource (state:ResourceGroupConfig, input:IArmResource) = ResourceGroupBuilder.AddResources(state, [ input ])
        
    [<CustomOperation "add_resources">]
    member _.AddResources(state:ResourceGroupConfig, input:IBuilder list) =
        let resources = input |> List.collect(fun i -> i.BuildResources state.Location)
        ResourceGroupBuilder.AddResources(state, resources)

let resourceGroup = ResourceGroupBuilder()
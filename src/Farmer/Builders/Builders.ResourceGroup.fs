[<AutoOpen>]
module Farmer.Builders.ResourceGroup

open Farmer
open Farmer.Arm.ResourceGroup

type ResourceGroupConfig = 
    { Name: string Option
      Parameters : string Set
      Outputs : Map<string, string>
      Location : Location
      Resources : IArmResource list 
      Mode: DeploymentMode
      Tags: Map<string,string> }
    member this.IncludeDeployment = 
        match this.Name, this.Resources with
        | Some _, _::_ -> true
        | _ -> false
    member this.Template () = 
        let template =
            { Parameters = [
                for resource in this.Resources do
                    match resource with
                    | :? IParameters as p -> yield! p.SecureParameters
                    | _ -> ()
              ] |> List.distinct
              Outputs = this.Outputs |> Map.toList
              Resources = this.Resources }

        let postDeployTasks = [
            for resource in this.Resources do
                match resource with
                | :? IPostDeploy as pd -> pd
                | _ -> ()
            ]

        { Location = this.Location
          Template = template
          PostDeployTasks = postDeployTasks }
    interface ITaggable<ResourceGroupConfig> with
        member _.Add state tags = {state with Tags = state.Tags |> Map.merge tags}
    interface IBuilder with
        member this.ResourceId =  resourceGroupDeployment.resourceId (this.Name |> Option.defaultValue "farmer-deploy")
        member this.BuildResources loc = 
            [   if this.IncludeDeployment then
                    { ResourceGroupDeployment.Name = this.Name |> Option.defaultValue "farmer-deployment" |> ResourceName
                      Parameters = this.Parameters
                      Outputs = this.Outputs
                      Location  = this.Location
                      Resources = this.Resources
                      Mode = this.Mode
                      Tags = this.Tags }
            ] 

type ResourceGroupBuilder() =
    member __.Yield _ =
        { Name = None
          Parameters = Set.empty
          Outputs = Map.empty
          Resources = List.empty
          Location = Location.WestEurope
          Mode = Incremental
          Tags = Map.empty }

    member __.Run (state:ResourceGroupConfig) =
        state.Template ()

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
                |> List.distinctBy(fun r -> r.ResourceId, r.GetType().Name) }

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
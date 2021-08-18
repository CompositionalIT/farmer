[<AutoOpen>]
module Farmer.Builders.ResourceGroup

open Farmer
open Farmer.Arm.ResourceGroup

type ResourceGroupConfig = 
    { Name: ArmExpression
      Dependencies: ResourceId Set
      Parameters : string Set
      Outputs : Map<string, string>
      Location : Location
      Resources : IArmResource list 
      Mode: DeploymentMode
      Tags: Map<string,string> }
    member this.ResourceId = resourceGroupDeployment.resourceId (this.Name.Eval())
    member private this.ContentDeployment = 
        if this.Parameters.IsEmpty && this.Outputs.IsEmpty && this.Resources.IsEmpty then
            None // this resource group has no content so there's nothing to deploy
        else
            let innerOutputs = 
                this.Resources
                |> List.collect 
                    (function 
                    | :? ResourceGroupDeployment as rg -> 
                        Map.toList rg.Outputs 
                        |> List.map fst
                        |> List.map (fun key -> $"{ rg.Name.Eval()}.{key}",$"[reference('{rg.Name.Eval()}').outputs['{key}'].value]")
                    | _ -> 
                        [] )
                |> Map.ofList
               
            { ResourceGroupDeployment.Name = this.Name
              Dependencies = this.Dependencies
              Outputs = Map.merge (Map.toList this.Outputs) innerOutputs // New values overwrite old values so supply this.Outputs as newValues
              Location  = this.Location
              Resources = this.Resources
              Mode = this.Mode
              Tags = this.Tags } 
            |> Some
    member this.Template = 
        this.ContentDeployment
        |> Option.map (fun x -> x.Template)
        |> Option.defaultValue 
            { Parameters = List.empty
              Outputs = List.empty
              Resources = List.empty }

    interface IDeploymentSource with 
        member this.Deployment= 
            { Location=this.Location
              Template = this.Template
              PostDeployTasks = 
                    this.Resources 
                    |> List.choose (function | :? IPostDeploy as pd -> Some pd |_ -> None)
              RequiredResourceGroups =
                    this.Resources
                    |> List.collect (function | :? ResourceGroupDeployment as rg -> rg.RequiredResourceGroups | _ -> [])
              Tags = this.Tags }
    interface IBuilder with
        member this.ResourceId = this.ResourceId
        member this.BuildResources loc = 
            [ match this.ContentDeployment with 
              | Some x -> x
              | None -> () 
            ]

type ResourceGroupBuilder() =
    member __.Yield _ =
        { Name = ArmExpression.Empty
          Dependencies = Set.empty
          Parameters = Set.empty
          Outputs = Map.empty
          Resources = List.empty
          Location = Location.WestEurope
          Mode = Incremental
          Tags = Map.empty }
          
    /// Creates an output value that will be returned by the ARM template.
    [<CustomOperation "name">]
    member _.SetName(state:ResourceGroupConfig, name:string ) = {state with Name = ArmExpression.literal name}
    member _.SetName(state:ResourceGroupConfig, name:ArmExpression ) = {state with Name = name}
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

    [<CustomOperation "depends_on">]
    member this.AddDepenencies(state:ResourceGroupConfig, dependencies: ResourceId list) =
        {state with Dependencies = Set.union state.Dependencies (Set.ofList dependencies) }
    
    interface ITaggable<ResourceGroupConfig> with member _.Add state tags = {state with Tags = state.Tags |> Map.merge tags}

let resourceGroup = ResourceGroupBuilder()
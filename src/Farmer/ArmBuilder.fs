[<AutoOpen>]
module Farmer.ArmBuilder

open Farmer.CoreTypes
open Farmer

module Resource =
    /// Creates a unique IArmResource from an arbitrary object.
    let ofObj armObject =
        { new IArmResource with
             member _.ResourceName = ResourceName (System.Guid.NewGuid().ToString())
             member _.JsonModel = armObject }

    /// Creates a unique IArmResource from a JSON string containing the output you want.
    let ofJson json = json |> Newtonsoft.Json.Linq.JObject.Parse |> ofObj

module Json =
    /// Creates a unique IArmResource from a JSON string containing the output you want.
    let toIArmResource = Resource.ofJson

module Subscription =
    /// Gets an ARM expression pointing to the tenant id of the current subscription.
    let TenantId = ArmExpression.create "subscription().tenantid"

/// Represents all configuration information to generate an ARM template.
type ArmConfig =
    { Parameters : string Set
      Outputs : Map<string, string>
      Location : Location
      Resources : IArmResource list }

type ArmBuilder() =
    member __.Yield _ =
        { Parameters = Set.empty
          Outputs = Map.empty
          Resources = List.empty
          Location = Location.WestEurope }

    member __.Run (state:ArmConfig) =
        let template =
            { Parameters = [
                for resource in state.Resources do
                    match resource with
                    | :? IParameters as p -> yield! p.SecureParameters
                    | _ -> ()
              ] |> List.distinct
              Outputs = state.Outputs |> Map.toList
              Resources = state.Resources }

        let postDeployTasks = [
            for resource in state.Resources do
                match resource with
                | :? IPostDeploy as pd -> pd
                | _ -> ()
            ]

        { Location = state.Location
          Template = template
          PostDeployTasks = postDeployTasks }

    /// Creates an output value that will be returned by the ARM template.
    [<CustomOperation "output">]
    member __.Output (state, outputName, outputValue) : ArmConfig = { state with Outputs = state.Outputs.Add(outputName, outputValue) }
    member this.Output (state:ArmConfig, outputName:string, (ResourceName outputValue)) = this.Output(state, outputName, outputValue)
    member this.Output (state:ArmConfig, outputName:string, outputValue:ArmExpression) = this.Output(state, outputName, outputValue.Eval())
    member this.Output (state:ArmConfig, outputName:string, outputValue:string option) =
        match outputValue with
        | Some outputValue -> this.Output(state, outputName, outputValue)
        | None -> state
    member this.Output (state:ArmConfig, outputName:string, outputValue:ArmExpression option) =
        match outputValue with
        | Some outputValue -> this.Output(state, outputName, outputValue)
        | None -> state

    /// Sets the default location of all resources.
    [<CustomOperation "location">]
    member __.Location (state, location) : ArmConfig = { state with Location = location }

    static member private AddResources(state:ArmConfig, resources:IArmResource list) =
        { state with
            Resources =
                state.Resources
                @ resources
                |> List.distinctBy(fun r -> r.ResourceName, r.GetType().Name) }

    /// Adds a builder's ARM resources to the ARM template.
    [<CustomOperation "add_resource">]
    member _.AddResource (state:ArmConfig, input:IBuilder) = ArmBuilder.AddResources(state, input.BuildResources state.Location)
    member _.AddResource (state:ArmConfig, input:Builder) = ArmBuilder.AddResources(state, input state.Location)
    member _.AddResource (state:ArmConfig, input:IArmResource) = ArmBuilder.AddResources(state, [ input ])

    [<CustomOperation "add_resources">]
    member this.AddResources(state:ArmConfig, input:IBuilder list) =
        let resources = input |> List.collect(fun i -> i.BuildResources state.Location)
        ArmBuilder.AddResources(state, resources)

let arm = ArmBuilder()
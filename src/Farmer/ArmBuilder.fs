[<AutoOpen>]
module Farmer.ArmBuilder

module Helpers =
    /// A low-level builder that takes in a location and generates raw ARM resources (and their
    /// resource name) in a form ready for JSON serialization.
    type ArmResourcesBuilder = Location -> (string * obj) list

    /// Adapts a raw ArmResourceBuilder into a "full" Builder that can be added as a resource to arm { } expressions.
    let asResourceBuilder (builder:ArmResourcesBuilder) =
        let output : Builder =
            fun (location:Location) _ -> [
                for resourceName, armObject in builder location do
                    { new IArmResource with
                         member _.ResourceName = ResourceName resourceName
                         member _.ToArmObject() = armObject }
            ]
        output

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
          Location = WestEurope }

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

    /// Adds a single resource to the ARM template.
    [<CustomOperation "add_resource">]
    member this.AddResource (state:ArmConfig, builder:IBuilder) =
        this.AddResource(state, builder.BuildResources)

    member __.AddResource (state:ArmConfig, builder:Builder) =
        let updatedResources =
            builder state.Location state.Resources
            |> List.fold(fun resources newResource ->
                let resourceType = newResource.GetType()
                let existing =
                    resources
                    |> List.filter(fun (r:IArmResource) ->
                        r.ResourceName = newResource.ResourceName &&
                        r.GetType() = resourceType)

                match existing with
                | _ :: _ -> printfn "'%s/%s' has been replaced or updated." resourceType.Name newResource.ResourceName.Value
                | [] -> ()
                newResource :: (resources |> List.except existing)) state.Resources
            |> List.rev

        { state with Resources = updatedResources }

    [<CustomOperation "add_resources">]
    /// Adds a sequence of resources to the ARM template.
    member this.AddResources (state:ArmConfig, builders:IBuilder list) =
        builders
        |> Seq.fold(fun state builder -> this.AddResource(state, builder)) state

let arm = ArmBuilder()
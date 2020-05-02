[<AutoOpen>]
module Farmer.ArmBuilder

/// Represents all configuration information to generate an ARM template.
type ArmConfig =
    { Parameters : string Set
      Outputs : Map<string, string>
      Location : Location
      Resources : IResource list }

type ArmBuilder() =
    member __.Yield _ =
        { Parameters = Set.empty
          Outputs = Map.empty
          Resources = List.empty
          Location = WestEurope }

    member __.Run (state:ArmConfig) =
        let resources =
            state.Resources
            |> List.groupBy(fun r -> r.GetType(), r.ResourceName)
            |> List.choose(fun ((resourceType, resourceName), instances) ->
                match instances with
                | [] ->
                   None
                | [ resource ] ->
                   Some resource
                | resource :: _ ->
                   printfn "Warning: %d %s resources were found with the same name of '%s'. The first one will be used." instances.Length resourceType.Name resourceName.Value
                   Some resource)
        let template =
            { Parameters = [
                for resource in resources do
                    match resource with
                    | :? IParameters as p -> yield! p.SecureParameters
                    | _ -> ()
              ] |> List.distinct
              Outputs = state.Outputs |> Map.toList
              Resources = resources }

        let postDeployTasks = [
            for resource in resources do
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
    member __.AddResource (state:ArmConfig, builder:ResourceBuilder) =
        let resources =
            builder state.Location state.Resources
            |> List.fold(fun resources action ->
                match action with
                | NewResource newResource -> resources @ [ newResource ]
                | MergedResource(oldVersion, newVersion) -> (resources |> List.filter ((<>) oldVersion)) @ [ newVersion ]
                | CouldNotLocate (ResourceName resourceName) -> failwithf "Could not locate the parent resource ('%s'). Make sure you have correctly specified the name, and that it was added to the arm { } builder before this one." resourceName
                | NotSet -> failwith "No parent resource name was set for this resource to link to.") state.Resources

        { state with Resources = resources }
    member this.AddResource (state:ArmConfig, builder:IResourceBuilder) =
        this.AddResource(state, builder.BuildResources)

    [<CustomOperation "add_resources">]
    /// Adds a sequence of resources to the ARM template.
    member this.AddResources (state:ArmConfig, resources:IResourceBuilder list) =
        resources
        |> Seq.fold(fun state resource -> this.AddResource(state, resource.BuildResources)) state

let arm = ArmBuilder()
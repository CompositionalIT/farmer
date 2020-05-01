[<AutoOpen>]
module Farmer.ArmBuilder

open System.IO
open System.IO.Compression

/// Represents all configuration information to generate an ARM template.
type ArmConfig =
    { Parameters : string Set
      Outputs : (string * string) list
      Location : Location
      Resources : IResource list }

type Deployment =
    { Location : Location
      Template : ArmTemplate
      PostDeployTasks : IPostDeploy list }

type ArmBuilder() =
    member __.Yield _ =
        { Parameters = Set.empty
          Outputs = List.empty
          Resources = List.empty
          Location = WestEurope }

    member __.Run (state:ArmConfig) =
        let resources =
            state.Resources
            |> List.groupBy(fun r -> r.ResourceName)
            |> List.choose(fun (resourceName, instances) ->
                match instances with
                | [] ->
                   None
                | [ resource ] ->
                   Some resource
                | resource :: _ ->
                   printfn "Warning: %d resources were found with the same name of '%s'. The first one will be used." instances.Length resourceName.Value
                   Some resource)
        let template =
            { Parameters = [
                for resource in resources do
                    match resource with
                    | :? IParameters as p -> yield! p.SecureParameters
                    | _ -> ()
              ] |> List.distinct
              Outputs = state.Outputs
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
    member __.Output (state, outputName, outputValue) : ArmConfig = { state with Outputs = (outputName, outputValue) :: state.Outputs }
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
    [<CustomOperation "add_resource">]

    (* These two "fake" methods are needed to ensure that extension members for each builder
       is always available. *)

    /// Adds a single resource to the ARM template.
    member __.AddResource (state:ArmConfig, builder:IResourceBuilder) =
        let resources =
            builder.BuildResources state.Location state.Resources
            |> List.fold(fun resources action ->
                match action with
                | NewResource newResource -> resources @ [ newResource ]
                | MergedResource(oldVersion, newVersion) -> (resources |> List.filter ((<>) oldVersion)) @ [ newVersion ]
                | CouldNotLocate (ResourceName resourceName) -> failwithf "Could not locate the parent resource ('%s'). Make sure you have correctly specified the name, and that it was added to the arm { } builder before this one." resourceName
                | NotSet -> failwith "No parent resource name was set for this resource to link to.") state.Resources

        { state with Resources = resources }
    [<CustomOperation "add_resources">]
    /// Adds a sequence of resources to the ARM template.
    member __.AddResources (state:ArmConfig, ()) = state

let internal addResources<'a> (addOne:ArmConfig * 'a -> ArmConfig) (state:ArmConfig) resources =
    (state, resources)
    ||> Seq.fold(fun state resource -> addOne (state, resource))

let arm = ArmBuilder()
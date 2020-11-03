namespace Farmer

[<AutoOpen>]
module ArmBuilder =

    open Farmer.CoreTypes
    open Farmer

    [<RequireQualifiedAccess>]
    module Builder =
        let lift (armResource: IArmResource) : Builder = fun _ -> [ armResource ]

    [<RequireQualifiedAccess>]
    module List =
        let collectF (x: 'T) (xs: ('T -> 'U list) list) =
            xs |> List.collect (fun f -> f x)

    /// Represents all configuration information to generate an ARM template.
    type ArmConfig =
        { Parameters : string Set
          Outputs : Map<string, string>
          Location : Location
          Resources : Builder list }

    [<RequireQualifiedAccess>]
    module ArmConfig =
        let empty = 
            { Parameters = Set.empty
              Outputs = Map.empty
              Resources = List.empty
              Location = Location.WestEurope }

        let create location =
            { Parameters = Set.empty
              Outputs = Map.empty
              Resources = List.empty
              Location = location }

        let addResources (state: ArmConfig) (resources: Builder list) =
            { state with Resources = state.Resources @ resources }

        let addResource (state: ArmConfig) (resource: Builder) =
            { state with Resources = resource::state.Resources }
        
            // |> List.distinctBy(fun r -> r.ResourceName, r.GetType().Name)

    type ArmBuilder() =
        member _.Yield (x: unit) = ArmConfig.empty
        member this.Yield (appInsight: #IBuilder) =
            this.Yield(appInsight)

        member _.Yield (location: Location) = 
            ArmConfig.create location
        member _.Yield (input: IBuilder) =
            ArmConfig.addResource ArmConfig.empty input.BuildResources
        member _.Yield (input: Builder) =
            ArmConfig.addResource ArmConfig.empty input
        member _.Yield (input: IArmResource) =
            Builder.lift input
            |> ArmConfig.addResource ArmConfig.empty

        member _.Zero () = ArmConfig.empty
        member _.Delay f = f()

        member _.Combine (state1: ArmConfig, state2: ArmConfig) =
            { Parameters = Set.union state1.Parameters state2.Parameters
              Outputs = 
                  state2.Outputs
                  |> List.ofSeq
                  |> List.fold (fun m kvPair -> 
                      Map.add kvPair.Key kvPair.Value m
                  ) state1.Outputs 
              Resources = state1.Resources @ state2.Resources 
              Location = state1.Location }

        member this.For (state: ArmConfig, f: unit -> ArmConfig) = this.Combine(state, f())
        
        member _.Run (state:ArmConfig) =
            let resources = 
                state.Resources
                |> List.collectF state.Location
                |> List.distinctBy(fun r -> r.ResourceName, r.GetType().Name)

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
        member _.Output (state, outputName, outputValue) : ArmConfig = { state with Outputs = state.Outputs.Add(outputName, outputValue) }
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
        member _.Location (state:ArmConfig, location: Location) = { state with Location = location }

        /// Adds a builder's ARM resources to the ARM template.
        [<CustomOperation "add_resource">]
        member _.AddResource (state:ArmConfig, input:IBuilder) = ArmConfig.addResource state input.BuildResources
        member _.AddResource (state:ArmConfig, input:Builder) = ArmConfig.addResource state input
        member _.AddResource (state:ArmConfig, input:IArmResource) = Builder.lift input |> ArmConfig.addResource state

        [<CustomOperation "add_resources">]
        member _.AddResources(state:ArmConfig, input:IBuilder list) =
            //let resources = input |> List.collect(fun i -> i.BuildResources state.Location)
            input 
            |> List.map (fun b -> b.BuildResources)
            |> ArmConfig.addResources state

    let arm = ArmBuilder()

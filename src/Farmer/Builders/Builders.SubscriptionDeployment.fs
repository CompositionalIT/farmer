[<AutoOpen>]
module Farmer.Builders.SubscriptionDeployment

open Farmer
open Farmer.CoreTypes

type SubscriptionDeployment=
    { Parameters : string Set
      Outputs : Map<string, string>
      Location : Location 
      Resources: ISubscriptionResourceBuilder list
      Tags: Map<string,string> }
    interface IDeploymentBuilder with
        member this.BuildDeployment () =
            let template =
                { Parameters = [
                    for resource in this.Resources do
                        match resource with
                        | :? IParameters as p -> yield! p.SecureParameters
                        | _ -> ()
                  ] |> List.distinct
                  Outputs = this.Outputs |> Map.toList
                  Resources = this.Resources|> List.collect (fun rg -> rg.BuildResources ()) }

            let postDeployTasks = [
                for resource in this.Resources do
                    match resource with
                    | :? IPostDeploy as pd -> pd
                    | _ -> ()
                ]

            { Schema = "https://schema.management.azure.com/schemas/2018-05-01/subscriptionDeploymentTemplate.json#"
              Location = this.Location
              Template = template
              PostDeployTasks = postDeployTasks }

type SubscriptionDeploymentBuilder()=
    member __.Yield _ =
        { Parameters = Set.empty
          Outputs = Map.empty
          Resources = List.empty
          Location = Location.WestEurope
          Tags = Map.empty }

    /// Creates an output value that will be returned by the ARM template.
    [<CustomOperation "output">]
    member __.Output (state, outputName, outputValue) : SubscriptionDeployment = { state with Outputs = state.Outputs.Add(outputName, outputValue) }
    member this.Output (state:SubscriptionDeployment, outputName:string, (ResourceName outputValue)) = this.Output(state, outputName, outputValue)
    member this.Output (state:SubscriptionDeployment, outputName:string, outputValue:ArmExpression) = this.Output(state, outputName, outputValue.Eval())
    member this.Output (state:SubscriptionDeployment, outputName:string, outputValue:string option) =
        match outputValue with
        | Some outputValue -> this.Output(state, outputName, outputValue)
        | None -> state
    member this.Output (state:SubscriptionDeployment, outputName:string, outputValue:ArmExpression option) =
        match outputValue with
        | Some outputValue -> this.Output(state, outputName, outputValue)
        | None -> state

    /// Sets the location for deployment metadata
    [<CustomOperation "location">]
    member this.Location (state, location) : SubscriptionDeployment = { state with Location = location }
    
    /// Adds the given resource group to the deployment
    [<CustomOperation "add_resource_groups">]
    member this.AddResourceGroups(state: SubscriptionDeployment, resGroups) =
        { state with
            Resources = state.Resources @ resGroups }

    /// Adds the given resource group to the deployment
    [<CustomOperation "add_resource_group">]
    member this.AddResourceGroup(state: SubscriptionDeployment, resGroup) =
        this.AddResourceGroups(state, [resGroup])

let subscriptionDeployment = SubscriptionDeploymentBuilder()
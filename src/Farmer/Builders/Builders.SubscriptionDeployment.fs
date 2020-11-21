[<AutoOpen>]
module Farmer.Builders.SubscriptionDeployment

open Farmer
open Farmer.CoreTypes

let private linkedTemplateOutput deplName outputName =
    sprintf "[reference('%s').outputs.%s.value]" deplName outputName

type SubscriptionDeployment=
    { Parameters : string Set
      Location : Location 
      Resources: ISubscriptionResourceBuilder list
      Tags: Map<string,string> }
    interface IDeploymentBuilder with
        member this.BuildDeployment _ =
            let deploymentNameSuffix = System.DateTime.UtcNow.ToString("yyyyMMddTHHmm") 
            let resources = 
                this.Resources 
                |> List.map (fun rg -> {| rg.BuildResources deploymentNameSuffix with Outputs = rg.Outputs |} )

            let outputs = 
                resources
                |> List.collect 
                    (fun x -> 
                        x.Outputs 
                        |> Map.toList
                        |> List.map (fun (k, _) -> k, linkedTemplateOutput x.DeploymentName k))
                |> List.fold (fun acc (k,v) -> Map.add k v acc) Map.empty

            let template =
                { Parameters = [
                    for resource in this.Resources do
                        match resource with
                        | :? IParameters as p -> yield! p.SecureParameters
                        | _ -> ()
                  ] |> List.distinct
                  Outputs = outputs |> Map.toList
                  Resources = resources |> List.collect (fun x -> x.Resources) }
            let postDeploy = 
                this.Resources 
                |> List.map (fun x-> x.RunPostDeployTasks) 

            { Schema = "https://schema.management.azure.com/schemas/2018-05-01/subscriptionDeploymentTemplate.json#"
              Location = this.Location
              Template = template
              PostDeployTasks = postDeploy }

type SubscriptionDeploymentBuilder()=
    member __.Yield _ =
        { Parameters = Set.empty
          Resources = List.empty
          Location = Location.WestEurope
          Tags = Map.empty }

    /// Sets the location for deployment metadata
    [<CustomOperation "location">]
    member this.Location (state, location) : SubscriptionDeployment = { state with Location = location }
    
    /// Adds the given resource group to the deployment
    [<CustomOperation "add_resources">]
    member this.AddResources(state: SubscriptionDeployment, resGroups) =
        { state with
            Resources = state.Resources @ resGroups }

    /// Adds the given resource group to the deployment
    [<CustomOperation "add_resource">]
    member this.AddResource(state: SubscriptionDeployment, resGroup) =
        this.AddResources(state, [resGroup])

let subscriptionDeployment = SubscriptionDeploymentBuilder()
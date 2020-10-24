[<AutoOpen>]
module Farmer.Builders.CognitiveServices

open Farmer
open Farmer.Arm.CognitiveServices
open Farmer.CognitiveServices
open Farmer.CoreTypes

type CognitiveServices =
    /// Gets an ARM Expression key for any Cognitives Services instance.
    static member getKey (resourceId:ResourceId) =
        let resourceId = resourceId.WithType (accounts)
        ArmExpression.create(sprintf "listKeys(%s, '%s').key1" resourceId.ArmExpression.Value accounts.ApiVersion, resourceId)
    static member getKey (name:ResourceName) = CognitiveServices.getKey (ResourceId.create name)

type CognitiveServicesConfig =
    { Name : ResourceName
      Sku : Sku
      Api : Kind
      Tags: Map<string,string>  }
    /// Gets an ARM expression to the key of this Cognitive Services instance.
    member this.Key = CognitiveServices.getKey (ResourceId.create this.Name)
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              Sku = this.Sku
              Kind = this.Api
              Tags = this.Tags }
        ]

type CognitiveServicesBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Sku = F0
          Api = AllInOne
          Tags = Map.empty }
    [<CustomOperation "name">]
    member _.Name (state:CognitiveServicesConfig, name) = { state with Name = ResourceName name }
    [<CustomOperation "sku">]
    member _.Sku (state:CognitiveServicesConfig, sku) = { state with Sku = sku }
    [<CustomOperation "api">]
    member _.Api (state:CognitiveServicesConfig, api) = { state with Api = api }
    [<CustomOperation "add_tags">]
    member _.Tags(state:CognitiveServicesConfig, pairs) =
        { state with
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
    member this.Tag(state:CognitiveServicesConfig, key, value) = this.Tags(state, [ (key,value) ])

let cognitiveServices = CognitiveServicesBuilder()
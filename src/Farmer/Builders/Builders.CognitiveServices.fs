[<AutoOpen>]
module Farmer.Builders.CognitiveServices

open Farmer
open Farmer.Arm.CognitiveServices
open Farmer.CognitiveServices

type CognitiveServicesConfig =
    { Name : ResourceName
      Sku : Sku
      Api : Kind }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location _ = [
            { Name = this.Name
              Location = location
              Sku = this.Sku
              Kind = this.Api }
        ]

type CognitiveServicesBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Sku = F0
          Api = AllInOne }
    [<CustomOperation "name">]
    member _.Name (state:CognitiveServicesConfig, name) = { state with Name = ResourceName name }
    [<CustomOperation "sku">]
    member _.Sku (state:CognitiveServicesConfig, sku) = { state with Sku = sku }
    [<CustomOperation "api">]
    member _.Api (state:CognitiveServicesConfig, api) = { state with Api = api }

let cognitiveServices = CognitiveServicesBuilder()
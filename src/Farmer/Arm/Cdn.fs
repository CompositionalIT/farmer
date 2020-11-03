[<AutoOpen>]
module Farmer.Arm.Cdn

open Farmer
open Farmer.CoreTypes
open Farmer.Cdn
open System

let profiles = ResourceType ("Microsoft.Cdn/profiles", "2019-04-15")
let endpoints = ResourceType ("Microsoft.Cdn/profiles/endpoints", "2019-04-15")
let customDomains = ResourceType ("Microsoft.Cdn/profiles/endpoints/customDomains", "2019-04-15")

type Profile =
    { Name : ResourceName
      Sku : Sku
      Tags: Map<string,string> }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| profiles.Create (this.Name, Location.Global, tags = this.Tags) with
                   sku = {| name = string this.Sku |}
                   properties = {||}
            |} |> box

module Profiles =
    type Endpoint =
        { Name : ResourceName
          Profile : ResourceName
          Dependencies : ResourceId list
          CompressedContentTypes : string Set
          QueryStringCachingBehaviour : QueryStringCachingBehaviour
          Http : FeatureFlag
          Https : FeatureFlag
          Compression : FeatureFlag
          Origin : ArmExpression
          OptimizationType : OptimizationType
          Tags: Map<string,string> }
        interface IArmResource with
            member this.ResourceName = this.Profile/this.Name
            member this.JsonModel =
                let dependencies = [
                    ResourceId.create this.Profile
                    yield! this.Origin.Owner |> Option.toList
                    yield! this.Dependencies
                ]
                {| endpoints.Create(this.Profile/this.Name, Location.Global, dependencies, this.Tags) with
                       properties =
                            {| originHostHeader = this.Origin.Eval()
                               queryStringCachingBehavior = string this.QueryStringCachingBehaviour
                               optimizationType = string this.OptimizationType
                               isHttpAllowed = this.Http.AsBoolean
                               isHttpsAllowed = this.Https.AsBoolean
                               isCompressionEnabled = this.Compression.AsBoolean
                               contentTypesToCompress = this.CompressedContentTypes
                               origins = [
                                   {| name = "origin"
                                      properties = {| hostName = this.Origin.Eval() |}
                                   |}
                               ]
                            |}
                |} :> _

    module Endpoints =
        type CustomDomain =
            { Name : ResourceName
              Profile : ResourceName
              Endpoint : ResourceName
              Hostname : Uri }
            interface IArmResource with
                member this.ResourceName = this.Profile/this.Endpoint/this.Name
                member this.JsonModel =
                    {| customDomains.Create (this.Profile/this.Endpoint/this.Name, dependsOn = [ ResourceId.create this.Endpoint ]) with
                        properties = {| hostName = string this.Hostname |}
                    |} :> _

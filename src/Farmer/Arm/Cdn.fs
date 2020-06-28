[<AutoOpen>]
module Farmer.Arm.Cdn

open Farmer
open Farmer.CoreTypes
open Farmer.Cdn
open System

let profiles = ResourceType "Microsoft.Cdn/profiles"
let endpoints = ResourceType "Microsoft.Cdn/profiles/endpoints"
let customDomains = ResourceType "Microsoft.Cdn/profiles/endpoints/customDomains"

type Profile =
    { Name : ResourceName
      Sku : Sku }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = profiles.ArmValue
               name = this.Name.Value
               apiVersion = "2019-04-15"
               location = "global"
               sku = {| name = string this.Sku |}
               properties = {||}
            |} |> box

module Profiles =
    type Endpoint =
        { Profile : ResourceName
          Name : ResourceName
          DependsOn : ResourceName list
          CompressedContentTypes : string Set
          QueryStringCachingBehaviour : QueryStringCachingBehaviour
          Http : FeatureFlag
          Https : FeatureFlag
          Compression : FeatureFlag
          Origin : string
          OptimizationType : OptimizationType }
        interface IArmResource with
            member this.ResourceName: ResourceName = this.Name
            member this.JsonModel =
                {| ``type`` = endpoints.ArmValue
                   name = this.Profile.Value + "/" + this.Name.Value
                   apiVersion = "2019-04-15"
                   location = "global"
                   dependsOn = [
                       this.Profile.Value
                       for dependency in this.DependsOn do
                        dependency.Value
                   ]
                   properties =
                        {| originHostHeader = this.Origin
                           queryStringCachingBehavior = string this.QueryStringCachingBehaviour
                           optimizationType = string this.OptimizationType
                           isHttpAllowed = this.Http.AsBoolean
                           isHttpsAllowed = this.Https.AsBoolean
                           isCompressionEnabled = this.Compression.AsBoolean
                           contentTypesToCompress = this.CompressedContentTypes
                           origins = [
                               {| name = "origin"
                                  properties = {| hostName = this.Origin |}
                               |}
                           ]
                        |}
                |} :> _

    module Endpoints =
        type CustomDomain =
            { Name : ResourceName
              Endpoint : ResourceName
              Hostname : Uri }
            interface IArmResource with
                member this.ResourceName = this.Name
                member this.JsonModel =
                    {| Name = this.Endpoint.Value + "/" + this.Name.Value
                       ``type`` = customDomains.ArmValue
                       apiVersion = "2019-04-15"
                       dependsOn = [ this.Endpoint.Value ]
                       properties = {| hostName = string this.Hostname |}
                    |} :> _

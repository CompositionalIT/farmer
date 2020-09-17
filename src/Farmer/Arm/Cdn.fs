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
            {| ``type`` = profiles.Path
               apiVersion = profiles.Version
               name = this.Name.Value
               location = "global"
               sku = {| name = string this.Sku |}
               properties = {||}
               tags = this.Tags
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
          OptimizationType : OptimizationType
          Tags: Map<string,string>  }
        interface IArmResource with
            member this.ResourceName: ResourceName = this.Name
            member this.JsonModel =
                {| ``type`` = endpoints.Path
                   apiVersion = endpoints.Version
                   name = this.Profile.Value + "/" + this.Name.Value
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
                   tags = this.Tags
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
                       ``type`` = customDomains.Path
                       apiVersion = customDomains.Version
                       dependsOn = [ this.Endpoint.Value ]
                       properties = {| hostName = string this.Hostname |}
                    |} :> _

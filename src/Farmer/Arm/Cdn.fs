[<AutoOpen>]
module Farmer.Arm.Cdn

open Farmer
open Farmer.CoreTypes
open Farmer.Cdn
open System

type Endpoint =
    { Name : ResourceName
      DependsOn : ResourceName list
      CompressedContentTypes : string Set
      QueryStringCachingBehaviour : QueryStringCachingBehaviour option
      Http : FeatureFlag
      Https : FeatureFlag
      Compression : FeatureFlag
      HostName : string
      CustomDomain : Uri option }

type Profile =
    { Name : ResourceName
      Location : Location
      Sku : Sku
      Endpoints : Endpoint list }

    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = "Microsoft.Cdn/profiles"
               name = this.Name.Value
               apiVersion = "2019-04-15"
               location = this.Location.ArmValue
               sku = {| name = string this.Sku |}
               properties = {||}
               resources = [
                   for endpoint in this.Endpoints do
                    {| ``type`` = "endpoints"
                       name = endpoint.Name.Value
                       apiVersion = "2019-04-15"
                       location = this.Location.ArmValue
                       dependsOn = [
                           this.Name.Value
                           for dependency in endpoint.DependsOn do
                            dependency.Value
                       ]
                       properties =
                            {| originHostHeader = endpoint.HostName
                               queryStringCachingBehavior =
                                match endpoint.QueryStringCachingBehaviour with
                                | Some behaviour -> string behaviour
                                | None -> "NotSet"
                               isHttpAllowed = endpoint.Http.AsBoolean
                               isHttpsAllowed = endpoint.Https.AsBoolean
                               isCompressionEnabled = endpoint.Compression.AsBoolean
                               contentTypesToCompress = endpoint.CompressedContentTypes
                               origins = [
                                   {| name = "origin1"
                                      properties = {| hostName = endpoint.HostName |}
                                   |}
                               ]
                            |}
                       resources = [
                           match endpoint.CustomDomain with
                           | Some customDomain ->
                              {| Name = endpoint.Name.Value + "domain"
                                 ``type`` = "customDomains"
                                 apiVersion = "2019-04-15"
                                 dependsOn = [ endpoint.Name.Value ]
                                 properties = {| hostName = string customDomain |}
                              |}
                           | None ->
                              ()
                       ]
                    |}
               ]
            |} |> box
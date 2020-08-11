[<AutoOpen>]
module Farmer.Builders.Cdn

open Farmer
open Farmer.Arm.Cdn
open Profiles
open Endpoints
open Farmer.CoreTypes
open Farmer.Cdn
open System

type EndpointConfig =
    { Name : ResourceName
      DependsOn : ResourceName list
      CompressedContentTypes : string Set
      QueryStringCachingBehaviour : QueryStringCachingBehaviour
      Http : FeatureFlag
      Https : FeatureFlag
      Compression : FeatureFlag
      Origin : string
      CustomDomain : Uri option
      OptimizationType : OptimizationType }

type CdnConfig =
    { Name : ResourceName
      Sku : Sku
      Endpoints : EndpointConfig list
      Tags: Map<string,string>  }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources _ = [
            { Name = this.Name
              Sku = this.Sku
              Tags = this.Tags}
            for endpoint in this.Endpoints do
                { Profile = this.Name
                  Name = endpoint.Name
                  DependsOn = endpoint.DependsOn
                  CompressedContentTypes = endpoint.CompressedContentTypes
                  QueryStringCachingBehaviour = endpoint.QueryStringCachingBehaviour
                  Http = endpoint.Http
                  Https = endpoint.Https
                  Compression = endpoint.Compression
                  Origin = endpoint.Origin
                  OptimizationType = endpoint.OptimizationType
                  Tags = this.Tags }
                match endpoint.CustomDomain with
                | Some customDomain ->
                    { Name = endpoint.Name.Map(sprintf "%sdomain")
                      Endpoint = endpoint.Name
                      Hostname = customDomain }
                | None ->
                    ()
        ]

type CdnBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Sku = Standard_Akamai
          Endpoints = []
          Tags = Map.empty}
    [<CustomOperation "name">]
    member _.Name(state:CdnConfig, name) = { state with Name = ResourceName name }
    [<CustomOperation "sku">]
    member _.Sku(state:CdnConfig, sku) = { state with Sku = sku }
    [<CustomOperation "add_endpoints">]
    member _.AddEndpoints(state:CdnConfig, endpoints) = { state with Endpoints = state.Endpoints @ endpoints }
    [<CustomOperation "add_tags">]
    member _.Tags(state:CdnConfig, pairs) = 
        { state with 
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
    member this.Tag(state:CdnConfig, key, value) = this.Tags(state, [ (key,value) ])

type EndpointBuilder() =
    member _.Yield _ : EndpointConfig =
        { Name = ResourceName.Empty
          DependsOn = []
          CompressedContentTypes = Set.empty
          QueryStringCachingBehaviour = UseQueryString
          Http = Enabled
          Https = Enabled
          Compression = Disabled
          Origin = ""
          CustomDomain = None
          OptimizationType = GeneralWebDelivery }

    /// Name of the endpoint within the CDN.
    [<CustomOperation "name">]
    member _.Name(state:EndpointConfig, name) = { state with Name = name }
    member this.Name(state:EndpointConfig, name) = this.Name(state, ResourceName name)
    /// The address of the origin.
    [<CustomOperation "origin">]
    member _.Origin(state:EndpointConfig, name) =
      { state with
          Name = state.Name.IfEmpty ((name |> Seq.filter Char.IsLetterOrDigit |> Seq.toArray |> String) + "-endpoint")
          Origin = name }

    [<CustomOperation "depends_on">]
    member _.DependsOn(state:EndpointConfig, resourceName) = { state with DependsOn = resourceName :: state.DependsOn }
    member _.DependsOn(state:EndpointConfig, resource:IBuilder) = { state with DependsOn = resource.DependencyName :: state.DependsOn }
    member _.DependsOn(state:EndpointConfig, resource:IArmResource) = { state with DependsOn = resource.ResourceName :: state.DependsOn }

    /// Adds a list of MIME content types on which compression applies.
    [<CustomOperation "add_compressed_content">]
    member _.AddCompressedContentTypes(state:EndpointConfig, types) = { state with CompressedContentTypes = state.CompressedContentTypes + Set types; Compression = Enabled }
    /// Defines how CDN caches requests that include query strings.
    [<CustomOperation "query_string_caching_behaviour">]
    member _.QueryStringCachingBehaviour(state:EndpointConfig, behaviour) = { state with QueryStringCachingBehaviour = behaviour }

    [<CustomOperation "enable_http">]
    member _.EnableHttp(state:EndpointConfig) = { state with Http = Enabled }
    [<CustomOperation "disable_http">]
    member _.DisableHttp(state:EndpointConfig) = { state with Http = Disabled }
    [<CustomOperation "enable_https">]
    member _.EnableHttps(state:EndpointConfig) = { state with Https = Enabled }
    [<CustomOperation "disable_https">]
    member _.DisableHttps(state:EndpointConfig) = { state with Https = Disabled }
    /// Name of the custom domain hostname.
    [<CustomOperation "custom_domain_name">]
    member _.CustomDomain(state:EndpointConfig, hostname) = { state with CustomDomain = Some (System.Uri hostname) }
    /// Specifies what scenario the customer wants this CDN endpoint to optimise for.
    [<CustomOperation "optimise_for">]
    member _.OptimiseFor(state:EndpointConfig, optimizationType) = { state with OptimizationType = optimizationType }

let cdn = CdnBuilder()
let endpoint = EndpointBuilder()
[<AutoOpen>]
module Farmer.Builders.Cdn

open Farmer
open Farmer.Arm.Cdn
open Farmer.CoreTypes
open Farmer.Cdn
open System

type CdnConfig =
    { Name : ResourceName
      Sku : Sku
      Endpoints : Endpoint list }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location _ = [
            { Name = this.Name
              Location = location
              Sku = this.Sku
              Endpoints = this.Endpoints }
        ]

type CdnBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Sku = Sku.Standard_Akamai
          Endpoints = [] }
    [<CustomOperation "name">]
    member _.Name(state:CdnConfig, name) = { state with Name = ResourceName name }
    [<CustomOperation "sku">]
    member _.Sku(state:CdnConfig, sku) = { state with Sku = sku }
    [<CustomOperation "add_endpoints">]
    member _.AddEndpoints(state:CdnConfig, endpoints) = { state with Endpoints = state.Endpoints @ endpoints }

type EndpointBuilder() =
    member _.Yield _ : Endpoint =
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
    member _.Name(state:Endpoint, name) = { state with Name = name }
    member this.Name(state:Endpoint, name) = this.Name(state, ResourceName name)
    /// The address of the origin.
    [<CustomOperation "origin">]
    member _.Origin(state:Arm.Cdn.Endpoint, name) =
      { state with
          Name = state.Name.IfEmpty ((name |> Seq.filter Char.IsLetterOrDigit |> Seq.toArray |> String) + "-endpoint")
          Origin = name }

    [<CustomOperation "depends_on">]
    member _.DependsOn(state:Endpoint, resourceName) = { state with DependsOn = resourceName :: state.DependsOn }
    member _.DependsOn(state:Endpoint, resource:IBuilder) = { state with DependsOn = resource.DependencyName :: state.DependsOn }
    member _.DependsOn(state:Endpoint, resource:IArmResource) = { state with DependsOn = resource.ResourceName :: state.DependsOn }

    /// Adds a list of MIME content types on which compression applies.
    [<CustomOperation "add_compressed_content">]
    member _.AddCompressedContentTypes(state:Endpoint, types) = { state with CompressedContentTypes = state.CompressedContentTypes + Set types; Compression = Enabled }
    /// Defines how CDN caches requests that include query strings.
    [<CustomOperation "query_string_caching_behaviour">]
    member _.QueryStringCachingBehaviour(state:Endpoint, behaviour) = { state with QueryStringCachingBehaviour = behaviour }

    [<CustomOperation "enable_http">]
    member _.EnableHttp(state:Endpoint) = { state with Http = Enabled }
    [<CustomOperation "disable_http">]
    member _.DisableHttp(state:Endpoint) = { state with Http = Disabled }
    [<CustomOperation "enable_https">]
    member _.EnableHttps(state:Endpoint) = { state with Https = Enabled }
    [<CustomOperation "disable_https">]
    member _.DisableHttps(state:Endpoint) = { state with Https = Disabled }
    /// Name of the custom domain hostname.
    [<CustomOperation "custom_domain_name">]
    member _.CustomDomain(state:Endpoint, hostname) = { state with CustomDomain = Some (System.Uri hostname) }
    /// Specifies what scenario the customer wants this CDN endpoint to optimise for.
    [<CustomOperation "optimise_for">]
    member _.OptimiseFor(state:Endpoint, optimizationType) = { state with OptimizationType = optimizationType }

let cdn = CdnBuilder()
let endpoint = EndpointBuilder()
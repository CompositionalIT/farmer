[<AutoOpen>]
module Farmer.Builders.Cdn

open Farmer
open Farmer.Arm.Cdn
open Farmer.CoreTypes
open Farmer.Cdn

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
          Sku = Sku.Standard_Microsoft
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
          QueryStringCachingBehaviour = None
          Http = Enabled
          Https = Enabled
          Compression = Enabled
          HostName = ""
          CustomDomain = None }

    [<CustomOperation "name">]
    member _.Name(state:Endpoint, name) = { state with Name = name }
    member this.Name(state:Endpoint, name) = this.Name(state, ResourceName name)

    [<CustomOperation "depends_on">]
    member _.DependsOn(state:Endpoint, resourceName) = { state with DependsOn = resourceName :: state.DependsOn }
    member _.DependsOn(state:Endpoint, resource:IBuilder) = { state with DependsOn = resource.DependencyName :: state.DependsOn }
    member _.DependsOn(state:Endpoint, resource:IArmResource) = { state with DependsOn = resource.ResourceName :: state.DependsOn }

    [<CustomOperation "add_compressed_content">]
    member _.AddCompressedContentTypes(state:Endpoint, types) = { state with CompressedContentTypes = state.CompressedContentTypes + Set types }

    [<CustomOperation "query_string_caching_behaviour">]
    member _.QueryStringCachingBehaviour(state:Endpoint, behaviour) = {state with QueryStringCachingBehaviour = Some behaviour }

    [<CustomOperation "enable_http">]
    member _.EnableHttp(state:Endpoint) = { state with Http = Enabled }
    [<CustomOperation "disable_http">]
    member _.DisableHttp(state:Endpoint) = { state with Http = Disabled }
    [<CustomOperation "enable_https">]
    member _.EnableHttps(state:Endpoint) = { state with Https = Enabled }
    [<CustomOperation "disable_https">]
    member _.DisableHttps(state:Endpoint) = { state with Https = Disabled }
    [<CustomOperation "enable_compression">]
    member _.EnableCompression(state:Endpoint) = { state with Compression = Enabled }
    [<CustomOperation "disable_compression">]
    member _.DisableCompression(state:Endpoint) = { state with Compression = Disabled }
    [<CustomOperation "hostname">]
    member _.HostName(state:Endpoint, name) = { state with HostName = name }
    [<CustomOperation "custom_domain_hostname">]
    member _.CustomDomain(state:Endpoint, hostname) = { state with CustomDomain = Some (System.Uri hostname) }

let cdn = CdnBuilder()
let endpoint = EndpointBuilder()
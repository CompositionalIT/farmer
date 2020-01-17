[<AutoOpen>]
module Farmer.Resources.Cdn

open Farmer
open Farmer.Models

type CdnConfig =
    { ProfileName : ResourceName
      EndpointName : ResourceName
      Sku : CdnSku
      HostName : string option
      HttpAllowed : bool
      HttpsAllowed : bool
      QueryStringCachingBehavior : QueryStringCacheBehavior option
      ContentTypes : string list
      CustomDomains : string list
    }
        
type CdnBuilder() =
    member _.Yield _ =
        { ProfileName = ResourceName.Empty
          EndpointName = ResourceName.Empty
          Sku = CdnSku.Standard_Microsoft
          HostName = None
          HttpAllowed = true
          HttpsAllowed = true
          QueryStringCachingBehavior = None
          ContentTypes = []
          CustomDomains = []
        }
             
    /// Sets the name of the CDN profile.
    [<CustomOperation "name">]
    member _.Name(state:CdnConfig, name) = { state with ProfileName = ResourceName name }
    /// Sets the name of the CDN endpoint.
    [<CustomOperation "endpoint_name">]
    member _.EndpointName(state:CdnConfig, name) = { state with EndpointName = ResourceName name }
    /// Sets the sku of the CDN.
    [<CustomOperation "sku">]
    member _.Sku(state:CdnConfig, sku) = { state with Sku = sku }
    [<CustomOperation "host_name">]
    member _.HostName(state:CdnConfig, header) = { state with HostName = Some header }
    [<CustomOperation "disable_http">]
    member _.DisableHttp(state:CdnConfig) = { state with HttpAllowed = false }
    [<CustomOperation "disable_https">]
    member _.DisableHttps(state:CdnConfig) = { state with HttpsAllowed = false }
    [<CustomOperation "query_string_cache_behavior">]
    member _.QueryStringCacheBehavior(state:CdnConfig, behavior) = { state with QueryStringCachingBehavior = Some behavior }
    [<CustomOperation "add_content_type_to_compress">]
    member _.AddContentType(state:CdnConfig, contentType) = { state with ContentTypes = contentType :: state.ContentTypes }
    [<CustomOperation "set_content_types_to_compress">]
    member _.SetContentTypes(state:CdnConfig, contentTypes) = { state with ContentTypes = contentTypes }
    [<CustomOperation "add_custom_domain">]
    member _.AddCustomDomain(state:CdnConfig, hostname) = { state with CustomDomains = hostname :: state.CustomDomains }

module Converters =   
    let cdnProfile _ (cdn:CdnConfig) =
        let endpoint =
            { Name = cdn.EndpointName
              OriginHostHeader = cdn.HostName
              IsHttpAllowed = Some cdn.HttpAllowed
              IsHttpsAllowed = Some cdn.HttpsAllowed
              ContentTypesToCompress = cdn.ContentTypes |> List.toArray
              IsCompressionEnabled = not cdn.ContentTypes.IsEmpty |> Some
              QueryStringCachingBehavior = cdn.QueryStringCachingBehavior
              OriginPath = None
              OptimizationPath = None
              ProbePath = None
              GeoFilters = [||]
              DeliveryPolicy = None
              Origins = [|
                  match cdn.HostName with
                  | Some hostName ->
                      {| Name = hostName.Replace(".", "-")
                         HostName = hostName
                         HttpPort = None
                         HttpsPort = None |}
                  | None ->
                        ()
              |]
              CustomDomains = [|
                  for domain in cdn.CustomDomains ->
                      { Name = sprintf "%s/%s/%s" cdn.ProfileName.Value cdn.EndpointName.Value domain |> ResourceName
                        HostName = domain }
              |]
            }
        { Name = cdn.ProfileName
          Sku = cdn.Sku
          Endpoint = endpoint }


let cdn = CdnBuilder()
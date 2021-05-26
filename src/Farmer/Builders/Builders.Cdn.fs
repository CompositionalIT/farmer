[<AutoOpen>]
module Farmer.Builders.Cdn

open Farmer
open Farmer.Arm.Cdn
open Profiles
open Endpoints
open Farmer.Cdn
open System
open DeliveryPolicy

type RuleConfig =
    { Name: ResourceName
      Order: int
      Conditions: Condition list
      Actions: Action list }

type EndpointConfig =
    { Name : ResourceName
      Dependencies : ResourceId Set
      CompressedContentTypes : string Set
      QueryStringCachingBehaviour : QueryStringCachingBehaviour
      Http : FeatureFlag
      Https : FeatureFlag
      Compression : FeatureFlag
      Origin : ArmExpression
      CustomDomain : string option
      OptimizationType : OptimizationType
      DeliveryPolicyDescription: string
      Rules: RuleConfig list }

type CdnConfig =
    { Name : ResourceName
      Sku : Sku
      Endpoints : EndpointConfig list
      Tags: Map<string,string> }
    interface IBuilder with
        member this.ResourceId = profiles.resourceId this.Name
        member this.BuildResources _ = [
            { Name = this.Name
              Sku = this.Sku
              Tags = this.Tags}
            for endpoint in this.Endpoints do
                { Name = endpoint.Name
                  Profile = this.Name
                  Dependencies = endpoint.Dependencies
                  CompressedContentTypes = endpoint.CompressedContentTypes
                  QueryStringCachingBehaviour = endpoint.QueryStringCachingBehaviour
                  Http = endpoint.Http
                  Https = endpoint.Https
                  Compression = endpoint.Compression
                  Origin = endpoint.Origin
                  OptimizationType = endpoint.OptimizationType
                  Tags = this.Tags
                  DeliveryPolicy =
                      { Description = endpoint.DeliveryPolicyDescription
                        Rules =
                            endpoint.Rules
                            |> List.map
                                (fun r ->
                                    { Name = r.Name
                                      Order = r.Order
                                      Conditions = r.Conditions
                                      Actions = r.Actions }) } }

                match endpoint.CustomDomain with
                | Some customDomain ->
                    { Name = endpoint.Name.Map (sprintf "%sdomain")
                      Profile = this.Name
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
    interface ITaggable<CdnConfig> with member _.Add state tags = { state with Tags = state.Tags |> Map.merge tags }

type EndpointBuilder() =
    interface IDependable<EndpointConfig> with member _.Add state newDeps = { state with Dependencies = state.Dependencies + newDeps }
    member _.Yield _ : EndpointConfig =
        { Name = ResourceName.Empty
          Dependencies = Set.empty
          CompressedContentTypes = Set.empty
          QueryStringCachingBehaviour = UseQueryString
          Http = Enabled
          Https = Enabled
          Compression = Disabled
          Origin = ArmExpression.Empty
          CustomDomain = None
          OptimizationType = GeneralWebDelivery
          DeliveryPolicyDescription = ""
          Rules = [] }

    /// Name of the endpoint within the CDN.
    [<CustomOperation "name">]
    member _.Name(state:EndpointConfig, name) = { state with Name = name }
    member this.Name(state:EndpointConfig, name) = this.Name(state, ResourceName name)
    /// The address of the origin.
    [<CustomOperation "origin">]
    member _.Origin(state:EndpointConfig, name:ArmExpression) =
      { state with
          Name = state.Name.IfEmpty ((name.Value |> Seq.filter Char.IsLetterOrDigit |> Seq.toArray |> String) + "-endpoint")
          Origin = name }
    member this.Origin(state:EndpointConfig, name) = this.Origin(state, ArmExpression.literal name)
    member this.Origin(state:EndpointConfig, name:Uri) = this.Origin(state, ArmExpression.literal name.Host)

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
    member _.CustomDomain(state:EndpointConfig, hostname) = { state with CustomDomain = Some hostname }
    /// Specifies what scenario the customer wants this CDN endpoint to optimise for.
    [<CustomOperation "optimise_for">]
    member _.OptimiseFor(state:EndpointConfig, optimizationType) = { state with OptimizationType = optimizationType }
    [<CustomOperation "add_rule">]
    member _.AddRule(state: EndpointConfig, rule: RuleConfig) = { state with Rules = state.Rules @ [ rule ] }
    [<CustomOperation "add_rules">]
    member _.AddRules(state: EndpointConfig, rules: RuleConfig list) = { state with Rules = state.Rules @ rules }

type RuleBuilder () =
    interface IDependable<EndpointConfig> with
        member _.Add state newDeps = { state with Dependencies = state.Dependencies + newDeps }
    member _.Yield _ : RuleConfig =
        { Name = ResourceName.Empty
          Order = 1
          Conditions = list.Empty
          Actions = list.Empty }
    [<CustomOperation "name">]
    member _.Name(state: RuleConfig, name) = { state with Name = name }
    member this.Name(state: RuleConfig, name) = this.Name (state, ResourceName name)
    [<CustomOperation "order">]
    member _.Order(state: RuleConfig, order) = { state with Order = order }
    [<CustomOperation "when_device_type">]
    member _.WhenDeviceType(state: RuleConfig, operator, deviceType) =
        { state with
              Conditions = state.Conditions @ [ IsDevice {| Operator = operator ; DeviceType = deviceType |} ] }
    [<CustomOperation "when_http_version">]
    member _.WhenHttpVersion(state: RuleConfig, operator, httpVersions) =
        { state with
              Conditions = state.Conditions @ [ HttpVersion {| Operator = operator ; HttpVersions = httpVersions |} ] }
    [<CustomOperation "when_request_cookies">]
    member _.WhenRequestCookies(state: RuleConfig, cookiesName, operator, cookiesValue, caseTransform) =
        { state with
              Conditions =
                  state.Conditions
                  @ [ RequestCookies
                          {| CookiesName = cookiesName
                             Operator = operator
                             CookiesValue = cookiesValue
                             CaseTransform = caseTransform |} ] }
    [<CustomOperation "when_post_argument">]
    member _.WhenPostArgument(state: RuleConfig, argumentName, operator, argumentValue, caseTransform) =
        { state with
              Conditions =
                  state.Conditions
                  @ [ PostArgument
                          {| ArgumentName = argumentName
                             Operator = operator
                             ArgumentValue = argumentValue
                             CaseTransform = caseTransform |} ] }
    [<CustomOperation "when_query_string">]
    member _.WhenQueryString(state: RuleConfig, operator, queryString, caseTransform) =
        { state with
              Conditions =
                  state.Conditions
                  @ [ QueryString
                          {| Operator = operator
                             QueryString = queryString
                             CaseTransform = caseTransform |} ] }
    [<CustomOperation "when_remote_address">]
    member _.WhenRemoteAddress(state: RuleConfig, operator, matchValues) =
        { state with
              Conditions = state.Conditions @ [ RemoteAddress {| Operator = operator ; MatchValues = matchValues |} ] }
    [<CustomOperation "when_request_body">]
    member _.WhenRequestBody(state: RuleConfig, operator, requestBody, caseTransform) =
        { state with
              Conditions =
                  state.Conditions
                  @ [ RequestBody
                          {| Operator = operator
                             RequestBody = requestBody
                             CaseTransform = caseTransform |} ] }
    [<CustomOperation "when_request_header">]
    member _.WhenRequestHeader(state: RuleConfig, headerName, operator, headerValue, caseTransform) =
        { state with
              Conditions =
                  state.Conditions
                  @ [ RequestHeader
                          {| HeaderName = headerName
                             Operator = operator
                             HeaderValue = headerValue
                             CaseTransform = caseTransform |} ] }
    [<CustomOperation "when_request_method">]
    member _.WhenRequestMethod(state: RuleConfig, operator, requestMethod) =
        { state with
              Conditions = state.Conditions @ [ RequestMethod {| Operator = operator ; RequestMethod = requestMethod |} ] }
    [<CustomOperation "when_request_protocol">]
    member _.WhenRequestProtocol(state: RuleConfig, operator, value) =
        { state with
              Conditions = state.Conditions @ [ RequestProtocol {| Operator = operator ; Value = value |} ] }
    [<CustomOperation "when_request_url">]
    member _.WhenRequestUrl(state: RuleConfig, operator, requestUrl, caseTransform) =
        { state with
              Conditions =
                  state.Conditions
                  @ [ RequestUrl
                          {| Operator = operator
                             RequestUrl = requestUrl
                             CaseTransform = caseTransform |} ] }
    [<CustomOperation "when_url_file_extension">]
    member _.WhenUrlFileExtension(state: RuleConfig, operator, extension, caseTransform) =
        { state with
              Conditions =
                  state.Conditions
                  @ [ UrlFileExtension
                          {| Operator = operator
                             Extension = extension
                             CaseTransform = caseTransform |} ] }
    [<CustomOperation "when_url_file_name">]
    member _.WhenUrlFileName(state: RuleConfig, operator, fileName, caseTransform) =
        { state with
              Conditions =
                  state.Conditions
                  @ [ UrlFileName
                          {| Operator = operator
                             FileName = fileName
                             CaseTransform = caseTransform |} ] }
    [<CustomOperation "when_url_path">]
    member _.WhenUrlPath(state: RuleConfig, operator, value, caseTransform) =
        { state with
              Conditions =
                  state.Conditions
                  @ [ UrlPath
                          {| Operator = operator
                             Value = value
                             CaseTransform = caseTransform |} ] }

    [<CustomOperation "cache_expiration">]
    member _.CacheExpiration(state: RuleConfig, cacheBehaviour, ?cacheDuration) =
        { state with
              Actions = state.Actions @ [ CacheExpiration {| CacheBehaviour = cacheBehaviour ; CacheDuration = cacheDuration |} ] }
    [<CustomOperation "cache_key_query_string">]
    member _.CacheKeyQueryString(state: RuleConfig, behaviour, parameters) =
        { state with
              Actions = state.Actions @ [ CacheKeyQueryString {| Behaviour = behaviour ; Parameters = parameters |} ] }
    [<CustomOperation "modify_request_header">]
    member _.ModifyRequestHeader(state: RuleConfig, action, httpHeaderName, httpHeaderValue) =
        { state with
              Actions =
                  state.Actions
                  @ [ ModifyRequestHeader
                          { Action = action
                            HttpHeaderName = httpHeaderName
                            HttpHeaderValue = httpHeaderValue } ] }
    [<CustomOperation "modify_response_header">]
    member _.ModifyResponseHeader(state: RuleConfig, action, httpHeaderName, httpHeaderValue) =
        { state with
              Actions =
                  state.Actions
                  @ [ ModifyResponseHeader
                          { Action = action
                            HttpHeaderName = httpHeaderName
                            HttpHeaderValue = httpHeaderValue } ] }

    [<CustomOperation "url_rewrite">]
    member _.UrlRewrite(state: RuleConfig, sourcePattern, destination, preserveUnmatchedPath) =
        { state with
              Actions =
                  state.Actions
                  @ [ UrlRewrite
                          {| SourcePattern = sourcePattern
                             Destination = destination
                             PreserveUnmatchedPath = preserveUnmatchedPath |} ] }
    [<CustomOperation "url_redirect">]
    member _.UrlRedirect
        (
            state: RuleConfig,
            redirectType,
            destinationProtocol,
            ?hostname,
            ?path,
            ?queryString,
            ?fragment
        ) =
        { state with
              Actions =
                  state.Actions
                  @ [ UrlRedirect
                          {| RedirectType = redirectType
                             DestinationProtocol = destinationProtocol
                             Hostname = hostname
                             Path = path
                             QueryString = queryString
                             Fragment = fragment |} ] }

let cdn = CdnBuilder()

let endpoint = EndpointBuilder()

let rule = RuleBuilder ()
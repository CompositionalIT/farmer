[<AutoOpen>]
module Farmer.Arm.Cdn

open Farmer
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
        member this.ResourceId = profiles.resourceId this.Name
        member this.JsonModel =
            {| profiles.Create (this.Name, Location.Global, tags = this.Tags) with
                   sku = {| name = string this.Sku |}
                   properties = {||}
            |} |> box

module DeliveryPolicy =
    type IOperator =
        abstract member AsOperator : string
        abstract member AsNegateCondition : bool

    type EqualityOperator =
        | Equals
        | NotEquals
        interface IOperator with
            member this.AsOperator = "Equal"

            member this.AsNegateCondition =
                match this with
                | Equals -> false
                | NotEquals -> true

    type StringComparisonOperator =
        | Any
        | Equals
        | Contains
        | BeginsWith
        | EndsWith
        | LessThan
        | LessThanOrEquals
        | GreaterThan
        | GreaterThanOrEquals
        | NotAny
        | NotEquals
        | NotContains
        | NotBeginsWith
        | NotEndsWith
        | NotLessThan
        | NotLessThanOrEquals
        | NotGreaterThan
        | NotGreaterThanOrEquals
        interface IOperator with
            member this.AsOperator =
                match this with
                | Any
                | NotAny -> "Any"
                | Equals
                | NotEquals -> "Equal"
                | Contains
                | NotContains -> "Contains"
                | BeginsWith
                | NotBeginsWith -> "BeginsWith"
                | EndsWith
                | NotEndsWith -> "EndsWith"
                | LessThan
                | NotLessThan -> "LessThan"
                | LessThanOrEquals
                | NotLessThanOrEquals -> "LessThanOrEqual"
                | GreaterThan
                | NotGreaterThan -> "GreaterThan"
                | GreaterThanOrEquals
                | NotGreaterThanOrEquals -> "GreaterThanOrEqual"

            member this.AsNegateCondition =
                match this with
                | NotAny
                | NotEquals
                | NotContains
                | NotBeginsWith
                | NotEndsWith
                | NotLessThan
                | NotLessThanOrEquals
                | NotGreaterThan
                | NotGreaterThanOrEquals -> true
                | _ -> false

    type RemoteAddressOperator =
        | Any
        | GeoMatch
        | IPMatch
        | NotAny
        | NotGeoMatch
        | NotIPMatch
        interface IOperator with
            member this.AsOperator =
                match this with
                | Any
                | NotAny -> "Any"
                | GeoMatch
                | NotGeoMatch -> "GeoMatch"
                | IPMatch
                | NotIPMatch -> "IPMatch"

            member this.AsNegateCondition =
                match this with
                | NotAny
                | NotGeoMatch
                | NotIPMatch -> true
                | _ -> false

    type DeviceType =
        | Mobile
        | Desktop
        member this.ArmValue =
            match this with
            | Desktop -> "Desktop"
            | Mobile -> "Mobile"

    type HttpVersion =
        | Version20
        | Version11
        | Version10
        | Version09
        member this.ArmValue =
            match this with
            | Version20 -> "2.0"
            | Version11 -> "1.1"
            | Version10 -> "1.0"
            | Version09 -> "0.9"

    type RequestMethod =
        | Get
        | Post
        | Put
        | Delete
        | Head
        | Options
        | Trace
        member this.ArmValue =
            match this with
            | Get -> "GET"
            | Post -> "POST"
            | Put -> "PUT"
            | Delete -> "DELETE"
            | Head -> "HEAD"
            | Options -> "OPTIONS"
            | Trace -> "TRACE"

    type Protocol =
        | Http
        | Https
        member this.ArmValue =
            match this with
            | Http -> "Http"
            | Https -> "Https"

    type UrlRedirectProtocol =
        | Http
        | Https
        | MatchRequest
        member this.ArmValue =
            match this with
            | Http -> "Http"
            | Https -> "Https"
            | MatchRequest -> "MatchRequest"

    type CaseTransform =
        | NoTransform
        | ToLowercase
        | ToUppercase
        member this.ArmValue =
            match this with
            | NoTransform -> []
            | ToLowercase -> [ "Lowercase" ]
            | ToUppercase -> [ "Uppercase" ]

    type Condition =
        | IsDevice of {| Operator: EqualityOperator ; DeviceType: DeviceType |}
        | HttpVersion of {| Operator: EqualityOperator ; HttpVersions: HttpVersion list |}
        | RequestCookies of
            {| CookiesName: string
               Operator: StringComparisonOperator
               CookiesValue: string list
               CaseTransform: CaseTransform |}
        | PostArgument of
            {| ArgumentName: string
               Operator: StringComparisonOperator
               ArgumentValue: string list
               CaseTransform: CaseTransform |}
        | QueryString of
            {| Operator: StringComparisonOperator
               QueryString: string list
               CaseTransform: CaseTransform |}
        | RemoteAddress of {| Operator: RemoteAddressOperator ; MatchValues: string list |}
        | RequestBody of
            {| Operator: StringComparisonOperator
               RequestBody: string list
               CaseTransform: CaseTransform |}
        | RequestHeader of
            {| HeaderName: string
               Operator: StringComparisonOperator
               HeaderValue: string list
               CaseTransform: CaseTransform |}
        | RequestMethod of {| Operator: EqualityOperator ; RequestMethod: RequestMethod |}
        | RequestProtocol of {| Operator: EqualityOperator ; Value: Protocol |}
        | RequestUrl of
            {| Operator: StringComparisonOperator
               RequestUrl: string list
               CaseTransform: CaseTransform |}
        | UrlFileExtension of
            {| Operator: StringComparisonOperator
               Extension: string list
               CaseTransform: CaseTransform |}
        | UrlFileName of
            {| Operator: StringComparisonOperator
               FileName: string list
               CaseTransform: CaseTransform |}
        | UrlPath of
            {| Operator: StringComparisonOperator
               Value: string list
               CaseTransform: CaseTransform |}

        member this.MapCondition
            (
                name: string,
                dataType: string,
                operator: IOperator,
                matchValues: string list,
                ?caseTransform: CaseTransform,
                ?selector: string,
                ?additionalParameters: Map<string, obj>
            ) =

            {| name = name
               parameters =
                   (match additionalParameters with
                    | Some p -> p
                    | None -> Map.empty<string, obj>)
                       .Add("@odata.type", dataType)
                       .Add("operator", operator.AsOperator)
                       .Add("negateCondition", operator.AsNegateCondition)
                       .Add("matchValues", matchValues)
                       .Add(
                           "transforms",
                           (match caseTransform with
                            | Some t -> t
                            | None -> CaseTransform.NoTransform)
                               .ArmValue
                       )
                       .Add (
                           "selector",
                           (match selector with
                            | Some s -> s
                            | None -> "")
                       ) |}


        member this.MapCondition
            (
                name: string,
                dataType: string,
                operator: IOperator,
                matchValue: string,
                ?caseTransform: CaseTransform,
                ?selector: string,
                ?additionalParameters: Map<string, obj>
            ) =
            this.MapCondition (
                name,
                dataType,
                operator,
                [ matchValue ],
                (match caseTransform with
                 | Some t -> t
                 | None -> CaseTransform.NoTransform),
                (match selector with
                 | Some s -> s
                 | None -> string None),
                match additionalParameters with
                | Some p -> p
                | None -> Map.empty<string, obj>
            )

        member this.JsonModel =
            match this with
            | IsDevice c ->
                this.MapCondition (
                    "IsDevice",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleIsDeviceConditionParameters",
                    c.Operator,
                    c.DeviceType.ArmValue
                )
            | HttpVersion c ->
                this.MapCondition (
                    "HttpVersion",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleHttpVersionConditionParameters",
                    c.Operator,
                    c.HttpVersions |> List.map (fun v -> v.ArmValue)
                )
            | RequestCookies c ->
                this.MapCondition (
                    "Cookies",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleCookiesConditionParameters",
                    c.Operator,
                    c.CookiesValue,
                    c.CaseTransform,
                    c.CookiesName
                )
            | PostArgument c ->
                this.MapCondition (
                    "PostArgs",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRulePostArgsConditionParameters",
                    c.Operator,
                    c.ArgumentValue,
                    c.CaseTransform,
                    c.ArgumentName
                )
            | QueryString c ->
                this.MapCondition (
                    "QueryString",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleQueryStringConditionParameters",
                    c.Operator,
                    c.QueryString,
                    c.CaseTransform
                )
            | RemoteAddress c ->
                this.MapCondition (
                    "RemoteAddress",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleRemoteAddressConditionParameters",
                    c.Operator,
                    c.MatchValues
                )
            | RequestBody c ->
                this.MapCondition (
                    "RequestBody",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleRequestBodyConditionParameters",
                    c.Operator,
                    c.RequestBody,
                    c.CaseTransform
                )
            | RequestHeader c ->
                this.MapCondition (
                    "RequestHeader",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleRequestHeaderConditionParameters",
                    c.Operator,
                    c.HeaderValue,
                    c.CaseTransform,
                    c.HeaderName
                )
            | RequestMethod c ->
                this.MapCondition (
                    "RequestMethod",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleRequestMethodConditionParameters",
                    c.Operator,
                    c.RequestMethod.ArmValue
                )
            | RequestProtocol c ->
                this.MapCondition (
                    "RequestScheme",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleRequestSchemeConditionParameters",
                    c.Operator,
                    c.Value.ArmValue
                )
            | RequestUrl c ->
                this.MapCondition (
                    "RequestUri",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleRequestUriConditionParameters",
                    c.Operator,
                    c.RequestUrl,
                    c.CaseTransform
                )
            | UrlFileExtension c ->
                this.MapCondition (
                    "UrlFileExtension",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleUrlFileExtensionMatchConditionParameters",
                    c.Operator,
                    c.Extension,
                    c.CaseTransform
                )
            | UrlFileName c ->
                this.MapCondition (
                    "UrlFileName",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleUrlFilenameConditionParameters",
                    c.Operator,
                    c.FileName,
                    c.CaseTransform
                )
            | UrlPath c ->
                this.MapCondition (
                    "UrlPath",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleUrlPathMatchConditionParameters",
                    c.Operator,
                    c.Value,
                    c.CaseTransform
                )

    type CacheBehaviour =
        | Override
        | BypassCache
        | SetIfMissing
        member this.ArmValue =
            match this with
            | Override -> "Override"
            | BypassCache -> "BypassCache"
            | SetIfMissing -> "SetIfMissing"

    type QueryStringCacheBehavior =
        | Include
        | IncludeAll
        | Exclude
        | ExcludeAll
        member this.ArmValue =
            match this with
            | Include -> "Include"
            | IncludeAll -> "IncludeAll"
            | Exclude -> "Exclude"
            | ExcludeAll -> "ExcludeAll"

    type ModifyHeaderAction =
        | Append
        | Overwrite
        | Delete
        member this.ArmValue =
            match this with
            | Append -> "Append"
            | Overwrite -> "Overwrite"
            | Delete -> "Delete"

    type ModifyHeader =
        { Action: ModifyHeaderAction
          HttpHeaderName: string
          HttpHeaderValue: string }

    type RedirectType =
        | Found
        | Moved
        | TemporaryRedirect
        | PermanentRedirect
        member this.ArmValue =
            match this with
            | Found -> "Found"
            | Moved -> "Moved"
            | TemporaryRedirect -> "TemporaryRedirect"
            | PermanentRedirect -> "PermanentRedirect"

    type Action =
        | CacheExpiration of {| CacheBehaviour: CacheBehaviour ; CacheDuration: TimeSpan option |}
        | CacheKeyQueryString of {| Behaviour: QueryStringCacheBehavior ; Parameters: string |}
        | ModifyRequestHeader of ModifyHeader
        | ModifyResponseHeader of ModifyHeader
        | UrlRewrite of
            {| SourcePattern: string
               Destination: string
               PreserveUnmatchedPath: bool |}
        | UrlRedirect of
            {| RedirectType: RedirectType
               DestinationProtocol: UrlRedirectProtocol
               Hostname: string option
               Path: string option
               QueryString: string option
               Fragment: string option |}

        member this.JsonModel =
            let map (name: string) (dataType: string) (parameters: Map<_, obj>) =
                {| name = name
                   parameters = parameters.Add ("@odata.type", dataType) |}

            let mapModifyHeader name (modifyHeader: ModifyHeader) =
                map
                    name
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleHeaderActionParameters"
                    (Map.empty<_, obj>
                        .Add("headerAction", modifyHeader.Action.ArmValue)
                        .Add("headerName", modifyHeader.HttpHeaderName)
                        .Add ("value", modifyHeader.HttpHeaderValue))

            let mapOption(value: string option) =
                match value with
                | Some p -> p
                | None -> null

            match this with
            | CacheExpiration a ->
                map
                    "CacheExpiration"
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleCacheExpirationActionParameters"
                    (Map.empty<_, obj>
                        .Add("cacheBehavior", a.CacheBehaviour.ArmValue)
                        .Add("cacheType", "All")
                        .Add (
                            "cacheDuration",
                            mapOption (
                                match a.CacheDuration with
                                | Some d -> Some (d.ToString "d\.hh\:mm\:ss")
                                | None -> None
                            )
                        ))
            | CacheKeyQueryString a ->
                map
                    "CacheKeyQueryString"
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleCacheKeyQueryStringBehaviorActionParameters"
                    (Map.empty<_, obj>
                        .Add("queryStringBehavior", a.Behaviour.ArmValue)
                        .Add ("queryParameters", a.Parameters))
            | ModifyRequestHeader a -> mapModifyHeader "ModifyRequestHeader" a
            | ModifyResponseHeader a -> mapModifyHeader "ModifyResponseHeader" a
            | UrlRewrite a ->
                map
                    "UrlRewrite"
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleUrlRewriteActionParameters"
                    (Map.empty<_, obj>
                        .Add("sourcePattern", a.SourcePattern)
                        .Add("destination", a.Destination)
                        .Add ("preserveUnmatchedPath", a.PreserveUnmatchedPath))
            | UrlRedirect a ->
                map
                    "UrlRedirect"
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleUrlRedirectActionParameters"
                    (Map.empty<_, obj>
                        .Add("redirectType", a.RedirectType.ArmValue)
                        .Add("destinationProtocol", a.DestinationProtocol.ArmValue)
                        .Add("customQueryString", mapOption a.QueryString)
                        .Add("customPath", mapOption a.Path)
                        .Add("customHostname", mapOption a.Hostname)
                        .Add ("customFragment", mapOption a.Fragment))

    type Rule =
        { Name: ResourceName
          Order: int
          Conditions: Condition list
          Actions: Action list }

type DeliveryPolicy = { Description: string ; Rules: DeliveryPolicy.Rule list }

module Profiles =
    type Endpoint =
        { Name : ResourceName
          Profile : ResourceName
          Dependencies : ResourceId Set
          CompressedContentTypes : string Set
          QueryStringCachingBehaviour : QueryStringCachingBehaviour
          Http : FeatureFlag
          Https : FeatureFlag
          Compression : FeatureFlag
          Origin : ArmExpression
          OptimizationType : OptimizationType
          DeliveryPolicy : DeliveryPolicy
          Tags: Map<string,string> }
        interface IArmResource with
            member this.ResourceId = endpoints.resourceId (this.Profile/this.Name)
            member this.JsonModel =
                let dependencies = [
                    profiles.resourceId this.Profile
                    yield! Option.toList this.Origin.Owner
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
                               deliveryPolicy =
                                  {| description = this.DeliveryPolicy.Description
                                     rules =
                                         this.DeliveryPolicy.Rules
                                         |> List.map
                                             (fun rule ->
                                                 {| name = rule.Name.Value
                                                    order = rule.Order
                                                    conditions = rule.Conditions |> List.map (fun c -> c.JsonModel)
                                                    actions = rule.Actions |> List.map (fun a -> a.JsonModel) |}) |} |}
                |} :> _

    module Endpoints =
        type CustomDomain =
            { Name : ResourceName
              Profile : ResourceName
              Endpoint : ResourceName
              Hostname : string }
            interface IArmResource with
                member this.ResourceId = customDomains.resourceId (this.Profile/this.Endpoint/this.Name)
                member this.JsonModel =
                    {| customDomains.Create (this.Profile/this.Endpoint/this.Name, dependsOn = [ endpoints.resourceId(this.Profile, this.Endpoint) ]) with
                        properties = {| hostName = this.Hostname |}
                    |} :> _

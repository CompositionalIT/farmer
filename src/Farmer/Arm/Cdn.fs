[<AutoOpen>]
module Farmer.Arm.Cdn

open Farmer
open Farmer.Cdn
open System
open DeliveryPolicy

let profiles = ResourceType("Microsoft.Cdn/profiles", "2019-04-15")
let endpoints = ResourceType("Microsoft.Cdn/profiles/endpoints", "2019-04-15")

let customDomains =
    ResourceType("Microsoft.Cdn/profiles/endpoints/customDomains", "2019-04-15")

type Profile = {
    Name: ResourceName
    Sku: Sku
    Tags: Map<string, string>
} with

    interface IArmResource with
        member this.ResourceId = profiles.resourceId this.Name

        member this.JsonModel =
            {|
                profiles.Create(this.Name, Location.Global, tags = this.Tags) with
                    sku = {| name = string this.Sku |}
                    properties = {| |}
            |}
            |> box

module CdnRule =
    type Condition =
        | IsDevice of
            {|
                Operator: EqualityOperator
                DeviceType: DeviceType
            |}
        | HttpVersion of
            {|
                Operator: EqualityOperator
                HttpVersions: HttpVersion list
            |}
        | RequestCookies of
            {|
                CookiesName: string
                Operator: ComparisonOperator
                CookiesValue: string list
                CaseTransform: CaseTransform
            |}
        | PostArgument of
            {|
                ArgumentName: string
                Operator: ComparisonOperator
                ArgumentValue: string list
                CaseTransform: CaseTransform
            |}
        | QueryString of
            {|
                Operator: ComparisonOperator
                QueryString: string list
                CaseTransform: CaseTransform
            |}
        | RemoteAddress of
            {|
                Operator: RemoteAddressOperator
                MatchValues: string list
            |}
        | RequestBody of
            {|
                Operator: ComparisonOperator
                RequestBody: string list
                CaseTransform: CaseTransform
            |}
        | RequestHeader of
            {|
                HeaderName: string
                Operator: ComparisonOperator
                HeaderValue: string list
                CaseTransform: CaseTransform
            |}
        | RequestMethod of
            {|
                Operator: EqualityOperator
                RequestMethod: RequestMethod
            |}
        | RequestProtocol of
            {|
                Operator: EqualityOperator
                Value: Protocol
            |}
        | RequestUrl of
            {|
                Operator: ComparisonOperator
                RequestUrl: string list
                CaseTransform: CaseTransform
            |}
        | UrlFileExtension of
            {|
                Operator: ComparisonOperator
                Extension: string list
                CaseTransform: CaseTransform
            |}
        | UrlFileName of
            {|
                Operator: ComparisonOperator
                FileName: string list
                CaseTransform: CaseTransform
            |}
        | UrlPath of
            {|
                Operator: ComparisonOperator
                Value: string list
                CaseTransform: CaseTransform
            |}

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

            {|
                name = name
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
                        .Add(
                            "selector",
                            (match selector with
                             | Some s -> s
                             | None -> "")
                        )
            |}


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
            this.MapCondition(
                name,
                dataType,
                operator,
                [ matchValue ],
                caseTransform |> Option.defaultValue NoTransform,
                selector |> Option.defaultValue "",
                additionalParameters |> Option.defaultValue Map.empty
            )

        member this.JsonModel =
            match this with
            | IsDevice c ->
                this.MapCondition(
                    "IsDevice",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleIsDeviceConditionParameters",
                    c.Operator,
                    c.DeviceType.ArmValue
                )
            | HttpVersion c ->
                this.MapCondition(
                    "HttpVersion",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleHttpVersionConditionParameters",
                    c.Operator,
                    c.HttpVersions |> List.map (fun v -> v.ArmValue)
                )
            | RequestCookies c ->
                this.MapCondition(
                    "Cookies",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleCookiesConditionParameters",
                    c.Operator,
                    c.CookiesValue,
                    c.CaseTransform,
                    c.CookiesName
                )
            | PostArgument c ->
                this.MapCondition(
                    "PostArgs",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRulePostArgsConditionParameters",
                    c.Operator,
                    c.ArgumentValue,
                    c.CaseTransform,
                    c.ArgumentName
                )
            | QueryString c ->
                this.MapCondition(
                    "QueryString",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleQueryStringConditionParameters",
                    c.Operator,
                    c.QueryString,
                    c.CaseTransform
                )
            | RemoteAddress c ->
                this.MapCondition(
                    "RemoteAddress",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleRemoteAddressConditionParameters",
                    c.Operator,
                    c.MatchValues
                )
            | RequestBody c ->
                this.MapCondition(
                    "RequestBody",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleRequestBodyConditionParameters",
                    c.Operator,
                    c.RequestBody,
                    c.CaseTransform
                )
            | RequestHeader c ->
                this.MapCondition(
                    "RequestHeader",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleRequestHeaderConditionParameters",
                    c.Operator,
                    c.HeaderValue,
                    c.CaseTransform,
                    c.HeaderName
                )
            | RequestMethod c ->
                this.MapCondition(
                    "RequestMethod",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleRequestMethodConditionParameters",
                    c.Operator,
                    c.RequestMethod.ArmValue
                )
            | RequestProtocol c ->
                this.MapCondition(
                    "RequestScheme",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleRequestSchemeConditionParameters",
                    c.Operator,
                    c.Value.ArmValue
                )
            | RequestUrl c ->
                this.MapCondition(
                    "RequestUri",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleRequestUriConditionParameters",
                    c.Operator,
                    c.RequestUrl,
                    c.CaseTransform
                )
            | UrlFileExtension c ->
                this.MapCondition(
                    "UrlFileExtension",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleUrlFileExtensionMatchConditionParameters",
                    c.Operator,
                    c.Extension,
                    c.CaseTransform
                )
            | UrlFileName c ->
                this.MapCondition(
                    "UrlFileName",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleUrlFilenameConditionParameters",
                    c.Operator,
                    c.FileName,
                    c.CaseTransform
                )
            | UrlPath c ->
                this.MapCondition(
                    "UrlPath",
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleUrlPathMatchConditionParameters",
                    c.Operator,
                    c.Value,
                    c.CaseTransform
                )

    type ModifyHeader = {
        Action: ModifyHeaderAction
        HttpHeaderName: string
        HttpHeaderValue: string
    }

    type Action =
        | CacheExpiration of {| CacheBehaviour: CacheBehaviour |}
        | CacheKeyQueryString of
            {|
                Behaviour: QueryStringCacheBehavior
                Parameters: string
            |}
        | ModifyRequestHeader of ModifyHeader
        | ModifyResponseHeader of ModifyHeader
        | UrlRewrite of
            {|
                SourcePattern: string
                Destination: string
                PreserveUnmatchedPath: bool
            |}
        | UrlRedirect of
            {|
                RedirectType: RedirectType
                DestinationProtocol: UrlRedirectProtocol
                Hostname: string option
                Path: string option
                QueryString: string option
                Fragment: string option
            |}

        member this.JsonModel =
            let map (name: string) (dataType: string) (parameters: Map<_, obj>) = {|
                name = name
                parameters = parameters.Add("@odata.type", dataType)
            |}

            let mapModifyHeader name (modifyHeader: ModifyHeader) =
                map
                    name
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleHeaderActionParameters"
                    (Map.empty<_, obj>
                        .Add("headerAction", modifyHeader.Action.ArmValue)
                        .Add("headerName", modifyHeader.HttpHeaderName)
                        .Add("value", modifyHeader.HttpHeaderValue))


            match this with
            | CacheExpiration a ->
                let armValue = a.CacheBehaviour.ArmValue

                map
                    "CacheExpiration"
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleCacheExpirationActionParameters"
                    (Map.empty<_, obj>
                        .Add("cacheBehavior", armValue.Behaviour)
                        .Add("cacheType", "All")
                        .Add(
                            "cacheDuration",
                            armValue.CacheDuration
                            |> Option.map (fun d -> d.ToString "d\.hh\:mm\:ss")
                            |> Option.toObj
                        ))
            | CacheKeyQueryString a ->
                map
                    "CacheKeyQueryString"
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleCacheKeyQueryStringBehaviorActionParameters"
                    (Map.empty<_, obj>
                        .Add("queryStringBehavior", a.Behaviour.ArmValue)
                        .Add("queryParameters", a.Parameters))
            | ModifyRequestHeader a -> mapModifyHeader "ModifyRequestHeader" a
            | ModifyResponseHeader a -> mapModifyHeader "ModifyResponseHeader" a
            | UrlRewrite a ->
                map
                    "UrlRewrite"
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleUrlRewriteActionParameters"
                    (Map.empty<_, obj>
                        .Add("sourcePattern", a.SourcePattern)
                        .Add("destination", a.Destination)
                        .Add("preserveUnmatchedPath", a.PreserveUnmatchedPath))
            | UrlRedirect a ->
                map
                    "UrlRedirect"
                    "#Microsoft.Azure.Cdn.Models.DeliveryRuleUrlRedirectActionParameters"
                    (Map.empty<_, obj>
                        .Add("redirectType", a.RedirectType.ArmValue)
                        .Add("destinationProtocol", a.DestinationProtocol.ArmValue)
                        .Add("customQueryString", a.QueryString |> Option.toObj)
                        .Add("customPath", a.Path |> Option.toObj)
                        .Add("customHostname", a.Hostname |> Option.toObj)
                        .Add("customFragment", a.Fragment |> Option.toObj))

type Rule = {
    Name: ResourceName
    Order: int
    Conditions: CdnRule.Condition list
    Actions: CdnRule.Action list
}

type DeliveryPolicy = {
    Description: string
    Rules: Rule list
}

module Profiles =
    type Endpoint = {
        Name: ResourceName
        Profile: ResourceName
        Dependencies: ResourceId Set
        CompressedContentTypes: string Set
        QueryStringCachingBehaviour: QueryStringCachingBehaviour
        Http: FeatureFlag
        Https: FeatureFlag
        Compression: FeatureFlag
        Origin: ArmExpression
        OptimizationType: OptimizationType
        DeliveryPolicy: DeliveryPolicy
        Tags: Map<string, string>
    } with

        interface IArmResource with
            member this.ResourceId = endpoints.resourceId (this.Profile / this.Name)

            member this.JsonModel =
                let dependencies = [
                    profiles.resourceId this.Profile
                    yield! Option.toList this.Origin.Owner
                    yield! this.Dependencies
                ]

                {|
                    endpoints.Create(this.Profile / this.Name, Location.Global, dependencies, this.Tags) with
                        properties = {|
                            originHostHeader = this.Origin.Eval()
                            queryStringCachingBehavior = string this.QueryStringCachingBehaviour
                            optimizationType = string this.OptimizationType
                            isHttpAllowed = this.Http.AsBoolean
                            isHttpsAllowed = this.Https.AsBoolean
                            isCompressionEnabled = this.Compression.AsBoolean
                            contentTypesToCompress = this.CompressedContentTypes
                            origins = [
                                {|
                                    name = "origin"
                                    properties = {| hostName = this.Origin.Eval() |}
                                |}
                            ]
                            deliveryPolicy = {|
                                description = this.DeliveryPolicy.Description
                                rules =
                                    this.DeliveryPolicy.Rules
                                    |> List.map (fun rule -> {|
                                        name = rule.Name.Value
                                        order = rule.Order
                                        conditions = rule.Conditions |> List.map (fun c -> c.JsonModel)
                                        actions = rule.Actions |> List.map (fun a -> a.JsonModel)
                                    |})
                            |}
                        |}
                |}

    module Endpoints =
        type CustomDomain = {
            Name: ResourceName
            Profile: ResourceName
            Endpoint: ResourceName
            Hostname: string
        } with

            interface IArmResource with
                member this.ResourceId =
                    customDomains.resourceId (this.Profile / this.Endpoint / this.Name)

                member this.JsonModel = {|
                    customDomains.Create(
                        this.Profile / this.Endpoint / this.Name,
                        dependsOn = [ endpoints.resourceId (this.Profile, this.Endpoint) ]
                    ) with
                        properties = {| hostName = this.Hostname |}
                |}
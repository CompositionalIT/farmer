[<AutoOpen>]
module Farmer.Builders.ApiManagement

open Farmer
open Farmer.Arm.ApiManagement

type ApiManagementConfig = {
    Name: ResourceName
    Location: Location
    PublisherName: string
    PublisherEmail: string
    Sku: ApiManagementSku
    Capacity: int
    PublicNetworkAccess: string option
    VirtualNetworkType: string option
    DisableGateway: bool option
    CustomProperties: Map<string, string>
    Tags: Map<string, string>
} with

    interface IBuilder with
        member this.ResourceId = apiManagement.resourceId this.Name

        member this.BuildResources _ = [
            {
                ApiManagementService.Name = this.Name
                Location = this.Location
                PublisherName = this.PublisherName
                PublisherEmail = this.PublisherEmail
                Sku = this.Sku
                Capacity = this.Capacity
                PublicNetworkAccess = this.PublicNetworkAccess
                VirtualNetworkType = this.VirtualNetworkType
                DisableGateway = this.DisableGateway
                CustomProperties = this.CustomProperties
                Tags = this.Tags
            }
        ]

type ApiManagementBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        Location = Location.WestEurope
        PublisherName = ""
        PublisherEmail = ""
        Sku = ApiManagementSku.Developer
        Capacity = 1
        PublicNetworkAccess = None
        VirtualNetworkType = None
        DisableGateway = None
        CustomProperties = Map.empty
        Tags = Map.empty
    }

    [<CustomOperation "name">]
    member _.Name(state: ApiManagementConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "location">]
    member _.Location(state: ApiManagementConfig, location) = { state with Location = location }

    [<CustomOperation "publisher_name">]
    member _.PublisherName(state: ApiManagementConfig, publisherName) = {
        state with
            PublisherName = publisherName
    }

    [<CustomOperation "publisher_email">]
    member _.PublisherEmail(state: ApiManagementConfig, publisherEmail) = {
        state with
            PublisherEmail = publisherEmail
    }

    [<CustomOperation "sku">]
    member _.Sku(state: ApiManagementConfig, sku) = { state with Sku = sku }

    [<CustomOperation "capacity">]
    member _.Capacity(state: ApiManagementConfig, capacity) = { state with Capacity = capacity }

    [<CustomOperation "public_network_access">]
    member _.PublicNetworkAccess(state: ApiManagementConfig, value) = {
        state with
            PublicNetworkAccess = Some value
    }

    [<CustomOperation "virtual_network_type">]
    member _.VirtualNetworkType(state: ApiManagementConfig, value) = {
        state with
            VirtualNetworkType = Some value
    }

    [<CustomOperation "disable_gateway">]
    member _.DisableGateway(state: ApiManagementConfig) = {
        state with
            DisableGateway = Some true
    }

    [<CustomOperation "custom_property">]
    member _.CustomProperty(state: ApiManagementConfig, key, value) = {
        state with
            CustomProperties = state.CustomProperties.Add(key, value)
    }

    [<CustomOperation "add_tags">]
    member _.AddTags(state: ApiManagementConfig, tags) = {
        state with
            Tags = state.Tags |> Map.merge tags
    }

    interface ITaggable<ApiManagementConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

let apiManagementService = ApiManagementBuilder()

type ApiManagementChildConfig = {
    Service: ResourceName
    ResourceType: string
    Name: ResourceName
    Properties: Map<string, obj>
} with

    interface IBuilder with
        member this.ResourceId =
            ResourceType($"Microsoft.ApiManagement/service/{this.ResourceType}", apiManagement.ApiVersion)
                .resourceId (ResourceName($"{this.Service.Value}/{this.Name.Value}"))

        member this.BuildResources _ = [
            {
                ServiceName = this.Service
                ResourceType = this.ResourceType
                Name = this.Name
                Properties = this.Properties
                Dependencies = []
            }
        ]

type ApiManagementChildBuilder() =
    member _.Yield _ = {
        Service = ResourceName.Empty
        ResourceType = "apis"
        Name = ResourceName.Empty
        Properties = Map.empty
    }

    [<CustomOperation "service">]
    member _.Service(state: ApiManagementChildConfig, service: ResourceName) = { state with Service = service }

    member this.Service(state: ApiManagementChildConfig, service: string) =
        this.Service(state, ResourceName service)

    [<CustomOperation "resource_type">]
    member _.ResourceType(state: ApiManagementChildConfig, resourceType) = {
        state with
            ResourceType = resourceType
    }

    [<CustomOperation "name">]
    member _.Name(state: ApiManagementChildConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "property">]
    member _.Property(state: ApiManagementChildConfig, key, value: obj) = {
        state with
            Properties = state.Properties.Add(key, value)
    }

    member this.Property(state: ApiManagementChildConfig, key, value: string) = this.Property(state, key, box value)
    member this.Property(state: ApiManagementChildConfig, key, value: bool) = this.Property(state, key, box value)
    member this.Property(state: ApiManagementChildConfig, key, value: int) = this.Property(state, key, box value)

let apiManagementChild = ApiManagementChildBuilder()

// Typed builders for the core API Management child resources. The raw child builder above
// is retained only as an escape hatch for new ARM resource types.
let private child service resourceType name properties = {
    ServiceName = service
    ResourceType = resourceType
    Name = name
    Properties = properties
    Dependencies = []
}

let private childWithDependencies service resourceType name dependencies properties = {
    ServiceName = service
    ResourceType = resourceType
    Name = name
    Properties = properties
    Dependencies = dependencies
}

type ApiManagementApiConfig = {
    Service: ResourceName
    Name: ResourceName
    DisplayName: string
    Path: string
    Protocols: string list
    ServiceUrl: string option
    Description: string option
    ApiVersion: string option
    Revision: string
    SubscriptionRequired: bool
} with

    interface IBuilder with
        member this.ResourceId =
            ResourceType("Microsoft.ApiManagement/service/apis", apiManagement.ApiVersion)
                .resourceId (ResourceName($"{this.Service.Value}/{this.Name.Value}"))

        member this.BuildResources _ = [
            child
                this.Service
                "apis"
                (ResourceName($"{this.Name.Value};rev={this.Revision}"))
                (Map.ofList [
                    "displayName", box this.DisplayName
                    "path", box this.Path
                    "protocols", box this.Protocols
                    "serviceUrl", box (this.ServiceUrl |> Option.toObj)
                    "description", box (this.Description |> Option.toObj)
                    "apiVersion", box (this.ApiVersion |> Option.toObj)
                    "subscriptionRequired", box this.SubscriptionRequired
                ])
        ]

type ApiManagementApiBuilder() =
    member _.Yield _ = {
        Service = ResourceName.Empty
        Name = ResourceName.Empty
        DisplayName = ""
        Path = ""
        Protocols = [ "https" ]
        ServiceUrl = None
        Description = None
        ApiVersion = None
        Revision = "1"
        SubscriptionRequired = true
    }

    [<CustomOperation "service">]
    member _.Service(s: ApiManagementApiConfig, v) = { s with Service = v }

    member this.Service(s: ApiManagementApiConfig, v: string) = this.Service(s, ResourceName v)

    [<CustomOperation "name">]
    member _.Name(s: ApiManagementApiConfig, v) = { s with Name = ResourceName v }

    [<CustomOperation "display_name">]
    member _.DisplayName(s: ApiManagementApiConfig, v) = { s with DisplayName = v }

    [<CustomOperation "path">]
    member _.Path(s: ApiManagementApiConfig, v) = { s with Path = v }

    [<CustomOperation "protocols">]
    member _.Protocols(s: ApiManagementApiConfig, v) = { s with Protocols = v }

    [<CustomOperation "service_url">]
    member _.ServiceUrl(s: ApiManagementApiConfig, v) = { s with ServiceUrl = Some v }

    [<CustomOperation "description">]
    member _.Description(s: ApiManagementApiConfig, v) = { s with Description = Some v }

    [<CustomOperation "api_version">]
    member _.ApiVersion(s: ApiManagementApiConfig, v) = { s with ApiVersion = Some v }

    [<CustomOperation "revision">]
    member _.Revision(s: ApiManagementApiConfig, v) = { s with Revision = v }

    [<CustomOperation "subscription_required">]
    member _.SubscriptionRequired(s: ApiManagementApiConfig, v) = { s with SubscriptionRequired = v }

let apiManagementApi = ApiManagementApiBuilder()

type ApiManagementOperationConfig = {
    Service: ResourceName
    Api: ResourceName
    Name: ResourceName
    DisplayName: string
    Method: string
    UrlTemplate: string
    Description: string option
} with

    interface IBuilder with
        member this.ResourceId =
            ResourceType("Microsoft.ApiManagement/service/apis/operations", apiManagement.ApiVersion)
                .resourceId (ResourceName($"{this.Service.Value}/{this.Api.Value}/{this.Name.Value}"))

        member this.BuildResources _ = [
            childWithDependencies
                this.Service
                "apis/operations"
                (ResourceName($"{this.Api.Value}/{this.Name.Value}"))
                [
                    ResourceType("Microsoft.ApiManagement/service/apis", apiManagement.ApiVersion)
                        .resourceId (this.Service, this.Api)
                ]
                (Map.ofList [
                    "displayName", box this.DisplayName
                    "method", box this.Method
                    "urlTemplate", box this.UrlTemplate
                    "description", box (this.Description |> Option.toObj)
                ])
        ]

type ApiManagementOperationBuilder() =
    member _.Yield _ = {
        Service = ResourceName.Empty
        Api = ResourceName.Empty
        Name = ResourceName.Empty
        DisplayName = ""
        Method = "GET"
        UrlTemplate = "/"
        Description = None
    }

    [<CustomOperation "service">]
    member _.Service(s: ApiManagementOperationConfig, v) = { s with Service = v }

    member this.Service(s: ApiManagementOperationConfig, v: string) = this.Service(s, ResourceName v)

    [<CustomOperation "api">]
    member _.Api(s: ApiManagementOperationConfig, v) = { s with Api = v }

    member this.Api(s: ApiManagementOperationConfig, v: string) = this.Api(s, ResourceName v)

    [<CustomOperation "name">]
    member _.Name(s: ApiManagementOperationConfig, v) = { s with Name = ResourceName v }

    [<CustomOperation "display_name">]
    member _.DisplayName(s: ApiManagementOperationConfig, v) = { s with DisplayName = v }

    [<CustomOperation "method">]
    member _.Method(s: ApiManagementOperationConfig, v) = { s with Method = v }

    [<CustomOperation "url_template">]
    member _.UrlTemplate(s: ApiManagementOperationConfig, v) = { s with UrlTemplate = v }

    [<CustomOperation "description">]
    member _.Description(s: ApiManagementOperationConfig, v) = { s with Description = Some v }

let apiManagementOperation = ApiManagementOperationBuilder()

type ApiManagementProductConfig = {
    Service: ResourceName
    Name: ResourceName
    DisplayName: string
    Description: string option
    Terms: string option
    SubscriptionRequired: bool
    ApprovalRequired: bool
    SubscriptionsLimit: int option
    State: string
} with

    interface IBuilder with
        member this.ResourceId =
            ResourceType("Microsoft.ApiManagement/service/products", apiManagement.ApiVersion)
                .resourceId (ResourceName($"{this.Service.Value}/{this.Name.Value}"))

        member this.BuildResources _ = [
            child
                this.Service
                "products"
                this.Name
                (Map.ofList [
                    "displayName", box this.DisplayName
                    "description", box (this.Description |> Option.toObj)
                    "terms", box (this.Terms |> Option.toObj)
                    "subscriptionRequired", box this.SubscriptionRequired
                    if this.SubscriptionRequired then
                        "approvalRequired", box this.ApprovalRequired
                    "subscriptionsLimit", box (this.SubscriptionsLimit |> Option.toNullable)
                    "state", box this.State
                ])
        ]

type ApiManagementProductBuilder() =
    member _.Yield _ = {
        Service = ResourceName.Empty
        Name = ResourceName.Empty
        DisplayName = ""
        Description = None
        Terms = None
        SubscriptionRequired = true
        ApprovalRequired = false
        SubscriptionsLimit = None
        State = "notPublished"
    }

    [<CustomOperation "service">]
    member _.Service(s: ApiManagementProductConfig, v) = { s with Service = v }

    member this.Service(s: ApiManagementProductConfig, v: string) = this.Service(s, ResourceName v)

    [<CustomOperation "name">]
    member _.Name(s: ApiManagementProductConfig, v) = { s with Name = ResourceName v }

    [<CustomOperation "display_name">]
    member _.DisplayName(s: ApiManagementProductConfig, v) = { s with DisplayName = v }

    [<CustomOperation "description">]
    member _.Description(s: ApiManagementProductConfig, v) = { s with Description = Some v }

    [<CustomOperation "terms">]
    member _.Terms(s: ApiManagementProductConfig, v) = { s with Terms = Some v }

    [<CustomOperation "subscription_required">]
    member _.SubscriptionRequired(s: ApiManagementProductConfig, v) = { s with SubscriptionRequired = v }

    [<CustomOperation "approval_required">]
    member _.ApprovalRequired(s: ApiManagementProductConfig, v) = { s with ApprovalRequired = v }

    [<CustomOperation "subscriptions_limit">]
    member _.SubscriptionsLimit(s: ApiManagementProductConfig, v) = { s with SubscriptionsLimit = Some v }

    [<CustomOperation "state">]
    member _.State(s: ApiManagementProductConfig, v) = { s with State = v }

let apiManagementProduct = ApiManagementProductBuilder()

type ApiManagementBackendConfig = {
    Service: ResourceName
    Name: ResourceName
    Url: string
    Protocol: string
    Title: string option
    Description: string option
    ResourceId: string option
} with

    interface IBuilder with
        member this.ResourceId =
            ResourceType("Microsoft.ApiManagement/service/backends", apiManagement.ApiVersion)
                .resourceId (ResourceName($"{this.Service.Value}/{this.Name.Value}"))

        member this.BuildResources _ = [
            child
                this.Service
                "backends"
                this.Name
                (Map.ofList [
                    "url", box this.Url
                    "protocol", box this.Protocol
                    "title", box (this.Title |> Option.toObj)
                    "description", box (this.Description |> Option.toObj)
                    "resourceId", box (this.ResourceId |> Option.toObj)
                ])
        ]

type ApiManagementBackendBuilder() =
    member _.Yield _ = {
        Service = ResourceName.Empty
        Name = ResourceName.Empty
        Url = ""
        Protocol = "http"
        Title = None
        Description = None
        ResourceId = None
    }

    [<CustomOperation "service">]
    member _.Service(s: ApiManagementBackendConfig, v) = { s with Service = v }

    member this.Service(s: ApiManagementBackendConfig, v: string) = this.Service(s, ResourceName v)

    [<CustomOperation "name">]
    member _.Name(s: ApiManagementBackendConfig, v) = { s with Name = ResourceName v }

    [<CustomOperation "url">]
    member _.Url(s: ApiManagementBackendConfig, v) = { s with Url = v }

    [<CustomOperation "protocol">]
    member _.Protocol(s: ApiManagementBackendConfig, v) = { s with Protocol = v }

    [<CustomOperation "title">]
    member _.Title(s: ApiManagementBackendConfig, v) = { s with Title = Some v }

    [<CustomOperation "description">]
    member _.Description(s: ApiManagementBackendConfig, v) = { s with Description = Some v }

    [<CustomOperation "resource_id">]
    member _.ResourceId(s: ApiManagementBackendConfig, v) = { s with ResourceId = Some v }

let apiManagementBackend = ApiManagementBackendBuilder()

type ApiManagementNamedValueConfig = {
    Service: ResourceName
    Name: ResourceName
    DisplayName: string
    Value: string
    Secret: bool
    Tags: string list
} with

    interface IBuilder with
        member this.ResourceId =
            ResourceType("Microsoft.ApiManagement/service/namedValues", apiManagement.ApiVersion)
                .resourceId (ResourceName($"{this.Service.Value}/{this.Name.Value}"))

        member this.BuildResources _ = [
            child
                this.Service
                "namedValues"
                this.Name
                (Map.ofList [
                    "displayName", box this.DisplayName
                    "value", box this.Value
                    "secret", box this.Secret
                    "tags", box (String.concat "," this.Tags)
                ])
        ]

type ApiManagementNamedValueBuilder() =
    member _.Yield _ = {
        Service = ResourceName.Empty
        Name = ResourceName.Empty
        DisplayName = ""
        Value = ""
        Secret = false
        Tags = []
    }

    [<CustomOperation "service">]
    member _.Service(s: ApiManagementNamedValueConfig, v) = { s with Service = v }

    member this.Service(s: ApiManagementNamedValueConfig, v: string) = this.Service(s, ResourceName v)

    [<CustomOperation "name">]
    member _.Name(s: ApiManagementNamedValueConfig, v) = { s with Name = ResourceName v }

    [<CustomOperation "display_name">]
    member _.DisplayName(s: ApiManagementNamedValueConfig, v) = { s with DisplayName = v }

    [<CustomOperation "value">]
    member _.Value(s: ApiManagementNamedValueConfig, v) = { s with Value = v }

    [<CustomOperation "secret">]
    member _.Secret(s: ApiManagementNamedValueConfig) = { s with Secret = true }

    [<CustomOperation "tags">]
    member _.Tags(s: ApiManagementNamedValueConfig, v) = { s with Tags = v }

let apiManagementNamedValue = ApiManagementNamedValueBuilder()

type ApiManagementPolicyConfig = {
    Service: ResourceName
    Scope: string
    Parent: ResourceName option
    Name: ResourceName
    Format: string
    Xml: string
} with

    interface IBuilder with
        member this.ResourceId =
            let name =
                this.Parent
                |> Option.map (fun parent -> parent / this.Name)
                |> Option.defaultValue this.Name

            ResourceType($"Microsoft.ApiManagement/service/{this.Scope}/policies", apiManagement.ApiVersion)
                .resourceId (ResourceName($"{this.Service.Value}/{name.Value}"))

        member this.BuildResources _ = [
            let name =
                this.Parent
                |> Option.map (fun parent -> parent / this.Name)
                |> Option.defaultValue this.Name

            child
                this.Service
                ($"{this.Scope}/policies")
                name
                (Map.ofList [ "format", box this.Format; "value", box this.Xml ])
        ]

type ApiManagementPolicyBuilder() =
    member _.Yield _ = {
        Service = ResourceName.Empty
        Scope = "service"
        Parent = None
        Name = ResourceName "policy"
        Format = "rawxml"
        Xml = "<policies />"
    }

    [<CustomOperation "service">]
    member _.Service(s: ApiManagementPolicyConfig, v) = { s with Service = v }

    member this.Service(s: ApiManagementPolicyConfig, v: string) = this.Service(s, ResourceName v)

    [<CustomOperation "scope">]
    member _.Scope(s: ApiManagementPolicyConfig, v) = { s with Scope = v }

    [<CustomOperation "api">]
    member _.Api(s: ApiManagementPolicyConfig, v) = { s with Parent = Some v }

    member this.Api(s: ApiManagementPolicyConfig, v: string) = this.Api(s, ResourceName v)

    [<CustomOperation "name">]
    member _.Name(s: ApiManagementPolicyConfig, v) = { s with Name = ResourceName v }

    [<CustomOperation "format">]
    member _.Format(s: ApiManagementPolicyConfig, v) = { s with Format = v }

    [<CustomOperation "xml">]
    member _.Xml(s: ApiManagementPolicyConfig, v) = { s with Xml = v }

let apiManagementPolicy = ApiManagementPolicyBuilder()
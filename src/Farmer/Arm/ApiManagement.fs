[<AutoOpen>]
module Farmer.Arm.ApiManagement

open Farmer

let apiManagement = ResourceType("Microsoft.ApiManagement/service", "2021-08-01")

[<RequireQualifiedAccess>]
type ApiManagementSku =
    | Consumption
    | Developer
    | Basic
    | Standard
    | Premium

    member this.ArmValue =
        match this with
        | Consumption -> "Consumption"
        | Developer -> "Developer"
        | Basic -> "Basic"
        | Standard -> "Standard"
        | Premium -> "Premium"

type ApiManagementService = {
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

    interface IArmResource with
        member this.ResourceId = apiManagement.resourceId this.Name

        member this.JsonModel = {|
            apiManagement.Create(this.Name, this.Location, tags = this.Tags) with
                sku = {|
                    name = this.Sku.ArmValue
                    capacity = this.Capacity
                |}
                properties = {|
                    publisherName = this.PublisherName
                    publisherEmail = this.PublisherEmail
                    publicNetworkAccess = this.PublicNetworkAccess |> Option.toObj
                    virtualNetworkType = this.VirtualNetworkType |> Option.toObj
                    disableGateway = this.DisableGateway |> Option.toNullable
                    customProperties = this.CustomProperties
                |}
        |}

/// A child resource under an API Management service. This covers the ARM child resource
/// surface (apis, operations, products, policies, backends, namedValues, loggers,
/// diagnostics, subscriptions, groups, tags, and the remaining service children).
type ApiManagementChild = {
    ServiceName: ResourceName
    ResourceType: string
    Name: ResourceName
    Properties: Map<string, obj>
    Dependencies: ResourceId list
} with

    interface IArmResource with
        member this.ResourceId =
            ResourceType($"Microsoft.ApiManagement/service/{this.ResourceType}", apiManagement.ApiVersion)
                .resourceId (ResourceName($"{this.ServiceName.Value}/{this.Name.Value}"))

        member this.JsonModel =
            let resourceType =
                ResourceType($"Microsoft.ApiManagement/service/{this.ResourceType}", apiManagement.ApiVersion)

            {|
                resourceType.Create(
                    ResourceName($"{this.ServiceName.Value}/{this.Name.Value}"),
                    dependsOn = apiManagement.resourceId this.ServiceName :: this.Dependencies
                ) with
                    properties = this.Properties
            |}
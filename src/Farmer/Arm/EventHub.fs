[<AutoOpen>]
module Farmer.Arm.EventHub

open Farmer
open Farmer.EventHub

let namespaces = ResourceType("Microsoft.EventHub/namespaces", "2017-04-01")

type CaptureDestination = StorageAccount of ResourceName * containerName: string

type Namespace = {
    Name: ResourceName
    Location: Location
    Sku: {| Name: EventHubSku; Capacity: int |}
    ZoneRedundant: bool option
    AutoInflateSettings: InflateSetting option
    Tags: Map<string, string>
} with

    member this.MaxThroughputUnits =
        this.AutoInflateSettings
        |> Option.bind (function
            | AutoInflate throughput -> Some throughput
            | ManualInflate -> None)

    interface IArmResource with
        member this.ResourceId = namespaces.resourceId this.Name

        member this.JsonModel = {|
            namespaces.Create(this.Name, this.Location, tags = this.Tags) with
                sku = {|
                    name = string this.Sku.Name
                    tier = string this.Sku.Name
                    capacity = this.Sku.Capacity
                |}
                properties = {|
                    zoneRedundant = this.ZoneRedundant |> Option.toNullable
                    isAutoInflateEnabled =
                        this.AutoInflateSettings
                        |> Option.map (function
                            | AutoInflate _ -> true
                            | ManualInflate -> false)
                        |> Option.toNullable
                    maximumThroughputUnits = this.MaxThroughputUnits |> Option.toNullable
                |}
        |}

module Namespaces =
    let eventHubs =
        ResourceType("Microsoft.EventHub/namespaces/eventhubs", "2017-04-01")

    let authorizationRules =
        ResourceType("Microsoft.EventHub/namespaces/AuthorizationRules", "2017-04-01")

    type EventHub = {
        Name: ResourceName
        Location: Location
        MessageRetentionDays: int option
        Partitions: int
        Dependencies: ResourceId Set
        CaptureDestination: CaptureDestination option
        Tags: Map<string, string>
    } with

        interface IArmResource with
            member this.ResourceId = eventHubs.resourceId this.Name

            member this.JsonModel = {|
                eventHubs.Create(this.Name, this.Location, this.Dependencies, this.Tags) with
                    properties = {|
                        messageRetentionInDays = this.MessageRetentionDays |> Option.toNullable
                        partitionCount = this.Partitions
                        status = "Active"
                        captureDescription =
                            match this.CaptureDestination with
                            | Some(StorageAccount(name, container)) ->
                                {|
                                    enabled = true
                                    encoding = "Avro"
                                    destination = {|
                                        name = "EventHubArchive.AzureBlockBlob"
                                        properties = {|
                                            storageAccountResourceId = storageAccounts.resourceId(name).Eval()
                                            blobContainer = container
                                        |}
                                    |}
                                |}
                                |> box
                            | None -> null
                    |}
            |}

    module EventHubs =
        let authorizationRules =
            ResourceType("Microsoft.EventHub/namespaces/eventhubs/AuthorizationRules", "2017-04-01")

        let consumerGroups =
            ResourceType("Microsoft.EventHub/namespaces/eventhubs/consumergroups", "2017-04-01")

        type ConsumerGroup = {
            ConsumerGroupName: ResourceName
            EventHub: ResourceName
            Location: Location
            Dependencies: ResourceId list
        } with

            interface IArmResource with
                member this.ResourceId =
                    consumerGroups.resourceId (this.EventHub / this.ConsumerGroupName)

                member this.JsonModel =
                    consumerGroups.Create(this.EventHub / this.ConsumerGroupName, this.Location, this.Dependencies)

        type AuthorizationRule = {
            Name: ResourceName
            Location: Location
            Dependencies: ResourceId list
            Rights: AuthorizationRuleRight Set
        } with

            interface IArmResource with
                member this.ResourceId = authorizationRules.resourceId this.Name

                member this.JsonModel = {|
                    authorizationRules.Create(this.Name, this.Location, this.Dependencies) with
                        properties = {|
                            rights = this.Rights |> Set.map string |> Set.toList
                        |}
                |}

[<AutoOpen>]
module Farmer.Arm.ServiceBus

open Farmer
open Farmer.ServiceBus
open System

let private tryGetIso (v: IsoDateTime option) =
    v |> Option.map (fun v -> v.Value) |> Option.toObj

let subscriptions =
    ResourceType("Microsoft.ServiceBus/namespaces/topics/subscriptions", "2022-01-01-preview")

let queues =
    ResourceType("Microsoft.ServiceBus/namespaces/queues", "2022-01-01-preview")

let topics =
    ResourceType("Microsoft.ServiceBus/namespaces/topics", "2022-01-01-preview")

let namespaces =
    ResourceType("Microsoft.ServiceBus/namespaces", "2022-01-01-preview")

let queueAuthorizationRules =
    ResourceType("Microsoft.ServiceBus/namespaces/queues/authorizationRules", "2022-01-01-preview")

let namespaceAuthorizationRules =
    ResourceType("Microsoft.ServiceBus/namespaces/AuthorizationRules", "2022-01-01-preview")

module Namespaces =
    module Topics =
        let rules = ResourceType("Rules", "2022-01-01-preview")

        type Subscription =
            {
                Name: ResourceName
                Topic: LinkedResource
                LockDuration: IsoDateTime option
                DuplicateDetectionHistoryTimeWindow: IsoDateTime option
                DefaultMessageTimeToLive: IsoDateTime option
                ForwardTo: ResourceName option
                MaxDeliveryCount: int option
                Session: bool option
                DeadLetteringOnMessageExpiration: bool option
                Rules: Rule list
                DependsOn: Set<ResourceId>
            }

            member private this.ResourceName =
                this.Topic.Name / this.Topic.ResourceId.Segments.[0] / this.Name

            interface IArmResource with
                member this.ResourceId = subscriptions.resourceId (this.ResourceName)

                member this.JsonModel =
                    {| subscriptions.Create(
                           this.ResourceName,
                           dependsOn = LinkedResource.addToSetIfManaged this.Topic this.DependsOn
                       ) with
                        properties =
                            {|
                                defaultMessageTimeToLive = tryGetIso this.DefaultMessageTimeToLive
                                requiresDuplicateDetection =
                                    match this.DuplicateDetectionHistoryTimeWindow with
                                    | Some _ -> Nullable true
                                    | None -> Nullable()
                                duplicateDetectionHistoryTimeWindow = tryGetIso this.DuplicateDetectionHistoryTimeWindow
                                deadLetteringOnMessageExpiration =
                                    this.DeadLetteringOnMessageExpiration |> Option.toNullable
                                forwardTo = this.ForwardTo |> Option.map (fun n -> n.Value) |> Option.toObj
                                maxDeliveryCount = this.MaxDeliveryCount |> Option.toNullable
                                requiresSession = this.Session |> Option.toNullable
                                lockDuration = tryGetIso this.LockDuration
                            |}
                        resources =
                            [
                                for rule in this.Rules do
                                    {| rules.Create(
                                           rule.Name,
                                           dependsOn = [ ResourceId.create (ResourceType("", ""), this.Name) ]
                                       ) with
                                        properties =
                                            match rule with
                                            | SqlFilter (_, expression) ->
                                                {|
                                                    filterType = "SqlFilter"
                                                    sqlFilter = box {| sqlExpression = expression |}
                                                    correlationFilter = null
                                                |}
                                            | CorrelationFilter (_, correlationId, properties) ->
                                                {|
                                                    filterType = "CorrelationFilter"
                                                    correlationFilter =
                                                        box
                                                            {|
                                                                correlationId = correlationId |> Option.toObj
                                                                properties = properties
                                                            |}
                                                    sqlFilter = null
                                                |}
                                    |}
                            ]
                    |}

    type Queue =
        {
            Name: ResourceName
            Namespace: LinkedResource
            LockDuration: IsoDateTime option
            DuplicateDetectionHistoryTimeWindow: IsoDateTime option
            Session: bool option
            DeadLetteringOnMessageExpiration: bool option
            DefaultMessageTimeToLive: IsoDateTime option
            ForwardTo: ResourceName option
            MaxDeliveryCount: int option
            MaxSizeInMegabytes: int<Mb> option
            EnablePartitioning: bool option
        }

        member private this.ResourceName = this.Namespace.Name / this.Name

        interface IArmResource with
            member this.ResourceId = queues.resourceId (this.ResourceName)

            member this.JsonModel =
                {| queues.Create(
                       this.ResourceName,
                       dependsOn = (LinkedResource.addToSetIfManaged this.Namespace Set.empty)
                   ) with
                    properties =
                        {|
                            lockDuration = tryGetIso this.LockDuration
                            requiresDuplicateDetection =
                                match this.DuplicateDetectionHistoryTimeWindow with
                                | Some _ -> Nullable true
                                | None -> Nullable()
                            duplicateDetectionHistoryTimeWindow = tryGetIso this.DuplicateDetectionHistoryTimeWindow
                            defaultMessageTimeToLive = tryGetIso this.DefaultMessageTimeToLive
                            requiresSession = this.Session |> Option.toNullable
                            deadLetteringOnMessageExpiration =
                                this.DeadLetteringOnMessageExpiration |> Option.toNullable
                            forwardTo = this.ForwardTo |> Option.map (fun x -> x.Value) |> Option.toObj
                            maxDeliveryCount = this.MaxDeliveryCount |> Option.toNullable
                            maxSizeInMegabytes = this.MaxSizeInMegabytes |> Option.toNullable
                            enablePartitioning = this.EnablePartitioning |> Option.toNullable
                        |}
                |}

    type QueueAuthorizationRule =
        {
            Name: ResourceName
            Location: Location
            Dependencies: ResourceId list
            Rights: AuthorizationRuleRight Set
        }

        interface IArmResource with
            member this.ResourceId = queueAuthorizationRules.resourceId this.Name

            member this.JsonModel =
                {| queueAuthorizationRules.Create(this.Name, this.Location, this.Dependencies) with
                    properties =
                        {|
                            rights = this.Rights |> Set.map string |> Set.toList
                        |}
                |}

    type NamespaceAuthorizationRule =
        {
            Name: ResourceName
            Location: Location
            Dependencies: ResourceId list
            Rights: AuthorizationRuleRight Set
        }

        interface IArmResource with
            member this.ResourceId = namespaceAuthorizationRules.resourceId this.Name

            member this.JsonModel =
                {| namespaceAuthorizationRules.Create(this.Name, this.Location, this.Dependencies) with
                    properties =
                        {|
                            rights = this.Rights |> Set.map string |> Set.toList
                        |}
                |}

    type Topic =
        {
            Name: ResourceName
            Dependencies: ResourceId Set
            Namespace: ResourceId
            DuplicateDetectionHistoryTimeWindow: IsoDateTime option
            DefaultMessageTimeToLive: IsoDateTime option
            EnablePartitioning: bool option
            MaxMessageSizeInKilobytes: int<Kb> option
            MaxSizeInMegabytes: int<Mb> option
        }

        interface IArmResource with
            member this.ResourceId = topics.resourceId (this.Namespace.Name, this.Name)

            member this.JsonModel =
                {| topics.Create(this.Namespace.Name / this.Name, dependsOn = this.Dependencies) with
                    properties =
                        {|
                            defaultMessageTimeToLive = tryGetIso this.DefaultMessageTimeToLive
                            requiresDuplicateDetection =
                                match this.DuplicateDetectionHistoryTimeWindow with
                                | Some _ -> Nullable true
                                | None -> Nullable()
                            duplicateDetectionHistoryTimeWindow = tryGetIso this.DuplicateDetectionHistoryTimeWindow
                            enablePartitioning = this.EnablePartitioning |> Option.toNullable
                            maxMessageSizeInKilobytes = this.MaxMessageSizeInKilobytes |> Option.toNullable
                            maxSizeInMegabytes = this.MaxSizeInMegabytes |> Option.toNullable
                        |}
                |}

type Namespace =
    {
        Name: ResourceName
        Location: Location
        Sku: Sku
        Dependencies: ResourceId Set
        ZoneRedundant: FeatureFlag option
        DisablePublicNetworkAccess: FeatureFlag option
        MinTlsVersion: TlsVersion option
        Tags: Map<string, string>
    }

    member this.Capacity =
        match this.Sku with
        | Basic -> None
        | Standard -> None
        | Premium OneUnit -> Some 1
        | Premium TwoUnits -> Some 2
        | Premium FourUnits -> Some 4

    interface IArmResource with
        member this.ResourceId = namespaces.resourceId this.Name

        member this.JsonModel =
            {| namespaces.Create(this.Name, this.Location, this.Dependencies, this.Tags) with
                sku =
                    {|
                        name = this.Sku.NameArmValue
                        tier = this.Sku.TierArmValue
                        capacity = this.Capacity |> Option.toNullable
                    |}
                properties =
                    {|
                        minimumTlsVersion =
                            match this.MinTlsVersion with
                            | Some Tls10 -> "1.0"
                            | Some Tls11 -> "1.1"
                            | Some Tls12 -> "1.2"
                            | None -> null
                        publicNetworkAccess =
                            match this.DisablePublicNetworkAccess with
                            | Some FeatureFlag.Enabled -> "Disabled"
                            | Some FeatureFlag.Disabled -> "Enabled"
                            | None -> null
                        zoneRedundant =
                            match this.ZoneRedundant with
                            | Some FeatureFlag.Enabled -> "true"
                            | Some FeatureFlag.Disabled -> "false"
                            | None -> null
                    |}
            |}

[<AutoOpen>]
module Farmer.Arm.ServiceBus

open Farmer
open Farmer.ServiceBus
open System

let private tryGetIso (v:IsoDateTime option) = v |> Option.map(fun v -> v.Value) |> Option.toObj

let subscriptions = ResourceType ("Microsoft.ServiceBus/namespaces/topics/subscriptions", "2017-04-01")
let queues = ResourceType ("Microsoft.ServiceBus/namespaces/queues", "2017-04-01")
let topics = ResourceType ("Microsoft.ServiceBus/namespaces/topics", "2017-04-01")
let namespaces = ResourceType ("Microsoft.ServiceBus/namespaces", "2017-04-01")
let queueAuthorizationRules = ResourceType ("Microsoft.ServiceBus/namespaces/queues/authorizationRules", "2017-04-01")
let namespaceAuthorizationRules = ResourceType ("Microsoft.ServiceBus/namespaces/AuthorizationRules", "2017-04-01")

module Namespaces =
    module Topics =
        let rules = ResourceType ("Rules", "2017-04-01")
        type Subscription =
            { Name : ResourceName
              Namespace : ResourceName
              Topic : ResourceName
              LockDuration : IsoDateTime option
              DuplicateDetectionHistoryTimeWindow : IsoDateTime option
              DefaultMessageTimeToLive : IsoDateTime option
              ForwardTo : ResourceName option
              MaxDeliveryCount : int option
              Session : bool option
              DeadLetteringOnMessageExpiration : bool option
              Rules : Rule list }
            interface IArmResource with
                member this.ResourceId = subscriptions.resourceId (this.Namespace/this.Topic/this.Name)
                member this.JsonModel =
                    {| subscriptions.Create(this.Namespace/this.Topic/this.Name, dependsOn = [ topics.resourceId(this.Namespace, this.Topic) ]) with
                        properties =
                         {| defaultMessageTimeToLive = tryGetIso this.DefaultMessageTimeToLive
                            requiresDuplicateDetection =
                                match this.DuplicateDetectionHistoryTimeWindow with
                                | Some _ -> Nullable true
                                | None -> Nullable()
                            duplicateDetectionHistoryTimeWindow = tryGetIso this.DuplicateDetectionHistoryTimeWindow
                            deadLetteringOnMessageExpiration = this.DeadLetteringOnMessageExpiration |> Option.toNullable
                            forwardTo = this.ForwardTo |> Option.map (fun n -> n.Value) |> Option.toObj
                            maxDeliveryCount = this.MaxDeliveryCount |> Option.toNullable
                            requiresSession = this.Session |> Option.toNullable
                            lockDuration = tryGetIso this.LockDuration
                         |}
                        resources = [
                         for rule in this.Rules do
                            {| rules.Create(rule.Name, dependsOn = [ ResourceId.create (ResourceType("", ""), this.Name)]) with
                                properties =
                                 match rule with
                                 | SqlFilter (_, expression) ->
                                     {| filterType = "SqlFilter"
                                        sqlFilter = box {| sqlExpression = expression |}
                                        correlationFilter = null |}
                                 | CorrelationFilter (_, correlationId, properties) ->
                                     {| filterType = "CorrelationFilter"
                                        correlationFilter =
                                            box {| correlationId = correlationId |> Option.toObj
                                                   properties = properties |}
                                        sqlFilter = null |}
                            |}
                        ]
                    |} :> _

    type Queue =
        { Name : ResourceName
          Namespace : ResourceName
          LockDuration : IsoDateTime option
          DuplicateDetectionHistoryTimeWindow : IsoDateTime option
          Session : bool option
          DeadLetteringOnMessageExpiration : bool option
          DefaultMessageTimeToLive : IsoDateTime
          MaxDeliveryCount : int option
          MaxSizeInMegabytes : int<Mb> option
          EnablePartitioning : bool option}
        interface IArmResource with
            member this.ResourceId = queues.resourceId (this.Namespace/this.Name)
            member this.JsonModel =
                {| queues.Create(this.Namespace/this.Name, dependsOn = [ namespaces.resourceId this.Namespace ]) with
                    properties =
                     {| lockDuration = tryGetIso this.LockDuration
                        requiresDuplicateDetection =
                            match this.DuplicateDetectionHistoryTimeWindow with
                            | Some _ -> Nullable true
                            | None -> Nullable()
                        duplicateDetectionHistoryTimeWindow = tryGetIso this.DuplicateDetectionHistoryTimeWindow
                        defaultMessageTimeToLive = this.DefaultMessageTimeToLive.Value
                        requiresSession = this.Session |> Option.toNullable
                        deadLetteringOnMessageExpiration = this.DeadLetteringOnMessageExpiration |> Option.toNullable
                        maxDeliveryCount = this.MaxDeliveryCount |> Option.toNullable
                        maxSizeInMegabytes = this.MaxSizeInMegabytes |> Option.toNullable
                        enablePartitioning = this.EnablePartitioning |> Option.toNullable |}
                |} :> _
    type QueueAuthorizationRule =
        { Name : ResourceName
          Location : Location
          Dependencies : ResourceId list
          Rights : AuthorizationRuleRight Set }
        interface IArmResource with
            member this.ResourceId = queueAuthorizationRules.resourceId this.Name
            member this.JsonModel =
                {| queueAuthorizationRules.Create(this.Name, this.Location, this.Dependencies) with
                    properties = {| rights = this.Rights |> Set.map string |> Set.toList |}
                |} :> _

    type NamespaceAuthorizationRule =
        { Name : ResourceName
          Location : Location
          Dependencies : ResourceId list
          Rights : AuthorizationRuleRight Set }
        interface IArmResource with
            member this.ResourceId = namespaceAuthorizationRules.resourceId this.Name
            member this.JsonModel =
                {| namespaceAuthorizationRules.Create(this.Name, this.Location, this.Dependencies) with
                    properties = {| rights = this.Rights |> Set.map string |> Set.toList |}
                |} :> _

    type Topic =
        { Name : ResourceName
          Dependencies : ResourceId Set
          Namespace : ResourceId
          DuplicateDetectionHistoryTimeWindow : IsoDateTime option
          DefaultMessageTimeToLive : IsoDateTime option
          EnablePartitioning : bool option
          MaxSizeInMegabytes : int<Mb> option }
        interface IArmResource with
            member this.ResourceId = topics.resourceId (this.Namespace.Name, this.Name)
            member this.JsonModel =
                {| topics.Create(this.Namespace.Name/this.Name, dependsOn = this.Dependencies) with
                    properties =
                        {| defaultMessageTimeToLive = tryGetIso this.DefaultMessageTimeToLive
                           requiresDuplicateDetection =
                               match this.DuplicateDetectionHistoryTimeWindow with
                               | Some _ -> Nullable true
                               | None -> Nullable()
                           duplicateDetectionHistoryTimeWindow = tryGetIso this.DuplicateDetectionHistoryTimeWindow
                           enablePartitioning = this.EnablePartitioning |> Option.toNullable
                           maxSizeInMegabytes = this.MaxSizeInMegabytes |> Option.toNullable |}
                |} :> _

type Namespace =
    { Name : ResourceName
      Location : Location
      Sku : Sku
      Dependencies : ResourceId Set
      Tags: Map<string,string>  }
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
                     {| name = this.Sku.NameArmValue
                        tier = this.Sku.TierArmValue
                        capacity = this.Capacity |> Option.toNullable |}
            |} :> _
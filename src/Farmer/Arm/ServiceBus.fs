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
          EnablePartitioning : bool option }
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
                        enablePartitioning = this.EnablePartitioning |> Option.toNullable |}
                |} :> _

    type Topic =
        { Name : ResourceName
          Namespace : ResourceName
          DuplicateDetectionHistoryTimeWindow : IsoDateTime option
          DefaultMessageTimeToLive : IsoDateTime option
          EnablePartitioning : bool option }
        interface IArmResource with
            member this.ResourceId = topics.resourceId (this.Namespace/this.Name)
            member this.JsonModel =
                {| topics.Create(this.Namespace/this.Name, dependsOn = [ namespaces.resourceId this.Namespace ]) with
                    properties =
                        {| defaultMessageTimeToLive = tryGetIso this.DefaultMessageTimeToLive
                           requiresDuplicateDetection =
                               match this.DuplicateDetectionHistoryTimeWindow with
                               | Some _ -> Nullable true
                               | None -> Nullable()
                           duplicateDetectionHistoryTimeWindow = tryGetIso this.DuplicateDetectionHistoryTimeWindow
                           enablePartitioning = this.EnablePartitioning |> Option.toNullable |}
                |} :> _

module private Sku =
    let name (sku:Sku) =
        match sku with
        | Basic -> "Basic"
        | Standard -> "Standard"
        | Premium OneUnit 
        | Premium TwoUnits
        | Premium FourUnits -> "Premium"
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
                     {| name = Sku.name this.Sku
                        tier = Sku.name this.Sku
                        capacity = this.Capacity |> Option.toNullable |}
            |} :> _
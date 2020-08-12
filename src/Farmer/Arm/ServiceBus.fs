[<AutoOpen>]
module Farmer.Arm.ServiceBus

open Farmer
open Farmer.CoreTypes
open Farmer.ServiceBus
open System

let private tryGetIso (v:IsoDateTime option) = v |> Option.map(fun v -> v.Value) |> Option.toObj

let subscriptions = ResourceType "Microsoft.ServiceBus/namespaces/topics/subscriptions"
let queues = ResourceType "Microsoft.ServiceBus/namespaces/queues"
let topics = ResourceType "Microsoft.ServiceBus/namespaces/topics"
let namespaces = ResourceType "Microsoft.ServiceBus/namespaces"

module Namespaces =
    module Topics =
        type CorrelationFilter =
            { CorrelationId : string option
              Properties : Map<string, obj> option }
        type Rule =
            | SqlFilter of Name:ResourceName * SqlExpression:string
            | CorrelationFilter of Name:ResourceName * CorrelationFilter
            member this.Name =
                match this with
                | SqlFilter (name, _) -> name
                | CorrelationFilter (name, _) -> name
        let correlation_property_filter (name:string) (properties:(string * string) seq) =
            let downcastProperties =
                properties
                |> Seq.map (fun (k,v) -> k, v :> obj)
                |> Map.ofSeq
                |> Some
            Rule.CorrelationFilter (ResourceName name, { CorrelationId = None; Properties = downcastProperties })
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
                member this.ResourceName = this.Name
                member this.JsonModel =
                    {| apiVersion = "2017-04-01"
                       name = this.Namespace.Value + "/" + this.Topic.Value + "/" + this.Name.Value
                       ``type`` = subscriptions.ArmValue
                       dependsOn = [ this.Topic.Value ]
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
                       resources = this.Rules |> List.map ( fun rule ->
                           {| apiVersion = "2017-04-01"
                              name = rule.Name.Value
                              ``type`` = "rules"
                              dependsOn = [ this.Name.Value ]
                              properties =
                               match rule with
                               | SqlFilter (_, sqlExpression) -> 
                                   {| filterType = "SqlFilter"
                                      sqlFilter = {| sqlExpression = sqlExpression |}
                                      correlationFilter = Unchecked.defaultof<_>
                                    |}
                               | CorrelationFilter (_, correlationFilter) -> 
                                   {| filterType = "CorrelationFilter"
                                      correlationFilter =
                                          {| correlationId = correlationFilter.CorrelationId |> Option.toObj
                                             properties =
                                                 correlationFilter.Properties
                                                 |> Option.map (fun m -> m |> Map.toSeq |> dict)
                                                 |> Option.toObj |}
                                      sqlFilter = Unchecked.defaultof<_> |}
                           |})
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
            member this.ResourceName = this.Name
            member this.JsonModel =
                {| apiVersion = "2017-04-01"
                   name = this.Namespace.Value + "/" + this.Name.Value
                   ``type`` = queues.ArmValue
                   dependsOn = [ this.Namespace.Value ]
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
            member this.ResourceName = this.Name
            member this.JsonModel =
                {| apiVersion = "2017-04-01"
                   name = this.Namespace.Value + "/" + this.Name.Value
                   ``type`` = topics.ArmValue
                   dependsOn = [ this.Namespace.Value ]
                   properties =
                       {| defaultMessageTimeToLive = tryGetIso this.DefaultMessageTimeToLive
                          requiresDuplicateDetection =
                              match this.DuplicateDetectionHistoryTimeWindow with
                              | Some _ -> Nullable true
                              | None -> Nullable()
                          duplicateDetectionHistoryTimeWindow = tryGetIso this.DuplicateDetectionHistoryTimeWindow
                          enablePartitioning = this.EnablePartitioning |> Option.toNullable |}
                |} :> _

type Namespace =
    { Name : ResourceName
      Location : Location
      Sku : Sku
      DependsOn : ResourceName list
      Tags: Map<string,string>  }
    member this.Capacity =
        match this.Sku with
        | Basic -> None
        | Standard -> None
        | Premium OneUnit -> Some 1
        | Premium TwoUnits -> Some 2
        | Premium FourUnits -> Some 4
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| apiVersion = "2017-04-01"
               ``type`` = namespaces.ArmValue
               name = this.Name.Value
               location = this.Location.ArmValue
               sku =
                    {| name = string this.Sku
                       tier = string this.Sku
                       capacity = this.Capacity |> Option.toNullable |}
               dependsOn = this.DependsOn |> List.map (fun r -> r.Value)
               tags = this.Tags
            |} :> _
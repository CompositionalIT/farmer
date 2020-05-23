[<AutoOpen>]
module Farmer.Arm.ServiceBus

open Farmer
open Farmer.CoreTypes
open Farmer.ServiceBus
open System

let private tryGetIso (v:IsoDateTime option) = v |> Option.map(fun v -> v.Value) |> Option.toObj

module Namespaces =
    module Topics =
        type Subscription =
            { Name : ResourceName
              LockDuration : IsoDateTime option
              DuplicateDetectionHistoryTimeWindow : IsoDateTime option
              DefaultMessageTimeToLive : IsoDateTime option
              MaxDeliveryCount : int option
              Session : bool option
              DeadLetteringOnMessageExpiration : bool option }

    type Queue =
        { Name : ResourceName
          LockDuration : IsoDateTime option
          DuplicateDetectionHistoryTimeWindow : IsoDateTime option
          Session : bool option
          DeadLetteringOnMessageExpiration : bool option
          DefaultMessageTimeToLive : IsoDateTime
          MaxDeliveryCount : int option
          EnablePartitioning : bool option }

    type Topic =
        { Name : ResourceName
          DuplicateDetectionHistoryTimeWindow : IsoDateTime option
          DefaultMessageTimeToLive : IsoDateTime option
          EnablePartitioning : bool option
          Subscriptions : Topics.Subscription list }

type Namespace =
    { Name : ResourceName
      Location : Location
      Sku : Sku
      Queues : Namespaces.Queue list
      Topics : Namespaces.Topic list
      DependsOn : ResourceName list }
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
            {| ``type`` = "Microsoft.ServiceBus/namespaces"
               apiVersion = "2017-04-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               sku =
                    {| name = string this.Sku
                       tier = string this.Sku
                       capacity = this.Capacity |> Option.toNullable |}
               dependsOn = this.DependsOn |> List.map (fun r -> r.Value)
               resources =
                [ for queue in this.Queues do
                     box {| apiVersion = "2017-04-01"
                            name = queue.Name.Value
                            ``type`` = "Queues"
                            dependsOn = [ this.Name.Value ]
                            properties =
                             {| lockDuration = tryGetIso queue.LockDuration
                                requiresDuplicateDetection =
                                    match queue.DuplicateDetectionHistoryTimeWindow with
                                    | Some _ -> Nullable true
                                    | None -> Nullable()
                                duplicateDetectionHistoryTimeWindow = tryGetIso queue.DuplicateDetectionHistoryTimeWindow
                                defaultMessageTimeToLive = queue.DefaultMessageTimeToLive.Value
                                requiresSession = queue.Session |> Option.toNullable
                                deadLetteringOnMessageExpiration = queue.DeadLetteringOnMessageExpiration |> Option.toNullable
                                maxDeliveryCount = queue.MaxDeliveryCount |> Option.toNullable
                                enablePartitioning = queue.EnablePartitioning |> Option.toNullable |}
                         |}
                  for topic in this.Topics do
                     box {| apiVersion = "2017-04-01"
                            name = topic.Name.Value
                            ``type`` = "Topics"
                            dependsOn = [ this.Name.Value ]
                            properties =
                                {| defaultMessageTimeToLive = tryGetIso topic.DefaultMessageTimeToLive
                                   requiresDuplicateDetection =
                                       match topic.DuplicateDetectionHistoryTimeWindow with
                                       | Some _ -> Nullable true
                                       | None -> Nullable()
                                   duplicateDetectionHistoryTimeWindow = tryGetIso topic.DuplicateDetectionHistoryTimeWindow
                                   enablePartitioning = topic.EnablePartitioning |> Option.toNullable |}
                            resources = [
                                for subscription in topic.Subscriptions do
                                    {| apiVersion = "2017-04-01"
                                       name = subscription.Name.Value
                                       ``type`` = "Subscriptions"
                                       dependsOn = [ topic.Name.Value ]
                                       properties =
                                        {| defaultMessageTimeToLive = tryGetIso subscription.DefaultMessageTimeToLive
                                           requiresDuplicateDetection =
                                               match subscription.DuplicateDetectionHistoryTimeWindow with
                                               | Some _ -> Nullable true
                                               | None -> Nullable()
                                           duplicateDetectionHistoryTimeWindow = tryGetIso subscription.DuplicateDetectionHistoryTimeWindow
                                           deadLetteringOnMessageExpiration = subscription.DeadLetteringOnMessageExpiration |> Option.toNullable
                                           maxDeliveryCount = subscription.MaxDeliveryCount |> Option.toNullable
                                           requiresSession = subscription.Session |> Option.toNullable
                                           lockDuration = tryGetIso subscription.LockDuration
                                        |}
                                    |}
                            ]
                         |}
                 ]
            |} :> _

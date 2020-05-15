[<AutoOpen>]
module Farmer.Arm.ServiceBus

open Farmer
open Farmer.CoreTypes
open Farmer.ServiceBus
open System

type ServiceBusQueue =
    { Name : ResourceName
      LockDuration : IsoDateTime option
      DuplicateDetectionHistoryTimeWindow : IsoDateTime option
      Session : bool option
      DeadLetteringOnMessageExpiration : bool option
      DefaultMessageTimeToLive : IsoDateTime
      MaxDeliveryCount : int option
      EnablePartitioning : bool option
      DependsOn : ResourceName list }

type Namespace =
    { Name : ResourceName
      Location : Location
      Sku : Sku
      Queues :ServiceBusQueue list
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
                     {| apiVersion = "2017-04-01"
                        name = queue.Name.Value
                        ``type`` = "Queues"
                        dependsOn = queue.DependsOn |> List.map (fun r -> r.Value)
                        properties =
                         {| lockDuration = queue.LockDuration |> Option.map (fun iso -> iso.Value) |> Option.toObj
                            requiresDuplicateDetection =
                                match queue.DuplicateDetectionHistoryTimeWindow with
                                | Some _ -> Nullable true
                                | None -> Nullable()
                            defaultMessageTimeToLive = queue.DefaultMessageTimeToLive.Value
                            duplicateDetectionHistoryTimeWindow =
                                queue.DuplicateDetectionHistoryTimeWindow
                                |> Option.map (fun iso -> iso.Value)
                                |> Option.toObj
                            requiresSession = queue.Session |> Option.toNullable
                            deadLetteringOnMessageExpiration = queue.DeadLetteringOnMessageExpiration |> Option.toNullable
                            maxDeliveryCount = queue.MaxDeliveryCount |> Option.toNullable
                            enablePartitioning = queue.EnablePartitioning |> Option.toNullable |}
                     |}
                 ]
            |} :> _

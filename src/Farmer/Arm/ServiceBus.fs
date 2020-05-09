[<AutoOpen>]
module Farmer.Arm.ServiceBus

open Farmer

type ServiceBusQueue =
    { Name : ResourceName
      LockDuration : string option
      DuplicateDetection : bool option
      DuplicateDetectionHistoryTimeWindow : string option
      Session : bool option
      DeadLetteringOnMessageExpiration : bool option
      MaxDeliveryCount : int option
      EnablePartitioning : bool option
      DependsOn : ResourceName list }

type Namespace =
    { Name : ResourceName
      Location : Location
      Sku : string
      Capacity : int option
      Queues :ServiceBusQueue list
      DependsOn : ResourceName list }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonValue =
            {| ``type`` = "Microsoft.ServiceBus/namespaces"
               apiVersion = "2017-04-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               sku =
                 {| name = this.Sku
                    tier = this.Sku
                    capacity = this.Capacity |> Option.toNullable |}
               dependsOn = this.DependsOn |> List.map (fun r -> r.Value)
               resources =
                 [ for queue in this.Queues do
                     {| apiVersion = "2017-04-01"
                        name = queue.Name.Value
                        ``type`` = "Queues"
                        dependsOn = queue.DependsOn |> List.map (fun r -> r.Value)
                        properties =
                         {| lockDuration = queue.LockDuration |> Option.toObj
                            requiresDuplicateDetection = queue.DuplicateDetection |> Option.toNullable
                            duplicateDetectionHistoryTimeWindow = queue.DuplicateDetectionHistoryTimeWindow |> Option.toObj
                            requiresSession = queue.Session |> Option.toNullable
                            deadLetteringOnMessageExpiration = queue.DeadLetteringOnMessageExpiration |> Option.toNullable
                            maxDeliveryCount = queue.MaxDeliveryCount |> Option.toNullable
                            enablePartitioning = queue.EnablePartitioning |> Option.toNullable |}
                     |}
                 ]
            |} :> _

[<AutoOpen>]
module Farmer.Arm.EventHub

open Farmer

type Namespace =
    { Name : ResourceName
      Location : Location
      Sku : {| Name : EventHubSku; Capacity : int |}
      ZoneRedundant : bool option
      AutoInflateSettings : InflateSetting option
      KafkaEnabled : bool option }
    member this.MaxThroughputUnits =
        this.AutoInflateSettings
        |> Option.bind (function
            | AutoInflate throughput -> Some throughput
            | ManualInflate -> None)
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = "Microsoft.EventHub/namespaces"
               apiVersion = "2017-04-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               sku =
                   {| name = string this.Sku.Name
                      tier = string this.Sku.Name
                      capacity = this.Sku.Capacity |}
               properties =
                   {| zoneRedundant = this.ZoneRedundant |> Option.toNullable
                      isAutoInflateEnabled =
                        this.AutoInflateSettings
                        |> Option.map (function
                            | AutoInflate _ -> true
                            | ManualInflate -> false)
                        |> Option.toNullable
                      maximumThroughputUnits = this.MaxThroughputUnits |> Option.toNullable
                      kafkaEnabled = this.KafkaEnabled |> Option.toNullable |}
            |} :> _
module Namespaces =
    type EventHub =
        { Name : ResourceName
          Location : Location
          MessageRetentionDays : int option
          Partitions : int
          Dependencies : ResourceName list }
        interface IArmResource with
            member this.ResourceName = this.Name
            member this.JsonModel =
               {| ``type`` = "Microsoft.EventHub/namespaces/eventhubs"
                  apiVersion = "2017-04-01"
                  name = this.Name.Value
                  location = this.Location.ArmValue
                  dependsOn = this.Dependencies |> List.map(fun d -> d.Value)
                  properties =
                      {| messageRetentionInDays = this.MessageRetentionDays |> Option.toNullable
                         partitionCount = this.Partitions
                         status = "Active" |}
               |} :> _
    module EventHubs =
        type ConsumerGroup =
            { Name : ResourceName
              Location : Location
              Dependencies : ResourceName list }
            interface IArmResource with
                member this.ResourceName = this.Name
                member this.JsonModel =
                    {| ``type`` = "Microsoft.EventHub/namespaces/eventhubs/consumergroups"
                       apiVersion = "2017-04-01"
                       name = this.Name.Value
                       location = this.Location.ArmValue
                       dependsOn = this.Dependencies |> List.map(fun d -> d.Value)
                    |} :> _

        type AuthorizationRule =
            { Name : ResourceName
              Location : Location
              Dependencies : ResourceName list
              Rights : AuthorizationRuleRight Set }
            interface IArmResource with
                member this.ResourceName = this.Name
                member this.JsonModel =
                    {| ``type`` = "Microsoft.EventHub/namespaces/eventhubs/AuthorizationRules"
                       apiVersion = "2017-04-01"
                       name = this.Name.Value
                       location = this.Location.ArmValue
                       dependsOn = this.Dependencies |> List.map(fun d -> d.Value)
                       properties = {| rights = this.Rights |> Set.map string |> Set.toList |}
                    |} :> _


[<AutoOpen>]
module Farmer.Arm.EventHub

open Farmer

type Namespace =
    { Name : ResourceName
      Location : Location
      Sku : {| Name : string; Tier : string; Capacity : int |}
      ZoneRedundant : bool option
      IsAutoInflateEnabled : bool option
      MaxThroughputUnits : int option
      KafkaEnabled : bool option }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = "Microsoft.EventHub/namespaces"
               apiVersion = "2017-04-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               sku =
                   {| name = this.Sku.Name
                      tier = this.Sku.Tier
                      capacity = this.Sku.Capacity |}
               properties =
                   {| zoneRedundant = this.ZoneRedundant |> Option.toNullable
                      isAutoInflateEnabled = this.IsAutoInflateEnabled |> Option.toNullable
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
              Rights : string list }
            interface IArmResource with
                member this.ResourceName = this.Name
                member this.JsonModel =
                    {| ``type`` = "Microsoft.EventHub/namespaces/eventhubs/AuthorizationRules"
                       apiVersion = "2017-04-01"
                       name = this.Name.Value
                       location = this.Location.ArmValue
                       dependsOn = this.Dependencies |> List.map(fun d -> d.Value)
                       properties = {| rights = this.Rights |}
                    |} :> _


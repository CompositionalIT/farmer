[<AutoOpen>]
module Farmer.Arm.EventHub

open Farmer
open Farmer.CoreTypes
open Farmer.EventHub

let namespaces = ResourceType ("Microsoft.EventHub/namespaces", "2017-04-01")
let eventHubs = ResourceType ("Microsoft.EventHub/namespaces/eventhubs", "2017-04-01")
let consumerGroups = ResourceType ("Microsoft.EventHub/namespaces/eventhubs/consumergroups", "2017-04-01")
let authorizationRules = ResourceType ("Microsoft.EventHub/namespaces/eventhubs/AuthorizationRules", "2017-04-01")

type CaptureDestination =
    | StorageAccount of ResourceName * containerName:string

type Namespace =
    { Name : ResourceName
      Location : Location
      Sku : {| Name : EventHubSku; Capacity : int |}
      ZoneRedundant : bool option
      AutoInflateSettings : InflateSetting option
      KafkaEnabled : bool option
      Tags: Map<string,string>  }
    member this.MaxThroughputUnits =
        this.AutoInflateSettings
        |> Option.bind (function
            | AutoInflate throughput -> Some throughput
            | ManualInflate -> None)
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = namespaces.Path
               apiVersion = namespaces.Version
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
               tags = this.Tags
            |} :> _
module Namespaces =
    type EventHub =
        { Name : ResourceName
          Location : Location
          MessageRetentionDays : int option
          Partitions : int
          Dependencies : ResourceName list
          CaptureDestination : CaptureDestination option
          Tags: Map<string,string>  }
        interface IArmResource with
            member this.ResourceName = this.Name
            member this.JsonModel =
               {| ``type`` = eventHubs.Path
                  apiVersion = eventHubs.Version
                  name = this.Name.Value
                  location = this.Location.ArmValue
                  dependsOn = this.Dependencies |> List.map(fun d -> d.Value)
                  properties =
                    {| messageRetentionInDays = this.MessageRetentionDays |> Option.toNullable
                       partitionCount = this.Partitions
                       status = "Active"
                       captureDescription =
                        match this.CaptureDestination with
                        | Some (StorageAccount(name, container)) ->
                            {| enabled = true
                               encoding = "Avro"
                               destination =
                                {| name = "EventHubArchive.AzureBlockBlob"
                                   properties =
                                    {| storageAccountResourceId = ArmExpression.resourceId(storageAccounts, name).Eval()
                                       blobContainer = container
                                    |}
                                |}
                            |} |> box
                        | None ->
                            null
                    |}
                  tags = this.Tags
               |} :> _
    module EventHubs =
        type ConsumerGroup =
            { Name : ResourceName
              Location : Location
              Dependencies : ResourceName list }
            interface IArmResource with
                member this.ResourceName = this.Name
                member this.JsonModel =
                    {| ``type`` = consumerGroups.Path
                       apiVersion = consumerGroups.Version
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
                    {| ``type`` = authorizationRules.Path
                       apiVersion = authorizationRules.Version
                       name = this.Name.Value
                       location = this.Location.ArmValue
                       dependsOn = this.Dependencies |> List.map(fun d -> d.Value)
                       properties = {| rights = this.Rights |> Set.map string |> Set.toList |}
                    |} :> _
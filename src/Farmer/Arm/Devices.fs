[<AutoOpen>]
module Farmer.Arm.Devices

open Farmer
open Farmer.CoreTypes

let iotHubs = ResourceType ("Microsoft.Devices/IotHubs", "2019-03-22")
let provisioningServices = ResourceType ("Microsoft.Devices/provisioningServices",  "2018-01-22")

type Sku =
| Free
| Paid of name:string * units:int
    static member F1 = Free
    static member B1 (units) = Paid ("B1", units)
    static member B2 (units) = Paid ("B2", units)
    static member B3 (units) = Paid ("B3", units)
    static member S1 (units) = Paid ("S1", units)
    static member S2 (units) = Paid ("S2", units)
    static member S3 (units) = Paid ("S3", units)
    member this.Name =
        match this with
        | Free -> "F1"
        | Paid(name, _) -> name
    member this.Capacity =
        match this with
        | Free -> 1
        | Paid(_, capacity) -> capacity
type DeliveryDetails =
    {| Ttl : IsoDateTime option
       LockDuration : IsoDateTime option
       MaxDeliveryCount : int option |}
let serialize (d:DeliveryDetails) =
    {| ttlAsIso8601 = d.Ttl |> Option.map(fun f -> f.Value) |> Option.toObj
       lockDurationAsIso8601 = d.LockDuration |> Option.map(fun f -> f.Value) |> Option.toObj
       maxDeliveryCount = d.MaxDeliveryCount |> Option.toNullable |}

type iotHubs =
    { Name : ResourceName
      Location : Location
      Sku : Sku
      RetentionDays : int option
      PartitionCount : int option
      DefaultTtl : IsoDateTime option
      MaxDeliveryCount : int option
      Feedback : DeliveryDetails option
      FileNotifications : DeliveryDetails option
      Tags: Map<string,string>  }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| iotHubs.Create(this.Name, this.Location, tags = this.Tags) with
                   properties =
                    {| eventHubEndpoints =
                        match this.RetentionDays, this.PartitionCount with
                        | None, None ->
                            null
                        | _ ->
                            box
                                {| events =
                                    {| retentionTimeInDays = this.RetentionDays |> Option.toNullable
                                       partitionCount = this.PartitionCount |> Option.toNullable |}
                                |}
                       cloudToDevice =
                        match this with
                        | { DefaultTtl = None; MaxDeliveryCount = None; Feedback = None } ->
                            null
                        | _ ->
                            box
                                {| defaultTtlAsIso8601 = this.DefaultTtl |> Option.map(fun v -> v.Value) |> Option.toObj
                                   maxDeliveryCount = this.MaxDeliveryCount |> Option.toNullable
                                   feedback = this.Feedback |> Option.map (serialize >> box) |> Option.toObj |}
                       messagingEndpoints =
                        this.FileNotifications
                        |> Option.map(fun fileNotifications -> box {| fileNotifications = fileNotifications |> serialize |})
                        |> Option.toObj
                    |}
                   sku =
                    {| name = this.Sku.Name
                       capacity = this.Sku.Capacity |}
            |} :> _

type ProvisioningServices =
    { Name : ResourceName
      Location : Location
      IotHubName : ResourceName
      IotHubKey : ArmExpression
      Tags: Map<string,string>  }
    member this.IotHubConnection =
        sprintf "concat('HostName=%s.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=',%s)"
            this.IotHubName.Value
            this.IotHubKey.Value
        |> ArmExpression.create
    member this.IotHubPath =
        sprintf "%s.azure-devices.net" this.IotHubName.Value
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| provisioningServices.Create(this.Name, this.Location, [ ResourceId.create this.IotHubName ], this.Tags) with
                   sku =
                     {| name = "S1"
                        capacity = 1 |}
                   properties =
                     {| iotHubs = [
                           {| connectionString = this.IotHubConnection.Eval()
                              location = this.Location.ArmValue
                              name = this.IotHubPath |}
                       ]
                     |}
            |} :> _
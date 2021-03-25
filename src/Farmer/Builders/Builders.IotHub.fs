[<AutoOpen>]
module Farmer.Builders.IotHub

open Farmer
open Farmer.Arm
open Farmer.IotHub

type IotHubConfig =
    { Name : ResourceName
      Sku : Sku
      Capacity : int
      RetentionDays : int option
      PartitionCount : int option
      DeviceProvisioning : FeatureFlag
      Tags: Map<string,string>  }
    member private this.BuildKey (policy:Policy) =
        $"listKeys('{this.Name.Value}','2019-03-22').value[{policy.Index}].primaryKey"
    member this.GetKey policy =
        ArmExpression.create(this.BuildKey policy, this.ResourceId)
    member this.GetConnectionString policy =
        let endpoint = $"reference('{this.Name.Value}').eventHubEndpoints.events.endpoint"
        let expr = $"concat('Endpoint=',{endpoint},';SharedAccessKeyName={policy.ToString().ToLower()};SharedAccessKey=',{this.BuildKey policy})"
        ArmExpression.create(expr, this.ResourceId)
    member private this.ResourceId = iotHubs.resourceId this.Name
    interface IBuilder with
        member this.ResourceId = this.ResourceId
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              Sku =
                match this.Sku with
                | F1 -> Free
                | B1 -> Devices.Sku.B1 this.Capacity
                | B2 -> Devices.Sku.B2 this.Capacity
                | B3 -> Devices.Sku.B3 this.Capacity
                | S1 -> Devices.Sku.S1 this.Capacity
                | S2 -> Devices.Sku.S2 this.Capacity
                | S3 -> Devices.Sku.S3 this.Capacity
              RetentionDays = this.RetentionDays
              PartitionCount = this.PartitionCount
              DefaultTtl = None
              MaxDeliveryCount = None
              Feedback = None
              FileNotifications = None
              Tags = this.Tags  }

            if this.DeviceProvisioning = Enabled then
                { Name = this.Name.Map(sprintf "%s-dps")
                  Location = location
                  IotHubKey = this.GetKey IotHubOwner
                  IotHubName = this.Name
                  Tags = this.Tags  }
        ]

type IotHubBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Sku = F1
          Capacity = 1
          RetentionDays = None
          PartitionCount = None
          DeviceProvisioning = Disabled
          Tags = Map.empty  }
    member _.Run state =
        match state.PartitionCount with
        | Some partitionCount when partitionCount < 2 || partitionCount > 128 ->
            failwith $"Invalid PartitionCount {partitionCount} - value must be between 2 and 128"
        | Some _
        | None ->
            state

    [<CustomOperation "name">]
    /// Sets the name of the IOT Hub instance.
    member _.Name (state:IotHubConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "sku">]
    /// Sets the name of the SKU/Tier for the IOT Hub instance.
    member _.Sku (state:IotHubConfig, sku) = { state with Sku = sku }

    [<CustomOperation "capacity">]
    /// Sets the name of the capacity for the IOT Hub instance.
    member _.Capacity (state:IotHubConfig, capacity) = { state with Capacity = capacity }

    [<CustomOperation "partition_count">]
    /// Sets the name of the SKU/Tier for the IOT Hub instance.
    member _.PartitionCount (state:IotHubConfig, partitions) = { state with PartitionCount = Some partitions }

    [<CustomOperation "retention_days">]
    /// Sets the name of the SKU/Tier for the IOT Hub instance.
    member _.RetentionDays (state:IotHubConfig, days) = { state with RetentionDays = Some days }

    [<CustomOperation "enable_device_provisioning">]
    /// Sets the name of the SKU/Tier for the IOT Hub instance.
    member _.DeviceProvisioning (state:IotHubConfig) = { state with DeviceProvisioning = Enabled }
    interface ITaggable<IotHubConfig> with member _.Add state tags = { state with Tags = state.Tags |> Map.merge tags }

let iotHub = IotHubBuilder()
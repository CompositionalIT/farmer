module IotHub

open Expecto
open Farmer
open Farmer.Builders
open Farmer.IotHub
open Microsoft.Azure.Management.DeviceProvisioningServices
open Microsoft.Azure.Management.DeviceProvisioningServices.Models
open Microsoft.Azure.Management.IotHub
open Microsoft.Azure.Management.IotHub.Models
open Microsoft.Rest
open Microsoft.Rest.Serialization
open System
open Farmer.CoreTypes

let iotClient = new IotHubClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")
let provisioningClient = new IotHubClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let tests = testList "IOT Hub" [
    test "Can create a basic hub" {
        let resource =
            let hub = iotHub {
                name "isaacsuperhub"
                sku B1
                capacity 2
                partition_count 2
                retention_days 3
            }

            arm { add_resource hub }
            |> findAzureResources<IotHubDescription> iotClient.SerializationSettings
            |> List.head

        Expect.equal resource.Name "isaacsuperhub" "Hub name does not match"
        Expect.equal resource.Sku.Name "B1" "Sku name is incorrect"
        Expect.equal resource.Sku.Capacity (Nullable 2L) "Sku capacity is incorrect"

        let events = resource.Properties.EventHubEndpoints.["events"]
        Expect.equal events.PartitionCount (Nullable 2) "Partition count is incorrect"
        Expect.equal events.RetentionTimeInDays (Nullable 3L) "Retention time is incorrect"
    }

    test "Creates a provisioning service" {
        let resource =
            let hub = iotHub {
                name "iothub"
                enable_device_provisioning
            }
            let deployment = arm { add_resource hub }
            (Deployment.getTemplate "farmer-resources" deployment).Resources.[1].JsonModel
            |> SafeJsonConvert.SerializeObject
            |> fun json -> SafeJsonConvert.DeserializeObject<ProvisioningServiceDescription>(json, provisioningClient.SerializationSettings)

        Expect.equal resource.Sku.Capacity (Nullable 1L) "Sku capacity is incorrect"
        Expect.equal resource.Sku.Name "S1" "Sku name capacity is incorrect"
    }
]
#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders

let hub = iotHub {
    name "isaacsuperhub"
    sku IotHub.B1
    capacity 2
    partition_count 2
    retention_days 3
    enable_device_provisioning
}

let deployment = arm {
    location Location.NorthEurope
    add_resource hub
    output "iot_key" (hub.GetKey IotHub.IotHubOwner)
    output "iot_connection" (hub.GetConnectionString IotHub.RegistryReadWrite)
}

deployment
|> Writer.quickWrite "generated-template"

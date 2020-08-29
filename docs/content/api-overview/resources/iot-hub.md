---
title: "IOT Hub"
date: 2020-05-19T23:14:14+02:00
chapter: false
weight: 13
---

#### Overview
The IOT Hub builder creates IOT Hub and linked Provision Services.

* IOT Hubs (`Microsoft.Devices/IotHubs`)
* Provisioning Services (`Microsoft.Devices/provisioningServices`)

#### Builder Keywords

| Keyword | Purpose |
|-|-|
| name | Specifies the name of the IOT Hub |
| sku | Sets the SKU of the IOT Hub |
| capacity | Sets the name of the capacity for the IOT Hub instance |
| partition_count | Sets the name of the SKU/Tier for the IOT Hub instance |
| retention_days | Sets the name of the SKU/Tier for the IOT Hub instance |
| enable_device_provisioning | Sets the name of the SKU/Tier for the IOT Hub instance |

#### Configuration Members

| Member | Purpose |
|-|-|
| GetKey | Returns an ARM expression to retrieve the IOT Hub key for a specific policy e.g IotHubOwner or RegistryReadWrite. Useful for e.g. supplying the key to another resource e.g. KeyVault or an app setting in the App Service. |
| GetConnectionString | Returns an ARM expression to generate an IOT Hub connection string for a specific policy e.g IotHubOwner or RegistryReadWrite. Useful for e.g. supplying the key to another resource e.g. KeyVault or an app setting in the App Service. |

#### Example

```fsharp
open Farmer
open Farmer.Builders

let hub = iotHub {
    name "yourhubname"
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
```
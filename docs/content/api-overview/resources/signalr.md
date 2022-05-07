---
title: "SignalR"
date: 2020-06-01T11:13:00+01:00
chapter: false
weight: 18
---

#### Overview
The SignalR builder creates SignalR services.

* SignalR Service (`Microsoft.SignalRService/signalR`)

#### Builder Keywords
| Keyword | Purpose |
|-|-|
| name | Sets the name of the SignalR service. |
| sku | Sets the sku of the SignalR service. |
| capacity | Sets the capacity of the SignalR service. (optional) |
| service_mode | Sets the service mode of the SignalR service. (optional) |
| allowed_origins | Sets the allowed origins of the SignalR service. (optional) |

#### Configuration Members

| Member | Purpose |
|-|-|
| Key | Returns an ARM expression to retrieve the primary Key of the service. Useful for e.g. supplying the connection string to another resource e.g. KeyVault or an app setting in the App Service. |
| ConnectionString | Returns an ARM expression to retrieve the primary Connection String of the service. Useful for e.g. supplying the connection string to another resource e.g. KeyVault or an app setting in the App Service. |

#### Example

```fsharp
open Farmer
open Farmer.Builders

let mySignalR = signalR {
    name "mysignalr"
    sku SignalR.Standard
    capacity 10
    service_mode ServiceMode.Default
    allowed_origins [ "https://github.com" ]
}
```

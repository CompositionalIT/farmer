---
title: "Traffic Manager"
date: 2021-05-235T13:01:00+01:00
chapter: false
weight: 20
---

#### Overview
The Traffic Manager builder (`trafficManager`) creates traffic manager profiles and their associated endpoints.

* Traffic Manager Profiles (`Microsoft.Network/trafficManagerProfiles`)
* Traffic Manager Azure Endpoints (`Microsoft.Network/trafficManagerProfiles/azureEndpoints`)
* Traffic Manager External Endpoints (`Microsoft.Network/trafficManagerProfiles/externalEndpoints`)

#### Builder Keywords

| Builder | Keyword | Purpose |
|-|-|-|
| trafficManager | name | Sets the name of the Traffic Manager profile. |
| trafficManager | dns_ttl | Sets the DNS TTL of the Traffic Manager profile, in seconds (default 30). |
| trafficManager | disable_profile | Disables the Traffic Manager profile. |
| trafficManager | enable_profile | Enables the Traffic Manager profile. |
| trafficManager | routing_method | Sets the routing method of the Traffic Manager profile (default Performance). |
| trafficManager | enable_traffic_view | Enables the Traffic View of the Traffic Manager profile. |
| trafficManager | disable_traffic_view | Disables the Traffic View of the Traffic Manager profile. |
| trafficManager | monitor_protocol | Sets the monitoring protocol of the Traffic Manager profile (default Https). |
| trafficManager | monitor_port | Sets the monitoring port of the Traffic Manager profile (default 443). |
| trafficManager | monitor_path | Sets the monitoring path of the Traffic Manager profile (default /). |
| trafficManager | monitor_interval | Sets the monitoring interval, in seconds, of the Traffic Manager profile (default 30). |
| trafficManager | monitor_timeout | Sets the monitoring timeout, in seconds, of the Traffic Manager profile (default 10). |
| trafficManager | monitor_tolerated_failures | Sets the monitoring tolerated number of failures, of the Traffic Manager profile (default 3). |
| trafficManager | add_endpoints | Adds Endpoints to the Traffic Manager profile. |
| endpoint | name | Sets the name of the Endpoint. |
| endpoint | weight | Sets the weight of the Endpoint. |
| endpoint | priority | Sets the priority of the Endpoint. |
| endpoint | enable_endpoint | Enables the Endpoint. |
| endpoint | disable_endpoint  | Disables the Endpoint. |
| endpoint | target_webapp | Sets the target of the Endpoint to a web app. |
| endpoint | target_external | Sets the target of the Endpoint to an external domain/IP and location. |

#### Example

```fsharp
open Farmer
open Farmer.Builders
open Farmer.TrafficManager

let myTrafficManager = trafficManager {
    name "my-trafficmanager-profile"
    routing_method RoutingMethod.Performance
    add_endpoints [ 
        endpoint {
            name "my-external-endpoint"
            weight 1
            priority 1
            target_external "mydomain.com" Location.WestUS
        }
        endpoint {
            name "my-web-app-endpoint"
            weight 1
            priority 2
            target_webapp (ResourceName "my-web-app")
        }
     ]
    monitor_path "/"
    monitor_port 443
    monitor_protocol Https
    monitor_interval 30<Seconds>
    monitor_timeout 5<Seconds>
    monitor_tolerated_failures 4
    enable_traffic_view
    dns_ttl 30<Seconds>
}

arm {
    location Location.EastUS
    add_resource myTrafficManager
}
```

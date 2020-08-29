---
title: "Network Security Group"
date: 2020-07-19T16:44:00-04:00
chapter: false
weight: 16
---

#### Overview
The Network Security Group builder creates network security groups with rules for securing network access to resources.

* Network Security Groups (`Microsoft.Network/networkSecurityGroups`)
* Security Rules (`Microsoft.Network/networkSecurityGroups/securityRules`)

#### Builder Keywords

| Applies To | Keyword | Purpose |
|-|-|-|
| nsg | name | Specifies the name of the network security group |
| nsg | add_rules | Adds security rules to the network security group |
| securityRule | name | The name of the security rule |
| securityRule | description | The description  of the security rule |
| securityRule | service | The service port(s) and protocol(s) protected by this security rule |
| securityRule | add_source | Specify access from any source protocol, address, and port |
| securityRule | add_source_any | Specify access from any address and any port |
| securityRule | add_source_address | Specify access from a specific address and any port |
| securityRule | add_source_network | Specify access from a specific network and any port |
| securityRule | add_source_tag | Specify access from a tagged source such as "Internet", "VirtualNetwork", or "AzureLoadBalancer" |
| securityRule | add_destination | Specify access to any source protocol, address, and port |
| securityRule | add_destination_any | Specify access to any address and any port |
| securityRule | add_destination_address | Specify access to a specific address and any port |
| securityRule | add_destination_network | Specify access from a specific network and any port |
| securityRule | add_destination_tag | Specify access to a tagged destination such as "Internet", "VirtualNetwork", or "AzureLoadBalancer" |
| securityRule | allow | Allows this traffic (the default) |
| securityRule | deny | Denies this traffic |

#### Basic Example

```fsharp
open Farmer
open Farmer.Builders
open Farmer.NetworkSecurity

// Create a rule for https services accessible from the internet
let httpsAccess = securityRule {
    name "web-servers"
    service (Service ("https", Port 443us))
    add_source_tag TCP "Internet"
    add_destination_any
}
// Create an NSG and add the rule to it.
let myNsg = nsg {
    name "my-nsg"
    add_rules [
        httpsAccess
    ]
}
```

#### Multiple Tier Private Network Example

```fsharp
open Farmer
open Farmer.Builders
open Farmer.NetworkSecurity

// Many services have a few ports, such as web services that are often on 80 and 443.
let web = Services ("web", [
    Service ("http", Port 80us)
    Service ("https", Port 443us)
])
// Some services only have a single port
let app = Service ("http", Port 8080us)
let database = Service ("postgres", Port 5432us)

// Different tiers may reside on different network segments
let corporateNet = "172.24.0.0/20"
let webNet = "10.100.30.0/24"
let appNet = "10.100.31.0/24"
let dbNet = "10.100.32.0/24"

// Create a rule for web servers - the 'web' service, accessible from the corporate network
let webAccess = securityRule {
    name "web-servers"
    description "Public web server access"
    service web
    add_source_network TCP corporateNet
    add_destination_network webNet
}

// Create another rule for app servers - accessible only from network with the web servers
let appAccess= securityRule {
    name "app-servers"
    description "Internal app server access"
    service app
    add_source_network TCP webNet
    add_destination_network appNet
}

// Create another rule for DB servers - accessible only from network with the app servers
let dbAccess = securityRule {
    name "db-servers"
    description "Internal database server access"
    service database
    add_source_network TCP appNet
    add_destination_network dbNet
}

// Create an NSG and add all 3 rules to it.
let myNsg = nsg {
    name "my-nsg"
    add_rules [
        webAccess
        appAccess
        dbAccess
    ]
}
```

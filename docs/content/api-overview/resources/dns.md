---
title: "DNS Zone"
date: 2020-09-14T18:53:46+01:00
chapter: false
weight: 8
---

#### Overview
The DNS Zone module contains two types of builders - `dnsZone`, used to create DNS Zones, and `___Record` (like `cnameRecord`, `aRecord`, ..), used to create DNS Records sets.
It supports most record types (except SOA, SRV and CAA) and has specific builders for every record type.

* DNS Zone (`Microsoft.Network/dnsZones`)
* A Record (`Microsoft.Network/dnsZones/A`)
* AAAA Record (`Microsoft.Network/dnsZones/AAAA`)
* CNAME Record (`Microsoft.Network/dnsZones/CNAME`)
* TXT Record (`Microsoft.Network/dnsZones/TXT`)
* MX Record (`Microsoft.Network/dnsZones/MX`)
* NS Record (`Microsoft.Network/dnsZones/NS`)
* PTR Record (`Microsoft.Network/dnsZones/PTR`)

#### TODO
The following items are currently unsupported:
- SOA records
- SRV records
- CAA records
- Private Zone (untested)
- Virtual network support for Private Zones
- Tags

#### DNS Zone Builder Keywords
| Keyword | Purpose |
|-|-|
| name | Sets the name of the domain. |
| zone_type | Sets the zone type. |
| add_records | Adds DNS Zone records (see below). |

Each Record type has its own custom builder. All builders share the following common keywords:

| Keyword | Purpose |
|-|-|
| name | Sets the name of the record set (default to `@`). |
| ttl | Sets the time-to-live of the record set. |

In addition, each record builder has its own custom keywords:

#### A Record Builder Keywords

| Keyword | Purpose |
|-|-|
| add_ipv4_addresses | Add IPv4 addresses to this record set. |
| target_resource | A reference to an azure resource from where the dns resource value is taken. |

#### AAAA Record Builder Keywords

| Keyword | Purpose |
|-|-|
| add_ipv6_addresses | Add IPv6 addresses to this record set. |
| target_resource | A reference to an azure resource from where the dns resource value is taken. |

#### CNAME Record Builder Keywords

| Keyword | Purpose |
|-|-|
| cname | Sets the canonical name for this CNAME record. |
| target_resource | A reference to an azure resource from where the dns resource value is taken. |

#### TXT Record Builder Keywords

| Keyword | Purpose |
|-|-|
| add_values | Add TXT values to this record set. |

#### MX Record Builder Keywords

| Keyword | Purpose |
|-|-|
| add_values | Add MX values to the record set. |

#### NS Record Builder Keywords

| Keyword | Purpose |
|-|-|
| add_nsd_names | Add NS values to this record set. |

#### PTR Record Builder Keywords

| Keyword | Purpose |
|-|-|
| add_ptrd_names | Add PTR names to this record set. |

#### Example
```fsharp
#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders

let dns = dnsZone {
    name "farmer.com"
    zone_type Dns.Public
    add_records [
        cnameRecord {
            name "www2"
            ttl 3600
            cname "farmer.github.com"
        }
        aRecord {
            ttl 7200
            add_ipv4_addresses [ "192.168.0.1"; "192.168.0.2" ]
        }
        aaaaRecord {
            ttl 7200
            add_ipv6_addresses [ "100:100:100:100" ]
        }
        txtRecord {
            ttl 3600
            add_values [ "v=spf1 include:spf.protection.outlook.com -all" ]
        }
        mxRecord {
            ttl 7200
            add_values [
                0, "farmer-com.mail.protection.outlook.com";
                1, "farmer2-com.mail.protection.outlook.com";
            ]
        }
    ]
}

let deployment = arm {
    location Location.NorthEurope
    add_resource dns
}

deployment
|> Writer.quickWrite "dns-example"
```
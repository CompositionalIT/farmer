---
title: "DNS Zone"
date: 2020-09-14T18:53:46+01:00
chapter: false
weight: 4
---

#### Overview
The DNS Zone module contains two types of builders - `dnsZone`, used to create DNS Zones, and `___Record` (like `cnameRecord`, `aRecord`, ..), used to create DNS Records sets.
It supports most record types (except CAA) and has specific builders for every record type.

* DNS Zone (`Microsoft.Network/dnsZones`)
* A Record (`Microsoft.Network/dnsZones/A`)
* AAAA Record (`Microsoft.Network/dnsZones/AAAA`)
* CNAME Record (`Microsoft.Network/dnsZones/CNAME`)
* TXT Record (`Microsoft.Network/dnsZones/TXT`)
* MX Record (`Microsoft.Network/dnsZones/MX`)
* NS Record (`Microsoft.Network/dnsZones/NS`)
* PTR Record (`Microsoft.Network/dnsZones/PTR`)
* SOA Record (`Microsoft.Network/dnsZones/SOA`)
* SRV Record (`Microsoft.Network/dnsZones/SRV`)

#### SOA records
You can only have one SOA record and it is [always created alongside a DNS zone](https://docs.microsoft.com/en-us/azure/dns/dns-zones-records#soa-records), whether you specify it or not.

You can use the builder provided by Farmer to edit any of its properties. You should **not**, however, edit the `host` as [this is set automatically by Azure](https://docs.microsoft.com/en-us/azure/dns/dns-operations-recordsets-portal#modify-soa-records).

Ideally it just wouldn't be exposed, however [contrary to the official documentation](https://docs.microsoft.com/en-us/azure/templates/microsoft.network/dnszones/soa?tabs=json#soarecord-object) Azure rejects the ARM record if it is absent. For this reason if you wish to use the SOA builder it is recommended to first deploy your DNS Zone without it, copy the generated SOA host from the portal and then finally paste it into the Farmer builder's `host` parameter.

#### NS Records
An NS record is automatically added to every DNS zone at the apex (@) containing the name of the Azure DNS servers assigned to the zone.

You can modify [certain properties of it, but not others](https://docs.microsoft.com/en-us/azure/dns/dns-zones-records#ns-records).

If you wish to create a *new* NS record set, you **must** give it a `name` field.

#### TODO
The following items are currently unsupported:
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
| link_to_unmanaged_dns_zone | Add the record to an existing DNS zone. |

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

#### SRV Record Builder Keywords

| Keyword | Purpose |
|-|-|
| name | The service and protocol [must be specified](https://docs.microsoft.com/en-us/azure/dns/dns-zones-records#srv-records) as part of the record set name, prefixed with underscores. |
| add_values | Add Farmer.DNS.SrvRecord values to this record set. |

#### SOA Record Builder Keywords

| Keyword | Purpose |
|-|-|
| host | Sets the host name for the record |
| email | Sets the email for the record |
| expire_time | Sets the expire time name for the record in seconds |
| minimum_ttl | Sets the minimum time to live for the record in seconds |
| refresh_time | Sets the refresh time for the record in seconds |
| retry_time | Sets the retry time for the record in seconds |
| serial_number | Sets the serial number for the record |

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
            name "aName"
            ttl 7200
            add_ipv4_addresses [ "192.168.0.1"; "192.168.0.2" ]
        }
        aaaaRecord {
            name "aaaaName"
            ttl 7200
            add_ipv6_addresses [ "2001:0db8:85a3:0000:0000:8a2e:0370:7334" ]
        }
        txtRecord {
            name "txtName"
            ttl 3600
            add_values [ "v=spf1 include:spf.protection.outlook.com -all" ]
        }
        mxRecord {
            name "mxName"
            ttl 7200
            add_values [
                0, "farmer-com.mail.protection.outlook.com";
                1, "farmer2-com.mail.protection.outlook.com";
            ]
        }
        soaRecord {
            name "soaName"
            host "ns1-09.azure-dns.com."
            ttl 3600
            email "test.microsoft.com"
            serial_number 1L
            minimum_ttl 300L
            refresh_time 3600L
            retry_time 300L
            expire_time 2419200L
        }
        srvRecord {
            name "_sip._tcp.name"
            ttl 3600
            add_values [
                { Priority = Some 100
                Weight = Some 1
                Port = Some 5061
                Target = Some "farmer.online.com."}
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
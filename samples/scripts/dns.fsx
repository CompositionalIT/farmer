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
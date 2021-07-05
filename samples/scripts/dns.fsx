#r "nuget:Farmer"

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
            host "ns1-09.azure-dns.com."
            ttl 3600
            email "test.microsoft.com"
            serial_number 1L 
            minimum_TTL 2L
            refresh_time 3L
            retry_time 4L
            expire_time 5L
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
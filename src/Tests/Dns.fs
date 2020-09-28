module Dns

open Expecto
open Farmer
open Farmer.Builders
open System
open Microsoft.Rest
open Microsoft.Azure.Management.Dns
open Microsoft.Azure.Management.Dns.Models

let client = new DnsManagementClient (Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let tests = testList "DNS Zone" [
    test "Public DNS Zone is created with a CNAME record" {

        let resources =
            arm {
                add_resources [
                    dnsZone {
                        name "farmer.com"
                        add_records [
                            cnameRecord {
                                name "www"
                                ttl 3600
                                cname "farmer.com"
                            }
                            aRecord {
                                ttl 7200
                                add_ipv4_addresses [ "192.168.0.1" ]
                            }
                        ]
                    }
                ]
            }

        let dnsZones =
            resources
            |> findAzureResources<Zone> client.SerializationSettings
            |> Array.ofList

        Expect.equal dnsZones.[0].Name "farmer.com" "DNS Zone name is wrong"
        Expect.equal dnsZones.[0].Type "Microsoft.Network/dnsZones" "DNS Zone type is wrong"
        Expect.equal dnsZones.[0].ZoneType (Nullable(ZoneType.Public)) "DNS Zone ZoneType is wrong"

        let dnsRecords =
            resources
            |> findAzureResources<RecordSet> client.SerializationSettings
            |> Array.ofList

        Expect.equal dnsRecords.[1].Name "farmer.com/www" "DNS CNAME record name is wrong"
        Expect.equal dnsRecords.[1].CnameRecord.Cname "farmer.com" "DNS CNAME record is wrong"
        Expect.equal dnsRecords.[1].TTL (Nullable(3600L)) "DNS record TTL is wrong"

        Expect.equal dnsRecords.[2].Name "farmer.com/@" "DNS A record name is wrong"
        Expect.sequenceEqual (dnsRecords.[2].ARecords |> Seq.map (fun x -> x.Ipv4Address)) [ "192.168.0.1" ] "DNS A record IP address is wrong"
        Expect.equal dnsRecords.[2].TTL (Nullable(7200L)) "DNS record TTL is wrong"
    }
]

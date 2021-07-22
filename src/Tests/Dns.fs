module Dns

open Expecto
open Farmer
open Farmer.Builders
open System
open Farmer.Dns
open Microsoft.Rest
open Microsoft.Azure.Management.Dns
open Microsoft.Azure.Management.Dns.Models
open Newtonsoft.Json.Linq

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
                                name "aName"
                                ttl 7200
                                add_ipv4_addresses [ "192.168.0.1" ]
                            }
                            soaRecord {
                                name "soaName"
                                host "ns1-09.azure-dns.com."
                                ttl 3600
                                email "azuredns-hostmaster.microsoft.com"
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
                            txtRecord {
                                name "txtName"
                                ttl 3600
                                add_values [
                                    "somevalue"
                                ]
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
        Expect.equal dnsZones.[0].ZoneType (Nullable ZoneType.Public) "DNS Zone ZoneType is wrong"

        let dnsRecords =
            resources
            |> findAzureResources<RecordSet> client.SerializationSettings
            |> Array.ofList

        Expect.equal dnsRecords.[1].Name "farmer.com/www" "DNS CNAME record name is wrong"
        Expect.equal dnsRecords.[1].CnameRecord.Cname "farmer.com" "DNS CNAME record is wrong"
        Expect.equal dnsRecords.[1].TTL (Nullable 3600L) "DNS record TTL is wrong"

        Expect.equal dnsRecords.[2].Name "farmer.com/aName" "DNS A record name is wrong"
        Expect.sequenceEqual (dnsRecords.[2].ARecords |> Seq.map (fun x -> x.Ipv4Address)) [ "192.168.0.1" ] "DNS A record IP address is wrong"
        Expect.equal dnsRecords.[2].TTL (Nullable(7200L)) "DNS record TTL is wrong"

        Expect.equal dnsRecords.[3].Name "farmer.com/soaName" "DNS SOA record name is wrong"
        Expect.equal dnsRecords.[3].SoaRecord.Host "ns1-09.azure-dns.com." "DNS SOA record host wrong"
        Expect.equal dnsRecords.[3].SoaRecord.Email "azuredns-hostmaster.microsoft.com" "DNS SOA record email wrong"
        Expect.equal dnsRecords.[3].SoaRecord.SerialNumber (Nullable 1L) "DNS SOA record serial number wrong"
        Expect.equal dnsRecords.[3].SoaRecord.MinimumTtl (Nullable 300L) "DNS SOA record minimum ttl wrong"
        Expect.equal dnsRecords.[3].SoaRecord.RefreshTime (Nullable 3600L) "DNS SOA record refresh time wrong"
        Expect.equal dnsRecords.[3].SoaRecord.RetryTime (Nullable 300L) "DNS SOA record retry time wrong"
        Expect.equal dnsRecords.[3].SoaRecord.ExpireTime (Nullable 2419200L) "DNS SOA record expire time wrong"
        Expect.equal dnsRecords.[3].TTL (Nullable 3600L) "DNS record TTL is wrong"

        Expect.equal dnsRecords.[4].Name "farmer.com/_sip._tcp.name" "DNS SRV record name is wrong"
        Expect.equal dnsRecords.[4].SrvRecords.[0].Priority (Nullable 100) "DNS SRV record priority wrong"
        Expect.equal dnsRecords.[4].SrvRecords.[0].Weight (Nullable 1) "DNS SRV record weight wrong"
        Expect.equal dnsRecords.[4].SrvRecords.[0].Port (Nullable 5061) "DNS SRV record port wrong"
        Expect.equal dnsRecords.[4].SrvRecords.[0].Target "farmer.online.com." "DNS SRV record target wrong"
        Expect.equal dnsRecords.[4].TTL (Nullable 3600L) "DNS record TTL is wrong"

        Expect.equal dnsRecords.[5].Name "farmer.com/txtName" "DNS TXT record name is wrong"
        Expect.sequenceEqual dnsRecords.[5].TxtRecords.[0].Value.[0] "somevalue" "DNS TXT record value is wrong"
        Expect.equal dnsRecords.[5].TTL (Nullable 3600L) "DNS record TTL is wrong"
    }
    test "Adding A record to existing zone" {
        let template =
            arm {
                add_resources [
                    aRecord {
                        name "arm"
                        ttl 3600
                        add_ipv4_addresses [ "10.100.200.28" ]
                        link_to_unmanaged_dns_zone (Farmer.Arm.Dns.zones.resourceId "farmer.com")
                    }
                ]
            }
        let jobj = template.Template |> Writer.toJson |> JObject.Parse
        let dependsOn = jobj.SelectToken("resources[?(@.name=='farmer.com/arm')].dependsOn") :?> JArray
        Expect.isEmpty dependsOn "DNS 'A' record linked to existing zone dependsOn."
        let expectedARecordType =
            { ResourceId.Type = ResourceType ("Microsoft.Network/dnsZones/A", "2018-05-01")
              ResourceGroup = None
              Subscription = None
              Name = ResourceName "farmer.com/arm"
              Segments = [] }
        Expect.equal template.Resources.[0].ResourceId expectedARecordType "Incorrect resourceId generated from standalone record builder"
    }
    test "DNS zone depends_on emits 'dependsOn'" {
        let zone =
            dnsZone {
                name "farmer.com"
                depends_on (Farmer.Arm.TrafficManager.profiles.resourceId "foo")
            }
        let template =
            arm {
                add_resources [ zone ]
            }
        let jobj = template.Template |> Writer.toJson |> JObject.Parse
        let zoneDependsOn = jobj.SelectToken("resources[?(@.name=='farmer.com')].dependsOn")
        Expect.isNotNull zoneDependsOn "Zone missing dependsOn"
        let zoneDependsOn = zoneDependsOn :?> JArray |> Seq.map string
        Expect.hasLength zoneDependsOn 1 "Zone should have one dependency"
        Expect.contains zoneDependsOn "[resourceId('Microsoft.Network/trafficManagerProfiles', 'foo')]" "Missing expected resource dependency"
    }
    test "Sequencing DNS record deployment through depends_on" {
        let zone =
            dnsZone {
                name "farmer.com"
            }
        let first = 
            cnameRecord {
                name "first"
                link_to_dns_zone zone
                cname "farmer.com"
                ttl 3600
            }
        let second = 
            cnameRecord {
                name "second"
                link_to_dns_zone zone
                cname "farmer.com"
                depends_on first
                ttl 3600
            }
        let template =
            arm {
                add_resources [ zone; first; second ]
            }
        template |> Writer.quickWrite "dns-cname"
        let jobj = template.Template |> Writer.toJson |> JObject.Parse
        let firstDependsOn = jobj.SelectToken("resources[?(@.name=='farmer.com/first')].dependsOn") :?> JArray |> Seq.map string
        Expect.hasLength firstDependsOn 1 "first 'CNAME' record dependsOn zone only."
        Expect.contains firstDependsOn "[resourceId('Microsoft.Network/dnsZones', 'farmer.com')]" "Missing dependency on zone"
        let secondDependsOn = jobj.SelectToken("resources[?(@.name=='farmer.com/second')].dependsOn") :?> JArray |> Seq.map string
        Expect.hasLength secondDependsOn 2 "second 'CNAME' record linked to first 'CNAME' record dependsOn."
        Expect.contains secondDependsOn "[resourceId('Microsoft.Network/dnsZones', 'farmer.com')]" "Missing dependency on zone"
        Expect.contains secondDependsOn "[resourceId('Microsoft.Network/dnsZones/CNAME', 'farmer.com', 'first')]" "Missing dependency on first 'CNAME' record"
    }
]

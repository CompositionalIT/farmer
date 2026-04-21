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

let client =
    new DnsManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let tests =
    testList "DNS Zone" [
        test "Public DNS Zone is created with a CNAME record" {

            let resources = arm {
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
                                    {
                                        Priority = Some 100
                                        Weight = Some 1
                                        Port = Some 5061
                                        Target = Some "farmer.online.com."
                                    }
                                ]
                            }
                            txtRecord {
                                name "txtName"
                                ttl 3600
                                add_values [ "somevalue" ]
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
            Expect.equal dnsRecords.[1].Type "Microsoft.Network/dnsZones/CNAME" "DNS record type is wrong"
            Expect.equal dnsRecords.[1].CnameRecord.Cname "farmer.com" "DNS CNAME record is wrong"
            Expect.equal dnsRecords.[1].TTL (Nullable 3600L) "DNS record TTL is wrong"

            Expect.equal dnsRecords.[2].Name "farmer.com/aName" "DNS A record name is wrong"
            Expect.equal dnsRecords.[2].Type "Microsoft.Network/dnsZones/A" "DNS record type is wrong"

            Expect.sequenceEqual
                (dnsRecords.[2].ARecords |> Seq.map (fun x -> x.Ipv4Address))
                [ "192.168.0.1" ]
                "DNS A record IP address is wrong"

            Expect.equal dnsRecords.[2].TTL (Nullable(7200L)) "DNS record TTL is wrong"

            Expect.equal dnsRecords.[3].Name "farmer.com/soaName" "DNS SOA record name is wrong"
            Expect.equal dnsRecords.[3].Type "Microsoft.Network/dnsZones/SOA" "DNS record type is wrong"
            Expect.equal dnsRecords.[3].SoaRecord.Host "ns1-09.azure-dns.com." "DNS SOA record host wrong"

            Expect.equal dnsRecords.[3].SoaRecord.Email "azuredns-hostmaster.microsoft.com" "DNS SOA record email wrong"

            Expect.equal dnsRecords.[3].SoaRecord.SerialNumber (Nullable 1L) "DNS SOA record serial number wrong"
            Expect.equal dnsRecords.[3].SoaRecord.MinimumTtl (Nullable 300L) "DNS SOA record minimum ttl wrong"
            Expect.equal dnsRecords.[3].SoaRecord.RefreshTime (Nullable 3600L) "DNS SOA record refresh time wrong"
            Expect.equal dnsRecords.[3].SoaRecord.RetryTime (Nullable 300L) "DNS SOA record retry time wrong"
            Expect.equal dnsRecords.[3].SoaRecord.ExpireTime (Nullable 2419200L) "DNS SOA record expire time wrong"
            Expect.equal dnsRecords.[3].TTL (Nullable 3600L) "DNS record TTL is wrong"

            Expect.equal dnsRecords.[4].Name "farmer.com/_sip._tcp.name" "DNS SRV record name is wrong"
            Expect.equal dnsRecords.[4].Type "Microsoft.Network/dnsZones/SRV" "DNS record type is wrong"
            Expect.equal dnsRecords.[4].SrvRecords.[0].Priority (Nullable 100) "DNS SRV record priority wrong"
            Expect.equal dnsRecords.[4].SrvRecords.[0].Weight (Nullable 1) "DNS SRV record weight wrong"
            Expect.equal dnsRecords.[4].SrvRecords.[0].Port (Nullable 5061) "DNS SRV record port wrong"
            Expect.equal dnsRecords.[4].SrvRecords.[0].Target "farmer.online.com." "DNS SRV record target wrong"
            Expect.equal dnsRecords.[4].TTL (Nullable 3600L) "DNS record TTL is wrong"

            Expect.equal dnsRecords.[5].Name "farmer.com/txtName" "DNS TXT record name is wrong"
            Expect.equal dnsRecords.[5].Type "Microsoft.Network/dnsZones/TXT" "DNS record type is wrong"
            Expect.sequenceEqual dnsRecords.[5].TxtRecords.[0].Value.[0] "somevalue" "DNS TXT record value is wrong"
            Expect.equal dnsRecords.[5].TTL (Nullable 3600L) "DNS record TTL is wrong"
        }
        test "Adding A record to existing zone" {
            let template = arm {
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

            let dependsOn =
                jobj.SelectToken("resources[?(@.name=='farmer.com/arm')].dependsOn") :?> JArray

            Expect.isEmpty dependsOn "DNS 'A' record linked to existing zone dependsOn."

            let expectedARecordType = {
                ResourceId.Type = ResourceType("Microsoft.Network/dnsZones/A", "2018-05-01")
                ResourceGroup = None
                Subscription = None
                Name = ResourceName "farmer.com"
                Segments = [ ResourceName "arm" ]
            }

            Expect.equal
                template.Resources.[0].ResourceId
                expectedARecordType
                "Incorrect resourceId generated from standalone record builder"
        }
        test "DNS zone depends_on emits 'dependsOn'" {
            let zone = dnsZone {
                name "farmer.com"
                depends_on (Farmer.Arm.TrafficManager.profiles.resourceId "foo")
            }

            let template = arm { add_resources [ zone ] }
            let jobj = template.Template |> Writer.toJson |> JObject.Parse
            let zoneDependsOn = jobj.SelectToken("resources[?(@.name=='farmer.com')].dependsOn")
            Expect.isNotNull zoneDependsOn "Zone missing dependsOn"
            let zoneDependsOn = zoneDependsOn :?> JArray |> Seq.map string
            Expect.hasLength zoneDependsOn 1 "Zone should have one dependency"

            Expect.contains
                zoneDependsOn
                "[resourceId('Microsoft.Network/trafficManagerProfiles', 'foo')]"
                "Missing expected resource dependency"
        }
        test "Sequencing DNS record deployment through depends_on" {
            let zone = dnsZone { name "farmer.com" }

            let first = cnameRecord {
                name "first"
                link_to_dns_zone zone
                cname "farmer.com"
                ttl 3600
            }

            let second = cnameRecord {
                name "second"
                link_to_dns_zone zone
                cname "farmer.com"
                depends_on first
                ttl 3600
            }

            let template = arm { add_resources [ zone; first; second ] }
            let jobj = template.Template |> Writer.toJson |> JObject.Parse

            let firstDependsOn =
                jobj.SelectToken("resources[?(@.name=='farmer.com/first')].dependsOn") :?> JArray
                |> Seq.map string

            Expect.hasLength firstDependsOn 1 "first 'CNAME' record dependsOn zone only."

            Expect.contains
                firstDependsOn
                "[resourceId('Microsoft.Network/dnsZones', 'farmer.com')]"
                "Missing dependency on zone"

            let secondDependsOn =
                jobj.SelectToken("resources[?(@.name=='farmer.com/second')].dependsOn") :?> JArray
                |> Seq.map string

            Expect.hasLength secondDependsOn 2 "second 'CNAME' record linked to first 'CNAME' record dependsOn."

            Expect.contains
                secondDependsOn
                "[resourceId('Microsoft.Network/dnsZones', 'farmer.com')]"
                "Missing dependency on zone"

            Expect.contains
                secondDependsOn
                "[resourceId('Microsoft.Network/dnsZones/CNAME', 'farmer.com', 'first')]"
                "Missing dependency on first 'CNAME' record"
        }
        test "Assigning target_resource on DNS record emits correct resource id" {
            let zone = dnsZone { name "farmer.com" }
            let tm = trafficManager { name "my-tm" }

            let targetA = aRecord {
                name "tm-a"
                link_to_dns_zone zone
                ttl 60
                target_resource tm
            }

            let targetCname = cnameRecord {
                name "tm-cname"
                link_to_dns_zone zone
                target_resource tm
                ttl 60
                depends_on targetA
            }

            let template = arm { add_resources [ zone; tm; targetA; targetCname ] }
            let jobj = template.Template |> Writer.toJson |> JObject.Parse

            let tmAresourceId =
                jobj.SelectToken("resources[?(@.name=='farmer.com/tm-a')].properties.targetResource.id")
                |> string

            Expect.equal
                tmAresourceId
                "[resourceId('Microsoft.Network/trafficManagerProfiles', 'my-tm')]"
                "Incorrect ID on target resource"

            let tmAresourceId =
                jobj.SelectToken("resources[?(@.name=='farmer.com/tm-cname')].properties.targetResource.id")
                |> string

            Expect.equal
                tmAresourceId
                "[resourceId('Microsoft.Network/trafficManagerProfiles', 'my-tm')]"
                "Incorrect ID on target resource"
        }
        test "DNS zone get NameServers" {
            let zone = dnsZone { name "farmer.com" }

            let template = arm {
                add_resources [ zone ]
                output "nameservers" zone.NameServers
            }

            let expected =
                "[string(reference(resourceId('Microsoft.Network/dnsZones', 'farmer.com'), '2018-05-01').nameServers)]"

            let jobj = template.Template |> Writer.toJson |> JObject.Parse
            let nsArm = jobj.SelectToken("outputs.nameservers.value").ToString()
            Expect.equal nsArm expected "Nameservers not gotten"
        }
        test "Delegate subdomain to another zone" {
            let nsrecords =
                "[reference(resourceId('Microsoft.Network/dnsZones/NS', 'subdomain.farmer.com', '@'), '2018-05-01').NSRecords]"

            let subdomainZone = dnsZone {
                name "subdomain.farmer.com"
                zone_type Dns.Public

                add_records [
                    aRecord {
                        name "aName"
                        ttl 7200
                        add_ipv4_addresses [ "192.168.0.1"; "192.168.0.2" ]
                    }
                ]
            }

            let template = arm {
                add_resources [
                    subdomainZone
                    // When delegating lookups to another DNS zone, you add an NS record to your existing zone and reference the delegated zone to get it's NSRecords.
                    nsRecord {
                        name "subdomain"
                        link_to_unmanaged_dns_zone (Farmer.Arm.Dns.zones.resourceId "farmer.com")
                        ttl (int (TimeSpan.FromDays 2.).TotalSeconds)
                        add_nsd_reference subdomainZone
                    }
                ]
            }

            let jobj = template.Template |> Writer.toJson |> JObject.Parse

            let delegatedNsRecord =
                jobj.SelectToken("resources[?(@.name=='farmer.com/subdomain')].properties.NSRecords")
                |> string

            Expect.equal
                delegatedNsRecord
                nsrecords
                "Incorrect reference generated for NS record of delegated subdomain."
        }
        test "Delegate subdomain to a zone in another group and subscription" {
            let fakeSubId = "8231b360-0d7f-460c-b421-62146c4716b3"

            let nsrecords =
                $"[reference(resourceId('{fakeSubId}', 'res-group', 'Microsoft.Network/dnsZones/NS', 'subdomain.farmer.com', '@'), '2018-05-01').NSRecords]"

            let template = arm {
                add_resources [
                    nsRecord {
                        name "subdomain"
                        link_to_unmanaged_dns_zone (Farmer.Arm.Dns.zones.resourceId "farmer.com")
                        ttl (int (TimeSpan.FromDays 2.).TotalSeconds)

                        add_nsd_reference (
                            ResourceId.create (
                                Farmer.Arm.Dns.zones,
                                ResourceName "subdomain.farmer.com",
                                "res-group",
                                fakeSubId
                            )
                        )
                    }
                ]
            }

            let jobj = template.Template |> Writer.toJson |> JObject.Parse

            let delegatedNsRecord =
                jobj.SelectToken("resources[?(@.name=='farmer.com/subdomain')].properties.NSRecords")
                |> string

            Expect.equal
                delegatedNsRecord
                nsrecords
                "Incorrect reference generated for NS record of delegated subdomain in different group/subscription."
        }
        test "Disallow adding NSD reference after NSD names are added to prevent overwriting" {
            Expect.throws
                (fun _ ->
                    arm {
                        add_resources [
                            nsRecord {
                                name "subdomain"
                                link_to_unmanaged_dns_zone (Farmer.Arm.Dns.zones.resourceId "farmer.com")
                                ttl (int (TimeSpan.FromDays 2.).TotalSeconds)
                                add_nsd_names [ "ns01.foo.bar " ]
                                add_nsd_reference (Farmer.Arm.Dns.zones.resourceId "subdomain.farmer.com")
                            }
                        ]
                    }
                    |> ignore)
                "Should fail when add_nsd_records was already called"
        }
        test "Disallow adding NSD records after NSD reference to prevent overwriting" {
            Expect.throws
                (fun _ ->
                    arm {
                        add_resources [
                            nsRecord {
                                name "subdomain"
                                link_to_unmanaged_dns_zone (Farmer.Arm.Dns.zones.resourceId "farmer.com")
                                ttl (int (TimeSpan.FromDays 2.).TotalSeconds)
                                add_nsd_reference (Farmer.Arm.Dns.zones.resourceId "subdomain.farmer.com")
                                add_nsd_names [ "ns01.foo.bar " ]
                            }
                        ]
                    }
                    |> ignore)
                "Should fail when add_nsd_reference was already called"
        }
        test "Private DNS Zone is created with records" {
            let resources = arm {
                add_resources [
                    dnsZone {
                        name "farmer.com"
                        zone_type Private

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
                            aaaaRecord {
                                name "aaaaName"
                                ttl 7200
                                add_ipv6_addresses [ "2001:0db8:85a3:0000:0000:8a2e:0370:7334" ]
                            }
                            ptrRecord {
                                name "ptrName"
                                ttl 3600
                                add_ptrd_names [ "farmer.com" ]
                            }
                            txtRecord {
                                name "txtName"
                                ttl 3600
                                add_values [ "somevalue" ]
                            }
                            mxRecord {
                                name "mxName"
                                ttl 7200

                                add_values [
                                    0, "farmer-com.mail.protection.outlook.com"
                                    1, "farmer2-com.mail.protection.outlook.com"
                                ]
                            }
                            srvRecord {
                                name "_sip._tcp.name"
                                ttl 3600

                                add_values [
                                    {
                                        Priority = Some 100
                                        Weight = Some 1
                                        Port = Some 5061
                                        Target = Some "farmer.online.com."
                                    }
                                ]
                            }
                            soaRecord {
                                name "soaName"
                                host "azureprivatedns.net"
                                ttl 3600
                                email "azuredns-hostmaster.microsoft.com"
                                serial_number 1L
                                minimum_ttl 300L
                                refresh_time 3600L
                                retry_time 300L
                                expire_time 2419200L
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
            Expect.equal dnsZones.[0].Type "Microsoft.Network/privateDnsZones" "DNS Zone type is wrong"
            Expect.equal dnsZones.[0].ZoneType (Nullable ZoneType.Private) "DNS Zone ZoneType is wrong"

            let jsn = resources.Template |> Writer.toJson
            let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/www')].type").ToString())
                "Microsoft.Network/privateDnsZones/CNAME"
                "DNS record type is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/www')].dependsOn[0]").ToString())
                "[resourceId('Microsoft.Network/privateDnsZones', 'farmer.com')]"
                "DNS dependsOn is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/www')].properties.cnameRecord.cname").ToString())
                "farmer.com"
                "DNS CNAME record is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/www')].properties.ttl").ToString())
                "3600"
                "DNS TTL is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/aName')].type").ToString())
                "Microsoft.Network/privateDnsZones/A"
                "DNS record type is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/aName')].dependsOn[0]").ToString())
                "[resourceId('Microsoft.Network/privateDnsZones', 'farmer.com')]"
                "DNS dependsOn is wrong"

            Expect.equal
                (jobj
                    .SelectToken("resources[?(@.name=='farmer.com/aName')].properties.aRecords[0].ipv4Address")
                    .ToString())
                "192.168.0.1"
                "DNS A record is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/aName')].properties.ttl").ToString())
                "7200"
                "DNS TTL is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/aaaaName')].type").ToString())
                "Microsoft.Network/privateDnsZones/AAAA"
                "DNS record type is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/aaaaName')].dependsOn[0]").ToString())
                "[resourceId('Microsoft.Network/privateDnsZones', 'farmer.com')]"
                "DNS dependsOn is wrong"

            Expect.equal
                (jobj
                    .SelectToken("resources[?(@.name=='farmer.com/aaaaName')].properties.aaaaRecords[0].ipv6Address")
                    .ToString())
                "2001:0db8:85a3:0000:0000:8a2e:0370:7334"
                "DNS AAAA record is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/aaaaName')].properties.ttl").ToString())
                "7200"
                "DNS TTL is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/ptrName')].type").ToString())
                "Microsoft.Network/privateDnsZones/PTR"
                "DNS record type is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/ptrName')].dependsOn[0]").ToString())
                "[resourceId('Microsoft.Network/privateDnsZones', 'farmer.com')]"
                "DNS dependsOn is wrong"

            Expect.equal
                (jobj
                    .SelectToken("resources[?(@.name=='farmer.com/ptrName')].properties.ptrRecords[0].ptrdname")
                    .ToString())
                "farmer.com"
                "DNS PTR record is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/ptrName')].properties.ttl").ToString())
                "3600"
                "DNS TTL is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/txtName')].type").ToString())
                "Microsoft.Network/privateDnsZones/TXT"
                "DNS record type is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/txtName')].dependsOn[0]").ToString())
                "[resourceId('Microsoft.Network/privateDnsZones', 'farmer.com')]"
                "DNS dependsOn is wrong"

            Expect.equal
                (jobj
                    .SelectToken("resources[?(@.name=='farmer.com/txtName')].properties.txtRecords[0].value[0]")
                    .ToString())
                "somevalue"
                "DNS TXT record is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/txtName')].properties.ttl").ToString())
                "3600"
                "DNS TTL is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/mxName')].type").ToString())
                "Microsoft.Network/privateDnsZones/MX"
                "DNS record type is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/mxName')].dependsOn[0]").ToString())
                "[resourceId('Microsoft.Network/privateDnsZones', 'farmer.com')]"
                "DNS dependsOn is wrong"

            Expect.equal
                (jobj
                    .SelectToken("resources[?(@.name=='farmer.com/mxName')].properties.mxRecords[0].exchange")
                    .ToString())
                "farmer-com.mail.protection.outlook.com"
                "DNS MX record exchange is wrong"

            Expect.equal
                (jobj
                    .SelectToken("resources[?(@.name=='farmer.com/mxName')].properties.mxRecords[0].preference")
                    .ToString())
                "0"
                "DNS MX record preference is wrong"

            Expect.equal
                (jobj
                    .SelectToken("resources[?(@.name=='farmer.com/mxName')].properties.mxRecords[1].exchange")
                    .ToString())
                "farmer2-com.mail.protection.outlook.com"
                "DNS MX record exchange is wrong"

            Expect.equal
                (jobj
                    .SelectToken("resources[?(@.name=='farmer.com/mxName')].properties.mxRecords[1].preference")
                    .ToString())
                "1"
                "DNS MX record preference is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/mxName')].properties.ttl").ToString())
                "7200"
                "DNS TTL is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/_sip._tcp.name')].type").ToString())
                "Microsoft.Network/privateDnsZones/SRV"
                "DNS record type is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/_sip._tcp.name')].dependsOn[0]").ToString())
                "[resourceId('Microsoft.Network/privateDnsZones', 'farmer.com')]"
                "DNS dependsOn is wrong"

            Expect.equal
                (jobj
                    .SelectToken("resources[?(@.name=='farmer.com/_sip._tcp.name')].properties.srvRecords[0].port")
                    .ToString())
                "5061"
                "DNS SRV record port is wrong"

            Expect.equal
                (jobj
                    .SelectToken("resources[?(@.name=='farmer.com/_sip._tcp.name')].properties.srvRecords[0].priority")
                    .ToString())
                "100"
                "DNS SRV record priority is wrong"

            Expect.equal
                (jobj
                    .SelectToken("resources[?(@.name=='farmer.com/_sip._tcp.name')].properties.srvRecords[0].target")
                    .ToString())
                "farmer.online.com."
                "DNS SRV record target is wrong"

            Expect.equal
                (jobj
                    .SelectToken("resources[?(@.name=='farmer.com/_sip._tcp.name')].properties.srvRecords[0].weight")
                    .ToString())
                "1"
                "DNS SRV record weight is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/_sip._tcp.name')].properties.ttl").ToString())
                "3600"
                "DNS TTL is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/soaName')].type").ToString())
                "Microsoft.Network/privateDnsZones/SOA"
                "DNS record type is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/soaName')].dependsOn[0]").ToString())
                "[resourceId('Microsoft.Network/privateDnsZones', 'farmer.com')]"
                "DNS dependsOn is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/soaName')].properties.soaRecord.email").ToString())
                "azuredns-hostmaster.microsoft.com"
                "DNS SOA record email is wrong"

            Expect.equal
                (jobj
                    .SelectToken("resources[?(@.name=='farmer.com/soaName')].properties.soaRecord.expireTime")
                    .ToString())
                "2419200"
                "DNS SOA record expireTime is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/soaName')].properties.soaRecord.host").ToString())
                "azureprivatedns.net"
                "DNS SOA record host is wrong"

            Expect.equal
                (jobj
                    .SelectToken("resources[?(@.name=='farmer.com/soaName')].properties.soaRecord.minimumTTL")
                    .ToString())
                "300"
                "DNS SOA record minimumTTL is wrong"

            Expect.equal
                (jobj
                    .SelectToken("resources[?(@.name=='farmer.com/soaName')].properties.soaRecord.refreshTime")
                    .ToString())
                "3600"
                "DNS SOA record refreshTime is wrong"

            Expect.equal
                (jobj
                    .SelectToken("resources[?(@.name=='farmer.com/soaName')].properties.soaRecord.retryTime")
                    .ToString())
                "300"
                "DNS SOA record retryTime is wrong"

            Expect.equal
                (jobj
                    .SelectToken("resources[?(@.name=='farmer.com/soaName')].properties.soaRecord.serialNumber")
                    .ToString())
                "1"
                "DNS SOA record serialNumber is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='farmer.com/soaName')].properties.ttl").ToString())
                "3600"
                "DNS TTL is wrong"
        }
        test "Disallow adding private NS records" {
            Expect.throws
                (fun _ ->
                    arm {
                        add_resources [
                            dnsZone {
                                name "farmer.com"
                                zone_type Private

                                add_records [
                                    nsRecord {
                                        name "subdomain"
                                        ttl (int (TimeSpan.FromDays 2.).TotalSeconds)
                                        add_nsd_names [ "ns01.foo.bar " ]
                                    }
                                ]
                            }
                        ]
                    }
                    |> ignore)
                "Should fail when adding NS record to private zone"
        }
        test "Can link dns record to unmanaged private DNS zone" {
            let resources = arm {
                add_resources [
                    cnameRecord {
                        name "www"
                        ttl 3600
                        cname "farmer.com"
                        zone_type Private
                        link_to_unmanaged_dns_zone (Farmer.Arm.Dns.zones.resourceId "private.farmer.com")
                    }
                    aRecord {
                        name "aName"
                        ttl 7200
                        add_ipv4_addresses [ "192.168.0.1" ]
                        zone_type Private
                        link_to_unmanaged_dns_zone (Farmer.Arm.Dns.zones.resourceId "private.farmer.com")
                    }
                    aaaaRecord {
                        name "aaaaName"
                        ttl 7200
                        add_ipv6_addresses [ "2001:0db8:85a3:0000:0000:8a2e:0370:7334" ]
                        zone_type Private
                        link_to_unmanaged_dns_zone (Farmer.Arm.Dns.zones.resourceId "private.farmer.com")
                    }
                    ptrRecord {
                        name "ptrName"
                        ttl 3600
                        add_ptrd_names [ "farmer.com" ]
                        zone_type Private
                        link_to_unmanaged_dns_zone (Farmer.Arm.Dns.zones.resourceId "private.farmer.com")
                    }
                    txtRecord {
                        name "txtName"
                        ttl 3600
                        add_values [ "somevalue" ]
                        zone_type Private
                        link_to_unmanaged_dns_zone (Farmer.Arm.Dns.zones.resourceId "private.farmer.com")
                    }
                    mxRecord {
                        name "mxName"
                        ttl 7200

                        add_values [
                            0, "farmer-com.mail.protection.outlook.com"
                            1, "farmer2-com.mail.protection.outlook.com"
                        ]

                        zone_type Private
                        link_to_unmanaged_dns_zone (Farmer.Arm.Dns.zones.resourceId "private.farmer.com")
                    }
                    srvRecord {
                        name "_sip._tcp.name"
                        ttl 3600

                        add_values [
                            {
                                Priority = Some 100
                                Weight = Some 1
                                Port = Some 5061
                                Target = Some "farmer.online.com."
                            }
                        ]

                        zone_type Private
                        link_to_unmanaged_dns_zone (Farmer.Arm.Dns.zones.resourceId "private.farmer.com")
                    }
                    soaRecord {
                        name "soaName"
                        host "azureprivatedns.net"
                        ttl 3600
                        email "azuredns-hostmaster.microsoft.com"
                        serial_number 1L
                        minimum_ttl 300L
                        refresh_time 3600L
                        retry_time 300L
                        expire_time 2419200L
                        zone_type Private
                        link_to_unmanaged_dns_zone (Farmer.Arm.Dns.zones.resourceId "private.farmer.com")
                    }
                ]
            }

            let jsn = resources.Template |> Writer.toJson
            let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='private.farmer.com/www')].type").ToString())
                "Microsoft.Network/privateDnsZones/CNAME"
                "DNS record type is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='private.farmer.com/aName')].type").ToString())
                "Microsoft.Network/privateDnsZones/A"
                "DNS record type is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='private.farmer.com/aaaaName')].type").ToString())
                "Microsoft.Network/privateDnsZones/AAAA"
                "DNS record type is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='private.farmer.com/ptrName')].type").ToString())
                "Microsoft.Network/privateDnsZones/PTR"
                "DNS record type is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='private.farmer.com/txtName')].type").ToString())
                "Microsoft.Network/privateDnsZones/TXT"
                "DNS record type is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='private.farmer.com/mxName')].type").ToString())
                "Microsoft.Network/privateDnsZones/MX"
                "DNS record type is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='private.farmer.com/_sip._tcp.name')].type").ToString())
                "Microsoft.Network/privateDnsZones/SRV"
                "DNS record type is wrong"

            Expect.equal
                (jobj.SelectToken("resources[?(@.name=='private.farmer.com/soaName')].type").ToString())
                "Microsoft.Network/privateDnsZones/SOA"
                "DNS record type is wrong"
        }
        test "Private DNS Zone is linked to vnet" {
            let deployment = arm {
                add_resources [
                    vnet {
                        name "my-net"
                        add_address_spaces [ "10.100.0.0/20" ]

                        add_subnets [
                            subnet {
                                name "net1"
                                prefix "10.100.0.0/24"
                            }
                        ]
                    }
                    dnsZone {
                        name "privnet.net"
                        zone_type Private

                        add_records [
                            aRecord {
                                name "aName"
                                ttl 7200
                                add_ipv4_addresses [ "192.168.0.1" ]
                            }
                        ]
                    }
                    privateDnsZoneVirtualNetworkLink {
                        name "my-net-link"
                        registration_enabled true
                        private_dns_zone (Arm.Dns.privateZones.resourceId "privnet.net")
                        virtual_network_id (Arm.Network.virtualNetworks.resourceId "my-net")
                    }
                ]
            }

            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse
            let vnetLink = jobj.SelectToken("resources[?(@.name=='privnet.net/my-net-link')]")
            Expect.isNotNull vnetLink "Incorrect name for vnet link"

            Expect.equal
                (vnetLink.SelectToken("type"))
                (JValue "Microsoft.Network/privateDnsZones/virtualNetworkLinks")
                "Private DNS zone vnet link type is wrong"

            Expect.hasLength
                (vnetLink.SelectToken("dependsOn"))
                2
                "Private DNS zone vnet link has wrong number of dependencies"

            Expect.contains
                (vnetLink.SelectToken("dependsOn"))
                (JValue "[resourceId('Microsoft.Network/privateDnsZones', 'privnet.net')]")
                "Private DNS zone vnet link missing dependency on DNS zone"

            Expect.contains
                (vnetLink.SelectToken("dependsOn"))
                (JValue "[resourceId('Microsoft.Network/virtualNetworks', 'my-net')]")
                "Private DNS zone vnet link missing dependency on vnet"

            Expect.equal
                (vnetLink.SelectToken("properties.registrationEnabled"))
                (JValue true)
                "Private DNS zone vnet link automatic record registration should be enabled"

            Expect.equal
                (vnetLink.SelectToken("properties.virtualNetwork.id"))
                (JValue "[resourceId('Microsoft.Network/virtualNetworks', 'my-net')]")
                "Private DNS zone vnet link vnet link is incorrect"
        }
    ]
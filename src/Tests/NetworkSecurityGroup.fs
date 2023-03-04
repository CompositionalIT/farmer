module NetworkSecurityGroup

open Expecto
open Farmer
open Farmer.NetworkSecurity
open Farmer.Arm.NetworkSecurityGroup
open Farmer.Builders
open Microsoft.Azure.Management.Network
open Microsoft.Azure.Management.Network.Models
open Microsoft.Rest
open System

let client =
    new NetworkManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let tests =
    testList "NetworkSecurityGroup" [
        test "Can create a network security group in an ARM template" {
            let resource =
                let nsg = {
                    Name = ResourceName "my-nsg"
                    Location = Location.WestEurope
                    SecurityRules = []
                    Tags = Map.empty
                }

                arm { add_resource nsg }
                |> findAzureResources<NetworkSecurityGroup> client.SerializationSettings
                |> List.head

            Expect.equal resource.Name "my-nsg" ""
        }
        test "Can create a network security group with rules in an ARM template" {
            let rules =
                let nsg = {
                    Name = ResourceName "my-nsg"
                    Location = Location.WestEurope
                    SecurityRules = []
                    Tags = Map.empty
                }

                let acceptRule = {
                    Name = ResourceName "accept-web"
                    Description = Some(sprintf "Rule created on %s" (DateTimeOffset.Now.Date.ToShortDateString()))
                    SecurityGroup = nsg.Name
                    Protocol = TCP
                    SourcePorts = Set [ AnyPort ]
                    DestinationPorts = Set [ Port 80us; Port 443us ]
                    SourceAddresses = [ AnyEndpoint ]
                    DestinationAddresses = [ Network(IPAddressCidr.parse "10.100.30.0/24") ]
                    Access = Allow
                    Direction = Inbound
                    Priority = 100
                }

                arm {
                    add_resource nsg
                    add_resource acceptRule
                }
                |> findAzureResources<SecurityRule> client.SerializationSettings

            match rules with
            | [ _; rule1 ] ->
                rule1.Validate()

                Expect.equal rule1.Name "my-nsg/accept-web" ""
                Expect.equal rule1.Access "Allow" ""
                Expect.equal rule1.DestinationAddressPrefixes.[0] "10.100.30.0/24" ""
                Expect.equal rule1.DestinationPortRanges.[0] "80" ""
                Expect.equal rule1.DestinationPortRanges.[1] "443" ""
                Expect.equal rule1.Direction "Inbound" ""
                Expect.equal rule1.Protocol "Tcp" ""
                Expect.equal rule1.Priority (Nullable 100) ""
                Expect.equal rule1.SourceAddressPrefix "*" ""
                Expect.equal rule1.SourcePortRange "*" ""
                Expect.equal rule1.SourcePortRanges.Count 0 ""
                rule1.Validate()
            | _ -> raiseFarmer "Unexpected number of resources in template."
        }
        test "Policy converted to security rules" {
            let webPolicy = securityRule {
                name "web-servers"
                description "Public web server access"
                services [ "http", 80; "https", 443 ]
                add_source_tag TCP "Internet"
                add_destination_network "10.100.30.0/24"
            }

            let myNsg = nsg {
                name "my-nsg"
                add_rules [ webPolicy ]
            }

            let nsg =
                arm { add_resource myNsg }
                |> findAzureResources<NetworkSecurityGroup> client.SerializationSettings

            match nsg.Head.SecurityRules |> List.ofSeq with
            | [ rule1 ] ->
                rule1.Validate()
                Expect.equal rule1.Name "web-servers" ""
                Expect.equal rule1.Access "Allow" ""
                Expect.equal rule1.DestinationAddressPrefixes.[0] "10.100.30.0/24" ""
                Expect.equal rule1.DestinationPortRanges.[0] "80" ""
                Expect.equal rule1.DestinationPortRanges.[1] "443" ""
                Expect.equal rule1.Direction "Inbound" ""
                Expect.equal rule1.Protocol "Tcp" ""
                Expect.equal rule1.Priority (Nullable 100) ""
                Expect.equal rule1.SourceAddressPrefix "Internet" ""
                Expect.equal rule1.SourcePortRange "*" ""
                Expect.equal rule1.SourcePortRanges.Count 0 ""
            | _ -> raiseFarmer "Unexpected number of resources in template."
        }
        test "Multitier Policy converted to security rules" {
            let appNet = "10.100.31.0/24"
            let dbNet = "10.100.32.0/24"

            let webPolicy = securityRule { // Web servers - accessible from anything
                name "web-servers"
                description "Public web server access"
                services [ "http", 80; "https", 443 ]
                add_source_tag TCP "Internet"
                add_destination_network "10.100.30.0/24"
            }

            let appPolicy = securityRule { // Only accessible by web servers
                name "app-servers"
                description "Internal app server access"
                services [ "http", 8080 ]
                add_source_network TCP "10.100.30.0/24"
                add_destination_network appNet
            }

            let dbPolicy = securityRule { // DB servers - not accessible by web, only by app servers
                name "db-servers"
                description "Internal database server access"
                services [ "postgres", 5432 ]
                add_source_network TCP appNet
                add_destination_network dbNet
            }

            let blockOutbound = securityRule {
                name "no-internet"
                description "Block traffic out to internet"
                add_source_network AnyProtocol appNet
                add_source_network AnyProtocol dbNet
                add_destination_tag "Internet"
                direction Outbound
                deny_traffic
            }

            let myNsg = nsg {
                name "my-nsg"
                add_rules [ webPolicy; appPolicy; dbPolicy; blockOutbound ]
                initial_rule_priority 1000
                priority_incr 50
            }

            let nsg =
                arm { add_resource myNsg }
                |> findAzureResources<NetworkSecurityGroup> client.SerializationSettings

            match nsg.Head.SecurityRules |> List.ofSeq with
            | [ rule1; rule2; rule3; rule4 ] ->
                // Web server access
                rule1.Validate()
                Expect.equal rule1.Name "web-servers" ""
                Expect.equal rule1.Access "Allow" ""
                Expect.equal rule1.DestinationAddressPrefixes.[0] "10.100.30.0/24" ""
                Expect.equal rule1.DestinationPortRanges.[0] "80" ""
                Expect.equal rule1.DestinationPortRanges.[1] "443" ""
                Expect.equal rule1.Direction "Inbound" ""
                Expect.equal rule1.Protocol "Tcp" ""
                Expect.equal rule1.Priority (Nullable 1000) ""
                Expect.equal rule1.SourceAddressPrefix "Internet" ""
                Expect.equal rule1.SourceAddressPrefixes.Count 0 ""
                Expect.equal rule1.SourcePortRange "*" ""
                Expect.equal rule1.SourcePortRanges.Count 0 ""
                // App server access
                rule2.Validate()
                Expect.equal rule2.Name "app-servers" ""
                Expect.equal rule2.Access "Allow" ""
                Expect.equal rule2.DestinationAddressPrefixes.[0] "10.100.31.0/24" ""
                Expect.equal rule2.DestinationPortRanges.[0] "8080" ""
                Expect.equal rule2.Direction "Inbound" ""
                Expect.equal rule2.Protocol "Tcp" ""
                Expect.equal rule2.Priority (Nullable 1050) ""
                Expect.equal rule2.SourceAddressPrefixes.[0] "10.100.30.0/24" ""
                Expect.equal rule2.SourcePortRanges.Count 0 ""
                // DB server access
                rule3.Validate()
                Expect.equal rule3.Name "db-servers" ""
                Expect.equal rule3.Access "Allow" ""
                Expect.equal rule3.DestinationAddressPrefixes.[0] "10.100.32.0/24" ""
                Expect.equal rule3.DestinationPortRanges.[0] "5432" ""
                Expect.equal rule3.Direction "Inbound" ""
                Expect.equal rule3.Protocol "Tcp" ""
                Expect.equal rule3.Priority (Nullable 1100) ""
                Expect.equal rule3.SourceAddressPrefixes.[0] "10.100.31.0/24" ""
                Expect.equal rule3.SourcePortRanges.Count 0 ""
                // Block Internet access
                rule4.Validate()
                Expect.equal rule4.Name "no-internet" ""
                Expect.equal rule4.Access "Deny" ""
                Expect.equal rule4.DestinationAddressPrefix "Internet" ""
                Expect.equal rule4.DestinationPortRange "*" ""
                Expect.equal rule4.Direction "Outbound" ""
                Expect.equal rule4.Protocol "*" ""
                Expect.equal rule4.Priority (Nullable 1150) ""
                Expect.containsAll rule4.SourceAddressPrefixes [ "10.100.31.0/24"; "10.100.32.0/24" ] ""
            | _ -> raiseFarmer "Unexpected number of resources in template."
        }
    ]

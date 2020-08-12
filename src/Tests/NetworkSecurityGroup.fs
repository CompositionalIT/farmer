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

let client = new NetworkManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")
let tests = testList "NetworkSecurityGroup" [
    test "Can create a network security group in an ARM template" {
        let resource =
            let nsg =
                { Name = ResourceName "my-nsg"
                  Location = Location.WestEurope
                  Tags = Map.empty }
            arm { add_resource nsg }
            |> findAzureResources<NetworkSecurityGroup> client.SerializationSettings
            |> List.head
        Expect.equal resource.Name "my-nsg" ""
    }
    test "Can create a network security group with rules in an ARM template" {
        let rules =
            let nsg =
                { Name = ResourceName "my-nsg"
                  Location = Location.WestEurope
                  Tags = Map.empty }
            let acceptRule =
                { Name = ResourceName "accept-web"
                  Description = Some (sprintf "Rule created on %s" (DateTimeOffset.Now.Date.ToShortDateString()))
                  SecurityGroup = nsg
                  Protocol = TCP
                  SourcePort = Some AnyPort
                  SourcePorts = [ ]
                  DestinationPort = None
                  DestinationPorts = [ Port 80us; Port 443us ]
                  SourceAddress = Some AnyEndpoint
                  SourceAddresses = [ ]
                  DestinationAddress = None
                  DestinationAddresses = [ Network (IPAddressCidr.parse "10.100.30.0/24") ]
                  Access = Allow
                  Direction = Inbound
                  Priority = 100
                } :> IArmResource
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
        | _ -> failwithf "Unexpected number of resources in template."
    }
    test "Policy converted to security rules" {
        let web = Services ("web", [
            Service ("http", Port 80us)
            Service ("https", Port 443us)
        ])
        let webPolicy = securityRule {
            name "web-servers"
            description "Public web server access"
            service web
            add_source_tag TCP "Internet"
            add_destination_network "10.100.30.0/24"
            allow
        }
        let myNsg = nsg {
            name "my-nsg"
            add_rules [ webPolicy ]
        }
        let rules = arm { add_resource myNsg } |> findAzureResources<SecurityRule> client.SerializationSettings
        match rules with
        | [ _; rule1 ] ->
            rule1.Validate()
            Expect.equal rule1.Name "my-nsg/web-servers" ""
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
        | _ -> failwithf "Unexpected number of resources in template."
    }
    test "Multitier Policy converted to security rules" {
        let web = Services ("web", [
            Service ("http", Port 80us)
            Service ("https", Port 443us)
        ])
        let webNet = "10.100.30.0/24"
        let app = Service ("http", Port 8080us)
        let appNet = "10.100.31.0/24"
        let database = Service ("postgres", Port 5432us)
        let dbNet = "10.100.32.0/24"
        let webPolicy = securityRule { // Web servers - accessible from anything
            name "web-servers"
            description "Public web server access"
            service web
            add_source_tag TCP "Internet"
            add_destination_network "10.100.30.0/24"
            allow
        }
        let appPolicy = securityRule { // Only accessible by web servers
            name "app-servers"
            description "Internal app server access"
            service app
            add_source_network TCP webNet
            add_destination_network appNet
            allow
        }
        let dbPolicy = securityRule { // DB servers - not accessible by web, only by app servers
            name "db-servers"
            description "Internal database server access"
            service database
            add_source_network TCP appNet
            add_destination_network dbNet
            allow
        }
        let myNsg = nsg {
            name "my-nsg"
            add_rules [
                webPolicy
                appPolicy
                dbPolicy
            ]
        }
        let rules = arm { add_resource myNsg } |> findAzureResources<SecurityRule> client.SerializationSettings
        match rules with
        | [ _; rule1; rule2; rule3 ] ->
            // Web server access
            rule1.Validate()
            Expect.equal rule1.Name "my-nsg/web-servers" ""
            Expect.equal rule1.Access "Allow" ""
            Expect.equal rule1.DestinationAddressPrefixes.[0] "10.100.30.0/24" ""
            Expect.equal rule1.DestinationPortRanges.[0] "80" ""
            Expect.equal rule1.DestinationPortRanges.[1] "443" ""
            Expect.equal rule1.Direction "Inbound" ""
            Expect.equal rule1.Protocol "Tcp" ""
            Expect.equal rule1.Priority (Nullable 100) ""
            Expect.equal rule1.SourceAddressPrefix "Internet" ""
            Expect.equal rule1.SourceAddressPrefixes.Count 0 ""
            Expect.equal rule1.SourcePortRange "*" ""
            Expect.equal rule1.SourcePortRanges.Count 0 ""
            // App server access
            rule2.Validate()
            Expect.equal rule2.Name "my-nsg/app-servers" ""
            Expect.equal rule2.Access "Allow" ""
            Expect.equal rule2.DestinationAddressPrefixes.[0] "10.100.31.0/24" ""
            Expect.equal rule2.DestinationPortRanges.[0] "8080" ""
            Expect.equal rule2.Direction "Inbound" ""
            Expect.equal rule2.Protocol "Tcp" ""
            Expect.equal rule2.Priority (Nullable 200) ""
            Expect.equal rule2.SourceAddressPrefixes.[0] "10.100.30.0/24" ""
            Expect.equal rule1.SourcePortRanges.Count 0 ""
            // DB server access
            rule3.Validate()
            Expect.equal rule3.Name "my-nsg/db-servers" ""
            Expect.equal rule3.Access "Allow" ""
            Expect.equal rule3.DestinationAddressPrefixes.[0] "10.100.32.0/24" ""
            Expect.equal rule3.DestinationPortRanges.[0] "5432" ""
            Expect.equal rule3.Direction "Inbound" ""
            Expect.equal rule3.Protocol "Tcp" ""
            Expect.equal rule3.Priority (Nullable 300) ""
            Expect.equal rule3.SourceAddressPrefixes.[0] "10.100.31.0/24" ""
            Expect.equal rule1.SourcePortRanges.Count 0 ""
        | _ -> failwithf "Unexpected number of resources in template."
    }
]

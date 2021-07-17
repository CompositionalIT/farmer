module AzureFirewall

open System
open Expecto
open Farmer
open Farmer.Arm.AzureFirewall
open Farmer.Builders

let tests = testList "Azure Firewall" [
    test "Link to builder" {
        let existingFwPolicy =
            { new IBuilder with
                member _.ResourceId = azureFirewallPolicies.resourceId "existing-firewall-policy"
                member _.BuildResources _ = []
            }
        let vwan = vwan {
            name "my-vwan"
            standard_vwan
        }
        let vhub = vhub {
            name "my-vhub"
            address_prefix (IPAddressCidr.parse "100.73.255.0/24")
            link_to_vwan vwan
        }
        let fw = azureFirewall {
            name "azfw"
            sku AzureFirewall.SkuName.AZFW_Hub AzureFirewall.SkuTier.Standard
            public_ip_reservation_count 2
            link_to_vhub vhub
            link_to_firewall_policy existingFwPolicy
        }
        Expect.equal fw.FirewallPolicy.Value.ResourceId (azureFirewallPolicies.resourceId "existing-firewall-policy") "Expected to be linked to existing FW policy"
        Expect.isEmpty fw.Dependencies "Expected not to depend on any resources since it links to an existing policy"
    }
]

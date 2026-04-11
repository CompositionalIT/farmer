module NetworkManager

open Expecto
open Farmer
open Farmer.Arm.NetworkManager
open Farmer.Builders
open Newtonsoft.Json.Linq

let tests =
    testList "Network Manager Tests" [
        test "Can create a basic network manager" {
            let manager = networkManager {
                name "my-manager"
                description "My network manager"
                add_scope_subscription "/subscriptions/00000000-0000-0000-0000-000000000000"
                add_scope_access SecurityAdmin
            }

            let template = arm { add_resource manager }
            let json = template.Template |> Writer.toJson
            let jobj = JObject.Parse json
            let resources = jobj.["resources"] :?> JArray

            let managerResource =
                resources
                |> Seq.find (fun r -> r.["type"].ToString() = "Microsoft.Network/networkManagers")

            Expect.equal (managerResource.["name"].ToString()) "my-manager" "Network manager name should match"

            Expect.equal
                (managerResource.["properties"].["description"].ToString())
                "My network manager"
                "Description should match"

            let scopes =
                managerResource.["properties"].["networkManagerScopeAccesses"] :?> JArray

            Expect.equal (scopes.[0].ToString()) "SecurityAdmin" "Scope access should be SecurityAdmin"

            let subscriptions =
                managerResource.["properties"].["networkManagerScopes"].["subscriptions"] :?> JArray

            Expect.equal
                (subscriptions.[0].ToString())
                "/subscriptions/00000000-0000-0000-0000-000000000000"
                "Subscription scope should match"
        }

        test "Can create a network manager with network groups" {
            let group1 = networkManagerGroup {
                name "prod-vnets"
                description "Production VNets"
            }

            let group2 = networkManagerGroup {
                name "dev-vnets"
                description "Development VNets"
            }

            let manager = networkManager {
                name "my-manager"
                add_scope_subscription "/subscriptions/00000000-0000-0000-0000-000000000000"
                add_scope_access SecurityAdmin
                add_network_groups [ group1; group2 ]
            }

            let template = arm { add_resource manager }
            let json = template.Template |> Writer.toJson
            let jobj = JObject.Parse json
            let resources = jobj.["resources"] :?> JArray

            let groupResources =
                resources
                |> Seq.filter (fun r -> r.["type"].ToString() = "Microsoft.Network/networkManagers/networkGroups")
                |> Seq.toList

            Expect.equal groupResources.Length 2 "Should create two network groups"

            let groupNames = groupResources |> List.map (fun r -> r.["name"].ToString())
            Expect.contains groupNames "my-manager/prod-vnets" "Should have prod-vnets group"
            Expect.contains groupNames "my-manager/dev-vnets" "Should have dev-vnets group"
        }

        test "Can create a security admin configuration with rules" {
            let prodGroup = networkManagerGroup {
                name "prod-vnets"
                description "Production VNets"
            }

            let denyInternetInbound = networkManagerSecurityAdminRule {
                name "deny-internet-inbound"
                description "Deny inbound traffic from Internet"
                priority 100
                direction SecurityAdmin.Inbound
                deny_traffic
                protocol SecurityAdmin.AnyProtocol
                add_source_service_tag "Internet"
                add_destination_ip_prefix "10.0.0.0/8"
                add_destination_port_range "22"
                add_destination_port_range "3389"
            }

            let allowVnetInbound = networkManagerSecurityAdminRule {
                name "allow-vnet-inbound"
                description "Allow inbound from VirtualNetwork"
                priority 200
                direction SecurityAdmin.Inbound
                allow_traffic
                protocol SecurityAdmin.AnyProtocol
                add_source_service_tag "VirtualNetwork"
                add_destination_service_tag "VirtualNetwork"
            }

            let ruleCollection = networkManagerSecurityAdminRuleCollection {
                name "baseline-rules"
                description "Baseline security rules"
                add_rules [ denyInternetInbound; allowVnetInbound ]
            }

            let secAdminConfig = networkManagerSecurityAdminConfiguration {
                name "baseline-config"
                description "Baseline security configuration"
                add_rule_collections [ ruleCollection ]
            }

            let manager = networkManager {
                name "my-manager"
                add_scope_subscription "/subscriptions/00000000-0000-0000-0000-000000000000"
                add_scope_access SecurityAdmin
                add_network_groups [ prodGroup ]
                add_security_admin_configurations [ secAdminConfig ]
            }

            let template = arm { add_resource manager }
            let json = template.Template |> Writer.toJson
            let jobj = JObject.Parse json
            let resources = jobj.["resources"] :?> JArray

            // Verify security admin configuration
            let configResource =
                resources
                |> Seq.tryFind (fun r ->
                    r.["type"].ToString() = "Microsoft.Network/networkManagers/securityAdminConfigurations")

            Expect.isSome configResource "Should create a security admin configuration"

            let config = configResource.Value
            Expect.equal (config.["name"].ToString()) "my-manager/baseline-config" "Config name should match"

            // Verify rule collection
            let ruleCollectionResource =
                resources
                |> Seq.tryFind (fun r ->
                    r.["type"].ToString() = "Microsoft.Network/networkManagers/securityAdminConfigurations/ruleCollections")

            Expect.isSome ruleCollectionResource "Should create a rule collection"

            let rc = ruleCollectionResource.Value

            Expect.equal
                (rc.["name"].ToString())
                "my-manager/baseline-config/baseline-rules"
                "Rule collection name should match"

            // Verify rules
            let ruleResources =
                resources
                |> Seq.filter (fun r ->
                    r.["type"].ToString() = "Microsoft.Network/networkManagers/securityAdminConfigurations/ruleCollections/rules")
                |> Seq.toList

            Expect.equal ruleResources.Length 2 "Should create 2 rules"

            let denyRule =
                ruleResources
                |> List.find (fun r -> r.["name"].ToString().EndsWith("deny-internet-inbound"))

            Expect.equal (denyRule.["kind"].ToString()) "Custom" "Rule kind should be Custom"

            Expect.equal (denyRule.["properties"].["access"].ToString()) "Deny" "Rule access should be Deny"

            Expect.equal (denyRule.["properties"].["direction"].ToString()) "Inbound" "Rule direction should be Inbound"

            Expect.equal (denyRule.["properties"].["priority"].ToString()) "100" "Rule priority should be 100"

            let denyRuleSources = denyRule.["properties"].["sources"] :?> JArray

            Expect.equal denyRuleSources.Count 1 "Deny rule should have one source"

            Expect.equal
                (denyRuleSources.[0].["addressPrefix"].ToString())
                "Internet"
                "Source should be Internet service tag"

            Expect.equal
                (denyRuleSources.[0].["addressPrefixType"].ToString())
                "ServiceTag"
                "Source type should be ServiceTag"

            let denyRuleDestinations = denyRule.["properties"].["destinations"] :?> JArray

            Expect.equal denyRuleDestinations.Count 1 "Deny rule should have one destination"

            Expect.equal
                (denyRuleDestinations.[0].["addressPrefix"].ToString())
                "10.0.0.0/8"
                "Destination should be IP prefix"

            Expect.equal
                (denyRuleDestinations.[0].["addressPrefixType"].ToString())
                "IPPrefix"
                "Destination type should be IPPrefix"

            let denyRuleDestPorts = denyRule.["properties"].["destinationPortRanges"] :?> JArray

            Expect.equal denyRuleDestPorts.Count 2 "Deny rule should have two destination port ranges"

            Expect.containsAll
                (denyRuleDestPorts |> Seq.map (fun x -> x.ToString()))
                [ "22"; "3389" ]
                "Port ranges should include 22 and 3389"
        }

        test "Security admin rule with AlwaysAllow access" {
            let rule = networkManagerSecurityAdminRule {
                name "always-allow-azure-lb"
                priority 50
                always_allow_traffic
                add_source_service_tag "AzureLoadBalancer"
                add_destination_ip_prefix "*"
            }

            let ruleCollectionId =
                Managed(securityAdminRuleCollections.resourceId (ResourceName "my-manager/my-config/my-collection"))

            let builtRule = rule.BuildRule ruleCollectionId
            let json = (builtRule :> IArmResource).JsonModel |> Serialization.toJson
            let jobj = JObject.Parse json
            Expect.equal (jobj.["kind"].ToString()) "Custom" "Kind should be Custom"

            Expect.equal (jobj.["properties"].["access"].ToString()) "AlwaysAllow" "Access should be AlwaysAllow"
        }

        test "Security admin rule defaults to wildcard ports when not specified" {
            let rule = networkManagerSecurityAdminRule {
                name "default-ports"
                priority 100
                deny_traffic
                add_source_service_tag "Internet"
                add_destination_ip_prefix "10.0.0.0/8"
            }

            let ruleCollectionId =
                Managed(securityAdminRuleCollections.resourceId (ResourceName "my-manager/my-config/my-collection"))

            let builtRule = rule.BuildRule ruleCollectionId
            let json = (builtRule :> IArmResource).JsonModel |> Serialization.toJson
            let jobj = JObject.Parse json
            let srcPorts = jobj.["properties"].["sourcePortRanges"] :?> JArray
            let dstPorts = jobj.["properties"].["destinationPortRanges"] :?> JArray
            Expect.equal (srcPorts.[0].ToString()) "*" "Source port should default to *"
            Expect.equal (dstPorts.[0].ToString()) "*" "Destination port should default to *"
        }

        test "Can create a network manager with multiple scope subscriptions" {
            let manager = networkManager {
                name "multi-sub-manager"

                add_scope_subscriptions [
                    "/subscriptions/00000000-0000-0000-0000-000000000001"
                    "/subscriptions/00000000-0000-0000-0000-000000000002"
                ]

                add_scope_access SecurityAdmin
                add_scope_access Connectivity
            }

            let template = arm { add_resource manager }
            let json = template.Template |> Writer.toJson
            let jobj = JObject.Parse json
            let resources = jobj.["resources"] :?> JArray

            let managerResource =
                resources
                |> Seq.find (fun r -> r.["type"].ToString() = "Microsoft.Network/networkManagers")

            let subscriptions =
                managerResource.["properties"].["networkManagerScopes"].["subscriptions"] :?> JArray

            Expect.equal subscriptions.Count 2 "Should have 2 subscriptions in scope"

            let accesses =
                managerResource.["properties"].["networkManagerScopeAccesses"] :?> JArray

            Expect.equal accesses.Count 2 "Should have 2 scope accesses"

            Expect.containsAll
                (accesses |> Seq.map (fun x -> x.ToString()))
                [ "SecurityAdmin"; "Connectivity" ]
                "Should have SecurityAdmin and Connectivity"
        }

        test "Network manager has correct dependencies on child resources" {
            let group = networkManagerGroup {
                name "my-group"
                description "My group"
            }

            let rule = networkManagerSecurityAdminRule {
                name "my-rule"
                priority 100
                deny_traffic
                add_source_service_tag "Internet"
                add_destination_ip_prefix "10.0.0.0/8"
            }

            let ruleCollection = networkManagerSecurityAdminRuleCollection {
                name "my-collection"
                add_rules [ rule ]
            }

            let config = networkManagerSecurityAdminConfiguration {
                name "my-config"
                add_rule_collections [ ruleCollection ]
            }

            let manager = networkManager {
                name "my-manager"
                add_scope_subscription "/subscriptions/00000000-0000-0000-0000-000000000000"
                add_scope_access SecurityAdmin
                add_network_groups [ group ]
                add_security_admin_configurations [ config ]
            }

            let template = arm { add_resource manager }
            let json = template.Template |> Writer.toJson
            let jobj = JObject.Parse json
            let resources = jobj.["resources"] :?> JArray

            // Total resources: 1 manager + 1 group + 1 config + 1 collection + 1 rule = 5
            Expect.equal (resources.Count) 5 "Should have 5 resources in total"
        }
    ]
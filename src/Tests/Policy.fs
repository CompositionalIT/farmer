module Policy

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Arm.Policy
open Newtonsoft.Json.Linq

let tests =
    testList "Azure Policy" [
        test "Creates a basic policy definition" {
            let policyRule =
                """{
                    "if": {
                        "field": "location",
                        "notIn": ["eastus", "westus"]
                    },
                    "then": {
                        "effect": "deny"
                    }
                }"""

            let policy =
                policyDefinition {
                    name "location-restriction-policy"
                    display_name "Restrict deployment locations"
                    description "This policy restricts resource deployments to specific Azure regions"
                    mode PolicyMode.All
                    policy_rule policyRule
                }

            let deployment = arm { add_resources [ policy ] }
            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse

            let policyResource =
                jobj.SelectToken("resources[?(@.type=='Microsoft.Authorization/policyDefinitions')]")

            Expect.isNotNull policyResource "Policy definition resource should exist"
            Expect.equal (policyResource.SelectToken("name").ToString()) "location-restriction-policy" "Name should be correct"

            Expect.equal
                (policyResource.SelectToken("properties.displayName").ToString())
                "Restrict deployment locations"
                "Display name should be correct"

            Expect.equal
                (policyResource.SelectToken("properties.mode").ToString())
                "All"
                "Mode should be correct"

            Expect.isNotNull (policyResource.SelectToken("properties.policyRule")) "Policy rule should exist"
        }

        test "Creates a policy assignment" {
            let policyRule =
                """{
                    "if": {
                        "field": "type",
                        "equals": "Microsoft.Storage/storageAccounts"
                    },
                    "then": {
                        "effect": "audit"
                    }
                }"""

            let policyDef =
                policyDefinition {
                    name "audit-storage-policy"
                    display_name "Audit Storage Accounts"
                    policy_rule policyRule
                }

            let assignment =
                policyAssignment {
                    name "audit-storage-assignment"
                    display_name "Audit Storage Assignment"
                    link_to_policy policyDef
                    enforcement_mode EnforcementMode.Default
                }

            let deployment = arm { add_resources [ policyDef; assignment ] }
            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse

            let assignmentResource =
                jobj.SelectToken("resources[?(@.type=='Microsoft.Authorization/policyAssignments')]")

            Expect.isNotNull assignmentResource "Policy assignment resource should exist"

            Expect.equal
                (assignmentResource.SelectToken("name").ToString())
                "audit-storage-assignment"
                "Assignment name should be correct"

            Expect.equal
                (assignmentResource.SelectToken("properties.enforcementMode").ToString())
                "Default"
                "Enforcement mode should be correct"

            Expect.isNotNull
                (assignmentResource.SelectToken("properties.policyDefinitionId"))
                "Policy definition ID should be set"
        }

        test "Policy assignment with DoNotEnforce mode" {
            let policyRule =
                """{
                    "if": {
                        "field": "tags.environment",
                        "exists": "false"
                    },
                    "then": {
                        "effect": "deny"
                    }
                }"""

            let policyDef =
                policyDefinition {
                    name "require-env-tag-policy"
                    policy_rule policyRule
                }

            let assignment =
                policyAssignment {
                    name "require-env-tag-test"
                    link_to_policy policyDef
                    enforcement_mode EnforcementMode.DoNotEnforce
                }

            let deployment = arm { add_resources [ policyDef; assignment ] }
            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse

            let assignmentResource =
                jobj.SelectToken("resources[?(@.type=='Microsoft.Authorization/policyAssignments')]")

            Expect.equal
                (assignmentResource.SelectToken("properties.enforcementMode").ToString())
                "DoNotEnforce"
                "Enforcement mode should be DoNotEnforce"
        }

        test "Policy definition has correct resource ID" {
            let policyRule =
                """{
                    "if": {
                        "field": "type",
                        "equals": "Microsoft.Compute/virtualMachines"
                    },
                    "then": {
                        "effect": "audit"
                    }
                }"""

            let policy =
                policyDefinition {
                    name "test-policy"
                    policy_rule policyRule
                }

            let resourceId = (policy :> IBuilder).ResourceId

            Expect.equal resourceId.Type.Type "Microsoft.Authorization/policyDefinitions" "Type should be correct"
            Expect.equal resourceId.Name.Value "test-policy" "Name should be correct"
        }

        test "Policy assignment depends on policy definition" {
            let policyRule =
                """{
                    "if": {
                        "field": "type",
                        "equals": "Microsoft.Network/virtualNetworks"
                    },
                    "then": {
                        "effect": "audit"
                    }
                }"""

            let policyDef =
                policyDefinition {
                    name "audit-vnets-policy"
                    policy_rule policyRule
                }

            let assignment =
                policyAssignment {
                    name "audit-vnets-assignment"
                    link_to_policy policyDef
                }

            let deployment = arm { add_resources [ policyDef; assignment ] }
            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse

            let assignmentResource =
                jobj.SelectToken("resources[?(@.type=='Microsoft.Authorization/policyAssignments')]")

            let dependsOn = assignmentResource.SelectToken("dependsOn")
            Expect.isNotNull dependsOn "DependsOn should exist"
            Expect.isTrue (dependsOn.ToString().Contains("audit-vnets-policy")) "Should depend on policy definition"
        }

        test "Policy definition with metadata" {
            let policyRule =
                """{
                    "if": {
                        "field": "type",
                        "equals": "Microsoft.Compute/virtualMachines"
                    },
                    "then": {
                        "effect": "audit"
                    }
                }"""

            let policy =
                policyDefinition {
                    name "vm-audit-policy"
                    policy_rule policyRule
                    add_metadata
                        (Map.ofList
                            [ "category", "Compute"
                              "version", "1.0.0" ])
                }

            let deployment = arm { add_resources [ policy ] }
            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse

            let policyResource =
                jobj.SelectToken("resources[?(@.type=='Microsoft.Authorization/policyDefinitions')]")

            let metadata = policyResource.SelectToken("properties.metadata")
            Expect.isNotNull metadata "Metadata should exist"
            Expect.equal (metadata.SelectToken("category").ToString()) "Compute" "Category should be correct"
            Expect.equal (metadata.SelectToken("version").ToString()) "1.0.0" "Version should be correct"
        }

        test "Policy assignment with system-assigned identity" {
            let policyRule =
                """{
                    "if": {
                        "field": "type",
                        "equals": "Microsoft.Storage/storageAccounts"
                    },
                    "then": {
                        "effect": "deployIfNotExists",
                        "details": {
                            "type": "Microsoft.Insights/diagnosticSettings",
                            "name": "setByPolicy"
                        }
                    }
                }"""

            let policyDef =
                policyDefinition {
                    name "deploy-storage-diagnostics"
                    policy_rule policyRule
                }

            let assignment =
                policyAssignment {
                    name "deploy-storage-diagnostics-assignment"
                    link_to_policy policyDef
                    system_identity
                }

            let deployment = arm { add_resources [ policyDef; assignment ] }
            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse

            let assignmentResource =
                jobj.SelectToken("resources[?(@.type=='Microsoft.Authorization/policyAssignments')]")

            let identity = assignmentResource.SelectToken("identity")
            Expect.isNotNull identity "Identity should exist"
            Expect.equal (identity.SelectToken("type").ToString()) "SystemAssigned" "Identity type should be SystemAssigned"
        }
    ]

module ConfigurationStore

open Expecto
open Farmer
open Farmer.Builders
open Farmer.ConfigurationStore
open Farmer.Arm
open TestHelpers

let private asStoreJson (arm: IArmResource) =
    arm.JsonModel
    |> convertTo<
        {|
            sku: {| name: string |}
            properties:
                {|
                    disableLocalAuth: System.Nullable<bool>
                    enablePurgeProtection: System.Nullable<bool>
                    publicNetworkAccess: string
                    softDeleteRetentionInDays: System.Nullable<int>
                    dataPlaneProxy: obj
                |}
        |}
        >

let private asKeyValueJson (arm: IArmResource) =
    arm.JsonModel
    |> convertTo<
        {|
            name: string
            properties:
                {|
                    value: string
                    contentType: string
                    tags: System.Collections.Generic.Dictionary<string, string>
                |}
        |}
        >

let tests =
    testList "App Configuration" [
        test "Can create a basic configuration store with Free SKU" {
            let store = configurationStore { name "my-app-config" }

            let resources = (store :> IBuilder).BuildResources Location.WestEurope
            Expect.equal resources.Length 1 "Should have exactly one resource"

            let arm = resources.[0]
            Expect.equal arm.ResourceId.Name (ResourceName "my-app-config") "Name should match"

            let json = asStoreJson arm
            Expect.equal json.sku.name "free" "SKU should be free by default"
            Expect.isFalse json.properties.disableLocalAuth.HasValue "disableLocalAuth should not be set"
            Expect.isFalse json.properties.enablePurgeProtection.HasValue "enablePurgeProtection should not be set"
            Expect.isNull json.properties.publicNetworkAccess "publicNetworkAccess should not be set"

            Expect.isFalse
                json.properties.softDeleteRetentionInDays.HasValue
                "softDeleteRetentionInDays should not be set"

            Expect.isNull json.properties.dataPlaneProxy "dataPlaneProxy should not be set"
        }

        test "Can create a configuration store with Standard SKU" {
            let store = configurationStore {
                name "my-app-config"
                sku Standard
            }

            let resources = (store :> IBuilder).BuildResources Location.WestEurope
            let json = asStoreJson resources.[0]
            Expect.equal json.sku.name "standard" "SKU should be standard"
        }

        test "Can create a configuration store with Developer SKU" {
            let store = configurationStore {
                name "my-app-config"
                sku Developer
            }

            let resources = (store :> IBuilder).BuildResources Location.WestEurope
            let json = asStoreJson resources.[0]
            Expect.equal json.sku.name "developer" "SKU should be developer"
        }

        test "Can disable local auth" {
            let store = configurationStore {
                name "my-app-config"
                sku Standard
                disable_local_auth
            }

            let resources = (store :> IBuilder).BuildResources Location.WestEurope
            let json = asStoreJson resources.[0]
            Expect.isTrue json.properties.disableLocalAuth.HasValue "disableLocalAuth should be set"
            Expect.isTrue json.properties.disableLocalAuth.Value "disableLocalAuth should be true"
        }

        test "Can enable purge protection" {
            let store = configurationStore {
                name "my-app-config"
                sku Standard
                enable_purge_protection
            }

            let resources = (store :> IBuilder).BuildResources Location.WestEurope
            let json = asStoreJson resources.[0]
            Expect.isTrue json.properties.enablePurgeProtection.HasValue "enablePurgeProtection should be set"
            Expect.isTrue json.properties.enablePurgeProtection.Value "enablePurgeProtection should be true"
        }

        test "Can set public network access to Disabled" {
            let store = configurationStore {
                name "my-app-config"
                public_network_access Disabled
            }

            let resources = (store :> IBuilder).BuildResources Location.WestEurope
            let json = asStoreJson resources.[0]
            Expect.equal json.properties.publicNetworkAccess "Disabled" "publicNetworkAccess should be Disabled"
        }

        test "Can set soft delete retention in days" {
            let store = configurationStore {
                name "my-app-config"
                sku Standard
                soft_delete_retention_in_days 7
            }

            let resources = (store :> IBuilder).BuildResources Location.WestEurope
            let json = asStoreJson resources.[0]
            Expect.isTrue json.properties.softDeleteRetentionInDays.HasValue "softDeleteRetentionInDays should be set"

            Expect.equal json.properties.softDeleteRetentionInDays.Value 7 "softDeleteRetentionInDays should be 7"
        }

        test "Can set data plane authentication mode" {
            let store = configurationStore {
                name "my-app-config"
                data_plane_authentication_mode Passthrough
            }

            let resources = (store :> IBuilder).BuildResources Location.WestEurope
            let json = asStoreJson resources.[0]
            Expect.isNotNull json.properties.dataPlaneProxy "dataPlaneProxy should be set"
            // Verify authenticationMode via JSON string
            let jsonStr = resources.[0].JsonModel |> Serialization.toJson
            Expect.stringContains jsonStr "Pass-through" "authenticationMode should be Pass-through"
        }

        test "Can add feature flags" {
            let store = configurationStore {
                name "my-app-config"

                add_feature_flags [
                    {
                        Name = "MyFeature"
                        Description = "A test feature flag"
                        Label = ""
                        State = true
                    }
                    {
                        Name = "AnotherFeature"
                        Description = "Another feature flag"
                        Label = "prod"
                        State = false
                    }
                ]
            }

            let resources = (store :> IBuilder).BuildResources Location.WestEurope
            Expect.equal resources.Length 3 "Should have store + 2 feature flags"

            let ff1 = resources.[1]
            let ff1Json = asKeyValueJson ff1
            Expect.stringContains ff1Json.name ".appconfig.featureflag~2FMyFeature" "Feature flag 1 name"

            let ff2 = resources.[2]
            let ff2Json = asKeyValueJson ff2
            Expect.stringContains ff2Json.name "AnotherFeature$prod" "Feature flag 2 name with label"

            Expect.equal
                ff1Json.properties.contentType
                "application/vnd.microsoft.appconfig.ff+json;charset=utf-8"
                "Content type"

            Expect.stringContains ff1Json.properties.value "\"enabled\":true" "Flag state should be true"
        }

        test "Can add a single feature flag" {
            let store = configurationStore {
                name "my-app-config"

                add_feature_flag {
                    Name = "SingleFeature"
                    Description = "Single feature flag"
                    Label = ""
                    State = false
                }
            }

            let resources = (store :> IBuilder).BuildResources Location.WestEurope
            Expect.equal resources.Length 2 "Should have store + 1 feature flag"

            let ffJson = asKeyValueJson resources.[1]
            Expect.stringContains ffJson.properties.value "\"enabled\":false" "Flag state should be false"
        }

        test "Can add key-value items" {
            let store = configurationStore {
                name "my-app-config"

                add_key_value {
                    Key = "my-key"
                    Label = None
                    Value = "my-value"
                    ContentType = None
                    KeyValueTags = Map.empty
                }
            }

            let resources = (store :> IBuilder).BuildResources Location.WestEurope
            Expect.equal resources.Length 2 "Should have store + key value"

            let kvArm = resources.[1]
            Expect.equal kvArm.ResourceId.Name (ResourceName "my-app-config") "Key value parent store name"

            let kvJson = asKeyValueJson kvArm
            Expect.equal kvJson.name "my-app-config/my-key" "Key value JSON name"
            Expect.equal kvJson.properties.value "my-value" "Value should match"
            Expect.isNull kvJson.properties.contentType "contentType should not be set"
        }

        test "Can add key-value with label" {
            let store = configurationStore {
                name "my-app-config"

                add_key_value {
                    Key = "my-key"
                    Label = Some "prod"
                    Value = "my-value"
                    ContentType = None
                    KeyValueTags = Map.empty
                }
            }

            let resources = (store :> IBuilder).BuildResources Location.WestEurope
            let kvArm = resources.[1]
            let kvJson = asKeyValueJson kvArm

            Expect.equal kvJson.name "my-app-config/my-key$prod" "Key value name with label should be correct"
        }

        test "Can add key-value with content type and tags" {
            let store = configurationStore {
                name "my-app-config"

                add_key_value {
                    Key = "my-json-key"
                    Label = None
                    Value = """{"enabled":true}"""
                    ContentType = Some "application/json"
                    KeyValueTags = Map [ "env", "prod" ]
                }
            }

            let resources = (store :> IBuilder).BuildResources Location.WestEurope
            let kvJson = asKeyValueJson resources.[1]
            Expect.equal kvJson.properties.contentType "application/json" "contentType should match"
            Expect.isNotNull kvJson.properties.tags "tags should be set"
            Expect.equal kvJson.properties.tags.["env"] "prod" "tag should match"
        }

        test "Endpoint returns correct ARM expression" {
            let store = configurationStore { name "my-app-config" }
            let endpoint = store.Endpoint.Eval()
            Expect.stringContains endpoint "my-app-config" "Endpoint should contain store name"
            Expect.stringContains endpoint "endpoint" "Endpoint should reference 'endpoint'"
        }

        test "Can add tags to store" {
            let store = configurationStore {
                name "my-app-config"
                add_tags [ "env", "prod"; "team", "devops" ]
            }

            Expect.equal store.Tags (Map [ "env", "prod"; "team", "devops" ]) "Tags should be set"
        }
    ]
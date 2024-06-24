module WebApp

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Identity
open Farmer.WebApp
open Farmer.Arm
open Microsoft.Azure.Management.WebSites
open Microsoft.Azure.Management.WebSites.Models
open Microsoft.Rest
open System

let getResource<'T when 'T :> IArmResource> (data: IArmResource list) =
    data
    |> List.choose (function
        | :? 'T as x -> Some x
        | _ -> None)

/// Client instance needed to get the serializer settings.
let dummyClient =
    new WebSiteManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let getResourceAtIndex o =
    o |> getResourceAtIndex dummyClient.SerializationSettings

let getResources (v: IBuilder) = v.BuildResources Location.WestEurope

let tests =
    testList "Web App Tests" [
        test "Basic Web App has service plan and AI dependencies set" {
            let resources = webApp { name "test" } |> getResources
            let wa = resources |> getResource<Web.Site> |> List.head

            Expect.containsAll
                wa.Dependencies
                [
                    ResourceId.create (components, ResourceName "test-ai")
                    ResourceId.create (serverFarms, ResourceName "test-farm")
                ]
                "Missing dependencies"

            Expect.hasLength (resources |> getResource<Insights.Components>) 1 "Should be one AI component"
            Expect.hasLength (resources |> getResource<Web.ServerFarm>) 1 "Should be one server farm"
        }

        for os, version in [ Windows, 2; Linux, 3 ] do
            test $"Web App has App Insights preconfigured for OS {os}" {
                let resources =
                    webApp {
                        name "test"
                        operating_system os
                    }
                    |> getResources

                let wa = resources |> getResource<Web.Site> |> List.head

                let expectedSettings = [
                    "APPINSIGHTS_INSTRUMENTATIONKEY"
                    "APPINSIGHTS_PROFILERFEATURE_VERSION"
                    "APPINSIGHTS_SNAPSHOTFEATURE_VERSION"
                    "ApplicationInsightsAgent_EXTENSION_VERSION"
                    "DiagnosticServices_EXTENSION_VERSION"
                    "InstrumentationEngine_EXTENSION_VERSION"
                    "SnapshotDebugger_EXTENSION_VERSION"
                    "XDT_MicrosoftApplicationInsights_BaseExtensions"
                    "XDT_MicrosoftApplicationInsights_Mode"
                ]

                let actualSettings = wa.AppSettings |> Option.defaultValue Map.empty
                let actualSettingsSeq = actualSettings |> Seq.map (fun x -> x.Key)
                Expect.containsAll actualSettingsSeq expectedSettings "Missing AI settings"

                Expect.equal
                    actualSettings["ApplicationInsightsAgent_EXTENSION_VERSION"]
                    (LiteralSetting $"~{version}")
                    "Wrong AI version"
            }

        test "Web App allows renaming of service plan and AI" {
            let resources =
                webApp {
                    name "test"
                    service_plan_name "supersp"
                    app_insights_name "superai"
                }
                |> getResources

            let wa = resources |> getResource<Web.Site> |> List.head

            Expect.containsAll
                wa.Dependencies
                [
                    ResourceId.create (serverFarms, ResourceName "supersp")
                    ResourceId.create (components, ResourceName "superai")
                ]
                "Missing dependencies"

            Expect.hasLength (resources |> getResource<Insights.Components>) 1 "Should be one AI component"
            Expect.hasLength (resources |> getResource<Web.ServerFarm>) 1 "Should be one server farm"
        }
        test "Web App creates dependencies but no resources with linked AI and Server Farm configs" {
            let sp = servicePlan { name "plan" }
            let ai = appInsights { name "ai" }

            let resources =
                webApp {
                    name "test"
                    link_to_app_insights ai
                    link_to_service_plan sp
                }
                |> getResources

            let wa = resources |> getResource<Web.Site> |> List.head

            Expect.containsAll
                wa.Dependencies
                [
                    ResourceId.create (serverFarms, ResourceName "plan")
                    ResourceId.create (components, ResourceName "ai")
                ]
                "Missing dependencies"

            Expect.isEmpty (resources |> getResource<Insights.Components>) "Should be no AI component"
            Expect.isEmpty (resources |> getResource<Web.ServerFarm>) "Should be no server farm"
        }
        test "Web Apps link together" {
            let first = webApp {
                name "first"
                link_to_service_plan (servicePlan { name "firstSp" })
            }

            let second =
                webApp {
                    name "test"
                    link_to_service_plan first
                }
                |> getResources

            let wa = second |> getResource<Web.Site> |> List.head

            Expect.containsAll
                wa.Dependencies
                [ ResourceId.create (serverFarms, ResourceName "firstSp") ]
                "Missing dependencies"

            Expect.isEmpty (second |> getResource<Web.ServerFarm>) "Should be no server farm"
        }
        test "Web App does not create dependencies for unmanaged linked resources" {
            let resources =
                webApp {
                    name "test"
                    link_to_unmanaged_app_insights (components.resourceId "test")
                    link_to_unmanaged_service_plan (serverFarms.resourceId "test2")
                }
                |> getResources

            let wa = resources |> getResource<Web.Site> |> List.head
            Expect.isEmpty wa.Dependencies "Should be no dependencies"
            Expect.isEmpty (resources |> getResource<Insights.Components>) "Should be no AI component"
            Expect.isEmpty (resources |> getResource<Web.ServerFarm>) "Should be no server farm"
        }
        test "Web app supports adding tags to resource" {
            let resources =
                webApp {
                    name "test"
                    add_tag "key" "value"
                    add_tags [ "alpha", "a"; "beta", "b" ]
                }
                |> getResources

            let wa = resources |> getResource<Web.Site> |> List.head

            Expect.containsAll
                (wa.Tags |> Map.toSeq)
                [ "key", "value"; "alpha", "a"; "beta", "b" ]
                "Should contain the given tags"

            Expect.equal 3 (wa.Tags |> Map.count) "Should not contain additional tags"
        }
        test "Web App correctly adds connection strings" {
            let sa = storageAccount { name "foo" }

            let wa =
                let resources =
                    webApp {
                        name "test"
                        connection_string "a"
                        connection_string ("b", sa.Key)
                        connection_string ("c", ArmExpression.create ("c"), ConnectionStringKind.SQLAzure)
                    }
                    |> getResources

                resources |> getResource<Web.Site> |> List.head

            let expected = [
                "a", (ParameterSetting(SecureParameter "a"), ConnectionStringKind.Custom)
                "b", (ExpressionSetting sa.Key, ConnectionStringKind.Custom)
                "c", (ExpressionSetting(ArmExpression.create ("c")), ConnectionStringKind.SQLAzure)
            ]

            let parameters = wa :> IParameters

            Expect.equal wa.ConnectionStrings (Map expected |> Some) "Missing connections"
            Expect.equal parameters.SecureParameters [ SecureParameter "a" ] "Missing parameter"
        }
        test "CORS works correctly" {
            let wa: Site =
                webApp {
                    name "test"
                    enable_cors [ "https://bbc.co.uk" ]
                    enable_cors_credentials
                }
                |> getResourceAtIndex 3

            Expect.sequenceEqual wa.SiteConfig.Cors.AllowedOrigins [ "https://bbc.co.uk" ] "Allowed Origins should be *"

            Expect.equal wa.SiteConfig.Cors.SupportCredentials (Nullable true) "Support Credentials"
        }

        test "If CORS is AllOrigins, cannot enable credentials" {
            Expect.throws
                (fun () ->
                    webApp {
                        name "test"
                        enable_cors AllOrigins
                        enable_cors_credentials
                    }
                    |> ignore)
                "Invalid CORS combination"
        }

        test "Automatically converts from * to AllOrigins" {
            let wa: Site =
                webApp {
                    name "test"
                    enable_cors [ "*" ]
                }
                |> getResourceAtIndex 3

            Expect.sequenceEqual wa.SiteConfig.Cors.AllowedOrigins [ "*" ] "Allowed Origins should be *"
        }

        test "CORS without credentials does not crash" {
            webApp {
                name "test"
                enable_cors AllOrigins
            }
            |> ignore

            webApp {
                name "test"
                enable_cors [ "https://bbc.co.uk" ]
            }
            |> ignore
        }

        test "If CORS is not enabled, ignores enable credentials" {
            let wa: Site =
                webApp {
                    name "test"
                    enable_cors_credentials
                }
                |> getResourceAtIndex 3

            Expect.isNull wa.SiteConfig.Cors "Should be no CORS settings"
        }

        test "Implicitly adds a dependency when adding a setting" {
            let sa = storageAccount { name "teststorage" }

            let sql = sqlServer {
                name "test"
                admin_username "user"
                add_databases [ sqlDb { name "thedb" } ]
            }

            let wa = webApp {
                name "testweb"
                setting "storage" sa.Key
                setting "conn" (sql.ConnectionString "thedb")
                setting "bad" (ArmExpression.literal "ignore_me")
            }

            let wa = wa |> getResources |> getResource<Web.Site> |> List.head

            Expect.contains
                wa.Dependencies
                (ResourceId.create (storageAccounts, sa.Name.ResourceName))
                "Storage Account is missing"

            Expect.contains
                wa.Dependencies
                (ResourceId.create (Sql.databases, ResourceName "test", ResourceName "thedb"))
                "Database is missing"
        }

        test "Implicitly adds a dependency when adding a connection string" {
            let sa = storageAccount { name "teststorage" }

            let wa = webApp {
                name "testweb"
                setting "storage" sa.Key
            }

            let wa = wa |> getResources |> getResource<Web.Site> |> List.head

            Expect.contains
                wa.Dependencies
                (ResourceId.create (storageAccounts, sa.Name.ResourceName))
                "Storage Account is missing"
        }

        test "Automatic Key Vault integration works correctly" {
            let sa = storageAccount { name "teststorage" }

            let wa = webApp {
                name "testweb"
                setting "astorage" sa.Key
                secret_setting "bsecret"
                secret_setting "csection:secret"
                secret_setting "dmy_secret"
                setting "eliteral" "value"
                use_keyvault
            }

            let kv = wa |> getResources |> getResource<Vault> |> List.head

            let secrets =
                wa
                |> getResources
                |> getResource<Vaults.Secret>
                |> List.sortBy (fun s -> s.Name)

            let site = wa |> getResources |> getResource<Web.Site> |> List.head
            let vault = wa |> getResources |> getResource<Vault> |> List.head

            let expectedSettings =
                Map [
                    "astorage",
                    LiteralSetting
                        "@Microsoft.KeyVault(SecretUri=https://testwebvault.vault.azure.net/secrets/astorage)"
                    "bsecret",
                    LiteralSetting "@Microsoft.KeyVault(SecretUri=https://testwebvault.vault.azure.net/secrets/bsecret)"
                    "csection:secret",
                    LiteralSetting
                        "@Microsoft.KeyVault(SecretUri=https://testwebvault.vault.azure.net/secrets/csection-secret)"
                    "dmy_secret",
                    LiteralSetting
                        "@Microsoft.KeyVault(SecretUri=https://testwebvault.vault.azure.net/secrets/dmy-secret)"
                    "eliteral", LiteralSetting "value"
                ]

            Expect.equal site.Identity.SystemAssigned Enabled "System Identity should be enabled"
            let settings = Expect.wantSome site.AppSettings "AppSettings should be set"
            Expect.containsAll settings expectedSettings "Incorrect settings"

            Expect.sequenceEqual
                kv.Dependencies
                [ ResourceId.create (sites, site.Name) ]
                "Key Vault dependencies are wrong"

            Expect.equal kv.Name (ResourceName(site.Name.Value + "vault")) "Key Vault name is wrong"
            Expect.equal wa.CommonWebConfig.Identity.SystemAssigned Enabled "System Identity should be turned on"

            Expect.equal
                kv.AccessPolicies.[0].ObjectId
                wa.SystemIdentity.PrincipalId.ArmExpression
                "Policy is incorrect"

            Expect.hasLength secrets 4 "Incorrect number of KV secrets"

            Expect.equal secrets.[0].Name.Value "testwebvault/astorage" "Incorrect secret name"
            Expect.equal secrets.[0].Value (ExpressionSecret sa.Key) "Incorrect secret value"

            Expect.sequenceEqual
                secrets.[0].Dependencies
                [ vaults.resourceId "testwebvault"; storageAccounts.resourceId "teststorage" ]
                "Incorrect secret dependencies"

            Expect.equal secrets.[1].Name.Value "testwebvault/bsecret" "Incorrect secret name"
            Expect.equal secrets.[1].Value (ParameterSecret(SecureParameter "bsecret")) "Incorrect secret value"

            Expect.sequenceEqual
                secrets.[1].Dependencies
                [ vaults.resourceId "testwebvault" ]
                "Incorrect secret dependencies"

            Expect.equal secrets.[2].Name.Value "testwebvault/csection-secret" "Incorrect secret name"

            Expect.equal secrets.[2].Value (ParameterSecret(SecureParameter "csection-secret")) "Incorrect secret value"

            Expect.equal secrets.[3].Name.Value "testwebvault/dmy-secret" "Incorrect secret name"
            Expect.equal secrets.[3].Value (ParameterSecret(SecureParameter "dmy-secret")) "Incorrect secret value"

            Expect.hasLength vault.AccessPolicies 1 "Incorrect number of access policies"

            Expect.sequenceEqual
                vault.AccessPolicies.[0].Permissions.Secrets
                [ KeyVault.Secret.Get ]
                "Incorrect permissions"
        }

        test "Managed KV integration works correctly" {
            let sa = storageAccount { name "teststorage" }

            let wa = webApp {
                name "testweb"
                setting "storage" sa.Key
                secret_setting "secret"
                setting "literal" "value"
                link_to_keyvault (ResourceName "testwebvault")
            }

            let vault = keyVault {
                name "testwebvault"
                add_access_policy (AccessPolicy.create (wa.SystemIdentity.PrincipalId, [ KeyVault.Secret.Get ]))
            }

            let vault = vault |> getResources |> getResource<Vault> |> List.head
            let secrets = wa |> getResources |> getResource<Vaults.Secret>
            let site = wa |> getResources |> getResource<Web.Site> |> List.head

            let expectedSettings =
                Map [
                    "storage",
                    LiteralSetting "@Microsoft.KeyVault(SecretUri=https://testwebvault.vault.azure.net/secrets/storage)"
                    "secret",
                    LiteralSetting "@Microsoft.KeyVault(SecretUri=https://testwebvault.vault.azure.net/secrets/secret)"
                    "literal", LiteralSetting "value"
                ]

            Expect.equal site.Identity.SystemAssigned Enabled "System Identity should be enabled"
            let settings = Expect.wantSome site.AppSettings "AppSettings should be set"
            Expect.containsAll settings expectedSettings "Incorrect settings"

            Expect.equal wa.CommonWebConfig.Identity.SystemAssigned Enabled "System Identity should be turned on"

            Expect.hasLength secrets 2 "Incorrect number of KV secrets"

            Expect.equal secrets.[0].Name.Value "testwebvault/secret" "Incorrect secret name"
            Expect.equal secrets.[0].Value (ParameterSecret(SecureParameter "secret")) "Incorrect secret value"

            Expect.sequenceEqual
                secrets.[0].Dependencies
                [ vaults.resourceId "testwebvault" ]
                "Incorrect secret dependencies"

            Expect.equal secrets.[1].Name.Value "testwebvault/storage" "Incorrect secret name"
            Expect.equal secrets.[1].Value (ExpressionSecret sa.Key) "Incorrect secret value"

            Expect.sequenceEqual
                secrets.[1].Dependencies
                [ vaults.resourceId "testwebvault"; storageAccounts.resourceId "teststorage" ]
                "Incorrect secret dependencies"
        }

        test "Handles identity correctly" {
            let wa: Site = webApp { name "testsite" } |> getResourceAtIndex 0
            Expect.isNull wa.Identity "Default managed identity should be null"

            let wa: Site =
                webApp {
                    name "othertestsite"
                    system_identity
                }
                |> getResourceAtIndex 3

            Expect.equal
                wa.Identity.Type
                (Nullable ManagedServiceIdentityType.SystemAssigned)
                "Should have system identity"

            Expect.isNull wa.Identity.UserAssignedIdentities "Should have no user assigned identities"

            let wa: Site =
                webApp {
                    name "thirdtestsite"
                    system_identity
                    add_identity (createUserAssignedIdentity "test")
                    add_identity (createUserAssignedIdentity "test2")
                }
                |> getResourceAtIndex 3

            Expect.equal
                wa.Identity.Type
                (Nullable ManagedServiceIdentityType.SystemAssignedUserAssigned)
                "Should have system identity"

            Expect.sequenceEqual
                (wa.Identity.UserAssignedIdentities |> Seq.map (fun r -> r.Key))
                [
                    "[resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', 'test2')]"
                    "[resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', 'test')]"
                ]
                "Should have two user assigned identities"

            Expect.contains
                (wa.SiteConfig.AppSettings |> Seq.map (fun s -> s.Name, s.Value))
                ("AZURE_CLIENT_ID",
                 "[reference(resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', 'test2')).clientId]")
                "Missing AZURE_CLIENT_ID"
        }

        test "Unmanaged server farm is fully qualified in ARM" {
            let farm =
                ResourceId.create (serverFarms, ResourceName "my-asp-name", "my-asp-resource-group")

            let wa: Site =
                webApp {
                    name "test"
                    link_to_unmanaged_service_plan farm
                }
                |> getResourceAtIndex 2

            Expect.equal
                wa.ServerFarmId
                "[resourceId('my-asp-resource-group', 'Microsoft.Web/serverfarms', 'my-asp-name')]"
                ""
        }

        test "Adds the Logging extension automatically for .NET Core apps" {
            let wa = webApp { name "siteX" }
            let extension = wa |> getResources |> getResource<SiteExtension> |> List.head

            Expect.equal extension.Name.Value "Microsoft.AspNetCore.AzureAppServices.SiteExtension" "Wrong extension"

            let wa = webApp {
                name "siteX"
                runtime_stack Runtime.Java11
            }

            let extensions = wa |> getResources |> getResource<SiteExtension>
            Expect.isEmpty extensions "Shouldn't be any extensions"

            let wa = webApp {
                name "siteX"
                automatic_logging_extension false
            }

            let extensions = wa |> getResources |> getResource<SiteExtension>
            Expect.isEmpty extensions "Shouldn't be any extensions"
        }

        test "Does not add the logging extension for apps using a docker image" {
            let wa = webApp {
                name "siteX"
                docker_image "someImage" "someCommand"
            }

            let extensions = wa |> getResources |> getResource<SiteExtension>
            Expect.isEmpty extensions "Shouldn't be any extensions"
        }

        test "Can specify different image for slots" {
            let wa = webApp {
                name "my-webapp-2651A324"
                sku (Sku.Standard "S1")
                docker_image "nginx:1.22.1" ""

                add_slots [
                    appSlot {
                        name "staging"
                        docker_image "nginx:1.23.1" ""
                    }
                ]
            }

            let (slot: Site) = wa |> getResourceAtIndex 3
            Expect.equal slot.Name "my-webapp-2651A324/staging" "Resource isn't the 'staging' slot"
            Expect.equal slot.SiteConfig.LinuxFxVersion "DOCKER|nginx:1.23.1" "Docker image not set on slot"
        }

        test "Handles add_extension correctly" {
            let wa = webApp {
                name "siteX"
                add_extension "extensionA"
                runtime_stack Runtime.Java11
            }

            let resources = wa |> getResources
            let sx = resources |> getResource<SiteExtension> |> List.head
            let r = sx :> IArmResource

            Expect.equal sx.SiteName (ResourceName "siteX") "Extension knows the site name"
            Expect.equal sx.Location Location.WestEurope "Location is correct"
            Expect.equal sx.Name (ResourceName "extensionA") "Extension name is correct"

            Expect.equal
                r.ResourceId.ArmExpression.Value
                "resourceId('Microsoft.Web/sites/siteextensions', 'siteX', 'extensionA')"
                "Resource name composed of site name and extension name"
        }

        test "Handles multiple add_extension correctly" {
            let wa = webApp {
                name "siteX"
                add_extension "extensionA"
                add_extension "extensionB"
                add_extension "extensionB"
                runtime_stack Runtime.Java11
            }

            let resources = wa |> getResources |> getResource<SiteExtension>

            let actual = List.sort resources

            let expected = [
                {
                    Location = Location.WestEurope
                    Name = ResourceName "extensionA"
                    SiteName = ResourceName "siteX"
                }
                {
                    Location = Location.WestEurope
                    Name = ResourceName "extensionB"
                    SiteName = ResourceName "siteX"
                }
            ]

            Expect.sequenceEqual actual expected "Both extensions defined"
        }

        test "SiteExtension ResourceId constructed correctly" {
            let siteName = ResourceName "siteX"
            let resourceId = siteExtensions.resourceId siteName

            Expect.equal resourceId.ArmExpression.Value "resourceId('Microsoft.Web/sites/siteextensions', 'siteX')" ""
        }

        test "Deploys AI configuration correctly" {
            let hasSetting key message (wa: Site) =
                Expect.isTrue (wa.SiteConfig.AppSettings |> Seq.exists (fun k -> k.Name = key)) message

            let wa: Site = webApp { name "testsite" } |> getResourceAtIndex 3

            wa
            |> hasSetting "APPINSIGHTS_INSTRUMENTATIONKEY" "Missing Windows instrumentation key"

            let wa: Site =
                webApp {
                    name "testsite"
                    operating_system Linux
                }
                |> getResourceAtIndex 2

            wa
            |> hasSetting "APPINSIGHTS_INSTRUMENTATIONKEY" "Missing Linux instrumentation key"

            let wa: Site =
                webApp {
                    name "testsite"
                    app_insights_off
                }
                |> getResourceAtIndex 2

            Expect.isEmpty wa.SiteConfig.AppSettings "Should be no settings"
        }

        test "Supports always on" {
            let template = webApp {
                name "web"
                always_on
            }

            Expect.equal template.CommonWebConfig.AlwaysOn true "AlwaysOn should be true"

            let w: Site = webApp { name "testDefault" } |> getResourceAtIndex 3
            Expect.equal w.SiteConfig.AlwaysOn (Nullable false) "always on should be false by default"
        }

        test "Supports 32 and 64 bit worker processes" {
            let site: Site = webApp { name "web" } |> getResourceAtIndex 3
            Expect.equal site.SiteConfig.Use32BitWorkerProcess (Nullable()) "Default worker process"

            let site: Site =
                webApp {
                    name "web2"
                    worker_process Bits32
                }
                |> getResourceAtIndex 3

            Expect.equal site.SiteConfig.Use32BitWorkerProcess (Nullable true) "Should use 32 bit worker process"

            let site: Site =
                webApp {
                    name "web3"
                    worker_process Bits64
                }
                |> getResourceAtIndex 3

            Expect.equal site.SiteConfig.Use32BitWorkerProcess (Nullable false) "Should not use 32 bit worker process"
        }

        test "Supports .NET 6" {
            let app = webApp {
                name "net6"
                runtime_stack Runtime.DotNet60
            }

            let site = app |> getResources |> getResource<Web.Site> |> List.head
            Expect.equal site.NetFrameworkVersion.Value "v6.0" "Wrong dotnet version"
            Expect.equal site.Metadata.Head ("CURRENT_STACK", "dotnet") "Stack should be dotnet"
        }

        test "Supports .NET 7" {
            let app = webApp {
                name "net7"
                runtime_stack Runtime.DotNet70
            }

            let site = app |> getResources |> getResource<Web.Site> |> List.head
            Expect.equal site.NetFrameworkVersion.Value "v7.0" "Wrong dotnet version"
            Expect.equal site.Metadata.Head ("CURRENT_STACK", "dotnet") "Stack should be dotnet"
        }

        test "Supports .NET 8" {
            let app = webApp {
                name "net8"
                runtime_stack Runtime.DotNet80
            }

            let site = app |> getResources |> getResource<Web.Site> |> List.head
            Expect.equal site.NetFrameworkVersion.Value "v8.0" "Wrong dotnet version"
            Expect.equal site.Metadata.Head ("CURRENT_STACK", "dotnet") "Stack should be dotnet"
        }

        test "Supports .NET 5 on Linux" {
            let app = webApp {
                name "net5"
                operating_system Linux
                runtime_stack Runtime.DotNet50
            }

            let site: Site = app |> getResourceAtIndex 2
            Expect.equal site.SiteConfig.LinuxFxVersion "DOTNETCORE|5.0" "Wrong dotnet version"
        }

        test "WebApp supports adding slots" {
            let slot = appSlot { name "warm-up" }

            let site: WebAppConfig = webApp {
                name "slots"
                add_slot slot
            }

            Expect.isTrue (site.CommonWebConfig.Slots.ContainsKey "warm-up") "Config should contain slot"

            let slots =
                site
                |> getResources
                |> getResource<Arm.Web.Site>
                |> List.filter (fun x -> x.ResourceType = Arm.Web.slots)
            // Default "production" slot is not included as it is created automatically in Azure
            Expect.hasLength slots 1 "Should only be 1 slot"
        }

        test "WebApp with slot and zip_deploy_slot does not have ZipDeployPath on slot" {
            let slot = appSlot { name "warm-up" }

            let site: WebAppConfig = webApp {
                name "slots"
                add_slot slot
                zip_deploy_slot "warm-up" "test.zip"
            }

            Expect.isTrue (site.CommonWebConfig.Slots.ContainsKey "warm-up") "Config should contain slot"

            let slots =
                site
                |> getResources
                |> getResource<Arm.Web.Site>
                |> List.filter (fun x -> x.ResourceType = Arm.Web.slots)
            // Default "production" slot is not included as it is created automatically in Azure
            Expect.hasLength slots 1 "Should only be 1 slot"
            Expect.isNone slots.[0].ZipDeployPath "Zip Deploy Path should be set to None"
        }

        test "WebApp with slot that has system assigned identity adds identity to slot" {
            let slot = appSlot {
                name "warm-up"
                system_identity
            }

            let site: WebAppConfig = webApp {
                name "webapp"
                add_slot slot
            }

            Expect.isTrue (site.CommonWebConfig.Slots.ContainsKey "warm-up") "Config should contain slot"

            let slots = site |> getResources |> getResource<Arm.Web.Site>
            // Default "production" slot is not included as it is created automatically in Azure
            Expect.hasLength slots 2 "Should only be 1 slot and 1 site"

            let expected = {
                SystemAssigned = Enabled
                UserAssigned = []
            }

            Expect.equal (slots.[1]).Identity expected "Slot should have slot setting"
        }

        test "WebApp with slot adds settings to slot" {
            let slot = appSlot { name "warm-up" }

            let site: WebAppConfig = webApp {
                name "slotsettings"
                add_slot slot
                setting "setting" "some value"
            }

            Expect.isTrue (site.CommonWebConfig.Slots.ContainsKey "warm-up") "Config should contain slot"

            let slots =
                site
                |> getResources
                |> getResource<Arm.Web.Site>
                |> List.filter (fun x -> x.ResourceType = Arm.Web.slots)
            // Default "production" slot is not included as it is created automatically in Azure
            Expect.hasLength slots 1 "Should only be 1 slot"

            let settings = Expect.wantSome (slots.[0]).AppSettings "AppSettings should be set"
            Expect.isTrue (settings.ContainsKey("setting")) "Slot should have slot setting"
        }

        test "WebApp with slot does not add settings to app service" {
            let slot = appSlot { name "warm-up" }

            let config = webApp {
                name "web"
                add_slot slot
                setting "setting" "some value"
                connection_string "DB"
            }

            let sites = config |> getResources |> getResource<Farmer.Arm.Web.Site>

            // Default "production" slot is not included as it is created automatically in Azure
            Expect.hasLength sites 2 "Should only be 1 slot and 1 site"

            Expect.isNone ((sites.[0]).AppSettings) "App service should not have any settings"
            Expect.isNone ((sites.[0]).ConnectionStrings) "App service should not have any connection strings"
        }

        //test "WebApp creates App Insights" { }

        test "WebApp adds literal settings to slots" {
            let slot = appSlot { name "warm-up" }

            let site: WebAppConfig = webApp {
                name "web"
                add_slot slot
                run_from_package
                website_node_default_version "xxx"
                docker_ci
                docker_use_azure_registry "registry"
            }

            Expect.isTrue (site.CommonWebConfig.Slots.ContainsKey "warm-up") "Config should contain slot"

            let sites = site |> getResources |> getResource<Arm.Web.Site>
            let slots = sites |> List.filter (fun x -> x.ResourceType = Arm.Web.slots)
            // Default "production" slot is not included as it is created automatically in Azure
            Expect.hasLength slots 1 "Should only be 1 slot"

            let settings = (slots.Item 0).AppSettings |> Option.defaultValue Map.empty

            let expectation =
                [
                    "APPINSIGHTS_INSTRUMENTATIONKEY"
                    "APPINSIGHTS_PROFILERFEATURE_VERSION"
                    "APPINSIGHTS_SNAPSHOTFEATURE_VERSION"
                    "ApplicationInsightsAgent_EXTENSION_VERSION"
                    "DiagnosticServices_EXTENSION_VERSION"
                    "InstrumentationEngine_EXTENSION_VERSION"
                    "SnapshotDebugger_EXTENSION_VERSION"
                    "XDT_MicrosoftApplicationInsights_BaseExtensions"
                    "XDT_MicrosoftApplicationInsights_Mode"
                    "DOCKER_ENABLE_CI"
                    "DOCKER_REGISTRY_SERVER_PASSWORD"
                    "DOCKER_REGISTRY_SERVER_URL"
                    "DOCKER_REGISTRY_SERVER_USERNAME"
                ]
                |> List.map (settings.ContainsKey)

            Expect.allEqual expectation true "Slot should have all literal settings"
        }

        test "WebApp with different settings on slot and service adds both settings to slot" {
            let slot = appSlot {
                name "warm-up"
                setting "slot" "slot value"
            }

            let site: WebAppConfig = webApp {
                name "web"
                add_slot slot
                setting "appService" "app service value"
            }

            Expect.isTrue (site.CommonWebConfig.Slots.ContainsKey "warm-up") "Config should contain slot"

            let slots =
                site
                |> getResources
                |> getResource<Arm.Web.Site>
                |> List.filter (fun x -> x.ResourceType = Arm.Web.slots)
            // Default "production" slot is not included as it is created automatically in Azure
            Expect.hasLength slots 1 "Should only be 1 slot"

            let settings = (slots.Item 0).AppSettings
            Expect.isTrue (settings.Value.ContainsKey("slot")) "Slot should have slot setting"
            Expect.isTrue (settings.Value.ContainsKey("appService")) "Slot should have app service setting"
        }

        test "WebApp with slot, slot settings override app service setting" {
            let slot = appSlot {
                name "warm-up"
                setting "override" "overridden"
            }

            let site: WebAppConfig = webApp {
                name "web"
                add_slot slot
                setting "override" "some value"
            }

            Expect.isTrue (site.CommonWebConfig.Slots.ContainsKey "warm-up") "Config should contain slot"

            let slots =
                site
                |> getResources
                |> getResource<Arm.Web.Site>
                |> List.filter (fun x -> x.ResourceType = Arm.Web.slots)
            // Default "production" slot is not included as it is created automatically in Azure
            Expect.hasLength slots 1 "Should only be 1 slot"

            let settings = Expect.wantSome slots.[0].AppSettings "AppSettings should be set"
            let (hasValue, value) = settings.TryGetValue("override")

            Expect.isTrue hasValue "Slot should have app service setting"
            Expect.equal value.Value "overridden" "Slot should have correct app service value"
        }

        test "WebApp with slot adds connection strings to slot" {
            let slot = appSlot { name "warm-up" }

            let site: WebAppConfig = webApp {
                name "web"
                add_slot slot
                connection_string "connection_string"
            }

            Expect.isTrue (site.CommonWebConfig.Slots.ContainsKey "warm-up") "Config should contain slot"

            let slots =
                site
                |> getResources
                |> getResource<Arm.Web.Site>
                |> List.filter (fun x -> x.ResourceType = Arm.Web.slots)
            // Default "production" slot is not included as it is created automatically in Azure
            Expect.hasLength slots 1 "Should only be 1 slot"

            let connStrings =
                Expect.wantSome slots.[0].ConnectionStrings "ConnectionStrings should be set"

            Expect.isTrue
                (connStrings.ContainsKey("connection_string"))
                "Slot should have app service connection string"
        }

        test "WebApp with different connection strings on slot and service adds both to slot" {
            let slot = appSlot {
                name "warm-up"
                connection_string "slot"
            }

            let site: WebAppConfig = webApp {
                name "web"
                add_slot slot
                connection_string "appService"
            }

            Expect.isTrue (site.CommonWebConfig.Slots.ContainsKey "warm-up") "Config should contain slot"

            let slots =
                site
                |> getResources
                |> getResource<Arm.Web.Site>
                |> List.filter (fun x -> x.ResourceType = Arm.Web.slots)
            // Default "production" slot is not included as it is created automatically in Azure
            Expect.hasLength slots 1 "Should only be 1 slot"

            let connStrings =
                Expect.wantSome slots.[0].ConnectionStrings "ConnectionStrings should be set"

            Expect.hasLength connStrings 2 "Slot should have two connection strings"
        }

        test "WebApp with slots and identity applies identity to slots" {
            let identity18 = userAssignedIdentity { name "im-18" }
            let identity21 = userAssignedIdentity { name "im-21" }

            let slot = appSlot {
                name "deploy"
                keyvault_identity identity21
            }

            let site: WebAppConfig = webApp {
                name "web"
                add_slot slot
                add_identity identity18
            }

            Expect.isTrue (site.CommonWebConfig.Slots.ContainsKey "deploy") "Config should contain slot"

            let slots =
                site
                |> getResources
                |> getResource<Arm.Web.Site>
                |> List.filter (fun x -> x.ResourceType = Arm.Web.slots)
            // Default "production" slot is not included as it is created automatically in Azure
            Expect.hasLength slots 1 "Should only be 1 slot"

            let theSlot = (slots.[0])
            Expect.hasLength (theSlot.Identity.UserAssigned) 2 "Slot should have 2 user-assigned identities"

            Expect.containsAll
                (theSlot.Identity.UserAssigned)
                [ identity18.UserAssignedIdentity; identity21.UserAssignedIdentity ]
                "Slot should have both user assigned identities"

            Expect.equal
                theSlot.KeyVaultReferenceIdentity
                (Some identity21.UserAssignedIdentity)
                "Slot should have correct keyvault identity"
        }

        test "WebApp with slot can use AutoSwapSlotName" {
            let warmupSlot = appSlot {
                name "warm-up"
                autoSlotSwapName "production"
            }

            let site: WebAppConfig = webApp {
                name "slots"
                add_slot warmupSlot
            }

            Expect.isTrue (site.CommonWebConfig.Slots.ContainsKey "warm-up") "Config should contain slot"

            let slot: Site = site |> getResourceAtIndex 4

            Expect.equal slot.Name "slots/warm-up" "Should be expected slot"
            Expect.equal slot.SiteConfig.AutoSwapSlotName "production" "Should use provided auto swap slot name"
        }

        test "Supports private endpoints" {
            let subnet = ResourceId.create (Network.subnets, ResourceName "subnet")

            let app = webApp {
                name "farmerWebApp"
                add_private_endpoint (Managed subnet, "myWebApp-ep")
            }

            let ep: Microsoft.Azure.Management.Network.Models.PrivateEndpoint =
                app |> getResourceAtIndex 4

            Expect.equal ep.Name "myWebApp-ep" "Incorrect name"
            Expect.hasLength ep.PrivateLinkServiceConnections.[0].GroupIds 1 "Incorrect group ids length"
            Expect.equal ep.PrivateLinkServiceConnections.[0].GroupIds.[0] "sites" "Incorrect group ids"

            Expect.equal
                ep.PrivateLinkServiceConnections.[0].PrivateLinkServiceId
                "[resourceId('Microsoft.Web/sites', 'farmerWebApp')]"
                "Incorrect PrivateLinkServiceId"

            Expect.equal ep.Subnet.Id (subnet.ArmExpression.Eval()) "Incorrect subnet id"
        }

        test "Supports keyvault reference identity" {
            let app = webApp { name "farmerWebApp" }
            let site: Site = app |> getResourceAtIndex 3
            Expect.isNull site.KeyVaultReferenceIdentity "Keyvault identity should not be set"

            let myId = userAssignedIdentity { name "myFarmerIdentity" }

            let app = webApp {
                name "farmerWebApp"
                keyvault_identity myId
            }

            let site: Site = app |> getResourceAtIndex 3

            Expect.equal
                site.KeyVaultReferenceIdentity
                "[resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', 'myFarmerIdentity')]"
                "Keyvault identity should not be set"
        }

        test "Validates name correctly" {
            let check (v: string) m =
                Expect.equal (WebAppName.Create v) (Error("Web App site names " + m))

            check "" "cannot be empty" "Name too short"
            let longName = Array.init 61 (fun _ -> 'a') |> String
            check longName $"max length is 60, but here is 61. The invalid value is '{longName}'" "Name too long"

            check
                "zz!z"
                "can only contain alphanumeric characters or the dash (-). The invalid value is 'zz!z'"
                "Bad character allowed"

            check "-zz" "cannot start with a dash (-). The invalid value is '-zz'" "Start with dash"
            check "zz-" "cannot end with a dash (-). The invalid value is 'zz-'" "End with dash"
        }

        test "Not setting the web app name causes an error" {
            Expect.throws
                (fun () -> webApp { runtime_stack Runtime.Java11 } |> ignore)
                "Not setting web app name should throw"
        }

        test "Supports health check" {
            let resources =
                webApp {
                    name "test"
                    health_check_path "/status"
                }
                |> getResources

            let wa = resources |> getResource<Web.Site> |> List.head

            Expect.equal wa.HealthCheckPath (Some "/status") "Health check path should be '/status'"
        }

        test "Supports secure custom domains with custom certificate" {
            let webappName = "test"
            let thumbprint = ArmExpression.literal "1111583E8FABEF4C0BEF694CBC41C28FB81CD111"

            let resources =
                webApp {
                    name webappName
                    custom_domain ("customDomain.io", thumbprint)
                }
                |> getResources

            let wa = resources |> getResource<Web.Site> |> List.head
            let nested = resources |> getResource<ResourceGroupDeployment>
            let expectedDomainName = "customDomain.io"

            // Testing HostnameBinding
            let hostnameBinding =
                nested.[0].Resources |> getResource<Web.HostNameBinding> |> List.head

            let expectedSslState = SslState.SslDisabled
            let exepectedSiteId = (Managed(Arm.Web.sites.resourceId wa.Name))

            Expect.equal
                hostnameBinding.DomainName
                expectedDomainName
                $"HostnameBinding domain name should have {expectedDomainName}"

            Expect.equal
                hostnameBinding.SslState
                expectedSslState
                $"HostnameBinding should have a {expectedSslState} Ssl state"

            Expect.equal hostnameBinding.SiteId exepectedSiteId $"HostnameBinding SiteId should be {exepectedSiteId}"

            // Testing certificate
            let cert = nested.[1].Resources |> getResource<Web.Certificate> |> List.head

            Expect.equal cert.DomainName expectedDomainName $"Certificate domain name should have {expectedDomainName}"

            // Testing hostname/certificate link.
            let bindingDeployment = nested.[2]

            let innerResource =
                bindingDeployment.Resources |> getResource<Web.HostNameBinding> |> List.head

            let innerExpectedSslState = SslState.SniBased thumbprint

            Expect.stringStarts
                bindingDeployment.DeploymentName.Value
                "[concat"
                "resourceGroupDeployment name should start as a valid ARM expression"

            Expect.stringEnds
                bindingDeployment.DeploymentName.Value
                ")]"
                "resourceGroupDeployment stage should end as a valid ARM expression"

            Expect.equal
                bindingDeployment.Resources.Length
                1
                "resourceGroupDeployment stage should only contain one resource"

            Expect.equal
                bindingDeployment.Dependencies.Count
                1
                "resourceGroupDeployment stage should only contain one dependencies"

            Expect.equal
                innerResource.SslState
                innerExpectedSslState
                $"hostnameBinding should have a {innerExpectedSslState} Ssl state inside the resourceGroupDeployment template"
        }

        test "Supports secure custom domains with app service managed certificate" {
            let webappName = "test"

            let resources =
                webApp {
                    name webappName
                    custom_domain "customDomain.io"
                }
                |> getResources

            let wa = resources |> getResource<Web.Site> |> List.head
            let nested = resources |> getResource<ResourceGroup.ResourceGroupDeployment>
            let expectedDomainName = "customDomain.io"

            // Testing HostnameBinding
            let hostnameBinding =
                nested.[0].Resources |> getResource<Web.HostNameBinding> |> List.head

            let expectedSslState = SslState.SslDisabled
            let exepectedSiteId = (Managed(Arm.Web.sites.resourceId wa.Name))

            Expect.equal
                hostnameBinding.DomainName
                expectedDomainName
                $"HostnameBinding domain name should have {expectedDomainName}"

            Expect.equal
                hostnameBinding.SslState
                expectedSslState
                $"HostnameBinding should have a {expectedSslState} Ssl state"

            Expect.equal hostnameBinding.SiteId exepectedSiteId $"HostnameBinding SiteId should be {exepectedSiteId}"

            // Testing certificate
            let cert = nested.[1].Resources |> getResource<Web.Certificate> |> List.head

            Expect.equal cert.DomainName expectedDomainName $"Certificate domain name should have {expectedDomainName}"

            // Testing hostname/certificate link.
            let bindingDeployment = nested.[2]

            let innerResource =
                bindingDeployment.Resources |> getResource<Web.HostNameBinding> |> List.head

            let innerExpectedSslState = SslState.SniBased cert.Thumbprint

            Expect.equal
                bindingDeployment.Resources.Length
                1
                "resourceGroupDeployment stage should only contain one resource"

            Expect.equal
                bindingDeployment.Dependencies.Count
                1
                "resourceGroupDeployment stage should only contain one dependencies"

            Expect.equal
                innerResource.SslState
                innerExpectedSslState
                $"hostnameBinding should have a {innerExpectedSslState} Ssl state inside the resourceGroupDeployment template"
        }

        test "Supports insecure custom domains" {
            let webappName = "test"

            let resources =
                webApp {
                    name webappName
                    custom_domain (DomainConfig.InsecureDomain "customDomain.io")
                }
                |> getResources

            let wa = resources |> getResource<Web.Site> |> List.head

            //Testing HostnameBinding
            let hostnameBinding =
                resources
                |> getResource<ResourceGroupDeployment>
                |> Seq.map (fun x -> getResource<Web.HostNameBinding> (x.Resources))
                |> Seq.concat
                |> Seq.head

            let expectedSslState = SslState.SslDisabled
            let exepectedSiteId = (Managed(Arm.Web.sites.resourceId wa.Name))
            let expectedDomainName = "customDomain.io"

            Expect.equal
                hostnameBinding.DomainName
                expectedDomainName
                $"HostnameBinding domain name should have {expectedDomainName}"

            Expect.equal
                hostnameBinding.SslState
                expectedSslState
                $"HostnameBinding should have a {expectedSslState} Ssl state"

            Expect.equal hostnameBinding.SiteId exepectedSiteId $"HostnameBinding SiteId should be {exepectedSiteId}"
        }

        test "Supports multiple custom domains" {
            let webappName = "test"

            let resources =
                webApp {
                    name webappName
                    custom_domain "secure.io"
                    custom_domain (DomainConfig.InsecureDomain "insecure.io")
                }
                |> getResources

            let wa = resources |> getResource<Web.Site> |> List.head

            let exepectedSiteId = (Managed(Arm.Web.sites.resourceId wa.Name))

            //Testing HostnameBinding
            let hostnameBindings =
                resources
                |> getResource<ResourceGroupDeployment>
                |> Seq.map (fun x -> getResource<Web.HostNameBinding> (x.Resources))
                |> Seq.concat

            let secureBinding =
                hostnameBindings |> Seq.filter (fun x -> x.DomainName = "secure.io") |> Seq.head

            let insecureBinding =
                hostnameBindings
                |> Seq.filter (fun x -> x.DomainName = "insecure.io")
                |> Seq.head

            Expect.equal secureBinding.SiteId exepectedSiteId $"HostnameBinding SiteId should be {exepectedSiteId}"

            Expect.equal insecureBinding.SiteId exepectedSiteId $"HostnameBinding SiteId should be {exepectedSiteId}"
        }

        test "Assigns correct dependencies when deploying multiple custom domains" {
            let webappName = "test"

            let resources =
                webApp {
                    name webappName
                    custom_domains [ "secure1.io"; "secure2.io"; "secure3.io" ]
                }
                |> getResources

            let wa = resources |> getResource<Web.Site> |> List.head

            let exepectedSiteId = (Managed(Arm.Web.sites.resourceId wa.Name))

            // Testing HostnameBinding
            let hostnameBindings =
                resources
                |> getResource<ResourceGroupDeployment>
                |> Seq.map (fun x -> getResource<Web.HostNameBinding> (x.Resources))
                |> Seq.concat
                |> Seq.toList

            let secureBinding1 =
                hostnameBindings
                |> List.filter (fun x -> x.DomainName = "secure1.io")
                |> List.head

            let secureBinding2 =
                hostnameBindings
                |> List.filter (fun x -> x.DomainName = "secure2.io")
                |> List.head

            let secureBinding3 =
                hostnameBindings
                |> List.filter (fun x -> x.DomainName = "secure3.io")
                |> List.head

            Expect.equal secureBinding1.SiteId exepectedSiteId $"HostnameBinding SiteId should be {exepectedSiteId}"
            Expect.equal secureBinding2.SiteId exepectedSiteId $"HostnameBinding SiteId should be {exepectedSiteId}"
            Expect.equal secureBinding3.SiteId exepectedSiteId $"HostnameBinding SiteId should be {exepectedSiteId}"

            // Testing dependencies.
            let deployments = resources |> getResource<ResourceGroupDeployment> |> Seq.toList

            let dependenciesOnOtherDeployments =
                deployments
                |> Seq.map (fun rg ->
                    rg.Dependencies
                    |> Seq.filter (fun dep -> deployments |> Seq.exists (fun x -> x.DeploymentName = dep.Name)))
                |> Seq.map (fun deps -> deps |> Seq.map (fun dep -> dep.Name))
                |> Seq.toList

            let siteDependency =
                deployments[0].Dependencies
                |> Set.filter (fun x -> x.Type = wa.ResourceType)
                |> Set.map (fun x -> x.Name)
                |> Seq.head

            Expect.hasLength deployments 9 "Should have three deploys per custom domain"
            Expect.isEmpty dependenciesOnOtherDeployments[0] "First deploy should not depend on another"
            Expect.equal siteDependency.Value webappName "First deployment should have a dependency on the site"

            seq { 1..1..8 }
            |> Seq.iter (fun x ->
                Expect.contains
                    dependenciesOnOtherDeployments[x]
                    deployments[x - 1].ResourceId.Name
                    "Each subsequent deploy should depend on previous deploy")
        }

        test "Supports adding ip restriction for allowed ip" {
            let ip = "1.2.3.4/32"

            let resources =
                webApp {
                    name "test"
                    add_allowed_ip_restriction "test-rule" ip
                }
                |> getResources

            let site = resources |> getResource<Web.Site> |> List.head

            let expectedRestriction =
                IpSecurityRestriction.Create "test-rule" (IPAddressCidr.parse ip) Allow

            Expect.equal
                site.IpSecurityRestrictions
                [ expectedRestriction ]
                "Should add allowed ip security restriction"
        }

        test "Supports adding ip restriction for denied ip" {
            let ip = IPAddressCidr.parse "1.2.3.4/32"

            let resources =
                webApp {
                    name "test"
                    add_denied_ip_restriction "test-rule" ip
                }
                |> getResources

            let site = resources |> getResource<Web.Site> |> List.head

            let expectedRestriction = IpSecurityRestriction.Create "test-rule" ip Deny

            Expect.equal site.IpSecurityRestrictions [ expectedRestriction ] "Should add denied ip security restriction"
        }

        test "Supports adding different ip restrictions to site and slot" {
            let siteIp = IPAddressCidr.parse "1.2.3.4/32"
            let slotIp = IPAddressCidr.parse "4.3.2.1/32"

            let warmupSlot = appSlot {
                name "warm-up"
                add_allowed_ip_restriction "slot-rule" slotIp
            }

            let resources =
                webApp {
                    name "test"
                    add_slot warmupSlot
                    add_allowed_ip_restriction "site-rule" siteIp
                }
                |> getResources

            let slot =
                resources
                |> getResource<Arm.Web.Site>
                |> List.filter (fun x -> x.ResourceType = Arm.Web.slots)
                |> List.head

            let site = resources |> getResource<Web.Site> |> List.head

            let expectedSlotRestriction = IpSecurityRestriction.Create "slot-rule" slotIp Allow
            let expectedSiteRestriction = IpSecurityRestriction.Create "site-rule" siteIp Allow

            Expect.equal
                slot.IpSecurityRestrictions
                [ expectedSlotRestriction ]
                "Slot should have correct allowed ip security restriction"

            Expect.equal
                site.IpSecurityRestrictions
                [ expectedSiteRestriction ]
                "Site should have correct allowed ip security restriction"
        }

        test "Linux automatically turns off logging extension" {
            let wa = webApp {
                name "siteX"
                operating_system Linux
            }

            let extensions = wa |> getResources |> getResource<SiteExtension>
            Expect.isEmpty extensions "Should not be any extensions"
        }

        test "Supports docker ports with WEBSITES_PORT" {
            let wa = webApp {
                name "testApp"
                docker_port 8080
            }

            let port = Expect.wantSome wa.DockerPort "Docker port should be set"
            Expect.equal port 8080 "Docker port should 8080"

            let site = wa |> getResources |> getResource<Web.Site> |> List.head

            let settings = Expect.wantSome site.AppSettings "AppSettings should be set"
            let (hasValue, value) = settings.TryGetValue("WEBSITES_PORT")

            Expect.isTrue hasValue "WEBSITES_PORT should be set"
            Expect.equal value.Value "8080" "WEBSITES_PORT should be 8080"

            let defaultWa = webApp { name "testApp" }
            Expect.isNone defaultWa.DockerPort "Docker port should not be set"
        }

        test "Web App enables zoneRedundant in service plan" {
            let resources =
                webApp {
                    name "test"
                    zone_redundant Enabled
                }
                |> getResources

            let sf = resources |> getResource<Web.ServerFarm> |> List.head

            Expect.equal sf.ZoneRedundant (Some Enabled) "ZoneRedundant should be enabled"
        }
        test "Can integrate with unmanaged vnet" {
            let subnetId =
                Arm.Network.subnets.resourceId (ResourceName "my-vnet", ResourceName "my-subnet")

            let wa = webApp {
                name "testApp"
                sku WebApp.Sku.S1
                link_to_unmanaged_vnet subnetId
            }

            let resources = wa |> getResources
            let site = resources |> getResource<Web.Site> |> List.head
            let vnet = Expect.wantSome site.LinkToSubnet "LinkToSubnet was not set"
            Expect.equal vnet (Direct(Unmanaged subnetId)) "LinkToSubnet was incorrect"

            let vnetConnections = resources |> getResource<Web.VirtualNetworkConnection>
            Expect.hasLength vnetConnections 1 "incorrect number of Vnet connections"
        }

        test "Can integrate with managed vnet" {
            let vnetConfig = vnet { name "my-vnet" }

            let wa = webApp {
                name "testApp"
                sku WebApp.Sku.S1
                link_to_vnet (vnetConfig, ResourceName "my-subnet")
            }

            let resources = wa |> getResources
            let site = resources |> getResource<Web.Site> |> List.head
            let vnet = Expect.wantSome site.LinkToSubnet "LinkToSubnet was not set"

            Expect.equal
                vnet
                (ViaManagedVNet((Arm.Network.virtualNetworks.resourceId "my-vnet"), ResourceName "my-subnet"))
                "LinkToSubnet was incorrect"

            let vnetConnections = resources |> getResource<Web.VirtualNetworkConnection>
            Expect.hasLength vnetConnections 1 "incorrect number of Vnet connections"
        }

        test "Supports redefining root application directory" {
            let wa = webApp {
                name "test"

                add_virtual_applications [
                    virtualApplication {
                        virtual_path "/"
                        physical_path "altdirectory"
                    }
                ]
            }

            let site = wa |> getResources |> getResource<Web.Site> |> List.head

            let expectedVirtualApplications =
                Map [
                    "/",
                    {
                        PhysicalPath = "site\\altdirectory"
                        PreloadEnabled = None
                    }
                ]

            Expect.equal
                site.VirtualApplications
                expectedVirtualApplications
                "Should add virtual application definition for root"
        }

        test "Can add startup command without docker" {
            let wa: Site =
                webApp {
                    name "test"
                    startup_command "foo"
                }
                |> getResourceAtIndex 3

            Expect.equal wa.SiteConfig.AppCommandLine "foo" "Command line not set correctly"
        }

        test "Supports defining additional virtual applications without changing root" {
            let wa = webApp {
                name "test"

                add_virtual_applications [
                    virtualApplication {
                        virtual_path "/subapp"
                        physical_path "wwwsubapp"
                    }
                ]
            }

            let site = wa |> getResources |> getResource<Web.Site> |> List.head

            let expectedVirtualApplications =
                Map [
                    ("/",
                     {
                         PhysicalPath = "site\\wwwroot"
                         PreloadEnabled = None
                     }),
                    1u
                    ("/subapp",
                     {
                         PhysicalPath = "site\\wwwsubapp"
                         PreloadEnabled = None
                     }),
                    1u
                ]

            Expect.distribution
                (site.VirtualApplications |> Seq.map (fun it -> (it.Key, it.Value)))
                expectedVirtualApplications
                "Should add virtual application definition for /subapp, but keep the root app around"
        }

        test "Supports virtual applications with preload enabled" {
            let wa = webApp {
                name "test"

                add_virtual_applications [
                    virtualApplication {
                        virtual_path "/subapp"
                        physical_path "wwwroot\\subApp"
                        preloaded
                    }
                ]
            }

            let site = wa |> getResources |> getResource<Web.Site> |> List.head

            let expectedVirtualApplications =
                Map [
                    ("/subapp",
                     {
                         PhysicalPath = "site\\wwwroot\\subApp"
                         PreloadEnabled = (Some true)
                     }),
                    1u
                ]

            Expect.distribution
                (site.VirtualApplications |> Seq.map (fun it -> (it.Key, it.Value)))
                expectedVirtualApplications
                "Should add preloaded virtual application definition"
        }
    ]
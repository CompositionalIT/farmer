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

let getResource<'T when 'T :> IArmResource> (data:IArmResource list) = data |> List.choose(function :? 'T as x -> Some x | _ -> None)
/// Client instance needed to get the serializer settings.
let dummyClient = new WebSiteManagementClient (Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")
let getResourceAtIndex o = o |> getResourceAtIndex dummyClient.SerializationSettings

let getResources (v:IBuilder) = v.BuildResources Location.WestEurope

let tests = testList "Web App Tests" [
    test "Basic Web App has service plan and AI dependencies set" {
        let resources = webApp { name "test" } |> getResources
        let wa = resources |> getResource<Web.Site> |> List.head

        Expect.containsAll wa.Dependencies [ ResourceId.create(components, ResourceName "test-ai"); ResourceId.create(serverFarms, ResourceName "test-farm") ] "Missing dependencies"
        Expect.hasLength (resources |> getResource<Insights.Components>) 1 "Should be one AI component"
        Expect.hasLength (resources |> getResource<Web.ServerFarm>) 1 "Should be one server farm"
    }
    test "Web App allows renaming of service plan and AI" {
        let resources = webApp { name "test"; service_plan_name "supersp"; app_insights_name "superai" } |> getResources
        let wa = resources |> getResource<Web.Site> |> List.head

        Expect.containsAll wa.Dependencies [ ResourceId.create(serverFarms, ResourceName "supersp"); ResourceId.create (components, ResourceName "superai") ] "Missing dependencies"
        Expect.hasLength (resources |> getResource<Insights.Components>) 1 "Should be one AI component"
        Expect.hasLength (resources |> getResource<Web.ServerFarm>) 1 "Should be one server farm"
    }
    test "Web App creates dependencies but no resources with linked AI and Server Farm configs" {
        let sp = servicePlan { name "plan" }
        let ai = appInsights { name "ai" }
        let resources = webApp { name "test"; link_to_app_insights ai; link_to_service_plan sp } |> getResources
        let wa = resources |> getResource<Web.Site> |> List.head
        Expect.containsAll wa.Dependencies [ ResourceId.create(serverFarms, ResourceName "plan"); ResourceId.create(components, ResourceName "ai") ] "Missing dependencies"
        Expect.isEmpty (resources |> getResource<Insights.Components>) "Should be no AI component"
        Expect.isEmpty (resources |> getResource<Web.ServerFarm>) "Should be no server farm"
    }
    test "Web App does not create dependencies for unmanaged linked resources" {
        let resources = webApp { name "test"; link_to_unmanaged_app_insights (components.resourceId "test"); link_to_unmanaged_service_plan (serverFarms.resourceId "test2") } |> getResources
        let wa = resources |> getResource<Web.Site> |> List.head
        Expect.isEmpty wa.Dependencies "Should be no dependencies"
        Expect.isEmpty (resources |> getResource<Insights.Components>) "Should be no AI component"
        Expect.isEmpty (resources |> getResource<Web.ServerFarm>) "Should be no server farm"
    }
    test "Web app supports adding tags to resource" {
        let resources = webApp { name "test"; add_tag "key" "value"; add_tags ["alpha","a"; "beta","b"]} |> getResources
        let wa = resources |> getResource<Web.Site> |> List.head
        Expect.containsAll (wa.Tags|> Map.toSeq)
            [ "key","value"
              "alpha","a"
              "beta","b"]
            "Should contain the given tags"
        Expect.equal 3 (wa.Tags|> Map.count) "Should not contain additional tags"
    }
    test "Web App correctly adds connection strings" {
        let sa = storageAccount { name "foo" }
        let wa =
            let resources = webApp { name "test"; connection_string "a"; connection_string ("b", sa.Key) } |> getResources
            resources |> getResource<Web.Site> |> List.head

        let expected = [
            "a", (ParameterSetting(SecureParameter "a"), Custom)
            "b", (ExpressionSetting sa.Key, Custom)
        ]
        let parameters = wa :> IParameters

        Expect.equal wa.ConnectionStrings (Map expected |> Some) "Missing connections"
        Expect.equal parameters.SecureParameters [ SecureParameter "a" ] "Missing parameter"
    }
    test "CORS works correctly" {
        let wa : Site =
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
        Expect.throws (fun () ->
            webApp {
                name "test"
                enable_cors AllOrigins
                enable_cors_credentials
            } |> ignore) "Invalid CORS combination"
    }

    test "Automatically converts from * to AllOrigins" {
        let wa : Site =
            webApp { name "test"; enable_cors [ "*" ] } |> getResourceAtIndex 3
        Expect.sequenceEqual wa.SiteConfig.Cors.AllowedOrigins [ "*" ] "Allowed Origins should be *"
    }

    test "CORS without credentials does not crash" {
        webApp { name "test"; enable_cors AllOrigins } |> ignore
        webApp { name "test"; enable_cors [ "https://bbc.co.uk" ] } |> ignore
    }

    test "If CORS is not enabled, ignores enable credentials" {
        let wa : Site =
            webApp { name "test"; enable_cors_credentials } |> getResourceAtIndex 3
        Expect.isNull wa.SiteConfig.Cors "Should be no CORS settings"
    }

    test "Implicitly adds a dependency when adding a setting" {
        let sa = storageAccount { name "teststorage" }
        let sql = sqlServer { name "test"; admin_username "user"; add_databases [ sqlDb { name "thedb" } ] }
        let wa = webApp {
            name "testweb"
            setting "storage" sa.Key
            setting "conn" (sql.ConnectionString "thedb")
            setting "bad" (ArmExpression.literal "ignore_me")
        }
        let wa = wa |> getResources |> getResource<Web.Site> |> List.head

        Expect.contains wa.Dependencies (ResourceId.create(storageAccounts, sa.Name.ResourceName)) "Storage Account is missing"
        Expect.contains wa.Dependencies (ResourceId.create(Sql.databases, ResourceName "test", ResourceName "thedb")) "Database is missing"
    }

    test "Implicitly adds a dependency when adding a connection string" {
        let sa = storageAccount { name "teststorage" }
        let wa = webApp { name "testweb"; setting "storage" sa.Key }
        let wa = wa |> getResources |> getResource<Web.Site> |> List.head
        Expect.contains wa.Dependencies (ResourceId.create(storageAccounts, sa.Name.ResourceName)) "Storage Account is missing"
    }

    test "Automatic Key Vault integration works correctly" {
        let sa = storageAccount { name "teststorage" }
        let wa = webApp { name "testweb"; setting "storage" sa.Key; secret_setting "secret"; setting "literal" "value"; use_keyvault }
        let kv = wa |> getResources |> getResource<Vault> |> List.head
        let secrets = wa |> getResources |> getResource<Vaults.Secret>
        let site = wa |> getResources |> getResource<Web.Site> |> List.head
        let vault = wa |> getResources |> getResource<Vault> |> List.head

        let expectedSettings = Map [
            "storage", LiteralSetting "@Microsoft.KeyVault(SecretUri=https://testwebvault.vault.azure.net/secrets/storage)"
            "secret", LiteralSetting "@Microsoft.KeyVault(SecretUri=https://testwebvault.vault.azure.net/secrets/secret)"
            "literal", LiteralSetting "value"
        ]

        Expect.equal site.Identity.SystemAssigned Enabled "System Identity should be enabled"
        Expect.containsAll site.AppSettings.Value expectedSettings "Incorrect settings"

        Expect.sequenceEqual kv.Dependencies [ ResourceId.create(sites, site.Name) ] "Key Vault dependencies are wrong"
        Expect.equal kv.Name (ResourceName (site.Name.Value + "vault")) "Key Vault name is wrong"
        Expect.equal wa.CommonWebConfig.Identity.SystemAssigned Enabled "System Identity should be turned on"
        Expect.equal kv.AccessPolicies.[0].ObjectId wa.SystemIdentity.PrincipalId.ArmExpression "Policy is incorrect"

        Expect.hasLength secrets 2 "Incorrect number of KV secrets"

        Expect.equal secrets.[0].Name.Value "testwebvault/storage" "Incorrect secret name"
        Expect.equal secrets.[0].Value (ExpressionSecret sa.Key) "Incorrect secret value"
        Expect.sequenceEqual secrets.[0].Dependencies [ vaults.resourceId "testwebvault"; storageAccounts.resourceId "teststorage" ] "Incorrect secret dependencies"

        Expect.equal secrets.[1].Name.Value "testwebvault/secret" "Incorrect secret name"
        Expect.equal secrets.[1].Value (ParameterSecret (SecureParameter "secret")) "Incorrect secret value"
        Expect.sequenceEqual secrets.[1].Dependencies [ vaults.resourceId "testwebvault" ] "Incorrect secret dependencies"

        Expect.hasLength vault.AccessPolicies 1 "Incorrect number of access policies"
        Expect.sequenceEqual vault.AccessPolicies.[0].Permissions.Secrets [ KeyVault.Secret.Get ] "Incorrect permissions"
    }

    test "Managed KV integration works correctly" {
        let sa = storageAccount { name "teststorage" }
        let wa = webApp { name "testweb"; setting "storage" sa.Key; secret_setting "secret"; setting "literal" "value"; link_to_keyvault (ResourceName "testwebvault") }
        let vault = keyVault { name "testwebvault"; add_access_policy (AccessPolicy.create (wa.SystemIdentity.PrincipalId, [ KeyVault.Secret.Get ])) }
        let vault = vault |> getResources |> getResource<Vault> |> List.head
        let secrets = wa |> getResources |> getResource<Vaults.Secret>
        let site = wa |> getResources |> getResource<Web.Site> |> List.head

        let expectedSettings = Map [
            "storage", LiteralSetting "@Microsoft.KeyVault(SecretUri=https://testwebvault.vault.azure.net/secrets/storage)"
            "secret", LiteralSetting "@Microsoft.KeyVault(SecretUri=https://testwebvault.vault.azure.net/secrets/secret)"
            "literal", LiteralSetting "value"
        ]

        Expect.equal site.Identity.SystemAssigned Enabled "System Identity should be enabled"
        Expect.containsAll site.AppSettings.Value expectedSettings "Incorrect settings"

        Expect.equal wa.CommonWebConfig.Identity.SystemAssigned Enabled "System Identity should be turned on"

        Expect.hasLength secrets 2 "Incorrect number of KV secrets"

        Expect.equal secrets.[0].Name.Value "testwebvault/secret" "Incorrect secret name"
        Expect.equal secrets.[0].Value (ParameterSecret (SecureParameter "secret")) "Incorrect secret value"
        Expect.sequenceEqual secrets.[0].Dependencies [ vaults.resourceId "testwebvault" ] "Incorrect secret dependencies"

        Expect.equal secrets.[1].Name.Value "testwebvault/storage" "Incorrect secret name"
        Expect.equal secrets.[1].Value (ExpressionSecret sa.Key) "Incorrect secret value"
        Expect.sequenceEqual secrets.[1].Dependencies [ vaults.resourceId "testwebvault"; storageAccounts.resourceId "teststorage" ] "Incorrect secret dependencies"
    }

    test "Handles identity correctly" {
        let wa : Site = webApp { name "testsite" } |> getResourceAtIndex 0
        Expect.isNull wa.Identity  "Default managed identity should be null"

        let wa : Site = webApp { name "othertestsite"; system_identity } |> getResourceAtIndex 3
        Expect.equal wa.Identity.Type (Nullable ManagedServiceIdentityType.SystemAssigned) "Should have system identity"
        Expect.isNull wa.Identity.UserAssignedIdentities "Should have no user assigned identities"

        let wa : Site = webApp { name "thirdtestsite"; system_identity; add_identity (createUserAssignedIdentity "test"); add_identity (createUserAssignedIdentity "test2") } |> getResourceAtIndex 3
        Expect.equal wa.Identity.Type (Nullable ManagedServiceIdentityType.SystemAssignedUserAssigned) "Should have system identity"
        Expect.sequenceEqual (wa.Identity.UserAssignedIdentities |> Seq.map(fun r -> r.Key)) [ "[resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', 'test2')]"; "[resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', 'test')]" ] "Should have two user assigned identities"
        Expect.contains (wa.SiteConfig.AppSettings |> Seq.map(fun s -> s.Name, s.Value)) ("AZURE_CLIENT_ID", "[reference(resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', 'test2')).clientId]") "Missing AZURE_CLIENT_ID"
    }

    test "Unmanaged server farm is fully qualified in ARM" {
        let farm = ResourceId.create(serverFarms, ResourceName "my-asp-name", "my-asp-resource-group")
        let wa : Site = webApp { name "test"; link_to_unmanaged_service_plan farm } |> getResourceAtIndex 2
        Expect.equal wa.ServerFarmId "[resourceId('my-asp-resource-group', 'Microsoft.Web/serverfarms', 'my-asp-name')]" ""
    }

    test "Adds the Logging extension automatically for .NET Core apps" {
        let wa = webApp { name "siteX" }
        let extension = wa |> getResources |> getResource<SiteExtension> |> List.head
        Expect.equal extension.Name.Value "Microsoft.AspNetCore.AzureAppServices.SiteExtension" "Wrong extension"

        let wa = webApp { name "siteX"; runtime_stack Runtime.Java11 }
        let extensions = wa |> getResources |> getResource<SiteExtension>
        Expect.isEmpty extensions "Shouldn't be any extensions"

        let wa = webApp { name "siteX"; automatic_logging_extension false }
        let extensions = wa |> getResources |> getResource<SiteExtension>
        Expect.isEmpty extensions "Shouldn't be any extensions"
    }

    test "Does not add the logging extension for apps using a docker image" {
        let wa = webApp { name "siteX" ; docker_image "someImage" "someCommand" }
        let extensions = wa |> getResources |> getResource<SiteExtension>
        Expect.isEmpty extensions "Shouldn't be any extensions"
    }

    test "Handles add_extension correctly" {
        let wa = webApp { name "siteX"; add_extension "extensionA"; runtime_stack Runtime.Java11 }
        let resources = wa |> getResources
        let sx = resources |> getResource<SiteExtension> |> List.head
        let r  = sx :> IArmResource

        Expect.equal sx.SiteName (ResourceName "siteX") "Extension knows the site name"
        Expect.equal sx.Location Location.WestEurope "Location is correct"
        Expect.equal sx.Name (ResourceName "extensionA") "Extension name is correct"
        Expect.equal r.ResourceId.ArmExpression.Value "resourceId('Microsoft.Web/sites/siteextensions', 'siteX/extensionA')" "Resource name composed of site name and extension name"
    }

    test "Handles multiple add_extension correctly" {
        let wa = webApp { name "siteX"; add_extension "extensionA"; add_extension "extensionB"; add_extension "extensionB"; runtime_stack Runtime.Java11 }
        let resources = wa |> getResources |> getResource<SiteExtension>

        let actual = List.sort resources
        let expected = [
            { Location = Location.WestEurope; Name = ResourceName "extensionA"; SiteName = ResourceName "siteX" }
            { Location = Location.WestEurope; Name = ResourceName "extensionB"; SiteName = ResourceName "siteX" }
        ]
        Expect.sequenceEqual actual expected "Both extensions defined"
    }

    test "SiteExtension ResourceId constructed correctly" {
        let siteName = ResourceName "siteX"
        let resourceId = siteExtensions.resourceId siteName

        Expect.equal resourceId.ArmExpression.Value "resourceId('Microsoft.Web/sites/siteextensions', 'siteX')" ""
    }

    test "Deploys AI configuration correctly" {
        let hasSetting key message (wa:Site) = Expect.isTrue (wa.SiteConfig.AppSettings |> Seq.exists(fun k -> k.Name = key)) message
        let wa : Site = webApp { name "testsite" } |> getResourceAtIndex 3
        wa |> hasSetting "APPINSIGHTS_INSTRUMENTATIONKEY" "Missing Windows instrumentation key"

        let wa : Site = webApp { name "testsite"; operating_system Linux } |> getResourceAtIndex 3
        wa |> hasSetting "APPINSIGHTS_INSTRUMENTATIONKEY" "Missing Linux instrumentation key"

        let wa : Site = webApp { name "testsite"; app_insights_off } |> getResourceAtIndex 2
        Expect.isEmpty wa.SiteConfig.AppSettings "Should be no settings"
    }

    test "Supports always on" {
        let template = webApp { name "web"; always_on }
        Expect.equal template.CommonWebConfig.AlwaysOn true "AlwaysOn should be true"

        let w:Site = webApp { name "testDefault" } |> getResourceAtIndex 3
        Expect.equal w.SiteConfig.AlwaysOn (Nullable false) "always on should be false by default"
    }

    test "Supports 32 and 64 bit worker processes" {
        let site : Site = webApp { name "web" } |> getResourceAtIndex 3
        Expect.equal site.SiteConfig.Use32BitWorkerProcess (Nullable()) "Default worker process"

        let site:Site = webApp { name "web2"; worker_process Bits32 } |> getResourceAtIndex 3
        Expect.equal site.SiteConfig.Use32BitWorkerProcess (Nullable true) "Should use 32 bit worker process"

        let site:Site = webApp { name "web3"; worker_process Bits64 } |> getResourceAtIndex 3
        Expect.equal site.SiteConfig.Use32BitWorkerProcess (Nullable false) "Should not use 32 bit worker process"
    }

    test "Supports .NET 5 EAP" {
        let app = webApp { name "net5"; runtime_stack Runtime.DotNet50 }
        let site:Site = app |> getResourceAtIndex 2
        Expect.equal site.SiteConfig.NetFrameworkVersion "v5.0" "Wrong dotnet version"
    }

    test "WebApp supports adding slots" {
        let slot = appSlot { name "warm-up" }
        let site:WebAppConfig = webApp { name "slots"; add_slot slot }
        Expect.isTrue (site.CommonWebConfig.Slots.ContainsKey "warm-up") "Config should contain slot"

        let slots =
            site
            |> getResources
            |> getResource<Arm.Web.Site>
            |> List.filter (fun x -> x.ResourceType = Arm.Web.slots)
        // Default "production" slot is not included as it is created automatically in Azure
        Expect.hasLength slots 1 "Should only be 1 slot"
    }

    test "WebApp with slot that has system assigned identity adds identity to slot" {
        let slot = appSlot { name "warm-up"; system_identity }
        let site:WebAppConfig = webApp { name "webapp"; add_slot slot }
        Expect.isTrue (site.CommonWebConfig.Slots.ContainsKey "warm-up") "Config should contain slot"

        let slots =
            site
            |> getResources
            |> getResource<Arm.Web.Site>
        // Default "production" slot is not included as it is created automatically in Azure
        Expect.hasLength slots 2 "Should only be 1 slot and 1 site"

        let expected = { SystemAssigned = Enabled; UserAssigned = [] }
        Expect.equal (slots.[1]).Identity expected "Slot should have slot setting"
    }

    test "WebApp with slot adds settings to slot" {
        let slot = appSlot { name "warm-up" }
        let site:WebAppConfig = webApp {
            name "slotsettings";
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

        Expect.isTrue ((slots.Item 0).AppSettings.Value.ContainsKey("setting")) "Slot should have slot setting"
    }

    test "WebApp with slot does not add settings to app service" {
        let slot = appSlot { name "warm-up" }
        let config = webApp {
            name "web"
            add_slot slot
            setting "setting" "some value"
            connection_string "DB"
        }

        let sites =
            config
            |> getResources
            |> getResource<Farmer.Arm.Web.Site>

        // Default "production" slot is not included as it is created automatically in Azure
        Expect.hasLength sites 2 "Should only be 1 slot and 1 site"

        Expect.isNone ((sites.[0]).AppSettings) "App service should not have any settings"
        Expect.isNone ((sites.[0]).ConnectionStrings) "App service should not have any settings"
    }

    test "WebApp adds literal settings to slots" {
        let slot = appSlot { name "warm-up" }
        let site:WebAppConfig = webApp {
            name "web"
            add_slot slot
            run_from_package
            website_node_default_version "xxx"
            docker_ci
            docker_use_azure_registry "registry" }
        Expect.isTrue (site.CommonWebConfig.Slots.ContainsKey "warm-up") "Config should contain slot"

        let sites = site |> getResources |> getResource<Arm.Web.Site>
        let slots = sites |> List.filter (fun x -> x.ResourceType = Arm.Web.slots)
        // Default "production" slot is not included as it is created automatically in Azure
        Expect.hasLength slots 1 "Should only be 1 slot"

        let settings = (slots.Item 0).AppSettings
        let expectation =
            [ "APPINSIGHTS_INSTRUMENTATIONKEY"
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
              "DOCKER_REGISTRY_SERVER_USERNAME"]
            |> List.map(settings.Value.ContainsKey)
        Expect.allEqual expectation true "Slot should have all literal settings"
    }

    test "WebApp with different settings on slot and service adds both settings to slot" {
        let slot = appSlot {
            name "warm-up"
            setting "slot" "slot value"
        }
        let site:WebAppConfig = webApp {
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

        let settings = (slots.Item 0).AppSettings;
        Expect.isTrue (settings.Value.ContainsKey("slot")) "Slot should have slot setting"
        Expect.isTrue (settings.Value.ContainsKey("appService")) "Slot should have app service setting"
    }

    test "WebApp with slot, slot settings override app service setting" {
        let slot = appSlot {
            name "warm-up"
            setting "override" "overridden"
        }
        let site:WebAppConfig = webApp {
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

        let (hasValue, value) = (slots.Item 0).AppSettings.Value.TryGetValue("override");

        Expect.isTrue hasValue "Slot should have app service setting"
        Expect.equal value.Value "overridden" "Slot should have correct app service value"
    }

    test "WebApp with slot adds connection strings to slot" {
        let slot = appSlot { name "warm-up" }
        let site:WebAppConfig = webApp {
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

        Expect.isTrue ((slots.Item 0).ConnectionStrings.Value.ContainsKey("connection_string")) "Slot should have app service connection string"
    }

    test "WebApp with different connection strings on slot and service adds both to slot" {
        let slot = appSlot {
            name "warm-up"
            connection_string "slot"
        }
        let site:WebAppConfig = webApp {
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

        Expect.equal ((slots.Item 0).ConnectionStrings.Value.Count) 2 "Slot should have two connection strings"
    }

    test "WebApp with slots and identity applies identity to slots" {
        let identity18 = userAssignedIdentity { name "im-18" }
        let identity21 = userAssignedIdentity { name "im-21" }
        let slot = appSlot{
            name "deploy"
            keyvault_identity identity21
        }
        let site:WebAppConfig = webApp {
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
        Expect.containsAll (theSlot.Identity.UserAssigned) [identity18.UserAssignedIdentity; identity21.UserAssignedIdentity] "Slot should have both user assigned identities"
        Expect.equal theSlot.KeyVaultReferenceIdentity (Some identity21.UserAssignedIdentity) "Slot should have correct keyvault identity"
    }

    test "Supports private endpoints" {
        let subnet = ResourceId.create(Network.subnets,ResourceName "subnet")
        let app = webApp { name "farmerWebApp"; add_private_endpoint (Managed subnet, "myWebApp-ep")}
        let ep:Microsoft.Azure.Management.Network.Models.PrivateEndpoint = app |> getResourceAtIndex 4
        Expect.equal ep.Name "myWebApp-ep" "Incorrect name"
        Expect.hasLength ep.PrivateLinkServiceConnections.[0].GroupIds 1 "Incorrect group ids length"
        Expect.equal ep.PrivateLinkServiceConnections.[0].GroupIds.[0] "sites" "Incorrect group ids"
        Expect.equal ep.PrivateLinkServiceConnections.[0].PrivateLinkServiceId "[resourceId('Microsoft.Web/sites', 'farmerWebApp')]" "Incorrect PrivateLinkServiceId"
        Expect.equal ep.Subnet.Id (subnet.ArmExpression.Eval()) "Incorrect subnet id"
    }

    test "Supports keyvault reference identity" {
        let app = webApp { name "farmerWebApp"}
        let site:Site = app |> getResourceAtIndex 3
        Expect.isNull site.KeyVaultReferenceIdentity "Keyvault identity should not be set"

        let myId = userAssignedIdentity { name "myFarmerIdentity" }
        let app = webApp { name "farmerWebApp"; keyvault_identity myId }
        let site:Site = app |> getResourceAtIndex 3
        Expect.equal site.KeyVaultReferenceIdentity "[resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', 'myFarmerIdentity')]" "Keyvault identity should not be set"
    }

    test "Validates name correctly" {
        let check (v:string) m = Expect.equal (WebAppName.Create v) (Error ("Web App site names " + m))

        check "" "cannot be empty" "Name too short"
        let longName = Array.init 61 (fun _ -> 'a') |> String
        check longName $"max length is 60, but here is 61. The invalid value is '{longName}'" "Name too long"
        check "zz!z" "can only contain alphanumeric characters or the dash (-). The invalid value is 'zz!z'" "Bad character allowed"
        check "-zz" "cannot start with a dash (-). The invalid value is '-zz'" "Start with dash"
        check "zz-" "cannot end with a dash (-). The invalid value is 'zz-'" "End with dash"
    }

    test "Not setting the web app name causes an error" {
        Expect.throws (fun () -> webApp { runtime_stack Runtime.Java11 } |> ignore) "Not setting web app name should throw"
    }

    test "Supports health check" {
        let resources = webApp { name "test"; health_check_path "/status" } |> getResources
        let wa = resources |> getResource<Web.Site> |> List.head

        Expect.equal wa.HealthCheckPath (Some "/status") "Health check path should be '/status'"
    }
]

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

        Expect.equal wa.ConnectionStrings (Map expected) "Missing connections"
        Expect.equal parameters.SecureParameters [ SecureParameter "a" ] "Missing parameter"
    }
    test "CORS works correctly" {
        let wa : Site =
            webApp {
                name "test"
                enable_cors [ "https://bbc.co.uk" ]
                enable_cors_credentials
            }
            |> getResourceAtIndex 0
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
            webApp { name "test"; enable_cors [ "*" ] } |> getResourceAtIndex 0
        Expect.sequenceEqual wa.SiteConfig.Cors.AllowedOrigins [ "*" ] "Allowed Origins should be *"
    }

    test "CORS without credentials does not crash" {
        webApp { name "test"; enable_cors AllOrigins } |> ignore
        webApp { name "test"; enable_cors [ "https://bbc.co.uk" ] } |> ignore
    }

    test "If CORS is not enabled, ignores enable credentials" {
        let wa : Site =
            webApp { name "test"; enable_cors_credentials } |> getResourceAtIndex 0
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
        Expect.containsAll site.AppSettings expectedSettings "Incorrect settings"

        Expect.sequenceEqual kv.Dependencies [ ResourceId.create(sites, site.Name) ] "Key Vault dependencies are wrong"
        Expect.equal kv.Name (ResourceName (site.Name.Value + "vault")) "Key Vault name is wrong"
        Expect.equal wa.Identity.SystemAssigned Enabled "System Identity should be turned on"
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
        Expect.containsAll site.AppSettings expectedSettings "Incorrect settings"

        Expect.equal wa.Identity.SystemAssigned Enabled "System Identity should be turned on"

        Expect.hasLength secrets 2 "Incorrect number of KV secrets"

        Expect.equal secrets.[0].Name.Value "testwebvault/secret" "Incorrect secret name"
        Expect.equal secrets.[0].Value (ParameterSecret (SecureParameter "secret")) "Incorrect secret value"
        Expect.sequenceEqual secrets.[0].Dependencies [ vaults.resourceId "testwebvault" ] "Incorrect secret dependencies"

        Expect.equal secrets.[1].Name.Value "testwebvault/storage" "Incorrect secret name"
        Expect.equal secrets.[1].Value (ExpressionSecret sa.Key) "Incorrect secret value"
        Expect.sequenceEqual secrets.[1].Dependencies [ vaults.resourceId "testwebvault"; storageAccounts.resourceId "teststorage" ] "Incorrect secret dependencies"
    }

    test "Handles identity correctly" {
        let wa : Site = webApp { name "" } |> getResourceAtIndex 0
        Expect.equal wa.Identity.Type (Nullable ManagedServiceIdentityType.None) "Incorrect default managed identity"
        Expect.isNull wa.Identity.UserAssignedIdentities "Should be no user assigned identities"

        let wa : Site = webApp { system_identity } |> getResourceAtIndex 0
        Expect.equal wa.Identity.Type (Nullable ManagedServiceIdentityType.SystemAssigned) "Should have system identity"
        Expect.isNull wa.Identity.UserAssignedIdentities "Should have no user assigned identities"

        let wa : Site = webApp { system_identity; add_identity (createUserAssignedIdentity "test"); add_identity (createUserAssignedIdentity "test2") } |> getResourceAtIndex 0
        Expect.equal wa.Identity.Type (Nullable ManagedServiceIdentityType.SystemAssignedUserAssigned) "Should have system identity"
        Expect.sequenceEqual (wa.Identity.UserAssignedIdentities |> Seq.map(fun r -> r.Key)) [ "[resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', 'test2')]"; "[resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', 'test')]" ] "Should have two user assigned identities"
        Expect.contains (wa.SiteConfig.AppSettings |> Seq.map(fun s -> s.Name, s.Value)) ("AZURE_CLIENT_ID", "[reference(resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', 'test2')).clientId]") "Missing AZURE_CLIENT_ID"
    }

    test "Unmanaged server farm is fully qualified in ARM" {
        let farm = ResourceId.create(serverFarms, ResourceName "my-asp-name", "my-asp-resource-group")
        let wa : Site = webApp { name "test"; link_to_unmanaged_service_plan farm } |> getResourceAtIndex 0
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
        let wa : Site = webApp { name "" } |> getResourceAtIndex 0
        wa |> hasSetting "APPINSIGHTS_INSTRUMENTATIONKEY" "Missing Windows instrumentation key"

        let wa : Site = webApp { name ""; operating_system Linux } |> getResourceAtIndex 0
        wa |> hasSetting "APPINSIGHTS_INSTRUMENTATIONKEY" "Missing Linux instrumentation key"

        let wa : Site = webApp { name ""; app_insights_off } |> getResourceAtIndex 0
        Expect.isEmpty wa.SiteConfig.AppSettings "Should be no settings"
    }

    test "Supports always on" {
        let template = webApp { name "web"; always_on }
        Expect.equal template.AlwaysOn true "AlwaysOn should be true"

        let w:Site = webApp { name "testDefault" } |> getResourceAtIndex 0
        Expect.equal w.SiteConfig.AlwaysOn (Nullable false) "always on should be false by default"
    }

    test "Supports 32 and 64 bit worker processes" {
        let site : Site = webApp { name "w" } |> getResourceAtIndex 0
        Expect.equal site.SiteConfig.Use32BitWorkerProcess (Nullable()) "Default worker process"

        let site:Site = webApp { worker_process Bits32 } |> getResourceAtIndex 0
        Expect.equal site.SiteConfig.Use32BitWorkerProcess (Nullable true) "Should use 32 bit worker process"

        let site:Site = webApp { worker_process Bits64 } |> getResourceAtIndex 0
        Expect.equal site.SiteConfig.Use32BitWorkerProcess (Nullable false) "Should not use 32 bit worker process"
    }
    test "Supports .NET 5 EAP" {
        let app = webApp { runtime_stack Runtime.DotNet50 }
        let site:Site = app |> getResourceAtIndex 0
        Expect.equal site.SiteConfig.NetFrameworkVersion "v5.0" "Wrong dotnet version"
    }
]
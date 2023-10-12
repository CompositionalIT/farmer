module Functions

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Arm
open Microsoft.Azure.Management.WebSites
open Microsoft.Azure.Management.WebSites.Models
open Microsoft.Rest
open System
open Farmer.WebApp
open Farmer.Identity

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

let getResources (b: #IBuilder) = b.BuildResources Location.WestEurope

let tests =
    testList
        "Functions tests"
        [
            test "Renames storage account correctly" {
                let f =
                    functions {
                        name "test"
                        storage_account_name "foo"
                    }

                let resources = getResources f
                let site = resources.[3] :?> Web.Site
                let storage = resources.[1] :?> Storage.StorageAccount

                Expect.contains
                    site.Dependencies
                    (storageAccounts.resourceId "foo")
                    "Storage account has not been added a dependency"

                Expect.equal f.StorageAccountId.Name.Value "foo" "Incorrect storage account name on site"
                Expect.equal storage.Name.ResourceName.Value "foo" "Incorrect storage account name"
            }
            test "Implicitly sets dependency on connection string" {
                let db = sqlDb { name "mySql" }

                let sql =
                    sqlServer {
                        name "test2"
                        admin_username "isaac"
                        add_databases [ db ]
                    }

                let f =
                    functions {
                        name "test"
                        storage_account_name "foo"
                        setting "db" (sql.ConnectionString db)
                    }
                    :> IBuilder

                let site = f.BuildResources Location.NorthEurope |> List.item 3 :?> Web.Site

                Expect.contains
                    site.Dependencies
                    (ResourceId.create (Sql.databases, ResourceName "test2", ResourceName "mySql"))
                    "Missing dependency"
            }
            test "Works with unmanaged storage account" {
                let externalStorageAccount =
                    ResourceId.create (storageAccounts, ResourceName "foo", "group")

                let functionsBuilder =
                    functions {
                        name "test"
                        link_to_unmanaged_storage_account externalStorageAccount
                        use_extension_version V1
                    }

                let f = functionsBuilder :> IBuilder
                let resources = getResources f
                let site = resources |> List.item 2 :?> Web.Site

                Expect.isFalse
                    (resources |> List.exists (fun r -> r.ResourceId.Type = storageAccounts))
                    "Storage Account should not exist"

                Expect.isFalse (site.Dependencies |> Set.contains externalStorageAccount) "Should not be a dependency"
                let settings = Expect.wantSome site.AppSettings "AppSettings should be set"

                Expect.stringContains
                    settings.["AzureWebJobsStorage"].Value
                    "foo"
                    "Web Jobs Storage setting should have storage account name"

                Expect.stringContains
                    settings.["AzureWebJobsDashboard"].Value
                    "foo"
                    "Web Jobs Dashboard setting should have storage account name"
            }
            test "Handles identity correctly" {
                let f: Site = functions { name "testfunc" } |> getResourceAtIndex 0
                Expect.isNull f.Identity "Default managed identity should be null"

                let f: Site =
                    functions {
                        name "func2"
                        system_identity
                    }
                    |> getResourceAtIndex 3

                Expect.equal
                    f.Identity.Type
                    (Nullable ManagedServiceIdentityType.SystemAssigned)
                    "Should have system identity"

                Expect.isNull f.Identity.UserAssignedIdentities "Should have no user assigned identities"

                let f: Site =
                    functions {
                        name "func3"
                        system_identity
                        add_identity (createUserAssignedIdentity "test")
                        add_identity (createUserAssignedIdentity "test2")
                    }
                    |> getResourceAtIndex 3

                Expect.equal
                    f.Identity.Type
                    (Nullable ManagedServiceIdentityType.SystemAssignedUserAssigned)
                    "Should have system identity"

                Expect.sequenceEqual
                    (f.Identity.UserAssignedIdentities |> Seq.map (fun r -> r.Key))
                    [
                        "[resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', 'test2')]"
                        "[resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', 'test')]"
                    ]
                    "Should have two user assigned identities"

            }

            test "Supports always on" {
                let f: Site = functions { name "testfunc" } |> getResourceAtIndex 3
                Expect.equal f.SiteConfig.AlwaysOn (Nullable false) "always on should be false by default"

                let f: Site =
                    functions {
                        name "func2"
                        always_on
                    }
                    |> getResourceAtIndex 3

                Expect.equal f.SiteConfig.AlwaysOn (Nullable true) "always on should be true"
            }

            test "Supports 32 and 64 bit worker processes" {
                let f: Site =
                    functions {
                        name "func"
                        worker_process Bitness.Bits32
                    }
                    |> getResourceAtIndex 3

                Expect.equal f.SiteConfig.Use32BitWorkerProcess (Nullable true) "Should use 32 bit worker process"

                let f: Site =
                    functions {
                        name "func2"
                        worker_process Bitness.Bits64
                    }
                    |> getResourceAtIndex 3

                Expect.equal f.SiteConfig.Use32BitWorkerProcess (Nullable false) "Should not use 32 bit worker process"
            }

            test "Managed KV integration works correctly" {
                let sa = storageAccount { name "teststorage" }

                let wa =
                    functions {
                        name "testfunc"
                        setting "storage" sa.Key
                        secret_setting "secret"
                        setting "literal" "value"
                        link_to_keyvault (ResourceName "testfuncvault")
                    }

                let vault =
                    keyVault {
                        name "testfuncvault"
                        add_access_policy (AccessPolicy.create (wa.SystemIdentity.PrincipalId, [ KeyVault.Secret.Get ]))
                    }

                let vault = vault |> getResources |> getResource<Vault> |> List.head
                let secrets = wa |> getResources |> getResource<Vaults.Secret>
                let site = wa |> getResources |> getResource<Web.Site> |> List.head

                let expectedSettings =
                    Map
                        [
                            "storage",
                            LiteralSetting
                                "@Microsoft.KeyVault(SecretUri=https://testfuncvault.vault.azure.net/secrets/storage)"
                            "secret",
                            LiteralSetting
                                "@Microsoft.KeyVault(SecretUri=https://testfuncvault.vault.azure.net/secrets/secret)"
                            "literal", LiteralSetting "value"
                        ]

                Expect.equal site.Identity.SystemAssigned Enabled "System Identity should be enabled"
                let settings = Expect.wantSome site.AppSettings "AppSettings should be set"
                Expect.containsAll settings expectedSettings "Incorrect settings"

                Expect.equal wa.CommonWebConfig.Identity.SystemAssigned Enabled "System Identity should be turned on"

                Expect.hasLength secrets 2 "Incorrect number of KV secrets"

                Expect.equal secrets.[0].Name.Value "testfuncvault/secret" "Incorrect secret name"
                Expect.equal secrets.[0].Value (ParameterSecret(SecureParameter "secret")) "Incorrect secret value"

                Expect.sequenceEqual
                    secrets.[0].Dependencies
                    [ vaults.resourceId "testfuncvault" ]
                    "Incorrect secret dependencies"

                Expect.equal secrets.[1].Name.Value "testfuncvault/storage" "Incorrect secret name"
                Expect.equal secrets.[1].Value (ExpressionSecret sa.Key) "Incorrect secret value"

                Expect.sequenceEqual
                    secrets.[1].Dependencies
                    [ vaults.resourceId "testfuncvault"; storageAccounts.resourceId "teststorage" ]
                    "Incorrect secret dependencies"
            }

            test "Supports dotnet-isolated runtime" {
                let f =
                    functions {
                        name "func"
                        use_runtime (FunctionsRuntime.DotNetIsolated)
                    }

                let resources = (f :> IBuilder).BuildResources Location.WestEurope
                let site = resources.[3] :?> Web.Site
                let settings = Expect.wantSome site.AppSettings "AppSettings should be set"

                Expect.equal
                    settings.["FUNCTIONS_WORKER_RUNTIME"]
                    (LiteralSetting "dotnet-isolated")
                    "Should use dotnet-isolated functions runtime"
            }

            test "Sets LinuxFxVersion correctly for dotnet runtimes" {
                let getLinuxFxVersion f =
                    let resources = (f :> IBuilder).BuildResources Location.WestEurope
                    let site = resources.[3] :?> Web.Site
                    site.LinuxFxVersion

                let f =
                    functions {
                        name "func"
                        use_runtime (FunctionsRuntime.DotNet50Isolated)
                        operating_system Linux
                    }

                Expect.equal (getLinuxFxVersion f) (Some "DOTNET-ISOLATED|5.0") "Should set linux fx runtime"

                let f =
                    functions {
                        name "func"
                        use_runtime (FunctionsRuntime.DotNet60Isolated)
                        operating_system Linux
                    }

                Expect.equal (getLinuxFxVersion f) (Some "DOTNET-ISOLATED|6.0") "Should set linux fx runtime"

                let f =
                    functions {
                        name "func"
                        use_runtime (FunctionsRuntime.DotNetCore31)
                        operating_system Linux
                    }

                Expect.equal (getLinuxFxVersion f) (Some "DOTNETCORE|3.1") "Should set linux fx runtime"

            }

            test "FunctionsApp supports adding slots" {
                let slot = appSlot { name "warm-up" }

                let site =
                    functions {
                        name "func"
                        add_slot slot
                    }

                Expect.isTrue (site.CommonWebConfig.Slots.ContainsKey "warm-up") "config should contain slot"

                let slots =
                    site
                    |> getResources
                    |> getResource<Arm.Web.Site>
                    |> List.filter (fun s -> s.ResourceType = Arm.Web.slots)

                Expect.hasLength slots 1 "Should only be 1 slot"
            }

            test "Functions App with slot that has system assigned identity adds identity to slot" {
                let slot =
                    appSlot {
                        name "warm-up"
                        system_identity
                    }

                let site =
                    functions {
                        name "func"
                        add_slot slot
                    }

                Expect.isTrue (site.CommonWebConfig.Slots.ContainsKey "warm-up") "Config should contain slot"

                let slots =
                    site
                    |> getResources
                    |> getResource<Arm.Web.Site>
                    |> List.filter (fun s -> s.ResourceType = Arm.Web.slots)
                // Default "production" slot is not included as it is created automatically in Azure
                Expect.hasLength slots 1 "Should only be 1 slot"

                let expected =
                    {
                        SystemAssigned = Enabled
                        UserAssigned = []
                    }

                Expect.equal (slots.Item 0).Identity expected "Slot should have slot setting"
            }

            test "Functions App with slot adds settings to slot" {
                let slot = appSlot { name "warm-up" }

                let site =
                    functions {
                        name "func"
                        add_slot slot
                        setting "setting" "some value"
                    }

                Expect.isTrue (site.CommonWebConfig.Slots.ContainsKey "warm-up") "Config should contain slot"

                let slots =
                    site
                    |> getResources
                    |> getResource<Arm.Web.Site>
                    |> List.filter (fun s -> s.ResourceType = Arm.Web.slots)
                // Default "production" slot is not included as it is created automatically in Azure
                Expect.hasLength slots 1 "Should only be 1 slot"

                let settings = Expect.wantSome slots.[0].AppSettings "AppSettings should be set"
                Expect.isTrue (settings.ContainsKey("setting")) "Slot should have slot setting"
            }

            test "Functions App with slot does not add settings to app service" {
                let slot = appSlot { name "warm-up" }

                let config =
                    functions {
                        name "func"
                        add_slot slot
                        setting "setting" "some value"
                    }

                let sites = config |> getResources |> getResource<Farmer.Arm.Web.Site>
                let slots = sites |> List.filter (fun s -> s.ResourceType = Arm.Web.slots)

                // Default "production" slot is not included as it is created automatically in Azure
                Expect.hasLength slots 1 "Should only be 1 slot"

                Expect.isNone (sites.[0].AppSettings) "App service should not have any settings"
                Expect.isNone (sites.[0].ConnectionStrings) "App service should not have any connection strings"
            }

            test "Functions App adds literal settings to slots" {
                let slot = appSlot { name "warm-up" }

                let site =
                    functions {
                        name "func"
                        add_slot slot
                        operating_system Windows
                    }

                Expect.isTrue (site.CommonWebConfig.Slots.ContainsKey "warm-up") "Config should contain slot"

                let slots =
                    site
                    |> getResources
                    |> getResource<Arm.Web.Site>
                    |> List.filter (fun s -> s.ResourceType = Arm.Web.slots)
                // Default "production" slot is not included as it is created automatically in Azure
                Expect.hasLength slots 1 "Should only be 1 slot"

                let settings = (slots.Item 0).AppSettings |> Option.defaultValue Map.empty

                let expectation =
                    [
                        "FUNCTIONS_WORKER_RUNTIME"
                        "WEBSITE_NODE_DEFAULT_VERSION"
                        "FUNCTIONS_EXTENSION_VERSION"
                        "AzureWebJobsStorage"
                        "APPINSIGHTS_INSTRUMENTATIONKEY"
                        "WEBSITE_CONTENTAZUREFILECONNECTIONSTRING"
                        "WEBSITE_CONTENTSHARE"
                    ]
                    |> List.map settings.ContainsKey

                Expect.allEqual expectation true "Slot should have all literal settings"
            }

            test "Functions App generates AzureWebJobsDashboard setting on version 1" {
                let slot = appSlot { name "warm-up" }

                let site =
                    functions {
                        name "func"
                        add_slot slot
                        operating_system Windows
                        use_extension_version V1
                    }

                let slots =
                    site
                    |> getResources
                    |> getResource<Arm.Web.Site>
                    |> List.filter (fun s -> s.ResourceType = Arm.Web.slots)

                let settings = (slots.Item 0).AppSettings |> Option.defaultValue Map.empty

                let expectation = [ "AzureWebJobsDashboard" ] |> List.map settings.ContainsKey

                Expect.allEqual expectation true "Version 1 function should have AzureWebJobsDashboard setting"
            }
            let cases = [ V2; V3; V4 ]

            for version in cases do
                test $"Functions App AzureWebJobsDashboard setting is not set on version {version.ArmValue}" {
                    let slot = appSlot { name "warm-up" }

                    let site =
                        functions {
                            name "func"
                            add_slot slot
                            operating_system Windows
                            use_extension_version version
                        }

                    let slots =
                        site
                        |> getResources
                        |> getResource<Arm.Web.Site>
                        |> List.filter (fun s -> s.ResourceType = Arm.Web.slots)

                    let settings = (slots.Item 0).AppSettings |> Option.defaultValue Map.empty

                    let expectation = settings.ContainsKey "AzureWebJobsDashboard"

                    Expect.isFalse
                        expectation
                        $"Version {version.ArmValue} function should not have AzureWebJobsDashboard setting"
                }

            test "Functions App with different settings on slot and service adds both settings to slot" {
                let slot =
                    appSlot {
                        name "warm-up"
                        setting "slot" "slot value"
                    }

                let site =
                    functions {
                        name "testfunc"
                        add_slot slot
                        setting "appService" "app service value"
                    }

                Expect.isTrue (site.CommonWebConfig.Slots.ContainsKey "warm-up") "Config should contain slot"

                let slots =
                    site
                    |> getResources
                    |> getResource<Arm.Web.Site>
                    |> List.filter (fun s -> s.ResourceType = Arm.Web.slots)
                // Default "production" slot is not included as it is created automatically in Azure
                Expect.hasLength slots 1 "Should only be 1 slot"

                let settings = Expect.wantSome slots.[0].AppSettings "AppSettings should be set"
                Expect.isTrue (settings.ContainsKey("slot")) "Slot should have slot setting"
                Expect.isTrue (settings.ContainsKey("appService")) "Slot should have app service setting"
            }

            test "Functions App with slot, slot settings override app service setting" {
                let slot =
                    appSlot {
                        name "warm-up"
                        setting "override" "overridden"
                    }

                let site =
                    functions {
                        name "testfunc"
                        add_slot slot
                        setting "override" "some value"
                    }

                Expect.isTrue (site.CommonWebConfig.Slots.ContainsKey "warm-up") "Config should contain slot"

                let sites = site |> getResources |> getResource<Arm.Web.Site>
                let slots = sites |> List.filter (fun s -> s.ResourceType = Arm.Web.slots)
                // Default "production" slot is not included as it is created automatically in Azure
                Expect.hasLength slots 1 "Should only be 1 slot"

                let settings = Expect.wantSome slots.[0].AppSettings "AppSettings should be set"
                let (hasValue, value) = settings.TryGetValue("override")

                Expect.isTrue hasValue "Slot should have app service setting"
                Expect.equal value.Value "overridden" "Slot should have correct app service value"
            }

            test "Publish as docker container" {
                let f =
                    functions {
                        name "func"

                        publish_as (
                            DockerContainer(docker (new Uri("http://www.farmer.io")) "Robert Lewandowski" "do it")
                        )
                    }

                let resources = (f :> IBuilder).BuildResources Location.WestEurope
                let site = resources.[3] :?> Web.Site
                let settings = Expect.wantSome site.AppSettings "AppSettings should be set"
                Expect.equal settings.["DOCKER_REGISTRY_SERVER_URL"] (LiteralSetting "http://www.farmer.io/") ""
                Expect.equal settings.["DOCKER_REGISTRY_SERVER_USERNAME"] (LiteralSetting "Robert Lewandowski") ""

                Expect.equal
                    settings.["DOCKER_REGISTRY_SERVER_PASSWORD"]
                    (LiteralSetting "[parameters('Robert Lewandowski-password')]")
                    ""

                Expect.equal site.AppCommandLine (Some "do it") ""
            }

            test "Service plans support Elastic Premium functions" {
                let sp =
                    servicePlan {
                        name "test"
                        sku WebApp.Sku.EP2
                        max_elastic_workers 25
                    }

                let resources = (sp :> IBuilder).BuildResources Location.WestEurope
                let serverFarm = resources.[0] :?> Web.ServerFarm

                Expect.equal serverFarm.Sku (ElasticPremium "EP2") "Incorrect SKU"
                Expect.equal serverFarm.Kind (Some "elastic") "Incorrect Kind"
                Expect.equal serverFarm.MaximumElasticWorkerCount (Some 25) "Incorrect worker count"
            }

            test "Supports health check" {
                let f: Site =
                    functions {
                        name "test"
                        health_check_path "/status"
                    }
                    |> getResourceAtIndex 3

                Expect.equal f.SiteConfig.HealthCheckPath "/status" "Health check path should be '/status'"
            }

            test "Not setting the functions name causes an error" {
                Expect.throws
                    (fun () -> functions { storage_account_name "foo" } |> ignore)
                    "Not setting functions name should throw"
            }

            test "Sets ftp state correctly in builder" {
                let f =
                    functions {
                        name "test"
                        ftp_state FTPState.Disabled
                    }
                    :> IBuilder

                let site = f.BuildResources Location.NorthEurope |> List.item 3 :?> Web.Site
                Expect.equal site.FTPState (Some FTPState.Disabled) "Incorrect FTP state set"
            }

            test "Sets ftp state correctly to 'disabled'" {
                let f =
                    functions {
                        name "test"
                        ftp_state FTPState.Disabled
                    }
                    :> IBuilder

                let deployment = arm { add_resource f }
                let jobj = Newtonsoft.Json.Linq.JObject.Parse(deployment.Template |> Writer.toJson)

                let ftpsStateValue =
                    jobj.SelectToken "resources[?(@.name=='test')].properties.siteConfig.ftpsState"
                    |> string

                Expect.equal
                    ftpsStateValue
                    "Disabled"
                    $"Incorrect value ('{ftpsStateValue}') set for ftpsState in generated template"
            }

            test "Correctly supports unmanaged storage account" {
                let functionsApp =
                    functions {
                        name "func"
                        use_extension_version V1

                        link_to_unmanaged_storage_account (
                            ResourceId.create (
                                Farmer.Arm.Storage.storageAccounts,
                                ResourceName "accountName",
                                group = "shared-group"
                            )
                        )
                    }

                let template = arm { add_resource functionsApp }
                let jsn = template.Template |> Writer.toJson
                let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

                let appSettings =
                    jobj.SelectTokens
                        "$..resources[?(@type=='Microsoft.Web/sites')].properties.siteConfig.appSettings.[*]"
                    |> Seq.map (fun x -> x.ToObject<{| name: string; value: string |}>())

                Expect.contains
                    appSettings
                    {|
                        name = "AzureWebJobsDashboard"
                        value =
                            "[concat('DefaultEndpointsProtocol=https;AccountName=accountName;AccountKey=', listKeys(resourceId('shared-group', 'Microsoft.Storage/storageAccounts', 'accountName'), '2017-10-01').keys[0].value, ';EndpointSuffix=', environment().suffixes.storage)]"
                    |}
                    "Invalid value for AzureWebJobsDashboard"

            }

            test "Correctly supports unmanaged App Insights" {
                let functionsApp =
                    functions {
                        name "func"

                        link_to_unmanaged_app_insights (
                            ResourceId.create (
                                Farmer.Arm.Insights.components,
                                ResourceName "theName",
                                group = "shared-group"
                            )
                        )
                    }

                let template = arm { add_resource functionsApp }
                let jsn = template.Template |> Writer.toJson
                let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

                let appSettings =
                    jobj.SelectTokens
                        "$..resources[?(@type=='Microsoft.Web/sites')].properties.siteConfig.appSettings.[*]"
                    |> Seq.map (fun x -> x.ToObject<{| name: string; value: string |}>())

                Expect.contains
                    appSettings
                    {|
                        name = "APPINSIGHTS_INSTRUMENTATIONKEY"
                        value =
                            "[reference(resourceId('shared-group', 'Microsoft.Insights/components', 'theName'), '2014-04-01').InstrumentationKey]"
                    |}
                    "Invalid value for APPINSIGHTS_INSTRUMENTATIONKEY"
            }

            test "Function app correctly adds connection strings" {
                let sa = storageAccount { name "foo" }

                let wa =
                    let resources =
                        functions {
                            name "test"
                            connection_string "a"
                            connection_string ("b", sa.Key)
                        }
                        |> getResources

                    resources |> getResource<Web.Site> |> List.head

                let expected =
                    [
                        "a", (ParameterSetting(SecureParameter "a"), Custom)
                        "b", (ExpressionSetting sa.Key, Custom)
                    ]

                let parameters = wa :> IParameters

                Expect.equal wa.ConnectionStrings (Map expected |> Some) "Missing connections"
                Expect.equal parameters.SecureParameters [ SecureParameter "a" ] "Missing parameter"
            }

            test "Supports adding ip restriction" {
                let ip = IPAddressCidr.parse "1.2.3.4/32"

                let resources =
                    functions {
                        name "test"
                        add_allowed_ip_restriction "test-rule" ip
                    }
                    |> getResources

                let site = resources |> getResource<Web.Site> |> List.head

                let expectedRestriction = IpSecurityRestriction.Create "test-rule" ip Allow

                Expect.equal
                    site.IpSecurityRestrictions
                    [ expectedRestriction ]
                    "Should add expected ip security restriction"
            }

            test "Supports adding ip restriction for denied ip" {
                let ip = IPAddressCidr.parse "1.2.3.4/32"

                let resources =
                    functions {
                        name "test"
                        add_denied_ip_restriction "test-rule" ip
                    }
                    |> getResources

                let site = resources |> getResource<Web.Site> |> List.head

                let expectedRestriction = IpSecurityRestriction.Create "test-rule" ip Deny

                Expect.equal
                    site.IpSecurityRestrictions
                    [ expectedRestriction ]
                    "Should add denied ip security restriction"
            }

            test "Supports adding different ip restrictions to site and slot" {
                let siteIp = IPAddressCidr.parse "1.2.3.4/32"
                let slotIp = IPAddressCidr.parse "4.3.2.1/32"

                let warmupSlot =
                    appSlot {
                        name "warm-up"
                        add_allowed_ip_restriction "slot-rule" slotIp
                    }

                let resources =
                    functions {
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

            test "Can integrate unmanaged vnet" {
                let subnetId =
                    Arm.Network.subnets.resourceId (ResourceName "my-vnet", ResourceName "my-subnet")

                let asp = serverFarms.resourceId "my-asp"

                let wa =
                    functions {
                        name "testApp"
                        link_to_unmanaged_service_plan asp
                        link_to_unmanaged_vnet subnetId
                    }

                let resources = wa |> getResources
                let site = resources |> getResource<Web.Site> |> List.head
                let vnet = Expect.wantSome site.LinkToSubnet "LinkToSubnet was not set"
                Expect.equal vnet (Direct(Unmanaged subnetId)) "LinkToSubnet was incorrect"

                let vnetConnections = resources |> getResource<Web.VirtualNetworkConnection>
                Expect.hasLength vnetConnections 1 "incorrect number of Vnet connections"
            }

            test "Can integrate managed vnet" {
                let vnetConfig = vnet { name "my-vnet" }
                let asp = serverFarms.resourceId "my-asp"

                let wa =
                    functions {
                        name "testApp"
                        link_to_unmanaged_service_plan asp
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

            let data =
                [
                    (FunctionsRuntime.DotNetFramework48, "v4.0")
                    (FunctionsRuntime.DotNet60Isolated, "v6.0")
                    (FunctionsRuntime.DotNet60, "v6.0")
                    (FunctionsRuntime.DotNet70Isolated, "v7.0")
                ]

            for runtime, expectedVersion in data do
                let dotnetVersion = runtime |> fst |> string

                test $"Supports correct version {dotnetVersion}-{expectedVersion} in netFrameworkVersion field" {
                    let app =
                        functions {
                            name dotnetVersion
                            use_runtime runtime
                        }

                    let site = app |> getResources |> getResource<Web.Site> |> List.head
                    Expect.equal site.NetFrameworkVersion.Value expectedVersion "Wrong netFrameworkVersion"
                }
        ]

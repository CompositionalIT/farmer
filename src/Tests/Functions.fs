module Functions

open Expecto
open Farmer
open Farmer.Builders
open Farmer.WebApp
open Farmer.Arm
open Microsoft.Azure.Management.Storage.Models
open Microsoft.Azure.Management.WebSites
open Microsoft.Azure.Management.WebSites.Models
open Microsoft.Rest
open System

let getResource<'T when 'T :> IArmResource> (data:IArmResource list) = data |> List.choose(function :? 'T as x -> Some x | _ -> None)
/// Client instance needed to get the serializer settings.
let dummyClient = new WebSiteManagementClient (Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")
let getResourceAtIndex o = o |> getResourceAtIndex dummyClient.SerializationSettings

let tests = testList "Functions tests" [
    test "Renames storage account correctly" {
        let f = functions { name "test"; storage_account_name "foo" }
        let resources = (f :> IBuilder).BuildResources Location.WestEurope
        let site = resources.[0] :?> Web.Site
        let storage = resources.[2] :?> Storage.StorageAccount

        Expect.contains site.Dependencies (storageAccounts.resourceId "foo") "Storage account has not been added a dependency"
        Expect.equal f.StorageAccountName.ResourceName.Value "foo" "Incorrect storage account  name on site"
        Expect.equal storage.Name.ResourceName.Value "foo" "Incorrect storage account name"
    }
    test "Implicitly sets dependency on connection string" {
        let db = sqlDb { name "mySql" }
        let sql = sqlServer { name "test2"; admin_username "isaac"; add_databases [ db ] }
        let f = functions { name "test"; storage_account_name "foo"; setting "db" (sql.ConnectionString db) } :> IBuilder
        let site = f.BuildResources Location.NorthEurope |> List.head :?> Web.Site
        Expect.contains site.Dependencies (ResourceId.create (Sql.databases, ResourceName "test2", ResourceName "mySql")) "Missing dependency"
    }
    test "Works with unmanaged storage account" {
        let externalStorageAccount = ResourceId.create(storageAccounts, ResourceName "foo", "group")
        let functionsBuilder = functions { name "test"; link_to_unmanaged_storage_account externalStorageAccount }
        let f = functionsBuilder :> IBuilder
        let resources = f.BuildResources Location.WestEurope
        let site = resources |> List.head :?> Web.Site

        Expect.isFalse (resources |> List.exists (fun r -> r.ResourceId.Type = storageAccounts)) "Storage Account should not exist"
        Expect.isFalse (site.Dependencies |> Set.contains externalStorageAccount) "Should not be a dependency"
        Expect.stringContains site.AppSettings.["AzureWebJobsStorage"].Value "foo" "Web Jobs Storage setting should have storage account name"
        Expect.stringContains site.AppSettings.["AzureWebJobsDashboard"].Value "foo" "Web Jobs Dashboard setting should have storage account name"
    }
]
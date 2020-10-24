module Functions

open Expecto
open Farmer
open Farmer.Builders
open Farmer.CoreTypes
open Farmer.WebApp
open Farmer.Arm
open Microsoft.Azure.Management.WebSites
open Microsoft.Azure.Management.WebSites.Models
open Microsoft.Rest
open System

let tests = testList "Functions tests" [
    test "Renames storage account correctly" {
        let f = functions { name "test"; storage_account_name "foo" }
        Expect.equal f.StorageAccountName.ResourceName.Value "foo" "Incorrect storage name"
    }
    test "Implicitly sets dependency on connection string" {
        let db = sqlDb { name "mySql" }
        let sql = sqlServer { name "test2"; admin_username "isaac"; add_databases [ db ] }
        let f = functions { name "test"; storage_account_name "foo"; setting "db" (sql.ConnectionString db) } :> IBuilder
        let site = f.BuildResources Location.NorthEurope |> List.head :?> Web.Site
        Expect.contains site.Dependencies (ResourceId.create (Sql.databases, ResourceName "test2", ResourceName "mySql")) "Missing dependency"
    }
]
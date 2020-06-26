module Maps

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Maps
open System
open Microsoft.Azure.Management.Maps
open Microsoft.Azure.Management.Maps.Models
open Microsoft.Rest

let client = new MapsManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")
let tests = testList "Maps" [
    test "Can create a basic maps account" {
        let resource =
            let account = maps {
                name "mymaps~@"
                sku S0
            }
            arm { add_resource account }
            |> findAzureResources<MapsAccount> client.SerializationSettings
            |> List.head

        resource.Validate()
        Expect.equal resource.Name "mymaps" ""
        Expect.equal resource.Sku.Name "S0" ""
    }
]


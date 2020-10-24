module CognitiveServices

open Expecto
open Farmer
open Farmer.CoreTypes
open Farmer.Builders
open Farmer.CognitiveServices
open Microsoft.Azure.Management.CognitiveServices
open Microsoft.Rest
open System

let dummyClient = new CognitiveServicesManagementClient (Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")
let getResourceAtIndex o = o |> getResourceAtIndex dummyClient.SerializationSettings

let tests = testList "Cognitive Services" [
    test "Basic Cognitive Services test" {
        let service = cognitiveServices {
            name "test"
            api TextAnalytics
            sku S0
            add_tags [ "a", "1"; "b", "2" ]
        }
        let model : Models.CognitiveServicesAccount = service |> getResourceAtIndex 0

        Expect.equal model.Name "test" "Name is wrong"
        Expect.equal model.Kind "TextAnalytics" "Kind is wrong"
        Expect.equal model.Sku.Name "S0" "Sku is wrong"
        Expect.sequenceEqual (model.Tags |> Seq.map(fun x -> x.Key, x.Value) ) [ "a", "1"; "b", "2" ] "Tags are wrong"
    }

    test "Key is correctly calculated on a CS instance" {
        let service = cognitiveServices { name "test" }
        Expect.equal service.Key.Owner.Value.ArmExpression.Value "resourceId('Microsoft.CognitiveServices/accounts', 'test')" "Owner is wrong"
        Expect.equal service.Key.Value "listKeys(resourceId('Microsoft.CognitiveServices/accounts', 'test'), '2017-04-18').key1" "Key is wrong"
    }

    test "Key is correctly calculated with a resource group" {
        let key = CognitiveServices.getKey(ResourceId.create("test", "resource group"))
        Expect.equal key.Value "listKeys(resourceId('resource group', 'Microsoft.CognitiveServices/accounts', 'test'), '2017-04-18').key1" "Key is wrong"
    }
]
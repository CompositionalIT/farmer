module CognitiveServices

open Expecto
open Farmer
open Farmer.Builders
open Farmer.CognitiveServices
open Microsoft.Azure.Management.CognitiveServices
open Microsoft.Rest
open System

let dummyClient = new CognitiveServicesManagementClient (Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")
let getResourceAtIndex o = o |> getResourceAtIndex dummyClient.SerializationSettings

let casesForSkuLessThanPremium = [
    CognitiveServices.Kind.AllInOne
    CognitiveServices.Kind.AnomalyDetector
    CognitiveServices.Kind.Bing_Autosuggest_v7
    CognitiveServices.Kind.Bing_CustomSearch
    CognitiveServices.Kind.Bing_EntitySearch
    CognitiveServices.Kind.Bing_SpellCheck_v7
    CognitiveServices.Kind.CognitiveServices
    CognitiveServices.Kind.ComputerVision
    CognitiveServices.Kind.ContentModerator
    CognitiveServices.Kind.CustomVision_Prediction
    CognitiveServices.Kind.CustomVision_Training
    CognitiveServices.Kind.Face
    CognitiveServices.Kind.FormRecognizer
    CognitiveServices.Kind.ImmersiveReader
    CognitiveServices.Kind.InkRecognizer
    CognitiveServices.Kind.LUIS
    CognitiveServices.Kind.LUIS_Authoring
    CognitiveServices.Kind.Personalizer
    CognitiveServices.Kind.QnAMaker
    CognitiveServices.Kind.SpeakerRecognition
    CognitiveServices.Kind.SpeechServices
    CognitiveServices.Kind.TextAnalytics
    CognitiveServices.Kind.TextTranslation
]

let premiumSkus = [
    CognitiveServices.Sku.S5
    CognitiveServices.Sku.S6
    CognitiveServices.Sku.S7
    CognitiveServices.Sku.S8
]

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
        let key = CognitiveServices.getKey(ResourceId.create(Arm.CognitiveServices.accounts, ResourceName "test", "resource group"))
        Expect.equal key.Value "listKeys(resourceId('resource group', 'Microsoft.CognitiveServices/accounts', 'test'), '2017-04-18').key1" "Key is wrong"
    }

    testList "resource with not available premium sku" [
        for kind in casesForSkuLessThanPremium ->
            test (sprintf "%A" kind) {
                try
                    cognitiveServices {
                        api kind
                        sku S5
                    } |> ignore
                    Expect.isTrue false "shouldn't happen"
                with e ->
                    Expect.equal e.Message (sprintf "Cognitive services sku (S5) is not available for this kind (%A)" kind) "err message is invalid"
            }
    ]

    testList "resource with not available premium sku" [
        for premSku in premiumSkus ->
            test (sprintf "%A" premSku) {
                let service = cognitiveServices {
                    api Bing_Search_v7
                    sku premSku
                }
                let model : Models.CognitiveServicesAccount = service |> getResourceAtIndex 0

                Expect.equal model.Kind "Bing.Search.v7" "Kind is wrong"
                Expect.equal model.Sku.Name (sprintf "%A" premSku) "Sku is wrong"
            }
    ]

    test "Bing Search V7 with SKU higher than 4" {
        let service = cognitiveServices {
            api Bing_Search_v7
            sku S6
        }
        let model : Models.CognitiveServicesAccount = service |> getResourceAtIndex 0

        Expect.equal model.Kind "Bing.Search.v7" "Kind is wrong"
        Expect.equal model.Sku.Name "S6" "Sku is wrong"
    }
]
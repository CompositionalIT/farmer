[<AutoOpen>]
module Farmer.Resources.CognitiveServices

open Farmer
open Arm.CognitiveServices

[<RequireQualifiedAccess>]
/// Type of SKU. See https://github.com/Azure/azure-quickstart-templates/tree/master/101-cognitive-services-translate
type CognitiveServicesSku =
    /// Free Tier
    | F0
    | S1
    | S2
    | S3
    | S4

type CognitiveServicesApi =
    | AllInOne
    | AnomalyDetector
    | Bing_Autosuggest_v7 | Bing_CustomSearch | Bing_EntitySearch | Bing_Search_v7 | Bing_SpellCheck_v7
    | CognitiveServices
    | ComputerVision
    | ContentModerator
    | CustomVision_Prediction | CustomVision_Training
    | Face
    | FormRecognizer
    | ImmersiveReader
    | InkRecognizer
    | LUIS | LUIS_Authoring
    | Personalizer
    | QnAMaker
    | SpeakerRecognition
    | SpeechServices
    | TextAnalytics
    | TextTranslation

type CognitiveServicesConfig =
    { Name : ResourceName
      Sku : CognitiveServicesSku
      Api : CognitiveServicesApi }
    interface IResourceBuilder with
        member this.BuildResources location _ = [
            { Name = this.Name
              Location = location
              Sku = this.Sku.ToString().Replace("_", ".")
              Kind = this.Api.ToString() }
        ]

type CognitiveServicesBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Sku = CognitiveServicesSku.F0
          Api = AllInOne }
    [<CustomOperation "name">]
    member _.Name (state:CognitiveServicesConfig, name) = { state with Name = ResourceName name }
    [<CustomOperation "sku">]
    member _.Sku (state:CognitiveServicesConfig, sku) = { state with Sku = sku }
    [<CustomOperation "api">]
    member _.Api (state:CognitiveServicesConfig, api) = { state with Api = api }

let cognitiveServices = CognitiveServicesBuilder()
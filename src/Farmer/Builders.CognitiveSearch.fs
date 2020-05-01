[<AutoOpen>]
module Farmer.Resources.CognitiveSearch

open Farmer
open Farmer.Models

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

module Converters =
    let cognitiveServices location config =
        { Name = config.Name
          Location = location
          Sku = config.Sku.ToString().Replace("_", ".")
          Kind = config.Api.ToString() }

    module Outputters =
        let cognitiveServices (service:Farmer.Models.CognitiveServices) =
            {| name = service.Name.Value
               ``type`` = "Microsoft.CognitiveServices/accounts"
               apiVersion = "2017-04-18"
               sku = {| name = service.Sku |}
               kind = service.Kind
               location = service.Location.ArmValue
               tags = {||}
               properties = {||} |}

let cognitiveServices = CognitiveServicesBuilder()

type Farmer.ArmBuilder.ArmBuilder with
    member this.AddResource(state:ArmConfig, config) =
        { state with
            Resources = CognitiveService (Converters.cognitiveServices state.Location config) :: state.Resources
        }
    member this.AddResources (state, configs) = addResources<CognitiveServicesConfig> this.AddResource state configs

[<AutoOpen>]
module TestHelpers

open Farmer
open Microsoft.Rest.Serialization

let createSimpleDeployment parameters = {
    Location = Location.NorthEurope
    PostDeployTasks = []
    Template = {
        Outputs = []
        Parameters = parameters |> List.map SecureParameter
        Resources = []
    }
    RequiredResourceGroups = []
    Tags = Map.empty
}

let convertTo<'T> = Serialization.toJson >> Serialization.ofJson<'T>

let farmerToMs<'T when 'T: null> (serializationSettings: Newtonsoft.Json.JsonSerializerSettings) data =
    data
    |> Serialization.toJson
    |> fun json -> SafeJsonConvert.DeserializeObject<'T>(json, serializationSettings)

let getResourceAtIndex serializationSettings index (builder: #IBuilder) =
    builder.BuildResources Location.WestEurope
    |> fun r -> r.[index].JsonModel |> farmerToMs serializationSettings

let findAzureResources<'T when 'T: null>
    (serializationSettings: Newtonsoft.Json.JsonSerializerSettings)
    (deployment: IDeploymentSource)
    =
    let template =
        deployment.Deployment.Template |> Writer.TemplateGeneration.processTemplate

    template.resources
    |> Seq.map Serialization.toJson
    |> Seq.choose (fun json ->
        SafeJsonConvert.DeserializeObject<'T>(json, serializationSettings)
        |> Option.ofObj)
    |> Seq.toList

type TypedArmTemplate<'ResT> = { Resources: 'ResT array }

let getFirstResourceOrFail (template: TypedArmTemplate<'ResourceType>) =
    if Array.length template.Resources < 1 then
        raiseFarmer "Template had no resources"

    template.Resources.[0]

let toTemplate loc (d: IBuilder) =
    let a = arm {
        location loc
        add_resource d
    }

    a.Template

let toTypedTemplate<'ResourceType> loc =
    toTemplate loc
    >> Writer.toJson
    >> Serialization.ofJson<TypedArmTemplate<'ResourceType>>
    >> getFirstResourceOrFail
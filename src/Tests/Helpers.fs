[<AutoOpen>]
module TestHelpers

open Farmer
open Microsoft.Rest.Serialization

let findAzureResources<'T when 'T : null> (serializationSettings:Newtonsoft.Json.JsonSerializerSettings) (deployment:Deployment) =
    let template = deployment.Template |> Writer.TemplateGeneration.processTemplate

    template.resources
    |> Seq.map SafeJsonConvert.SerializeObject
    |> Seq.choose (fun json -> SafeJsonConvert.DeserializeObject<'T>(json, serializationSettings) |> Option.ofObj)
    |> Seq.toList

let convertResourceBuilder mapper (serializationSettings:Newtonsoft.Json.JsonSerializerSettings) (resourceBuilder:IBuilder) =
    resourceBuilder.BuildResources NorthEurope []
    |> List.pick(fun r ->
            r.JsonValue
            |> SafeJsonConvert.SerializeObject
            |> SafeJsonConvert.DeserializeObject
            |> mapper
            |> SafeJsonConvert.SerializeObject
            |> fun json -> SafeJsonConvert.DeserializeObject(json, serializationSettings)
            |> Option.ofObj
    )
[<AutoOpen>]
module TestHelpers

open Farmer
open Farmer.Models
open Microsoft.Rest.Serialization

let findAzureResources<'T when 'T : null> (serializationSettings:Newtonsoft.Json.JsonSerializerSettings) (deployment:Deployment) =
    let template = deployment.Template |> Writer.TemplateGeneration.processTemplate

    template.resources
    |> Seq.map SafeJsonConvert.SerializeObject
    |> Seq.choose (fun json -> SafeJsonConvert.DeserializeObject<'T>(json, serializationSettings) |> Option.ofObj)
    |> Seq.toList

let convertSingleConfig converter outputter mapper (serializationSettings:Newtonsoft.Json.JsonSerializerSettings) config =
    config
    |> converter NorthEurope []
    |> function
    | NewResource r ->
        r
        |> outputter
        |> mapper
        |> SafeJsonConvert.SerializeObject
        |> fun json -> SafeJsonConvert.DeserializeObject(json, serializationSettings)
    | _ ->
        failwith "not possible"
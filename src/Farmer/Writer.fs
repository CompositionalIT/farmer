module Farmer.Writer

open Farmer.CoreTypes
open Newtonsoft.Json
open System.IO

module TemplateGeneration =
    let processTemplate (template:ArmTemplate) = {|
        ``$schema`` = "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"
        contentVersion = "1.0.0.0"
        resources = template.Resources |> List.map(fun r -> r.JsonModel)
        parameters =
            template.Parameters
            |> List.map(fun (SecureParameter p) -> p, {| ``type`` = "securestring" |})
            |> Map.ofList
        outputs =
            template.Outputs
            |> List.map(fun (k, v) ->
                k, {| ``type`` = "string"
                      value = v |})
            |> Map.ofList
    |}

    let serialize data =
        JsonConvert.SerializeObject(data, Formatting.Indented, JsonSerializerSettings(NullValueHandling = NullValueHandling.Ignore))

/// Returns a JSON string representing the supplied ARMTemplate.
let toJson = TemplateGeneration.processTemplate >> TemplateGeneration.serialize

/// Writes the provided JSON to a file based on the supplied template name. The postfix ".json" will automatically be added to the filename.
let toFile folder templateName json =
    let filename =
        let filename = sprintf "%s.json" templateName
        Path.Combine(folder, filename)
    let directory = Path.GetDirectoryName filename
    if not (Directory.Exists directory) then
        Directory.CreateDirectory directory |> ignore
    File.WriteAllText(filename, json)
    filename

/// Converts the supplied ARMTemplate to JSON and then writes it out to the provided template name. The postfix ".json" will automatically be added to the filename.
let quickWrite templateName deployment =
    deployment.Template
    |> toJson
    |> toFile "." templateName
    |> ignore
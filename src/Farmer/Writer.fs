module Farmer.Writer

open System.IO
open System
open System.Reflection
open Farmer

module TemplateGeneration =
    let processTemplate (template: ArmTemplate) = {|
        ``$schema`` = "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"
        contentVersion = "1.0.0.0"
        resources = template.Resources |> List.map (fun r -> r.JsonModel)
        parameters =
            template.Parameters
            |> List.map (fun (SecureParameter p) -> p, {| ``type`` = "securestring" |})
            |> Map.ofList
        outputs =
            template.Outputs
            |> List.map (fun (k, v) -> k, {| ``type`` = "string"; value = v |})
            |> Map.ofList
    |}

let branding () =
    let version =
        Assembly
            .GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            .InformationalVersion

    printfn "=================================================="
    printfn "Farmer %s" version
    printfn "Repeatable deployments in Azure made easy!"
    printfn "=================================================="

/// Returns a JSON string representing the supplied ARMTemplate.
let toJson = TemplateGeneration.processTemplate >> Serialization.toJson

/// Writes the provided JSON to a file based on the supplied template name. The postfix ".json" will automatically be added to the filename.
let toFile folder templateName json =
    let filename =
        let filename = $"{templateName}.json"
        Path.Combine(folder, filename)

    let directory = Path.GetDirectoryName filename

    if not (Directory.Exists directory) then
        Directory.CreateDirectory directory |> ignore

    File.WriteAllText(filename, json)
    filename

/// Converts the supplied ARMTemplate to JSON and then writes it out to the provided template name. The postfix ".json" will automatically be added to the filename.
let quickWrite templateName (deployment: IDeploymentSource) =
    deployment.Deployment.Template |> toJson |> toFile "." templateName |> ignore
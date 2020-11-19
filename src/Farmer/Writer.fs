namespace Farmer

module Writer =
    open Newtonsoft.Json
    open System.IO
    open System
    open System.Reflection

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

    let internal toJsonImpl = TemplateGeneration.processTemplate >> TemplateGeneration.serialize
    let internal quickWriteImpl templateName deployment =
        deployment.Template
        |> toJsonImpl
        |> toFile "." templateName
        |> ignore

    /// Returns a JSON string representing the supplied ARMTemplate.
    let toJson = toJsonImpl

    /// Converts the supplied ARMTemplate to JSON and then writes it out to the provided template name. The postfix ".json" will automatically be added to the filename.
    let quickWrite templateName deployment = quickWriteImpl templateName deployment

[<AutoOpen>]
module TemplateWriterExtensions =
    type Deployment with
        /// Returns a JSON string representing the supplied ARMTemplate.
        member this.ToJson () = this.Template |> Writer.toJsonImpl

        /// Converts the supplied ARMTemplate to JSON and then writes it out to the provided template name. The postfix ".json" will automatically be added to the filename.
        member this.ToFile templateName = this |> Writer.quickWriteImpl templateName

    type ArmTemplate with
        member this.ToJson () = this |> Writer.toJsonImpl

    type IBuilder with
        member this.ToJson location =
            [ for resource in this.BuildResources location do resource.JsonModel ]
            |> Writer.TemplateGeneration.serialize

    type IArmResource with
        member this.ToJson =
            Writer.TemplateGeneration.serialize this.JsonModel

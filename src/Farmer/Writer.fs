module Farmer.Writer

open Farmer.Models
open Farmer.Resources
open Newtonsoft.Json
open System.IO

module TemplateGeneration =
    let processTemplate (template:ArmTemplate) = {|
        ``$schema`` = "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"
        contentVersion = "1.0.0.0"
        resources =
            template.Resources
            |> List.map(function
                | StorageAccount s -> Converters.Outputters.storageAccount s |> box

                | AppInsights ai -> Converters.Outputters.appInsights ai |> box
                | ServerFarm s -> Converters.Outputters.serverFarm s |> box
                | WebApp wa -> Converters.Outputters.webApp wa |> box

                | CosmosAccount cds -> Converters.Outputters.cosmosDbAccount cds |> box
                | CosmosSqlDb db -> Converters.Outputters.cosmosDbSql db |> box
                | CosmosContainer c -> Converters.Outputters.cosmosDbContainer c |> box

                | SqlServer sql -> Converters.Outputters.sqlAzure sql |> box

                | ContainerGroup g -> Converters.Outputters.containerGroup g |> box
                | Ip address -> Converters.Outputters.publicIpAddress address |> box
                | Vnet vnet -> Converters.Outputters.virtualNetwork vnet |> box
                | Nic nic -> Converters.Outputters.networkInterface nic |> box
                | Vm vm -> Converters.Outputters.virtualMachine vm |> box

                | AzureSearch search -> Converters.Outputters.search search |> box

                | KeyVault vault -> Converters.Outputters.keyVault vault |> box
                | KeyVaultSecret secret -> Converters.Outputters.keyVaultSecret secret |> box

                | RedisCache redis -> Converters.Outputters.redisCache redis |> box

                | EventHub hub -> Converters.Outputters.eventHub hub |> box
                | EventHubNamespace ns -> Converters.Outputters.eventHubNs ns |> box
                | ConsumerGroup group -> Converters.Outputters.consumerGroup group |> box
                | EventHubAuthRule rule -> Converters.Outputters.authRule rule |> box

                | CognitiveService service -> Converters.Outputters.cognitiveServices service |> box
                | ContainerRegistry registry -> Converters.Outputters.containerRegistry registry |> box
                | ExpressRoute circuit -> Converters.Outputters.expressRoute circuit |> box
            )
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
    let filename = sprintf "%s.json" templateName
    let filename = Path.Combine(folder, filename)
    File.WriteAllText(filename, json)
    filename

/// Converts the supplied ARMTemplate to JSON and then writes it out to the provided template name. The postfix ".json" will automatically be added to the filename.
let quickWrite templateName deployment =
    deployment.Template
    |> toJson
    |> toFile "." templateName
    |> ignore
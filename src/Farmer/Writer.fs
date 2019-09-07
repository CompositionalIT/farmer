module Farmer.Writer

open Farmer.Internal

module Outputters =
    // module Dependencies =
    //     let toString = function
    //         | ServerFarmDependency, (v:Value) -> sprintf "[resourceId('Microsoft.Web/serverfarms', %s)]" v.QuotedValue
    //         | StorageDependency, v -> sprintf "[resourceId('Microsoft.Storage/storageAccounts/', %s)]" v.QuotedValue
    //         | AppInsightsDependency, v -> sprintf "[resourceId('Microsoft.Insights/components/', %s)]" v.QuotedValue

    let appInsights (resource:AppInsights) = {|
        ``type`` = ResourceType.AppInsights
        kind = "web"
        name = resource.Name.Command
        location = resource.Location.Command
        apiVersion = "2014-04-01"
        tags =
            [ sprintf "[concat('hidden-link:', resourceGroup().id, '/providers/Microsoft.Web/sites/', %s)]" resource.LinkedWebsite.QuotedValue, "Resource"
              "displayName", "AppInsightsComponent" ] |> Map.ofList
        properties =
            {| name = resource.Name.Command |}
    |}

    let storageAccount (resource:StorageAccount) = {|
        ``type`` = ResourceType.StorageAccount
        sku = {| name = resource.Sku.Command |}
        kind = "storage"
        name = resource.Name.Command
        apiVersion = "2017-10-01"
        location = resource.Location.Command
    |}

    let serverFarm (resource:ServerFarm) = {|
        ``type`` = ResourceType.ServerFarm
        sku = {| name = resource.Sku.Command |}
        name = resource.Name.Command
        apiVersion = "2016-09-01"
        location = resource.Location.Command
        properties =
            {| name = resource.Name.Command
               perSiteScaling = false
               reserved = false |}
    |}

    let webApp (serverFarmInfo:ServerFarm) (webApp:WebApp) = {|
        ``type`` = ResourceType.WebSite
        name = webApp.Name.Command
        apiVersion = "2016-08-01"
        location = serverFarmInfo.Location.Command
        dependsOn = webApp.Dependencies |> List.map ResourceType.makePath
        resources =
            webApp.Extensions
            |> Set.toList
            |> List.map (function
            | AppInsightsExtension ->
                {| apiVersion = "2016-08-01"
                   name = "Microsoft.ApplicationInsights.AzureWebSites"
                   ``type`` = "siteextensions"
                   dependsOn = [ ResourceType.makePath (ResourceType.WebSite, webApp.Name) ]
                   properties = {||}
                |})
        properties =
            {| serverFarmId = ResourceType.makePath (ResourceType.ServerFarm, serverFarmInfo.Name)
               siteConfig =
                {| appSettings =
                      webApp.AppSettings
                      |> List.map(fun (k,v) -> {| name = k; value = v.Command |})
                |}
            |}
    |}

let processTemplate (template:ArmTemplate) = {|
    ``$schema`` = "https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#"
    contentVersion = "1.0.0.0"
    parameters =
        template.Parameters
        |> List.map(fun p -> p, {| ``type`` = "string" |})
        |> Map.ofList
    variables = template.Variables |> Map.ofList
    resources = [
        template.Resources
        |> List.collect (function
            | :? AppInsights as ai ->
                [ Outputters.appInsights ai |> box ]
            | :? StorageAccount as s ->
                [ Outputters.storageAccount s |> box ]
            | :? ServerFarm as s ->
                let sf = Outputters.serverFarm s |> box
                let apps =
                    s.WebApps
                    |> List.map (Outputters.webApp s >> box)
                sf :: apps
            | _ ->
                failwith "Not supported. Sorry!"
        )]
        |> List.concat
    outputs = [
        for (k, v) in template.Outputs ->
            k, Map [ "type", "string"; "value", v.Command ]
    ] |> Map
|}

let toJson = processTemplate >> Newtonsoft.Json.JsonConvert.SerializeObject
let toFile f c = System.IO.File.WriteAllText(f, c)
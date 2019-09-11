module Farmer.Writer

open Farmer.Internal

module Outputters =
    let appInsights (resource:AppInsights) =
        let (ResourceName linkedWebsite) = resource.LinkedWebsite
        {| ``type`` = ResourceType.AppInsights.Value
           kind = "web"
           name = resource.Name.Value
           location = resource.Location
           apiVersion = "2014-04-01"
           tags =
               [ sprintf "[concat('hidden-link:', resourceGroup().id, '/providers/Microsoft.Web/sites/', '%s')]" linkedWebsite, "Resource"
                 "displayName", "AppInsightsComponent" ] |> Map.ofList
           properties =
               {| name = resource.Name.Value |} |}

    let storageAccount (resource:StorageAccount) = {|
        ``type`` = ResourceType.StorageAccount.Value
        sku = {| name = resource.Sku |}
        kind = "Storage"
        name = resource.Name.Value
        apiVersion = "2017-10-01"
        location = resource.Location
    |}

    let serverFarm (resource:ServerFarm) = {|
        ``type`` = ResourceType.ServerFarm.Value
        sku = {| name = resource.Sku |}
        name = resource.Name.Value
        apiVersion = "2016-09-01"
        location = resource.Location
        properties =
            {| name = resource.Name.Value
               perSiteScaling = false
               reserved = false |}
    |}

    let webApp (serverFarmInfo:ServerFarm) (webApp:WebApp) = {|
        ``type`` = ResourceType.WebSite.Value
        name = webApp.Name.Value
        apiVersion = "2016-08-01"
        location = serverFarmInfo.Location
        dependsOn = webApp.Dependencies |> List.map(fun p -> p.Value)
        resources =
            webApp.Extensions
            |> Set.toList
            |> List.map (function
            | AppInsightsExtension ->
                {| apiVersion = "2016-08-01"
                   name = "Microsoft.ApplicationInsights.AzureWebSites"
                   ``type`` = "siteextensions"
                   dependsOn = [ webApp.Name.Value ]
                   properties = {||}
                |})
        properties =
            {| serverFarmId = serverFarmInfo.Name.Value
               siteConfig =
                {| appSettings =
                    webApp.AppSettings
                    |> List.map(fun (k,v) -> {| name = k; value = v |})
                |}
            |}
    |}

let processTemplate (template:ArmTemplate) = {|
    ``$schema`` = "https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#"
    contentVersion = "1.0.0.0"
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
            k, Map [ "type", "string"; "value", v ]
    ] |> Map
|}

let toJson = processTemplate >> Newtonsoft.Json.JsonConvert.SerializeObject
let toFile f c = System.IO.File.WriteAllText(f, c)
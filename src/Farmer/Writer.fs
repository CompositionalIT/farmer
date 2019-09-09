module Farmer.Writer

open Farmer.Internal

module Outputters =
    let appInsights (resource:AppInsights) =
        let (ResourceName linkedWebsite) = resource.LinkedWebsite
        {| ``type`` = ResourcePath.AppInsights.Value
           kind = "web"
           name = resource.Name.Command
           location = resource.Location.Command
           apiVersion = "2014-04-01"
           tags =
               [ sprintf "[concat('hidden-link:', resourceGroup().id, '/providers/Microsoft.Web/sites/', %s)]" linkedWebsite.QuotedValue, "Resource"
                 "displayName", "AppInsightsComponent" ] |> Map.ofList
           properties =
               {| name = resource.Name.Command |} |}

    let storageAccount (resource:StorageAccount) = {|
        ``type`` = ResourcePath.StorageAccount.Value
        sku = {| name = resource.Sku.Command |}
        kind = "Storage"
        name = resource.Name.Command
        apiVersion = "2017-10-01"
        location = resource.Location.Command
    |}

    let serverFarm (resource:ServerFarm) = {|
        ``type`` = ResourcePath.ServerFarm.Value
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
        ``type`` = ResourcePath.WebSite.Value
        name = webApp.Name.Command
        apiVersion = "2016-08-01"
        location = serverFarmInfo.Location.Command
        dependsOn = webApp.Dependencies |> List.map(fun p -> p.ResourceIdPath |> toExpr)
        resources =
            webApp.Extensions
            |> Set.toList
            |> List.map (function
            | AppInsightsExtension ->
                {| apiVersion = "2016-08-01"
                   name = "Microsoft.ApplicationInsights.AzureWebSites"
                   ``type`` = "siteextensions"
                   dependsOn = [ (ResourcePath.makeWebSite webApp.Name).ResourceIdPath |> toExpr ]
                   properties = {||}
                |})
        properties =
            {| serverFarmId = (ResourcePath.makeServerFarm serverFarmInfo.Name).ResourceIdPath |> toExpr
               siteConfig =
                {| appSettings =
                      webApp.AppSettings
                      |> List.map(fun (k,v) -> {| name = k; value = v.Command |})
                |}
            |}
    |}

    let cosmosDbServer (cosmosDb:CosmosDbServer) = {|
        ``type`` = ResourcePath.CosmosDb.Value
        name = cosmosDb.Name.Command
        apiVersion = "2016-03-31"
        location = cosmosDb.Location.Command
        kind = "GlobalDocumentDB"
        properties =
            let baseProps =
                {| consistencyPolicy =
                      match cosmosDb.ConsistencyPolicy with
                      | BoundedStaleness(maxStaleness, maxInterval) ->
                          box {| defaultConsistencyLevel = "BoundedStaleness"
                                 maxStalenessPrefix = maxStaleness
                                 maxIntervalInSeconds = maxInterval |}
                      | Session
                      | Eventual
                      | ConsistentPrefix
                      | Strong ->
                          box {| defaultConsistencyLevel = string cosmosDb.ConsistencyPolicy |}
                   databaseAccountOfferType = "Standard" |}
            match cosmosDb.WriteModel with
            | AutoFailover secondary ->
                {| baseProps with
                      enableAutomaticFailover = true
                      locations = [
                        {| locationName = cosmosDb.Location.Command; failoverPriority = 0 |}
                        {| locationName = secondary; failoverPriority = 1 |}
                      ]
                |} |> box
            | MultiMaster secondary ->
                {| baseProps with
                      autoenableMultipleWriteLocations = true
                      locations = [
                        {| locationName = cosmosDb.Location.Command; failoverPriority = 0 |}
                        {| locationName = secondary; failoverPriority = 1 |}
                      ]
                |} |> box
            | Standard ->
                {| baseProps with
                    locations = [ 
                        {| locationName = cosmosDb.Location.Command; failoverPriority = 0 |}
                    ]
                |} |> box
    |}

    let cosmosDbSql (cosmosDbSql:CosmosDbSql) = {|
        ``type`` = ResourcePath.CosmosDbSql.Value
        name = cosmosDbSql.Name.Command
        apiVersion = "2016-03-31"
        dependsOn = cosmosDbSql.Dependencies |> List.map(fun p -> p.ResourceIdPath |> toExpr)
        properties =
            {| resource = {| id = cosmosDbSql.Name.Command |}
               options = {| throughput = cosmosDbSql.Throughput.Command |} |}
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
            | :? CosmosDbServer as cds ->
                let server = Outputters.cosmosDbServer cds |> box
                let dbs =
                    cds.Databases
                    |> List.map (Outputters.cosmosDbSql >> box)
                server :: dbs
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
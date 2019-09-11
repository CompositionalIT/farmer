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

    let cosmosDbContainer (accountName:ResourceName) (databaseName:ResourceName) (container:CosmosDbContainer) = {|
        ``type`` = ResourceType.CosmosDbSqlContainer.Value
        name = sprintf "%s/sql/%s/%s" accountName.Value databaseName.Value container.Name.Value
        apiVersion = "2016-03-31"
        dependsOn = [
            databaseName.Value
        ]
//TODO:        "dependsOn": [ "[resourceId('Microsoft.DocumentDB/databaseAccounts/apis/databases', variables('accountName'), 'sql', parameters('databaseName'))]" ],
        properties =
            {| resource =
                {| id = container.Name.Value
                   partitionKey =
                    {| paths = container.PartitionKey.Paths
                       kind = string container.PartitionKey.Kind |}
                   indexingPolicy =
                    {| indexingMode = "consistent"
                       includedPaths =
                           container.IndexingPolicy.IncludedPaths
                           |> List.map(fun p ->
                            {| path = p.Path
                               indexes =
                                p.Indexes
                                |> List.map(fun i ->
                                    {| kind = string i.Kind
                                       dataType = (string i.DataType).ToLower()
                                       precision = -1 |})
                            |})
                       excludedPaths =
                        container.IndexingPolicy.ExcludedPaths
                        |> List.map(fun p -> {| path = p |})
                    |}
                |}
            |}
        |}
// {
//             "type": "Microsoft.DocumentDb/databaseAccounts/apis/databases/containers",
//TODO:        "name": "[concat(variables('accountName'), '/sql/', parameters('databaseName'), '/', parameters('container1Name'))]",
//             "apiVersion": "2016-03-31",
//TODO:        "dependsOn": [ "[resourceId('Microsoft.DocumentDB/databaseAccounts/apis/databases', variables('accountName'), 'sql', parameters('databaseName'))]" ],
//             "properties":
//             {
//                 "resource":{
//                     "id":  "[parameters('container1Name')]",
//                     "partitionKey": {
//                         "paths": [
//                         "/MyPartitionKey1"
//                         ],
//                         "kind": "Hash"
//                     },
//                     "indexingPolicy": {
//                         "indexingMode": "consistent",
//                         "includedPaths": [{
//                                 "path": "/*",
//                                 "indexes": [
//                                     {
//                                         "kind": "Range",
//                                         "dataType": "number",
//                                         "precision": -1
//                                     },
//                                     {
//                                         "kind": "Range",
//                                         "dataType": "string",
//                                         "precision": -1
//                                     }
//                                 ]
//                             }
//                         ],
//                         "excludedPaths": [{
//                                 "path": "/MyPathToNotIndex/*"
//                             }
//                         ]
//                     }
//                 }
//             }
//         },
    let cosmosDbServer (cosmosDb:CosmosDbServer) = {|
        ``type`` = ResourceType.CosmosDb.Value
        name = cosmosDb.Name.Value
        apiVersion = "2016-03-31"
        location = cosmosDb.Location
        kind = "GlobalDocumentDB"
        properties =
            let baseProps =
                let consistencyPolicy =
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
                {| consistencyPolicy = consistencyPolicy
                   databaseAccountOfferType = "Standard" |}
            match cosmosDb.WriteModel with
            | AutoFailover secondary ->
                {| baseProps with
                      enableAutomaticFailover = true
                      locations = [
                        {| locationName = cosmosDb.Location; failoverPriority = 0 |}
                        {| locationName = secondary; failoverPriority = 1 |}
                      ]
                |} |> box
            | MultiMaster secondary ->
                {| baseProps with
                      autoenableMultipleWriteLocations = true
                      locations = [
                        {| locationName = cosmosDb.Location; failoverPriority = 0 |}
                        {| locationName = secondary; failoverPriority = 1 |}
                      ]
                |} |> box
            | NoFailover ->
                box baseProps
    |}

    let cosmosDbSql (cosmosServerName:ResourceName) (cosmosDbSql:CosmosDbSql) = {|
        ``type`` = ResourceType.CosmosDbSql.Value
        name = sprintf "%s/sql/%s" cosmosServerName.Value cosmosDbSql.Name.Value
        apiVersion = "2016-03-31"
        dependsOn = cosmosDbSql.Dependencies |> List.map(fun p -> p.Value)
        properties =
            {| resource = {| id = cosmosDbSql.Name.Value |}
               options = {| throughput = cosmosDbSql.Throughput |} |}
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
            | :? CosmosDbServer as cds ->
                let server = Outputters.cosmosDbServer cds |> box
                let dbsAndContainers =
                    cds.Databases
                    |> List.collect (fun db ->
                        let db = Outputters.cosmosDbSql cds.Name db
                        let containers =
                            cds.Databases
                            |> List.collect(fun db ->
                                db.Containers
                                |> List.map (Outputters.cosmosDbContainer cds.Name db.Name >> box))
                        (box db) :: containers
                    )
                [ yield server
                  yield! dbsAndContainers ]
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
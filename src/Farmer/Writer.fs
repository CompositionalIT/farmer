module Farmer.Writer

open Farmer.Internal

module Outputters =
    let storageAccount (resource:StorageAccount) = {|
        ``type`` = ResourceType.StorageAccount.Value
        sku = {| name = resource.Sku |}
        kind = "StorageV2"
        name = resource.Name.Value
        apiVersion = "2018-07-01"
        location = resource.Location
    |}

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
               {| name = resource.Name.Value
                  Application_Type = "web"
                  ApplicationId = linkedWebsite |}
        |}
    let serverFarm (farm:ServerFarm) = {|
        ``type`` = ResourceType.ServerFarm.Value
        sku =
            let baseProps =
                {| name = farm.Sku
                   tier = farm.Tier
                   size = farm.WorkerSize |}
            if farm.IsDynamic then box {| baseProps with family = "Y"; capacity = 0 |}
            else box {| baseProps with numberOfWorkers = farm.WorkerCount |}
        name = farm.Name.Value
        apiVersion = "2016-09-01"
        location = farm.Location
        properties =
            if farm.IsDynamic then
                box {| name = farm.Name.Value; computeMode = "Dynamic" |}
            else
                box {| name = farm.Name.Value
                       perSiteScaling = false
                       reserved = false |}
    |}
    let webApp (farm:ServerFarm) (webApp:WebApp) =
        let baseProps = {|
            ``type`` = ResourceType.WebSite.Value
            name = webApp.Name.Value
            apiVersion = "2016-08-01"
            location = farm.Location
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
                {| serverFarmId = farm.Name.Value
                   siteConfig =
                    {| appSettings =
                        webApp.AppSettings
                        |> List.map(fun (k,v) -> {| name = k; value = v |})
                    |}
                |}
        |}
        match webApp.Kind with
        | Some kind -> box {| baseProps with kind = kind |}
        | None -> box baseProps

    let cosmosDbContainer (accountName:ResourceName) (databaseName:ResourceName) (container:CosmosDbContainer) = {|
        ``type`` = ResourceType.CosmosDbSqlContainer.Value
        name = sprintf "%s/sql/%s/%s" accountName.Value databaseName.Value container.Name.Value
        apiVersion = "2016-03-31"
        dependsOn = [
            databaseName.Value
        ]
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

    let sqlAzure (database:SqlAzure) = {|
        ``type`` = ResourceType.SqlAzure.Value
        name = database.ServerName.Value
        apiVersion = "2014-04-01-preview"
        location = database.Location
        tags = {| displayName = "SqlServer" |}
        properties =
            let (SecureParameter passwordParam) = database.AdministratorLoginPassword
            {| administratorLogin = database.AdministratorLogin
               administratorLoginPassword = sprintf "[parameters('%s')]" passwordParam
               version = "12.0" |}
        resources = [
            yield
                {| ``type`` = "databases"
                   name = database.DbName.Value               
                   apiVersion = "2015-01-01"
                   location = database.Location
                   tags = {| displayName = "Database" |}
                   properties =
                    {| edition = database.DbEdition
                       collation = database.DbCollation
                       requestedServiceObjectiveName = database.DbObjective |}
                   dependsOn = [
                       database.ServerName.Value
                   ]
                   resources = [
                       {| ``type`` = "transparentDataEncryption"
                          comments = "Transparent Data Encryption"
                          name = "current"                      
                          apiVersion = "2014-04-01-preview"
                          properties = {| status = string database.TransparentDataEncryption |}
                          dependsOn = [ database.DbName.Value ]
                       |}
                   ]
                |} |> box
            yield!
                database.FirewallRules
                |> List.map(fun fw ->
                    {| ``type`` = "firewallrules"
                       name = fw.Name
                       apiVersion = "2014-04-01"
                       location = database.Location
                       properties =
                        {| endIpAddress = string fw.Start
                           startIpAddress = string fw.End |}
                       dependsOn = [ database.ServerName.Value ]
                    |} |> box)
        ]

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
            | :? SqlAzure as sql ->
                [ yield Outputters.sqlAzure sql |> box ]
            | s ->
                failwithf "'%s' is not supported. Sorry!" (s.GetType().FullName)
        )]
        |> List.concat
    parameters =
        [ for resource in template.Resources do
            match resource with
            | :? SqlAzure as sql ->
                let (SecureParameter p) = sql.AdministratorLoginPassword
                yield p, {| ``type`` = "securestring" |}
            | _ ->
                ()            
        ] |> Map.ofList
    outputs = [
        for (k, v) in template.Outputs ->
            k, Map [ "type", "string"; "value", v ]
    ] |> Map
|}

let toJson = processTemplate >> Newtonsoft.Json.JsonConvert.SerializeObject
let toFile f c = System.IO.File.WriteAllText(f, c)
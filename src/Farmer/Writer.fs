module Farmer.Writer

open Farmer.Models
open Farmer.Resources
open Newtonsoft.Json
open System
open System.IO

module Outputters =
    let private containerAccess (a:StorageContainerAccess) =
        match a with
        | Private -> "None"
        | Container -> "Container"
        | Blob -> "Blob"

    let private storageAccountContainer (parent:StorageAccount) (name, access) = {|
        ``type`` = "blobServices/containers"
        apiVersion = "2018-03-01-preview"
        name = "default/" + name
        dependsOn = [ parent.Name.Value ]
        properties = {| publicAccess = containerAccess access |}
    |}

    let storageAccount (resource:StorageAccount) = {|
        ``type`` = "Microsoft.Storage/storageAccounts"
        sku = {| name = resource.Sku |}
        kind = "StorageV2"
        name = resource.Name.Value
        apiVersion = "2018-07-01"
        location = resource.Location.Value
        resources = resource.Containers |> List.map (storageAccountContainer resource)
    |}

    let containerGroup (resource:ContainerGroups.ContainerGroup) = {|
        ``type`` = "Microsoft.ContainerInstance/containerGroups"
        apiVersion = "2018-10-01"
        name = resource.Name.Value
        location = resource.Location.Value
        properties =
            {| containers =
                resource.ContainerInstances
                |> List.map (fun container ->
                    {| name = container.Name.Value.ToLowerInvariant ()
                       properties =
                        {| image = container.Image
                           ports = container.Ports |> List.map (fun port -> {| port = port |})
                           resources =
                            {| requests =
                                {| cpu = container.Resources.Cpu
                                   memoryInGb = container.Resources.Memory |}
                            |}
                        |}
                    |})
               osType =
                   match resource.OsType with
                   | ContainerGroups.ContainerGroupOsType.Windows -> "Windows"
                   | ContainerGroups.ContainerGroupOsType.Linux -> "Linux"
               restartPolicy =
                   match resource.RestartPolicy with
                   | ContainerGroups.ContainerGroupRestartPolicy.Always -> "always"
                   | ContainerGroups.ContainerGroupRestartPolicy.Never -> "never"
                   | ContainerGroups.ContainerGroupRestartPolicy.OnFailure -> "onfailure"
               ipAddress =
                {| ``type`` =
                    match resource.IpAddress.Type with
                    | ContainerGroups.ContainerGroupIpAddressType.PublicAddress -> "Public"
                    | ContainerGroups.ContainerGroupIpAddressType.PrivateAddress -> "Private"
                   ports = resource.IpAddress.Ports
                   |> List.map (fun port ->
                    {| protocol = port.Protocol.ToString()
                       port = port.Port |})
                |}
            |}
    |}
    let appInsights (resource:AppInsights) = {|
        ``type`` = "Microsoft.Insights/components"
        kind = "web"
        name = resource.Name.Value
        location = resource.Location.Value
        apiVersion = "2014-04-01"
        tags =
            [ match resource.LinkedWebsite with
              | Some linkedWebsite -> sprintf "[concat('hidden-link:', resourceGroup().id, '/providers/Microsoft.Web/sites/', '%s')]" linkedWebsite.Value, "Resource"
              | None -> ()
              "displayName", "AppInsightsComponent" ]
            |> Map.ofList
        properties =
         match resource.LinkedWebsite with
         | Some linkedWebsite ->
            box {| name = resource.Name.Value
                   Application_Type = "web"
                   ApplicationId = linkedWebsite.Value |}
         | None ->
            box {| name = resource.Name.Value
                   Application_Type = "web" |}
    |}
    let serverFarm (farm:ServerFarm) =
        {|  ``type`` = "Microsoft.Web/serverfarms"
            sku =
                {| name = farm.Sku
                   tier = farm.Tier
                   size = farm.WorkerSize
                   family = if farm.IsDynamic then "Y" else null
                   capacity = if farm.IsDynamic then 0 else farm.WorkerCount |}
            name = farm.Name.Value
            apiVersion = "2018-02-01"
            location = farm.Location.Value
            properties =
                if farm.IsDynamic then
                    box {| name = farm.Name.Value
                           computeMode = "Dynamic"
                           reserved = farm.IsLinux |}
                else
                    box {| name = farm.Name.Value
                           perSiteScaling = false
                           reserved = farm.IsLinux |}
            kind = farm.Kind |> Option.toObj
        |}
    let webApp (webApp:WebApp) = {|
        ``type`` = "Microsoft.Web/sites"
        name = webApp.Name.Value
        apiVersion = "2016-08-01"
        location = webApp.Location.Value
        dependsOn = webApp.Dependencies |> List.map(fun p -> p.Value)
        kind = webApp.Kind
        properties =
            {| serverFarmId = webApp.ServerFarm.Value
               siteConfig =
                    [ "alwaysOn", box webApp.AlwaysOn
                      "appSettings", webApp.AppSettings |> List.map(fun (k,v) -> {| name = k; value = v |}) |> box
                      match webApp.LinuxFxVersion with Some v -> "linuxFxVersion", box v | None -> ()
                      match webApp.NetFrameworkVersion with Some v -> "netFrameworkVersion", box v | None -> ()
                      match webApp.JavaVersion with Some v -> "javaVersion", box v | None -> ()
                      match webApp.JavaContainer with Some v -> "javaContainer", box v | None -> ()
                      match webApp.JavaContainerVersion with Some v -> "javaContainerVersion", box v | None -> ()
                      match webApp.PhpVersion with Some v -> "phpVersion", box v | None -> ()
                      match webApp.PythonVersion with Some v -> "pythonVersion", box v | None -> ()
                      "metadata", webApp.Metadata |> List.map(fun (k,v) -> {| name = k; value = v |}) |> box ]
                    |> Map.ofList
             |}
    |}
    let cosmosDbContainer (container:CosmosDbContainer) = {|
        ``type`` = "Microsoft.DocumentDb/databaseAccounts/apis/databases/containers"
        name = sprintf "%s/sql/%s/%s" container.Account.Value container.Database.Value container.Name.Value
        apiVersion = "2016-03-31"
        dependsOn = [ container.Database.Value ]
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
    let cosmosDbServer (cosmosDb:CosmosDbAccount) = {|
        ``type`` = "Microsoft.DocumentDB/databaseAccounts"
        name = cosmosDb.Name.Value
        apiVersion = "2016-03-31"
        location = cosmosDb.Location.Value
        kind = "GlobalDocumentDB"
        properties =
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
               databaseAccountOfferType = "Standard"
               enableAutomaticFailure = match cosmosDb.WriteModel with AutoFailover _ -> Nullable true | _ -> Nullable()
               autoenableMultipleWriteLocations = match cosmosDb.WriteModel with MultiMaster _ -> Nullable true | _ -> Nullable()
               locations =
                match cosmosDb.WriteModel with
                | AutoFailover secondary
                | MultiMaster secondary ->
                    [ {| locationName = cosmosDb.Location.Value; failoverPriority = 0 |}
                      {| locationName = secondary.Value; failoverPriority = 1 |} ]
                | NoFailover -> []
                |} |> box
    |}
    let cosmosDbSql (cosmosDbSql:CosmosDbSql) = {|
        ``type`` = "Microsoft.DocumentDB/databaseAccounts/apis/databases"
        name = sprintf "%s/sql/%s" cosmosDbSql.Account.Value cosmosDbSql.Name.Value
        apiVersion = "2016-03-31"
        dependsOn = [ cosmosDbSql.Account.Value ]
        properties =
            {| resource = {| id = cosmosDbSql.Name.Value |}
               options = {| throughput = cosmosDbSql.Throughput |} |}
    |}
    let sqlAzure (database:SqlAzure) = {|
        ``type`` = "Microsoft.Sql/servers"
        name = database.ServerName.Value
        apiVersion = "2014-04-01-preview"
        location = database.Location.Value
        tags = {| displayName = "SqlServer" |}
        properties =
            {| administratorLogin = database.Credentials.Username
               administratorLoginPassword = database.Credentials.Password.AsArmRef.Eval()
               version = "12.0" |}
        resources = [
            box
                {| ``type`` = "databases"
                   name = database.DbName.Value
                   apiVersion = "2015-01-01"
                   location = database.Location.Value
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
                |}
            for rule in database.FirewallRules do
                box
                    {| ``type`` = "firewallrules"
                       name = rule.Name
                       apiVersion = "2014-04-01"
                       location = database.Location.Value
                       properties =
                        {| endIpAddress = string rule.Start
                           startIpAddress = string rule.End |}
                       dependsOn = [ database.ServerName.Value ]
                    |}
        ]
    |}
    let publicIpAddress (ipAddress:VM.PublicIpAddress) = {|
        ``type`` = "Microsoft.Network/publicIPAddresses"
        apiVersion = "2018-11-01"
        name = ipAddress.Name.Value
        location = ipAddress.Location.Value
        properties =
           match ipAddress.DomainNameLabel with
           | Some label ->
               box
                   {| publicIPAllocationMethod = "Dynamic"
                      dnsSettings = {| domainNameLabel = label.ToLower() |}
                   |}
           | None ->
               box {| publicIPAllocationMethod = "Dynamic" |}
    |}
    let virtualNetwork (vnet:VM.VirtualNetwork) = {|
        ``type`` = "Microsoft.Network/virtualNetworks"
        apiVersion = "2018-11-01"
        name = vnet.Name.Value
        location = vnet.Location.Value
        properties =
             {| addressSpace = {| addressPrefixes = vnet.AddressSpacePrefixes |}
                subnets =
                 vnet.Subnets
                 |> List.map(fun subnet ->
                    {| name = subnet.Name.Value
                       properties = {| addressPrefix = subnet.Prefix |}
                    |})
             |}
    |}
    let networkInterface (nic:VM.NetworkInterface) = {|
        ``type`` = "Microsoft.Network/networkInterfaces"
        apiVersion = "2018-11-01"
        name = nic.Name.Value
        location = nic.Location.Value
        dependsOn = [
            nic.VirtualNetwork.Value
            for config in nic.IpConfigs do
                config.PublicIpName.Value
        ]
        properties =
            {| ipConfigurations =
                 nic.IpConfigs
                 |> List.mapi(fun index ipConfig ->
                     {| name = sprintf "ipconfig%i" (index + 1)
                        properties =
                         {| privateIPAllocationMethod = "Dynamic"
                            publicIPAddress = {| id = sprintf "[resourceId('Microsoft.Network/publicIPAddresses','%s')]" ipConfig.PublicIpName.Value |}
                            subnet = {| id = sprintf "[resourceId('Microsoft.Network/virtualNetworks/subnets', '%s', '%s')]" nic.VirtualNetwork.Value ipConfig.SubnetName.Value |}
                         |}
                     |})
            |}
    |}
    let virtualMachine (vm:VM.VirtualMachine) = {|
        ``type`` = "Microsoft.Compute/virtualMachines"
        apiVersion = "2018-10-01"
        name = vm.Name.Value
        location = vm.Location.Value
        dependsOn = [
            vm.NetworkInterfaceName.Value
            match vm.StorageAccount with
            | Some s -> s.Value
            | None -> ()
        ]
        properties =
         {| hardwareProfile = {| vmSize = vm.Size |}
            osProfile =
             {|
                computerName = vm.Name.Value
                adminUsername = vm.Credentials.Username
                adminPassword = vm.Credentials.Password.AsArmRef.Eval()
             |}
            storageProfile =
                let vmNameLowerCase = vm.Name.Value.ToLower()
                {| imageReference =
                    {| publisher = vm.Image.Publisher
                       offer = vm.Image.Offer
                       sku = vm.Image.Sku
                       version = "latest" |}
                   osDisk =
                    {| createOption = "FromImage"
                       name = sprintf "%s-osdisk" vmNameLowerCase
                       diskSizeGB = vm.OsDisk.Size
                       managedDisk = {| storageAccountType = string vm.OsDisk.DiskType |}
                    |}
                   dataDisks =
                    vm.DataDisks
                    |> List.mapi(fun lun dataDisk ->
                        {| createOption = "Empty"
                           name = sprintf "%s-datadisk-%i" vmNameLowerCase lun
                           diskSizeGB = dataDisk.Size
                           lun = lun
                           managedDisk = {| storageAccountType = string dataDisk.DiskType |} |})
                |}
            networkProfile =
                {| networkInterfaces = [
                    {| id = sprintf "[resourceId('Microsoft.Network/networkInterfaces','%s')]" vm.NetworkInterfaceName.Value |}
                   ]
                |}
            diagnosticsProfile =
                match vm.StorageAccount with
                | Some storageAccount ->
                    box
                        {| bootDiagnostics =
                            {| enabled = true
                               storageUri = sprintf "[reference('%s').primaryEndpoints.blob]" storageAccount.Value
                            |}
                        |}
                | None ->
                    box {| bootDiagnostics = {| enabled = false |} |}
        |}
    |}
    let search (search:Search) = {|
        ``type`` = "Microsoft.Search/searchServices"
        apiVersion = "2015-08-19"
        name = search.Name.Value
        location = search.Location.Value
        sku =
         {| name = search.Sku |}
        properties =
         {| replicaCount = search.ReplicaCount
            partitionCount = search.PartitionCount
            hostingMode = search.HostingMode |}
    |}

    let keyVault (keyVault:KeyVault) = {|
      ``type``= "Microsoft.KeyVault/vaults"
      name = keyVault.Name.Value
      apiVersion = "2018-02-14"
      location = keyVault.Location.Value
      properties =
        {| tenantId = keyVault.TenantId
           sku = {| name = keyVault.Sku; family = "A" |}
           enabledForDeployment = keyVault.EnabledForDeployment |> Option.toNullable
           enabledForDiskEncryption = keyVault.EnabledForDiskEncryption |> Option.toNullable
           enabledForTemplateDeployment = keyVault.EnabledForTemplateDeployment |> Option.toNullable
           enablePurgeProtection = keyVault.EnablePurgeProtection |> Option.toNullable
           createMode = keyVault.CreateMode |> Option.toObj
           vaultUri = keyVault.Uri |> Option.toObj
           accessPolicies =
                [| for policy in keyVault.AccessPolicies do
                    {| objectId = policy.ObjectId
                       tenantId = keyVault.TenantId
                       applicationId = policy.ApplicationId |> Option.toObj
                       permissions =
                        {| keys = policy.Permissions.Keys
                           storage = policy.Permissions.Storage
                           certificates = policy.Permissions.Certificates
                           secrets = policy.Permissions.Secrets |}
                    |}
                |]
           networkAcls =
            {| defaultAction = keyVault.DefaultAction |> Option.toObj
               bypass = keyVault.Bypass |> Option.toObj
               ipRules = keyVault.IpRules
               virtualNetworkRules = keyVault.VnetRules |}
        |}
    |}
    let keyVaultSecret (keyVaultSecret:KeyVaultSecret) = {|
        ``type`` = "Microsoft.KeyVault/vaults/secrets"
        name = keyVaultSecret.Name.Value
        apiVersion = "2018-02-14"
        location = keyVaultSecret.Location.Value
        dependsOn = [
            keyVaultSecret.ParentKeyVault.Value
            for dependency in keyVaultSecret.Dependencies do
                dependency.Value ]
        properties =
            {| value = keyVaultSecret.Value.Value
               contentType = keyVaultSecret.ContentType |> Option.toObj
               attributes =
                {| enabled = keyVaultSecret.Enabled
                   nbf = keyVaultSecret.ActivationDate
                   exp = keyVaultSecret.ExpirationDate
                |}
            |}
        |}
    let redisCache (redis:Redis) = {|
        ``type`` = "Microsoft.Cache/Redis"
        apiVersion = "2018-03-01"
        name = redis.Name.Value
        location = redis.Location.Value
        properties =
            {| sku =
                {| name = redis.Sku.Name
                   family = redis.Sku.Family
                   capacity = redis.Sku.Capacity
                |}
               enableNonSslPort = redis.NonSslEnabled |> Option.toNullable
               shardCount = redis.ShardCount |> Option.toNullable
               minimumTlsVersion = redis.MinimumTlsVersion |> Option.toObj
               redisConfiguration = redis.RedisConfiguration
            |}
    |}

    let eventHubNs (ns:EventHubNamespace) = {|
        ``type`` = "Microsoft.EventHub/namespaces"
        apiVersion = "2017-04-01"
        name = ns.Name.Value
        location = ns.Location.Value
        sku =
            {| name = ns.Sku.Name
               tier = ns.Sku.Tier
               capacity = ns.Sku.Capacity |}
        properties =
            {| zoneRedundant = ns.ZoneRedundant |> Option.toNullable
               isAutoInflateEnabled = ns.IsAutoInflateEnabled |> Option.toNullable
               maximumThroughputUnits = ns.MaxThroughputUnits |> Option.toNullable
               kafkaEnabled = ns.KafkaEnabled |> Option.toNullable |}
    |}

    let eventHub (hub:EventHub) = {|
        ``type`` = "Microsoft.EventHub/namespaces/eventhubs"
        apiVersion = "2017-04-01"
        name = hub.Name.Value
        location = hub.Location.Value
        dependsOn = hub.Dependencies |> List.map(fun d -> d.Value)
        properties =
            {| messageRetentionInDays = hub.MessageRetentionDays |> Option.toNullable
               partitionCount = hub.Partitions
               status = "Active" |}
    |}

    let consumerGroup (group:EventHubConsumerGroup) = {|
        ``type`` = "Microsoft.EventHub/namespaces/eventhubs/consumergroups"
        apiVersion = "2017-04-01"
        name = group.Name.Value
        location = group.Location.Value
        dependsOn = group.Dependencies |> List.map(fun d -> d.Value)
    |}

    let authRule (rule:EventHubAuthorizationRule) = {|
        ``type`` = "Microsoft.EventHub/namespaces/eventhubs/AuthorizationRules"
        apiVersion = "2017-04-01"
        name = rule.Name.Value
        location = rule.Location.Value
        dependsOn = rule.Dependencies |> List.map(fun d -> d.Value)
        properties = {| rights = rule.Rights |}
    |}

module TemplateGeneration =
    let processTemplate (template:ArmTemplate) = {|
        ``$schema`` = "https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#"
        contentVersion = "1.0.0.0"
        resources =
            template.Resources
            |> List.map(function
                | StorageAccount s -> Outputters.storageAccount s |> box

                | AppInsights ai -> Outputters.appInsights ai |> box
                | ServerFarm s -> Outputters.serverFarm s |> box
                | WebApp wa -> Outputters.webApp wa |> box

                | CosmosAccount cds -> Outputters.cosmosDbServer cds |> box
                | CosmosSqlDb db -> Outputters.cosmosDbSql db |> box
                | CosmosContainer c -> Outputters.cosmosDbContainer c |> box

                | SqlServer sql -> Outputters.sqlAzure sql |> box

                | ContainerGroup g -> Outputters.containerGroup g |> box
                | Ip address -> Outputters.publicIpAddress address |> box
                | Vnet vnet -> Outputters.virtualNetwork vnet |> box
                | Nic nic -> Outputters.networkInterface nic |> box
                | Vm vm -> Outputters.virtualMachine vm |> box

                | AzureSearch search -> Outputters.search search |> box

                | KeyVault vault -> Outputters.keyVault vault |> box
                | KeyVaultSecret secret -> Outputters.keyVaultSecret secret |> box

                | RedisCache redis -> Outputters.redisCache redis |> box

                | EventHub hub -> Outputters.eventHub hub |> box
                | EventHubNamespace ns -> Outputters.eventHubNs ns |> box
                | ConsumerGroup group -> Outputters.consumerGroup group |> box
                | EventHubAuthRule rule -> Outputters.authRule rule |> box
            )
        parameters =
            template.Parameters
            |> List.map(fun (SecureParameter p) -> p, {| ``type`` = "securestring" |})
            |> Map.ofList
        outputs =
            template.Outputs
            |> List.map(fun (k, v) ->
                k, Map [ "type", "string"
                         "value", v ])
            |> Map.ofList
    |}

    let serialize data =
        JsonConvert.SerializeObject(data, Formatting.Indented, JsonSerializerSettings(NullValueHandling = NullValueHandling.Ignore))

/// Returns a JSON string representing the supplied ARMTemplate.
let toJson = TemplateGeneration.processTemplate >> TemplateGeneration.serialize

/// Writes the provided JSON to a file based on the supplied template name. The postfix ".json" will automatically be added to the filename.
let toFile templateName json =
    let filename = sprintf "%s.json" templateName
    File.WriteAllText(filename, json)
    filename

/// Converts the supplied ARMTemplate to JSON and then writes it out to the provided template name. The postfix ".json" will automatically be added to the filename.
let quickWrite templateName deployment =
    deployment.Template
    |> toJson
    |> toFile templateName
    |> ignore
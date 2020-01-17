module Farmer.Writer

open Farmer.Resources
open Newtonsoft.Json
open System
open System.IO

module Outputters =
    open Farmer.Models

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
        {| ``type`` = "Microsoft.Web/serverfarms"
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
                          computeMode = "Dynamic" |}
               else
                   box {| name = farm.Name.Value
                          perSiteScaling = false
                          reserved = false |}
           kind = farm.Kind |> Option.toObj
        |}
    let webApp (webApp:WebApp) = {|
        ``type`` = "Microsoft.Web/sites"
        name = webApp.Name.Value
        apiVersion = "2016-08-01"
        location = webApp.Location.Value
        dependsOn = webApp.Dependencies |> List.map(fun p -> p.Value)
        kind = webApp.Kind
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
               administratorLoginPassword = database.Credentials.Password.AsArmRef
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
                adminPassword = vm.Credentials.Password.AsArmRef
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

    let cdnCustomDomain (parent:CdnEndpoint) (customDomain:CdnCustomDomain) =
        {| name = customDomain.Name.Value
           ``type`` = "Microsoft.Cdn/profiles/endpoints/customDomains"
           apiVersion = "2019-04-15"
           dependsOn = [| parent.Name.Value |]
           properties = {| hostName = customDomain.HostName |}
        |}
        
    let cdnEndpoint (parent:CdnProfile) (endpoint:CdnEndpoint) =
        {| name = endpoint.Name.Value
           ``type`` = "Microsoft.Cdn/profiles/endpoints"
           apiVersion = "2019-04-15"
           dependsOn = [| parent.Name.Value |]
           location = "Global"
           properties =
               {| originHostHeader = endpoint.OriginHostHeader |> Option.toObj
                  originPath = endpoint.OriginPath |> Option.toObj
                  contentTypesToCompress = endpoint.ContentTypesToCompress
                  isCompressionEnabled = endpoint.IsCompressionEnabled |> Option.toNullable
                  isHttpAllowed = endpoint.IsHttpAllowed |> Option.toNullable
                  isHttpsAllowed = endpoint.IsHttpsAllowed |> Option.toNullable
                  queryStringCachingBehaviour = endpoint.QueryStringCachingBehavior |> Option.map string |> Option.toObj
                  optimizationPath = endpoint.OptimizationPath |> Option.toObj
                  probePath = endpoint.ProbePath |> Option.toObj
                  geoFilters = [|
                    for filter in endpoint.GeoFilters ->
                        {| relativePath = filter.RelativePath
                           action = filter.Action |> string
                           countryCodes = filter.CountryCodes |> Array.map string |}
                  |]
                  deliveryPolicy =
                    endpoint.DeliveryPolicy
                    |> Option.map(fun policy ->
                        {| description = policy.Description |> Option.toObj
                           rules = [|
                               for rule in policy.Rules ->
                                   {| name = rule.Name |> Option.toObj
                                      order = rule.Order
                                      conditions = rule.Conditions |> Array.map(fun c -> {| name = c |})
                                      actions = rule.Actions |> Array.map(fun c -> {| name = c |})
                                   |}
                           |]
                        |} |> box)
                    |> Option.toObj
                  origins = [|
                      for origin in endpoint.Origins ->
                          {| name = origin.Name
                             properties =
                                 {| hostName = origin.HostName
                                    httpPort = origin.HttpPort |> Option.toNullable
                                    httpsPort = origin.HttpsPort |> Option.toNullable |}
                          |}
                  |]
               |}
           resources =
               endpoint.CustomDomains
               |> Array.map (cdnCustomDomain endpoint)
        |}

    let cdnProfile (profile:CdnProfile) =
        {| name = profile.Name.Value
           ``type`` = "Microsoft.Cdn/profiles"
           apiVersion = "2019-04-15"
           location = "Global"
           sku = {| name = string profile.Sku |}
           resources = [| cdnEndpoint profile profile.Endpoint |]
        |}

open Farmer.Models
let processTemplate (template:ArmTemplate) = {|
    ``$schema`` = "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"
    contentVersion = "1.0.0.0"
    resources =
        template.Resources
        |> List.map(function
            | AppInsights ai -> Outputters.appInsights ai |> box
            | StorageAccount s -> Outputters.storageAccount s |> box
            | ContainerGroup g -> Outputters.containerGroup g |> box
            | ServerFarm s -> Outputters.serverFarm s |> box
            | WebApp wa -> Outputters.webApp wa |> box
            | CosmosAccount cds -> Outputters.cosmosDbServer cds |> box
            | CosmosSqlDb db -> Outputters.cosmosDbSql db |> box
            | CosmosContainer c -> Outputters.cosmosDbContainer c |> box
            | SqlServer sql -> Outputters.sqlAzure sql |> box
            | Ip address -> Outputters.publicIpAddress address |> box
            | Vnet vnet -> Outputters.virtualNetwork vnet |> box
            | Nic nic -> Outputters.networkInterface nic |> box
            | Vm vm -> Outputters.virtualMachine vm |> box
            | AzureSearch search -> Outputters.search search |> box
            | KeyVault vault -> Outputters.keyVault vault |> box
            | KeyVaultSecret secret -> Outputters.keyVaultSecret secret |> box
            | CdnProfile profile -> Outputters.cdnProfile profile |> box
        )
    parameters =
        template.Resources
        |> List.choose(function
            | SqlServer sql -> Some sql.Credentials.Password
            | Vm vm -> Some vm.Credentials.Password
            | KeyVaultSecret { Value = ParameterSecret secureParameter } -> Some secureParameter
            | _ -> None)
        |> List.map(fun (SecureParameter p) -> p, {| ``type`` = "securestring" |})
        |> Map.ofList
    outputs =
        template.Outputs
        |> List.map(fun (k, v) ->
            k, Map [ "type", "string"
                     "value", v ])
        |> Map.ofList
|}

let toJson x =
    let x = processTemplate x
    JsonConvert.SerializeObject(x, Formatting.Indented, JsonSerializerSettings(NullValueHandling = NullValueHandling.Ignore))

let toFile armTemplateName json =
    let templateFilename = sprintf "%s.json" armTemplateName
    File.WriteAllText(templateFilename, json)
    templateFilename

let toBatchFile armTemplateName resourceGroupName (Location location) templateFilename =
    let batchFilename = sprintf "%s.bat" armTemplateName

    let azureCliBatch =
        sprintf """az login && az group create -l %s -n %s && az group deployment create -g %s --template-file %s"""
            location
            resourceGroupName
            resourceGroupName
            templateFilename

    File.WriteAllText(batchFilename, azureCliBatch)
    batchFilename

let generateDeployScript resourceGroupName (deployment:Deployment) =
    let templateName = "farmer-deploy"

    deployment.Template
    |> toJson
    |> toFile templateName
    |> toBatchFile templateName resourceGroupName deployment.Location

let quickDeploy resourceGroupName deployment =
    generateDeployScript resourceGroupName deployment
    |> Diagnostics.Process.Start
    |> ignore
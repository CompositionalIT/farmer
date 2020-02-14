module Farmer.Writer

open Farmer.Models
open Farmer.Resources
open Newtonsoft.Json
open System
open System.IO
open System.Runtime.InteropServices

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

module TemplateGeneration =
    let processTemplate (template:ArmTemplate) = {|
        ``$schema`` = "https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#"
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

module ParameterFile =
    module Passwords =
        open System
        let lowerCaseLetters = String [|'a'..'z'|]
        let upperCaseLetters = String [|'A'..'Z'|]
        let digits = String [|'0' .. '9'|]
        let special = "!�$%^&*()_-+="
        let allCharacters = lowerCaseLetters + upperCaseLetters + digits + special

        let isValid (s:string) =
            let isInString (src:string) = s |> Seq.exists (string >> src.Contains)
            isInString lowerCaseLetters && isInString upperCaseLetters && isInString digits && isInString special
            
        let generatePassword randomNumber length =
            Seq.init length (fun _ -> allCharacters.[randomNumber allCharacters.Length])
            |> Seq.toArray
            |> String

        /// Creates a password that is known to conform to lower, upper and numeric constraints.
        let generateConformingPassword length template =
            let rnd = Random (template.GetHashCode())

            Seq.initInfinite (fun _ -> generatePassword rnd.Next length)
            |> Seq.take 100
            |> Seq.filter isValid
            |> Seq.tryHead
            |> function
            | None -> failwith "Unable to generate a valid password that meet the requested requirements!"
            | Some password -> password

    let toParameters parameters =
        {| ``$schema`` = "https://schema.management.azure.com/schemas/2015-01-01/deploymentParameters.json#"
           contentVersion = "1.0.0.0"
           parameters =
                parameters
                |> List.map(fun (name, value) -> name, {| value = value |})
                |> Map.ofList
        |}     

       
    let generateParametersFile (armTemplate:ArmTemplate) =
        armTemplate.Parameters
        |> List.map(fun (SecureParameter p) -> p, Passwords.generateConformingPassword 24 armTemplate)
        |> toParameters
        |> TemplateGeneration.serialize
        |> toFile "farmer-deploy-parameters"

module AzureCli =
    let setLinuxExecutePermissions filename =
        let command = sprintf "chmod +x %s" filename
        let startInfo = 
            System.Diagnostics.ProcessStartInfo( 
                FileName = "/bin/bash",
                Arguments = "-c \""+ command + "\"",
                UseShellExecute = false,
                RedirectStandardOutput = true )

        use proc = new System.Diagnostics.Process(StartInfo = startInfo)
        proc.Start() |> ignore
        proc.WaitForExit() |> ignore
        filename

    let toAzureCliCmd resourceGroupName (Location location) templateFilename parametersFilename =
        sprintf """az login && az group create -l %s -n %s && az group deployment create -g %s --template-file %s --parameters @%s"""
            location
            resourceGroupName
            resourceGroupName
            templateFilename
            parametersFilename

    let (|OperatingSystem|_|) platform () =
        if RuntimeInformation.IsOSPlatform platform then Some() else None

    let toScriptFile armTemplateName azureCliCmd =
        match () with
        | OperatingSystem OSPlatform.Windows ->
            let scriptFilename = sprintf "%s.bat" armTemplateName
            File.WriteAllText(scriptFilename, azureCliCmd)
            scriptFilename
        | OperatingSystem OSPlatform.OSX
        | OperatingSystem OSPlatform.Linux ->
            let bashHeader = "#!/bin/bash\n"
            let scriptFilename = sprintf "%s.sh" armTemplateName
            File.WriteAllText(scriptFilename, bashHeader + azureCliCmd)
            setLinuxExecutePermissions scriptFilename
        | _ ->
            RuntimeInformation.OSDescription 
            |> sprintf "OSPlatform: %s not supported" 
            |> System.NotImplementedException 
            |> raise

    let generateDeployScript resourceGroupName (deployment:Deployment) =
        let templateName = "farmer-deploy"
        let templateFilename = deployment.Template |> toJson |> toFile templateName
        let parameterFilename = deployment.Template |> ParameterFile.generateParametersFile

        toAzureCliCmd resourceGroupName deployment.Location templateFilename parameterFilename
        |> toScriptFile templateName

/// Converts the supplied ARMTemplate to JSON and then writes it out to the provided template name. The postfix ".json" will automatically be added to the filename.
let quickWrite templateName deployment =
    deployment.Template
    |> toJson
    |> toFile templateName
    |> ignore

/// Executes the supplied Deployment against a resource group using a locally-installed Azure CLI.
let quickDeploy resourceGroupName deployment =
    AzureCli.generateDeployScript resourceGroupName deployment
    |> System.Diagnostics.Process.Start
    |> ignore

/// Represents an Azure service principal which has permissions to
/// deploy ARM templates on the supplied Subscription ID.
type AzureCredentials =
    { ClientId : Guid
      ClientSecret : Guid
      TenantId : Guid }

module AzureRest =
    open FsHttp.DslCE
    
    type ErrorDetails =
        { Code : string
          Message : string
          Details : {| Code : string
                       Message : string |} array }

    type DeploymentStatus =
        | Provisioning of string
        | Provisioned of Map<string, string>
        | ProvisioningFailed of ErrorDetails

    let toResult (response:FsHttp.Domain.Response) =
        match int response.statusCode with
        | code when code >= 200 && code < 300 -> Ok response
        | _ -> Error response
    let getContent<'T> (response:FsHttp.Domain.Response) =
        response.content.ReadAsStringAsync().Result
        |> JsonConvert.DeserializeObject<'T>
    let getBearerToken tenantId clientId clientSecret =
        http {
            POST (sprintf "https://login.microsoftonline.com/%s/oauth2/token" tenantId)
            body
            formUrlEncoded
                [ "grant_type", "client_credentials"
                  "client_id", clientId
                  "client_secret", clientSecret
                  "resource", "https://management.azure.com" ]
        }
        |> toResult
        |> Result.map getContent<{| access_token:string |}>
        |> Result.mapError getContent<{| Error:string; Error_description:string |}>
    let createResourceGroup accessToken subscriptionId resourceGroup location =
        http {
            PUT (sprintf "https://management.azure.com/subscriptions/%s/resourcegroups/%s?api-version=2019-05-01" subscriptionId resourceGroup)
            BearerAuth accessToken
            body
            json (sprintf """{ "location": "%s", "tags": { "Deployed with Farmer": "" }}""" location)
        } |> toResult
    let deployTemplate accessToken subscriptionId resourceGroup deployment templateJson =
        http {
            PUT (sprintf "https://management.azure.com/subscriptions/%s/resourcegroups/%s/providers/Microsoft.Resources/deployments/%s?api-version=2019-05-01" subscriptionId resourceGroup deployment)
            BearerAuth accessToken
            body
            json (sprintf """{ "properties": { "mode": "Incremental", "template": %s } }""" templateJson)
        } |> toResult

    open Result

    let getDeploymentStatus accessToken subscriptionId resourceGroup deployment = result {
        let! deploymentDetails =
            http {
                GET (sprintf "https://management.azure.com/subscriptions/%s/resourcegroups/%s/providers/Microsoft.Resources/deployments/%s?api-version=2018-05-01" subscriptionId resourceGroup deployment)
                BearerAuth accessToken
            }
            |> toResult
            |> Result.mapError(fun _ -> "Cannot get deployment details.")

        let content =
            deploymentDetails
            |> getContent<
                {| Properties :
                    {| ProvisioningState : string
                       Outputs : Map<string, {| value : string |}>
                       Error : obj |}
                |}>

        return
            match content.Properties.Error, content.Properties.ProvisioningState with
            | null, ("Accepted" | "Running") ->
                content.Properties.ProvisioningState
                |> Provisioning
            | null, _ ->
                content.Properties.Outputs
                |> Map.map(fun _ v -> v.value)
                |> Provisioned
            | error ->
                error
                |> string
                |> JsonConvert.DeserializeObject<ErrorDetails>
                |> ProvisioningFailed
    }
type DeploymentResult =
    | DeploymentRejected of string
    | DeploymentFailed of string
    | DeploymentSucceeded of Map<string,string>

type DeploymentOutput =
    { DeploymentName : string
      Result : DeploymentResult }

module RestDeployment =
    let getDeployNumber =
        let r = Random()
        fun () -> r.Next 10000

    /// Represents the "raw" result of a deployment, which is result of result. The "top" level result
    /// is the initial stage of deployment. If this succeeds, a sequence of results are provided back
    /// representing the ongoing polling of the deployment.
    type RawDeploymentResult = {| DeploymentName : string; Result : Result<Result<AzureRest.DeploymentStatus, string> seq, string> |}

    /// Deploys a template using the Rest API.
    open Result
    let deployTemplate (credentials:AzureCredentials) subscriptionId (armTemplateJson:string, location:string, resourceGroup:string) : RawDeploymentResult =
        let deploymentName = sprintf "FarmerDeploy%d" (getDeployNumber())
        let deploymentResult = result {
            let! bearerToken =
                AzureRest.getBearerToken (string credentials.TenantId) (string credentials.ClientId) (string credentials.ClientSecret)
                |> Result.mapError(fun error -> sprintf "Unable to obtain bearer token! %s - %s" error.Error error.Error_description)
                |> Result.map(fun response -> response.access_token)

            do!
                AzureRest.createResourceGroup bearerToken subscriptionId resourceGroup location
                |> Result.mapError(fun _ -> "Unable to create resource group")
                |> Result.ignore

            do!
                armTemplateJson
                |> AzureRest.deployTemplate bearerToken subscriptionId resourceGroup deploymentName
                |> Result.mapError(fun e -> sprintf "Azure rejected the deployment request: %s" (e.content.ReadAsStringAsync().Result))
                |> Result.ignore

            return
                Seq.initInfinite (fun _ ->
                    Async.Sleep 5000 |> Async.RunSynchronously
                    AzureRest.getDeploymentStatus bearerToken subscriptionId resourceGroup deploymentName)
                |> Seq.distinct
        }

        {| DeploymentName = deploymentName
           Result = deploymentResult |}

    /// Gets the final deployment result once a deployment has started.
    let getDeploymentResult statuses =
        statuses
        |> Seq.choose(function
            | Ok (AzureRest.Provisioning _) -> None
            | Ok (AzureRest.Provisioned outputs) -> Some (DeploymentSucceeded outputs)
            | Ok (AzureRest.ProvisioningFailed error) -> Some (DeploymentFailed (string error))
            | Error err -> Some (DeploymentFailed err))
        |> Seq.tryHead
        |> function
            | None -> DeploymentFailed "Could not get any deployment status."
            | Some res -> res

    /// Monitors an ARM template with optional progress reports.
    let reportDeploymentProgress onStatus (deployment: RawDeploymentResult) : DeploymentOutput =
        let output =
            match deployment.Result with
            | Error err ->
                DeploymentRejected err
            | Ok statuses ->
                statuses
                |> Seq.map (fun a -> onStatus a; a)
                |> getDeploymentResult

        { DeploymentName = deployment.DeploymentName
          Result = output }

/// Executes the supplied Deployment against a resource group using a the Azure REST API.
/// It requires a service principle containing a client id, secret and tenant ID. Use this API for unattended installs e.g. continuous deployment etc. 
let fullDeploy credentials (subscriptionId:Guid) resourceGroupName deployment =
    let armTemplateJson = deployment.Template |> toJson

    (armTemplateJson, deployment.Location.Value, resourceGroupName)
    |> RestDeployment.deployTemplate credentials (string subscriptionId)
    |> RestDeployment.reportDeploymentProgress (printfn "%A")
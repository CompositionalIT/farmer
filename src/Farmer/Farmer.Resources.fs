// Contains the list of all ARM Azure resources supported by Farmer.
module Farmer.Resources.Arm

open Farmer
open System

module ContainerInstance =
    type ContainerGroup =
        { Name : ResourceName
          Location : Location
          ContainerInstances :
            {| Name : ResourceName
               Image : string
               Ports : uint16 list
               Cpu : int
               Memory : float |} list
          OsType : string
          RestartPolicy : string
          IpAddress : {| Type : string; Ports : {| Protocol : string; Port : uint16 |} list |} }

        interface IResource with
            member this.ResourceName = this.Name
            member this.ToArmObject() =
                {| ``type`` = "Microsoft.ContainerInstance/containerGroups"
                   apiVersion = "2018-10-01"
                   name = this.Name.Value
                   location = this.Location.ArmValue
                   properties =
                       {| containers =
                           this.ContainerInstances
                           |> List.map (fun container ->
                               {| name = container.Name.Value.ToLowerInvariant ()
                                  properties =
                                   {| image = container.Image
                                      ports = container.Ports |> List.map (fun port -> {| port = port |})
                                      resources =
                                       {| requests =
                                           {| cpu = container.Cpu
                                              memoryInGb = container.Memory |}
                                       |}
                                   |}
                               |})
                          osType = this.OsType
                          restartPolicy = this.RestartPolicy
                          ipAddress = this.IpAddress
                       |}
                |} :> _

module CognitiveServices =
    type Accounts =
        { Name : ResourceName
          Location : Location
          Sku : string
          Kind : string }
        interface IResource with
            member this.ResourceName = this.Name
            member this.ToArmObject() =
                {| name = this.Name.Value
                   ``type`` = "Microsoft.CognitiveServices/accounts"
                   apiVersion = "2017-04-18"
                   sku = {| name = this.Sku |}
                   kind = this.Kind
                   location = this.Location.ArmValue
                   tags = {||}
                   properties = {||} |} :> _

module Web =
    type ServerFarm =
        { Name : ResourceName
          Location : Location
          Sku: string
          WorkerSize : string
          IsDynamic : bool
          Kind : string option
          Tier : string
          WorkerCount : int
          IsLinux : bool }
        interface IResource with
            member this.ResourceName = this.Name
            member this.ToArmObject() =
                {| ``type`` = "Microsoft.Web/serverfarms"
                   sku =
                       {| name = this.Sku
                          tier = this.Tier
                          size = this.WorkerSize
                          family = if this.IsDynamic then "Y" else null
                          capacity = if this.IsDynamic then 0 else this.WorkerCount |}
                   name = this.Name.Value
                   apiVersion = "2018-02-01"
                   location = this.Location.ArmValue
                   properties =
                       if this.IsDynamic then
                           box {| name = this.Name.Value
                                  computeMode = "Dynamic"
                                  reserved = this.IsLinux |}
                       else
                           box {| name = this.Name.Value
                                  perSiteScaling = false
                                  reserved = this.IsLinux |}
                   kind = this.Kind |> Option.toObj
                |} :> _

    module ZipDeploy =
        open System.IO
        open System.IO.Compression

        type ZipDeployKind =
            | DeployFolder of string
            | DeployZip of string
            member this.Value = match this with DeployFolder s | DeployZip s -> s
            /// Tries to create a ZipDeployKind from a string path.
            static member TryParse path =
                if (File.GetAttributes path).HasFlag FileAttributes.Directory then
                    Some(DeployFolder path)
                else if Path.GetExtension path = ".zip" then
                    Some(DeployZip path)
                else
                    None
            /// Processes a ZipDeployKind and returns the filename of the zip file.
            /// If the ZipDeployKind is a DeployFolder, the folder will be zipped first and the generated zip file returned.
            member this.GetZipPath targetFolder =
                match this with
                | DeployFolder appFolder ->
                    let packageFilename = Path.Combine(targetFolder, (Path.GetFileName appFolder) + ".zip")
                    File.Delete packageFilename
                    ZipFile.CreateFromDirectory(appFolder, packageFilename)
                    packageFilename
                | DeployZip zipFilePath ->
                    zipFilePath

    type Sites =
        { Name : ResourceName
          ServicePlan : ResourceName
          Location : Location
          AppSettings : List<string * string>
          AlwaysOn : bool
          HTTPSOnly : bool
          Dependencies : ResourceName list
          Kind : string
          LinuxFxVersion : string option
          AppCommandLine : string option
          NetFrameworkVersion : string option
          JavaVersion : string option
          JavaContainer : string option
          JavaContainerVersion : string option
          PhpVersion : string option
          PythonVersion : string option
          Metadata : List<string * string>
          ZipDeployPath : string option
          Parameters : SecureParameter list }
        interface IParameters with
            member this.SecureParameters = this.Parameters
        interface IPostDeploy with
            member this.Run resourceGroupName =
                match this with
                | { ZipDeployPath = Some path; Name = name } ->
                    let path =
                        ZipDeploy.ZipDeployKind.TryParse path
                        |> Option.defaultWith (fun () ->
                            failwithf "Path '%s' must either be a folder to be zipped, or an existing zip." path)
                    printfn "Running ZIP deploy for %s" path.Value
                    Some(Deploy.Az.zipDeploy name.Value path.GetZipPath resourceGroupName)
                | _ ->
                    None
        interface IResource with
            member this.ResourceName = this.Name
            member this.ToArmObject() =
                {| ``type`` = "Microsoft.Web/sites"
                   name = this.Name.Value
                   apiVersion = "2016-08-01"
                   location = this.Location.ArmValue
                   dependsOn = this.Dependencies |> List.map(fun p -> p.Value)
                   kind = this.Kind
                   properties =
                       {| serverFarmId = this.ServicePlan.Value
                          httpsOnly = this.HTTPSOnly
                          siteConfig =
                               [ "alwaysOn", box this.AlwaysOn
                                 "appSettings", this.AppSettings |> List.map(fun (k,v) -> {| name = k; value = v |}) |> box
                                 match this.LinuxFxVersion with Some v -> "linuxFxVersion", box v | None -> ()
                                 match this.AppCommandLine with Some v -> "appCommandLine", box v | None -> ()
                                 match this.NetFrameworkVersion with Some v -> "netFrameworkVersion", box v | None -> ()
                                 match this.JavaVersion with Some v -> "javaVersion", box v | None -> ()
                                 match this.JavaContainer with Some v -> "javaContainer", box v | None -> ()
                                 match this.JavaContainerVersion with Some v -> "javaContainerVersion", box v | None -> ()
                                 match this.PhpVersion with Some v -> "phpVersion", box v | None -> ()
                                 match this.PythonVersion with Some v -> "pythonVersion", box v | None -> ()
                                 "metadata", this.Metadata |> List.map(fun (k,v) -> {| name = k; value = v |}) |> box ]
                               |> Map.ofList
                        |}
                |} :> _

module Insights =
    type Components =
        { Name : ResourceName
          Location : Location
          LinkedWebsite : ResourceName option }
        interface IResource with
            member this.ResourceName = this.Name
            member this.ToArmObject() =
                {| ``type`` = "Microsoft.Insights/components"
                   kind = "web"
                   name = this.Name.Value
                   location = this.Location.ArmValue
                   apiVersion = "2014-04-01"
                   tags =
                       [ match this.LinkedWebsite with
                         | Some linkedWebsite -> sprintf "[concat('hidden-link:', resourceGroup().id, '/providers/Microsoft.Web/sites/', '%s')]" linkedWebsite.Value, "Resource"
                         | None -> ()
                         "displayName", "AppInsightsComponent" ]
                       |> Map.ofList
                   properties =
                    match this.LinkedWebsite with
                    | Some linkedWebsite ->
                       box {| name = this.Name.Value
                              Application_Type = "web"
                              ApplicationId = linkedWebsite.Value |}
                    | None ->
                       box {| name = this.Name.Value
                              Application_Type = "web" |}
                |} :> _

module ContainerRegistry =
    type Registries =
        { Name : ResourceName
          Location : Location
          Sku : string
          AdminUserEnabled : bool }
        interface IResource with
            member this.ResourceName = this.Name
            member this.ToArmObject() =
                {| name = this.Name.Value
                   ``type`` = "Microsoft.ContainerRegistry/registries"
                   apiVersion = "2019-05-01"
                   sku = {| name = this.Sku |}
                   location = this.Location.ArmValue
                   tags = {||}
                   properties = {| adminUserEnabled = this.AdminUserEnabled |}
                |} :> _

module DocumentDb =
    module DatabaseAccounts =
        module SqlDatabases =
            type Containers =
                { Name : ResourceName
                  Account : ResourceName
                  Database : ResourceName
                  PartitionKey :
                    {| Paths : string list
                       Kind : string |}
                  IndexingPolicy :
                    {| IncludedPaths :
                        {| Path : string
                           Indexes :
                            {| Kind : string
                               DataType : string |} list
                        |} list
                       ExcludedPaths : string list
                    |}
                }
                interface IResource with
                    member this.ResourceName = this.Name
                    member this.ToArmObject() =
                        {| ``type`` = "Microsoft.DocumentDb/databaseAccounts/sqlDatabases/containers"
                           name = sprintf "%s/%s/%s" this.Account.Value this.Database.Value this.Name.Value
                           apiVersion = "2020-03-01"
                           dependsOn = [ this.Database.Value ]
                           properties =
                               {| resource =
                                   {| id = this.Name.Value
                                      partitionKey =
                                       {| paths = this.PartitionKey.Paths
                                          kind = this.PartitionKey.Kind |}
                                      indexingPolicy =
                                       {| indexingMode = "consistent"
                                          includedPaths =
                                              this.IndexingPolicy.IncludedPaths
                                              |> List.map(fun p ->
                                               {| path = p.Path
                                                  indexes =
                                                   p.Indexes
                                                   |> List.map(fun i ->
                                                       {| kind = i.Kind
                                                          dataType = i.DataType
                                                          precision = -1 |})
                                               |})
                                          excludedPaths =
                                           this.IndexingPolicy.ExcludedPaths
                                           |> List.map(fun p -> {| path = p |})
                                       |}
                                   |}
                               |}
                        |} :> _

        type SqlDatabases =
            { Name : ResourceName
              Account : ResourceName
              Throughput : string }
            interface IResource with
                member this.ResourceName = this.Name
                member this.ToArmObject() =
                    {| ``type`` = "Microsoft.DocumentDB/databaseAccounts/sqlDatabases"
                       name = sprintf "%s/%s" this.Account.Value this.Name.Value
                       apiVersion = "2020-03-01"
                       dependsOn = [ this.Account.Value ]
                       properties =
                           {| resource = {| id = this.Name.Value |}
                              options = {| throughput = this.Throughput |} |}
                    |} :> _

    type DatabaseAccount =
        { Name : ResourceName
          Location : Location
          ConsistencyPolicy : string
          MaxStaleness : int option
          MaxInterval : int option
          EnableAutomaticFailure : bool option
          EnableMultipleWriteLocations : bool option
          FailoverLocations : {| Location :  Location; Priority : int |} list
          PublicNetworkAccess : FeatureFlag
          FreeTier : bool }
        interface IResource with
            member this.ResourceName = this.Name
            member this.ToArmObject() =
                {| ``type`` = "Microsoft.DocumentDB/databaseAccounts"
                   name = this.Name.Value
                   apiVersion = "2020-03-01"
                   location = this.Location.ArmValue
                   kind = "GlobalDocumentDB"
                   tags =
                       {| defaultExperience = "Core (SQL)"
                          CosmosAccountType = "Non-Production" |}
                   properties =
                       {| consistencyPolicy =
                            {| defaultConsistencyLevel = this.ConsistencyPolicy
                               maxStalenessPrefix = this.MaxStaleness |> Option.toNullable
                               maxIntervalInSeconds = this.MaxInterval |> Option.toNullable
                            |}
                          databaseAccountOfferType = "Standard"
                          enableAutomaticFailure = this.EnableAutomaticFailure |> Option.toNullable
                          autoenableMultipleWriteLocations = this.EnableMultipleWriteLocations |> Option.toNullable
                          locations = [
                            for location in this.FailoverLocations do
                                {| locationName = location.Location.ArmValue
                                   failoverPriority = location.Priority |}
                          ]
                          publicNetworkAccess = string this.PublicNetworkAccess
                          enableFreeTier = this.FreeTier
                       |} |> box
                |} :> _

module EventHub =
    type Namespace =
        { Name : ResourceName
          Location : Location
          Sku : {| Name : string; Tier : string; Capacity : int |}
          ZoneRedundant : bool option
          IsAutoInflateEnabled : bool option
          MaxThroughputUnits : int option
          KafkaEnabled : bool option }
        interface IResource with
            member this.ResourceName = this.Name
            member this.ToArmObject() =
                {| ``type`` = "Microsoft.EventHub/namespaces"
                   apiVersion = "2017-04-01"
                   name = this.Name.Value
                   location = this.Location.ArmValue
                   sku =
                       {| name = this.Sku.Name
                          tier = this.Sku.Tier
                          capacity = this.Sku.Capacity |}
                   properties =
                       {| zoneRedundant = this.ZoneRedundant |> Option.toNullable
                          isAutoInflateEnabled = this.IsAutoInflateEnabled |> Option.toNullable
                          maximumThroughputUnits = this.MaxThroughputUnits |> Option.toNullable
                          kafkaEnabled = this.KafkaEnabled |> Option.toNullable |}
                |} :> _
    module Namespaces =
        type EventHub =
            { Name : ResourceName
              Location : Location
              MessageRetentionDays : int option
              Partitions : int
              Dependencies : ResourceName list }
            interface IResource with
                member this.ResourceName = this.Name
                member this.ToArmObject() =
                   {| ``type`` = "Microsoft.EventHub/namespaces/eventhubs"
                      apiVersion = "2017-04-01"
                      name = this.Name.Value
                      location = this.Location.ArmValue
                      dependsOn = this.Dependencies |> List.map(fun d -> d.Value)
                      properties =
                          {| messageRetentionInDays = this.MessageRetentionDays |> Option.toNullable
                             partitionCount = this.Partitions
                             status = "Active" |}
                   |} :> _
        module EventHubs =
            type ConsumerGroup =
                { Name : ResourceName
                  Location : Location
                  Dependencies : ResourceName list }
                interface IResource with
                    member this.ResourceName = this.Name
                    member this.ToArmObject() =
                        {| ``type`` = "Microsoft.EventHub/namespaces/eventhubs/consumergroups"
                           apiVersion = "2017-04-01"
                           name = this.Name.Value
                           location = this.Location.ArmValue
                           dependsOn = this.Dependencies |> List.map(fun d -> d.Value)
                        |} :> _

            type AuthorizationRule =
                { Name : ResourceName
                  Location : Location
                  Dependencies : ResourceName list
                  Rights : string list }
                interface IResource with
                    member this.ResourceName = this.Name
                    member this.ToArmObject() =
                        {| ``type`` = "Microsoft.EventHub/namespaces/eventhubs/AuthorizationRules"
                           apiVersion = "2017-04-01"
                           name = this.Name.Value
                           location = this.Location.ArmValue
                           dependsOn = this.Dependencies |> List.map(fun d -> d.Value)
                           properties = {| rights = this.Rights |}
                        |} :> _

module KeyVault =
    module Vaults =
        type Secret =
            { Name : ResourceName
              Value : SecretValue
              ParentKeyVault : ResourceName
              Location : Location
              ContentType : string option
              Enabled : bool Nullable
              ActivationDate : int Nullable
              ExpirationDate : int Nullable
              Dependencies : ResourceName list }
            interface IParameters with
                member this.SecureParameters =
                    match this with
                    | { Value = ParameterSecret secureParameter } -> [ secureParameter ]
                    | _ -> []
            interface IResource with
                member this.ResourceName = this.Name
                member this.ToArmObject() =
                    {| ``type`` = "Microsoft.KeyVault/vaults/secrets"
                       name = this.Name.Value
                       apiVersion = "2018-02-14"
                       location = this.Location.ArmValue
                       dependsOn = [
                           this.ParentKeyVault.Value
                           for dependency in this.Dependencies do
                               dependency.Value ]
                       properties =
                           {| value = this.Value.Value
                              contentType = this.ContentType |> Option.toObj
                              attributes =
                               {| enabled = this.Enabled
                                  nbf = this.ActivationDate
                                  exp = this.ExpirationDate
                               |}
                           |}
                       |} :> _

    type Vault =
        { Name : ResourceName
          Location : Location
          TenantId : string
          Sku : string
          Uri : string option
          EnabledForDeployment : bool option
          EnabledForDiskEncryption : bool option
          EnabledForTemplateDeployment : bool option
          EnableSoftDelete : bool option
          CreateMode : string option
          EnablePurgeProtection : bool option
          AccessPolicies :
            {| ObjectId : string
               ApplicationId : string option
               Permissions :
                {| Keys : string array
                   Secrets : string array
                   Certificates : string array
                   Storage : string array |}
            |} array
          DefaultAction : string option
          Bypass: string option
          IpRules : string list
          VnetRules : string list }
        interface IResource with
            member this.ResourceName = this.Name
            member this.ToArmObject() =
                {| ``type``= "Microsoft.KeyVault/vaults"
                   name = this.Name.Value
                   apiVersion = "2018-02-14"
                   location = this.Location.ArmValue
                   properties =
                     {| tenantId = this.TenantId
                        sku = {| name = this.Sku; family = "A" |}
                        enabledForDeployment = this.EnabledForDeployment |> Option.toNullable
                        enabledForDiskEncryption = this.EnabledForDiskEncryption |> Option.toNullable
                        enabledForTemplateDeployment = this.EnabledForTemplateDeployment |> Option.toNullable
                        enablePurgeProtection = this.EnablePurgeProtection |> Option.toNullable
                        createMode = this.CreateMode |> Option.toObj
                        vaultUri = this.Uri |> Option.toObj
                        accessPolicies =
                             [| for policy in this.AccessPolicies do
                                 {| objectId = policy.ObjectId
                                    tenantId = this.TenantId
                                    applicationId = policy.ApplicationId |> Option.toObj
                                    permissions =
                                     {| keys = policy.Permissions.Keys
                                        storage = policy.Permissions.Storage
                                        certificates = policy.Permissions.Certificates
                                        secrets = policy.Permissions.Secrets |}
                                 |}
                             |]
                        networkAcls =
                         {| defaultAction = this.DefaultAction |> Option.toObj
                            bypass = this.Bypass |> Option.toObj
                            ipRules = this.IpRules
                            virtualNetworkRules = this.VnetRules |}
                     |}
                 |} :> _

module Cache =
    type Redis =
        { Name : ResourceName
          Location : Location
          Sku :
            {| Name : string
               Family : char
               Capacity : int |}
          RedisConfiguration : Map<string, string>
          NonSslEnabled : bool option
          ShardCount : int option
          MinimumTlsVersion : string option }
        interface IResource with
            member this.ResourceName = this.Name
            member this.ToArmObject() =
                {| ``type`` = "Microsoft.Cache/Redis"
                   apiVersion = "2018-03-01"
                   name = this.Name.Value
                   location = this.Location.ArmValue
                   properties =
                       {| sku =
                           {| name = this.Sku.Name
                              family = this.Sku.Family
                              capacity = this.Sku.Capacity
                           |}
                          enableNonSslPort = this.NonSslEnabled |> Option.toNullable
                          shardCount = this.ShardCount |> Option.toNullable
                          minimumTlsVersion = this.MinimumTlsVersion |> Option.toObj
                          redisConfiguration = this.RedisConfiguration
                       |}
                |} :> _

module Search =
    type SearchService =
        { Name : ResourceName
          Location : Location
          Sku : string
          HostingMode : string
          ReplicaCount : int
          PartitionCount : int }
        interface IResource with
            member this.ResourceName = this.Name
            member this.ToArmObject() =
                {| ``type`` = "Microsoft.Search/searchServices"
                   apiVersion = "2015-08-19"
                   name = this.Name.Value
                   location = this.Location.ArmValue
                   sku =
                    {| name = this.Sku |}
                   properties =
                    {| replicaCount = this.ReplicaCount
                       partitionCount = this.PartitionCount
                       hostingMode = this.HostingMode |}
                |} :> _

module ServiceBus =
    type ServiceBusQueue =
        { Name : ResourceName
          LockDuration : string option
          DuplicateDetection : bool option
          DuplicateDetectionHistoryTimeWindow : string option
          Session : bool option
          DeadLetteringOnMessageExpiration : bool option
          MaxDeliveryCount : int option
          EnablePartitioning : bool option
          DependsOn : ResourceName list }

    type Namespace =
        { Name : ResourceName
          Location : Location
          Sku : string
          Capacity : int option
          Queues :ServiceBusQueue list
          DependsOn : ResourceName list }
        interface IResource with
            member this.ResourceName = this.Name
            member this.ToArmObject() =
                {| ``type`` = "Microsoft.ServiceBus/namespaces"
                   apiVersion = "2017-04-01"
                   name = this.Name.Value
                   location = this.Location.ArmValue
                   sku =
                     {| name = this.Sku
                        tier = this.Sku
                        capacity = this.Capacity |> Option.toNullable |}
                   dependsOn = this.DependsOn |> List.map (fun r -> r.Value)
                   resources =
                     [ for queue in this.Queues do
                         {| apiVersion = "2017-04-01"
                            name = queue.Name.Value
                            ``type`` = "Queues"
                            dependsOn = queue.DependsOn |> List.map (fun r -> r.Value)
                            properties =
                             {| lockDuration = queue.LockDuration |> Option.toObj
                                requiresDuplicateDetection = queue.DuplicateDetection |> Option.toNullable
                                duplicateDetectionHistoryTimeWindow = queue.DuplicateDetectionHistoryTimeWindow |> Option.toObj
                                requiresSession = queue.Session |> Option.toNullable
                                deadLetteringOnMessageExpiration = queue.DeadLetteringOnMessageExpiration |> Option.toNullable
                                maxDeliveryCount = queue.MaxDeliveryCount |> Option.toNullable
                                enablePartitioning = queue.EnablePartitioning |> Option.toNullable |}
                         |}
                     ]
                |} :> _

module Sql =
    type Server =
        { ServerName : ResourceName
          Location : Location
          Credentials : {| Username : string; Password : SecureParameter |}
          Databases :
              {| Name : ResourceName
                 Edition : string
                 Collation : string
                 Objective : string
                 TransparentDataEncryption : FeatureFlag |} list
          FirewallRules :
              {| Name : string
                 Start : System.Net.IPAddress
                 End : System.Net.IPAddress |} list
        }
        interface IParameters with
            member this.SecureParameters = [ this.Credentials.Password ]
        interface IResource with
            member this.ResourceName = this.ServerName
            member this.ToArmObject() =
                {| ``type`` = "Microsoft.Sql/servers"
                   name = this.ServerName.Value
                   apiVersion = "2014-04-01-preview"
                   location = this.Location.ArmValue
                   tags = {| displayName = this.ServerName.Value |}
                   properties =
                       {| administratorLogin = this.Credentials.Username
                          administratorLoginPassword = this.Credentials.Password.AsArmRef.Eval()
                          version = "12.0" |}
                   resources = [
                       for database in this.Databases do
                           box
                               {| ``type`` = "databases"
                                  name = database.Name.Value
                                  apiVersion = "2015-01-01"
                                  location = this.Location.ArmValue
                                  tags = {| displayName = database.Name.Value |}
                                  properties =
                                   {| edition = database.Edition
                                      collation = database.Collation
                                      requestedServiceObjectiveName = database.Objective |}
                                  dependsOn = [
                                      this.ServerName.Value
                                  ]
                                  resources = [
                                      match database.TransparentDataEncryption with
                                      | Enabled ->
                                          {| ``type`` = "transparentDataEncryption"
                                             comments = "Transparent Data Encryption"
                                             name = "current"
                                             apiVersion = "2014-04-01-preview"
                                             properties = {| status = string database.TransparentDataEncryption |}
                                             dependsOn = [ database.Name.Value ]
                                          |}
                                       | Disabled ->
                                           ()
                                  ]
                               |}
                       for rule in this.FirewallRules do
                           box
                               {| ``type`` = "firewallrules"
                                  name = rule.Name
                                  apiVersion = "2014-04-01"
                                  location = this.Location.ArmValue
                                  properties =
                                   {| endIpAddress = string rule.Start
                                      startIpAddress = string rule.End |}
                                  dependsOn = [ this.ServerName.Value ]
                               |}
                   ]
                |} :> _

module Storage =
    type StorageAccount =
        { Name : ResourceName
          Location : Location
          Sku : StorageSku
          Containers : (string * string) list }
        interface IResource with
            member this.ResourceName = this.Name
            member this.ToArmObject() =
                {| ``type`` = "Microsoft.Storage/storageAccounts"
                   sku = {| name = this.Sku.ArmValue |}
                   kind = "StorageV2"
                   name = this.Name.Value
                   apiVersion = "2018-07-01"
                   location = this.Location.ArmValue
                   resources = [
                       for (name, access) in this.Containers do
                        {| ``type`` = "blobServices/containers"
                           apiVersion = "2018-03-01-preview"
                           name = "default/" + name
                           dependsOn = [ this.Name.Value ]
                           properties = {| publicAccess = access |}
                        |}
                   ]
                |} :> _

module Network =
    type PublicIpAddress =
        { Name : ResourceName
          Location : Location
          DomainNameLabel : string option }
        interface IResource with
            member this.ResourceName = this.Name
            member this.ToArmObject() =
                {| ``type`` = "Microsoft.Network/publicIPAddresses"
                   apiVersion = "2018-11-01"
                   name = this.Name.Value
                   location = this.Location.ArmValue
                   properties =
                      match this.DomainNameLabel with
                      | Some label ->
                          box
                              {| publicIPAllocationMethod = "Dynamic"
                                 dnsSettings = {| domainNameLabel = label.ToLower() |}
                              |}
                      | None ->
                          box {| publicIPAllocationMethod = "Dynamic" |}
                |} :> _
    type VirtualNetwork =
        { Name : ResourceName
          Location : Location
          AddressSpacePrefixes : string list
          Subnets : {| Name : ResourceName; Prefix : string |} list }
        interface IResource with
            member this.ResourceName = this.Name
            member this.ToArmObject() =
                {| ``type`` = "Microsoft.Network/virtualNetworks"
                   apiVersion = "2018-11-01"
                   name = this.Name.Value
                   location = this.Location.ArmValue
                   properties =
                        {| addressSpace = {| addressPrefixes = this.AddressSpacePrefixes |}
                           subnets =
                            this.Subnets
                            |> List.map(fun subnet ->
                               {| name = subnet.Name.Value
                                  properties = {| addressPrefix = subnet.Prefix |}
                               |})
                        |}
                |} :> _
    type NetworkInterface =
        { Name : ResourceName
          Location : Location
          IpConfigs :
            {| SubnetName : ResourceName
               PublicIpName : ResourceName |} list
          VirtualNetwork : ResourceName }
        interface IResource with
            member this.ResourceName = this.Name
            member this.ToArmObject() =
                {| ``type`` = "Microsoft.Network/networkInterfaces"
                   apiVersion = "2018-11-01"
                   name = this.Name.Value
                   location = this.Location.ArmValue
                   dependsOn = [
                       this.VirtualNetwork.Value
                       for config in this.IpConfigs do
                           config.PublicIpName.Value
                   ]
                   properties =
                       {| ipConfigurations =
                            this.IpConfigs
                            |> List.mapi(fun index ipConfig ->
                                {| name = sprintf "ipconfig%i" (index + 1)
                                   properties =
                                    {| privateIPAllocationMethod = "Dynamic"
                                       publicIPAddress = {| id = sprintf "[resourceId('Microsoft.Network/publicIPAddresses','%s')]" ipConfig.PublicIpName.Value |}
                                       subnet = {| id = sprintf "[resourceId('Microsoft.Network/virtualNetworks/subnets', '%s', '%s')]" this.VirtualNetwork.Value ipConfig.SubnetName.Value |}
                                    |}
                                |})
                       |}
                |} :> _

    type ExpressRouteCircuit =
        { Name : ResourceName
          Location : Location
          Tier : string // ExpressRouteTier
          Family : string // ExpressRouteFamily
          ServiceProviderName : string
          PeeringLocation : string
          Bandwidth : int
          GlobalReachEnabled : bool
          Peerings :
            {| PeeringType : string
               AzureASN : int
               PeerASN : int64
               PrimaryPeerAddressPrefix : string
               SecondaryPeerAddressPrefix : string
               SharedKey : string option
               VlanId : int
            |} list }
        interface IResource with
            member this.ResourceName = this.Name
            member this.ToArmObject() =
                {| ``type`` = "Microsoft.Network/expressRouteCircuits"
                   apiVersion = "2019-02-01"
                   name = this.Name.Value
                   location = this.Location.ArmValue
                   sku = {| name = String.Format("{0}_{1}", this.Tier, this.Family); tier = this.Tier; family = this.Family |}
                   properties =
                       {| peerings = [
                            for peer in this.Peerings do
                                {| name = peer.PeeringType
                                   properties =
                                       {| peeringType = peer.PeeringType
                                          azureASN = peer.AzureASN
                                          peerASN = peer.PeerASN
                                          primaryPeerAddressPrefix = peer.PrimaryPeerAddressPrefix
                                          secondaryPeerAddressPrefix = peer.SecondaryPeerAddressPrefix
                                          vlanId = peer.VlanId
                                          sharedKey = peer.SharedKey |}
                                |}
                          ]
                          serviceProviderProperties =
                            {| serviceProviderName = this.ServiceProviderName
                               peeringLocation = this.PeeringLocation
                               bandwidthInMbps = this.Bandwidth |}
                          globalReachEnabled = this.GlobalReachEnabled |}
                |} :> _
module Compute =
    type VirtualMachine =
        { Name : ResourceName
          Location : Location
          StorageAccount : ResourceName option
          Size : VMSize
          Credentials : {| Username : string; Password : SecureParameter |}
          Image : ImageDefinition
          OsDisk : {| Size : int; DiskType : string |}
          DataDisks : {| Size : int; DiskType : string |} list
          NetworkInterfaceName : ResourceName }
        interface IParameters with
            member this.SecureParameters = [ this.Credentials.Password ]
        interface IResource with
            member this.ResourceName = this.Name
            member this.ToArmObject() =
                {| ``type`` = "Microsoft.Compute/virtualMachines"
                   apiVersion = "2018-10-01"
                   name = this.Name.Value
                   location = this.Location.ArmValue
                   dependsOn = [
                       this.NetworkInterfaceName.Value
                       match this.StorageAccount with
                       | Some s -> s.Value
                       | None -> ()
                   ]
                   properties =
                    {| hardwareProfile = {| vmSize = this.Size.ArmValue |}
                       osProfile =
                        {|
                           computerName = this.Name.Value
                           adminUsername = this.Credentials.Username
                           adminPassword = this.Credentials.Password.AsArmRef.Eval()
                        |}
                       storageProfile =
                           let vmNameLowerCase = this.Name.Value.ToLower()
                           {| imageReference =
                               {| publisher = this.Image.Publisher.ArmValue
                                  offer = this.Image.Offer.ArmValue
                                  sku = this.Image.Sku.ArmValue
                                  version = "latest" |}
                              osDisk =
                               {| createOption = "FromImage"
                                  name = sprintf "%s-osdisk" vmNameLowerCase
                                  diskSizeGB = this.OsDisk.Size
                                  managedDisk = {| storageAccountType = this.OsDisk.DiskType |}
                               |}
                              dataDisks =
                               this.DataDisks
                               |> List.mapi(fun lun dataDisk ->
                                   {| createOption = "Empty"
                                      name = sprintf "%s-datadisk-%i" vmNameLowerCase lun
                                      diskSizeGB = dataDisk.Size
                                      lun = lun
                                      managedDisk = {| storageAccountType = dataDisk.DiskType |} |})
                           |}
                       networkProfile =
                           {| networkInterfaces = [
                               {| id = sprintf "[resourceId('Microsoft.Network/networkInterfaces','%s')]" this.NetworkInterfaceName.Value |}
                              ]
                           |}
                       diagnosticsProfile =
                           match this.StorageAccount with
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
                |} :> _
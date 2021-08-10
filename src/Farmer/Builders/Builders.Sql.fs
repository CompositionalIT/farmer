[<AutoOpen>]
module Farmer.Builders.SqlAzure

open Farmer
open Farmer.Sql
open Farmer.Arm.Sql
open System.Net
open Servers
open Databases

type SqlAzureDbConfig =
    { Name : ResourceName
      Sku : DbPurchaseModel option
      MaxSize : int<Mb> option
      Collation : string
      Encryption : FeatureFlag }

type SqlAzureConfig =
    { Name : SqlAccountName
      AdministratorCredentials : {| UserName : string; Password : SecureParameter |}
      MinTlsVersion : TlsVersion option
      FirewallRules : {| Name : ResourceName; Start : IPAddress; End : IPAddress |} list
      ElasticPoolSettings :
        {| Name : ResourceName option
           Sku : PoolSku
           PerDbLimits : {| Min: int<DTU>; Max : int<DTU> |} option
           Capacity : int<Mb> option |}
      Databases : SqlAzureDbConfig list
      GeoReplicaServer : 
        {| /// Suffix name for server and database name
           NameSuffix : string; 
           /// Replication location, different from the original one
           Location : Farmer.Location;
           /// Override database Skus
           DbSku : Farmer.Sql.DtuSku option |} option
      Tags: Map<string,string>  }
    /// Gets a basic .NET connection string using the administrator username / password.
    member this.ConnectionString (database:SqlAzureDbConfig) =
        let expr =
            ArmExpression.concat [
                ArmExpression.literal $"Server=tcp:{this.Name.ResourceName.Value}.database.windows.net,1433;Initial Catalog={database.Name.Value};Persist Security Info=False;User ID={this.AdministratorCredentials.UserName};Password="
                this.AdministratorCredentials.Password.ArmExpression
                ArmExpression.literal ";MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
            ]
        expr.WithOwner (databases.resourceId (this.Name.ResourceName, database.Name))
    member this.ConnectionString databaseName =
        this.Databases
        |> List.tryFind(fun db -> db.Name = databaseName)
        |> Option.map this.ConnectionString
        |> Option.defaultWith(fun _ -> failwith $"Unknown database name {databaseName.Value}")
    member this.ConnectionString databaseName = this.ConnectionString (ResourceName databaseName)
    /// The key of the parameter that is required by Farmer for the SQL password.
    member this.PasswordParameter = $"password-for-{this.Name.ResourceName.Value}"

    interface IBuilder with
        member this.ResourceId = servers.resourceId this.Name.ResourceName
        member this.BuildResources location = [
            let elasticPoolName =
                this.ElasticPoolSettings.Name
                |> Option.defaultValue (this.Name.ResourceName-"pool")

            { ServerName = this.Name
              Location = location
              Credentials =
                {| Username = this.AdministratorCredentials.UserName
                   Password = this.AdministratorCredentials.Password |}
              MinTlsVersion = this.MinTlsVersion
              Tags = this.Tags
            }

            for database in this.Databases do
                { Name = database.Name
                  Server = this.Name
                  Location = location
                  MaxSizeBytes =
                    match database.Sku, database.MaxSize with
                    | Some _, Some maxSize -> Some (Mb.toBytes maxSize)
                    | _ -> None
                  Sku =
                   match database.Sku with
                   | Some dbSku -> Standalone dbSku
                   | None -> Pool elasticPoolName
                  Collation = database.Collation }

                match database.Encryption with
                | Enabled ->
                  { Server = this.Name
                    Database = database.Name }
                | Disabled ->
                  ()

            for rule in this.FirewallRules do
                { Name = rule.Name
                  Start = rule.Start
                  End = rule.End
                  Location = location
                  Server = this.Name }

            if this.Databases |> List.exists(fun db -> db.Sku.IsNone) then
                { Name = elasticPoolName
                  Server = this.Name
                  Location = location
                  Sku = this.ElasticPoolSettings.Sku
                  MaxSizeBytes = this.ElasticPoolSettings.Capacity |> Option.map Mb.toBytes
                  MinMax = this.ElasticPoolSettings.PerDbLimits |> Option.map(fun l -> l.Min, l.Max) }

            match this.GeoReplicaServer with
            | Some replica ->
                if replica.Location.ArmValue = location.ArmValue then
                    failwith $"Geo-replica cannot be deployed to the same location than the main database {this.Name}: {location.ArmValue}"
                else
                let replicaServerName = 
                    match (this.Name.ResourceName.Value + replica.NameSuffix) |> SqlAccountName.Create with
                    | Ok x -> x
                    | Error e -> failwith e

                { ServerName = replicaServerName
                  Location = replica.Location
                  Credentials =
                    {| Username = this.AdministratorCredentials.UserName
                       Password = this.AdministratorCredentials.Password |}
                  MinTlsVersion = this.MinTlsVersion
                  Tags = this.Tags
                }
                for database in this.Databases do
                    let geoSku =
                        match replica.DbSku, database.Sku with
                        | Some relicaSku, _ -> relicaSku.Name, relicaSku.Edition
                        | None, Some dbSku -> dbSku.Name, dbSku.Edition
                        | None, None -> this.ElasticPoolSettings.Sku.Name, this.ElasticPoolSettings.Sku.Edition

                    let primaryDatabaseFullId =
                        ArmExpression.create(
                            $"concat('/subscriptions/', subscription().subscriptionId, '/resourceGroups/', resourceGroup().name, '/providers/Microsoft.Sql/servers/', '{this.Name.ResourceName.Value}', '/databases/','{database.Name.Value}')"
                        ).Eval()

                    {| apiVersion = "2021-02-01-preview"
                       location = replica.Location.ArmValue
                       dependsOn = [ Farmer.ResourceId.create(Farmer.Arm.Sql.servers, replicaServerName.ResourceName).Eval(); ]
                       name = $"{replicaServerName.ResourceName.Value}/{database.Name.Value + replica.NameSuffix}"
                       ``type`` = Farmer.Arm.Sql.databases.Type
                       sku =
                           {|  name = fst geoSku
                               tier = snd geoSku
                           |}
                       properties =
                           {|   createMode = "OnlineSecondary"
                                secondaryType = "Geo"
                                sourceDatabaseId = primaryDatabaseFullId
                                zoneRedundant = false
                                licenseType = ""
                                readScale = "Disabled"
                                highAvailabilityReplicaCount = 0
                                minCapacity = ""
                                autoPauseDelay = ""
                                requestedBackupStorageRedundancy = ""
    
                           |}
                    |} |> Farmer.Resource.ofObj
            | None -> ()
        ]

type SqlDbBuilder() =
    member _.Yield _ =
        { Name = ResourceName ""
          Collation = "SQL_Latin1_General_CP1_CI_AS"
          Sku = None
          MaxSize = None
          Encryption = Disabled }
    /// Sets the name of the database.
    [<CustomOperation "name">]
    member _.DbName(state:SqlAzureDbConfig, name) = { state with Name = name }
    member this.DbName(state:SqlAzureDbConfig, name:string) = this.DbName(state, ResourceName name)
    /// Sets the sku of the database
    [<CustomOperation "sku">]
    member _.DbSku(state:SqlAzureDbConfig, sku:DtuSku) = { state with Sku = Some (DTU sku) }
    member _.DbSku(state:SqlAzureDbConfig, sku:MSeries) = { state with Sku = Some (VCore(MemoryIntensive sku, LicenseRequired)) }
    member _.DbSku(state:SqlAzureDbConfig, sku:FSeries) = { state with Sku = Some (VCore(CpuIntensive sku, LicenseRequired)) }
    member _.DbSku(state:SqlAzureDbConfig, sku:VCoreSku) = { state with Sku = Some (VCore (sku, LicenseRequired)) }
    /// Sets the collation of the database.
    [<CustomOperation "collation">]
    member _.DbCollation(state:SqlAzureDbConfig, collation:string) = { state with Collation = collation }
    /// States that you already have a SQL license and qualify for Azure Hybrid Benefit discount.
    [<CustomOperation "hybrid_benefit">]
    member _.ZoneRedundant(state:SqlAzureDbConfig) =
        { state with
            Sku =
                match state.Sku with
                | Some (VCore (v, _)) ->
                    Some (VCore (v, AzureHybridBenefit))
                | Some (DTU _)
                | None ->
                    failwith "You can only set licensing on VCore databases. Ensure that you have already set the SKU to a VCore model."
        }
    /// Sets the maximum size of the database, if this database is not part of an elastic pool.
    [<CustomOperation "db_size">]
    member _.MaxSize(state:SqlAzureDbConfig, size:int<Mb>) = { state with MaxSize = Some size }
    /// Enables encryption of the database.
    [<CustomOperation "use_encryption">]
    member _.UseEncryption(state:SqlAzureDbConfig) = { state with Encryption = Enabled }
    /// Adds a custom firewall rule given a name, start and end IP address range.
    member _.Run (state:SqlAzureDbConfig) =
        if state.Name = ResourceName.Empty then failwith "You must set a database name."
        state

type SqlServerBuilder() =
    let makeIp (text:string) = IPAddress.Parse text
    member __.Yield _ =
        { Name = (SqlAccountName.Create "defaultvalue").OkValue
          AdministratorCredentials = {| UserName = ""; Password = SecureParameter "" |}
          ElasticPoolSettings =
            {| Name = None
               Sku = PoolSku.Basic50
               PerDbLimits = None
               Capacity = None |}
          Databases = []
          FirewallRules = []
          MinTlsVersion = None
          GeoReplicaServer = None
          Tags = Map.empty  }
    member __.Run state : SqlAzureConfig =
        { state with
            AdministratorCredentials =
                if System.String.IsNullOrWhiteSpace state.AdministratorCredentials.UserName then failwith $"You must specify the admin_username for SQL Server instance {state.Name.ResourceName.Value}"
                {| state.AdministratorCredentials with
                    Password = SecureParameter state.PasswordParameter |} }
    /// Sets the name of the SQL server.
    [<CustomOperation "name">]
    member _.ServerName(state:SqlAzureConfig, serverName) = { state with Name = serverName }
    member this.ServerName(state:SqlAzureConfig, serverName:string) = this.ServerName(state, SqlAccountName.Create(serverName).OkValue)
    /// Sets the name of the elastic pool. If not set, the name will be generated based off the server name.
    [<CustomOperation "elastic_pool_name">]
    member _.Name(state:SqlAzureConfig, name) = { state with ElasticPoolSettings = {| state.ElasticPoolSettings with Name = Some name |} }
    member this.Name(state, name) = this.Name(state, ResourceName name)
    /// Sets the sku of the server, to be shared on all databases that do not have an explicit sku set.
    [<CustomOperation "elastic_pool_sku">]
    member _.Sku(state:SqlAzureConfig, sku) = { state with ElasticPoolSettings = {| state.ElasticPoolSettings with Sku = sku |} }
    /// The per-database min and max DTUs to allocate.
    [<CustomOperation "elastic_pool_database_min_max">]
    member _.PerDbLimits(state:SqlAzureConfig, min, max) = { state with ElasticPoolSettings = {| state.ElasticPoolSettings with PerDbLimits = Some {| Min = min; Max = max |} |} }
    /// The per-database min and max DTUs to allocate.
    [<CustomOperation "elastic_pool_capacity">]
    member _.PoolCapacity(state:SqlAzureConfig, capacity) = { state with ElasticPoolSettings = {| state.ElasticPoolSettings with Capacity = Some capacity |} }
    /// The per-database min and max DTUs to allocate.
    [<CustomOperation "add_databases">]
    member _.AddDatabases(state:SqlAzureConfig, databases) = { state with Databases = state.Databases @ databases }
    /// Adds a firewall rule that enables access to a specific IP Address range.
    [<CustomOperation "add_firewall_rule">]
    member __.AddFirewallRule(state:SqlAzureConfig, name, startRange, endRange) =
        { state with
            FirewallRules =
                {| Name = ResourceName name
                   Start = makeIp startRange
                   End = makeIp endRange |}
                :: state.FirewallRules }
    /// Adds a firewall rules that enables access to a specific IP Address range.
    [<CustomOperation "add_firewall_rules">]
    member __.AddFirewallRules(state:SqlAzureConfig, listOfRules:(string*string*string) list) =
        let newRules =
            listOfRules |> List.map(fun (name, startRange, endRange) ->
                {| Name = ResourceName name
                   Start = makeIp startRange
                   End = makeIp endRange |})
        { state with FirewallRules = newRules @ state.FirewallRules }
    /// Adds a firewall rule that enables access to other Azure services.
    [<CustomOperation "enable_azure_firewall">]
    member this.UseAzureFirewall(state:SqlAzureConfig) =
        this.AddFirewallRule(state, "allow-azure-services", "0.0.0.0", "0.0.0.0")
    /// Sets the admin username of the server (note: the password is supplied as a securestring parameter to the generated ARM template).
    [<CustomOperation "admin_username">]
    member __.AdminUsername(state:SqlAzureConfig, username) =
        { state with
            AdministratorCredentials =
                {| state.AdministratorCredentials with
                    UserName = username |} }

    /// Set minimum TLS version
    [<CustomOperation "min_tls_version">]
    member _.SetMinTlsVersion(state:SqlAzureConfig, minTlsVersion) =
        { state with MinTlsVersion = Some minTlsVersion }

    /// Geo-replicate all the databases in this server to another location, having NameSuffix after original server and database names.
    [<CustomOperation "geo_replicate">]
    member _.SetGeoReplication(state:SqlAzureConfig, replicaSettings) =
        { state with GeoReplicaServer = Some replicaSettings }

    interface ITaggable<SqlAzureConfig> with member _.Add state tags = { state with Tags = state.Tags |> Map.merge tags }

let sqlServer = SqlServerBuilder()
let sqlDb = SqlDbBuilder()
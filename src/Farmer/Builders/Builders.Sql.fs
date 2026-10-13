[<AutoOpen>]
module Farmer.Builders.SqlAzure

open Farmer
open Farmer.Arm
open Farmer.Arm.Sql.Servers
open Farmer.Arm.Sql.Servers.Databases
open Farmer.Sql
open Farmer.Identity
open System
open System.Net

type SqlAzureDbConfig = {
    Name: ResourceName
    Server: LinkedResource option
    ElasticPool: LinkedResource option
    Sku: DbPurchaseModel option
    MaxSize: int<Mb> option
    Collation: string
    Encryption: FeatureFlag
} with

    interface IBuilder with
        member this.ResourceId =
            match this.Server with
            | Some server -> {
                Type = databases
                ResourceGroup = server.ResourceId.ResourceGroup
                Subscription = server.ResourceId.Subscription
                Name = server.Name
                Segments = [ this.Name ]
              }
            | None -> raiseFarmer "You must link the SQL database to a SQL server before adding it as a resource."

        member this.BuildResources location = [
            let server =
                this.Server
                |> Option.defaultWith (fun _ ->
                    raiseFarmer "You must link the SQL database to a SQL server before adding it as a resource.")

            let serverName =
                match SqlAccountName.Create server.Name.Value with
                | Ok name -> name
                | Error error -> raiseFarmer error

            let sku =
                match this.ElasticPool, this.Sku with
                | Some pool, _ -> LinkedPool pool
                | None, Some dbSku -> Standalone dbSku
                | None, None -> raiseFarmer "A standalone SQL database must have a SKU."

            let database = {
                Name = this.Name
                Server = serverName
                ServerResource = server
                Location = location
                MaxSizeBytes = this.MaxSize |> Option.map Mb.toBytes
                Sku = sku
                Collation = this.Collation
            }

            yield database

            match this.Encryption with
            | Enabled ->
                yield {
                    Server = serverName
                    Database = this.Name
                }
            | Disabled -> ()
        ]

type SqlAzureConfig = {
    Name: SqlAccountName
    Credentials: SqlCredentials option
    MinTlsVersion: TlsVersion option
    Identity: ManagedIdentity
    FirewallRules:
        {|
            Name: ResourceName
            Start: IPAddress
            End: IPAddress
        |} list
    ElasticPoolSettings: {|
        Name: ResourceName option
        Sku: PoolSku
        PerDbLimits: {| Min: int<DTU>; Max: int<DTU> |} option
        Capacity: int<Mb> option
        HighAvailabilityReplicaCount: int option
    |}
    Databases: SqlAzureDbConfig list
    GeoReplicaServer: GeoReplicationSettings option
    Tags: Map<string, string>
} with

    /// Gets a basic .NET connection string using the administrator username / password.
    member this.ConnectionString(database: SqlAzureDbConfig) =
        match this.Credentials with
        | Some(SqlOnly sqlCredentials)
        | Some(SqlAndEntra(sqlCredentials, _)) ->
            let expr =
                ArmExpression.concat [
                    ArmExpression.literal
                        $"Server=tcp:{this.Name.ResourceName.Value}.database.windows.net,1433;Initial Catalog={database.Name.Value};Persist Security Info=False;User ID={sqlCredentials.Username};Password="
                    sqlCredentials.Password.ArmExpression
                    ArmExpression.literal
                        ";MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
                ]

            expr.WithOwner(databases.resourceId (this.Name.ResourceName, database.Name))
        | Some(EntraOnly _) ->
            raiseFarmer
                "Cannot create a connection string for a database that is using Azure Active Directory authentication."
        | None -> raiseFarmer "Cannot create a connection string for a database that does not have any credentials set."

    member this.ConnectionString databaseName =
        this.Databases
        |> List.tryFind (fun db -> db.Name = databaseName)
        |> Option.map this.ConnectionString
        |> Option.defaultWith (fun _ -> raiseFarmer $"Unknown database name {databaseName.Value}")

    member this.ConnectionString databaseName =
        this.ConnectionString(ResourceName databaseName)

    /// The key of the parameter that is required by Farmer for the SQL password.
    member this.PasswordParameter = $"password-for-{this.Name.ResourceName.Value}"

    interface IBuilder with
        member this.ResourceId = servers.resourceId this.Name.ResourceName

        member this.BuildResources location = [
            let elasticPoolName =
                this.ElasticPoolSettings.Name
                |> Option.defaultValue (this.Name.ResourceName - "pool")

            {
                ServerName = this.Name
                Location = location
                Credentials =
                    match this.Credentials with
                    | Some credentials -> credentials
                    | None -> raiseFarmer "No credentials have been set for the SQL Server instance."
                MinTlsVersion = this.MinTlsVersion
                Identity = this.Identity
                Tags = this.Tags
            }

            for database in this.Databases do
                {
                    Name = database.Name
                    Server = this.Name
                    ServerResource = Managed(servers.resourceId this.Name.ResourceName)
                    Location = location
                    MaxSizeBytes =
                        match database.Sku, database.MaxSize with
                        | Some _, Some maxSize -> Some(Mb.toBytes maxSize)
                        | _ -> None
                    Sku =
                        match database.Sku with
                        | Some dbSku -> Standalone dbSku
                        | None -> Pool elasticPoolName
                    Collation = database.Collation
                }

                match database.Encryption with
                | Enabled -> {
                    Server = this.Name
                    Database = database.Name
                  }
                | Disabled -> ()

            for rule in this.FirewallRules do
                {
                    Name = rule.Name
                    Start = rule.Start
                    End = rule.End
                    Location = location
                    Server = this.Name
                }

            if this.Databases |> List.exists (fun db -> db.Sku.IsNone) then
                {
                    Name = elasticPoolName
                    Server = this.Name
                    Location = location
                    Sku = this.ElasticPoolSettings.Sku
                    MaxSizeBytes = this.ElasticPoolSettings.Capacity |> Option.map Mb.toBytes
                    MinMax = this.ElasticPoolSettings.PerDbLimits |> Option.map (fun l -> l.Min, l.Max)
                    HighAvailabilityReplicaCount = this.ElasticPoolSettings.HighAvailabilityReplicaCount
                }

            match this.GeoReplicaServer with
            | Some replica ->
                if replica.Location.ArmValue = location.ArmValue then
                    raiseFarmer
                        $"Geo-replica cannot be deployed to the same location than the main database {this.Name}: {location.ArmValue}"
                else
                    let replicaServerName =
                        match (this.Name.ResourceName.Value + replica.NameSuffix) |> SqlAccountName.Create with
                        | Ok x -> x
                        | Error e -> raiseFarmer e

                    {
                        ServerName = replicaServerName
                        Location = replica.Location
                        Credentials =
                            match this.Credentials with
                            | Some credentials -> credentials
                            | None -> raiseFarmer "No credentials have been set for the SQL Server instance."
                        MinTlsVersion = this.MinTlsVersion
                        Identity = this.Identity
                        Tags = this.Tags
                    }

                    for database in this.Databases do
                        let geoSku =
                            match replica.DbSku, database.Sku with
                            | Some relicaSku, _ -> relicaSku.Name, relicaSku.Edition
                            | None, Some dbSku -> dbSku.Name, dbSku.Edition
                            | None, None -> this.ElasticPoolSettings.Sku.Name, this.ElasticPoolSettings.Sku.Edition

                        let primaryDatabaseFullId =
                            ArmExpression
                                .create(
                                    $"concat('/subscriptions/', subscription().subscriptionId, '/resourceGroups/', resourceGroup().name, '/providers/Microsoft.Sql/servers/', '{this.Name.ResourceName.Value}', '/databases/','{database.Name.Value}')"
                                )
                                .Eval()

                        {|
                            apiVersion = "2021-02-01-preview"
                            location = replica.Location.ArmValue
                            dependsOn = [
                                Farmer.ResourceId.create(servers, replicaServerName.ResourceName).Eval()
                                primaryDatabaseFullId
                            ]
                            name = $"{replicaServerName.ResourceName.Value}/{database.Name.Value + replica.NameSuffix}"
                            ``type`` = databases.Type
                            sku = {|
                                name = fst geoSku
                                tier = snd geoSku
                            |}
                            properties = {|
                                createMode = "OnlineSecondary"
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
                        |}
                        |> Farmer.Resource.ofObj
            | None -> ()
        ]

type SqlDbBuilder() =
    member _.Yield _ = {
        Name = ResourceName ""
        Server = None
        ElasticPool = None
        Collation = "SQL_Latin1_General_CP1_CI_AS"
        Sku = None
        MaxSize = None
        Encryption = Disabled
    }

    /// Sets the name of the database.
    [<CustomOperation "name">]
    member _.DbName(state: SqlAzureDbConfig, name) = { state with Name = name }

    member this.DbName(state: SqlAzureDbConfig, name: string) = this.DbName(state, ResourceName name)

    /// Links the database to a SQL server that is managed outside this builder.
    [<CustomOperation "link_to_sql_server">]
    member _.LinkToSqlServer(state: SqlAzureDbConfig, server: ResourceId) = {
        state with
            Server = Some(Unmanaged server)
    }

    member this.LinkToSqlServer(state: SqlAzureDbConfig, server: string) =
        this.LinkToSqlServer(state, servers.resourceId (ResourceName server))

    /// Links the database to an elastic pool that is managed outside this builder.
    [<CustomOperation "link_to_elastic_pool">]
    member _.LinkToElasticPool(state: SqlAzureDbConfig, pool: ResourceId) = {
        state with
            ElasticPool = Some(Unmanaged pool)
            Sku = None
    }

    member this.LinkToElasticPool(state: SqlAzureDbConfig, pool: string) =
        this.LinkToElasticPool(state, elasticPools.resourceId (ResourceName pool))

    /// Sets the sku of the database
    [<CustomOperation "sku">]
    member _.DbSku(state: SqlAzureDbConfig, sku: DtuSku) = { state with Sku = Some(DTU sku) }

    member _.DbSku(state: SqlAzureDbConfig, sku: MSeries) = {
        state with
            Sku = Some(VCore(MemoryIntensive sku, LicenseRequired))
    }

    member _.DbSku(state: SqlAzureDbConfig, sku: FSeries) = {
        state with
            Sku = Some(VCore(CpuIntensive sku, LicenseRequired))
    }

    member _.DbSku(state: SqlAzureDbConfig, sku: VCoreSku) = {
        state with
            Sku = Some(VCore(sku, LicenseRequired))
    }

    /// Sets the collation of the database.
    [<CustomOperation "collation">]
    member _.DbCollation(state: SqlAzureDbConfig, collation: string) = { state with Collation = collation }

    /// States that you already have a SQL license and qualify for Azure Hybrid Benefit discount.
    [<CustomOperation "hybrid_benefit">]
    member _.ZoneRedundant(state: SqlAzureDbConfig) = {
        state with
            Sku =
                match state.Sku with
                | Some(VCore(v, _)) -> Some(VCore(v, AzureHybridBenefit))
                | Some(DTU _)
                | None ->
                    raiseFarmer
                        "You can only set licensing on VCore databases. Ensure that you have already set the SKU to a VCore model."
    }

    /// Sets the maximum size of the database, if this database is not part of an elastic pool.
    [<CustomOperation "db_size">]
    member _.MaxSize(state: SqlAzureDbConfig, size: int<Mb>) = { state with MaxSize = Some size }

    /// Enables encryption of the database.
    [<CustomOperation "use_encryption">]
    member _.UseEncryption(state: SqlAzureDbConfig) = { state with Encryption = Enabled }

    /// Adds a custom firewall rule given a name, start and end IP address range.
    member _.Run(state: SqlAzureDbConfig) =
        if state.Name = ResourceName.Empty then
            raiseFarmer "You must set a database name."

        state

type SqlVirtualNetworkRuleConfig = {
    Name: ResourceName
    SqlServer: LinkedResource
    VNet: LinkedResource
    Subnet: ResourceName
    IgnoreMissingVnetServiceEndpoint: bool option
    Tags: Map<string, string>
} with

    interface IBuilder with
        member this.ResourceId =
            virtualNetworkRules.resourceId (this.SqlServer.Name / this.Name)

        member this.BuildResources _ = [
            let resource: IArmResource = {
                Farmer.Arm.Sql.VirtualNetworkRule.Name = this.Name
                SqlServer = this.SqlServer
                VNet = this.VNet
                Subnet = this.Subnet
                IgnoreMissingVnetServiceEndpoint = this.IgnoreMissingVnetServiceEndpoint
                Tags = this.Tags
            }

            resource
        ]

    interface ITaggable<SqlVirtualNetworkRuleConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

type SqlVirtualNetworkRuleBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        SqlServer = Managed(servers.resourceId ResourceName.Empty)
        VNet = Managed(Network.virtualNetworks.resourceId ResourceName.Empty)
        Subnet = ResourceName.Empty
        IgnoreMissingVnetServiceEndpoint = None
        Tags = Map.empty
    }

    member _.Run state =
        if state.Name = ResourceName.Empty then
            raiseFarmer "You must set a virtual network rule name."

        if state.SqlServer.Name = ResourceName.Empty then
            raiseFarmer "You must set a SQL server name."

        if state.VNet.Name = ResourceName.Empty then
            raiseFarmer "You must set a virtual network."

        if state.Subnet = ResourceName.Empty then
            raiseFarmer "You must set a subnet name."

        state

    [<CustomOperation "name">]
    member _.Name(state: SqlVirtualNetworkRuleConfig, name: string) = { state with Name = ResourceName name }

    [<CustomOperation "sql_server">]
    member _.SqlServer(state: SqlVirtualNetworkRuleConfig, sqlServer: string) = {
        state with
            SqlServer = Managed(servers.resourceId (ResourceName sqlServer))
    }

    member _.SqlServer(state: SqlVirtualNetworkRuleConfig, sqlServer: ResourceName) = {
        state with
            SqlServer = Managed(servers.resourceId sqlServer)
    }

    member _.SqlServer<'T when 'T :> IBuilder>(state: SqlVirtualNetworkRuleConfig, sqlServer: 'T) = {
        state with
            SqlServer = Managed sqlServer.ResourceId
    }

    [<CustomOperation "link_to_sql_server">]
    member _.LinkToSqlServer(state: SqlVirtualNetworkRuleConfig, sqlServer: ResourceId) = {
        state with
            SqlServer = Unmanaged sqlServer
    }

    member _.LinkToSqlServer<'T when 'T :> IBuilder>(state: SqlVirtualNetworkRuleConfig, sqlServer: 'T) = {
        state with
            SqlServer = Unmanaged sqlServer.ResourceId
    }

    member _.LinkToSqlServer(state: SqlVirtualNetworkRuleConfig, sqlServer: string) = {
        state with
            SqlServer = Unmanaged(servers.resourceId (ResourceName sqlServer))
    }

    [<CustomOperation "virtual_network">]
    member _.VirtualNetwork(state: SqlVirtualNetworkRuleConfig, vnet: ResourceId) = { state with VNet = Managed vnet }

    member _.VirtualNetwork(state: SqlVirtualNetworkRuleConfig, vnet: ResourceName) = {
        state with
            VNet = Managed(Network.virtualNetworks.resourceId vnet)
    }

    member _.VirtualNetwork<'T when 'T :> IBuilder>(state: SqlVirtualNetworkRuleConfig, vnet: 'T) = {
        state with
            VNet = Managed vnet.ResourceId
    }

    member _.VirtualNetwork(state: SqlVirtualNetworkRuleConfig, vnet: string) = {
        state with
            VNet = Managed(Network.virtualNetworks.resourceId (ResourceName vnet))
    }

    [<CustomOperation "link_to_virtual_network">]
    member _.LinkToVirtualNetwork(state: SqlVirtualNetworkRuleConfig, vnet: ResourceId) = {
        state with
            VNet = Unmanaged vnet
    }

    member _.LinkToVirtualNetwork<'T when 'T :> IBuilder>(state: SqlVirtualNetworkRuleConfig, vnet: 'T) = {
        state with
            VNet = Unmanaged vnet.ResourceId
    }

    member _.LinkToVirtualNetwork(state: SqlVirtualNetworkRuleConfig, vnet: string) = {
        state with
            VNet = Unmanaged(Network.virtualNetworks.resourceId (ResourceName vnet))
    }

    [<CustomOperation "subnet">]
    member _.Subnet(state: SqlVirtualNetworkRuleConfig, subnet: string) = {
        state with
            Subnet = ResourceName subnet
    }

    member _.Subnet(state: SqlVirtualNetworkRuleConfig, subnet: ResourceName) = { state with Subnet = subnet }

    [<CustomOperation "ignore_missing_vnet_service_endpoint">]
    member _.IgnoreMissingVnetServiceEndpoint(state: SqlVirtualNetworkRuleConfig, value: bool) = {
        state with
            IgnoreMissingVnetServiceEndpoint = Some value
    }

    [<CustomOperation "add_tags">]
    member _.AddTags(state: SqlVirtualNetworkRuleConfig, tags) = {
        state with
            Tags = state.Tags |> Map.merge tags
    }

let sqlVirtualNetworkRule = SqlVirtualNetworkRuleBuilder()

type SqlServerBuilder() =
    let makeIp (text: string) = IPAddress.Parse text

    member _.Yield _ = {
        Name = SqlAccountName.Empty
        Credentials = None
        ElasticPoolSettings = {|
            Name = None
            Sku = PoolSku.Basic50
            PerDbLimits = None
            Capacity = None
            HighAvailabilityReplicaCount = None
        |}
        Databases = []
        FirewallRules = []
        MinTlsVersion = None
        Identity = ManagedIdentity.Empty
        GeoReplicaServer = None
        Tags = Map.empty
    }

    member _.Run(state: SqlAzureConfig) : SqlAzureConfig =
        if state.Name.ResourceName = ResourceName.Empty then
            raiseFarmer "No SQL Server account name has been set."

        if state.Credentials.IsNone then
            raiseFarmer "No credentials have been set for the SQL Server instance."

        state

    /// Sets the name of the SQL server.
    [<CustomOperation "name">]
    member _.ServerName(state: SqlAzureConfig, serverName) = { state with Name = serverName }

    member this.ServerName(state: SqlAzureConfig, serverName: string) =
        this.ServerName(state, SqlAccountName.Create(serverName).OkValue)

    /// Sets the name of the elastic pool. If not set, the name will be generated based off the server name.
    [<CustomOperation "elastic_pool_name">]
    member _.Name(state: SqlAzureConfig, name) = {
        state with
            ElasticPoolSettings = {|
                state.ElasticPoolSettings with
                    Name = Some name
            |}
    }

    member this.Name(state, name) = this.Name(state, ResourceName name)

    /// Sets the sku of the server, to be shared on all databases that do not have an explicit sku set.
    [<CustomOperation "elastic_pool_sku">]
    member _.Sku(state: SqlAzureConfig, sku) = {
        state with
            ElasticPoolSettings = {|
                state.ElasticPoolSettings with
                    Sku = sku
            |}
    }

    /// The per-database min and max DTUs to allocate.
    [<CustomOperation "elastic_pool_database_min_max">]
    member _.PerDbLimits(state: SqlAzureConfig, min, max) = {
        state with
            ElasticPoolSettings = {|
                state.ElasticPoolSettings with
                    PerDbLimits = Some {| Min = min; Max = max |}
            |}
    }

    /// The per-database min and max DTUs to allocate.
    [<CustomOperation "elastic_pool_capacity">]
    member _.PoolCapacity(state: SqlAzureConfig, capacity) = {
        state with
            ElasticPoolSettings = {|
                state.ElasticPoolSettings with
                    Capacity = Some capacity
            |}
    }

    /// Sets the number of high availability replicas for a Hyperscale elastic pool.
    [<CustomOperation "elastic_pool_high_availability_replica_count">]
    member _.HighAvailabilityReplicaCount(state: SqlAzureConfig, count) = {
        state with
            ElasticPoolSettings = {|
                state.ElasticPoolSettings with
                    HighAvailabilityReplicaCount = Some count
            |}
    }

    /// The per-database min and max DTUs to allocate.
    [<CustomOperation "add_databases">]
    member _.AddDatabases(state: SqlAzureConfig, databases) = {
        state with
            Databases = state.Databases @ databases
    }

    /// Adds a firewall rule that enables access to a specific IP Address range.
    [<CustomOperation "add_firewall_rule">]
    member _.AddFirewallRule(state: SqlAzureConfig, name, startRange, endRange) = {
        state with
            FirewallRules =
                {|
                    Name = ResourceName name
                    Start = makeIp startRange
                    End = makeIp endRange
                |}
                :: state.FirewallRules
    }

    /// Adds a firewall rules that enables access to a specific IP Address range.
    [<CustomOperation "add_firewall_rules">]
    member _.AddFirewallRules(state: SqlAzureConfig, listOfRules: (string * string * string) list) =
        let newRules =
            listOfRules
            |> List.map (fun (name, startRange, endRange) -> {|
                Name = ResourceName name
                Start = makeIp startRange
                End = makeIp endRange
            |})

        {
            state with
                FirewallRules = newRules @ state.FirewallRules
        }

    /// Adds a firewall rule that enables access to other Azure services.
    [<CustomOperation "enable_azure_firewall">]
    member this.UseAzureFirewall(state: SqlAzureConfig) =
        this.AddFirewallRule(state, "allow-azure-services", "0.0.0.0", "0.0.0.0")

    /// Sets the admin username of the server (note: the password is supplied as a securestring parameter to the generated ARM template) using SQL authentication.
    /// If you have already set the Entra ID credentials, they will be preserved as a hybrid setup.
    [<CustomOperation "admin_username">]
    member _.AdminUsername(state: SqlAzureConfig, username) =
        let sqlCredentials = {
            Username = username
            Password = SecureParameter state.PasswordParameter
        }

        {
            state with
                Credentials =
                    match state.Credentials with
                    | None
                    | Some(SqlOnly _) -> Some(SqlOnly sqlCredentials)
                    | Some(SqlAndEntra(_, entraCredentials))
                    | Some(EntraOnly entraCredentials) -> Some(SqlAndEntra(sqlCredentials, entraCredentials))
        }

    /// Set minimum TLS version
    [<CustomOperation "min_tls_version">]
    member _.SetMinTlsVersion(state: SqlAzureConfig, minTlsVersion) = {
        state with
            MinTlsVersion = Some minTlsVersion
    }

    /// Adds a user-assigned managed identity to the SQL server.
    [<CustomOperation "add_identity">]
    member _.AddIdentity(state: SqlAzureConfig, identity: UserAssignedIdentity) = {
        state with
            Identity = state.Identity + identity
    }

    member this.AddIdentity(state, identity: UserAssignedIdentityConfig) =
        this.AddIdentity(state, identity.UserAssignedIdentity)

    /// Adds a system-assigned managed identity to the SQL server.
    [<CustomOperation "system_identity">]
    member _.SystemIdentity(state: SqlAzureConfig) = {
        state with
            Identity = {
                state.Identity with
                    SystemAssigned = Enabled
            }
    }

    /// Geo-replicate all the databases in this server to another location, having NameSuffix after original server and database names.
    [<CustomOperation "geo_replicate">]
    member _.SetGeoReplication(state, replicaSettings) = {
        state with
            GeoReplicaServer = Some replicaSettings
    }

    /// Activates Entra ID authentication using the supplied Entra username (i.e. email address) for the administrator account. Farmer determines the Object ID / SID using `ad user list`.
    /// If you have set the SQL admin credentials, they will be preserved as a hybrid setup.
    [<CustomOperation "entra_id_admin_user">]
    member this.SetEntraIdAuthenticationUser(state: SqlAzureConfig, login, sid) =
        this.SetEntraIdAuthentication(state, login, sid, PrincipalType.User)

    /// Activates Entra ID authentication using the supplied Entra groupname for the administrator account. Farmer determines the Object ID / SID using `ad user list`.
    /// If you have set the SQL admin credentials, they will be preserved as a hybrid setup.
    [<CustomOperation "entra_id_admin_group">]
    member this.SetEntraIdAuthenticationGroup(state: SqlAzureConfig, login, sid) =
        this.SetEntraIdAuthentication(state, login, sid, PrincipalType.Group)

    /// Activates Entra ID authentication using the supplied login named, associated objectId and principal type of the administrator account.
    /// If you have set the SQL admin credentials, they will be preserved as a hybrid setup.
    [<CustomOperation "entra_id_admin">]
    member _.SetEntraIdAuthentication(state: SqlAzureConfig, login, objectId: ObjectId, principalType) =
        let entraCredentials = {
            Login = login
            Sid = objectId
            PrincipalType = principalType
        }

        {
            state with
                Credentials =
                    match state.Credentials with
                    | None
                    | Some(EntraOnly _) -> Some(EntraOnly entraCredentials)
                    | Some(SqlAndEntra(sqlCredentials, _))
                    | Some(SqlOnly sqlCredentials) -> Some(SqlAndEntra(sqlCredentials, entraCredentials))
        }

    interface ITaggable<SqlAzureConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

let sqlServer = SqlServerBuilder()
let sqlDb = SqlDbBuilder()
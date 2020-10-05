[<AutoOpen>]
module Farmer.Builders.SqlAzure

open Farmer
open Farmer.CoreTypes
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
    { Name : ResourceName
      AdministratorCredentials : {| UserName : string; Password : SecureParameter |}
      FirewallRules : {| Name : ResourceName; Start : IPAddress; End : IPAddress |} list
      ElasticPoolSettings :
        {| Name : ResourceName option
           Sku : PoolSku
           PerDbLimits : {| Min: int<DTU>; Max : int<DTU> |} option
           Capacity : int<Mb> option |}
      Databases : SqlAzureDbConfig list
      Tags: Map<string,string>  }
    /// Gets a basic .NET connection string using the administrator username / password.
    member this.ConnectionString (database:SqlAzureDbConfig) =
        let expr =
            ArmExpression.concat [
                ArmExpression.literal
                    (sprintf "Server=tcp:%s.database.windows.net,1433;Initial Catalog=%s;Persist Security Info=False;User ID=%s;Password="
                        this.Name.Value
                        database.Name.Value
                        this.AdministratorCredentials.UserName)
                this.AdministratorCredentials.Password.ArmExpression
                ArmExpression.literal ";MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
            ]
        expr.WithOwner (ResourceId.create(databases, database.Name))
    member this.ConnectionString databaseName =
        this.Databases
        |> List.tryFind(fun db -> db.Name = databaseName)
        |> Option.map this.ConnectionString
        |> Option.defaultWith(fun _ -> failwithf "Unknown database name %s" databaseName.Value)
    member this.ConnectionString databaseName = this.ConnectionString (ResourceName databaseName)

    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            let elasticPoolName =
                this.ElasticPoolSettings.Name
                |> Option.defaultValue (this.Name.Map (sprintf "%s-pool"))

            { ServerName = this.Name
              Location = location
              Credentials =
                {| Username = this.AdministratorCredentials.UserName
                   Password = this.AdministratorCredentials.Password |}
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
    let makeIp = IPAddress.Parse
    member __.Yield _ =
        { Name = ResourceName ""
          AdministratorCredentials = {| UserName = ""; Password = SecureParameter "" |}
          ElasticPoolSettings =
            {| Name = None
               Sku = PoolSku.Basic50
               PerDbLimits = None
               Capacity = None |}
          Databases = []
          FirewallRules = []
          Tags = Map.empty  }
    member __.Run(state) : SqlAzureConfig =
        { state with
            Name =
                if state.Name = ResourceName.Empty then failwith "You must set a server name"
                else state.Name |> Helpers.sanitiseDb |> ResourceName
            AdministratorCredentials =
                if System.String.IsNullOrWhiteSpace state.AdministratorCredentials.UserName then failwithf "You must specify the admin_username for SQL Server instance %s" state.Name.Value
                {| state.AdministratorCredentials with
                    Password = SecureParameter (sprintf "password-for-%s" state.Name.Value) |} }
    /// Sets the name of the SQL server.
    [<CustomOperation "name">]
    member _.ServerName(state:SqlAzureConfig, serverName) = { state with Name = serverName }
    member this.ServerName(state:SqlAzureConfig, serverName:string) = this.ServerName(state, ResourceName serverName)
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
    [<CustomOperation "add_tags">]
    member _.Tags(state:SqlAzureConfig, pairs) =
        { state with
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
    member this.Tag(state:SqlAzureConfig, key, value) = this.Tags(state, [ (key,value) ])
let sqlServer = SqlServerBuilder()
let sqlDb = SqlDbBuilder()
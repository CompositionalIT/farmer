[<AutoOpen>]
module Farmer.Builders.SqlAzure

open Farmer
open Farmer.CoreTypes
open Farmer.Sql
open Farmer.Arm.Sql
open System.Net

type SqlAzureDbConfig =
    { Name : ResourceName
      Sku : DbSku option
      Collation : string
      Encryption : FeatureFlag }

type SqlAzureConfig =
    { Name : ResourceName
      AdministratorCredentials : {| UserName : string; Password : SecureParameter |}
      FirewallRules : {| Name : string; Start : IPAddress; End : IPAddress |} list
      ElasticPoolSettings :
        {| Name : ResourceName option
           Sku : PoolSku
           PerDbLimits : {| Min: int<DTU>; Max : int<DTU> |} option
           Capacity : int<Mb> option |}
      Databases : SqlAzureDbConfig list }
    /// Gets a basic .NET connection string using the administrator username / password.
    member this.ConnectionString (database:SqlAzureDbConfig) =
        concat
            [ literal
                (sprintf "Server=tcp:%s.database.windows.net,1433;Initial Catalog=%s;Persist Security Info=False;User ID=%s;Password="
                    this.Name.Value
                    database.Name.Value
                    this.AdministratorCredentials.UserName)
              this.AdministratorCredentials.Password.AsArmRef
              literal ";MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" ]
    member this.ConnectionString databaseName =
        this.Databases
        |> List.tryFind(fun db -> db.Name = databaseName)
        |> Option.map this.ConnectionString
        |> Option.defaultWith(fun _ -> failwithf "Unknown database name %s" databaseName.Value)
    member this.ConnectionString databaseName = this.ConnectionString (ResourceName databaseName)

    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location resources = [
            let elasticPoolName =
                this.ElasticPoolSettings.Name
                |> Option.defaultValue (this.Name.Map (sprintf "%s-pool"))

            { ServerName = this.Name
              Location = location
              Credentials =
                {| Username = this.AdministratorCredentials.UserName
                   Password = this.AdministratorCredentials.Password |}
              FirewallRules = this.FirewallRules
              ElasticPool =
                if this.Databases |> List.forall(fun db -> db.Sku.IsSome) then None
                else
                    Some {| Name = elasticPoolName
                            Sku = this.ElasticPoolSettings.Sku
                            MaxSizeBytes = this.ElasticPoolSettings.Capacity |> Option.map(fun mb -> int64 mb * 1024L * 1024L)
                            MinMax = this.ElasticPoolSettings.PerDbLimits |> Option.map(fun l -> l.Min, l.Max) |}
              Databases = [
                  for database in this.Databases do
                    {| Name = database.Name
                       Sku =
                        match database.Sku with
                        | Some dbSku -> Standalone dbSku
                        | None -> Pool elasticPoolName
                       Collation = database.Collation
                       TransparentDataEncryption = database.Encryption |}
              ]
            }
        ]

type SqlDbBuilder() =
    member _.Yield _ =
        { Name = ResourceName ""
          Collation = "SQL_Latin1_General_CP1_CI_AS"
          Sku = None
          Encryption = Disabled }
    /// Sets the name of the database.
    [<CustomOperation "name">]
    member _.DbName(state:SqlAzureDbConfig, name) = { state with Name = name }
    member this.DbName(state:SqlAzureDbConfig, name:string) = this.DbName(state, ResourceName name)
    // Sets the sku of the database
    [<CustomOperation "sku">]
    member _.DbSku(state:SqlAzureDbConfig, sku:DbSku) = { state with Sku = Some sku }
    // Sets the collation of the database.
    [<CustomOperation "collation">]
    member _.DbCollation(state:SqlAzureDbConfig, collation:string) = { state with Collation = collation }
    // Enables encryption of the database.
    [<CustomOperation "use_encryption">]
    member _.UseEncryption(state:SqlAzureDbConfig) = { state with Encryption = Enabled }
    // Adds a custom firewall rule given a name, start and end IP address range.
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
          FirewallRules = [] }
    member __.Run(state) =
        { state with
            Name =
                if state.Name = ResourceName.Empty then failwith "You must set a server name"
                else state.Name |> Helpers.sanitiseDb |> ResourceName
            AdministratorCredentials =
                if System.String.IsNullOrWhiteSpace state.AdministratorCredentials.UserName then failwith "You must specify an admin_username."
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
    member __.AddFirewallWall(state:SqlAzureConfig, name, startRange, endRange) =
        { state with
            FirewallRules =
                {| Name = name
                   Start = makeIp startRange
                   End = makeIp endRange |}
                :: state.FirewallRules }
    /// Adds a firewall rule that enables access to other Azure services.
    [<CustomOperation "enable_azure_firewall">]
    member this.UseAzureFirewall(state:SqlAzureConfig) =
        this.AddFirewallWall(state, "Allow Azure services", "0.0.0.0", "0.0.0.0")
    /// Sets the admin username of the server (note: the password is supplied as a securestring parameter to the generated ARM template).
    [<CustomOperation "admin_username">]
    member __.AdminUsername(state:SqlAzureConfig, username) =
        { state with
            AdministratorCredentials =
                {| state.AdministratorCredentials with
                    UserName = username |} }
        
let sqlServer = SqlServerBuilder()
let sqlDb = SqlDbBuilder()
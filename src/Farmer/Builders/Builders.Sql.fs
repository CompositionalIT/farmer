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
      ElasticPoolSku : PoolSku
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

    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location resources = [
            let elasticPoolName = this.Name.Map (sprintf "%s-pool")
            { ServerName = this.Name
              Location = location
              Credentials =
                {| Username = this.AdministratorCredentials.UserName
                   Password = this.AdministratorCredentials.Password |}
              FirewallRules = this.FirewallRules
              ElasticPool =
                if this.Databases |> List.forall(fun db -> db.Sku.IsSome) then None
                else Some {| Name = elasticPoolName; Sku = this.ElasticPoolSku |}
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

type SqlBuilder() =
    let makeIp = IPAddress.Parse
    member __.Yield _ =
        { Name = ResourceName ""
          AdministratorCredentials = {| UserName = ""; Password = SecureParameter "" |}
          ElasticPoolSku = PoolSku.Basic50
          Databases = []
          FirewallRules = [] }
    member __.Run(state) =
        { state with
            Name = state.Name |> Helpers.sanitiseDb |> ResourceName
            AdministratorCredentials =
                if System.String.IsNullOrWhiteSpace state.AdministratorCredentials.UserName then failwith "You must specify an admin_username."
                {| state.AdministratorCredentials with
                    Password = SecureParameter (sprintf "password-for-%s" state.Name.Value) |} }
    /// Sets the name of the SQL server.
    [<CustomOperation "name">]
    member __.ServerName(state:SqlAzureConfig, serverName) = { state with Name = serverName }
    member this.ServerName(state:SqlAzureConfig, serverName:string) = this.ServerName(state, ResourceName serverName)
    /// Sets the sku of the server, to be shared on all databases that do not have an explicit sku set.
    [<CustomOperation "elastic_pool_sku">]
    member __.Sku(state:SqlAzureConfig, sku) = { state with ElasticPoolSku = sku }
    [<CustomOperation "add_databases">]
    member _.AddDatabases(state:SqlAzureConfig, databases) = { state with Databases = state.Databases @ databases }
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

open WebApp
type WebAppBuilder with
    member this.DependsOn(state:WebAppConfig, sqlDb:SqlAzureConfig) =
        this.DependsOn(state, sqlDb.Name)
type FunctionsBuilder with
    member this.DependsOn(state:FunctionsConfig, sqlDb:SqlAzureConfig) =
        this.DependsOn(state, sqlDb.Name)

let sqlServer = SqlBuilder()
let sqlDb = SqlDbBuilder()

[<AutoOpen>]
module Farmer.Resources.SqlAzure

open Farmer
open Farmer.Models

type SqlSku = Free | Basic | Standard of string | Premium of string

module Sku =
    let ``Free`` = Free
    let ``Basic`` = Basic
    let ``S0`` = Standard "S0"
    let ``S1`` = Standard "S1"
    let ``S2`` = Standard "S2"
    let ``S3`` = Standard "S3"
    let ``S4`` = Standard "S4"
    let ``S6`` = Standard "S6"
    let ``S7`` = Standard "S7"
    let ``S9`` = Standard "S9"
    let ``S12`` =Standard "S12"
    let ``P1`` = Premium "P1"
    let ``P2`` = Premium "P2"
    let ``P4`` = Premium "P4"
    let ``P6`` = Premium "P6"
    let ``P11`` = Premium "P11"
    let ``P15`` = Premium "P15"

type SqlAzureConfig =
    { ServerName : ResourceRef
      AdministratorCredentials : {| UserName : string; Password : SecureParameter |}
      Name : ResourceName
      DbEdition : SqlSku
      DbCollation : string
      Encryption : FeatureFlag
      FirewallRules : {| Name : string; Start : System.Net.IPAddress; End : System.Net.IPAddress |} list }
    /// Gets the ARM expression path to the FQDN of this VM.
    member this.FullyQualifiedDomainName =
        sprintf "reference(concat('Microsoft.Sql/servers/', variables('%s'))).fullyQualifiedDomainName" this.ServerName.ResourceName.Value
        |> ArmExpression
    /// Gets a basic .NET connection string using the administrator username / password.
    member this.ConnectionString =
        concat
            [ literal
                (sprintf "Server=tcp:%s.database.windows.net,1433;Initial Catalog=%s;Persist Security Info=False;User ID=%s;Password="
                    this.ServerName.ResourceName.Value
                    this.Name.Value
                    this.AdministratorCredentials.UserName)
              this.AdministratorCredentials.Password.AsArmRef
              literal ";MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" ]
    member this.Server =
        this.ServerName.ResourceName

type SqlBuilder() =
    let makeIp = System.Net.IPAddress.Parse
    member __.Yield _ =
        { ServerName = AutomaticPlaceholder
          AdministratorCredentials = {| UserName = ""; Password = SecureParameter "" |}
          Name = ResourceName ""
          DbEdition = Free
          DbCollation = "SQL_Latin1_General_CP1_CI_AS"
          Encryption = Disabled
          FirewallRules = [] }
    member __.Run(state) =
        { state with
            ServerName =
                match state.ServerName with
                | External x -> External(x |> Helpers.santitiseDb |> ResourceName)
                | AutomaticallyCreated x -> AutomaticallyCreated(x |> Helpers.santitiseDb |> ResourceName)
                | AutomaticPlaceholder -> failwith "You must specific an server name, or link to an existing server."
            Name = state.Name |> Helpers.santitiseDb |> ResourceName
            AdministratorCredentials =
                match state.ServerName with
                | External _ -> state.AdministratorCredentials
                | AutomaticallyCreated _
                | AutomaticPlaceholder ->
                    if System.String.IsNullOrWhiteSpace state.AdministratorCredentials.UserName then failwith "You must specific an admin_username."
                    {| state.AdministratorCredentials with
                        Password = SecureParameter (sprintf "password-for-%s" state.ServerName.ResourceName.Value) |} }
    [<CustomOperation "server_name">]
    /// Sets the name of the SQL server.
    member __.ServerName(state:SqlAzureConfig, serverName) = { state with ServerName = AutomaticallyCreated serverName }
    member this.ServerName(state:SqlAzureConfig, serverName:string) = this.ServerName(state, ResourceName serverName)
    [<CustomOperation "link_to_server">]
    /// Sets the name of the SQL server.
    member __.LinkToServerName(state:SqlAzureConfig, serverName) = { state with ServerName = External serverName }
    member this.LinkToServerName(state:SqlAzureConfig, serverName) = this.LinkToServerName(state, ResourceName serverName)
    /// Sets the name of the database.
    [<CustomOperation "name">]
    member __.Name(state:SqlAzureConfig, name) = { state with Name = name }
    member this.Name(state:SqlAzureConfig, name:string) = this.Name(state, ResourceName name)
    /// Sets the sku of the database.
    [<CustomOperation "sku">]
    member __.DatabaseEdition(state:SqlAzureConfig, edition:SqlSku) = { state with DbEdition = edition }
    /// Sets the collation of the database.
    [<CustomOperation "collation">]
    member __.Collation(state:SqlAzureConfig, collation:string) = { state with DbCollation = collation }
    /// Enables encryption of the database.
    [<CustomOperation "use_encryption">]
    member __.Encryption(state:SqlAzureConfig) = { state with Encryption = Enabled }
    /// Adds a custom firewall rule given a name, start and end IP address range.
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
        this.DependsOn(state, sqlDb.ServerName.ResourceName)
type FunctionsBuilder with
    member this.DependsOn(state:FunctionsConfig, sqlDb:SqlAzureConfig) =
        this.DependsOn(state, sqlDb.ServerName.ResourceName)

module Converters =
    let sql location (existingServers:SqlAzure list) (sql:SqlAzureConfig) =
        let database =
            {| Name = sql.Name
               Edition =
                 match sql.DbEdition with
                 | SqlSku.Basic -> "Basic"
                 | SqlSku.Free -> "Free"
                 | SqlSku.Standard _ -> "Standard"
                 | SqlSku.Premium _ -> "Premium"
               Objective =
                 match sql.DbEdition with
                 | SqlSku.Basic -> "Basic"
                 | SqlSku.Free -> "Free"
                 | SqlSku.Standard s -> s
                 | SqlSku.Premium p -> p
               Collation = sql.DbCollation
               TransparentDataEncryption = sql.Encryption |}

        match sql.ServerName with
        | AutomaticallyCreated serverName ->
            { ServerName = serverName
              Location = location
              Credentials =
                {| Username = sql.AdministratorCredentials.UserName
                   Password = sql.AdministratorCredentials.Password |}
              FirewallRules = sql.FirewallRules
              Databases = [ database ]
            }
            |> NewResource
        | External resourceName ->
            existingServers
            |> List.tryFind(fun g -> g.ServerName = resourceName)
            |> Option.map(fun server -> MergedResource(server, { server with Databases = database :: server.Databases }))
            |> Option.defaultValue (CouldNotLocate resourceName)
        | AutomaticPlaceholder ->
            NotSet
    module Outputters =
        let sqlAzure (server:SqlAzure) = {|
            ``type`` = "Microsoft.Sql/servers"
            name = server.ServerName.Value
            apiVersion = "2014-04-01-preview"
            location = server.Location.Value
            tags = {| displayName = server.ServerName.Value |}
            properties =
                {| administratorLogin = server.Credentials.Username
                   administratorLoginPassword = server.Credentials.Password.AsArmRef.Eval()
                   version = "12.0" |}
            resources = [
                for database in server.Databases do
                    box
                        {| ``type`` = "databases"
                           name = database.Name.Value
                           apiVersion = "2015-01-01"
                           location = server.Location.Value
                           tags = {| displayName = database.Name.Value |}
                           properties =
                            {| edition = database.Edition
                               collation = database.Collation
                               requestedServiceObjectiveName = database.Objective |}
                           dependsOn = [
                               server.ServerName.Value
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
                for rule in server.FirewallRules do
                    box
                        {| ``type`` = "firewallrules"
                           name = rule.Name
                           apiVersion = "2014-04-01"
                           location = server.Location.Value
                           properties =
                            {| endIpAddress = string rule.Start
                               startIpAddress = string rule.End |}
                           dependsOn = [ server.ServerName.Value ]
                        |}
            ]
        |}


type ArmBuilder.ArmBuilder with
    member __.AddResource(state:ArmConfig, config:SqlAzureConfig) =
        state.AddOrMergeResource (Converters.sql state.Location) config (function SqlServer config -> Some config | _ -> None) SqlServer
    member this.AddResources (state, configs) = addResources<SqlAzureConfig> this.AddResource state configs

let sql = SqlBuilder()
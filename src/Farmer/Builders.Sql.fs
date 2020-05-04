[<AutoOpen>]
module Farmer.Resources.SqlAzure

open Farmer
open Arm.Sql
open System.Net
[<RequireQualifiedAccess>]
type SqlSku = Free | Basic | Standard of string | Premium of string

module Sku =
    let ``Free`` = SqlSku.Free
    let ``Basic`` = SqlSku.Basic
    let ``S0`` = SqlSku.Standard "S0"
    let ``S1`` = SqlSku.Standard "S1"
    let ``S2`` = SqlSku.Standard "S2"
    let ``S3`` = SqlSku.Standard "S3"
    let ``S4`` = SqlSku.Standard "S4"
    let ``S6`` = SqlSku.Standard "S6"
    let ``S7`` = SqlSku.Standard "S7"
    let ``S9`` = SqlSku.Standard "S9"
    let ``S12`` =SqlSku.Standard "S12"
    let ``P1`` = SqlSku.Premium "P1"
    let ``P2`` = SqlSku.Premium "P2"
    let ``P4`` = SqlSku.Premium "P4"
    let ``P6`` = SqlSku.Premium "P6"
    let ``P11`` = SqlSku.Premium "P11"
    let ``P15`` = SqlSku.Premium "P15"

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
    interface IResourceBuilder with
        member this.BuildResources location resources = [
            let database =
                {| Name = this.Name
                   Edition =
                     match this.DbEdition with
                     | SqlSku.Basic -> "Basic"
                     | SqlSku.Free -> "Free"
                     | SqlSku.Standard _ -> "Standard"
                     | SqlSku.Premium _ -> "Premium"
                   Objective =
                     match this.DbEdition with
                     | SqlSku.Basic -> "Basic"
                     | SqlSku.Free -> "Free"
                     | SqlSku.Standard s -> s
                     | SqlSku.Premium p -> p
                   Collation = this.DbCollation
                   TransparentDataEncryption = this.Encryption |}

            let server =
                match this.ServerName with
                | AutomaticallyCreated serverName ->
                    { ServerName = serverName
                      Location = location
                      Credentials =
                        {| Username = this.AdministratorCredentials.UserName
                           Password = this.AdministratorCredentials.Password |}
                      FirewallRules = this.FirewallRules
                      Databases = [ database ] }
                | External serverName ->
                    resources
                    |> Helpers.tryMergeResource serverName (fun server -> { server with Databases = database :: server.Databases })
                | AutomaticPlaceholder ->
                    failwith "SQL Server Name has not been set."
            server
        ]

type SqlBuilder() =
    let makeIp = IPAddress.Parse
    member __.Yield _ =
        { ServerName = AutomaticPlaceholder
          AdministratorCredentials = {| UserName = ""; Password = SecureParameter "" |}
          Name = ResourceName ""
          DbEdition = SqlSku.Free
          DbCollation = "SQL_Latin1_General_CP1_CI_AS"
          Encryption = Disabled
          FirewallRules = [] }
    member __.Run(state) =
        { state with
            ServerName =
                match state.ServerName with
                | External x -> External(x |> Helpers.santitiseDb |> ResourceName)
                | AutomaticallyCreated x -> AutomaticallyCreated(x |> Helpers.santitiseDb |> ResourceName)
                | AutomaticPlaceholder -> failwith "You must specific a server name, or link to an existing server."
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

let sql = SqlBuilder()
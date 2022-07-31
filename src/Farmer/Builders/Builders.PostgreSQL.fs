[<AutoOpen>]
module Farmer.Builders.PostgreSQLAzure

open System
open System.Net

open Farmer
open Farmer.PostgreSQL
open Arm.DBforPostgreSQL
open Servers


type PostgreSQLDbConfig =
    {
        Name: ResourceName
        DbCollation: string option
        DbCharset: string option
    }


type PostgreSQLConfig =
    {
        Name: ResourceName
        AdministratorCredentials: {| UserName: string
                                     Password: SecureParameter |}
        Version: Version
        GeoRedundantBackup: bool
        StorageAutogrow: bool
        BackupRetention: int<Days>
        StorageSize: int<Gb>
        Capacity: int<VCores>
        Tier: Sku
        Databases: PostgreSQLDbConfig list
        FirewallRules: {| Name: ResourceName
                          Start: IPAddress
                          End: IPAddress |} list
        VirtualNetworkRules: {| Name: ResourceName
                                VirtualNetworkSubnetId: ResourceId |} list
        Tags: Map<string, string>
    }

    interface IBuilder with
        member this.ResourceId = databases.resourceId this.Name

        member this.BuildResources location =
            [
                {
                    Name = this.Name
                    Location = location
                    Credentials =
                        {|
                            Username = this.AdministratorCredentials.UserName
                            Password = this.AdministratorCredentials.Password
                        |}
                    Version = this.Version
                    StorageSize = this.StorageSize * 1024<Mb> / 1<Gb>
                    Capacity = this.Capacity
                    Tier = this.Tier
                    Family = PostgreSQLFamily.Gen5
                    GeoRedundantBackup = FeatureFlag.ofBool this.GeoRedundantBackup
                    StorageAutoGrow = FeatureFlag.ofBool this.StorageAutogrow
                    BackupRetention = this.BackupRetention
                    Tags = this.Tags
                }

                for database in this.Databases do
                    {
                        Name = database.Name
                        Server = this.Name
                        Collation = database.DbCollation |> Option.defaultValue "English_United States.1252"
                        Charset = database.DbCharset |> Option.defaultValue "UTF8"
                    }

                for rule in this.FirewallRules do
                    {
                        Name = rule.Name
                        Start = rule.Start
                        End = rule.End
                        Server = this.Name
                        Location = location
                    }

                for rule in this.VirtualNetworkRules do
                    {
                        Name = rule.Name
                        VirtualNetworkSubnetId = rule.VirtualNetworkSubnetId
                        Server = this.Name
                        Location = location
                    }
            ]


[<AutoOpen>]
module private Helpers =
    let isAsciiDigit (c: Char) = (c >= '0' && c <= '9')
    let isAsciiLowercase (c: char) = (c >= 'a' && c <= 'z')
    let isAsciiUppercase (c: char) = (c >= 'A' && c <= 'Z')

    let isAsciiLetter (c: Char) =
        isAsciiLowercase c || isAsciiUppercase c

    let isAsciiLetterOrDigit (c: Char) = isAsciiLetter c || isAsciiDigit c

    let isLegalServernameChar c =
        isAsciiLowercase c || isAsciiDigit c || c = '-'

[<RequireQualifiedAccess>]
module Validate =
    let reservedUsernames =
        [
            "azure_pg_admin"
            "admin"
            "root"
            "azure_superuser"
            "administrator"
            "root"
            "guest"
        ]

    let username (paramName: string) (candidate: string) =
        if String.IsNullOrWhiteSpace candidate then
            raiseFarmer $"{paramName} can not be null, empty, or blank"

        if candidate.Length > 63 then
            raiseFarmer $"{paramName} must have a length between 1 and 63, was {candidate.Length}"

        if isAsciiDigit candidate.[0] then
            raiseFarmer $"{paramName} can not begin with a digit"

        if not (Seq.forall isAsciiLetterOrDigit candidate) then
            raiseFarmer $"{paramName} can only consist of ASCII letters and digits, was '{candidate}'"

        if candidate.StartsWith "pg_" then
            raiseFarmer $"{paramName} must not start with 'pg_'"

        if Seq.contains candidate reservedUsernames then
            raiseFarmer $"{paramName} can not be one of %A{reservedUsernames}"

    let servername (name: string) =
        if String.IsNullOrWhiteSpace name then
            raiseFarmer "Server name can not be null, empty, or blank"

        if name.Length > 63 || name.Length < 3 then
            raiseFarmer $"Server name must have a length between 3 and 63, was {name.Length}"

        if name.[0] = '-' || name.[name.Length - 1] = '-' then
            raiseFarmer "Server name must not start or end with a hyphen ('-')"

        if isAsciiDigit name.[0] then
            raiseFarmer "Server name must not start with a digit"

        if not (Seq.forall isLegalServernameChar name) then
            raiseFarmer $"Server name can only consist of ASCII lowercase letters, digits, or hyphens. Was '{name}'"

    let dbname (name: string) =
        if String.IsNullOrWhiteSpace name then
            raiseFarmer "Database name can not be null, empty, or blank"

        if name.Length > 63 then
            raiseFarmer $"Database name must have a length between 1 and 63, was {name.Length}"

        if isAsciiDigit name.[0] then
            raiseFarmer "Server name must not start with a digit"

    let minBackupRetention = 7<Days>
    let maxBackupRetention = 35<Days>

    let backupRetention (days: int<Days>) =
        if days < minBackupRetention || days > maxBackupRetention then
            raiseFarmer
                $"Backup retention must be between {minBackupRetention} and {maxBackupRetention} days, but was {days}"

    let minStorageSize = 5<Gb>
    let maxStorageSize = 1024<Gb>

    let storageSize (size: int<Gb>) =
        if size < minStorageSize || size > maxStorageSize then
            raiseFarmer $"Storage space must between {minStorageSize} and {maxStorageSize} GB, but was {size}"

    let minCapacity = 1<VCores>
    let maxCapacity = 64<VCores>

    let capacity (capacity: int<VCores>) =
        if capacity < minCapacity || capacity > maxCapacity then
            raiseFarmer $"Capacity must be between {minCapacity} and {maxCapacity} cores, was {capacity}"

        let c = int capacity

        if ((c &&& (c - 1)) <> 0) then
            raiseFarmer $"Capacity must be a power of two, was {capacity}"

type PostgreSQLDbBuilder() =
    member _.Yield _ : PostgreSQLDbConfig =
        {
            Name = ResourceName ""
            DbCharset = None
            DbCollation = None
        }

    member _.Run(state: PostgreSQLDbConfig) =
        if state.Name = ResourceName.Empty then
            raiseFarmer "You must set a database name"

        state

    [<CustomOperation "name">]
    member _.SetName(state: PostgreSQLDbConfig, name: ResourceName) =
        Validate.dbname name.Value
        { state with Name = name }

    member this.SetName(state: PostgreSQLDbConfig, name: string) = this.SetName(state, ResourceName name)

    [<CustomOperation "collation">]
    member _.SetCollation(state: PostgreSQLDbConfig, collation: string) =
        if String.IsNullOrWhiteSpace collation then
            raiseFarmer "collation must have a value"

        { state with
            DbCollation = Some collation
        }

    [<CustomOperation "charset">]
    member _.SetCharset(state: PostgreSQLDbConfig, charSet: string) =
        if String.IsNullOrWhiteSpace charSet then
            raiseFarmer "charSet must have a value"

        { state with DbCharset = Some charSet }

let postgreSQLDb = PostgreSQLDbBuilder()

type PostgreSQLBuilder() =
    member _.Yield _ : PostgreSQLConfig =
        {
            Name = ResourceName ""
            AdministratorCredentials =
                {|
                    UserName = ""
                    Password = SecureParameter ""
                |}
            Version = VS_11
            GeoRedundantBackup = false
            StorageAutogrow = true
            BackupRetention = Validate.minBackupRetention
            StorageSize = Validate.minStorageSize
            Capacity = 2<VCores>
            Tier = Basic
            Databases = []
            FirewallRules = []
            VirtualNetworkRules = []
            Tags = Map.empty
        }

    member _.Run state : PostgreSQLConfig =
        state.Name.Value |> Validate.servername

        state.AdministratorCredentials.UserName
        |> Validate.username "AdministratorCredentials.UserName"

        { state with
            AdministratorCredentials =
                {| state.AdministratorCredentials with
                    Password = SecureParameter $"password-for-{state.Name.Value}"
                |}
        }

    /// Sets the name of the PostgreSQL server
    [<CustomOperation "name">]
    member _.ServerName(state: PostgreSQLConfig, serverName) =
        let (ResourceName n) = serverName
        Validate.servername n
        { state with Name = serverName }

    member this.ServerName(state: PostgreSQLConfig, serverName) =
        this.ServerName(state, ResourceName serverName)

    /// Sets the name of the admin user
    [<CustomOperation "admin_username">]
    member _.AdminUsername(state: PostgreSQLConfig, adminUsername: string) =
        Validate.username "adminUserName" adminUsername

        { state with
            Databases = List.rev state.Databases
            AdministratorCredentials =
                {| state.AdministratorCredentials with
                    UserName = adminUsername
                |}
        }

    /// Sets geo-redundant backup
    [<CustomOperation "geo_redundant_backup">]
    member _.SetGeoRedundantBackup(state: PostgreSQLConfig, enabled: bool) =
        { state with
            GeoRedundantBackup = enabled
        }

    /// Enables geo-redundant backup
    [<CustomOperation "enable_geo_redundant_backup">]
    member this.EnableGeoRedundantBackup(state: PostgreSQLConfig) = this.SetGeoRedundantBackup(state, true)

    /// Disables geo-redundant backup
    [<CustomOperation "disable_geo_redundant_backup">]
    member this.DisableGeoRedundantBackup(state: PostgreSQLConfig) =
        this.SetGeoRedundantBackup(state, false)

    /// Sets storage autogrow
    [<CustomOperation "storage_autogrow">]
    member _.SetStorageAutogrow(state: PostgreSQLConfig, enabled: bool) =
        { state with StorageAutogrow = enabled }

    /// Enables storage autogrow
    [<CustomOperation "enable_storage_autogrow">]
    member this.EnableStorageAutogrow(state: PostgreSQLConfig) = this.SetStorageAutogrow(state, true)

    /// Disables storage autogrow
    [<CustomOperation "disable_storage_autogrow">]
    member this.DisableStorageAutogrow(state: PostgreSQLConfig) = this.SetStorageAutogrow(state, false)

    /// sets storage size in MBs
    [<CustomOperation "storage_size">]
    member _.SetStorageSizeInMBs(state: PostgreSQLConfig, size: int<Gb>) =
        Validate.storageSize size
        { state with StorageSize = size }

    /// sets the backup retention in days
    [<CustomOperation "backup_retention">]
    member _.SetBackupRetention(state: PostgreSQLConfig, retention: int<Days>) =
        Validate.backupRetention retention

        { state with
            BackupRetention = retention
        }

    /// Sets the PostgreSQl server version
    [<CustomOperation "server_version">]
    member _.SetServerVersion(state: PostgreSQLConfig, version: Version) = { state with Version = version }

    /// Sets capacity
    [<CustomOperation "capacity">]
    member _.SetCapacity(state: PostgreSQLConfig, capacity: int<VCores>) =
        Validate.capacity capacity
        { state with Capacity = capacity }

    /// Sets tier
    [<CustomOperation "tier">]
    member _.SetTier(state: PostgreSQLConfig, tier: Sku) = { state with Tier = tier }

    /// Adds a new database to the server, either by specifying the name of the database or providing a PostgreSQLDbConfig
    [<CustomOperation "add_database">]
    member _.AddDatabase(state: PostgreSQLConfig, database) =
        { state with
            Databases = database :: state.Databases
        }

    member this.AddDatabase(state: PostgreSQLConfig, dbName: string) =
        let db = postgreSQLDb { name dbName }
        this.AddDatabase(state, db)

    /// Adds a custom firewall rule given a name, start and end IP address range.
    [<CustomOperation "add_firewall_rule">]
    member _.AddFirewallWall(state: PostgreSQLConfig, name, startRange: string, endRange: string) =
        { state with
            FirewallRules =
                {|
                    Name = ResourceName name
                    Start = IPAddress.Parse startRange
                    End = IPAddress.Parse endRange
                |}
                :: state.FirewallRules
        }

    /// Adds a custom firewall rules given a name, start and end IP address range.
    [<CustomOperation "add_firewall_rules">]
    member _.AddFirewallRules(state: PostgreSQLConfig, listOfRules: (string * string * string) list) =
        let newRules =
            listOfRules
            |> List.map (fun (name, startRange, endRange) ->
                {|
                    Name = ResourceName name
                    Start = IPAddress.Parse startRange
                    End = IPAddress.Parse endRange
                |})

        { state with
            FirewallRules = newRules @ state.FirewallRules
        }

    /// Adds a firewall rule that enables access to other Azure services.
    [<CustomOperation "enable_azure_firewall">]
    member this.EnableAzureFirewall(state: PostgreSQLConfig) =
        this.AddFirewallWall(state, "allow-azure-services", "0.0.0.0", "0.0.0.0")

    interface ITaggable<PostgreSQLConfig> with
        member _.Add state tags =
            { state with
                Tags = state.Tags |> Map.merge tags
            }

    /// Adds a custom vnet rule given a name and a virtualNetworkSubnetId.
    [<CustomOperation "add_vnet_rule">]
    member _.AddVnetRule(state: PostgreSQLConfig, name, virtualNetworkSubnetId: ResourceId) =
        { state with
            VirtualNetworkRules =
                {|
                    Name = ResourceName name
                    VirtualNetworkSubnetId = virtualNetworkSubnetId
                |}
                :: state.VirtualNetworkRules
        }

    /// Adds a custom firewall rules given a name and a virtualNetworkSubnetId.
    [<CustomOperation "add_vnet_rules">]
    member _.AddVnetRules(state: PostgreSQLConfig, listOfRules: (string * ResourceId) list) =
        let newRules =
            listOfRules
            |> List.map (fun (name, virtualNetworkSubnetId) ->
                {|
                    Name = ResourceName name
                    VirtualNetworkSubnetId = virtualNetworkSubnetId
                |})

        { state with
            VirtualNetworkRules = newRules @ state.VirtualNetworkRules
        }

let postgreSQL = PostgreSQLBuilder()

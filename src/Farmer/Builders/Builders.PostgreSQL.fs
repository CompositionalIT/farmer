[<AutoOpen>]
module Farmer.Builders.PostgreSQLAzure

open System
open System.Net

open Farmer
open Farmer.CoreTypes
open Farmer.PostgreSQL
open Arm.DBforPostgreSQL
open Servers


type PostgreSQLDbBuilderConfig =
    { Name : ResourceName 
      DbCollation : string option
      DbCharset : string option }


type PostgreSQLServerBuilderConfig =
    { Name : ResourceName
      AdministratorCredentials : {| UserName : string; Password : SecureParameter |}
      Version : Version
      GeoRedundantBackup : bool
      StorageAutogrow : bool
      BackupRetention : int<Days>
      StorageSize : int<Gb>
      Capacity : int<VCores>
      Tier : Sku
      Databases : PostgreSQLDbBuilderConfig list
      FirewallRules : {| Name : ResourceName; Start : IPAddress; End : IPAddress |} list
      Tags: Map<string,string>  }

    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              Credentials = {| Username = this.AdministratorCredentials.UserName
                               Password = this.AdministratorCredentials.Password |}
              Version = this.Version
              StorageSize = this.StorageSize * 1024<Mb> / 1<Gb>
              Capacity = this.Capacity
              Tier = this.Tier
              Family = PostgreSQLFamily.Gen5
              GeoRedundantBackup = FeatureFlag.ofBool this.GeoRedundantBackup
              StorageAutoGrow = FeatureFlag.ofBool this.StorageAutogrow
              BackupRetention = this.BackupRetention
              Tags = this.Tags  }

            for database in this.Databases do            
                { Name = database.Name
                  Server = this.Name
                  Collation = database.DbCollation |> Option.defaultValue "English_United States.1252"
                  Charset = database.DbCharset |> Option.defaultValue "UTF8" }

            for rule in this.FirewallRules do
                { Name = rule.Name
                  Start = rule.Start
                  End = rule.End
                  Server = this.Name
                  Location = location }
        ]


[<AutoOpen>]
module private Helpers =
    let isAsciiDigit (c : Char) = (c >= '0' && c <= '9')
    let isAsciiLowercase (c : char) = (c >= 'a' && c <= 'z')
    let isAsciiUppercase (c : char) = (c >= 'A' && c <= 'Z')
    let isAsciiLetter (c : Char) = isAsciiLowercase c || isAsciiUppercase c
    let isAsciiLetterOrDigit (c : Char) = isAsciiLetter c || isAsciiDigit c

    let isLegalServernameChar c = isAsciiLowercase c || isAsciiDigit c || c = '-'

[<RequireQualifiedAccess>]
module Validate =
    let reservedUsernames = [
        "azure_pg_admin"; "admin"; "root"; "azure_superuser"; "administrator"; "root"; "guest"
    ]

    let username (paramName : string) (candidate : string) =
        if String.IsNullOrWhiteSpace candidate then
            failwithf "%s can not be null, empty, or blank" paramName
        if candidate.Length > 63 then
            failwithf "%s must have a length between 1 and 63, was %d" paramName candidate.Length
        if isAsciiDigit candidate.[0] then
            failwithf "%s can not begin with a digit" paramName
        if not (Seq.forall isAsciiLetterOrDigit candidate) then
            failwithf "%s can only consist of ASCII letters and digits, was '%s'" paramName candidate
        if candidate.StartsWith "pg_" then
            failwithf "%s must not start with 'pg_'" paramName
        if Seq.contains candidate reservedUsernames then
            failwithf "%s can not be one of %A" paramName reservedUsernames

    let servername (name : string) =
        if String.IsNullOrWhiteSpace name then
            failwith "Server name can not be null, empty, or blank"
        if name.Length > 63 || name.Length < 3 then
            failwithf "Server name must have a length between 3 and 63, was %d" name.Length
        if name.[0] = '-' || name.[name.Length-1] = '-' then
            failwith "Server name must not start or end with a hyphen ('-')"
        if isAsciiDigit name.[0] then
            failwith "Server name must not start with a digit"
        if not (Seq.forall isLegalServernameChar name) then
            failwithf "Server name can only consist of ASCII lowercase letters, digits, or hyphens. Was '%s'" name

    let dbname (name : string) =
        if String.IsNullOrWhiteSpace name then
            failwith "Database name can not be null, empty, or blank"
        if name.Length > 63 then
            failwithf "Database name must have a length between 1 and 63, was %d" name.Length
        if isAsciiDigit name.[0] then
            failwith "Server name must not start with a digit"

    let minBackupRetention = 7<Days>
    let maxBackupRetention = 35<Days>
    let backupRetention (days: int<Days>) =
        if days < minBackupRetention || days > maxBackupRetention then
            failwithf "Backup retention must be between %d and %d days, was %d"
                minBackupRetention maxBackupRetention days

    let minStorageSize = 5<Gb>
    let maxStorageSize = 1024<Gb>
    let storageSize (size : int<Gb>) =
        if size < minStorageSize || size > maxStorageSize then
            failwithf "Storage space must between %d and %d GB, was %d"
                minStorageSize maxStorageSize size

    let minCapacity = 1<VCores>
    let maxCapacity = 64<VCores>
    let capacity (capacity:int<VCores>) =
        if capacity < minCapacity || capacity > maxCapacity then
            failwithf "Capacity must be between %d and %d cores, was %d"
                minCapacity maxCapacity capacity
        let c = int capacity
        if ((c &&& (c - 1)) <> 0) then
            failwithf "Capacity must be a power of two, was %d" capacity


type PostgreSQLDbBuilder() =
    member _this.Yield _ : PostgreSQLDbBuilderConfig =
        { Name = ResourceName ""
          DbCharset = None
          DbCollation = None }

    member _this.Run (state:PostgreSQLDbBuilderConfig) =
        if state.Name = ResourceName.Empty then failwith "You must set a database name"
        state

    [<CustomOperation("name")>]
    member _this.SetName(state: PostgreSQLDbBuilderConfig, name:ResourceName) =
        Validate.dbname name.Value
        { state with Name = name }
    member this.SetName(state: PostgreSQLDbBuilderConfig, name: string) =
        this.SetName(state, ResourceName name)

    [<CustomOperation("collation")>]
    member _this.SetCollation(state: PostgreSQLDbBuilderConfig, collation:string) =
        if String.IsNullOrWhiteSpace collation then failwith "collation must have a value"
        { state with DbCollation = Some collation }

    [<CustomOperation("charset")>]
    member _this.SetCharset(state: PostgreSQLDbBuilderConfig, charSet:string) =
        if String.IsNullOrWhiteSpace charSet then failwith "charSet must have a value"
        { state with DbCharset = Some charSet }

let postgreSQLDb = PostgreSQLDbBuilder()

type PostgreSQLBuilder() =
    member _this.Yield _ : PostgreSQLServerBuilderConfig =
        { Name = ResourceName ""
          AdministratorCredentials = {| UserName = ""; Password = SecureParameter "" |}
          Version = VS_11
          GeoRedundantBackup = false
          StorageAutogrow = true
          BackupRetention = Validate.minBackupRetention
          StorageSize = Validate.minStorageSize
          Capacity = 2<VCores>
          Tier = Basic
          Databases = []
          FirewallRules = []
          Tags = Map.empty  }

    member _this.Run state : PostgreSQLServerBuilderConfig =
        state.Name.Value |> Validate.servername 
        state.AdministratorCredentials.UserName |> Validate.username "AdministratorCredentials.UserName"
        { state with 
            AdministratorCredentials = 
                {| state.AdministratorCredentials with 
                    Password = sprintf "password-for-%s" state.Name.Value |> SecureParameter |}  }

    /// Sets the name of the PostgreSQL server
    [<CustomOperation "name">]
    member _this.ServerName(state:PostgreSQLServerBuilderConfig, serverName) =
        let (ResourceName n) = serverName
        Validate.servername n
        { state with Name = serverName }
    member this.ServerName(state:PostgreSQLServerBuilderConfig, serverName) =
        this.ServerName(state, ResourceName serverName)

    /// Sets the name of the admin user
    [<CustomOperation "admin_username">]
    member _this.AdminUsername(state:PostgreSQLServerBuilderConfig, adminUsername:string) =
        Validate.username "adminUserName" adminUsername
        { state with 
            Databases = List.rev state.Databases
            AdministratorCredentials = 
                {| state.AdministratorCredentials with UserName = adminUsername  |} }

    /// Sets geo-redundant backup
    [<CustomOperation "geo_redundant_backup">]
    member _this.SetGeoRedundantBackup(state:PostgreSQLServerBuilderConfig, enabled:bool) =
        { state with GeoRedundantBackup = enabled }

    /// Enables geo-redundant backup
    [<CustomOperation "enable_geo_redundant_backup">]
    member this.EnableGeoRedundantBackup(state:PostgreSQLServerBuilderConfig) =
        this.SetGeoRedundantBackup(state, true)

    /// Disables geo-redundant backup
    [<CustomOperation "disable_geo_redundant_backup">]
    member this.DisableGeoRedundantBackup(state:PostgreSQLServerBuilderConfig) =
        this.SetGeoRedundantBackup(state, false)

    /// Sets storage autogrow
    [<CustomOperation "storage_autogrow">]
    member _this.SetStorageAutogrow(state:PostgreSQLServerBuilderConfig, enabled:bool) =
        { state with StorageAutogrow = enabled }

    /// Enables storage autogrow
    [<CustomOperation "enable_storage_autogrow">]
    member this.EnableStorageAutogrow(state:PostgreSQLServerBuilderConfig) =
        this.SetStorageAutogrow(state, true)

    /// Disables storage autogrow
    [<CustomOperation "disable_storage_autogrow">]
    member this.DisableStorageAutogrow(state:PostgreSQLServerBuilderConfig) =
        this.SetStorageAutogrow(state, false)

    /// sets storage size in MBs
    [<CustomOperation "storage_size">]
    member _this.SetStorageSizeInMBs(state:PostgreSQLServerBuilderConfig, size:int<Gb>) =
        Validate.storageSize size
        { state with StorageSize = size }

    /// sets the backup retention in days
    [<CustomOperation "backup_retention">]
    member _this.SetBackupRetention (state:PostgreSQLServerBuilderConfig, retention:int<Days>) =
        Validate.backupRetention retention
        { state with BackupRetention = retention }

    /// Sets the PostgreSQl server version
    [<CustomOperation "server_version">]
    member _this.SetServerVersion (state:PostgreSQLServerBuilderConfig, version:Version) =
        { state with Version = version }

    /// Sets capacity
    [<CustomOperation "capacity">]
    member _this.SetCapacity (state:PostgreSQLServerBuilderConfig, capacity:int<VCores>) =
        Validate.capacity capacity
        { state with Capacity = capacity }

    /// Sets tier
    [<CustomOperation "tier">]
    member _this.SetTier (state:PostgreSQLServerBuilderConfig, tier:Sku) =
        { state with Tier = tier }

    /// Sets database name
    [<CustomOperation "add_database">]
    member _this.AddDatabase (state:PostgreSQLServerBuilderConfig, database) =
        { state with Databases = database :: state.Databases }
    member this.AddDatabase (state:PostgreSQLServerBuilderConfig, dbName:string) =
        let db = postgreSQLDb { name dbName }
        this.AddDatabase(state, db)

    /// Adds a custom firewall rule given a name, start and end IP address range.
    [<CustomOperation "add_firewall_rule">]
    member _this.AddFirewallWall(state:PostgreSQLServerBuilderConfig, name, startRange, endRange) =
        { state with
            FirewallRules =
                {| Name = ResourceName name
                   Start = IPAddress.Parse startRange
                   End = IPAddress.Parse endRange |}
                :: state.FirewallRules }

    /// Adds a firewall rule that enables access to other Azure services.
    [<CustomOperation "enable_azure_firewall">]
    member this.EnableAzureFirewall(state:PostgreSQLServerBuilderConfig) =
        this.AddFirewallWall(state, "allow-azure-services", "0.0.0.0", "0.0.0.0")

    [<CustomOperation "add_tags">]
    member _.Tags(state:PostgreSQLServerBuilderConfig, pairs) = 
        { state with 
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
    member this.Tag(state:PostgreSQLServerBuilderConfig, key, value) = this.Tags(state, [ (key,value) ])

let postgreSQL = PostgreSQLBuilder()

[<AutoOpen>]
module Farmer.Builders.PostgreSQLAzure

open System
open Farmer
open Farmer.CoreTypes
open Farmer.PostgreSQL
open Arm.DBforPostgreSQL

type PostgreSQLBuilderConfig =
    { ServerName : ResourceRef
      AdminUserName : string option
      Version : Version
      GeoRedundantBackup : bool
      StorageAutogrow : bool
      BackupRetention : int<Days>
      StorageSize : int<Gb>
      Capacity : int<VCores>
      Tier : Sku }
    interface IBuilder with
        member this.DependencyName = this.ServerName.ResourceName
        member this.BuildResources location resources = [
            let inMB (gb: int<Gb>) = 1024 * (int gb) * 1<Mb>
            match this.ServerName with
            | External resName ->
                resources |> Helpers.mergeResource resName (fun server -> { server with Databases = [] })
            | AutomaticallyCreated serverName ->
                { ServerName = serverName
                  Location = location
                  Username = this.AdminUserName |> Option.defaultWith(fun () -> "admin username not set")
                  Password = SecureParameter "administratorLoginPassword"
                  Version = this.Version
                  StorageSize = this.StorageSize |> inMB
                  Capacity = this.Capacity
                  Tier = this.Tier
                  Family = PostgreSQLFamily.Gen5
                  GeoRedundantBackup = FeatureFlag.ofBool this.GeoRedundantBackup
                  StorageAutoGrow = FeatureFlag.ofBool this.StorageAutogrow
                  BackupRetention = this.BackupRetention
                  Databases = [] }
            | AutomaticPlaceholder ->
                failwith "You must specify a server name, or link to an existing server."
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


type PostgreSQLBuilder() =
    member _this.Yield _ =
        { PostgreSQLBuilderConfig.ServerName = AutomaticPlaceholder
          AdminUserName = None
          Version = VS_11
          GeoRedundantBackup = false
          StorageAutogrow = true
          BackupRetention = Validate.minBackupRetention
          StorageSize = Validate.minStorageSize
          Capacity = 2<VCores>
          Tier = Basic }

    member this.Run state =
        state.AdminUserName |> Option.defaultWith(fun () -> failwith "admin username not set") |> ignore
        state

    /// Sets the name of the PostgreSQL server
    [<CustomOperation "server_name">]
    member _this.ServerName(state:PostgreSQLBuilderConfig, serverName) =
        let (ResourceName n) = serverName
        Validate.servername n
        { state with ServerName = AutomaticallyCreated serverName }
    member this.ServerName(state:PostgreSQLBuilderConfig, serverName:string) =
        this.ServerName(state, ResourceName serverName)

    /// Sets the name of the admin user
    [<CustomOperation "admin_username">]
    member this.AdminUsername(state:PostgreSQLBuilderConfig, adminUsername:string) =
        Validate.username "adminUserName" adminUsername
        { state with AdminUserName = Some adminUsername }

    /// Sets geo-redundant backup
    [<CustomOperation "geo_redundant_backup">]
    member this.SetGeoRedundantBackup(state:PostgreSQLBuilderConfig, enabled:bool) =
        { state with GeoRedundantBackup = enabled }

    /// Enables geo-redundant backup
    [<CustomOperation "enable_geo_redundant_backup">]
    member this.EnableGeoRedundantBackup(state:PostgreSQLBuilderConfig) =
        this.SetGeoRedundantBackup(state, true)

    /// Disables geo-redundant backup
    [<CustomOperation "disable_geo_redundant_backup">]
    member this.DisableGeoRedundantBackup(state:PostgreSQLBuilderConfig) =
        this.SetGeoRedundantBackup(state, false)

    /// Sets storage autogrow
    [<CustomOperation "storage_autogrow">]
    member this.SetStorageAutogrow(state:PostgreSQLBuilderConfig, enabled:bool) =
        { state with StorageAutogrow = enabled }

    /// Enables storage autogrow
    [<CustomOperation "enable_storage_autogrow">]
    member this.EnableStorageAutogrow(state:PostgreSQLBuilderConfig) =
        this.SetStorageAutogrow(state, true)

    /// Disables storage autogrow
    [<CustomOperation "disable_storage_autogrow">]
    member this.DisableStorageAutogrow(state:PostgreSQLBuilderConfig) =
        this.SetStorageAutogrow(state, false)

    /// sets storage size in MBs
    [<CustomOperation "storage_size">]
    member this.SetStorageSizeInMBs(state:PostgreSQLBuilderConfig, size:int<Gb>) =
        Validate.storageSize size
        { state with StorageSize = size }

    /// sets the backup retention in days
    [<CustomOperation "backup_retention">]
    member this.SetBackupRetention (state:PostgreSQLBuilderConfig, retention:int<Days>) =
        Validate.backupRetention retention
        { state with BackupRetention = retention }

    /// Sets the PostgreSQl server version
    [<CustomOperation "server_version">]
    member this.SetServerVersion (state:PostgreSQLBuilderConfig, version:Version) =
        { state with Version = version }

    /// Sets capacity
    [<CustomOperation "capacity">]
    member this.SetCapacity (state:PostgreSQLBuilderConfig, capacity:int<VCores>) =
        Validate.capacity capacity
        { state with Capacity = capacity }

    /// Sets tier
    [<CustomOperation "tier">]
    member this.SetTier (state:PostgreSQLBuilderConfig, tier:Sku) =
        { state with Tier = tier }

let postgreSQL = PostgreSQLBuilder()

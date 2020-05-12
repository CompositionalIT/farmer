[<AutoOpen>]
module Farmer.Builders.PostgreSQLAzure

open System
open Farmer
open Arm.PostgreSQL

[<Measure>] type Days
[<Measure>] type GB
[<Measure>] type VCores

type PostgreSQLBuilderState = {
    ServerName : ResourceRef
    AdminUserName : string option
    Version : ServerVersion
    GeoRedundantBackup : bool
    StorageAutogrow : bool
    BackupRetention : int<Days>
    StorageSize : int<GB>
    Capacity : int<VCores>
    Tier : SkuTier
}


[<AutoOpen>]
module private Helpers =
    module Option =
        let getOrFailWith msg = function
            | None -> failwith msg
            | Some v -> v

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

    let minStorageSize = 5<GB>
    let maxStorageSize = 1024<GB>
    let storageSize (size : int<GB>) =
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
    let inMB (gb: int<GB>) = 1024 * (int gb)

    member _this.Yield _ = {
        PostgreSQLBuilderState.ServerName = AutomaticPlaceholder
        AdminUserName = None
        Version = VS_11
        GeoRedundantBackup = false
        StorageAutogrow = true
        BackupRetention = Validate.minBackupRetention
        StorageSize = Validate.minStorageSize
        Capacity = 2<VCores>
        Tier = Basic
    }

    member _this.Run (state: PostgreSQLBuilderState) =
        let adminName = state.AdminUserName |> Option.getOrFailWith "admin username not set"

        { new IBuilder with
            member _.BuildResources location resources =
                let serverResource =
                    match state.ServerName with
                    | External resName ->
                        resources |> Helpers.mergeResource resName (fun server -> { server with Databases = [] })
                    | AutomaticallyCreated serverName ->
                        { ServerName = serverName
                          Location = location
                          Username = adminName
                          Password = SecureParameter "administratorLoginPassword"
                          Version = state.Version
                          StorageSize = state.StorageSize |> inMB
                          Capacity = int state.Capacity
                          Tier = state.Tier
                          Family = Gen5
                          GeoRedundantBackup = FeatureFlag.ofBool state.GeoRedundantBackup
                          StorageAutoGrow = FeatureFlag.ofBool state.StorageAutogrow
                          BackupRetention = int state.BackupRetention
                          Databases = [] }
                    | AutomaticPlaceholder -> failwith "You must specific a server name, or link to an existing server."

                [serverResource] }

    /// Sets the name of the PostgreSQL server
    [<CustomOperation "server_name">]
    member _this.ServerName(state:PostgreSQLBuilderState, serverName) =
        let (ResourceName n) = serverName
        Validate.servername n
        { state with ServerName = AutomaticallyCreated serverName }
    member this.ServerName(state:PostgreSQLBuilderState, serverName:string) =
        this.ServerName(state, ResourceName serverName)

    /// Sets the name of the admin user
    [<CustomOperation "admin_username">]
    member this.AdminUsername(state:PostgreSQLBuilderState, adminUsername:string) =
        Validate.username "adminUserName" adminUsername
        { state with AdminUserName = Some adminUsername }

    /// Sets geo-redundant backup
    [<CustomOperation "geo_redundant_backup">]
    member this.SetGeoRedundantBackup(state:PostgreSQLBuilderState, enabled:bool) =
        { state with GeoRedundantBackup = enabled }

    /// Enables geo-redundant backup
    [<CustomOperation "enable_geo_redundant_backup">]
    member this.EnableGeoRedundantBackup(state:PostgreSQLBuilderState) =
        this.SetGeoRedundantBackup(state, true)

    /// Disables geo-redundant backup
    [<CustomOperation "disable_geo_redundant_backup">]
    member this.DisableGeoRedundantBackup(state:PostgreSQLBuilderState) =
        this.SetGeoRedundantBackup(state, false)

    /// Sets storage autogrow
    [<CustomOperation "storage_autogrow">]
    member this.SetStorageAutogrow(state:PostgreSQLBuilderState, enabled:bool) =
        { state with StorageAutogrow = enabled }

    /// Enables storage autogrow
    [<CustomOperation "enable_storage_autogrow">]
    member this.EnableStorageAutogrow(state:PostgreSQLBuilderState) =
        this.SetStorageAutogrow(state, true)

    /// Disables storage autogrow
    [<CustomOperation "disable_storage_autogrow">]
    member this.DisableStorageAutogrow(state:PostgreSQLBuilderState) =
        this.SetStorageAutogrow(state, false)

    /// sets storage size in MBs
    [<CustomOperation "storage_size">]
    member this.SetStorageSizeInMBs(state:PostgreSQLBuilderState, size:int<GB>) =
        Validate.storageSize size
        { state with StorageSize = size }

    /// sets the backup retention in days
    [<CustomOperation "backup_retention">]
    member this.SetBackupRetention (state:PostgreSQLBuilderState, retention:int<Days>) =
        Validate.backupRetention retention
        { state with BackupRetention = retention }

    /// Sets the PostgreSQl server version
    [<CustomOperation "server_version">]
    member this.SetServerVersion (state:PostgreSQLBuilderState, version:ServerVersion) =
        { state with Version = version }

    /// Sets capacity
    [<CustomOperation "capacity">]
    member this.SetCapacity (state:PostgreSQLBuilderState, capacity:int<VCores>) =
        Validate.capacity capacity
        { state with Capacity = capacity }

    /// Sets tier
    [<CustomOperation "tier">]
    member this.SetTier (state:PostgreSQLBuilderState, tier:SkuTier) =
        { state with Tier = tier }


let postgreSQL = PostgreSQLBuilder()

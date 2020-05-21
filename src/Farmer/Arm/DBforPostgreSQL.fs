[<AutoOpen>]
module Farmer.Arm.DBforPostgreSQL

open Farmer
open Farmer.CoreTypes
open Farmer.PostgreSQL

type [<RequireQualifiedAccess>] PostgreSQLFamily = Gen5

type Database =
    { Name : ResourceName
      Edition : string
      Collation : string }

type Server =
    { ServerName : ResourceName
      Location : Location
      Username : string
      Password : SecureParameter
      Version : Version
      Capacity : int<VCores>
      StorageSize : int<Mb>
      Tier : Sku
      Family : PostgreSQLFamily
      GeoRedundantBackup : FeatureFlag
      StorageAutoGrow : FeatureFlag
      BackupRetention : int<Days>
      Databases : Database list }

    member this.Sku =
        {| name = sprintf "%s_%O_%d" this.Tier.Name this.Family this.Capacity
           tier = this.Tier.ToString()
           capacity = this.Capacity
           family = this.Family.ToString()
           size = this.StorageSize |}

    member this.GetStorageProfile () = {|
        storageMB = this.StorageSize
        backupRetentionDays = this.BackupRetention
        geoRedundantBackup = this.GeoRedundantBackup.ToString()
        storageAutoGrow = this.StorageAutoGrow.ToString()
    |}

    member this.GetProperties () =
        let version =
            match this.Version with
            | VS_9_5 -> "9.5"
            | VS_9_6 -> "9.6"
            | VS_10 -> "10"
            | VS_11 -> "11"
        {| administratorLogin = this.Username
           administratorLoginPassword = this.Password.AsArmRef.Eval()
           version = version
           storageProfile = this.GetStorageProfile() |}

    interface IParameters with
        member this.SecureParameters = [ this.Password ]

    interface IArmResource with
        member this.ResourceName = this.ServerName
        member this.JsonModel =
            box {| ``type`` = "Microsoft.DBforPostgreSQL/servers"
                   apiVersion = "2017-12-01"
                   name = this.ServerName.Value
                   location = this.Location.ArmValue
                   tags = {| displayName = this.ServerName.Value |}
                   sku = this.Sku
                   properties = this.GetProperties()
            |}

[<AutoOpen>]
module Farmer.Arm.DBforPostgreSQL

open System.Net
open Farmer
open Farmer.CoreTypes
open Farmer.PostgreSQL

type [<RequireQualifiedAccess>] PostgreSQLFamily = Gen5

module Servers =
    type Database =
        { Name : ResourceName
          Server : ResourceName
          Charset : string
          Collation : string }
        interface IArmResource with
            member this.ResourceName = this.Name
            member this.JsonModel =
                {|  ``type`` = "Microsoft.DBforPostgreSQL/servers/databases"
                    name = this.Server.Value + "/" + this.Name.Value
                    apiVersion = "2017-12-01"
                    dependsOn = [ this.Server.Value ]
                    properties = {|  charset = this.Charset; collation = this.Collation |}
                |} :> _

    type FirewallRule =
        { Name : ResourceName
          Server : ResourceName
          Location : Location
          Start : IPAddress
          End : IPAddress }
        interface IArmResource with
            member this.ResourceName = this.Name
            member this.JsonModel =
                {|  ``type`` = "Microsoft.DBforPostgreSQL/servers/firewallrules"
                    name = this.Server.Value + "/" + this.Name.Value
                    apiVersion = "2014-04-01"
                    location = this.Location.ArmValue
                    properties = {| endIpAddress = string this.Start; startIpAddress = string this.End |}
                    dependsOn = [ this.Server.Value ]
                |} :> _

type Server =
    { Name : ResourceName
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
      BackupRetention : int<Days> }

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
        member this.ResourceName = this.Name
        member this.JsonModel =
            box {|
                ``type`` = "Microsoft.DBforPostgreSQL/servers"
                apiVersion = "2017-12-01"
                name = this.Name.Value
                location = this.Location.ArmValue
                tags = {| displayName = this.Name.Value |}
                sku = this.Sku
                properties = this.GetProperties()
            |}

[<AutoOpen>]
module Farmer.Arm.DBforPostgreSQL

open System.Net
open Farmer
open Farmer.CoreTypes
open Farmer.PostgreSQL

type [<RequireQualifiedAccess>] PostgreSQLFamily = Gen5

type Database =
    { Name : ResourceName
      Charset : string
      Collation : string }

type FirewallRule =
    { Name : string
      Start : IPAddress
      End : IPAddress } 

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
      Databases : Database list
      FirewallRules : FirewallRule list }

    member this.Sku =
        {| name = sprintf "%s_%O_%d" this.Tier.Name this.Family this.Capacity
           tier = this.Tier.ToString()
           capacity = this.Capacity
           family = this.Family.ToString()
           size = this.StorageSize |}

    static member WithDatabase (db: Database option) (server: Server) = 
        match db with
        | None -> server
        | Some database ->  {
            server with Databases = database :: server.Databases
        }

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
            box {| 
                ``type`` = "Microsoft.DBforPostgreSQL/servers"
                apiVersion = "2017-12-01"
                name = this.ServerName.Value
                location = this.Location.ArmValue
                tags = {| displayName = this.ServerName.Value |}
                sku = this.Sku
                properties = this.GetProperties()
                resources = [ 
                    for database in this.Databases do
                        box {|  ``type`` = "databases"
                                name = database.Name.Value
                                apiVersion = "2017-12-01"
                                dependsOn = [ this.ServerName.Value ]
                                properties = {|  charset = database.Charset; collation = database.Collation |} 
                            |}
                    for rule in this.FirewallRules do
                        box {|  ``type`` = "firewallrules"
                                name = rule.Name
                                apiVersion = "2014-04-01"
                                location = this.Location.ArmValue
                                properties = {| endIpAddress = string rule.Start; startIpAddress = string rule.End |}
                                dependsOn = [ this.ServerName.Value ]
                            |}
                ]
            |}

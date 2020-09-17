[<AutoOpen>]
module Farmer.Arm.DBforPostgreSQL

open System.Net
open Farmer
open Farmer.CoreTypes
open Farmer.PostgreSQL

let databases = ResourceType ("Microsoft.DBforPostgreSQL/servers/databases", "2017-12-01")
let firewallRules = ResourceType ("Microsoft.DBforPostgreSQL/servers/firewallrules",  "2017-12-01")
let servers = ResourceType ("Microsoft.DBforPostgreSQL/servers", "2017-12-01")

[<RequireQualifiedAccess>]
type PostgreSQLFamily =
    | Gen5
    override this.ToString() =
        match this with
        | Gen5 -> "Gen5"
    member this.AsArmValue =
        match this with
        | Gen5 -> "Gen5"


module Servers =
    type Database =
        { Name : ResourceName
          Server : ResourceName
          Charset : string
          Collation : string }
        interface IArmResource with
            member this.ResourceName = this.Name
            member this.JsonModel =
                {|  ``type`` = databases.Path
                    apiVersion = databases.Version
                    name = this.Server.Value + "/" + this.Name.Value
                    dependsOn = [ this.Server.Value ]
                    properties = {|  charset = this.Charset; collation = this.Collation |}
                |} :> _

    type FirewallRule =
        { Name : ResourceName
          Server : ResourceName
          Start : IPAddress
          End : IPAddress
          Location : Location }
        interface IArmResource with
            member this.ResourceName = this.Name
            member this.JsonModel =
                {|  ``type`` = firewallRules.Path
                    apiVersion = firewallRules.Version
                    name = this.Server.Value + "/" + this.Name.Value
                    location = this.Location.ArmValue
                    properties = {| startIpAddress = string this.Start; endIpAddress = string this.End; |}
                    dependsOn = [ this.Server.Value ]
                |} :> _

type Server =
    { Name : ResourceName
      Location : Location
      Credentials : {| Username : string; Password : SecureParameter |}
      Version : Version
      Capacity : int<VCores>
      StorageSize : int<Mb>
      Tier : Sku
      Family : PostgreSQLFamily
      GeoRedundantBackup : FeatureFlag
      StorageAutoGrow : FeatureFlag
      BackupRetention : int<Days>
      Tags: Map<string,string>  }

    member this.Sku =
        {| name = sprintf "%s_%s_%d" this.Tier.Name this.Family.AsArmValue this.Capacity
           tier = string this.Tier
           capacity = this.Capacity
           family = string this.Family
           size = string this.StorageSize |}

    member this.GetStorageProfile () = {|
        storageMB = this.StorageSize
        backupRetentionDays = this.BackupRetention
        geoRedundantBackup = string this.GeoRedundantBackup
        storageAutoGrow = string this.StorageAutoGrow
    |}

    member this.GetProperties () =
        let version =
            match this.Version with
            | VS_9_5 -> "9.5"
            | VS_9_6 -> "9.6"
            | VS_10 -> "10"
            | VS_11 -> "11"

        {| administratorLogin = this.Credentials.Username
           administratorLoginPassword = this.Credentials.Password.AsArmRef.Eval()
           version = version
           storageProfile = this.GetStorageProfile() |}

    interface IParameters with
        member this.SecureParameters = [ this.Credentials.Password ]

    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {|
                ``type`` = servers.Path
                apiVersion = servers.Version
                name = this.Name.Value
                location = this.Location.ArmValue
                tags = this.Tags |> Map.add "displayName" this.Name.Value
                sku = this.Sku
                properties = this.GetProperties()
            |} :> _

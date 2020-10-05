[<AutoOpen>]
module Farmer.Arm.Sql

open Farmer
open Farmer.CoreTypes
open Farmer.Sql
open System.Net

let servers = ResourceType ("Microsoft.Sql/servers", "2019-06-01-preview")
let elasticPools = ResourceType ("Microsoft.Sql/servers/elasticPools", "2017-10-01-preview")
let firewallRules = ResourceType ("Microsoft.Sql/servers/firewallrules", "2014-04-01")
let databases = ResourceType ("Microsoft.Sql/servers/databases", "2019-06-01-preview")
let transparentDataEncryption = ResourceType ("Microsoft.Sql/servers/databases/transparentDataEncryption", "2014-04-01-preview")

type DbKind = Standalone of DbPurchaseModel | Pool of ResourceName

type Server =
    { ServerName : ResourceName
      Location : Location
      Credentials : {| Username : string; Password : SecureParameter |}
      Tags: Map<string,string> }
    interface IParameters with
        member this.SecureParameters = [ this.Credentials.Password ]
    interface IArmResource with
        member this.ResourceName = this.ServerName
        member this.JsonModel =
            {| servers.Create(this.ServerName,this.Location, tags = (this.Tags |> Map.add "displayName" this.ServerName.Value)) with
                properties =
                 {| administratorLogin = this.Credentials.Username
                    administratorLoginPassword = this.Credentials.Password.ArmExpression.Eval()
                    version = "12.0" |}
            |} :> _

module Servers =
    type ElasticPool =
        { Name : ResourceName
          Server : ResourceName
          Location : Location
          Sku : PoolSku
          MinMax : (int<DTU> * int<DTU>) option
          MaxSizeBytes : int64 option }
        interface IArmResource with
            member this.ResourceName = this.Name
            member this.JsonModel =
                {| elasticPools.Create(this.Server/this.Name, this.Location, [ ResourceId.create this.Server ]) with
                    properties =
                     {| maxSizeBytes = this.MaxSizeBytes |> Option.toNullable
                        perDatabaseSettings =
                         match this.MinMax with
                         | Some (min, max) -> box {| minCapacity = min; maxCapacity = max |}
                         | None -> null
                     |}
                    sku = {| name = this.Sku.Name; tier = this.Sku.Edition; size = string this.Sku.Capacity |} |} :> _

    type FirewallRule =
        { Name : ResourceName
          Server : ResourceName
          Location : Location
          Start : IPAddress
          End : IPAddress }
        interface IArmResource with
            member this.ResourceName = this.Name
            member this.JsonModel =
                {| firewallRules.Create(this.Server/this.Name, this.Location, [ ResourceId.create this.Server ]) with
                    properties =
                     {| startIpAddress = string this.Start
                        endIpAddress = string this.End |}
                |} :> _

    type Database =
        { Name : ResourceName
          Server : ResourceName
          Location : Location
          MaxSizeBytes : int64 option
          Sku : DbKind
          Collation : string }
        interface IArmResource with
            member this.ResourceName = this.Name
            member this.JsonModel =
                let dependsOn = [
                        ResourceId.create this.Server
                        match this.Sku with
                        | Pool poolName -> ResourceId.create poolName
                        | Standalone _ -> ()
                ]
                {| databases.Create(this.Server/this.Name, this.Location, dependsOn, tags = Map [ "displayName", this.Name.Value ]) with
                    sku =
                        match this.Sku with
                        | Standalone sku -> box {| name = sku.Name; tier = sku.Edition |}
                        | Pool _ -> null
                    properties =
                        {| collation = this.Collation
                           maxSizeBytes = this.MaxSizeBytes |> Option.toNullable
                           licenseType =
                            match this.Sku with
                            | Standalone (VCore (_, license)) ->
                                license.ArmValue
                            | Standalone (DTU _)
                            | Pool _ ->
                                null
                           elasticPoolId =
                            match this.Sku with
                            | Standalone _ ->
                                null
                            | Pool pool ->
                                ResourceId.create(elasticPools, this.Server, pool).Eval()
                        |}
                |} :> _

    module Databases =
        type TransparentDataEncryption =
            { Server : ResourceName
              Database : ResourceName }
            member this.Name = this.Server/this.Database/"current"
            interface IArmResource with
                member this.ResourceName = this.Name
                member this.JsonModel =
                   {| transparentDataEncryption.Create(this.Name, dependsOn = [ ResourceId.create this.Database ]) with
                        comments = "Transparent Data Encryption"
                        properties = {| status = string Enabled |}
                   |} :> _
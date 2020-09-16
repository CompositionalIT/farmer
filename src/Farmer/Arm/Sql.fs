[<AutoOpen>]
module Farmer.Arm.Sql

open Farmer
open Farmer.CoreTypes
open Farmer.Sql
open System.Net

let servers = ResourceType "Microsoft.Sql/servers"
let elasticPools = ResourceType "Microsoft.Sql/servers/elasticPools"
let firewallRules = ResourceType "Microsoft.Sql/servers/firewallrules"
let databases = ResourceType "Microsoft.Sql/servers/databases"
let transparentDataEncryption = ResourceType "Microsoft.Sql/servers/databases/transparentDataEncryption"

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
            {| ``type`` = servers.ArmValue
               name = this.ServerName.Value
               apiVersion = "2019-06-01-preview"
               location = this.Location.ArmValue
               tags =
                this.Tags
                |> Map.add "displayName" this.ServerName.Value
               properties =
                {| administratorLogin = this.Credentials.Username
                   administratorLoginPassword = this.Credentials.Password.AsArmRef.Eval()
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
                {| ``type`` = elasticPools.ArmValue
                   name = this.Server.Value + "/" + this.Name.Value
                   properties =
                    {| maxSizeBytes = this.MaxSizeBytes |> Option.toNullable
                       perDatabaseSettings =
                        match this.MinMax with
                        | Some (min, max) -> box {| minCapacity = min; maxCapacity = max |}
                        | None -> null
                    |}
                   apiVersion = "2017-10-01-preview"
                   location = this.Location.ArmValue
                   sku = {| name = this.Sku.Name; tier = this.Sku.Edition; size = string this.Sku.Capacity |}
                   dependsOn = [ this.Server.Value ] |} :> _

    type FirewallRule =
        { Name : ResourceName
          Server : ResourceName
          Location : Location
          Start : IPAddress
          End : IPAddress }
        interface IArmResource with
            member this.ResourceName = this.Name
            member this.JsonModel =
                {| ``type`` = firewallRules.ArmValue
                   name = this.Server.Value + "/" + this.Name.Value
                   apiVersion = "2014-04-01"
                   location = this.Location.ArmValue
                   properties =
                    {| endIpAddress = string this.Start
                       startIpAddress = string this.End |}
                   dependsOn = [ this.Server.Value ]
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
                {| ``type`` = databases.ArmValue
                   name = this.Server.Value + "/" + this.Name.Value
                   apiVersion = "2019-06-01-preview"
                   location = this.Location.ArmValue
                   tags = {| displayName = this.Name.Value |}
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
                                    ArmExpression.resourceId(elasticPools, this.Server, pool).Eval()
                        |}
                   dependsOn =
                    [ this.Server.Value
                      match this.Sku with
                      | Standalone _ -> ()
                      | Pool poolName -> poolName.Value
                    ]
                |} :> _

    module Databases =
        type TransparentDataEncryption =
            { Server : ResourceName
              Database : ResourceName }
            member this.Name = ResourceName (sprintf "%s/%s/current" this.Server.Value this.Database.Value)
            interface IArmResource with
                member this.ResourceName = this.Name
                member this.JsonModel =
                   {| ``type`` = transparentDataEncryption.ArmValue
                      comments = "Transparent Data Encryption"
                      name = this.Name.Value
                      apiVersion = "2014-04-01-preview"
                      properties = {| status = string Enabled |}
                      dependsOn = [ this.Database.Value ] |} :> _
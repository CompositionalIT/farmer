[<AutoOpen>]
module Farmer.Arm.Sql

open Farmer
open Farmer.Sql
open System.Net

let servers = ResourceType("Microsoft.Sql/servers", "2019-06-01-preview")

let elasticPools =
    ResourceType("Microsoft.Sql/servers/elasticPools", "2017-10-01-preview")

let firewallRules =
    ResourceType("Microsoft.Sql/servers/firewallrules", "2014-04-01")

let databases =
    ResourceType("Microsoft.Sql/servers/databases", "2019-06-01-preview")

let transparentDataEncryption =
    ResourceType("Microsoft.Sql/servers/databases/transparentDataEncryption", "2014-04-01-preview")

type DbKind =
    | Standalone of DbPurchaseModel
    | Pool of ResourceName

type Server =
    {
        ServerName: SqlAccountName
        Location: Location
        Credentials: {| Username: string
                        Password: SecureParameter |}
        MinTlsVersion: TlsVersion option
        Tags: Map<string, string>
    }

    interface IParameters with
        member this.SecureParameters = [ this.Credentials.Password ]

    interface IArmResource with
        member this.ResourceId = servers.resourceId this.ServerName.ResourceName

        member this.JsonModel =
            {| servers.Create(
                   this.ServerName.ResourceName,
                   this.Location,
                   tags = (this.Tags |> Map.add "displayName" this.ServerName.ResourceName.Value)
               ) with
                properties =
                    {|
                        administratorLogin = this.Credentials.Username
                        administratorLoginPassword = this.Credentials.Password.ArmExpression.Eval()
                        version = "12.0"
                        minimalTlsVersion =
                            match this.MinTlsVersion with
                            | Some Tls10 -> "1.0"
                            | Some Tls11 -> "1.1"
                            | Some Tls12 -> "1.2"
                            | None -> null
                    |}
            |}

module Servers =
    type ElasticPool =
        {
            Name: ResourceName
            Server: SqlAccountName
            Location: Location
            Sku: PoolSku
            MinMax: (int<DTU> * int<DTU>) option
            MaxSizeBytes: int64 option
        }

        interface IArmResource with
            member this.ResourceId = elasticPools.resourceId (this.Server.ResourceName / this.Name)

            member this.JsonModel =
                {| elasticPools.Create(
                       this.Server.ResourceName / this.Name,
                       this.Location,
                       [ servers.resourceId this.Server.ResourceName ]
                   ) with
                    properties =
                        {|
                            maxSizeBytes = this.MaxSizeBytes |> Option.toNullable
                            perDatabaseSettings =
                                match this.MinMax with
                                | Some (min, max) ->
                                    box
                                        {|
                                            minCapacity = min
                                            maxCapacity = max
                                        |}
                                | None -> null
                        |}
                    sku =
                        {|
                            name = this.Sku.Name
                            tier = this.Sku.Edition
                            size = string this.Sku.Capacity
                        |}
                |}

    type FirewallRule =
        {
            Name: ResourceName
            Server: SqlAccountName
            Location: Location
            Start: IPAddress
            End: IPAddress
        }

        interface IArmResource with
            member this.ResourceId =
                firewallRules.resourceId (this.Server.ResourceName / this.Name)

            member this.JsonModel =
                {| firewallRules.Create(
                       this.Server.ResourceName / this.Name,
                       this.Location,
                       [ servers.resourceId this.Server.ResourceName ]
                   ) with
                    properties =
                        {|
                            startIpAddress = string this.Start
                            endIpAddress = string this.End
                        |}
                |}

    type Database =
        {
            Name: ResourceName
            Server: LinkedResource
            Location: Location
            MaxSizeBytes: int64 option
            Sku: DbKind
            Collation: string
            Tags: Map<string, string>
        }

        interface IArmResource with
            member this.ResourceId = databases.resourceId (this.Server.Name / this.Name)

            member this.JsonModel =
                let dependsOn = Set [
                    match this.Server with
                    | Unmanaged _ -> ()
                    | Managed serverName -> 
                        serverName

                        match this.Sku with
                        | Standalone _ -> ()
                        | Pool poolName ->
                            {serverName with Type = elasticPools; Segments = [poolName]}
                  ]

                let serverName = 
                  SqlAccountName.Create this.Server.Name
                  |> Result.get

                {| databases.Create(
                       serverName.ResourceName / this.Name,
                       this.Location,
                       dependsOn,
                       tags = this.Tags.Add("displayName", this.Name.Value)
                   ) with
                    sku =
                        match this.Sku with
                        | Standalone (VCore (GeneralPurpose (S_Gen5 (_, max)), _) as sku)
                        | Standalone (VCore (BusinessCritical (S_Gen5 (_, max)), _) as sku)
                        | Standalone (VCore (Hyperscale (S_Gen5 (_, max)), _) as sku) -> (* Serverless *)
                            box
                                {|
                                    name = sku.Name
                                    tier = sku.Edition
                                    capacity = max
                                    family = "Gen5"
                                |}
                        | Standalone sku ->
                            box
                                {|
                                    name = sku.Name
                                    tier = sku.Edition
                                |}
                        | Pool _ -> null
                    properties =
                        {|
                            collation = this.Collation
                            maxSizeBytes = this.MaxSizeBytes |> Option.toNullable
                            licenseType =
                                match this.Sku with
                                | Standalone (VCore (_, license)) -> license.ArmValue
                                | Standalone (DTU _)
                                | Pool _ -> null
                            elasticPoolId =
                                match this.Sku with
                                | Standalone _ -> null
                                | Pool pool -> elasticPools.resourceId(this.Server.Name, pool).Eval()
                            autoPauseDelay =
                                match this.Sku with
                                | Standalone (VCore (GeneralPurpose (S_Gen5 _), _))
                                | Standalone (VCore (BusinessCritical (S_Gen5 _), _))
                                | Standalone (VCore (Hyperscale (S_Gen5 _), _)) -> -1 |> box
                                | _ -> null
                            minCapacity =
                                match this.Sku with
                                | Standalone (VCore (GeneralPurpose (S_Gen5 (min, _)), _) as sku)
                                | Standalone (VCore (BusinessCritical (S_Gen5 (min, _)), _) as sku)
                                | Standalone (VCore (Hyperscale (S_Gen5 (min, _)), _) as sku) -> min |> box
                                | _ -> null
                        |}
                |}

    module Databases =
        type TransparentDataEncryption =
            {
                Server: LinkedResource
                Database: ResourceName
            }

            member this.Name = this.Server.Name / this.Database / "current"

            interface IArmResource with
                member this.ResourceId = transparentDataEncryption.resourceId this.Name

                member this.JsonModel =
                    {| transparentDataEncryption.Create(
                           this.Name,
                           dependsOn = [ {this.Server.ResourceId with Type = databases; Segments = [this.Database] } ]
                       ) with
                        comments = "Transparent Data Encryption"
                        properties = {| status = string Enabled |}
                    |}

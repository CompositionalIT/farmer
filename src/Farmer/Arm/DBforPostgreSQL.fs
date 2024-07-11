[<AutoOpen>]
module Farmer.Arm.DBforPostgreSQL

open System.Net
open Farmer
open Farmer.PostgreSQL

let databases =
    ResourceType("Microsoft.DBforPostgreSQL/servers/databases", "2017-12-01")

let firewallRules =
    ResourceType("Microsoft.DBforPostgreSQL/servers/firewallrules", "2017-12-01")

let virtualNetworkRules =
    ResourceType("Microsoft.DBforPostgreSQL/servers/virtualNetworkRules", "2017-12-01")

let servers = ResourceType("Microsoft.DBforPostgreSQL/servers", "2017-12-01")

let flexibleFirewallRules =
    ResourceType("Microsoft.DBforPostgreSQL/flexibleservers/firewallrules", "2023-06-01-preview")

let flexibleDatabases =
    ResourceType("Microsoft.DBforPostgreSQL/flexibleServers/databases", "2023-06-01-preview")

let flexibleServers =
    ResourceType("Microsoft.DBforPostgreSQL/flexibleServers", "2023-06-01-preview")



type PostgreSQLFamily =
    | Gen5

    member this.AsArmValue =
        match this with
        | Gen5 -> "Gen5"

module Servers =
    type Database = {
        Name: ResourceName
        Server: ResourceName
        Charset: string
        Collation: string
        ResourceType: ResourceType
        ServerType: ResourceType
    } with

        interface IArmResource with
            member this.ResourceId = this.ResourceType.resourceId (this.Server / this.Name)

            member this.JsonModel = {|
                this.ResourceType.Create(
                    this.Server / this.Name,
                    dependsOn = [ this.ServerType.resourceId this.Server ]
                ) with
                    properties = {|
                        charset = this.Charset
                        collation = this.Collation
                    |}
            |}

    type FirewallRule = {
        Name: ResourceName
        Server: ResourceName
        Start: IPAddress
        End: IPAddress
        Location: Location
        ResourceType: ResourceType
        ServerType: ResourceType
    } with

        interface IArmResource with
            member this.ResourceId = this.ResourceType.resourceId (this.Server / this.Name)

            member this.JsonModel = {|
                this.ResourceType.Create(
                    this.Server / this.Name,
                    this.Location,
                    [ this.ServerType.resourceId this.Server ]
                ) with
                    properties = {|
                        startIpAddress = string this.Start
                        endIpAddress = string this.End
                    |}
            |}

    type VnetRule = {
        Name: ResourceName
        Server: ResourceName
        VirtualNetworkSubnetId: ResourceId
        Location: Location
    } with

        interface IArmResource with
            member this.ResourceId = virtualNetworkRules.resourceId (this.Server / this.Name)

            member this.JsonModel = {|
                virtualNetworkRules.Create(this.Server / this.Name, this.Location, [ servers.resourceId this.Server ]) with
                    properties = {|
                        virtualNetworkSubnetId = this.VirtualNetworkSubnetId.Eval()
                    |}
            |}

type Server = {
    Name: ResourceName
    Location: Location
    Credentials: {|
        Username: string
        Password: SecureParameter
    |}
    Version: Version
    Capacity: int<VCores>
    StorageSize: int<Mb>
    Sku: Sku
    Family: PostgreSQLFamily
    GeoRedundantBackup: FeatureFlag
    StorageAutoGrow: FeatureFlag
    BackupRetention: int<Days>
    Tags: Map<string, string>
} with

    interface IParameters with
        member this.SecureParameters = [ this.Credentials.Password ]

    interface IArmResource with
        member this.ResourceId = servers.resourceId this.Name

        member this.JsonModel = {|
            servers.Create(this.Name, this.Location, tags = (this.Tags |> Map.add "displayName" this.Name.Value)) with
                sku = {|
                    name = $"{this.Sku.Name}_{this.Family.AsArmValue}_{this.Capacity}"
                    tier = string this.Sku
                    capacity = this.Capacity
                    family = string this.Family
                    size = string this.StorageSize
                |}
                properties =
                    let version =
                        match this.Version with
                        | VS_9_5 -> "9.5"
                        | VS_9_6 -> "9.6"
                        | VS_10 -> "10"
                        | VS_11 -> "11"

                    {|
                        administratorLogin = this.Credentials.Username
                        administratorLoginPassword = this.Credentials.Password.ArmExpression.Eval()
                        version = version
                        storageProfile = {|
                            storageMB = this.StorageSize
                            backupRetentionDays = this.BackupRetention
                            geoRedundantBackup = string this.GeoRedundantBackup
                            storageAutoGrow = string this.StorageAutoGrow
                        |}
                    |}
        |}

type FlexibleServer = {
    Name: ResourceName
    Location: Location
    Credentials: {|
        Username: string
        Password: SecureParameter
    |}
    Version: FlexibleVersion
    Tier: FlexibleTier
    Storage: {|
        Size: int<Gb>
        AutoGrow: FeatureFlag
        PerformanceTier: Vm.DiskPerformanceTier option
    |}
    Backup: {|
        Retention: int<Days>
        GeoRedundancy: FeatureFlag
    |}
    Tags: Map<string, string>
} with

    interface IParameters with
        member this.SecureParameters = [ this.Credentials.Password ]

    interface IArmResource with
        member this.ResourceId = flexibleServers.resourceId this.Name

        member this.JsonModel = {|
            flexibleServers.Create(
                this.Name,
                this.Location,
                tags = (this.Tags |> Map.add "displayName" this.Name.Value)
            ) with
                sku = {|
                    name = this.Tier.VmSize.ArmValue
                    tier = this.Tier.ArmValue
                |}
                properties = {|
                    administratorLogin = this.Credentials.Username
                    administratorLoginPassword = this.Credentials.Password.ArmExpression.Eval()
                    version = this.Version.ArmValue
                    backup = {|
                        backupRetentionDays = this.Backup.Retention
                        geoRedundantBackup = string this.Backup.GeoRedundancy
                    |}
                    Storage = {|
                        StorageSizeGB = this.Storage.Size
                        AutoGrow = this.Storage.AutoGrow.ArmValue
                        tier = this.Storage.PerformanceTier |> Option.map _.ArmValue |> Option.toObj
                    |}
                |}
        |}
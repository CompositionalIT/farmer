[<AutoOpen>]
module Farmer.Arm.Storage

open Farmer
open Farmer.Storage

let storageAccounts =
    ResourceType("Microsoft.Storage/storageAccounts", "2023-05-01")

let blobServices =
    ResourceType("Microsoft.Storage/storageAccounts/blobServices", "2019-06-01")

let containers =
    ResourceType("Microsoft.Storage/storageAccounts/blobServices/containers", "2018-03-01-preview")

let fileServices =
    ResourceType("Microsoft.Storage/storageAccounts/fileServices", "2019-06-01")

let fileShares =
    ResourceType("Microsoft.Storage/storageAccounts/fileServices/shares", "2019-06-01")

let queueServices =
    ResourceType("Microsoft.Storage/storageAccounts/queueServices", "2019-06-01")

let queues =
    ResourceType("Microsoft.Storage/storageAccounts/queueServices/queues", "2019-06-01")

let tableServices =
    ResourceType("Microsoft.Storage/storageAccounts/tableServices", "2019-06-01")

let tables =
    ResourceType("Microsoft.Storage/storageAccounts/tableServices/tables", "2019-06-01")

let managementPolicies =
    ResourceType("Microsoft.Storage/storageAccounts/managementPolicies", "2019-06-01")

let roleAssignments =
    ResourceType("Microsoft.Storage/storageAccounts/providers/roleAssignments", "2018-09-01-preview")

type Metadata = Map<string, string>

type ImmutabilityPolicyState =
    | Unlocked
    | Locked

    member this.ArmValue =
        match this with
        | Unlocked -> "Unlocked"
        | Locked -> "Locked"

[<RequireQualifiedAccess>]
type NetworkRuleSetBypass =
    | None
    | AzureServices
    | Logging
    | Metrics

    static member ArmValue =
        function
        | None -> "None"
        | AzureServices -> "AzureServices"
        | Logging -> "Logging"
        | Metrics -> "Metrics"

[<RequireQualifiedAccess>]
type RuleAction =
    | Allow
    | Deny

    member this.ArmValue =
        match this with
        | Allow -> "Allow"
        | Deny -> "Deny"

type VirtualNetworkRule = {
    Subnet: ResourceName
    VirtualNetwork: ResourceName
    Action: RuleAction
}

type IpRuleValue =
    | IpRulePrefix of IPAddressCidr
    | IpRuleAddress of System.Net.IPAddress

    member this.ArmValue =
        match this with
        | IpRulePrefix(cidr) -> cidr |> IPAddressCidr.format
        | IpRuleAddress(address) -> address.ToString()

type IpRule = {
    Value: IpRuleValue
    Action: RuleAction
}

type NetworkRuleSet = {
    Bypass: Set<NetworkRuleSetBypass>
    VirtualNetworkRules: VirtualNetworkRule list
    IpRules: IpRule list
    DefaultAction: RuleAction
}

/// Needed to build subnet resource ids for ACLs.
let private subnets = ResourceType("Microsoft.Network/virtualNetworks/subnets", "")

type StorageAccount = {
    Name: StorageAccountName
    Location: Location
    Sku: Sku
    Dependencies: ResourceId list
    StaticWebsite:
        {|
            IndexPage: string
            ErrorPage: string option
            ContentPath: string
        |} option
    EnableHierarchicalNamespace: bool option
    DefaultToOAuthAuthentication: FeatureFlag option
    DisablePublicNetworkAccess: FeatureFlag option
    DisableBlobPublicAccess: FeatureFlag option
    DisableSharedKeyAccess: FeatureFlag option
    DnsZoneType: string option
    ImmutableStorageWithVersioning:
        {|
            Enable: bool option
            ImmutabilityPolicy: {|
                AllowProtectedAppendWrites: bool option
                ImmutabilityPeriodSinceCreationInDays: int option
                State: ImmutabilityPolicyState option
            |} option
        |} option
    MinTlsVersion: TlsVersion option
    NetworkAcls: NetworkRuleSet option
    /// <remarks>Azure default is false</remarks>
    RequireInfrastructureEncryption: bool option
    SupportsHttpsTrafficOnly: FeatureFlag option
    Tags: Map<string, string>
} with

    interface IArmResource with
        member this.ResourceId = storageAccounts.resourceId this.Name.ResourceName

        member this.JsonModel = {|
            storageAccounts.Create(this.Name.ResourceName, this.Location, this.Dependencies, this.Tags) with
                sku = {|
                    name =
                        let performanceTier =
                            match this.Sku with
                            | GeneralPurpose(V1(V1Replication.LRS performanceTier))
                            | GeneralPurpose(V2(V2Replication.LRS performanceTier, _)) -> performanceTier.ArmValue
                            | Files _
                            | BlockBlobs _ -> "Premium"
                            | GeneralPurpose _
                            | Blobs _ -> "Standard"

                        let replicationModel =
                            match this.Sku with
                            | GeneralPurpose(V1 replication) -> replication.ReplicationModelDescription
                            | GeneralPurpose(V2(replication, _)) -> replication.ReplicationModelDescription
                            | Blobs(replication, _) -> replication.ReplicationModelDescription
                            | Files replication
                            | BlockBlobs replication -> replication.ReplicationModelDescription

                        $"{performanceTier}_{replicationModel}"
                |}
                kind =
                    match this.Sku with
                    | GeneralPurpose(V1 _) -> "Storage"
                    | GeneralPurpose(V2 _) -> "StorageV2"
                    | Blobs _ -> "BlobStorage"
                    | Files _ -> "FileStorage"
                    | BlockBlobs _ -> "BlockBlobStorage"
                extendedLocation = "" // TODO:
                identity = "" // TODO: user assigned identityt
                properties = {|
                    accessTier =
                        match this.Sku with
                        | Blobs(_, Some tier)
                        | GeneralPurpose(V2(_, Some tier)) -> tier.ArmValue
                        | _ -> null
                    networkAcls =
                        this.NetworkAcls
                        |> Option.map (fun networkRuleSet -> {|
                            bypass =
                                networkRuleSet.Bypass
                                |> Set.map NetworkRuleSetBypass.ArmValue
                                |> Set.toSeq
                                |> String.concat ","
                            virtualNetworkRules =
                                networkRuleSet.VirtualNetworkRules
                                |> List.map (fun rule -> {|
                                    id = subnets.resourceId(rule.VirtualNetwork, rule.Subnet).Eval()
                                    action = rule.Action.ArmValue
                                |})
                            ipRules =
                                networkRuleSet.IpRules
                                |> List.map (fun rule -> {|
                                    value = rule.Value.ArmValue
                                    action = rule.Action.ArmValue
                                |})
                            defaultAction = networkRuleSet.DefaultAction.ArmValue
                        |})
                        |> Option.defaultValue Unchecked.defaultof<_>
                    allowBlobPublicAccess = this.DisableBlobPublicAccess.BooleanValue
                    allowSharedKeyAccess = this.DisableSharedKeyAccess.BooleanValue
                    defaultToOAuthAuthentication = this.DefaultToOAuthAuthentication.BooleanValue
                    dnsEndpointType = this.DnsZoneType |> Option.toObj
                    encryption = {|
                        requireInfrastructureEncryption = this.RequireInfrastructureEncryption |> Option.toNullable
                    |}
                    immutableStorageWithVersioning =
                        this.ImmutableStorageWithVersioning
                        |> Option.map (fun immutableStorage -> {|
                            enable = immutableStorage.Enable |> Option.toNullable
                            policy =
                                immutableStorage.ImmutabilityPolicy
                                |> Option.map (fun immutableStorage ->
                                    {|
                                        allowProtectedAppendWrites = immutableStorage.AllowProtectedAppendWrites |> Option.toNullable
                                        immutabilityPeriodSinceCreationInDays = immutableStorage.ImmutabilityPeriodSinceCreationInDays |> Option.toNullable
                                        state = immutableStorage.State |> Option.map _.ArmValue |> Option.toObj
                                    |})
                        |})
                    isHnsEnabled = this.EnableHierarchicalNamespace |> Option.toNullable
                    minimumTlsVersion = this.MinTlsVersion.ArmValue ()
                    publicNetworkAccess = this.DisablePublicNetworkAccess.ArmValue ()
                    supportsHttpsTrafficOnly = this.SupportsHttpsTrafficOnly.BooleanValue
                |}
        |}

    interface IPostDeploy with
        member this.Run _ =
            this.StaticWebsite
            |> Option.map (fun staticWebsite -> result {
                let! enableStaticResponse =
                    Deploy.Az.enableStaticWebsite
                        this.Name.ResourceName.Value
                        staticWebsite.IndexPage
                        staticWebsite.ErrorPage

                printfn
                    $"Deploying content of %s{staticWebsite.ContentPath} folder to $web container for storage account %s{this.Name.ResourceName.Value}"

                let! uploadResponse =
                    Deploy.Az.batchUploadStaticWebsite this.Name.ResourceName.Value staticWebsite.ContentPath

                return enableStaticResponse + ", " + uploadResponse
            })

[<AutoOpen>]
module Extensions =
    type AllOrSpecific<'T> with

        member this.Emit(specificItemMapper: 'T -> string) =
            match this with
            | All -> [ "*" ]
            | Specific items -> [
                for item in items do
                    specificItemMapper item
              ]

/// A generic storage service that can be used for Blob, Table, Queue or FileServices
type StorageService = {
    StorageAccount: StorageResourceName
    CorsRules: CorsRule list
    Policies: Policy list
    IsVersioningEnabled: bool
    ResourceType: ResourceType
} with

    interface IArmResource with
        member this.ResourceId =
            this.ResourceType.resourceId (this.StorageAccount.ResourceName / "default")

        member this.JsonModel =
            let resolvePolicy (pol: Policy) =
                match pol with
                | DeleteRetention p
                | Restore p
                | ContainerDeleteRetention p -> {| enabled = p.Enabled; days = p.Days |} |> box
                | ChangeFeed p ->
                    {|
                        enabled = p.Enabled
                        retentionInDays = p.RetentionInDays
                    |}
                    |> box
                | LastAccessTimeTracking p ->
                    {|
                        enable = p.Enabled
                        name = "AccessTimeTracking"
                        trackingGranularityInDays = p.TrackingGranularityInDays
                        blobType = [| "blockBlob" |]
                    |}
                    |> box

            {|
                this.ResourceType.Create(
                    this.StorageAccount.ResourceName / "default",
                    dependsOn = [ storageAccounts.resourceId this.StorageAccount.ResourceName ]
                ) with
                    properties = {|
                        cors = {|
                            corsRules = [
                                for rule in this.CorsRules do
                                    {|
                                        allowedOrigins = rule.AllowedOrigins.Emit(fun r -> r.OriginalString)
                                        allowedMethods = [
                                            for httpMethod in rule.AllowedMethods.Value do
                                                httpMethod.ArmValue
                                        ]
                                        maxAgeInSeconds = rule.MaxAgeInSeconds
                                        exposedHeaders = rule.ExposedHeaders.Emit id
                                        allowedHeaders = rule.AllowedHeaders.Emit id
                                    |}
                            ]
                        |}
                        IsVersioningEnabled = this.IsVersioningEnabled
                        deleteRetentionPolicy =
                            this.Policies
                            |> List.tryFind (function
                                | DeleteRetention _ -> true
                                | _ -> false)
                            |> Option.map resolvePolicy
                            |> Option.defaultValue null
                        restorePolicy =
                            this.Policies
                            |> List.tryFind (function
                                | Restore _ -> true
                                | _ -> false)
                            |> Option.map resolvePolicy
                            |> Option.defaultValue null
                        containerDeleteRetentionPolicy =
                            this.Policies
                            |> List.tryFind (function
                                | ContainerDeleteRetention _ -> true
                                | _ -> false)
                            |> Option.map resolvePolicy
                            |> Option.defaultValue null
                        lastAccessTimeTrackingPolicy =
                            this.Policies
                            |> List.tryFind (function
                                | LastAccessTimeTracking _ -> true
                                | _ -> false)
                            |> Option.map resolvePolicy
                            |> Option.defaultValue null
                        changeFeed =
                            this.Policies
                            |> List.tryFind (function
                                | ChangeFeed _ -> true
                                | _ -> false)
                            |> Option.map resolvePolicy
                            |> Option.defaultValue null
                    |}
            |}

module BlobServices =
    type Container = {
        Name: StorageResourceName
        StorageAccount: ResourceName
        Accessibility: StorageContainerAccess
    } with

        interface IArmResource with
            member this.ResourceId =
                containers.resourceId (this.StorageAccount / "default" / this.Name.ResourceName)

            member this.JsonModel = {|
                containers.Create(
                    this.StorageAccount / "default" / this.Name.ResourceName,
                    dependsOn = [ storageAccounts.resourceId this.StorageAccount ]
                ) with
                    properties = {|
                        publicAccess =
                            match this.Accessibility with
                            | Private -> "None"
                            | Container -> "Container"
                            | Blob -> "Blob"
                    |}
            |}

module FileShares =
    type FileShare = {
        Name: StorageResourceName
        ShareQuota: int<Gb> option
        StorageAccount: ResourceName
    } with

        interface IArmResource with
            member this.ResourceId =
                fileShares.resourceId (this.StorageAccount / "default" / this.Name.ResourceName)

            member this.JsonModel = {|
                fileShares.Create(
                    this.StorageAccount / "default" / this.Name.ResourceName,
                    dependsOn = [ storageAccounts.resourceId this.StorageAccount ]
                ) with
                    properties = {|
                        shareQuota = this.ShareQuota |> Option.defaultValue 5120<Gb>
                    |}
            |}

module Tables =
    type Table = {
        Name: StorageResourceName
        StorageAccount: ResourceName
    } with

        interface IArmResource with
            member this.ResourceId =
                tables.resourceId (this.StorageAccount / "default" / this.Name.ResourceName)

            member this.JsonModel =
                tables.Create(
                    this.StorageAccount / "default" / this.Name.ResourceName,
                    dependsOn = [ storageAccounts.resourceId this.StorageAccount ]
                )

module Queues =
    type Queue = {
        Name: StorageResourceName
        Metadata: Metadata option
        StorageAccount: ResourceName
    } with

        interface IArmResource with
            member this.ResourceId =
                queues.resourceId (this.StorageAccount / "default" / this.Name.ResourceName)

            member this.JsonModel =
                let queue =
                    queues.Create(
                        this.StorageAccount / "default" / this.Name.ResourceName,
                        dependsOn = [ storageAccounts.resourceId this.StorageAccount ]
                    )

                match this.Metadata with
                | Some m -> {|
                    queue with
                        properties = box {| metadata = m |}
                  |}
                | None -> queue

module ManagementPolicies =
    type ManagementPolicy = {
        Rules:
            {|
                Name: ResourceName
                CoolBlobAfter: int<Days> option
                ArchiveBlobAfter: int<Days> option
                DeleteBlobAfter: int<Days> option
                DeleteSnapshotAfter: int<Days> option
                Filters: string list
            |} list
        StorageAccount: ResourceName
    } with

        member this.ResourceName = this.StorageAccount / "default"

        interface IArmResource with
            member this.ResourceId = managementPolicies.resourceId this.ResourceName

            member this.JsonModel = {|
                managementPolicies.Create(
                    this.ResourceName,
                    dependsOn = [ storageAccounts.resourceId this.StorageAccount ]
                ) with
                    properties = {|
                        policy = {|
                            rules = [
                                for rule in this.Rules do
                                    {|
                                        enabled = true
                                        name = rule.Name.Value
                                        ``type`` = "Lifecycle"
                                        definition = {|
                                            actions = {|
                                                baseBlob = {|
                                                    tierToCool =
                                                        rule.CoolBlobAfter
                                                        |> Option.map (fun days ->
                                                            {|
                                                                daysAfterModificationGreaterThan = days
                                                            |}
                                                            |> box)
                                                        |> Option.toObj
                                                    tierToArchive =
                                                        rule.ArchiveBlobAfter
                                                        |> Option.map (fun days ->
                                                            {|
                                                                daysAfterModificationGreaterThan = days
                                                            |}
                                                            |> box)
                                                        |> Option.toObj
                                                    delete =
                                                        rule.DeleteBlobAfter
                                                        |> Option.map (fun days ->
                                                            {|
                                                                daysAfterModificationGreaterThan = days
                                                            |}
                                                            |> box)
                                                        |> Option.toObj
                                                |}
                                                snapshot =
                                                    rule.DeleteSnapshotAfter
                                                    |> Option.map (fun days ->
                                                        {|
                                                            delete = {|
                                                                daysAfterCreationGreaterThan = days
                                                            |}
                                                        |}
                                                        |> box)
                                                    |> Option.toObj
                                            |}
                                            filters = {|
                                                blobTypes = [ "blockBlob" ]
                                                prefixMatch = rule.Filters
                                            |}
                                        |}
                                    |}
                            ]
                        |}
                    |}
            |}
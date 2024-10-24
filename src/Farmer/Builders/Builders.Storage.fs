[<AutoOpen>]
module Farmer.Builders.Storage

open Farmer
open Farmer.Builders
open Farmer.Storage
open Farmer.Arm.RoleAssignment
open Farmer.Arm.Storage
open BlobServices
open BlobContainers
open FileShares

type StorageAccount =
    /// Gets an ARM Expression connection string for any Storage Account.
    static member getConnectionString(storageAccount: ResourceId) =
        let expr =
            $"concat('DefaultEndpointsProtocol=https;AccountName={storageAccount.Name.Value};AccountKey=', listKeys({storageAccount.ArmExpression.Value}, '2017-10-01').keys[0].value, ';EndpointSuffix=', environment().suffixes.storage)"

        ArmExpression.create (expr, storageAccount)

    /// Gets an ARM Expression connection string for any Storage Account.
    static member getConnectionString(storageAccountName: StorageAccountName, ?group) =
        let resourceId =
            ResourceId.create (storageAccounts, storageAccountName.ResourceName, ?group = group)

        StorageAccount.getConnectionString(resourceId).WithOwner(resourceId)

type StoragePolicy = {
    CoolBlobAfter: int<Days> option
    ArchiveBlobAfter: int<Days> option
    DeleteBlobAfter: int<Days> option
    DeleteSnapshotAfter: int<Days> option
    Filters: string list
}

[<Struct>]
type StorageQueueConfig = {
    Name: StorageResourceName
    Metadata: Metadata option
}

[<Struct>]
type BlobContainerImmutabilityPoliciesConfig = {
    AllowProtectedAppendWrites: AllowProtectedAppendWrites option
    ImmutabilityPeriodSinceCreation: int<Days> option
}

type StorageAccountConfig = {
    /// The name of the storage account.
    Name: StorageAccountName
    /// The sku of the storage account.
    Sku: Sku
    /// Whether to enable Data Lake Storage Gen2.
    EnableDataLake: bool option
    /// Containers for the storage account.
    Containers: (StorageResourceName * StorageContainerAccess * BlobContainerImmutabilityPoliciesConfig option) list
    /// File shares
    FileShares: (StorageResourceName * int<Gb> option) list
    /// Queues
    Queues: StorageQueueConfig list
    /// Network Access Control Lists
    NetworkAcls: NetworkRuleSet option
    /// Tables
    Tables: StorageResourceName Set
    /// Rules
    Rules: Map<ResourceName, StoragePolicy>
    RoleAssignments: Roles.RoleAssignment Set
    /// Static Website Settings
    StaticWebsite:
        {|
            IndexPage: string
            ContentPath: string
            ErrorPage: string option
        |} option
    /// The CORS rules for a storage service
    CorsRules: List<Storage.StorageService * CorsRule>
    /// The Policies for a storage service
    Policies: List<Storage.StorageService * Policy list>
    /// Versioning enable information for a storage service
    IsVersioningEnabled: List<Storage.StorageService * bool>
    /// Minimum TLS version
    MinTlsVersion: TlsVersion option
    /// Supports Https Traffic Only
    SupportsHttpsTrafficOnly: FeatureFlag option
    /// Tags to apply to the storage account
    Tags: Map<string, string>
    /// DNS endpoint type
    DnsZoneType: string option
    /// Disable Public Network Acccess
    DisablePublicNetworkAccess: FeatureFlag option
    /// Disable blob public access
    DisableBlobPublicAccess: FeatureFlag option
    /// Disable Shared Key Access
    DisableSharedKeyAccess: FeatureFlag option
    /// Default to Azure Active Directory authorization in the Azure portal
    DefaultToOAuthAuthentication: FeatureFlag option
    /// <summary>Enable infrastructure encryption in addition to data encryption</summary>
    /// <remarks>Azure default is false</remarks>
    RequireInfrastructureEncryption: bool option
} with

    /// Gets the ARM expression path to the key of this storage account.
    member this.Key = StorageAccount.getConnectionString (this.Name)

    /// Gets the Primary endpoint for static website (if enabled)
    member this.WebsitePrimaryEndpoint =
        ArmExpression
            .reference(storageAccounts, this.ResourceId)
            .Map(sprintf "%s.primaryEndpoints.web")

    member this.WebsitePrimaryEndpointHost =
        this.WebsitePrimaryEndpoint.Map(fun uri -> $"replace(replace({uri}, 'https://', ''), '/', '')")

    member this.Endpoint = $"{this.Name.ResourceName.Value}.blob.core.windows.net"
    member this.ResourceId = storageAccounts.resourceId this.Name.ResourceName

    interface IBuilder with
        member this.ResourceId = this.ResourceId

        member this.BuildResources location = [
            {
                Name = this.Name
                Location = location
                Sku = this.Sku
                Dependencies =
                    this.RoleAssignments
                    |> Seq.choose (fun roleAssignment -> roleAssignment.Principal.ArmExpression.Owner)
                    |> Seq.append (
                        match this.NetworkAcls with
                        | Some acl ->
                            acl.VirtualNetworkRules
                            |> Seq.map (fun r -> r.VirtualNetwork)
                            |> Seq.distinct
                            |> Seq.map Arm.Network.virtualNetworks.resourceId
                        | None -> Seq.empty
                    )
                    |> Seq.toList
                DefaultToOAuthAuthentication = this.DefaultToOAuthAuthentication
                DisablePublicNetworkAccess = this.DisablePublicNetworkAccess
                DisableBlobPublicAccess = this.DisableBlobPublicAccess
                DisableSharedKeyAccess = this.DisableSharedKeyAccess
                DnsZoneType = this.DnsZoneType
                EnableHierarchicalNamespace = this.EnableDataLake
                ImmutableStorageWithVersioning = None // TODO: Implement
                MinTlsVersion = this.MinTlsVersion
                NetworkAcls = this.NetworkAcls
                RequireInfrastructureEncryption = this.RequireInfrastructureEncryption
                StaticWebsite = this.StaticWebsite
                SupportsHttpsTrafficOnly = this.SupportsHttpsTrafficOnly
                Tags = this.Tags
            }
            for name, access, immutabilityPolicies in this.Containers do
                {
                    Name = name
                    StorageAccount = this.Name.ResourceName
                    Accessibility = access
                }

                match immutabilityPolicies with
                | None -> ()
                | Some immutabilityPolicies -> {
                    StorageAccount = this.Name.ResourceName
                    Container = name
                    AllowProtectedAppendWrites = immutabilityPolicies.AllowProtectedAppendWrites
                    ImmutabilityPeriodSinceCreation = immutabilityPolicies.ImmutabilityPeriodSinceCreation
                  }
            for (name, shareQuota) in this.FileShares do
                {
                    Name = name
                    ShareQuota = shareQuota
                    StorageAccount = this.Name.ResourceName
                }
            for queue in this.Queues do
                {
                    Queues.Queue.Name = queue.Name
                    Queues.Queue.Metadata = queue.Metadata
                    Queues.Queue.StorageAccount = this.Name.ResourceName
                }
            for table in this.Tables do
                {
                    Tables.Table.Name = table
                    Tables.Table.StorageAccount = this.Name.ResourceName
                }
            match this.Rules |> Map.toList with
            | [] -> ()
            | rules -> {
                ManagementPolicies.ManagementPolicy.StorageAccount = this.Name.ResourceName
                ManagementPolicies.ManagementPolicy.Rules = [
                    for name, rule in rules do
                        {| rule with Name = name |}
                ]
              }
            for roleAssignment in this.RoleAssignments do
                let uniqueName =
                    $"{this.Name.ResourceName.Value}{roleAssignment.Principal.ArmExpression.Value}{roleAssignment.Role.Id}"
                    |> DeterministicGuid.create
                    |> string
                    |> ResourceName

                {
                    Name = uniqueName
                    RoleDefinitionId = roleAssignment.Role
                    PrincipalId = roleAssignment.Principal
                    PrincipalType = PrincipalType.ServicePrincipal
                    Scope = SpecificResource this.ResourceId
                    Dependencies =
                        Set [
                            ResourceId.create (storageAccounts, this.Name.ResourceName)
                            yield! roleAssignment.Owner |> Option.toList
                        ]
                }

            let storageResourceName = StorageResourceName.Create(this.Name.ResourceName).OkValue

            let rules = this.CorsRules |> List.groupBy fst
            let versioning = this.IsVersioningEnabled |> List.groupBy fst
            let policies = this.Policies |> List.groupBy fst

            let allSvcs =
                rules
                |> List.map fst
                |> (@) (versioning |> List.map fst)
                |> (@) (policies |> List.map fst)
                |> List.distinct

            for svc in allSvcs do
                {
                    ResourceType =
                        match svc with
                        | StorageService.Blobs -> blobServices
                        | StorageService.Queues -> queueServices
                        | StorageService.Tables -> tableServices
                        | StorageService.Files -> fileServices
                    StorageAccount = storageResourceName
                    CorsRules = this.CorsRules |> List.filter (fst >> (=) svc) |> List.map (fun (_, s) -> s)
                    Policies =
                        this.Policies
                        |> List.filter (fst >> (=) svc)
                        |> List.map (fun (_, s) -> s)
                        |> List.collect id
                    IsVersioningEnabled =
                        this.IsVersioningEnabled
                        |> List.filter (fst >> (=) svc)
                        |> List.forall (fun (_, ive) -> ive = true)
                }
        ]

type StorageAccountBuilder() =
    member _.Yield _ = {
        Name = StorageAccountName.Empty
        Sku = Sku.Standard_LRS
        EnableDataLake = None
        Containers = []
        FileShares = []
        Rules = Map.empty
        Queues = List.empty
        NetworkAcls = None
        Tables = Set.empty
        RoleAssignments = Set.empty
        StaticWebsite = None
        CorsRules = []
        Policies = []
        IsVersioningEnabled = []
        MinTlsVersion = None
        SupportsHttpsTrafficOnly = None
        Tags = Map.empty
        DnsZoneType = None
        DisablePublicNetworkAccess = None
        DisableBlobPublicAccess = None
        DisableSharedKeyAccess = None
        DefaultToOAuthAuthentication = None
        RequireInfrastructureEncryption = None
    }

    member _.Run state =
        if state.Name.ResourceName = ResourceName.Empty then
            raiseFarmer "No Storage Account name has been set."

        state

    static member private AddContainers
        (state, access, names: string seq, ?immutabilityPolicies: BlobContainerImmutabilityPoliciesConfig)
        =
        {
            state with
                Containers =
                    let containers =
                        names
                        |> List.ofSeq
                        |> List.map (fun name ->
                            ((StorageResourceName.Create name).OkValue, access, immutabilityPolicies))

                    state.Containers @ containers
        }

    static member private AddContainer
        (state, access, name: string, ?immutabilityPolicies: BlobContainerImmutabilityPoliciesConfig)
        =
        StorageAccountBuilder.AddContainers(state, access, [ name ], ?immutabilityPolicies = immutabilityPolicies)

    static member private AddFileShares(state: StorageAccountConfig, names: string seq, quota) = {
        state with
            FileShares =
                let fileShares =
                    names
                    |> List.ofSeq
                    |> List.map (fun name -> (StorageResourceName.Create(name).OkValue, quota))

                state.FileShares @ fileShares
    }

    static member private AddFileShare(state: StorageAccountConfig, name: string, quota) =
        StorageAccountBuilder.AddFileShares(state, [ name ], quota)

    /// Sets the name of the storage account.
    [<CustomOperation "name">]
    member _.Name(state: StorageAccountConfig, name: ResourceName) = {
        state with
            Name = StorageAccountName.Create(name).OkValue
    }

    member this.Name(state: StorageAccountConfig, name) = this.Name(state, ResourceName name)

    /// Sets the sku of the storage account.
    [<CustomOperation "sku">]
    member _.Sku(state: StorageAccountConfig, sku) = { state with Sku = sku }

    /// Adds private container.
    [<CustomOperation "add_private_container">]
    member _.AddPrivateContainer
        (state: StorageAccountConfig, name, ?immutabilityPolicies: BlobContainerImmutabilityPoliciesConfig)
        =
        StorageAccountBuilder.AddContainer(state, Private, name, ?immutabilityPolicies = immutabilityPolicies)

    /// Adds private containers.
    [<CustomOperation "add_private_containers">]
    member _.AddPrivateContainers
        (state: StorageAccountConfig, names, ?immutabilityPolicies: BlobContainerImmutabilityPoliciesConfig)
        =
        StorageAccountBuilder.AddContainers(state, Private, names, ?immutabilityPolicies = immutabilityPolicies)

    /// Adds container with anonymous read access for blobs and containers.
    [<CustomOperation "add_public_container">]
    member _.AddPublicContainer
        (state: StorageAccountConfig, name, ?immutabilityPolicies: BlobContainerImmutabilityPoliciesConfig)
        =
        StorageAccountBuilder.AddContainer(state, Container, name, ?immutabilityPolicies = immutabilityPolicies)

    /// Adds containers with anonymous read access for blobs and containers.
    [<CustomOperation "add_public_containers">]
    member _.AddPublicContainers
        (state: StorageAccountConfig, names, ?immutabilityPolicies: BlobContainerImmutabilityPoliciesConfig)
        =
        StorageAccountBuilder.AddContainers(state, Container, names, ?immutabilityPolicies = immutabilityPolicies)

    /// Adds container with anonymous read access for blobs only.
    [<CustomOperation "add_blob_container">]
    member _.AddBlobContainer
        (state: StorageAccountConfig, name, ?immutabilityPolicies: BlobContainerImmutabilityPoliciesConfig)
        =
        StorageAccountBuilder.AddContainer(state, Blob, name, ?immutabilityPolicies = immutabilityPolicies)

    /// Adds containers with anonymous read access for blobs only.
    [<CustomOperation "add_blob_containers">]
    member _.AddBlobContainers
        (state: StorageAccountConfig, names, ?immutabilityPolicies: BlobContainerImmutabilityPoliciesConfig)
        =
        StorageAccountBuilder.AddContainers(state, Blob, names, ?immutabilityPolicies = immutabilityPolicies)

    /// Adds a file share with no quota.
    [<CustomOperation "add_file_share">]
    member _.AddFileShare(state: StorageAccountConfig, name) =
        StorageAccountBuilder.AddFileShare(state, name, None)

    /// Adds file shares with no quota.
    [<CustomOperation "add_file_shares">]
    member _.AddFileShares(state: StorageAccountConfig, names) =
        StorageAccountBuilder.AddFileShares(state, names, None)

    /// Adds a file share with specified quota.
    [<CustomOperation "add_file_share_with_quota">]
    member _.AddFileShareWithQuota(state: StorageAccountConfig, name: string, quota) =
        StorageAccountBuilder.AddFileShare(state, name, Some quota)

    /// Adds file shares with specified quota.
    [<CustomOperation "add_file_shares_with_quota">]
    member _.AddFileSharesWithQuota(state: StorageAccountConfig, names: string list, quota) =
        StorageAccountBuilder.AddFileShares(state, names, Some quota)

    /// Adds a single queue to the storage account.
    [<CustomOperation "add_queue">]
    member this.AddQueue(state: StorageAccountConfig, queue: StorageQueueConfig) = this.AddQueues(state, [ queue ])

    /// Adds a single queue to the storage account.
    [<CustomOperation "add_queue">]
    member this.AddQueue(state: StorageAccountConfig, name: string) = this.AddQueues(state, [ name ])

    /// Adds a set of queues to the storage account.
    [<CustomOperation "add_queues">]
    member _.AddQueues(state: StorageAccountConfig, names: string seq) = {
        state with
            Queues =
                let queues =
                    names
                    |> List.ofSeq
                    |> List.map (fun name -> {
                        Name = StorageResourceName.Create(name).OkValue
                        Metadata = None
                    })

                state.Queues @ queues
    }

    /// Adds a set of queues to the storage account.
    [<CustomOperation "add_queues">]
    member _.AddQueues(state: StorageAccountConfig, queues: StorageQueueConfig seq) = {
        state with
            Queues = state.Queues @ List.ofSeq queues
    }

    /// Adds a set of queues to the storage account with the same metadata.
    [<CustomOperation "add_queues">]
    member this.AddQueues
        (state: StorageAccountConfig, queues: StorageQueueConfig seq, metadata: (string * string) list)
        =
        let qs =
            queues
            |> Seq.map (fun queue -> {
                queue with
                    Metadata = Some(metadata |> Map.ofSeq)
            })

        (state, qs) ||> Seq.fold (fun state queue -> this.AddQueue(state, queue))

    /// Adds a single table to the storage account.
    [<CustomOperation "add_table">]
    member _.AddTable(state: StorageAccountConfig, name: string) = {
        state with
            Tables = state.Tables.Add(StorageResourceName.Create(name).OkValue)
    }

    /// Adds a set of tables to the storage account.
    [<CustomOperation "add_tables">]
    member this.AddTables(state: StorageAccountConfig, names) =
        (state, names) ||> Seq.fold (fun state name -> this.AddTable(state, name))

    /// Enable static website support, using the supplied local content path to the storage account's $web folder as a post-deployment task, and setting the index page as supplied.
    [<CustomOperation "use_static_website">]
    member _.StaticWebsite(state: StorageAccountConfig, contentPath, indexPage) = {
        state with
            StaticWebsite =
                Some {|
                    IndexPage = indexPage
                    ErrorPage = None
                    ContentPath = contentPath
                |}
    }

    /// Sets the error page for the static website.
    [<CustomOperation "static_website_error_page">]
    member _.StaticWebsiteErrorPage(state: StorageAccountConfig, errorPage) = {
        state with
            StaticWebsite =
                state.StaticWebsite
                |> Option.map (fun staticWebsite -> {|
                    staticWebsite with
                        ErrorPage = Some errorPage
                |})
    }

    /// Enables support for hierarchical namespace, also known as Data Lake Storage Gen2.
    [<CustomOperation "enable_data_lake">]
    member _.UseHns(state: StorageAccountConfig, value) = {
        state with
            EnableDataLake = Some value
    }

    /// Adds tags to the storage account
    /// Adds a lifecycle rule
    [<CustomOperation "add_lifecycle_rule">]
    member _.AddLifecycleRule(state: StorageAccountConfig, ruleName, actions, filters) =
        let rule = {
            Filters = filters
            CoolBlobAfter =
                actions
                |> List.tryPick (function
                    | CoolAfter days -> Some days
                    | _ -> None)
            ArchiveBlobAfter =
                actions
                |> List.tryPick (function
                    | ArchiveAfter days -> Some days
                    | _ -> None)
            DeleteBlobAfter =
                actions
                |> List.tryPick (function
                    | DeleteAfter days -> Some days
                    | _ -> None)
            DeleteSnapshotAfter =
                actions
                |> List.tryPick (function
                    | DeleteSnapshotAfter days -> Some days
                    | _ -> None)
        }

        {
            state with
                Rules = state.Rules.Add(ResourceName ruleName, rule)
        }

    static member private GrantAccess(state: StorageAccountConfig, assignment) = {
        state with
            RoleAssignments = state.RoleAssignments.Add assignment
    }

    [<CustomOperation "grant_access">]
    member _.GrantAccess(state: StorageAccountConfig, principalId: PrincipalId, role) =
        StorageAccountBuilder.GrantAccess(
            state,
            {
                Principal = principalId
                Role = role
                Owner = None
            }
        )

    member _.GrantAccess(state: StorageAccountConfig, identity: UserAssignedIdentityConfig, role) =
        StorageAccountBuilder.GrantAccess(
            state,
            {
                Principal = identity.PrincipalId
                Role = role
                Owner = Some identity.ResourceId
            }
        )

    member _.GrantAccess(state: StorageAccountConfig, identity: Identity.SystemIdentity, role) =
        StorageAccountBuilder.GrantAccess(
            state,
            {
                Principal = identity.PrincipalId
                Role = role
                Owner = Some identity.ResourceId
            }
        )

    [<CustomOperation "default_blob_access_tier">]
    member _.SetDefaultAccessTier(state: StorageAccountConfig, tier) = {
        state with
            Sku =
                match state.Sku with
                | Blobs(replication, _) -> Blobs(replication, Some tier)
                | GeneralPurpose(V2(replication, _)) -> GeneralPurpose(V2(replication, Some tier))
                | other ->
                    raiseFarmer
                        $"You can only set the default access tier for Blobs or General Purpose V2 storage accounts. This account is %A{other}."
    }

    /// Specify network access control lists for this storage account.
    [<CustomOperation "set_network_acls">]
    member _.SetNetworkAcls(state: StorageAccountConfig, networkAcls) = {
        state with
            NetworkAcls = Some networkAcls
    }

    /// Restrict access to this storage account to a subnet on a virtual network.
    [<CustomOperation "restrict_to_subnet">]
    member _.RestrictToSubnet(state: StorageAccountConfig, vnet: string, subnet: string) =
        let allowVnet = {
            Subnet = ResourceName subnet
            VirtualNetwork = ResourceName vnet
            Action = RuleAction.Allow
        }

        match state.NetworkAcls with
        | None -> {
            state with
                NetworkAcls =
                    {
                        Bypass = set [ NetworkRuleSetBypass.AzureServices ]
                        VirtualNetworkRules = [ allowVnet ]
                        IpRules = []
                        DefaultAction = RuleAction.Deny
                    }
                    |> Some
          }
        | Some existingAcl -> {
            state with
                NetworkAcls =
                    {
                        existingAcl with
                            VirtualNetworkRules = allowVnet :: existingAcl.VirtualNetworkRules
                    }
                    |> Some
          }

    /// Restrict access to this storage account to a IP address network prefix.
    [<CustomOperation "restrict_to_prefix">]
    member _.RestrictToPrefix(state: StorageAccountConfig, cidr: string) =
        let allowIp = {
            Value = IpRulePrefix(IPAddressCidr.parse cidr)
            Action = RuleAction.Allow
        }

        match state.NetworkAcls with
        | None -> {
            state with
                NetworkAcls =
                    {
                        Bypass = set [ NetworkRuleSetBypass.AzureServices ]
                        VirtualNetworkRules = []
                        IpRules = [ allowIp ]
                        DefaultAction = RuleAction.Deny
                    }
                    |> Some
          }
        | Some existingAcl -> {
            state with
                NetworkAcls =
                    {
                        existingAcl with
                            IpRules = allowIp :: existingAcl.IpRules
                    }
                    |> Some
          }

    /// Restrict access to this storage account to an IP address.
    [<CustomOperation "restrict_to_ip">]
    member this.RestrictToIp(state: StorageAccountConfig, ip: string) =
        let allowIp = {
            Value = IpRuleAddress(System.Net.IPAddress.Parse ip)
            Action = RuleAction.Allow
        }

        match state.NetworkAcls with
        | None -> {
            state with
                NetworkAcls =
                    {
                        Bypass = set [ NetworkRuleSetBypass.AzureServices ]
                        VirtualNetworkRules = []
                        IpRules = [ allowIp ]
                        DefaultAction = RuleAction.Deny
                    }
                    |> Some
          }
        | Some existingAcl -> {
            state with
                NetworkAcls =
                    {
                        existingAcl with
                            IpRules = allowIp :: existingAcl.IpRules
                    }
                    |> Some
          }

    [<CustomOperation "restrict_to_ips">]
    member this.RestrictToIps(state: StorageAccountConfig, ips: string list) =
        let allowIps =
            ips
            |> List.map (fun ip -> {
                Value = IpRuleAddress(System.Net.IPAddress.Parse ip)
                Action = RuleAction.Allow
            })

        match state.NetworkAcls with
        | None -> {
            state with
                NetworkAcls =
                    {
                        Bypass = set [ NetworkRuleSetBypass.AzureServices ]
                        VirtualNetworkRules = []
                        IpRules = allowIps
                        DefaultAction = RuleAction.Deny
                    }
                    |> Some
          }
        | Some existingAcl -> {
            state with
                NetworkAcls =
                    {
                        existingAcl with
                            IpRules = allowIps @ existingAcl.IpRules
                    }
                    |> Some
          }

    /// Restrict access to this storage account to the private endpoints and azure services.
    [<CustomOperation "restrict_to_azure_services">]
    member _.RestrictToAzureServices(state: StorageAccountConfig, bypass: NetworkRuleSetBypass list) =
        match state.NetworkAcls with
        | None -> {
            state with
                DisablePublicNetworkAccess = Some FeatureFlag.Disabled
                NetworkAcls =
                    {
                        Bypass = set bypass
                        VirtualNetworkRules = []
                        IpRules = []
                        DefaultAction = RuleAction.Deny
                    }
                    |> Some
          }
        | Some existingAcl -> {
            state with
                DisablePublicNetworkAccess = Some FeatureFlag.Disabled
                NetworkAcls =
                    {
                        existingAcl with
                            Bypass = Set.union (set bypass) existingAcl.Bypass
                    }
                    |> Some
          }

    /// Adds a set of CORS rules to the storage account.
    [<CustomOperation "add_cors_rules">]
    member _.AddCorsRules(state: StorageAccountConfig, rules) = {
        state with
            CorsRules = state.CorsRules @ rules
    }

    /// Adds a set of policies to the storage account.
    [<CustomOperation "add_policies">]
    member _.AddPolicies(state: StorageAccountConfig, policies) = {
        state with
            Policies = state.Policies @ policies
    }

    /// Adds a versioning enabled rule to the storage account.
    [<CustomOperation "enable_versioning">]
    member _.EnableVersioning(state: StorageAccountConfig, enableVersioning) = {
        state with
            IsVersioningEnabled = state.IsVersioningEnabled @ enableVersioning
    }

    /// Set minimum TLS version
    [<CustomOperation "min_tls_version">]
    member _.SetMinTlsVersion(state: StorageAccountConfig, minTlsVersion) = {
        state with
            MinTlsVersion = Some minTlsVersion
    }

    /// Set support https traffic only
    [<CustomOperation "supports_https_traffic_only">]
    member _.SupportsHttpsTrafficOnly(state: StorageAccountConfig, ?supportsHttpsTrafficOnly: FeatureFlag) =
        let flag = defaultArg supportsHttpsTrafficOnly FeatureFlag.Enabled

        {
            state with
                SupportsHttpsTrafficOnly = Some flag
        }

    /// Set DNS Endpoint type
    [<CustomOperation "use_azure_dns_zone">]
    member _.SetDnsEndpointType(state: StorageAccountConfig) = {
        state with
            DnsZoneType = Some "AzureDnsZone"
    }

    /// Disable public network access, all access must be through a private endpoint.
    [<CustomOperation "disable_public_network_access">]
    member _.DisablePublicNetworkAccess(state: StorageAccountConfig) = {
        state with
            DisablePublicNetworkAccess = Some FeatureFlag.Enabled
            NetworkAcls =
                {
                    Bypass = set [ NetworkRuleSetBypass.None ]
                    VirtualNetworkRules = []
                    IpRules = []
                    DefaultAction = RuleAction.Deny
                }
                |> Some
    }

    /// Disable blob public access
    [<CustomOperation "disable_blob_public_access">]
    member _.DisableBlobPublicAccess(state: StorageAccountConfig, ?flag: FeatureFlag) =
        let flag = defaultArg flag FeatureFlag.Enabled

        {
            state with
                DisableBlobPublicAccess = Some flag
        }

    /// Disable shared key access
    [<CustomOperation "disable_shared_key_access">]
    member _.DisableSharedKeyAccess(state: StorageAccountConfig, ?flag: FeatureFlag) =
        let flag = defaultArg flag FeatureFlag.Enabled

        {
            state with
                DisableSharedKeyAccess = Some flag
        }

    /// Default to Azure Active Directory authorization in the Azure portal
    [<CustomOperation "default_to_oauth_authentication">]
    member _.DefaultToOAuthAuthentication(state: StorageAccountConfig, ?flag: FeatureFlag) =
        let flag = defaultArg flag FeatureFlag.Enabled

        {
            state with
                DefaultToOAuthAuthentication = Some flag
        }

    interface ITaggable<StorageAccountConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

/// Allow adding storage accounts directly to CDNs
type EndpointBuilder with

    member this.Origin(state: EndpointConfig, storage: StorageAccountConfig) =
        let state = this.Origin(state, storage.Endpoint)
        this.DependsOn(state, storage.ResourceId)

type StorageQueueBuilder() =
    member _.Yield _ = {
        Name = StorageResourceName.Empty
        Metadata = Some(Map.empty)
    }

    member _.Run state =
        if state.Name.ResourceName = ResourceName.Empty then
            raiseFarmer "No Storage Account name has been set."

        state

    /// Sets the name of the storage queue.
    [<CustomOperation "name">]
    member _.Name(state: StorageQueueConfig, name: string) = {
        state with
            Name = StorageResourceName.Create(ResourceName name).OkValue
    }

    /// Sets the name of the storage account.
    [<CustomOperation "metadata">]
    member _.Name(state: StorageQueueConfig, metadata: (string * string) list) =
        let m = metadata |> Map.ofList
        { state with Metadata = Some(m) }

type BlobContainerImmutabilityPoliciesBuilder() =
    member _.Yield _ = {
        AllowProtectedAppendWrites = None
        ImmutabilityPeriodSinceCreation = None
    }

    member _.Run state : BlobContainerImmutabilityPoliciesConfig = state

    /// Sets the AllowProtectedAppendWrites property.
    [<CustomOperation "allow_protected_append_writes">]
    member _.AllowProtectedAppendWrites(state: BlobContainerImmutabilityPoliciesConfig, value) = {
        state with
            AllowProtectedAppendWrites = Some value
    }

    /// Sets the ImmutabilityPeriodSinceCreation property.
    [<CustomOperation "immutability_period_since_creation">]
    member _.ImmutabilityPeriodSinceCreation(state: BlobContainerImmutabilityPoliciesConfig, value) = {
        state with
            ImmutabilityPeriodSinceCreation = Some value
    }


let storageAccount = StorageAccountBuilder()
let storageQueue = StorageQueueBuilder()
let blobContainerImmutabilityPolicies = BlobContainerImmutabilityPoliciesBuilder()
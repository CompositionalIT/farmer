[<AutoOpen>]
module Farmer.Builders.Storage

open Farmer
open Farmer.Storage
open Farmer.Arm.RoleAssignment
open Farmer.Arm.Storage
open BlobServices
open FileShares

type StorageAccount =
    /// Gets an ARM Expression connection string for any Storage Account.
    static member getConnectionString (storageAccount:ResourceId) =
        let expr = $"concat('DefaultEndpointsProtocol=https;AccountName={storageAccount.Name.Value};AccountKey=', listKeys({storageAccount.ArmExpression.Value}, '2017-10-01').keys[0].value)"
        ArmExpression.create(expr, storageAccount)
    /// Gets an ARM Expression connection string for any Storage Account.
    static member getConnectionString (storageAccountName:StorageAccountName, ?group) =
        StorageAccount.getConnectionString (ResourceId.create (storageAccounts, storageAccountName.ResourceName, ?group = group))

type StoragePolicy =
    { CoolBlobAfter : int<Days> option
      ArchiveBlobAfter : int<Days> option
      DeleteBlobAfter : int<Days> option
      DeleteSnapshotAfter : int<Days> option
      Filters : string list }

type StorageAccountConfig =
    { /// The name of the storage account.
      Name : StorageAccountName
      /// The sku of the storage account.
      Sku : Sku
      /// Whether to enable Data Lake Storage Gen2.
      EnableDataLake : bool option
      /// Containers for the storage account.
      Containers : (StorageResourceName * StorageContainerAccess) list
      /// File shares
      FileShares: (StorageResourceName * int<Gb> option) list
      /// Queues
      Queues : StorageResourceName Set
      /// Network Access Control Lists
      NetworkAcls : NetworkRuleSet option
      /// Tables
      Tables : StorageResourceName Set
      /// Rules
      Rules : Map<ResourceName, StoragePolicy>
      RoleAssignments : Roles.RoleAssignment Set
      /// Static Website Settings
      StaticWebsite : {| IndexPage : string; ContentPath : string; ErrorPage : string option |} option
      /// The CORS rules for a storage service
      CorsRules : List<Storage.StorageService * CorsRule>
      /// The Policies for a storage service
      Policies : List<Storage.StorageService * Policy list>
      /// Versioning enable information for a storage service
      IsVersioningEnabled : List<Storage.StorageService * bool>
      /// Minimum TLS version
      MinTlsVersion : TlsVersion option
      /// Tags to apply to the storage account
      Tags: Map<string,string> }
    /// Gets the ARM expression path to the key of this storage account.
    member this.Key = StorageAccount.getConnectionString this.Name
    /// Gets the Primary endpoint for static website (if enabled)
    member this.WebsitePrimaryEndpoint =
        ArmExpression
            .reference(storageAccounts, this.ResourceId)
            .Map(sprintf "%s.primaryEndpoints.web")
    member this.WebsitePrimaryEndpointHost =
        this.WebsitePrimaryEndpoint
            .Map(fun uri -> $"replace(replace({uri}, 'https://', ''), '/', '')")
    member this.Endpoint = $"{this.Name.ResourceName.Value}.blob.core.windows.net"
    member this.ResourceId = storageAccounts.resourceId this.Name.ResourceName
    interface IBuilder with
        member this.ResourceId = this.ResourceId
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              Sku = this.Sku
              EnableHierarchicalNamespace = this.EnableDataLake
              Dependencies =
                this.RoleAssignments
                |> Seq.choose(fun roleAssignment -> roleAssignment.Principal.ArmExpression.Owner)
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
              NetworkAcls = this.NetworkAcls
              StaticWebsite = this.StaticWebsite
              MinTlsVersion = this.MinTlsVersion
              Tags = this.Tags }
            for name, access in this.Containers do
                { Name = name
                  StorageAccount = this.Name.ResourceName
                  Accessibility = access }
            for (name, shareQuota) in this.FileShares do
                { Name = name
                  ShareQuota = shareQuota
                  StorageAccount = this.Name.ResourceName }
            for queue in this.Queues do
                { Queues.Queue.Name = queue
                  Queues.Queue.StorageAccount = this.Name.ResourceName }
            for table in this.Tables do
                { Tables.Table.Name = table
                  Tables.Table.StorageAccount = this.Name.ResourceName }
            match this.Rules |> Map.toList with
            | [] ->
                ()
            | rules ->
                { ManagementPolicies.ManagementPolicy.StorageAccount = this.Name.ResourceName
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
                { Name = uniqueName
                  RoleDefinitionId = roleAssignment.Role
                  PrincipalId = roleAssignment.Principal
                  PrincipalType = PrincipalType.ServicePrincipal
                  Scope = ResourceGroup
                  Dependencies = Set [
                      ResourceId.create(storageAccounts, this.Name.ResourceName)
                      yield! roleAssignment.Owner |> Option.toList
                  ] }

            let storageResourceName = StorageResourceName.Create(this.Name.ResourceName).OkValue

            let rules =
                this.CorsRules
                |> List.groupBy fst
            let versioning =
                this.IsVersioningEnabled
                |> List.groupBy fst
            let policies =
                this.Policies
                |> List.groupBy fst

            let allSvcs =
                rules
                |> List.map fst
                |> (@) (versioning |> List.map fst)
                |> (@) (policies |> List.map fst)
                |> List.distinct

            for svc in allSvcs do
                { ResourceType =
                    match svc with
                    | StorageService.Blobs -> blobServices
                    | StorageService.Queues -> queueServices
                    | StorageService.Tables -> tableServices
                    | StorageService.Files -> fileServices
                  StorageAccount = storageResourceName
                  CorsRules =
                    this.CorsRules
                    |> List.filter (fst >> (=) svc)
                    |> List.map (fun (_, s) -> s)
                  Policies =
                    this.Policies
                    |> List.filter (fst >> (=) svc)
                    |> List.map (fun (_, s) -> s)
                    |> List.collect id
                  IsVersioningEnabled =
                    this.IsVersioningEnabled
                    |> List.filter (fst >> (=) svc)
                    |> List.forall (fun (_, ive) -> ive = true)  }
        ]

type StorageAccountBuilder() =
    member _.Yield _ = {
        Name = StorageAccountName.Create("default").OkValue
        Sku = Sku.Standard_LRS
        EnableDataLake = None
        Containers = []
        FileShares = []
        Rules = Map.empty
        Queues = Set.empty
        NetworkAcls = None
        Tables = Set.empty
        RoleAssignments = Set.empty
        StaticWebsite = None
        CorsRules = []
        Policies = []
        IsVersioningEnabled = []
        MinTlsVersion = None
        Tags = Map.empty
    }
    static member private AddContainer(state, access, name:string) = { state with Containers = state.Containers @ [ ((StorageResourceName.Create name).OkValue, access) ] }
    static member private AddFileShare(state:StorageAccountConfig, name:string, quota) = { state with FileShares = state.FileShares @ [ (StorageResourceName.Create(name).OkValue, quota) ] }

    /// Sets the name of the storage account.
    [<CustomOperation "name">]
    member _.Name(state:StorageAccountConfig, name:ResourceName) = { state with Name = StorageAccountName.Create(name).OkValue }
    member this.Name(state:StorageAccountConfig, name) = this.Name(state, ResourceName name)
    /// Sets the sku of the storage account.
    [<CustomOperation "sku">]
    member _.Sku(state:StorageAccountConfig, sku) = { state with Sku = sku }
    /// Adds private container.
    [<CustomOperation "add_private_container">]
    member _.AddPrivateContainer(state:StorageAccountConfig, name) = StorageAccountBuilder.AddContainer(state, Private, name)
    /// Adds container with anonymous read access for blobs and containers.
    [<CustomOperation "add_public_container">]
    member _.AddPublicContainer(state:StorageAccountConfig, name) =  StorageAccountBuilder.AddContainer(state, Container, name)
    /// Adds container with anonymous read access for blobs only.
    [<CustomOperation "add_blob_container">]
    member _.AddBlobContainer(state:StorageAccountConfig, name) = StorageAccountBuilder.AddContainer(state, Blob, name)
    /// Adds a file share with no quota.
    [<CustomOperation "add_file_share">]
    member _.AddFileShare(state:StorageAccountConfig, name) = StorageAccountBuilder.AddFileShare(state, name, None)
    /// Adds a file share with specified quota.
    [<CustomOperation "add_file_share_with_quota">]
    member _.AddFileShareWithQuota(state:StorageAccountConfig, name:string, quota) = StorageAccountBuilder.AddFileShare(state, name, Some quota)
    /// Adds a single queue to the storage account.
    [<CustomOperation "add_queue">]
    member _.AddQueue(state:StorageAccountConfig, name:string) = { state with Queues = state.Queues.Add (StorageResourceName.Create(name).OkValue) }
    /// Adds a set of queues to the storage account.
    [<CustomOperation "add_queues">]
    member this.AddQueues(state:StorageAccountConfig, names) =
        (state, names) ||> Seq.fold(fun state name -> this.AddQueue(state, name))
    /// Adds a single table to the storage account.
    [<CustomOperation "add_table">]
    member _.AddTable(state:StorageAccountConfig, name:string) = { state with Tables = state.Tables.Add (StorageResourceName.Create(name).OkValue) }
    /// Adds a set of tables to the storage account.
    [<CustomOperation "add_tables">]
    member this.AddTables(state:StorageAccountConfig, names) =
        (state, names) ||> Seq.fold(fun state name -> this.AddTable(state, name))
    /// Enable static website support, using the supplied local content path to the storage account's $web folder as a post-deployment task, and setting the index page as supplied.
    [<CustomOperation "use_static_website">]
    member _.StaticWebsite(state:StorageAccountConfig, contentPath, indexPage) =
        { state with StaticWebsite = Some {| IndexPage = indexPage; ErrorPage = None; ContentPath = contentPath |} }
    /// Sets the error page for the static website.
    [<CustomOperation "static_website_error_page">]
    member _.StaticWebsiteErrorPage(state:StorageAccountConfig, errorPage) =
        { state with StaticWebsite = state.StaticWebsite |> Option.map(fun staticWebsite -> {| staticWebsite with ErrorPage = Some errorPage |}) }
    /// Enables support for hierarchical namespace, also known as Data Lake Storage Gen2.
    [<CustomOperation "enable_data_lake">]
    member _.UseHns(state:StorageAccountConfig, value) = { state with EnableDataLake = Some value }
    /// Adds tags to the storage account
    /// Adds a lifecycle rule
    [<CustomOperation "add_lifecycle_rule">]
    member _.AddLifecycleRule(state:StorageAccountConfig, ruleName, actions, filters) =
        let rule =
            { Filters = filters
              CoolBlobAfter = actions |> List.tryPick(function CoolAfter days -> Some days | _ -> None)
              ArchiveBlobAfter = actions |> List.tryPick(function ArchiveAfter days -> Some days | _ -> None)
              DeleteBlobAfter = actions |> List.tryPick(function DeleteAfter days -> Some days | _ -> None)
              DeleteSnapshotAfter = actions |> List.tryPick(function DeleteSnapshotAfter days -> Some days | _ -> None) }
        { state with Rules = state.Rules.Add (ResourceName ruleName, rule) }
    static member private GrantAccess (state:StorageAccountConfig, assignment) = { state with RoleAssignments = state.RoleAssignments.Add assignment }
    [<CustomOperation "grant_access">]
    member _.GrantAccess (state:StorageAccountConfig, principalId:PrincipalId, role) =
        StorageAccountBuilder.GrantAccess (state, { Principal = principalId; Role = role; Owner = None })
    member _.GrantAccess(state:StorageAccountConfig, identity:UserAssignedIdentityConfig, role) =
        StorageAccountBuilder.GrantAccess (state, { Principal = identity.PrincipalId; Role = role; Owner = Some identity.ResourceId })
    member _.GrantAccess(state:StorageAccountConfig, identity:Identity.SystemIdentity, role) =
        StorageAccountBuilder.GrantAccess (state, { Principal = identity.PrincipalId; Role = role; Owner = Some identity.ResourceId })
    [<CustomOperation "default_blob_access_tier">]
    member _.SetDefaultAccessTier(state:StorageAccountConfig, tier) =
        { state with
            Sku =
                match state.Sku with
                | Blobs (replication, _) -> Blobs(replication, Some tier)
                | GeneralPurpose (V2 (replication, _)) -> GeneralPurpose (V2 (replication, Some tier))
                | other -> raiseFarmer $"You can only set the default access tier for Blobs or General Purpose V2 storage accounts. This account is %A{other}."
        }
    /// Specify network access control lists for this storage account.
    [<CustomOperation "set_network_acls">]
    member _.SetNetworkAcls(state:StorageAccountConfig, networkAcls) = { state with NetworkAcls = Some networkAcls }
    /// Restrict access to this storage account to a subnet on a virtual network.
    [<CustomOperation "restrict_to_subnet">]
    member _.RestrictToSubnet(state:StorageAccountConfig, vnet:string, subnet:string) =
        let allowVnet = { Subnet = ResourceName subnet; VirtualNetwork = ResourceName vnet; Action = RuleAction.Allow }
        match state.NetworkAcls with
        | None ->
            { state with
                NetworkAcls =
                    { Bypass = set [ NetworkRuleSetBypass.AzureServices ]
                      VirtualNetworkRules = [ allowVnet ]
                      IpRules = []
                      DefaultAction = RuleAction.Deny } |> Some
            }
        | Some existingAcl ->
            { state with
                NetworkAcls =
                    { existingAcl with
                        VirtualNetworkRules = allowVnet :: existingAcl.VirtualNetworkRules
                    } |> Some
            }
    /// Restrict access to this storage account to a IP address network prefix.
    [<CustomOperation "restrict_to_prefix">]
    member _.RestrictToPrefix(state:StorageAccountConfig, cidr:string) =
        let allowIp = { Value = IpRulePrefix (IPAddressCidr.parse cidr); Action = RuleAction.Allow }
        match state.NetworkAcls with
        | None ->
            { state with
                NetworkAcls =
                    { Bypass = set [ NetworkRuleSetBypass.AzureServices ]
                      VirtualNetworkRules = []
                      IpRules = [ allowIp ]
                      DefaultAction = RuleAction.Deny } |> Some
            }
        | Some existingAcl ->
            { state with
                NetworkAcls =
                    { existingAcl with
                        IpRules = allowIp :: existingAcl.IpRules
                    } |> Some
            }
    /// Restrict access to this storage account to an IP address.
    [<CustomOperation "restrict_to_ip">]
    member this.RestrictToIp(state:StorageAccountConfig, ip:string) =
        let allowIp = { Value = IpRuleAddress (System.Net.IPAddress.Parse ip); Action = RuleAction.Allow }
        match state.NetworkAcls with
        | None ->
            { state with
                NetworkAcls =
                    { Bypass = set [ NetworkRuleSetBypass.AzureServices ]
                      VirtualNetworkRules = []
                      IpRules = [ allowIp ]
                      DefaultAction = RuleAction.Deny } |> Some
            }
        | Some existingAcl ->
            { state with
                NetworkAcls =
                    { existingAcl with
                        IpRules = allowIp :: existingAcl.IpRules
                    } |> Some
            }
    /// Adds a set of CORS rules to the storage account.
    [<CustomOperation "add_cors_rules">]
    member _.AddCorsRules(state:StorageAccountConfig, rules) =
        { state with CorsRules = state.CorsRules @ rules }
    /// Adds a set of policies to the storage account.
    [<CustomOperation "add_policies">]
    member _.AddPolicies(state:StorageAccountConfig, policies) =
        { state with Policies = state.Policies @ policies }
    /// Adds a versioning enabled rule to the storage account.
    [<CustomOperation "enable_versioning">]
    member _.EnableVersioning(state:StorageAccountConfig, enableVersioning) =
        { state with IsVersioningEnabled = state.IsVersioningEnabled @ enableVersioning }
    /// Set minimum TLS version
    [<CustomOperation "min_tls_version">]
    member _.SetMinTlsVersion(state:StorageAccountConfig, minTlsVersion) =
        { state with MinTlsVersion = Some minTlsVersion }
    interface ITaggable<StorageAccountConfig> with member _.Add state tags = { state with Tags = state.Tags |> Map.merge tags }

/// Allow adding storage accounts directly to CDNs
type EndpointBuilder with
    member this.Origin(state:EndpointConfig, storage:StorageAccountConfig) =
        let state = this.Origin(state, storage.Endpoint)
        this.DependsOn(state, storage.ResourceId)

let storageAccount = StorageAccountBuilder()
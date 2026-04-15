[<AutoOpen>]
module Farmer.Builders.NetworkManager

open Farmer
open Farmer.Arm.NetworkManager

/// Configuration for a security admin rule
type SecurityAdminRuleConfig = {
    Name: ResourceName
    Description: string option
    Access: SecurityAdmin.Access
    Direction: SecurityAdmin.Direction
    Priority: int
    Protocol: SecurityAdmin.Protocol
    Sources: AddressPrefix list
    Destinations: AddressPrefix list
    SourcePortRanges: string list
    DestinationPortRanges: string list
} with

    member internal this.BuildRule(ruleCollectionId: LinkedResource) : SecurityAdminRule = {
        Name = this.Name
        RuleCollectionId = ruleCollectionId
        Description = this.Description
        Access = this.Access
        Direction = this.Direction
        Priority = this.Priority
        Protocol = this.Protocol
        Sources = this.Sources
        Destinations = this.Destinations
        SourcePortRanges = this.SourcePortRanges
        DestinationPortRanges = this.DestinationPortRanges
    }

type SecurityAdminRuleBuilder() =
    member _.Yield _ = {
        SecurityAdminRuleConfig.Name = ResourceName.Empty
        Description = None
        Access = SecurityAdmin.Access.Allow
        Direction = SecurityAdmin.Direction.Inbound
        Priority = 100
        Protocol = SecurityAdmin.Protocol.AnyProtocol
        Sources = []
        Destinations = []
        SourcePortRanges = []
        DestinationPortRanges = []
    }

    /// Sets the name of the rule.
    [<CustomOperation "name">]
    member _.Name(state: SecurityAdminRuleConfig, name: string) = { state with Name = ResourceName name }

    /// Sets the description of the rule.
    [<CustomOperation "description">]
    member _.Description(state: SecurityAdminRuleConfig, description: string) = {
        state with
            Description = Some description
    }

    /// Sets the rule to allow traffic (default).
    [<CustomOperation "allow_traffic">]
    member _.Allow(state: SecurityAdminRuleConfig) = {
        state with
            Access = SecurityAdmin.Access.Allow
    }

    /// Sets the rule to always allow traffic regardless of other rules.
    [<CustomOperation "always_allow_traffic">]
    member _.AlwaysAllow(state: SecurityAdminRuleConfig) = {
        state with
            Access = SecurityAdmin.Access.AlwaysAllow
    }

    /// Sets the rule to deny traffic.
    [<CustomOperation "deny_traffic">]
    member _.Deny(state: SecurityAdminRuleConfig) = {
        state with
            Access = SecurityAdmin.Access.Deny
    }

    /// Sets the direction of the rule (default: Inbound).
    [<CustomOperation "direction">]
    member _.Direction(state: SecurityAdminRuleConfig, direction: SecurityAdmin.Direction) = {
        state with
            Direction = direction
    }

    /// Sets the priority of the rule (1–4096).
    [<CustomOperation "priority">]
    member _.Priority(state: SecurityAdminRuleConfig, priority: int) = { state with Priority = priority }

    /// Sets the protocol of the rule (default: AnyProtocol).
    [<CustomOperation "protocol">]
    member _.Protocol(state: SecurityAdminRuleConfig, protocol: SecurityAdmin.Protocol) = {
        state with
            Protocol = protocol
    }

    /// Adds an IP prefix as a source address (e.g. "10.0.0.0/24").
    [<CustomOperation "add_source_ip_prefix">]
    member _.AddSourceIPPrefix(state: SecurityAdminRuleConfig, prefix: string) = {
        state with
            Sources = state.Sources @ [ AddressPrefix.OfIPPrefix prefix ]
    }

    /// Adds a service tag as a source address (e.g. "Internet", "AzureCloud").
    [<CustomOperation "add_source_service_tag">]
    member _.AddSourceServiceTag(state: SecurityAdminRuleConfig, tag: string) = {
        state with
            Sources = state.Sources @ [ AddressPrefix.OfServiceTag tag ]
    }

    /// Adds an IP prefix as a destination address (e.g. "10.1.0.0/24").
    [<CustomOperation "add_destination_ip_prefix">]
    member _.AddDestinationIPPrefix(state: SecurityAdminRuleConfig, prefix: string) = {
        state with
            Destinations = state.Destinations @ [ AddressPrefix.OfIPPrefix prefix ]
    }

    /// Adds a service tag as a destination address (e.g. "VirtualNetwork").
    [<CustomOperation "add_destination_service_tag">]
    member _.AddDestinationServiceTag(state: SecurityAdminRuleConfig, tag: string) = {
        state with
            Destinations = state.Destinations @ [ AddressPrefix.OfServiceTag tag ]
    }

    /// Adds a source port range (e.g. "80", "443", "1024-65535").
    [<CustomOperation "add_source_port_range">]
    member _.AddSourcePortRange(state: SecurityAdminRuleConfig, range: string) = {
        state with
            SourcePortRanges = state.SourcePortRanges @ [ range ]
    }

    /// Adds a destination port range (e.g. "80", "443", "1024-65535").
    [<CustomOperation "add_destination_port_range">]
    member _.AddDestinationPortRange(state: SecurityAdminRuleConfig, range: string) = {
        state with
            DestinationPortRanges = state.DestinationPortRanges @ [ range ]
    }

    /// Adds multiple source port ranges.
    [<CustomOperation "add_source_port_ranges">]
    member _.AddSourcePortRanges(state: SecurityAdminRuleConfig, ranges: string list) = {
        state with
            SourcePortRanges = state.SourcePortRanges @ ranges
    }

    /// Adds multiple destination port ranges.
    [<CustomOperation "add_destination_port_ranges">]
    member _.AddDestinationPortRanges(state: SecurityAdminRuleConfig, ranges: string list) = {
        state with
            DestinationPortRanges = state.DestinationPortRanges @ ranges
    }

let networkManagerSecurityAdminRule = SecurityAdminRuleBuilder()

/// Configuration for a security admin rule collection
type SecurityAdminRuleCollectionConfig = {
    Name: ResourceName
    Description: string option
    AppliesToGroups: ResourceId list
    Rules: SecurityAdminRuleConfig list
} with

    member internal this.BuildRuleCollection(securityAdminConfigId: LinkedResource) : SecurityAdminRuleCollection =
        let ruleCollectionId =
            Managed(securityAdminRuleCollections.resourceId (armName securityAdminConfigId.ResourceId / this.Name))

        {
            Name = this.Name
            SecurityAdminConfigurationId = securityAdminConfigId
            Description = this.Description
            AppliesToGroups = this.AppliesToGroups
            Rules = this.Rules |> List.map (fun r -> r.BuildRule ruleCollectionId)
        }

type SecurityAdminRuleCollectionBuilder() =
    member _.Yield _ = {
        SecurityAdminRuleCollectionConfig.Name = ResourceName.Empty
        Description = None
        AppliesToGroups = []
        Rules = []
    }

    /// Sets the name of the rule collection.
    [<CustomOperation "name">]
    member _.Name(state: SecurityAdminRuleCollectionConfig, name: string) = { state with Name = ResourceName name }

    /// Sets the description of the rule collection.
    [<CustomOperation "description">]
    member _.Description(state: SecurityAdminRuleCollectionConfig, description: string) = {
        state with
            Description = Some description
    }

    /// Adds a network group that this rule collection applies to.
    [<CustomOperation "add_applies_to_group">]
    member _.AddAppliesToGroup(state: SecurityAdminRuleCollectionConfig, groupId: ResourceId) = {
        state with
            AppliesToGroups = state.AppliesToGroups @ [ groupId ]
    }

    /// Adds rules to this rule collection.
    [<CustomOperation "add_rules">]
    member _.AddRules(state: SecurityAdminRuleCollectionConfig, rules: SecurityAdminRuleConfig list) = {
        state with
            Rules = state.Rules @ rules
    }

    member _.Run(state: SecurityAdminRuleCollectionConfig) =
        if state.AppliesToGroups.IsEmpty then
            raiseFarmer
                $"SecurityAdminRuleCollection '{state.Name.Value}' must specify at least one network group via 'add_applies_to_group'."

        state

let networkManagerSecurityAdminRuleCollection = SecurityAdminRuleCollectionBuilder()

/// Configuration for a security admin configuration
type SecurityAdminConfigurationConfig = {
    Name: ResourceName
    Description: string option
    ApplyOnNetworkIntentPolicyBasedServices: string list
    RuleCollections: SecurityAdminRuleCollectionConfig list
    NetworkManagerId: LinkedResource option
} with

    member internal this.BuildConfiguration(networkManagerId: LinkedResource) : SecurityAdminConfiguration =
        let securityAdminConfigId =
            Managed(securityAdminConfigurations.resourceId (armName networkManagerId.ResourceId / this.Name))

        {
            Name = this.Name
            NetworkManagerId = networkManagerId
            Description = this.Description
            ApplyOnNetworkIntentPolicyBasedServices = this.ApplyOnNetworkIntentPolicyBasedServices
            RuleCollections =
                this.RuleCollections
                |> List.map (fun rc -> rc.BuildRuleCollection securityAdminConfigId)
        }

    interface IBuilder with
        member this.ResourceId =
            match this.NetworkManagerId with
            | Some nmId -> securityAdminConfigurations.resourceId (armName nmId.ResourceId / this.Name)
            | None ->
                raiseFarmer
                    $"SecurityAdminConfiguration '{this.Name.Value}' requires link_to_network_manager or link_to_unmanaged_network_manager"

        member this.BuildResources _ =
            match this.NetworkManagerId with
            | None ->
                raiseFarmer
                    $"SecurityAdminConfiguration '{this.Name.Value}' requires link_to_network_manager or link_to_unmanaged_network_manager"
            | Some nmId ->
                let config = this.BuildConfiguration nmId

                [
                    yield config :> IArmResource
                    for ruleCollection in config.RuleCollections do
                        yield ruleCollection :> IArmResource

                        for rule in ruleCollection.Rules do
                            yield rule :> IArmResource
                ]

type SecurityAdminConfigurationBuilder() =
    member _.Yield _ = {
        SecurityAdminConfigurationConfig.Name = ResourceName.Empty
        Description = None
        ApplyOnNetworkIntentPolicyBasedServices = []
        RuleCollections = []
        NetworkManagerId = None
    }

    /// Sets the name of the security admin configuration.
    [<CustomOperation "name">]
    member _.Name(state: SecurityAdminConfigurationConfig, name: string) = { state with Name = ResourceName name }

    /// Sets the description of the security admin configuration.
    [<CustomOperation "description">]
    member _.Description(state: SecurityAdminConfigurationConfig, description: string) = {
        state with
            Description = Some description
    }

    /// Adds rule collections to this security admin configuration.
    [<CustomOperation "add_rule_collections">]
    member _.AddRuleCollections
        (state: SecurityAdminConfigurationConfig, ruleCollections: SecurityAdminRuleCollectionConfig list)
        =
        {
            state with
                RuleCollections = state.RuleCollections @ ruleCollections
        }

    /// Links this configuration to a Farmer-managed Network Manager in the same deployment.
    [<CustomOperation "link_to_network_manager">]
    member _.LinkToNetworkManager(state: SecurityAdminConfigurationConfig, networkManager: IBuilder) = {
        state with
            NetworkManagerId = Some(Managed networkManager.ResourceId)
    }

    /// Links this configuration to an existing Network Manager outside this deployment.
    [<CustomOperation "link_to_unmanaged_network_manager">]
    member _.LinkToUnmanagedNetworkManager(state: SecurityAdminConfigurationConfig, resourceId: ResourceId) = {
        state with
            NetworkManagerId = Some(Unmanaged resourceId)
    }

let networkManagerSecurityAdminConfiguration = SecurityAdminConfigurationBuilder()

/// Configuration for a network group
type NetworkManagerGroupConfig = {
    Name: ResourceName
    Description: string option
    NetworkManagerId: LinkedResource option
} with

    member internal this.BuildGroup(networkManagerId: LinkedResource) : NetworkManagerGroup = {
        Name = this.Name
        NetworkManagerId = networkManagerId
        Description = this.Description
    }

    interface IBuilder with
        member this.ResourceId =
            match this.NetworkManagerId with
            | Some nmId -> networkManagerGroups.resourceId (armName nmId.ResourceId / this.Name)
            | None ->
                raiseFarmer
                    $"NetworkManagerGroup '{this.Name.Value}' requires link_to_network_manager or link_to_unmanaged_network_manager"

        member this.BuildResources _ =
            match this.NetworkManagerId with
            | None ->
                raiseFarmer
                    $"NetworkManagerGroup '{this.Name.Value}' requires link_to_network_manager or link_to_unmanaged_network_manager"
            | Some nmId -> [ this.BuildGroup nmId :> IArmResource ]

type NetworkManagerGroupBuilder() =
    member _.Yield _ = {
        NetworkManagerGroupConfig.Name = ResourceName.Empty
        Description = None
        NetworkManagerId = None
    }

    /// Sets the name of the network group.
    [<CustomOperation "name">]
    member _.Name(state: NetworkManagerGroupConfig, name: string) = { state with Name = ResourceName name }

    /// Sets the description of the network group.
    [<CustomOperation "description">]
    member _.Description(state: NetworkManagerGroupConfig, description: string) = {
        state with
            Description = Some description
    }

    /// Links this group to a Farmer-managed Network Manager in the same deployment.
    [<CustomOperation "link_to_network_manager">]
    member _.LinkToNetworkManager(state: NetworkManagerGroupConfig, networkManager: IBuilder) = {
        state with
            NetworkManagerId = Some(Managed networkManager.ResourceId)
    }

    /// Links this group to an existing Network Manager outside this deployment.
    [<CustomOperation "link_to_unmanaged_network_manager">]
    member _.LinkToUnmanagedNetworkManager(state: NetworkManagerGroupConfig, resourceId: ResourceId) = {
        state with
            NetworkManagerId = Some(Unmanaged resourceId)
    }

let networkManagerGroup = NetworkManagerGroupBuilder()

/// Configuration for a network manager
type NetworkManagerConfig = {
    Name: ResourceName
    Description: string option
    ScopeAccesses: NetworkManagerScopeAccess list
    ScopeSubscriptions: string list
    ScopeManagementGroups: string list
    NetworkGroups: NetworkManagerGroupConfig list
    SecurityAdminConfigurations: SecurityAdminConfigurationConfig list
    Dependencies: ResourceId Set
    Tags: Map<string, string>
} with

    interface IBuilder with
        member this.ResourceId = networkManagers.resourceId this.Name

        member this.BuildResources location =
            let networkManagerId = Managed(networkManagers.resourceId this.Name)

            let builtGroups =
                this.NetworkGroups |> List.map (fun g -> g.BuildGroup networkManagerId)

            let builtConfigs =
                this.SecurityAdminConfigurations
                |> List.map (fun c -> c.BuildConfiguration networkManagerId)

            [
                yield
                    {
                        NetworkManager.Name = this.Name
                        Location = location
                        Description = this.Description
                        ScopeAccesses = this.ScopeAccesses
                        ScopeSubscriptions = this.ScopeSubscriptions
                        ScopeManagementGroups = this.ScopeManagementGroups
                        NetworkGroups = builtGroups
                        SecurityAdminConfigurations = builtConfigs
                        Dependencies = this.Dependencies
                        Tags = this.Tags
                    }
                    :> IArmResource
                for group in builtGroups do
                    yield group :> IArmResource
                for config in builtConfigs do
                    yield config :> IArmResource

                    for ruleCollection in config.RuleCollections do
                        yield ruleCollection :> IArmResource

                        for rule in ruleCollection.Rules do
                            yield rule :> IArmResource
            ]

type NetworkManagerBuilder() =
    member _.Yield _ = {
        NetworkManagerConfig.Name = ResourceName.Empty
        Description = None
        ScopeAccesses = []
        ScopeSubscriptions = []
        ScopeManagementGroups = []
        NetworkGroups = []
        SecurityAdminConfigurations = []
        Dependencies = Set.empty
        Tags = Map.empty
    }

    /// Sets the name of the network manager.
    [<CustomOperation "name">]
    member _.Name(state: NetworkManagerConfig, name: string) = { state with Name = ResourceName name }

    /// Sets the description of the network manager.
    [<CustomOperation "description">]
    member _.Description(state: NetworkManagerConfig, description: string) = {
        state with
            Description = Some description
    }

    /// Adds subscriptions to the scope of this network manager.
    [<CustomOperation "add_scope_subscriptions">]
    member _.AddScopeSubscriptions(state: NetworkManagerConfig, subscriptions: string list) = {
        state with
            ScopeSubscriptions = state.ScopeSubscriptions @ subscriptions
    }

    /// Adds a subscription to the scope of this network manager.
    [<CustomOperation "add_scope_subscription">]
    member _.AddScopeSubscription(state: NetworkManagerConfig, subscription: string) = {
        state with
            ScopeSubscriptions = state.ScopeSubscriptions @ [ subscription ]
    }

    /// Adds management groups to the scope of this network manager.
    [<CustomOperation "add_scope_management_groups">]
    member _.AddScopeManagementGroups(state: NetworkManagerConfig, managementGroups: string list) = {
        state with
            ScopeManagementGroups = state.ScopeManagementGroups @ managementGroups
    }

    /// Adds scope access types (SecurityAdmin, Connectivity).
    [<CustomOperation "add_scope_accesses">]
    member _.AddScopeAccesses(state: NetworkManagerConfig, accesses: NetworkManagerScopeAccess list) = {
        state with
            ScopeAccesses = state.ScopeAccesses @ accesses
    }

    /// Adds a scope access type (SecurityAdmin or Connectivity).
    [<CustomOperation "add_scope_access">]
    member _.AddScopeAccess(state: NetworkManagerConfig, access: NetworkManagerScopeAccess) = {
        state with
            ScopeAccesses = state.ScopeAccesses @ [ access ]
    }

    /// Adds network groups to this network manager.
    [<CustomOperation "add_network_groups">]
    member _.AddNetworkGroups(state: NetworkManagerConfig, groups: NetworkManagerGroupConfig list) = {
        state with
            NetworkGroups = state.NetworkGroups @ groups
    }

    /// Adds security admin configurations to this network manager.
    [<CustomOperation "add_security_admin_configurations">]
    member _.AddSecurityAdminConfigurations
        (state: NetworkManagerConfig, configurations: SecurityAdminConfigurationConfig list)
        =
        {
            state with
                SecurityAdminConfigurations = state.SecurityAdminConfigurations @ configurations
        }

    interface IDependable<NetworkManagerConfig> with
        member _.Add state newDeps = {
            state with
                Dependencies = state.Dependencies + newDeps
        }

    interface ITaggable<NetworkManagerConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

let networkManager = NetworkManagerBuilder()
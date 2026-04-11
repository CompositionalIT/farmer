[<AutoOpen>]
module Farmer.Arm.NetworkManager

open Farmer

// https://learn.microsoft.com/en-us/azure/templates/microsoft.network/networkmanagers
let networkManagers =
    ResourceType("Microsoft.Network/networkManagers", "2023-02-01")

// https://learn.microsoft.com/en-us/azure/templates/microsoft.network/networkmanagers/networkgroups
let networkManagerGroups =
    ResourceType("Microsoft.Network/networkManagers/networkGroups", "2023-02-01")

// https://learn.microsoft.com/en-us/azure/templates/microsoft.network/networkmanagers/securityadminconfigurations
let securityAdminConfigurations =
    ResourceType("Microsoft.Network/networkManagers/securityAdminConfigurations", "2023-02-01")

// https://learn.microsoft.com/en-us/azure/templates/microsoft.network/networkmanagers/securityadminconfigurations/rulecollections
let securityAdminRuleCollections =
    ResourceType("Microsoft.Network/networkManagers/securityAdminConfigurations/ruleCollections", "2023-02-01")

// https://learn.microsoft.com/en-us/azure/templates/microsoft.network/networkmanagers/securityadminconfigurations/rulecollections/rules
let securityAdminRules =
    ResourceType("Microsoft.Network/networkManagers/securityAdminConfigurations/ruleCollections/rules", "2023-02-01")

/// Scope access type for a network manager
type NetworkManagerScopeAccess =
    | SecurityAdmin
    | Connectivity

    member this.ArmValue =
        match this with
        | SecurityAdmin -> "SecurityAdmin"
        | Connectivity -> "Connectivity"

/// Types used in security admin configurations, scoped to avoid name conflicts
module SecurityAdmin =
    /// Access type for security admin rules (Allow, AlwaysAllow, or Deny)
    type Access =
        | Allow
        | AlwaysAllow
        | Deny

        member this.ArmValue =
            match this with
            | Allow -> "Allow"
            | AlwaysAllow -> "AlwaysAllow"
            | Deny -> "Deny"

    /// Network protocol for network manager security admin rules
    type Protocol =
        /// Any protocol
        | AnyProtocol
        /// Transmission Control Protocol
        | Tcp
        /// User Datagram Protocol
        | Udp
        /// Internet Control Message Protocol
        | Icmp

        member this.ArmValue =
            match this with
            | AnyProtocol -> "Any"
            | Tcp -> "Tcp"
            | Udp -> "Udp"
            | Icmp -> "Icmp"

    /// Traffic direction for security admin rules
    type Direction =
        | Inbound
        | Outbound

        member this.ArmValue =
            match this with
            | Inbound -> "Inbound"
            | Outbound -> "Outbound"

/// Address prefix type for security admin rules
type AddressPrefixType =
    | IPPrefix
    | ServiceTag

    member this.ArmValue =
        match this with
        | IPPrefix -> "IPPrefix"
        | ServiceTag -> "ServiceTag"

/// An address prefix with its type for use in security admin rules
type AddressPrefix = {
    Prefix: string
    PrefixType: AddressPrefixType
} with

    static member OfIPPrefix(prefix: string) = {
        Prefix = prefix
        PrefixType = IPPrefix
    }

    static member OfServiceTag(tag: string) = {
        Prefix = tag
        PrefixType = ServiceTag
    }

    member this.JsonModel = {|
        addressPrefix = this.Prefix
        addressPrefixType = this.PrefixType.ArmValue
    |}

/// Gets the full composite ARM name (first/second/third) from a ResourceId
let internal armName (resourceId: ResourceId) =
    resourceId.Name :: resourceId.Segments
    |> List.map (fun n -> n.Value)
    |> String.concat "/"
    |> ResourceName

/// A security admin rule in a rule collection
type SecurityAdminRule = {
    Name: ResourceName
    RuleCollectionId: LinkedResource
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

    interface IArmResource with
        member this.ResourceId =
            securityAdminRules.resourceId (armName this.RuleCollectionId.ResourceId / this.Name)

        member this.JsonModel = {|
            securityAdminRules.Create(
                armName this.RuleCollectionId.ResourceId / this.Name,
                dependsOn = [ this.RuleCollectionId.ResourceId ]
            ) with
                kind = "Custom"
                properties = {|
                    description = this.Description |> Option.toObj
                    access = this.Access.ArmValue
                    direction = this.Direction.ArmValue
                    priority = this.Priority
                    protocol = this.Protocol.ArmValue
                    sources = this.Sources |> List.map (fun s -> s.JsonModel)
                    destinations = this.Destinations |> List.map (fun d -> d.JsonModel)
                    sourcePortRanges =
                        if this.SourcePortRanges.IsEmpty then
                            [ "*" ]
                        else
                            this.SourcePortRanges
                    destinationPortRanges =
                        if this.DestinationPortRanges.IsEmpty then
                            [ "*" ]
                        else
                            this.DestinationPortRanges
                |}
        |}

/// A rule collection in a security admin configuration
type SecurityAdminRuleCollection = {
    Name: ResourceName
    SecurityAdminConfigurationId: LinkedResource
    Description: string option
    AppliesToGroups: ResourceId list
    Rules: SecurityAdminRule list
} with

    interface IArmResource with
        member this.ResourceId =
            securityAdminRuleCollections.resourceId (armName this.SecurityAdminConfigurationId.ResourceId / this.Name)

        member this.JsonModel = {|
            securityAdminRuleCollections.Create(
                armName this.SecurityAdminConfigurationId.ResourceId / this.Name,
                dependsOn = [ this.SecurityAdminConfigurationId.ResourceId ]
            ) with
                properties = {|
                    description = this.Description |> Option.toObj
                    appliesToGroups =
                        this.AppliesToGroups
                        |> List.map (fun groupId -> {| networkGroupId = groupId.Eval() |})
                |}
        |}

/// A security admin configuration in a network manager
type SecurityAdminConfiguration = {
    Name: ResourceName
    NetworkManagerId: LinkedResource
    Description: string option
    ApplyOnNetworkIntentPolicyBasedServices: string list
    RuleCollections: SecurityAdminRuleCollection list
} with

    interface IArmResource with
        member this.ResourceId =
            securityAdminConfigurations.resourceId (armName this.NetworkManagerId.ResourceId / this.Name)

        member this.JsonModel = {|
            securityAdminConfigurations.Create(
                armName this.NetworkManagerId.ResourceId / this.Name,
                dependsOn = [ this.NetworkManagerId.ResourceId ]
            ) with
                properties = {|
                    description = this.Description |> Option.toObj
                    applyOnNetworkIntentPolicyBasedServices =
                        if this.ApplyOnNetworkIntentPolicyBasedServices.IsEmpty then
                            [ "None" ]
                        else
                            this.ApplyOnNetworkIntentPolicyBasedServices
                |}
        |}

/// A network group in a network manager
type NetworkManagerGroup = {
    Name: ResourceName
    NetworkManagerId: LinkedResource
    Description: string option
} with

    interface IArmResource with
        member this.ResourceId =
            networkManagerGroups.resourceId (armName this.NetworkManagerId.ResourceId / this.Name)

        member this.JsonModel = {|
            networkManagerGroups.Create(
                armName this.NetworkManagerId.ResourceId / this.Name,
                dependsOn = [ this.NetworkManagerId.ResourceId ]
            ) with
                properties = {|
                    description = this.Description |> Option.toObj
                |}
        |}

/// A network manager resource
type NetworkManager = {
    Name: ResourceName
    Location: Location
    Description: string option
    ScopeAccesses: NetworkManagerScopeAccess list
    ScopeSubscriptions: string list
    ScopeManagementGroups: string list
    NetworkGroups: NetworkManagerGroup list
    SecurityAdminConfigurations: SecurityAdminConfiguration list
    Dependencies: ResourceId Set
    Tags: Map<string, string>
} with

    interface IArmResource with
        member this.ResourceId = networkManagers.resourceId this.Name

        member this.JsonModel = {|
            networkManagers.Create(this.Name, this.Location, this.Dependencies, this.Tags) with
                properties = {|
                    description = this.Description |> Option.toObj
                    networkManagerScopeAccesses = this.ScopeAccesses |> List.map (fun s -> s.ArmValue)
                    networkManagerScopes = {|
                        subscriptions = this.ScopeSubscriptions
                        managementGroups = this.ScopeManagementGroups
                    |}
                |}
        |}
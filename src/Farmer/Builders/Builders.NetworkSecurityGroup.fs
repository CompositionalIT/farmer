[<AutoOpen>]

module Farmer.Builders.NetworkSecurityGroup

open Farmer
open Farmer.Arm.NetworkSecurityGroup
open Farmer.NetworkSecurity
open System.Net

/// Application security group configuration
type ApplicationSecurityGroupConfig = {
    Name: ResourceName
    Dependencies: ResourceId Set
    Tags: Map<string, string>
} with

    interface IBuilder with
        member this.BuildResources location = [
            {
                ApplicationSecurityGroup.Name = this.Name
                Location = location
                Dependencies = this.Dependencies
                Tags = this.Tags
            }
        ]

        member this.ResourceId = applicationSecurityGroups.resourceId this.Name

type ApplicationSecurityGroupBuilder() =
    member _.Yield _ = {
        ApplicationSecurityGroupConfig.Name = ResourceName.Empty
        Dependencies = Set.empty
        Tags = Map.empty
    }

    [<CustomOperation "name">]
    member _.Name(config: ApplicationSecurityGroupConfig, name) = { config with Name = ResourceName name }

    interface IDependable<ApplicationSecurityGroupConfig> with
        member _.Add state newDeps = {
            state with
                Dependencies = state.Dependencies + newDeps
        }

    interface ITaggable<ApplicationSecurityGroupConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

let applicationSecurityGroup = ApplicationSecurityGroupBuilder()

/// Network access policy
type SecurityRuleConfig = {
    Name: ResourceName
    Nsg: LinkedResource option
    Dependencies: ResourceId Set
    Description: string option
    Services: NetworkService list
    Sources: (NetworkProtocol * Endpoint * Port) list
    Destinations: Endpoint list
    Operation: Operation
    Direction: TrafficDirection
    Priority: int option
} with

    member internal this.buildNsgRule() =
        let nsg =
            match this.Nsg with
            | None -> raiseFarmer "Network Security Group must be specified for security rule."
            | Some nsg -> nsg

        let priority =
            match this.Priority with
            | None -> raiseFarmer "Priority must be specified for security rule."
            | Some priority -> priority

        {
            Name = this.Name
            Description = None
            SecurityGroup = nsg
            Protocol =
                let protocols =
                    this.Sources |> List.map (fun (protocol, _, _) -> protocol) |> List.distinct

                match protocols with
                | [] -> raiseFarmer $"You must set a source for security rule {this.Name.Value}"
                | [ protocol ] -> protocol
                | _ -> AnyProtocol
            SourcePorts = this.Sources |> List.map (fun (_, _, sourcePort) -> sourcePort) |> Set
            SourceAddresses =
                this.Sources
                |> List.filter (fun (_, sourceEndpoint, _) ->
                    match sourceEndpoint with
                    | ApplicationSecurityGroup _ -> false
                    | _ -> true)
                |> List.map (fun (_, sourceAddress, _) -> sourceAddress)
                |> List.distinct
            SourceApplicationSecurityGroups =
                this.Sources
                |> List.choose (fun (_, sourceEndpoint, _) ->
                    match sourceEndpoint with
                    | ApplicationSecurityGroup asgId -> Some asgId
                    | _ -> None)
            Dependencies = this.Dependencies
            DestinationPorts =
                match this.Services with
                | [] -> Set [ AnyPort ]
                | services -> services |> List.map (fun (NetworkService(_, port)) -> port) |> Set
            DestinationAddresses =
                this.Destinations
                |> List.filter (fun endpoint ->
                    match endpoint with
                    | ApplicationSecurityGroup asgId -> false
                    | _ -> true)
            DestinationApplicationSecurityGroups =
                this.Destinations
                |> List.choose (fun endpoint ->
                    match endpoint with
                    | ApplicationSecurityGroup asgId -> Some asgId
                    | _ -> None)
            Access = this.Operation
            Direction = this.Direction
            Priority = priority
        }

    interface IBuilder with
        member this.ResourceId = networkSecurityGroups.resourceId this.Name

        member this.BuildResources _ = [ this.buildNsgRule () ]

type SecurityRuleBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        Nsg = None
        Description = None
        Dependencies = Set.empty
        Services = []
        Sources = []
        Destinations = []
        Operation = Allow
        Direction = TrafficDirection.Inbound
        Priority = None
    }

    /// Sets the name of the security rule
    [<CustomOperation "name">]
    member _.Name(state: SecurityRuleConfig, name) = { state with Name = ResourceName name }

    /// Links the rule to a Farmer-managed network security group in this same deployment
    [<CustomOperation "network_security_group">]
    member _.NetworkSecurityGroup(state: SecurityRuleConfig, nsgId) = { state with Nsg = Some(Managed nsgId) }

    member _.NetworkSecurityGroup(state: SecurityRuleConfig, nsg: IBuilder) = {
        state with
            Nsg = Some(Managed nsg.ResourceId)
    }

    /// Links the rule to an existing network security group.
    [<CustomOperation "link_to_network_security_group">]
    member _.LinkToNetworkSecurityGroup(state: SecurityRuleConfig, nsgId) = {
        state with
            Nsg = Some(Unmanaged nsgId)
    }

    member _.LinkToNetworkSecurityGroup(state: SecurityRuleConfig, nsg: IBuilder) = {
        state with
            Nsg = Some(Unmanaged nsg.ResourceId)
    }

    /// Sets the description of the security rule
    [<CustomOperation "description">]
    member _.Description(state: SecurityRuleConfig, description) = {
        state with
            Description = Some description
    }

    /// Sets the service or services protected by this rule.
    [<CustomOperation("services")>]
    member _.Services(state: SecurityRuleConfig, services) = { state with Services = services }

    member this.Services(state: SecurityRuleConfig, services) =
        let services = [
            for (name, port) in services do
                NetworkService(name, Port(uint16 port))
        ]

        this.Services(state, services)

    /// Sets the source endpoint that is matched in this rule
    [<CustomOperation("add_source")>]
    member _.AddSource(state: SecurityRuleConfig, source) = {
        state with
            Sources = source :: state.Sources
    }

    /// Sets the rule to match on any source endpoint.
    [<CustomOperation("add_source_any")>]
    member _.AddSourceAny(state: SecurityRuleConfig, protocol) = {
        state with
            Sources = (protocol, AnyEndpoint, AnyPort) :: state.Sources
    }

    /// Sets the rule to match on a tagged source endpoint, such as 'Internet'.
    [<CustomOperation("add_source_tag")>]
    member _.AddSourceTag(state: SecurityRuleConfig, protocol, tag) = {
        state with
            Sources = (protocol, Tag tag, AnyPort) :: state.Sources
    }

    /// Sets the rule to match on a source address.
    [<CustomOperation("add_source_address")>]
    member _.AddSourceAddress(state: SecurityRuleConfig, protocol, sourceAddress: string) = {
        state with
            Sources = (protocol, Host(IPAddress.Parse sourceAddress), AnyPort) :: state.Sources
    }

    member _.AddSourceAddress(state: SecurityRuleConfig, protocol, sourceAddress: ArmExpression) = {
        state with
            Sources = (protocol, Expression sourceAddress, AnyPort) :: state.Sources
    }

    /// Sets the rule to a managed source application security group.
    [<CustomOperation("add_source_application_security_group")>]
    member _.AddSourceApplicationSecurityGroup
        (state: SecurityRuleConfig, protocol, asg: ApplicationSecurityGroupConfig)
        =
        {
            state with
                Sources =
                    (protocol, ApplicationSecurityGroup(Managed (asg :> IBuilder).ResourceId), AnyPort)
                    :: state.Sources
        }

    /// Sets the rule to an unmanaged source application security group.
    [<CustomOperation("link_source_application_security_group")>]
    member _.LinkSourceApplicationSecurityGroup
        (state: SecurityRuleConfig, protocol, asg: ApplicationSecurityGroupConfig)
        =
        {
            state with
                Sources =
                    (protocol, ApplicationSecurityGroup(Unmanaged (asg :> IBuilder).ResourceId), AnyPort)
                    :: state.Sources
        }

    /// Sets the rule to match on a source network.
    [<CustomOperation("add_source_network")>]
    member _.AddSourceNetwork(state: SecurityRuleConfig, protocol, sourceNetwork) = {
        state with
            Sources = (protocol, Network(IPAddressCidr.parse sourceNetwork), AnyPort) :: state.Sources
    }

    /// Sets the destination endpoint that is matched in this rule
    [<CustomOperation("add_destination")>]
    member _.AddDestination(state: SecurityRuleConfig, dest) = {
        state with
            Destinations = dest :: state.Destinations
    }

    /// Sets the rule to match on any destination endpoint.
    [<CustomOperation("add_destination_any")>]
    member _.AddDestinationAny(state: SecurityRuleConfig) = {
        state with
            Destinations = AnyEndpoint :: state.Destinations
    }

    /// Sets the rule to match on a tagged destination endpoint, such as 'Internet'.
    [<CustomOperation("add_destination_tag")>]
    member _.AddDestinationTag(state: SecurityRuleConfig, tag) = {
        state with
            Destinations = Tag tag :: state.Destinations
    }

    /// Sets the rule to match on a destination address.
    [<CustomOperation("add_destination_address")>]
    member _.AddDestinationAddress(state: SecurityRuleConfig, destAddress: string) = {
        state with
            Destinations = Host(IPAddress.Parse destAddress) :: state.Destinations
    }

    member _.AddDestinationAddress(state: SecurityRuleConfig, destAddress: ArmExpression) = {
        state with
            Destinations = Expression destAddress :: state.Destinations
    }

    /// Sets the rule to a managed destination application security group.
    [<CustomOperation("add_destination_application_security_group")>]
    member _.AddDestinationApplicationSecurityGroup(state: SecurityRuleConfig, asg: ApplicationSecurityGroupConfig) = {
        state with
            Destinations =
                ApplicationSecurityGroup(Managed (asg :> IBuilder).ResourceId)
                :: state.Destinations
    }

    /// Sets the rule to an unmanaged destination application security group.
    [<CustomOperation("link_destination_application_security_group")>]
    member _.LinkDestinationApplicationSecurityGroup
        (state: SecurityRuleConfig, protocol, asg: ApplicationSecurityGroupConfig)
        =
        {
            state with
                Destinations =
                    ApplicationSecurityGroup(Unmanaged (asg :> IBuilder).ResourceId)
                    :: state.Destinations
        }

    /// Sets the rule to match on a destination network.
    [<CustomOperation("add_destination_network")>]
    member _.AddDestinationNetwork(state: SecurityRuleConfig, destNetwork) = {
        state with
            Destinations = Network(IPAddressCidr.parse destNetwork) :: state.Destinations
    }

    /// Sets the rule to allow this traffic (default value).
    [<CustomOperation("allow_traffic")>]
    member _.Allow(state: SecurityRuleConfig) = { state with Operation = Allow }

    /// Sets the rule to deny this traffic.
    [<CustomOperation("deny_traffic")>]
    member _.Deny(state: SecurityRuleConfig) = { state with Operation = Deny }

    /// Specify the direction of traffic this rule applies to (defaults to inbound).
    [<CustomOperation("direction")>]
    member _.Direction(state: SecurityRuleConfig, direction) = { state with Direction = direction }

    /// Specify the priority for the rule.
    [<CustomOperation "priority">]
    member _.Priority(state: SecurityRuleConfig, priority) = { state with Priority = Some priority }

    interface IDependable<SecurityRuleConfig> with
        member _.Add state newDeps = {
            state with
                Dependencies = state.Dependencies + newDeps
        }

let securityRule = SecurityRuleBuilder()

type NsgConfig = {
    Name: ResourceName
    Dependencies: ResourceId Set
    SecurityRules: SecurityRuleConfig list
    Tags: Map<string, string>
    InitialRulePriority: int
    PriorityIncrementor: int
} with

    interface IBuilder with
        member this.ResourceId = networkSecurityGroups.resourceId this.Name

        member this.BuildResources location = [
            {
                Name = this.Name
                Dependencies = this.Dependencies
                Location = location
                SecurityRules =
                    seq {
                        // Policy Rules
                        for index, rule in List.indexed this.SecurityRules do
                            {
                                rule with
                                    Nsg = Some(Managed (this :> IBuilder).ResourceId)
                                    Priority =
                                        rule.Priority
                                        |> Option.defaultValue (
                                            index * this.PriorityIncrementor + this.InitialRulePriority
                                        )
                                        |> Some
                            }
                                .buildNsgRule ()
                    }
                    |> List.ofSeq
                Tags = this.Tags
            }
        ]

type NsgBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        Dependencies = Set.empty
        SecurityRules = []
        Tags = Map.empty
        InitialRulePriority = 100
        PriorityIncrementor = 100
    }

    /// Sets the name of the network security group
    [<CustomOperation "name">]
    member _.Name(state: NsgConfig, name) = { state with Name = ResourceName name }

    /// Adds rules to this NSG.
    [<CustomOperation "add_rules">]
    member _.AddSecurityRules(state: NsgConfig, rules) = {
        state with
            SecurityRules = state.SecurityRules @ rules
    }

    /// Initial rule priority sets the priority of the first rule.
    [<CustomOperation "initial_rule_priority">]
    member _.InitialRulePriority(state: NsgConfig, initialPriority) = {
        state with
            InitialRulePriority = initialPriority
    }

    /// First rule is priority 100. After that, this sets how much priority is increased per each rule. Default 100.
    [<CustomOperation "priority_incr">]
    member _.PriorityIncrementor(state: NsgConfig, priority_incr) = {
        state with
            PriorityIncrementor = priority_incr
    }

    interface IDependable<NsgConfig> with
        member _.Add state newDeps = {
            state with
                Dependencies = state.Dependencies + newDeps
        }

    interface ITaggable<NsgConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

let nsg = NsgBuilder()
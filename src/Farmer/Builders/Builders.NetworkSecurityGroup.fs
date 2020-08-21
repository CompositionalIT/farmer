[<AutoOpen>]

module Farmer.Builders.NetworkSecurityGroup

open Farmer
open Farmer.Arm.NetworkSecurityGroup
open Farmer.NetworkSecurity
open System.Net

/// Network access policy
type SecurityRuleConfig =
    { Name: ResourceName
      Description : string option
      Services: NetworkService list
      Sources: (NetworkProtocol * Endpoint * Port) list
      Destinations : Endpoint list
      Operation : Operation }

type SecurityRuleBuilder () =
    member __.Yield _ =
        { Name = ResourceName.Empty
          Description = None
          Services = []
          Sources = []
          Destinations = []
          Operation = Allow }
    /// Sets the name of the security rule
    [<CustomOperation "name">]
    member _.Name(state:SecurityRuleConfig, name) = { state with Name = ResourceName name }
    /// Sets the description of the security rule
    [<CustomOperation "description">]
    member _.Description(state:SecurityRuleConfig, description) = { state with Description = Some description }
    /// Sets the service or services protected by this rule.
    [<CustomOperation("services")>]
    member _.Services(state:SecurityRuleConfig, services) =
        { state with Services = services }
    member this.Services(state:SecurityRuleConfig, services) =
        let services = [ for (name, port) in services do NetworkService (name, Port(uint16 port)) ]
        this.Services(state, services)
    /// Sets the source endpoint that is matched in this rule
    [<CustomOperation("add_source")>]
    member _.AddSource(state:SecurityRuleConfig, source) = { state with Sources = source :: state.Sources }
    /// Sets the rule to match on any source endpoint.
    [<CustomOperation("add_source_any")>]
    member _.AddSourceAny(state:SecurityRuleConfig, protocol) = { state with Sources = (protocol, AnyEndpoint, AnyPort) :: state.Sources }
    /// Sets the rule to match on a tagged source endpoint, such as 'Internet'.
    [<CustomOperation("add_source_tag")>]
    member _.AddSourceTag(state:SecurityRuleConfig, protocol, tag) = { state with Sources = (protocol, Tag tag, AnyPort) :: state.Sources }
    /// Sets the rule to match on a source address.
    [<CustomOperation("add_source_address")>]
    member _.AddSourceAddress(state:SecurityRuleConfig, protocol, sourceAddress) = { state with Sources = (protocol, Host (IPAddress.Parse sourceAddress), AnyPort) :: state.Sources }
    /// Sets the rule to match on a source network.
    [<CustomOperation("add_source_network")>]
    member _.AddSourceNetwork(state:SecurityRuleConfig, protocol, sourceNetwork) = { state with Sources = (protocol, Network (IPAddressCidr.parse sourceNetwork), AnyPort) :: state.Sources }
    /// Sets the destination endpoint that is matched in this rule
    [<CustomOperation("add_destination")>]
    member _.AddDestination(state:SecurityRuleConfig, dest) = { state with Destinations = dest :: state.Destinations }
    /// Sets the rule to match on any destination endpoint.
    [<CustomOperation("add_destination_any")>]
    member _.AddDestinationAny(state:SecurityRuleConfig) = { state with Destinations = AnyEndpoint :: state.Destinations }
    /// Sets the rule to match on a tagged destination endpoint, such as 'Internet'.
    [<CustomOperation("add_destination_tag")>]
    member _.AddDestinationTag(state:SecurityRuleConfig, tag) = { state with Destinations = Tag tag :: state.Destinations }
    /// Sets the rule to match on a destination address.
    [<CustomOperation("add_destination_address")>]
    member _.AddDestinationAddress(state:SecurityRuleConfig, destAddress) = { state with Destinations = Host (IPAddress.Parse destAddress) :: state.Destinations }
    /// Sets the rule to match on a destination network.
    [<CustomOperation("add_destination_network")>]
    member _.AddDestinationNetwork(state:SecurityRuleConfig, destNetwork) = { state with Destinations = Network (IPAddressCidr.parse destNetwork):: state.Destinations }
    /// Sets the rule to allow this traffic (default value).
    [<CustomOperation("allow_traffic")>]
    member _.Allow(state:SecurityRuleConfig) = { state with Operation = Allow }
    /// Sets the rule to deny this traffic.
    [<CustomOperation("deny_traffic")>]
    member _.Deny(state:SecurityRuleConfig) = { state with Operation = Deny }

let securityRule = SecurityRuleBuilder()

let internal buildNsgRule (nsg:NetworkSecurityGroup) (rule:SecurityRuleConfig) (priority:int) =
    { Name = rule.Name
      Description = None
      SecurityGroup = nsg
      Protocol =
        let protocols = rule.Sources |> List.map(fun (protocol, _, _) -> protocol) |> Set
        if protocols.Count > 1 then AnyProtocol else protocols |> Seq.head
      SourcePorts = rule.Sources |> List.map(fun (_, _, sourcePort) -> sourcePort) |> Set
      SourceAddresses = rule.Sources |> List.map(fun (_, sourceAddress, _) -> sourceAddress) |> List.distinct
      DestinationPorts = rule.Services |> List.map(fun (NetworkService(_, port)) -> port) |> Set
      DestinationAddresses = rule.Destinations
      Access = rule.Operation
      Direction = Inbound
      Priority = priority }

type NsgConfig =
    { Name : ResourceName
      SecurityRules : SecurityRuleConfig list
      Tags: Map<string,string>  }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            let securityGroup =
                { Name = this.Name
                  Location = location
                  Tags = this.Tags  }

            // NSG
            securityGroup
            // Policy Rules
            for priority, rule in List.indexed this.SecurityRules do
                buildNsgRule securityGroup rule ((priority + 1) * 100)
        ]
type NsgBuilder() =
    member __.Yield _ =
        { Name = ResourceName.Empty
          SecurityRules = []
          Tags = Map.empty  }
    /// Sets the name of the network security group
    [<CustomOperation "name">]
    member _.Name(state:NsgConfig, name) = { state with Name = ResourceName name }
    /// Adds rules to this NSG.
    [<CustomOperation "add_rules">]
    member _.AddSecurityRules (state:NsgConfig, rules) = { state with SecurityRules = state.SecurityRules @ rules }
    [<CustomOperation "add_tags">]
    member _.Tags(state:NsgConfig, pairs) =
        { state with
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
    member this.Tag(state:NsgConfig, key, value) = this.Tags(state, [ (key,value) ])
let nsg = NsgBuilder()

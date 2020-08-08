[<AutoOpen>]

module Farmer.Builders.NetworkSecurityGroup

open Farmer
open Farmer.Arm.NetworkSecurityGroup
open Farmer.NetworkSecurity
open System.Collections.Generic
open System.Net

/// Network access policy
type SecurityRuleConfig =
    { Name: ResourceName
      Description : string option
      Services: {| Name : string; Services : Service list |}
      Sources: (NetworkProtocol * Endpoint * Port) list
      Destinations : Endpoint list
      Operation : Operation }

type SecurityRuleBuilder () =
    member __.Yield _ =
        { Name = ResourceName.Empty
          Description = None
          Services = {| Name = ""; Services = [] |}
          Sources = [ ]
          Destinations = [ ]
          Operation = Allow }
    /// Sets the name of the security rule
    [<CustomOperation "name">]
    member _.Name(state:SecurityRuleConfig, name) = { state with Name = ResourceName name }
    /// Sets the description of the security rule
    [<CustomOperation "description">]
    member _.Description(state:SecurityRuleConfig, description) = { state with Description = Some description }
    /// Sets the service or services protected by this rule.
    [<CustomOperation("service")>]
    member _.Service(state:SecurityRuleConfig, name, services) =
        { state with
            Services =
                {| Name = name
                   Services = [
                    for (name, port) in services do
                        Service (name, Port(uint16 port))
                   ]
                |}
        }
    member this.Service(state:SecurityRuleConfig, name, port) = this.Service(state, "", [ name, port ])
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
    let wildcardOrTag (addresses:Endpoint seq) =
        // Use a wildcard if there is one
        if addresses |> Seq.contains AnyEndpoint then Some AnyEndpoint, []
        // Use the first tag that is set (only one supported), otherwise use addresses
        else
            addresses
            |> Seq.tryFind (function Tag _ -> true | _ -> false)
            |> function
            | Some (Tag tag) ->
                Some (Tag tag), []
            | None | Some (AnyEndpoint | Network _ | Host _) ->
                None, List.ofSeq addresses

    let destPorts = rule.Services.Services |> List.map(fun (Service(_, port)) -> port) |> Set
    let protocols = rule.Sources |> List.map(fun (protocol, _, _) -> protocol) |> Set
    let sourceAddresses = rule.Sources |> List.map(fun (_, sourceAddress, _) -> sourceAddress) |> List.distinct
    let sourcePorts = rule.Sources |> List.map(fun (_, _, sourcePort) -> sourcePort) |> Set

    let sourceAddress, sourceAddresses = wildcardOrTag sourceAddresses
    let destAddress, destAddresses = wildcardOrTag rule.Destinations

    { Name = rule.Name
      Description = None
      SecurityGroup = nsg
      Protocol = if protocols.Count > 1 then AnyProtocol else protocols |> Seq.head

      // TODO: What is the semantic meaning here? Is there a relationship between source port and ports?
      SourcePort = if sourcePorts.Contains AnyPort then Some AnyPort else None
      SourcePorts = if sourcePorts.Contains AnyPort then [] else sourcePorts |> List.ofSeq
      SourceAddress = sourceAddress
      SourceAddresses = sourceAddresses
      DestinationPort = if destPorts.Contains AnyPort then Some AnyPort else None
      DestinationPorts = if destPorts.Contains AnyPort then [] else destPorts |> List.ofSeq
      DestinationAddress = destAddress
      DestinationAddresses = destAddresses
      Access = rule.Operation
      Direction = Inbound
      Priority = priority }

type NsgConfig =
    { Name : ResourceName
      SecurityRules : SecurityRuleConfig list }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            let securityGroup =
                { Name = this.Name
                  Location = location }

            // NSG
            securityGroup
            // Policy Rules
            for priority, rule in List.indexed this.SecurityRules do
                buildNsgRule securityGroup rule ((priority + 1) * 100)
        ]
type NsgBuilder() =
    member __.Yield _ =
        { Name = ResourceName.Empty; SecurityRules = [] }
    /// Sets the name of the network security group
    [<CustomOperation "name">]
    member _.Name(state:NsgConfig, name) = { state with Name = ResourceName name }
    /// Adds rules to this NSG.
    [<CustomOperation "add_rules">]
    member _.AddSecurityRules (state:NsgConfig, rules) = { state with SecurityRules = state.SecurityRules @ rules }

let nsg = NsgBuilder()

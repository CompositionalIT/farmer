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
      Service: Service
      Source: (NetworkProtocol * Endpoint * Port) list
      Destination : Endpoint list
      Operation : Operation }

type SecurityRuleBuilder () =
    member __.Yield _ =
        { Name = ResourceName.Empty
          Description = None
          Service = Service("any", AnyPort)
          Source = [ ]
          Destination = [ ]
          Operation = Allow }
    /// Sets the name of the security rule
    [<CustomOperation "name">]
    member _.Name(state:SecurityRuleConfig, name) = { state with Name = ResourceName name }
    /// Sets the description of the security rule
    [<CustomOperation "description">]
    member _.Description(state:SecurityRuleConfig, description) = { state with Description = Some description }
    /// Sets the service or services protected by this rule.
    [<CustomOperation("service")>]
    member _.Service(state:SecurityRuleConfig, service) = { state with Service = service }
    member this.Service(state:SecurityRuleConfig, (name, port)) = this.Service(state, Service (name, Port(uint16 port)))
    /// Sets the source endpoint that is matched in this rule
    [<CustomOperation("add_source")>]
    member _.AddSource(state:SecurityRuleConfig, source) = { state with Source = source :: state.Source }
    /// Sets the rule to match on any source endpoint.
    [<CustomOperation("add_source_any")>]
    member _.AddSourceAny(state:SecurityRuleConfig, protocol) = { state with Source = (protocol, AnyEndpoint, AnyPort) :: state.Source }
    /// Sets the rule to match on a tagged source endpoint, such as 'Internet'.
    [<CustomOperation("add_source_tag")>]
    member _.AddSourceTag(state:SecurityRuleConfig, protocol, tag) = { state with Source = (protocol, Tag tag, AnyPort) :: state.Source }
    /// Sets the rule to match on a source address.
    [<CustomOperation("add_source_address")>]
    member _.AddSourceAddress(state:SecurityRuleConfig, protocol, sourceAddress) = { state with Source = (protocol, Host (IPAddress.Parse sourceAddress), AnyPort) :: state.Source }
    /// Sets the rule to match on a source network.
    [<CustomOperation("add_source_network")>]
    member _.AddSourceNetwork(state:SecurityRuleConfig, protocol, sourceNetwork) = { state with Source = (protocol, Network (IPAddressCidr.parse sourceNetwork), AnyPort) :: state.Source }
    /// Sets the destination endpoint that is matched in this rule
    [<CustomOperation("add_destination")>]
    member _.AddDestination(state:SecurityRuleConfig, dest) = { state with Destination = dest :: state.Destination }
    /// Sets the rule to match on any destination endpoint.
    [<CustomOperation("add_destination_any")>]
    member _.AddDestinationAny(state:SecurityRuleConfig) = { state with Destination = AnyEndpoint :: state.Destination }
    /// Sets the rule to match on a tagged destination endpoint, such as 'Internet'.
    [<CustomOperation("add_destination_tag")>]
    member _.AddDestinationTag(state:SecurityRuleConfig, tag) = { state with Destination = Tag tag :: state.Destination }
    /// Sets the rule to match on a destination address.
    [<CustomOperation("add_destination_address")>]
    member _.AddDestinationAddress(state:SecurityRuleConfig, destAddress) = { state with Destination = Host (IPAddress.Parse destAddress) :: state.Destination }
    /// Sets the rule to match on a destination network.
    [<CustomOperation("add_destination_network")>]
    member _.AddDestinationNetwork(state:SecurityRuleConfig, destNetwork) = { state with Destination = Network (IPAddressCidr.parse destNetwork):: state.Destination }
    /// Sets the rule to allow this traffic (default value).
    [<CustomOperation("allow")>]
    member _.Allow(state:SecurityRuleConfig) = { state with Operation = Allow }
    /// Sets the rule to deny this traffic.
    [<CustomOperation("deny")>]
    member _.Deny(state:SecurityRuleConfig) = { state with Operation = Deny }

let securityRule = SecurityRuleBuilder()

module SecurityRule =
    let internal buildNsgRule (nsg:NetworkSecurityGroup) (rule:SecurityRuleConfig) (priority:int) =
        let sourcePorts = HashSet()
        let sourceAddresses = HashSet()
        let protocols = HashSet()
        let destPorts = HashSet()
        let destAddresses = rule.Destination

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

        let rec collateRules (service:Service) =
            match service with
            | Service (_, servicePort) ->
                destPorts.Add servicePort |> ignore
                for (protocol, sourceEndpoint, sourcePort) in rule.Source do
                    protocols.Add protocol |> ignore
                    sourcePorts.Add sourcePort |> ignore
                    sourceAddresses.Add sourceEndpoint |> ignore
            | Services (_, services) ->
                for service in services do
                    collateRules service

        collateRules rule.Service

        let sourceAddress, sourceAddresses = wildcardOrTag sourceAddresses
        let destAddress, destAddresses = wildcardOrTag destAddresses

        { Name = rule.Name
          Description = None
          SecurityGroup = nsg
          Protocol = if protocols.Count > 1 then AnyProtocol else protocols |> Seq.head
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
                SecurityRule.buildNsgRule securityGroup rule ((priority + 1) * 100)
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

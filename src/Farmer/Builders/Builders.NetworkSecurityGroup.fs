[<AutoOpen>]

module Farmer.Builders.NetworkSecurityGroup

open Farmer
open Farmer.NetworkSecurity
open Farmer.Arm.NetworkSecurityGroup

/// Network access policy
type SecurityRule =
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
          Operation = Allow
        }
    /// Sets the name of the security rule
    [<CustomOperation "name">]
    member _.Name(state:SecurityRule, name) = { state with Name = ResourceName name }
    /// Sets the description of the security rule
    [<CustomOperation "description">]
    member _.Description(state:SecurityRule, description) = { state with Description = Some description }
    /// Sets the service or services protected by this rule.
    [<CustomOperation("service")>]
    member _.Service(state:SecurityRule, service) = { state with Service = service }
    member _.Service(state:SecurityRule, nameAndPort) = { state with Service = Service (nameAndPort |> fst, nameAndPort |> snd |> uint16 |> Port) }
    /// Sets the source endpoint that is matched in this rule
    [<CustomOperation("add_source")>]
    member _.AddSource(state:SecurityRule, source) = { state with Source = source :: state.Source }
    /// Sets the rule to match on any source endpoint.
    [<CustomOperation("add_source_any")>]
    member _.AddSourceAny(state:SecurityRule, protocol) = { state with Source = (protocol, AnyEndpoint, AnyPort) :: state.Source }
    /// Sets the rule to match on a tagged source endpoint, such as 'Internet'.
    [<CustomOperation("add_source_tag")>]
    member _.AddSourceTag(state:SecurityRule, protocol, tag) = { state with Source = (protocol, Tag tag, AnyPort) :: state.Source }
    /// Sets the rule to match on a source address.
    [<CustomOperation("add_source_address")>]
    member _.AddSourceAddress(state:SecurityRule, protocol, sourceAddress) = { state with Source = (protocol, Host (System.Net.IPAddress.Parse sourceAddress), AnyPort) :: state.Source }
    /// Sets the rule to match on a source network.
    [<CustomOperation("add_source_network")>]
    member _.AddSourceNetwork(state:SecurityRule, protocol, sourceNetwork) = { state with Source = (protocol, Network (IPAddressCidr.parse sourceNetwork), AnyPort) :: state.Source }
    /// Sets the destination endpoint that is matched in this rule
    [<CustomOperation("add_destination")>]
    member _.AddDestination(state:SecurityRule, dest) = { state with Destination = dest :: state.Destination }
    /// Sets the rule to match on any destination endpoint.
    [<CustomOperation("add_destination_any")>]
    member _.AddDestinationAny(state:SecurityRule) = { state with Destination = AnyEndpoint :: state.Destination }
    /// Sets the rule to match on a tagged destination endpoint, such as 'Internet'.
    [<CustomOperation("add_destination_tag")>]
    member _.AddDestinationTag(state:SecurityRule, tag) = { state with Destination = Tag tag :: state.Destination }
    /// Sets the rule to match on a destination address.
    [<CustomOperation("add_destination_address")>]
    member _.AddDestinationAddress(state:SecurityRule, destAddress) = { state with Destination = Host (System.Net.IPAddress.Parse destAddress) :: state.Destination }
    /// Sets the rule to match on a destination network.
    [<CustomOperation("add_destination_network")>]
    member _.AddDestinationNetwork(state:SecurityRule, destNetwork) = { state with Destination = Network (IPAddressCidr.parse destNetwork):: state.Destination }
    /// Sets the rule to allow this traffic (default value).
    [<CustomOperation("allow")>]
    member _.Allow(state:SecurityRule) = { state with Operation = Allow }
    /// Sets the rule to deny this traffic.
    [<CustomOperation("deny")>]
    member _.Deny(state:SecurityRule) = { state with Operation = Deny }

let securityRule = SecurityRuleBuilder()

module SecurityRule =
    let internal buildNsgRule (nsg:NetworkSecurityGroup) (rule:SecurityRule) (priority:int) : IArmResource =
        let sourcePorts = System.Collections.Generic.HashSet<Port> ()
        let sourceAddresses = System.Collections.Generic.HashSet<Endpoint> ()
        let protocols = System.Collections.Generic.HashSet<NetworkProtocol> ()
        let destPorts = System.Collections.Generic.HashSet<Port> ()
        let destAddresses = rule.Destination
        let wildcardOrTag (addresses:Endpoint seq) =
            // Use a wildcard if there is one
            if addresses |> Seq.contains AnyEndpoint then Some AnyEndpoint, []
            else // Use the first tag that is set (only one supported), otherwise use addresses
                addresses
                |> Seq.tryFind (function | Tag _ -> true | _ -> false) |> function
                | Some (Tag tag) -> Some (Tag tag), []
                | _ -> None, addresses |> List.ofSeq

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
          Priority = priority
        } :> IArmResource

type NsgConfig =
    { Name : ResourceName
      SecurityRules : SecurityRule list }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location =
            let nsg = 
                { Name = this.Name
                  Location = location
                }
            let policyRules =
                seq {
                    for rule in this.SecurityRules do
                        yield SecurityRule.buildNsgRule nsg rule
                }
                |> Seq.mapi (fun priority rule -> rule ((priority + 1) * 100))
                |> List.ofSeq
            (nsg :> IArmResource) :: policyRules
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

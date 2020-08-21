[<AutoOpen>]
module Farmer.Arm.NetworkSecurityGroup

open Farmer
open Farmer.CoreTypes
open Farmer.NetworkSecurity

let networkSecurityGroups = ResourceType "Microsoft.Network/networkSecurityGroups"
let securityRules = ResourceType "Microsoft.Network/networkSecurityGroups/securityRules"

type NetworkSecurityGroup =
    { Name : ResourceName
      Location : Location
      Tags: Map<string,string>  }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = networkSecurityGroups.ArmValue
               apiVersion = "2020-04-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               tags = this.Tags |} :> _

let (|SingleEndpoint|ManyEndpoints|) endpoints =
    // Use a wildcard if there is one
    if endpoints |> Seq.contains AnyEndpoint then SingleEndpoint AnyEndpoint
    // Use the first tag that is set (only one supported), otherwise use addresses
    else
        endpoints
        |> Seq.tryFind (function Tag _ -> true | _ -> false)
        |> function
        | Some (Tag tag) ->
            SingleEndpoint (Tag tag)
        | None | Some (AnyEndpoint | Network _ | Host _) ->
            ManyEndpoints (List.ofSeq endpoints)

let (|SinglePort|ManyPorts|) (ports:_ Set) =
    if ports.Contains AnyPort
    then SinglePort AnyPort
    else ManyPorts (ports |> List.ofSeq)

type SecurityRule =
    { Name : ResourceName
      Description : string option
      SecurityGroup : NetworkSecurityGroup
      Protocol : NetworkProtocol
      SourcePorts : Port Set
      DestinationPorts : Port Set
      SourceAddresses : Endpoint list
      DestinationAddresses : Endpoint list
      Access : Operation
      Direction : TrafficDirection
      Priority : int }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = securityRules.ArmValue
               apiVersion = "2020-04-01"
               name = sprintf "%s/%s" this.SecurityGroup.Name.Value this.Name.Value
               dependsOn = [ ArmExpression.resourceId(networkSecurityGroups, this.SecurityGroup.Name).Eval() ]
               properties =
                [ "description", this.Description |> Option.toObj |> box
                  "protocol", box this.Protocol.ArmValue

                  match this.SourcePorts with
                  | SinglePort port ->
                    "sourcePortRange", box port.ArmValue
                    "sourcePortRanges", box []
                  | ManyPorts ports ->
                    "sourcePortRanges", box [ for port in ports -> port.ArmValue ]

                  match this.DestinationPorts with
                  | SinglePort port ->
                    "destinationPortRange", box port.ArmValue
                    "destinationPortRanges", box []
                  | ManyPorts ports ->
                    "destinationPortRanges", box [ for port in ports -> port.ArmValue ]

                  match this.SourceAddresses with
                  | SingleEndpoint endpoint ->
                    "sourceAddressPrefix", box endpoint.ArmValue
                    "sourceAddressPrefixes", box []
                  | ManyEndpoints endpoints ->
                    "sourceAddressPrefixes", box [ for endpoint in endpoints -> endpoint.ArmValue ]

                  match this.DestinationAddresses with
                  | SingleEndpoint endpoint ->
                    "destinationAddressPrefix", endpoint.ArmValue |> box
                    "destinationAddressPrefixes", box []
                  | ManyEndpoints endpoints ->
                    "destinationAddressPrefixes", box [ for endpoint in endpoints -> endpoint.ArmValue ]

                  "access", box this.Access.ArmValue
                  "priority", box this.Priority
                  "direction", box this.Direction.ArmValue
                ] |> Map
            |} :> _

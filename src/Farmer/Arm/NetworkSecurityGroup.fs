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
    }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = networkSecurityGroups.ArmValue
               apiVersion = "2020-04-01"
               name = this.Name.Value
               location = this.Location.ArmValue
            |} :> _

type SecurityRule =
    { Name : ResourceName
      Description : string option
      SecurityGroup : NetworkSecurityGroup
      Protocol : NetworkProtocol
      SourcePort : string // Supports a string, like '*'
      SourcePorts : Port list
      DestinationPort : string // Supports a string, like '*'
      DestinationPorts : Port list
      SourceAddress : string // Supports a string like '*' or tag like 'Internet'
      SourceAddresses : Endpoint list
      DestinationAddress : string // Supports a string like '*' or tag like 'Internet'
      DestinationAddresses : Endpoint list
      Access : Operation
      Direction : TrafficDirection
      Priority : int
    }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = securityRules.ArmValue
               apiVersion = "2020-04-01"
               name = sprintf "%s/%s" this.SecurityGroup.Name.Value this.Name.Value
               dependsOn = [ ArmExpression.resourceId(networkSecurityGroups, this.SecurityGroup.Name).Eval() ]
               properties =
                    {| description = this.Description |> Option.defaultValue null
                       protocol = this.Protocol.ArmValue
                       sourcePortRanges = this.SourcePorts |> List.map Port.ArmValue
                       sourcePortRange = this.SourcePort
                       destinationPortRanges = this.DestinationPorts |> List.map Port.ArmValue
                       destinationPortRange = this.DestinationPort
                       sourceAddressPrefix = this.SourceAddress
                       sourceAddressPrefixes = this.SourceAddresses |> List.map Endpoint.ArmValue
                       destinationAddressPrefix = this.DestinationAddress
                       destinationAddressPrefixes = this.DestinationAddresses |> List.map Endpoint.ArmValue
                       access = this.Access.ArmValue 
                       priority = this.Priority
                       direction = this.Direction.ArmValue
                    |}
            |} :> _

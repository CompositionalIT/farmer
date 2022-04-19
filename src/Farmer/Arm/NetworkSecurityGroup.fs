[<AutoOpen>]
module Farmer.Arm.NetworkSecurityGroup

open Farmer
open Farmer.NetworkSecurity

let networkSecurityGroups = ResourceType ("Microsoft.Network/networkSecurityGroups", "2020-04-01")
let securityRules = ResourceType ("Microsoft.Network/networkSecurityGroups/securityRules", "2020-04-01")

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

let private (|SinglePort|ManyPorts|) (ports:_ Set) =
    if ports.Contains AnyPort
    then SinglePort AnyPort
    else ManyPorts (ports |> List.ofSeq)

module private EndpointWriter =
    let toPrefixes = function
        | SingleEndpoint _ -> []
        | ManyEndpoints endpoints -> [ for endpoint in endpoints -> endpoint.ArmValue ]
    let toPrefix = function
        | SingleEndpoint endpoint -> endpoint.ArmValue
        | ManyEndpoints _ -> null
    let toRange = function
        | SinglePort p -> box p.ArmValue
        | ManyPorts _ -> null
    let toRanges = function
        | SinglePort _ -> []
        | ManyPorts ports -> [ for port in ports -> port.ArmValue ]

type SecurityRule =
    { Name : ResourceName
      Description : string option
      SecurityGroup : ResourceName
      Protocol : NetworkProtocol
      SourcePorts : Port Set
      DestinationPorts : Port Set
      SourceAddresses : Endpoint list
      DestinationAddresses : Endpoint list
      Access : Operation
      Direction : TrafficDirection
      Priority : int }
    member this.PropertiesModel =
        {| description = this.Description |> Option.toObj
           protocol = this.Protocol.ArmValue
           sourcePortRange = this.SourcePorts |> EndpointWriter.toRange
           sourcePortRanges = this.SourcePorts |> EndpointWriter.toRanges
           destinationPortRange = this.DestinationPorts |> EndpointWriter.toRange
           destinationPortRanges = this.DestinationPorts |> EndpointWriter.toRanges
           sourceAddressPrefix = this.SourceAddresses |> EndpointWriter.toPrefix
           sourceAddressPrefixes = this.SourceAddresses |> EndpointWriter.toPrefixes
           destinationAddressPrefix = this.DestinationAddresses |> EndpointWriter.toPrefix
           destinationAddressPrefixes = this.DestinationAddresses |> EndpointWriter.toPrefixes
           access = this.Access.ArmValue
           priority = this.Priority
           direction = this.Direction.ArmValue
        |}
    interface IArmResource with
        member this.ResourceId = securityRules.resourceId (this.SecurityGroup/this.Name)

        member this.JsonModel =
            let dependsOn = [ networkSecurityGroups.resourceId this.SecurityGroup ]
            {| securityRules.Create(this.SecurityGroup/this.Name, dependsOn = dependsOn) with
                properties = this.PropertiesModel
            |}

type NetworkSecurityGroup =
    { Name : ResourceName
      Location : Location
      SecurityRules : SecurityRule list
      Tags: Map<string,string>  }
    interface IArmResource with
        member this.ResourceId = networkSecurityGroups.resourceId this.Name
        member this.JsonModel =
            {| networkSecurityGroups.Create(this.Name, this.Location, tags = this.Tags) with
                properties =
                    {| securityRules =
                        this.SecurityRules |> List.map (fun rule ->
                            {| name = rule.Name.Value
                               ``type`` = securityRules.Type
                               properties = rule.PropertiesModel
                            |} )
                    |}
            |}

[<AutoOpen>]
module Farmer.Arm.NetworkSecurityGroup

open Farmer
open Farmer.NetworkSecurity

let applicationSecurityGroups =
    ResourceType("Microsoft.Network/applicationSecurityGroups", "2023-04-01")

let networkSecurityGroups =
    ResourceType("Microsoft.Network/networkSecurityGroups", "2022-07-01")

let securityRules =
    ResourceType("Microsoft.Network/networkSecurityGroups/securityRules", "2022-07-01")

let (|SingleEndpoint|ManyEndpoints|) endpoints =
    // Use a wildcard if there is one
    if endpoints |> Seq.contains AnyEndpoint then
        SingleEndpoint AnyEndpoint
    // Use the first tag that is set (only one supported), otherwise use addresses
    else
        endpoints
        |> Seq.tryFind (function
            | Tag _ -> true
            | _ -> false)
        |> function
            | Some(Tag tag) -> SingleEndpoint(Tag tag)
            | None
            | Some(AnyEndpoint | Network _ | Host _ | ApplicationSecurityGroup _ | Expression _) ->
                ManyEndpoints(List.ofSeq endpoints)

let private (|SinglePort|ManyPorts|) (ports: _ Set) =
    if ports.Contains AnyPort then
        SinglePort AnyPort
    else
        ManyPorts(ports |> List.ofSeq)

module private EndpointWriter =
    let toPrefixes =
        function
        | SingleEndpoint _ -> []
        | ManyEndpoints endpoints -> [ for endpoint in endpoints -> endpoint.ArmValue ]

    let toPrefix =
        function
        | SingleEndpoint endpoint -> endpoint.ArmValue
        | ManyEndpoints _ -> null

    let toRange =
        function
        | SinglePort p -> box p.ArmValue
        | ManyPorts _ -> null

    let toRanges =
        function
        | SinglePort _ -> []
        | ManyPorts ports -> [ for port in ports -> port.ArmValue ]

type SecurityRule = {
    Name: ResourceName
    Dependencies: ResourceId Set
    Description: string option
    SecurityGroup: LinkedResource
    Protocol: NetworkProtocol
    SourceAddresses: Endpoint list
    SourceApplicationSecurityGroups: LinkedResource list
    SourcePorts: Port Set
    DestinationAddresses: Endpoint list
    DestinationApplicationSecurityGroups: LinkedResource list
    DestinationPorts: Port Set
    Access: Operation
    Direction: TrafficDirection
    Priority: int
} with

    /// Get any managed application security group resource IDs.
    static member internal AllDependencies securityRule =
        securityRule.SourceApplicationSecurityGroups
        @ securityRule.DestinationApplicationSecurityGroups
        |> List.choose (function
            | Managed id -> Some id
            | _ -> None)
        |> Set.ofList
        |> Set.union securityRule.Dependencies

    member this.PropertiesModel = {|
        description = this.Description |> Option.toObj
        protocol = this.Protocol.ArmValue
        sourcePortRange = this.SourcePorts |> EndpointWriter.toRange
        sourcePortRanges = this.SourcePorts |> EndpointWriter.toRanges
        sourceApplicationSecurityGroups = this.SourceApplicationSecurityGroups |> List.map LinkedResource.AsIdObject
        destinationPortRange = this.DestinationPorts |> EndpointWriter.toRange
        destinationPortRanges = this.DestinationPorts |> EndpointWriter.toRanges
        sourceAddressPrefix = this.SourceAddresses |> EndpointWriter.toPrefix
        sourceAddressPrefixes = this.SourceAddresses |> EndpointWriter.toPrefixes
        destinationAddressPrefix = this.DestinationAddresses |> EndpointWriter.toPrefix
        destinationAddressPrefixes = this.DestinationAddresses |> EndpointWriter.toPrefixes
        destinationApplicationSecurityGroups =
            this.DestinationApplicationSecurityGroups |> List.map LinkedResource.AsIdObject
        access = this.Access.ArmValue
        priority = this.Priority
        direction = this.Direction.ArmValue
    |}

    interface IArmResource with
        member this.ResourceId = securityRules.resourceId (this.SecurityGroup.Name / this.Name)

        member this.JsonModel =
            let dependsOn =
                this.Dependencies |> LinkedResource.addToSetIfManaged this.SecurityGroup

            {|
                securityRules.Create(this.SecurityGroup.Name / this.Name, dependsOn = dependsOn) with
                    properties = this.PropertiesModel
            |}

type NetworkSecurityGroup = {
    Name: ResourceName
    Location: Location
    Dependencies: ResourceId Set
    SecurityRules: SecurityRule list
    Tags: Map<string, string>
} with

    interface IArmResource with
        member this.ResourceId = networkSecurityGroups.resourceId this.Name

        member this.JsonModel =
            let dependencies =
                [
                    this.Dependencies
                    yield! this.SecurityRules |> List.map SecurityRule.AllDependencies
                ]
                |> Set.unionMany

            {|
                networkSecurityGroups.Create(this.Name, this.Location, dependsOn = dependencies, tags = this.Tags) with
                    properties = {|
                        securityRules =
                            this.SecurityRules
                            |> List.map (fun rule -> {|
                                name = rule.Name.Value
                                ``type`` = securityRules.Type
                                properties = rule.PropertiesModel
                            |})
                    |}
            |}

type ApplicationSecurityGroup = {
    Name: ResourceName
    Dependencies: ResourceId Set
    Location: Location
    Tags: Map<string, string>
} with

    interface IArmResource with
        member this.ResourceId = networkSecurityGroups.resourceId this.Name

        member this.JsonModel =
            applicationSecurityGroups.Create(this.Name, this.Location, dependsOn = this.Dependencies, tags = this.Tags)
[<AutoOpen>]
module Farmer.Builders.NatGateway

open Farmer
open Farmer.Arm.Network
open Farmer.PublicIpAddress

type NatGatewayConfig = {
    Name: ResourceName
    IdleTimeout: int<Minutes>
    Sku: NatGateway.Sku
    Tags: Map<string, string>
} with

    interface IBuilder with
        member this.ResourceId = natGateways.resourceId this.Name

        member this.BuildResources location = [
            // Currently generate with a single public IP.
            {
                PublicIpAddress.Name = ResourceName $"{this.Name.Value}-publicip-1"
                AvailabilityZones = NoZone
                Location = location
                Sku =
                    match this.Sku with
                    | Farmer.NatGateway.Sku.Standard -> PublicIpAddress.Sku.Standard
                    | Farmer.NatGateway.Sku.StandardV2 -> PublicIpAddress.Sku.StandardV2
                AllocationMethod = AllocationMethod.Static
                AddressVersion = Network.AddressVersion.IPv4
                DomainNameLabel = None
                Tags = this.Tags
            }
            {
                NatGateway.Name = this.Name
                Sku = this.Sku
                Location = location
                PublicIpAddresses = [
                    LinkedResource.Managed(publicIPAddresses.resourceId $"{this.Name.Value}-publicip-1")
                ]
                PublicIpPrefixes = []
                IdleTimeout = this.IdleTimeout
                Tags = this.Tags
            }
        ]

type NatGatewayBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        IdleTimeout = 4<Minutes>
        Sku = NatGateway.Sku.Standard
        Tags = Map.empty
    }

    [<CustomOperation "name">]
    member _.Name(state: NatGatewayConfig, name: string) = { state with Name = ResourceName name }

    [<CustomOperation "idle_timeout">]
    member _.SetIdleTimeout(state: NatGatewayConfig, idleTimeout: int<Minutes>) =
        if idleTimeout > 120<Minutes> then
            raiseFarmer "Maximum idle timeout is 120 minutes."

        { state with IdleTimeout = idleTimeout }

    [<CustomOperation "sku">]
    member _.Sku(state: NatGatewayConfig, sku: NatGateway.Sku) = { state with Sku = sku }

let natGateway = NatGatewayBuilder()
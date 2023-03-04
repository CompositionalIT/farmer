[<AutoOpen>]
module Farmer.Builders.NatGateway

open Farmer
open Farmer.Arm.Network
open Farmer.PublicIpAddress

type NatGatewayConfig =
    {
        Name: ResourceName
        IdleTimeout: int<Minutes>
        Tags: Map<string, string>
    }

    interface IBuilder with
        member this.ResourceId = natGateways.resourceId this.Name

        member this.BuildResources location = [
            // Currently just generate with a single public IP.
            {
                PublicIpAddress.Name = ResourceName $"{this.Name.Value}-publicip-1"
                AvailabilityZone = None
                Location = location
                Sku = Sku.Standard
                AllocationMethod = AllocationMethod.Static
                DomainNameLabel = None
                Tags = this.Tags
            }
            {
                NatGateway.Name = this.Name
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
        Tags = Map.empty
    }

    [<CustomOperation "name">]
    member _.Name(state: NatGatewayConfig, name: string) = { state with Name = ResourceName name }

    [<CustomOperation "idle_timeout">]
    member _.SetIdleTimeout(state: NatGatewayConfig, idleTimeout: int<Minutes>) =
        if idleTimeout > 120<Minutes> then
            raiseFarmer "Maximum idle timeout is 120 minutes."

        { state with IdleTimeout = idleTimeout }

let natGateway = NatGatewayBuilder()

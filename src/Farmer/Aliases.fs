[<AutoOpen>]
module Farmer.Aliases

[<AutoOpen>]
module BuilderExtensions =
    open Farmer.Builders
    open Farmer.Arm.Network

    type IPrivateEndpoints<'TConfig> with

        member this.AddPrivateEndpoint(state: 'TConfig, subnetId: LinkedResource) =
            this.AddPrivateEndpoint(state, SubnetReference.create subnetId)

        member this.AddPrivateEndpoint(state: 'TConfig, subnet: SubnetConfig) =
            this.AddPrivateEndpoint(state, SubnetReference.create subnet)

        member this.AddPrivateEndpoint(state, (subnetRef: LinkedResource, epName)) =
            this.AddPrivateEndpoint(state, (SubnetReference.create subnetRef, epName))

        member this.AddPrivateEndpoint(state: 'TConfig, (vnetRef, subnetName): LinkedResource * ResourceName) =
            this.AddPrivateEndpoint(state, SubnetReference.create (vnetRef, subnetName))

        member this.AddPrivateEndpoint(state, (vnetRef, subnetName, epName): LinkedResource * ResourceName * string) =
            this.AddPrivateEndpoint(state, ((SubnetReference.create (vnetRef, subnetName)), epName))

        member this.AddPrivateEndpoint(state: 'TConfig, (vnet, subnetName): VirtualNetworkConfig * ResourceName) =
            this.AddPrivateEndpoint(state, SubnetReference.create (vnet, subnetName))

        member this.AddPrivateEndpoints(state: 'TConfig, subnetIds: LinkedResource list) =
            this.AddPrivateEndpoints(state, subnetIds |> List.map SubnetReference.create |> Set)

        member this.AddPrivateEndpoints(state: 'TConfig, subnets: SubnetConfig list) =
            this.AddPrivateEndpoints(state, subnets |> List.map SubnetReference.create |> Set)

let arm = Farmer.Builders.ResourceGroup.DeploymentBuilder()
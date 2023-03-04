[<AutoOpen>]
module Farmer.Builders.PrivateEndpoint

open Farmer
open Farmer.Arm
open Farmer.Arm.Network

type PrivateEndpointConfig =
    {
        Name: ResourceName
        Subnet: SubnetReference option
        Resource: LinkedResource option
        CustomNetworkInterfaceName: string option
        GroupIds: string list
    }

    interface IBuilder with
        member this.ResourceId = privateEndpoints.resourceId this.Name

        member this.BuildResources location = [
            match this.Subnet, this.Resource with
            | Some subnet, Some resource -> {
                PrivateEndpoint.Name = this.Name
                Location = location
                Subnet = subnet
                Resource = resource
                CustomNetworkInterfaceName = this.CustomNetworkInterfaceName
                GroupIds = []
              }
            | _ ->
                raiseFarmer
                    $"Subnet and Resource must be specified. Subnet: '{this.Subnet}' Resource: '{this.Resource}'"
        ]

    /// If a CustomNetworkInterfaceName is set via 'custom_nic_name', this returns the private IP.
    member this.CustomNicEndpointIP(idx: int) : ArmExpression option =
        this.CustomNetworkInterfaceName
        |> Option.map (fun customNicName ->
            let nicId = ResourceId.create (networkInterfaces, ResourceName customNicName)

            $"reference({nicId.ArmExpression.Value}, '{networkInterfaces.ApiVersion}').ipConfigurations[{idx}].properties.privateIpAddress"
            |> ArmExpression.create)

    member this.CustomNicFirstEndpointIP = this.CustomNicEndpointIP 0

type PrivateEndpointBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        Subnet = None
        Resource = None
        CustomNetworkInterfaceName = None
        GroupIds = []
    }

    [<CustomOperation "name">]
    member _.Name(state: PrivateEndpointConfig, name: string) = { state with Name = ResourceName name }

    [<CustomOperation "subnet_reference">]
    member _.Subnet(state: PrivateEndpointConfig, subnetReference: SubnetReference) =
        { state with
            Subnet = Some subnetReference
        }

    [<CustomOperation "link_to_subnet">]
    member _.LinkToSubnet(state: PrivateEndpointConfig, subnetId: ResourceId) =
        { state with
            Subnet = Some(SubnetReference.Direct(Managed subnetId))
        }

    [<CustomOperation "link_to_unmanaged_subnet">]
    member _.LinkToUnmanagedSubnet(state: PrivateEndpointConfig, subnetId: ResourceId) =
        { state with
            Subnet = Some(SubnetReference.Direct(Unmanaged subnetId))
        }

    [<CustomOperation "resource">]
    member _.Resource(state: PrivateEndpointConfig, resource: LinkedResource) = { state with Resource = Some resource }

    [<CustomOperation "custom_nic_name">]
    member _.CustomNetworkInterfaceName(state: PrivateEndpointConfig, customNicName: string) =
        { state with
            CustomNetworkInterfaceName = Some customNicName
        }

    [<CustomOperation "add_group_ids">]
    member _.AddGroupIds(state: PrivateEndpointConfig, groupIds: string list) =
        { state with
            GroupIds = state.GroupIds @ groupIds
        }

let privateEndpoint = PrivateEndpointBuilder()

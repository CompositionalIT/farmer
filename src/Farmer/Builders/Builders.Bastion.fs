[<AutoOpen>]
module Farmer.Builders.Bastion

open Farmer
open Farmer.Arm.Bastion
open Farmer.Arm.Network
open Farmer.PublicIpAddress

type BastionConfig =
    { Name : ResourceName
      VirtualNetwork : ResourceName
      Tags : Map<string, string> }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location =
            let publicIpName = sprintf "%s-ip" this.Name.Value |> ResourceName
            [
                // IP Address
                { Name = publicIpName
                  Location = location
                  AllocationMethod = AllocationMethod.Static
                  Sku = PublicIpAddress.Sku.Standard
                  DomainNameLabel = None
                  Tags = this.Tags }
                // Bastion
                { BastionHost.Name = this.Name
                  Location = location
                  VirtualNetwork = this.VirtualNetwork
                  IpConfigs = [
                      {| PublicIpName = publicIpName |}
                  ]
                  Tags = this.Tags
                }
            ]

type BastionBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          VirtualNetwork = ResourceName.Empty
          Tags = Map.empty }
    /// Sets the name of the bastion host.
    [<CustomOperation "name">]
    member __.Name(state:BastionConfig, name) = { state with Name = ResourceName name }
    /// Sets the virtual network where this bastion host is attached.
    [<CustomOperation "vnet">]
    member __.VNet(state:BastionConfig, vnet) = { state with VirtualNetwork = ResourceName vnet }

let bastion = BastionBuilder()

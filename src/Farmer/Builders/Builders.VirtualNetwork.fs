[<AutoOpen>]
module Farmer.Builders.VirtualNetwork

open Farmer
open Farmer.CoreTypes
open Farmer.Arm.Network
open Helpers

type SubnetDelegationService =
    /// Microsoft.ApiManagement/service
    static member ApiManagementService = "Microsoft.ApiManagement/service"
    /// Microsoft.AzureCosmosDB/clusters
    static member CosmosDBClusters = "Microsoft.AzureCosmosDB/clusters"
    /// Microsoft.BareMetal/AzureVMware
    static member BareMetalVMware = "Microsoft.BareMetal/AzureVMware"
    /// Microsoft.BareMetal/CrayServers
    static member BareMetalCrayServers = "Microsoft.BareMetal/CrayServers"
    /// Microsoft.Batch/batchAccounts
    static member BatchAccounts = "Microsoft.Batch/batchAccounts"
    /// Microsoft.ContainerInstance/containerGroups
    static member ContainerGroups = "Microsoft.ContainerInstance/containerGroups"
    /// Microsoft.Databricks/workspaces
    static member DatabricksWorkspaces = "Microsoft.Databricks/workspaces"
    /// Microsoft.MachineLearningServices/workspaces
    static member MachineLearningWorkspaces = "Microsoft.MachineLearningServices/workspaces"
    /// Microsoft.Netapp/volumes
    static member NetappVolumes = "Microsoft.Netapp/volumes"
    /// Microsoft.ServiceFabricMesh/networks
    static member ServiceFabricMeshNetworks = "Microsoft.ServiceFabricMesh/networks"
    /// Microsoft.Sql/managedInstances
    static member SqlManagedInstances = "Microsoft.Sql/managedInstances"

type SubnetConfig =
    { Name: ResourceName
      Prefix: IPAddressCidr
      Delegations: string list }

type SubnetBuilder() =
    member __.Yield _ = { Name = ResourceName.Empty; Prefix = { Address = System.Net.IPAddress.Parse("10.100.0.0"); Prefix = 16 }; Delegations = [] }
    /// Sets the name of the subnet
    [<CustomOperation "name">]
    member __.Name(state:SubnetConfig, name) = { state with Name = ResourceName name }
    /// Sets the network prefix in CIDR notation
    [<CustomOperation "prefix">]
    member __.Prefix(state:SubnetConfig, prefix) = { state with Prefix = IPAddressCidr.parse prefix }
    /// Sets the network prefix in CIDR notation
    [<CustomOperation "add_delegations">]
    member __.AddDelegations(state:SubnetConfig, delegations) = { state with Delegations = state.Delegations @ delegations }

let subnet = SubnetBuilder ()
/// Specification for a subnet to build from an address space.
type SubnetBuildSpec =
    { Name: string
      Size: int
      Delegations: string list }
/// Builds a subnet of a certain CIDR block size.
let build_subnet name size =
    { Name = name; Size = size; Delegations = [] }
/// Builds a subnet of a certain CIDR block size with service delegations.
let build_subnet_delegations name size delegations =
    { Name = name; Size = size; Delegations = delegations }

/// A specification building an address space and subnets.
type AddressSpaceSpec =
    { Space : string
      Subnets : SubnetBuildSpec list }
/// Builder for an address space with automatically carved subnets.
type AddressSpaceBuilder() =
    member __.Yield _ = { Space = ""; Subnets = [] }
    [<CustomOperation("space")>]
    member __.Space(state:AddressSpaceSpec, space) = { state with Space = space }
    [<CustomOperation("subnets")>]
    member __.Subnets(state:AddressSpaceSpec, subnets) = { state with Subnets = subnets }

let address_space = AddressSpaceBuilder ()

type VirtualNetworkConfig =
    { Name : ResourceName
      AddressSpacePrefixes : string list
      Subnets : SubnetConfig list
      Tags: Map<string,string>  }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              AddressSpacePrefixes = this.AddressSpacePrefixes
              Subnets = this.Subnets |> List.map (fun subnetConfig ->
                  {| Name = subnetConfig.Name
                     Prefix = IPAddressCidr.format subnetConfig.Prefix
                     Delegations = subnetConfig.Delegations |> List.map (fun delegation ->
                         {| Name = ResourceName delegation; ServiceName = delegation |})
                  |})
              Tags = this.Tags
            }
        ]

type VirtualNetworkBuilder() =
    member __.Yield _ =
      { Name = ResourceName.Empty
        AddressSpacePrefixes = []
        Subnets = []
        Tags = Map.empty }
    /// Sets the name of the virtual network
    [<CustomOperation "name">]
    member __.Name(state:VirtualNetworkConfig, name) = { state with Name = ResourceName name }
    /// Adds address spaces prefixes
    [<CustomOperation "add_address_spaces">]
    member __.AddAddressSpaces(state:VirtualNetworkConfig, prefixes) = { state with AddressSpacePrefixes = state.AddressSpacePrefixes @ prefixes }
    /// Adds subnets
    [<CustomOperation "add_subnets">]
    member __.AddSubnets(state:VirtualNetworkConfig, subnets) = { state with Subnets = state.Subnets @ subnets }
    [<CustomOperation "build_address_spaces">]
    member __.BuildAddressSpaces(state:VirtualNetworkConfig, addressSpaces:AddressSpaceSpec list) =
        let newSubnets =
            addressSpaces |> List.map (
                fun addressSpaceConfig ->
                    let addressSpace = addressSpaceConfig.Space |> IPAddressCidr.parse
                    let subnetCidrs =
                        IPAddressCidr.carveAddressSpace addressSpace
                            (addressSpaceConfig.Subnets
                            |> Seq.map (fun subnet ->
                                if subnet.Size > 29 then
                                    invalidArg "size" (sprintf "Subnet must be of /29 or larger, cannot carve subnet %s of /%d" subnet.Name subnet.Size)
                                subnet.Size)
                            |> List.ofSeq)
                    Seq.zip (addressSpaceConfig.Subnets |> Seq.map (fun s -> s.Name, s.Delegations)) subnetCidrs
                    |> Seq.map (fun ((name, delegations), cidr) ->
                        {
                            Name = ResourceName name
                            Prefix = cidr
                            Delegations = delegations
                        }
                    )
                ) |> Seq.concat
        let newAddressSpaces = addressSpaces |> Seq.map (fun addressSpace -> addressSpace.Space)
        { state
          with Subnets = state.Subnets |> Seq.append newSubnets |> List.ofSeq
               AddressSpacePrefixes = state.AddressSpacePrefixes |> Seq.append newAddressSpaces |> List.ofSeq }
      [<CustomOperation "add_tags">]
      member _.Tags(state:VirtualNetworkConfig, pairs) =
          { state with
              Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
      [<CustomOperation "add_tag">]
      member this.Tag(state:VirtualNetworkConfig, key, value) = this.Tags(state, [ (key,value) ])

let vnet = VirtualNetworkBuilder ()
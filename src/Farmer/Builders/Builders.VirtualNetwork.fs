[<AutoOpen>]
module Farmer.Builders.VirtualNetwork

open Farmer
open Farmer.CoreTypes
open Farmer.Arm.Network

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
    member __.Yield _ = { Name = ResourceName.Empty; Prefix = {| Address = System.Net.IPAddress.Parse("10.100.0.0"); Prefix = 16 |}; Delegations = [] }
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
    
type VirtualNetworkConfig =
    { Name : ResourceName
      AddressSpacePrefixes : string list
      Subnets : SubnetConfig list; }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location _ = [
            { Name = this.Name
              Location = location
              AddressSpacePrefixes = this.AddressSpacePrefixes
              Subnets = this.Subnets |> List.map (fun subnetConfig ->
                  {| Name = subnetConfig.Name
                     Prefix = (sprintf "%O/%d" subnetConfig.Prefix.Address subnetConfig.Prefix.Prefix)
                     Delegations = subnetConfig.Delegations |> List.map (fun delegation ->
                         {| Name = ResourceName delegation; ServiceName = delegation |})
                  |})
            }
        ]

type VirtualNetworkBuilder() =
    member __.Yield _ = { Name = ResourceName.Empty; AddressSpacePrefixes = []; Subnets = [] }
    /// Sets the name of the virtual network
    [<CustomOperation "name">]
    member __.Name(state:VirtualNetworkConfig, name) = { state with Name = ResourceName name }
    /// Adds address spaces prefixes
    [<CustomOperation "add_address_spaces">]
    member __.AddAddressSpaces(state:VirtualNetworkConfig, prefixes) = { state with AddressSpacePrefixes = state.AddressSpacePrefixes @ prefixes }
    /// Adds subnets
    [<CustomOperation "add_subnets">]
    member __.AddSubnets(state:VirtualNetworkConfig, subnets) = { state with Subnets = state.Subnets @ subnets }

let vnet = VirtualNetworkBuilder ()
[<AutoOpen>]
module Farmer.Builders.PrivateLink

open Farmer
open Farmer.Arm.PrivateLink
open System
open System.Net.Sockets
open Farmer.Builders

type PrivateLinkServiceIpConfig =
    {
        Name: ResourceName
        PrivateIpAllocationMethod: AllocationMethod
        PrivateIpAddressVersion: AddressFamily
        Primary: bool
        SubnetId: ResourceId option
    }

    static member internal BuildResource(ipConfig: PrivateLinkServiceIpConfig) =
        match ipConfig.SubnetId with
        | None -> raiseFarmer "Private link service IP config requires a subnet"
        | Some subnetId when subnetId.Type <> Farmer.Arm.Network.subnets ->
            raiseFarmer "Private link service IP config subnet resource ID must be a subnet resource Id"
        | Some subnetId ->
            {|
                Name = ipConfig.Name.IfEmpty $"{subnetId.Name.Value}-{subnetId.Segments.Head.Value}"
                Primary = ipConfig.Primary
                PrivateIpAllocationMethod = ipConfig.PrivateIpAllocationMethod
                PrivateIpAddressVersion = ipConfig.PrivateIpAddressVersion
                SubnetId = subnetId
            |}

type PrivateLinkServiceIpConfigBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            PrivateIpAllocationMethod = AllocationMethod.DynamicPrivateIp
            PrivateIpAddressVersion = AddressFamily.InterNetwork
            Primary = false
            SubnetId = None
        }

    [<CustomOperation "name">]
    member _.Name(state: PrivateLinkServiceIpConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "private_ip_allocation">]
    member _.PrivateIpAllocation(state: PrivateLinkServiceIpConfig, allocationMethod: AllocationMethod) =
        { state with
            PrivateIpAllocationMethod = allocationMethod
        }

    [<CustomOperation "private_ip_address_version">]
    member _.PrivateIpAddressVersion(state: PrivateLinkServiceIpConfig, addressFamily: AddressFamily) =
        { state with
            PrivateIpAddressVersion = addressFamily
        }

    [<CustomOperation "primary">]
    member _.Primary(state: PrivateLinkServiceIpConfig, primary: bool) = { state with Primary = primary }

    [<CustomOperation "link_to_subnet">]
    member _.SubnetId(state: PrivateLinkServiceIpConfig, subnetId: ResourceId) = { state with SubnetId = Some subnetId }

let privateLinkIpConfig = PrivateLinkServiceIpConfigBuilder()

type PrivateLinkServiceConfig =
    {
        Name: ResourceName
        Dependencies: ResourceId Set
        AutoApprovedSubscriptions: Guid list
        EnableProxyProtocol: bool option
        LoadBalancerFrontendIpConfigIds: ResourceId list
        IpConfigs: PrivateLinkServiceIpConfig list
        VisibleToSubscriptions: Guid list
        Tags: Map<string, string>
    }

    interface IBuilder with
        member this.ResourceId = privateLinkServices.resourceId (this.Name)

        member this.BuildResources location =
            [
                {
                    PrivateLinkService.Name = this.Name
                    Location = location
                    Dependencies = this.Dependencies
                    AutoApprovedSubscriptions = this.AutoApprovedSubscriptions
                    EnableProxyProtocol = this.EnableProxyProtocol |> Option.defaultValue false
                    LoadBalancerFrontendIpConfigIds = this.LoadBalancerFrontendIpConfigIds
                    IpConfigs = this.IpConfigs |> List.map PrivateLinkServiceIpConfig.BuildResource
                    VisibleToSubscriptions = this.VisibleToSubscriptions
                    Tags = this.Tags
                }
            ]

type PrivateLinkBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            Dependencies = Set.empty
            AutoApprovedSubscriptions = []
            EnableProxyProtocol = None
            LoadBalancerFrontendIpConfigIds = []
            IpConfigs = []
            VisibleToSubscriptions = []
            Tags = Map.empty
        }

    interface IDependable<PrivateLinkServiceConfig> with
        member _.Add state newDeps =
            { state with
                Dependencies = state.Dependencies + newDeps
            }

    interface ITaggable<StorageAccountConfig> with
        member _.Add state tags =
            { state with
                Tags = state.Tags |> Map.merge tags
            }

    [<CustomOperation "name">]
    member _.Name(state: PrivateLinkServiceConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "add_auto_approved_subscriptions">]
    member _.AddAutoApprovedSubscriptions(state: PrivateLinkServiceConfig, subscriptions) =
        { state with
            AutoApprovedSubscriptions = state.AutoApprovedSubscriptions @ subscriptions
        }

    [<CustomOperation "add_load_balancer_frontend_ids">]
    member _.AddLoadBalancerFrontendIpConfigs(state: PrivateLinkServiceConfig, lbFrontendIpConfigs) =
        { state with
            LoadBalancerFrontendIpConfigIds = state.LoadBalancerFrontendIpConfigIds @ lbFrontendIpConfigs
        }

    [<CustomOperation "add_ip_configs">]
    member _.AddIpConfigs(state: PrivateLinkServiceConfig, ipConfigs) =
        { state with
            IpConfigs = state.IpConfigs @ ipConfigs
        }

    [<CustomOperation "proxy_protocol">]
    member _.ProxyProtocol(state: PrivateLinkServiceConfig, flag: FeatureFlag) =
        { state with
            EnableProxyProtocol = Some flag.AsBoolean
        }

    [<CustomOperation "add_visible_to_subscriptions">]
    member _.AddVisibleToSubscriptions(state: PrivateLinkServiceConfig, subscriptions) =
        { state with
            VisibleToSubscriptions = state.VisibleToSubscriptions @ subscriptions
        }

let privateLink = PrivateLinkBuilder()

module Farmer.Arm.PrivateLink

open System
open System.Net.Sockets
open Farmer

let privateLinkServices =
    ResourceType("Microsoft.Network/privateLinkServices", "2021-08-01")

type PrivateLinkService = {
    Name: ResourceName
    Location: Location
    Dependencies: ResourceId Set
    AutoApprovedSubscriptions: Guid list
    EnableProxyProtocol: bool
    LoadBalancerFrontendIpConfigIds: ResourceId list
    IpConfigs:
        {|
            Name: ResourceName
            PrivateIpAllocationMethod: AllocationMethod
            PrivateIpAddressVersion: AddressFamily
            Primary: bool
            SubnetId: ResourceId
        |} list
    VisibleToSubscriptions: Guid list
    Tags: Map<string, string>
} with

    interface IArmResource with
        member this.ResourceId = privateLinkServices.resourceId this.Name

        member this.JsonModel = {|
            privateLinkServices.Create(this.Name, this.Location, this.Dependencies, this.Tags) with
                properties = {|
                    autoApproval =
                        match this.AutoApprovedSubscriptions with
                        | [] -> Unchecked.defaultof<_>
                        | approvedSubscriptions -> {|
                            subscriptions = approvedSubscriptions
                          |}
                    enableProxyProtocol = this.EnableProxyProtocol
                    loadBalancerFrontendIpConfigurations =
                        this.LoadBalancerFrontendIpConfigIds
                        |> List.map (fun frontend -> {| id = frontend.Eval() |})
                    ipConfigurations =
                        this.IpConfigs
                        |> List.map (fun ipconfig -> {|
                            name = ipconfig.Name.Value
                            properties = {|
                                primary = ipconfig.Primary
                                privateIPAllocationMethod =
                                    match ipconfig.PrivateIpAllocationMethod with
                                    | DynamicPrivateIp -> "Dynamic"
                                    | StaticPrivateIp _ -> "Static"
                                privateIPAddress =
                                    match ipconfig.PrivateIpAllocationMethod with
                                    | DynamicPrivateIp -> null
                                    | StaticPrivateIp ip -> string ip
                                privateIPAddressVersion =
                                    match ipconfig.PrivateIpAddressVersion with
                                    | AddressFamily.InterNetwork -> "IPv4"
                                    | AddressFamily.InterNetworkV6 -> "IPv6"
                                    | _ ->
                                        raiseFarmer
                                            "Unsupported PrivateIpAddressVersion - should be InterNetwork (IPv4) or InterNetworkV6 (IPv6)."
                                subnet = {| id = ipconfig.SubnetId.Eval() |}
                            |}
                        |})
                    visibility =
                        match this.AutoApprovedSubscriptions @ this.VisibleToSubscriptions |> List.distinct with
                        | [] -> Unchecked.defaultof<_>
                        | visibleTo -> {| subscriptions = visibleTo |}
                |}
        |}

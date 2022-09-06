[<AutoOpen>]
module Farmer.Builders.RouteTable

open Farmer
open Farmer.Arm

type RouteConfig =
    {
        Name: ResourceName
        AddressPrefix: IPAddressCidr
        NextHopType: HopType
        NextHopIpAddress: IPAddressCidr
        HasBgpOverride: FeatureFlag
    }

type RouteTableConfig =
    {
        Name: ResourceName
        DisableBGPRoutePropagation: FeatureFlag 
        Routes: Network.Route list
        Tags: Map<string, string>
    }

    interface IBuilder with
        member this.ResourceId = routeTables.resourceId this.Name

        member this.BuildResources location =
            [
                {
                    RouteTable.Name = this.Name
                    Location = location
                    DisableBGPRoutePropagation = this.DisableBGPRoutePropagation.AsBoolean
                    Routes = this.Routes
                    Tags = this.Tags
                }
            ]

type RouteTableBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            Tags = Map.empty
        }

    [<CustomOperation "name">]
    member _.Name(state: RouteTableConfig, name: string) = { state with Name = ResourceName name }
    [<CustomOperation "disableBgpRoutePropagation">]
    member _.DisableBGPRoutePropagation(state: RouteTableConfig, flag: bool) = { state with DisableBGPRoutePropagation = FeatureFlag.ofBool flag }
    [<CustomOperation "add_route">]
    member _.AddRoute(state: RouteTableConfig, routeConfig: RouteConfig) =
        let route: Network.Route = {
            Name = routeConfig.Name
            AddressPrefix = routeConfig.AddressPrefix
            NextHopType = routeConfig.NextHopType
            NextHopIpAddress = routeConfig.NextHopIpAddress
            HasBgpOverride = routeConfig.HasBgpOverride.AsBoolean
        }
        { state with
            Routes =  [route] @ state.Routes
        }
    

let routeTable = RouteTableBuilder()

type RouteBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            Tags = Map.empty
        }

    [<CustomOperation "name">]
    member _.Name(state: RouteConfig, name: string) = { state with Name = ResourceName name }
    [<CustomOperation "addressPrefix">]
    member _.AddressPrefix(state: RouteConfig, ip: IPAddressCidr) = { state with AddressPrefix = ip }
    [<CustomOperation "nextHopType">]
    member _.NextHopType(state: RouteConfig, ht: HopType) = { state with NextHopType = ht }
    [<CustomOperation "nextHopIpAddress">]
    member _.NextHopIpAddress(state: RouteConfig, ip: IPAddressCidr) = { state with NextHopIpAddress = ip }
    [<CustomOperation "hasBgpOverride">]
    member _.HasBgpOverride(state: RouteConfig, flag: FeatureFlag) = { state with HasBgpOverride = flag }
[<AutoOpen>]
module Farmer.Builders.RouteTable

open Farmer
open Farmer.Arm
open Farmer.Route

type RouteConfig =
    {
        Name: ResourceName
        AddressPrefix: IPAddressCidr option
        NextHopType: Route.HopType
        HasBgpOverride: FeatureFlag option
    }

type RouteTableConfig =
    {
        Name: ResourceName
        DisableBGPRoutePropagation: FeatureFlag option
        Routes: RouteConfig list
        Tags: Map<string, string>
    }

    interface IBuilder with
        member this.ResourceId = routeTables.resourceId this.Name

        member this.BuildResources location =
            let routes: Network.Route list =
                this.Routes
                |> List.map (fun r ->
                    match r.AddressPrefix with
                    | None -> raiseFarmer ("address prefix is required")
                    | Some addressPrefix ->
                        {
                            Name = r.Name
                            AddressPrefix = addressPrefix
                            NextHopType = r.NextHopType
                            HasBgpOverride = r.HasBgpOverride |> Option.defaultValue FeatureFlag.Disabled
                        })

            let routeTable: Network.RouteTable =
                {
                    RouteTable.Name = this.Name
                    Location = location
                    DisableBGPRoutePropagation =
                        this.DisableBGPRoutePropagation |> Option.defaultValue FeatureFlag.Disabled
                    Routes = routes
                    Tags = this.Tags
                }

            [ routeTable ]

type RouteTableBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            Routes = []
            DisableBGPRoutePropagation = None
            Tags = Map.empty
        }

    [<CustomOperation "name">]
    member _.Name(state: RouteTableConfig, name: string) = { state with Name = ResourceName name }

    [<CustomOperation "disableBgpRoutePropagation">]
    member _.DisableBGPRoutePropagation(state: RouteTableConfig, flag: bool) =
        { state with
            DisableBGPRoutePropagation = Some(FeatureFlag.ofBool flag)
        }

    [<CustomOperation "add_routes">]
    member _.AddRoute(state: RouteTableConfig, routeConfigs: RouteConfig list) =
        { state with
            Routes = routeConfigs @ state.Routes
        }

let routeTable = RouteTableBuilder()

type RouteBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            AddressPrefix = None
            NextHopType = Route.HopType.Nothing
            HasBgpOverride = None
        }

    [<CustomOperation "name">]
    member _.Name(state: RouteConfig, name: string) = { state with Name = ResourceName name }

    [<CustomOperation "addressPrefix">]
    member _.AddressPrefix(state: RouteConfig, ip: IPAddressCidr) = { state with AddressPrefix = Some ip }

    member _.AddressPrefix(state: RouteConfig, ip: string) =
        { state with
            AddressPrefix = Some(IPAddressCidr.parse ip)
        }

    [<CustomOperation "nextHopType">]
    member _.NextHopType(state: RouteConfig, ht: Route.HopType) = { state with NextHopType = ht }

    [<CustomOperation "nextHopIpAddress">]
    member _.NextHopIpAddress(state: RouteConfig, ip: System.Net.IPAddress) =
        { state with
            NextHopType = VirtualAppliance(Some ip)
        }

    member _.NextHopIpAddress(state: RouteConfig, ip: string) =
        { state with
            NextHopType = VirtualAppliance(Some(System.Net.IPAddress.Parse ip))
        }

    [<CustomOperation "hasBgpOverride">]
    member _.HasBgpOverride(state: RouteConfig, flag: bool) =
        { state with
            HasBgpOverride = Some(FeatureFlag.ofBool flag)
        }

let route = RouteBuilder()

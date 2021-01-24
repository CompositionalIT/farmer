[<AutoOpen>]
module Farmer.Arm.ExpressRouteConnection

open Farmer

let expressRouteGateways = ResourceType ("Microsoft.Network/expressRouteGateways", "2020-07-01")
let expressRouteConnections = ResourceType ("Microsoft.Network/expressRouteGateways/expressRouteConnections", "2020-07-01")
let expressRouteCircuitPeerings = ResourceType ("Microsoft.Network/expressRouteCircuits/peerings", "2020-07-01")

type ErConnPropagatedRouteTable =
    {
        ErConnLabels : string list
        ErConnIds : string list
    }

type ErConnStaticRoute =
    {
        AddressPrefixes : IPAddressCidr list
        Name : string
        NextHopIpAddress : System.Net.IPAddress
    }

type ErConnRoutingConfiguration =
    {
        ErConnAssociatedRouteTable : string
        ErConnPropagatedRouteTable : ErConnPropagatedRouteTable option
        ErConnVnetRoutes : ErConnStaticRoute list
    }

type ExpressRouteConnection =
    {
        Name : ResourceName
        ExpressRouteGateway : ResourceName
        ExpressRoute : ResourceName
        AuthorizationKey : string option
        EnableInternetSecurity : bool
        ErConnRoutingConfiguration : ErConnRoutingConfiguration option
        RoutingWeight : int option
    }
    interface IArmResource with
        member this.ResourceId = expressRouteConnections.resourceId (this.ExpressRouteGateway/"expressRouteConnections"/this.Name)
        member this.JsonModel =
            {| expressRouteConnections.Create(this.ExpressRouteGateway/this.Name, dependsOn = [expressRouteGateways.resourceId this.ExpressRouteGateway]) with
                location = "[resourceGroup().location]"
                properties =
                {| authorizationKey = this.AuthorizationKey |> Option.defaultValue null
                   enableInternetSecurity = this.EnableInternetSecurity
                   expressRouteCircuitPeering = {| id = (expressRouteCircuitPeerings.resourceId (this.ExpressRoute, ResourceName("AzurePrivatePeering"))).ArmExpression.Eval() |}
                   routingConfiguration =
                    this.ErConnRoutingConfiguration
                    |> Option.map (fun routingConfig ->
                        {| associatedRouteTable = {| id = routingConfig.ErConnAssociatedRouteTable|}
                           propagatedRouteTables = routingConfig.ErConnPropagatedRouteTable
                               |> Option.map (fun propagated  ->
                                {| labels = propagated.ErConnLabels
                                   ids = propagated.ErConnIds |> List.map (fun id -> {| id=id |})
                                |})
                               |> Option.defaultValue Unchecked.defaultof<_>
                           vnetRoutes =
                            {| staticRoutes =
                                routingConfig.ErConnVnetRoutes
                                |> List.map (fun staticRoute ->
                                    {| addressPrefixes = staticRoute.AddressPrefixes |> List.map IPAddressCidr.format
                                       name = staticRoute.Name
                                       nextHopIpAddress = staticRoute.NextHopIpAddress |> string
                                    |}
                                )
                            |}
                        |})
                    |> Option.defaultValue Unchecked.defaultof<_>
                   routingWeight = this.RoutingWeight |> Option.defaultValue 0
                |}
            |} :> _

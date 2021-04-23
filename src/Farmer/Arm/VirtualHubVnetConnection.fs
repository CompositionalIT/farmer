[<AutoOpen>]
module Farmer.Arm.VirtualHubVnetConnection

open Farmer

//https://docs.microsoft.com/en-us/azure/templates/microsoft.network/virtualhubs/hubvirtualnetworkconnections
let virtualHubVnetConnections = ResourceType ("Microsoft.Network/virtualHubs/hubVirtualNetworkConnections", "2020-05-01")

type VnetConnPropagatedRouteTable =
    {
        VnetConnLabels : string list
        VnetConnIds : string list
    }

type VnetConnStaticRoute =
    {
        AddressPrefixes : IPAddressCidr list
        Name : string
        NextHopIpAddress : System.Net.IPAddress
    }

type VnetRoutingConfiguration =
    {
        VnetConnAssociatedRouteTable : string
        VnetConnPropagatedRouteTable : VnetConnPropagatedRouteTable option
        VnetConnVnetRoutes : VnetConnStaticRoute list
    }

type VirtualHubVnetConnection =
    {
      Name : ResourceName
      RemoteVirtualNetwork : ResourceName
      AllowHubToRemoteVnetTransit : bool option
      AllowRemoteVnetToUseHubVnetGateways : bool option
      EnableInternetSecurity : bool
      VnetRoutingConfiguration : VnetRoutingConfiguration option
      VHUB : ResourceName
      ERGW : ResourceName
    }
    interface IArmResource with
        member this.ResourceId = virtualHubVnetConnections.resourceId (this.VHUB/this.Name)
        member this.JsonModel =
            let dependencies = [
                virtualHubs.resourceId this.VHUB
                expressRouteGateways.resourceId this.ERGW
                ]
            {| virtualHubVnetConnections.Create(this.VHUB/this.Name, dependsOn = dependencies) with
                properties =
                    {|
                        allowHubToRemoteVnetTransit = this.AllowHubToRemoteVnetTransit |> Option.defaultValue true
                        allowRemoteVnetToUseHubVnetGateways = this.AllowRemoteVnetToUseHubVnetGateways |> Option.defaultValue true
                        enableInternetSecurity = this.EnableInternetSecurity
                        remoteVirtualNetwork = {| id = (virtualNetworks.resourceId this.RemoteVirtualNetwork).ArmExpression.Eval() |}
                        routingConfiguration =
                         this.VnetRoutingConfiguration
                         |> Option.map (fun routingConfig ->
                             {| associatedRouteTable = {| id = routingConfig.VnetConnAssociatedRouteTable |}
                                propagatedRouteTables = routingConfig.VnetConnPropagatedRouteTable
                                    |> Option.map (fun (propagated:VnetConnPropagatedRouteTable)  ->
                                    {| labels = propagated.VnetConnLabels
                                       ids = propagated.VnetConnIds |> List.map (fun id -> {| id=id |})
                                    |})
                                    |> Option.defaultValue Unchecked.defaultof<_>
                                vnetRoutes =
                                 {| staticRoutes =
                                     routingConfig.VnetConnVnetRoutes
                                     |> List.map (fun staticRoute ->
                                         {| addressPrefixes = staticRoute.AddressPrefixes |> List.map IPAddressCidr.format
                                            name = staticRoute.Name
                                            nextHopIpAddress = staticRoute.NextHopIpAddress |> string
                                         |}
                                     )
                                 |}
                            |})
                         |> Option.defaultValue Unchecked.defaultof<_>
                     |}
               |}:> _

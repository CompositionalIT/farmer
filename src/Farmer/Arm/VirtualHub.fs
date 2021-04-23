[<AutoOpen>]
module Farmer.Arm.VirtualHub

open Farmer

// Further examples and information can be found at https://docs.microsoft.com/en-us/azure/templates/microsoft.network/virtualhubs
let virtualHubs = ResourceType ("Microsoft.Network/virtualHubs", "2020-07-01")

// https://docs.microsoft.com/en-us/azure/templates/microsoft.network/virtualhubs/hubroutetables
let virtualHubRouteTables = ResourceType ("Microsoft.Network/virtualHubs/hubRouteTables", "2020-07-01")


[<RequireQualifiedAccess>]
type RoutingState =
    | Provisioned
    | Provisioning
    | Failed
    | None
    member this.ArmValue =
        match this with
        | Provisioned -> "Provisioned"
        | Provisioning -> "Provisioning"
        | Failed -> "Failed"
        | None -> "None"

[<RequireQualifiedAccess>]
type VHubSku =
    | Standard
    | Basic
    member this.ArmValue =
        match this with
        | Standard -> "Standard"
        | Basic -> "Basic"

type Route =
    {
      AddressPrefixes : IPAddressCidr list
      NextHopIpAddress : System.Net.IPAddress
    }

// Note - none of the VirtualHub Propertie Objects are required so are being set as options
type VirtualHub =
    { /// It's recommended to use resource group + -vhub.
      /// e.g. "name": "[concat(resourceGroup().name,'-vhub')]"
      Name : ResourceName
      /// The addressPrefix that is associated with VirtualHub - cidr block string
      AddressPrefix : IPAddressCidr option
      /// Property on VHub
      AllowBranchToBranchTraffic : bool option
      /// The azureFirewall that is associated with VirtualHub
      AzureFirewall : ResourceId option
      /// The expressRouteGateway that is associated with VirtualHub
      ExpressRouteGateway : ResourceId option
      /// The P2SVpnGateway that is associated with VirtualHub
      P2SVpnGateway : ResourceId option
      /// The routeTable that is associated with VirtualHub
      RouteTable : Route list
      /// The routingState that is associated with VirtualHub
      RoutingState : RoutingState option
      /// The securityProvider that is associated with VirtualHub
      SecurityProvider : string option
      /// The securityPartnerProvider that is associated with VirtualHub
      SecurityPartnerProvider : ResourceId option
      /// The virtualHubRouteTableV2s that is an array of all VirtualHub route table v2s associated
      VirtualHubRouteTableV2s : obj list
      /// The virtualHubSku that is associated with VirtualHub
      VirtualHubSku : VHubSku option
      /// The VirtualRouterAsn that is associated with VirtualHub
      VirtualRouterAsn : int option
      /// The virtualRouterIps that is associated with VirtualHub - an array of IPs (string)
      VirtualRouterIps : System.Net.IPAddress list
      /// The VPN Gateway associated with the Virtual Hub
      VpnGateway : ResourceId option
      /// To be used for the depends on for VHUB to connect to previous VWAN deployment
      VWAN : ResourceName
    }
    interface IArmResource with
        member this.ResourceId = virtualHubs.resourceId this.Name
        member this.JsonModel =
            let dependencies = [virtualWans.resourceId this.VWAN]
            {| virtualHubs.Create(this.Name, dependsOn = dependencies) with
                location = "[resourceGroup().location]"
                properties =
                    {|
                       addressPrefix = this.AddressPrefix |> Option.map IPAddressCidr.format |> Option.defaultValue null
                       azureFirewall = this.AzureFirewall |> Option.defaultValue Unchecked.defaultof<ResourceId>
                       routeTable = {| routes = this.RouteTable |}
                       sku = (this.VirtualHubSku |> Option.defaultValue VHubSku.Standard).ArmValue
                       virtualWan =  {| id = (virtualWans.resourceId this.VWAN).ArmExpression.Eval() |}
                    |}
            |}:> _


// VHUB DEFAULT ROUTE TABLE
type HubRouteTable =
    {
      Name : ResourceName
      DestinationType : string
      Destinations : string
      NextHop : ResourceId
      NextHopType : string
    }

// https://docs.microsoft.com/en-us/azure/templates/microsoft.network/virtualhubs/hubroutetables
type VirtualHubRouteTable =
    {
      Name : ResourceName
      RouteTable : HubRouteTable option
      Label : string list
      AZFW : ResourceName
      VHUB : ResourceName
    }
    interface IArmResource with
        member this.ResourceId = virtualHubRouteTables.resourceId this.Name
        member this.JsonModel =
            let dependencies = [
              virtualHubs.resourceId this.VHUB
            ]
            {| virtualHubRouteTables.Create(this.Name, dependsOn = dependencies) with
                location = "[resourceGroup().location]"
                properties =
                    {|
                      routes = this.RouteTable
                      labels = this.Label
                    |}
            |}:> _

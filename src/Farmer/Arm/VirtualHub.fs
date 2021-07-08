[<AutoOpen>]
module Farmer.Arm.VirtualHub

open Farmer
open Farmer.VirtualHub

// Further examples and information can be found at https://docs.microsoft.com/en-us/azure/templates/microsoft.network/virtualhubs
let virtualHubs = ResourceType ("Microsoft.Network/virtualHubs", "2020-07-01")

// https://docs.microsoft.com/en-us/azure/templates/microsoft.network/virtualhubs/hubroutetables
let hubRouteTables = ResourceType ("Microsoft.Network/virtualHubs/hubRouteTables", "2020-07-01")

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

type Route =
    {
      AddressPrefixes : IPAddressCidr list
      NextHopIpAddress : System.Net.IPAddress
    }

// none of the VirtualHub Properties Objects are required so are being set as options
type VirtualHub =
    { Name : ResourceName
      Location : Location
      Dependencies : ResourceId Set
      /// The addressPrefix that is associated with VirtualHub - cidr block string
      AddressPrefix : IPAddressCidr option
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
      VirtualHubSku : Sku
      /// The VirtualRouterAsn that is associated with VirtualHub
      VirtualRouterAsn : int option
      /// The virtualRouterIps that is associated with VirtualHub - an array of IPs (string)
      VirtualRouterIps : System.Net.IPAddress list
      /// The VPN Gateway associated with the Virtual Hub
      VpnGateway : ResourceId option
      /// To be used for the depends on for VHUB to connect to previous VWAN deployment
      Vwan : ResourceId option }
    interface IArmResource with
        member this.ResourceId = virtualHubs.resourceId this.Name
        member this.JsonModel =
            {| virtualHubs.Create(this.Name, this.Location, this.Dependencies) with
                properties =
                    {|
                       addressPrefix = this.AddressPrefix |> Option.map IPAddressCidr.format |> Option.defaultValue null
                       azureFirewall = this.AzureFirewall |> Option.defaultValue Unchecked.defaultof<ResourceId>
                       routeTable = {| routes = this.RouteTable |}
                       sku = this.VirtualHubSku.ArmValue
                       virtualWan = this.Vwan |> Option.map (fun resId -> box {| id = resId.ArmExpression.Eval() |}) |> Option.defaultValue null
                    |}
            |}:> _

open Farmer.VirtualHub.HubRouteTable
type HubRoute =
    { Name : string
      Destination : Destination
      NextHop : NextHop }
    member this.JsonModel =
        {| name = this.Name
           destinationType = this.Destination.DestinationTypeArmValue
           destinations = this.Destination.DestinationsArmValue
           nextHopType = this.NextHop.NextHopTypeArmValue
           nextHop = this.NextHop.NextHopArmValue
        |}

// https://docs.microsoft.com/en-us/azure/templates/microsoft.network/virtualhubs/hubroutetables
type HubRouteTable =
    { Name : ResourceName
      VirtualHub : ResourceId
      Dependencies : ResourceId Set
      Routes : HubRoute list
      Labels : string list }
    interface IArmResource with
        member this.ResourceId = hubRouteTables.resourceId (this.VirtualHub.Name/this.Name)
        member this.JsonModel =
            {| hubRouteTables.Create(this.VirtualHub.Name/this.Name, dependsOn=this.Dependencies) with
                properties =
                    {|
                      routes = this.Routes |> List.map (fun r -> r.JsonModel)
                      labels = this.Labels
                    |}
            |} :> _
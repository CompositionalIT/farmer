[<AutoOpen>]
module Farmer.Arm.VirtualHubRouteTable

open Farmer

// https://docs.microsoft.com/en-us/azure/templates/microsoft.network/virtualhubs/hubroutetables
let virtualHubRouteTables = ResourceType ("Microsoft.Network/virtualHubs/hubRouteTables", "2020-05-01")

type HubRoute =
    {
        Name : string
        Destinations : IPAddressCidr list
        NextHop : ResourceName
    }

type VirtualHubRouteTable =
    {
      Name : ResourceName
      Routes : HubRoute list
      Labels : string list
      AZFW : ResourceName
    }
    interface IArmResource with
        member this.ResourceId = virtualHubRouteTables.resourceId this.Name
        member this.JsonModel =
            let dependencies = [azureFirewalls.resourceId this.AZFW]
            {| virtualHubRouteTables.Create(this.Name, dependsOn = dependencies) with
                properties =
                    {|
                       labels = this.Labels
                       routes = this.Routes |> List.map (fun route ->
                         {| name = route.Name
                            destinationType = "CIDR"
                            destinations = route.Destinations |> List.map IPAddressCidr.format
                            nextHopType = "ResourceId"
                            nextHop = (azureFirewalls.resourceId this.AZFW).ArmExpression.Eval()
                         |})
                    |}
            |}:> _

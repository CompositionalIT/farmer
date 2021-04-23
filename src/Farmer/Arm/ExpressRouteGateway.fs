[<AutoOpen>]
module Farmer.Arm.ExpressRouteGateway

open Farmer

// https://docs.microsoft.com/en-us/azure/templates/microsoft.network/expressroutegateways
let virtualHubExpressRouteGateway = ResourceType ("Microsoft.Network/expressRouteGateways", "2020-07-01")

// VHUB EXPRESSROUTE GATEWAY

type AutoScaleConfigurationBounds =
    {
      min : int
      max : int option
    }

type AutoScaleConfiguration =
    {
      Bounds : AutoScaleConfigurationBounds
    }

type VirtualHubExpressRouteGateway =
    {
      Name : ResourceName
      AutoScaleConfiguration : AutoScaleConfiguration
      VHUB : ResourceName
      AZFW: ResourceName
    }
    interface IArmResource with
        member this.ResourceId = virtualHubExpressRouteGateway.resourceId this.Name
        member this.JsonModel =
            let dependencies = [
                virtualHubs.resourceId this.VHUB
                azureFirewalls.resourceId this.AZFW
              ]
            {| virtualHubExpressRouteGateway.Create (this.Name, dependsOn = dependencies) with
                location = "[resourceGroup().location]"
                properties =
                    {|
                      virtualHub =
                        {|
                          id = (virtualHubs.resourceId this.VHUB).ArmExpression.Eval()
                        |}
                      autoScaleConfiguration =
                        {|
                          bounds = this.AutoScaleConfiguration.Bounds
                        |}
                    |}
            |}:> _

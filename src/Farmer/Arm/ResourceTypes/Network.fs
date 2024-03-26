module Farmer.Arm.ResourceTypes.Network

open Farmer

let connections = ResourceType("Microsoft.Network/connections", "2020-04-01")

let expressRouteCircuits =
    ResourceType("Microsoft.Network/expressRouteCircuits", "2019-02-01")

let expressRouteCircuitAuthorizations =
    ResourceType("Microsoft.Network/expressRouteCircuits/authorizations", "2019-02-01")

let networkInterfaces =
    ResourceType("Microsoft.Network/networkInterfaces", "2018-11-01")

let networkInterfacesIpConfigurations =
    ResourceType("Microsoft.Network/networkInterfaces/ipConfigurations", "2023-04-01")

let networkProfiles =
    ResourceType("Microsoft.Network/networkProfiles", "2020-04-01")

let publicIPAddresses =
    ResourceType("Microsoft.Network/publicIPAddresses", "2018-11-01")

let publicIPPrefixes =
    ResourceType("Microsoft.Network/publicIPPrefixes", "2021-08-01")

let serviceEndpointPolicies =
    ResourceType("Microsoft.Network/serviceEndpointPolicies", "2020-07-01")

let subnets =
    ResourceType("Microsoft.Network/virtualNetworks/subnets", "2020-07-01")

let virtualNetworks =
    ResourceType("Microsoft.Network/virtualNetworks", "2020-07-01")

let virtualNetworkGateways =
    ResourceType("Microsoft.Network/virtualNetworkGateways", "2020-05-01")

let localNetworkGateways =
    ResourceType("Microsoft.Network/localNetworkGateways", "")

let natGateways = ResourceType("Microsoft.Network/natGateways", "2021-08-01")

let privateEndpoints =
    ResourceType("Microsoft.Network/privateEndpoints", "2021-05-01")

let virtualNetworkPeering =
    ResourceType("Microsoft.Network/virtualNetworks/virtualNetworkPeerings", "2020-05-01")

let routeTables = ResourceType("Microsoft.Network/routeTables", "2021-01-01")
let routes = ResourceType("Microsoft.Network/routeTables/routes", "2021-01-01")

let routeServers = ResourceType("Microsoft.Network/virtualHubs", "2022-11-01")

let routeServerIPConfigs =
    ResourceType("Microsoft.Network/virtualHubs/ipConfigurations", "2022-11-01")

let routeServerBGPConnections =
    ResourceType("Microsoft.Network/virtualHubs/bgpConnections", "2022-11-01")

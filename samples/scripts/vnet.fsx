#r "nuget:Farmer"

open Farmer
open Farmer.Builders
open Farmer.Network

let serviceEndpointPolicy =
    Farmer.Arm.Network.serviceEndpointPolicies.resourceId "svc-endpt-policy"


let privateNet = vnet {
    name "my-vnet"

    build_address_spaces [
        addressSpace {
            space "10.28.0.0/16"
            build_subnet "services" 24
            build_subnet "corporate-west" 18
            build_subnet "corporate-east" 18
            build_subnet "GatewaySubnet" 29
            build_subnet_delegated "containers" 27 [ SubnetDelegationService.ContainerGroups ]

            build_subnet_service_endpoints "can-use-servicebus" 28 [
                EndpointServiceType.ServiceBus, [ Location.EastUS ]
            ]

            build_subnet_service_endpoint_policies "can-use-storage" 28 [
                EndpointServiceType.Storage, [ Location.EastUS ]
            ] [ serviceEndpointPolicy ]

            subnets [
                buildSubnet "more-services" 24
                buildSubnetDelegations "more-containers" 27 [ SubnetDelegationService.ContainerGroups ]
            ]
        }
        addressSpace {
            space "10.30.0.0/16"
            subnets [ buildSubnet "stuff" 23; buildSubnet "more-stuff" 28 ]
        }
    ]
}

let deployment = arm {
    location Location.EastUS
    add_resource privateNet
}

deployment |> Writer.quickWrite "vnet-sample"
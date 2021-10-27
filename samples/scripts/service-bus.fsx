#r "../../src/Farmer/bin/Debug/net5.0/Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.ServiceBus

let initialServiceBus = serviceBus {
    name "allMyQueues"
    sku Standard
    add_topics [
        topic {
            name "existing-topic"
        }
    ]
}

let followUpDeployment = resourceGroup {
    name "service-bus-test"
    add_resource (topic{
        name "new-topic"
        link_to_unmanaged_namespace "allMyQueues"
    })
    add_resource (subscription{
        name "af.new-topic"
        link_to_unmanaged_topic "allMyQueues/existing-topic"
        forward_to "new-topic"
    })
    depends_on initialServiceBus
}
let deployment = arm {
    location Location.NorthEurope
    add_resource initialServiceBus
    add_resource followUpDeployment
}

deployment
|> Deploy.execute "service-bus-test" []
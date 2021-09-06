#r "../../src/Farmer/bin/Debug/net5.0/Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.ServiceBus

let myServiceBus = serviceBus {
    name "rsptest-allMyQueues"
    sku Standard
    add_queues [
        queue { name "queuenumberone" }
        queue { name "queuenumbertwo" }
    ]
    add_topics [
        topic {
            name "thetopic"
            add_subscriptions [
                subscription {
                    name "thesub"
                    forward_to "queuenumberone"
                }
            ]
        }
    ]
}

let deployment = arm {
    location Location.NorthEurope
    add_resource myServiceBus
    output "NamespaceDefaultConnectionString" myServiceBus.NamespaceDefaultConnectionString
    output "DefaultSharedAccessPolicyPrimaryKey" myServiceBus.DefaultSharedAccessPolicyPrimaryKey
}

deployment
|> Deploy.execute "rsptest-sb" []
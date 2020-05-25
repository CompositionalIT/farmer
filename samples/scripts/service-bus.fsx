#r @"..\..\src\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.ServiceBus

let myServiceBus = serviceBus {
    name "allMyQueues"
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
|> Deploy.execute "service-bus-test" []
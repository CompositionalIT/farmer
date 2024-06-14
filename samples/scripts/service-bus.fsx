#r "nuget:Farmer"

open Farmer
open Farmer.Builders
open Farmer.ServiceBus

let myServiceBus = serviceBus {
    name "allMyQueues"
    sku Standard
    min_tls_version TlsVersion.Tls12
    enable_zone_redundancy
    disable_public_network_access
    add_queues [ queue { name "queuenumberone" }; queue { name "queuenumbertwo" } ]

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

deployment |> Deploy.execute "service-bus-test" []
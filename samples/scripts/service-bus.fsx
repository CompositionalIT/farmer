#r "../../src/Farmer/bin/Debug/net5.0/Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.ServiceBus

// let myServiceBus =
//   serviceBus {
//     name "rsptest-allMyQueues"
//     sku Standard
//     add_queues [ queue { name "queuenumberone" } ]

//     add_topics [ topic {
//                    name "thetopic"
//                    add_subscriptions []
//                  } ]
//   }

let q = queue {
  name "queuenumbertwo"
  link_to_unmanaged_namespace "rsptest-allMyQueues"
}

let sub = subscription {
  name "thesub"
  forward_to "queuenumberone"
  link_to_unmanaged_topic "rsptest-allMyQueues/thetopic"
  depends_on q
}

let deployment =
  arm {
    location Location.NorthEurope
    add_resource sub
    add_resource q
  }

deployment |> Deploy.execute "rsptest-sb" []

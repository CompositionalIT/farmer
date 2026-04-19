#r "nuget:Farmer"

open Farmer
open Farmer.Builders
open Farmer.EventHub

let myEh = eventHub {
    name "first-hub"
    namespace_name "allmyevents"

    sku Standard
    enable_zone_redundant
    enable_auto_inflate 3
    add_authorization_rule "FirstRule" [ Listen; Send ]
    add_authorization_rule "SecondRule" AllAuthorizationRights

    partitions 2
    message_retention_days 3
    add_consumer_group "myGroup"
}

let secondHub = eventHub {
    name "second-hub"
    link_to_namespace "allmyevents"
    partitions 1
    message_retention_days 1
}

let deployment = arm {
    location Location.NorthEurope
    add_resource myEh
    add_resource secondHub
}

// Generate the ARM template here...
deployment |> Writer.quickWrite "farmer-deploy"
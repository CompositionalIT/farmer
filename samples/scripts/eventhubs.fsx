#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.EventHub

let deployment = arm {
    location Location.NorthEurope
        
    eventHub {
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

    eventHub {
        name "second-hub"
        link_to_namespace "allmyevents"
        partitions 1
        message_retention_days 1
    }
}

// Generate the ARM template here...
deployment
|> Writer.quickWrite "farmer-deploy"

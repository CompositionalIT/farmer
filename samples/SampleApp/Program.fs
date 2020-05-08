open Farmer
open Farmer.Builders

let eventGrid (topicName, subscriptionName, subscriptionUrl:string) (location:Location) = [
        topicName,
            box {| name = topicName
                   ``type`` = "Microsoft.EventGrid/topics"
                   location = location.ArmValue
                   apiVersion = "2018-01-01" |}

        topicName + "/Microsoft.EventGrid/" + subscriptionName,
            box {| name = topicName + "/Microsoft.EventGrid/" + subscriptionName
                   ``type`` = "Microsoft.EventGrid/topics/providers/eventSubscriptions"
                   location = location.ArmValue
                   apiVersion = "2018-01-01"
                   properties =
                       {| destination =
                            {| endpointType = "WebHook"
                               properties = {| endpointUrl = subscriptionUrl |}
                            |}
                          filter = {| includedEventTypes = [ "All" ] |}
                       |}
                   dependsOn = [ topicName ]
                |}
    ]

let createEventGrid = eventGrid >> Helpers.asResourceBuilder

let myEventGrid = createEventGrid ("THE-TOPIC", "THE-SUB", "https://requestb.in/1jz6i2h1")

let storage = storageAccount {
    name "isaacstorage"
}

let deployment = arm {
    location NorthEurope
    add_resource storage
    add_resource myEventGrid
}

deployment
|> Writer.quickWrite @"generated-template"
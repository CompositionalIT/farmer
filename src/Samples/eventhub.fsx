#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"
#r @"C:\Users\isaac\.nuget\packages\newtonsoft.json\12.0.3\lib\netstandard2.0\newtonsoft.json.dll"

open Farmer
open Farmer.Resources

let myHub = eventHub {
    name "isaacsHub"
    sku EventHubSku.Standard
    capacity 1
    enable_kafka
    max_throughput 0
    message_retention_days 1
    partitions 2
}

let deployment = arm {
    location NorthEurope
    add_resource myHub
}

Writer.quickWrite "my-resource-group" deployment


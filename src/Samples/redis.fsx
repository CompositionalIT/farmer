#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"
#r @"C:\Users\isaac\.nuget\packages\newtonsoft.json\12.0.3\lib\netstandard2.0\Newtonsoft.Json.dll"

open Farmer
open Farmer.Resources.Redis
open Farmer.Models

let myCache = redis {
    name "isaacsredis"
    sku Basic
    capacity 0
    enable_non_ssl_port
    setting "maxclients" 256
    setting "maxmemory-reserved" 2
    setting "maxfragmentationmemory-reserved" 12
    setting "maxmemory-delta" 2
}

let template = arm {
    location NorthEurope
    add_resource myCache
}

template
|> Writer.quickWrite "my-resource-group-name"
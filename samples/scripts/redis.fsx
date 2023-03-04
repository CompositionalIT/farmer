#r "nuget:Farmer"

open Farmer
open Farmer.Builders

let myCache = redis {
    name "myredis"
    sku Redis.Standard
    capacity 0
    enable_non_ssl_port
    setting "maxclients" 256
    setting "maxmemory-reserved" 2
    setting "maxfragmentationmemory-reserved" 12
    setting "maxmemory-delta" 2
}

let template = arm {
    location Location.NorthEurope
    add_resource myCache
}

template |> Writer.quickWrite "my-resource-group-name"

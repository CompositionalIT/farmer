#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer
open Farmer.Resources.Redis

let myCache = redis {
    name "myredis"
    sku RedisSku.Standard
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
#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders

let isaacWebApp = webApp {
    name "isaacsuperweb"
    app_insights_off
}
let isaacStorage = storageAccount {
    name "isaacsuperstore"
}
let isaacCdn = cdn {
    name "isaacsupercdn"
    add_endpoints [
        endpoint {
            origin isaacStorage
            optimise_for Cdn.OptimizationType.LargeFileDownload
        }
        endpoint {
            origin isaacWebApp
            disable_http
        }
        endpoint {
            name "custom-endpoint-name"
            origin "mysite.com"
            add_compressed_content [ "text/plain"; "text/html"; "text/css" ]
            query_string_caching_behaviour Cdn.BypassCaching
        }
    ]
}

let deployment = arm {
    add_resources [
        isaacStorage
        isaacCdn
        isaacWebApp
    ]
}

deployment |> Writer.quickWrite "generated-template"
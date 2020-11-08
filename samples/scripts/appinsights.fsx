#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders

let myAppInsights = appInsights {
    name "isaacsAi"
}

let myFunctions = functions {
    name "mysuperwebapp"
    link_to_app_insights myAppInsights.Name
}

let template = arm {
    location Location.NorthEurope
    add_resource myAppInsights
    add_resource myFunctions
}

template
|> Deploy.execute "deleteme" Deploy.NoParameters

open Newtonsoft.Json

let start = {| properties = {| outputs = {| Name = {| value = "Isaac" |}; Age = {| value = 40 |} |} |} |}
let p1 = start |> JsonConvert.SerializeObject
let p2 = p1 |> JsonConvert.DeserializeObject<{| properties : {| outputs : Map<string, {| value : string |}> |} |}>
let p3 = p2.properties.outputs |> Map.map (fun _ value -> value.value)


let p4 = p3 |> JsonConvert.SerializeObject
let p5 = p4 |> JsonConvert.DeserializeObject<{| Age : int; Name : string |}>
#r "nuget:Farmer"

open Farmer
open Farmer.Builders

let myLake = dataLake {
    name "isaacsLake"
    enable_encryption
    sku DataLake.Commitment_10TB
}

let deployment = arm {
    location Location.NorthEurope
    add_resource myLake
}

deployment
|> Deploy.execute "my-resource-group-name" Deploy.NoParameters
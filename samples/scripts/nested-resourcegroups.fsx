#r "../../src/Farmer/bin/Debug/net5.0/Farmer.dll"

open Farmer
open Farmer.Builders

let myStorage = storageAccount {
    name "myfarmerstorage"
}
let nested = resourceGroup {
    name "farmer-test-inner"
    add_resource myStorage
}
let template = arm {
    location Location.UKSouth
    add_resource nested
}

template |> Writer.quickWrite "template"
template |> Deploy.execute "farmer-test-rg" []
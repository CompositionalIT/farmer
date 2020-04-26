#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"
#r @"../../../../../.nuget/packages/Newtonsoft.Json/12.0.2/lib/netstandard2.0/Newtonsoft.Json.dll"

open Farmer
open Farmer.Resources

let myQueue = queue {
    name "isaacssuperqueue"
    sku ServiceBusNamespaceSku.Standard
}

let d = arm {
    location NorthEurope
    add_resource myQueue
    output "NamespaceDefaultConnectionString" myQueue.NamespaceDefaultConnectionString
    output "DefaultSharedAccessPolicyPrimaryKey" myQueue.DefaultSharedAccessPolicyPrimaryKey
}

let r = d |> Deploy.execute "service-bus-test" []

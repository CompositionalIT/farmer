#r @"..\..\src\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer
open Farmer.Resources

let myQueue = serviceBus {
    namespace_name "allMyQueues"
    sku ServiceBusNamespaceSku.Standard

    name "isaacssuperqueue"
}

let mySecondQueue = serviceBus {
    name "isaacssecondsuperqueue"
    link_to_namespace myQueue
}


let deployment = arm {
    location NorthEurope
    add_resource myQueue
    add_resource mySecondQueue
    output "1-NamespaceDefaultConnectionString" myQueue.NamespaceDefaultConnectionString
    output "1-DefaultSharedAccessPolicyPrimaryKey" myQueue.DefaultSharedAccessPolicyPrimaryKey
    output "2-NamespaceDefaultConnectionString" mySecondQueue.NamespaceDefaultConnectionString
    output "2-DefaultSharedAccessPolicyPrimaryKey" mySecondQueue.DefaultSharedAccessPolicyPrimaryKey
}

deployment
|> Deploy.execute "service-bus-test" []
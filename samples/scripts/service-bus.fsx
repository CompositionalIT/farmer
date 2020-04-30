#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"
#r @"../../../../../.nuget/packages/Newtonsoft.Json/12.0.2/lib/netstandard2.0/Newtonsoft.Json.dll"

open Farmer
open Farmer.Resources

let myQueue = serviceBus {
    namespace_name "allMyQueues"
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
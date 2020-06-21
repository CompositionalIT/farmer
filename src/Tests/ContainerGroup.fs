module ContainerGroup

open Expecto
open Farmer
open Farmer.ContainerGroup
open Farmer.Builders
open Microsoft.Azure.Management.ContainerInstance
open Microsoft.Azure.Management.ContainerInstance.Models
open Microsoft.Rest
open System

let nginx = containerInstance {
    name "nginx"
    image "nginx:1.17.6-alpine"
    ports [ 80us; 443us ]
    memory 0.5<Gb>
    cpu_cores 1
}
let fsharpApp = containerInstance {
    name "fsharpApp"
    image "myapp:1.7.2"
    ports [ 8080us ]
    memory 1.5<Gb>
    cpu_cores 2
}

/// Client instance needed to get the serializer settings.
let dummyClient = new ContainerInstanceManagementClient (Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let tests = testList "Container Group" [
    test "Single container in a group is correctly created" {
        let group = containerGroup {
            name "appWithHttpFrontend"
            operating_system Linux
            add_tcp_port 80us
            add_tcp_port 443us
            restart_policy AlwaysRestart
            add_instances [ nginx ]
        }
        let group =
            arm { add_resource group }
            |> findAzureResources<ContainerGroup> dummyClient.SerializationSettings
            |> List.head

        group.Validate()

        Expect.equal group.Name "appWithHttpFrontend" "Group name is not set correctly."
        Expect.equal group.IpAddress.Ports.[0].PortProperty 443 "Port #1 should be 443"
        Expect.equal group.IpAddress.Ports.[1].PortProperty 80 "Port #2 should be 80"
        Expect.equal group.OsType "Linux" "OS should be Linux"

        Expect.equal group.Containers.[0].Image "nginx:1.17.6-alpine" "Incorrect image"
        Expect.equal group.Containers.[0].Name "nginx" "Incorrect instance name"
        Expect.equal group.Containers.[0].Resources.Requests.MemoryInGB 0.5 "Incorrect memory"
        Expect.equal group.Containers.[0].Resources.Requests.Cpu 1.0 "Incorrect CPU"
    }

    test "Multiple containers are correctly added" {
        let group = containerGroup {
            name "test"
            add_instances [ nginx; fsharpApp ]
        }

        let group =
            arm { add_resource group }
            |> findAzureResources<ContainerGroup> dummyClient.SerializationSettings
            |> List.head
        group.Validate()
        Expect.equal group.Containers.Count 2 "Should be two containers"
        Expect.equal group.Containers.[0].Name "nginx" "Nginx should be the first container"
        Expect.equal group.Containers.[1].Name "fsharpapp" "FSharpApp should the second container"
        Expect.equal group.Containers.[1].Resources.Requests.MemoryInGB 1.5 "Should have 1.5gb on FSharp App"
        Expect.equal group.Containers.[1].Resources.Requests.Cpu 2.0 "Should have 2 CPUs on FSharp App"
        Expect.equal group.Containers.[1].Ports.[0].Port 8080 "FSharp App port #1 should be 8080"
    }
]


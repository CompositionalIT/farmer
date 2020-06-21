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
            restart_policy AlwaysRestart
            add_udp_port 123us
            add_instances [ nginx ]
        }
        let group =
            arm { add_resource group }
            |> findAzureResources<ContainerGroup> dummyClient.SerializationSettings
            |> List.head

        group.Validate()

        Expect.equal group.Name "appWithHttpFrontend" "Group name is not set correctly."
        Expect.equal group.OsType "Linux" "OS should be Linux"
        Expect.equal group.IpAddress.Ports.[1].PortProperty 123 "Incorrect udp port"

        let containerInstance = group.Containers.[0]
        Expect.equal containerInstance.Image "nginx:1.17.6-alpine" "Incorrect image"
        Expect.equal containerInstance.Name "nginx" "Incorrect instance name"
        Expect.equal containerInstance.Resources.Requests.MemoryInGB 0.5 "Incorrect memory"
        Expect.equal containerInstance.Resources.Requests.Cpu 1.0 "Incorrect CPU"
        Expect.equal containerInstance.Ports.[0].Port 80 "Incorrect port"
        Expect.equal containerInstance.Ports.[1].Port 443 "Incorrect port"
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

        Expect.hasLength group.Containers 2 "Should be two containers"
        Expect.equal group.Containers.[0].Name "nginx" "Incorrect container name"
        Expect.equal group.Containers.[1].Name "fsharpapp" "Incorrect container name "
        Expect.equal group.Containers.[1].Resources.Requests.MemoryInGB 1.5 "Incorrect memory"
        Expect.equal group.Containers.[1].Resources.Requests.Cpu 2.0 "Incorrect CPU count"
        Expect.equal group.Containers.[1].Ports.[0].Port 8080 "Incorrect FSharp App port"
    }

    test "Implicitly creates ports for group based on instances" {
        let group = containerGroup {
            name "test"
            add_instances [ nginx ]
        }
        let group =
            arm { add_resource group }
            |> findAzureResources<ContainerGroup> dummyClient.SerializationSettings
            |> List.head

        let ports = group.IpAddress.Ports |> Seq.map(fun p -> p.PortProperty) |> Set
        Expect.equal ports (Set [ 443; 80 ]) "Incorrect implicitly created ports"
    }
]


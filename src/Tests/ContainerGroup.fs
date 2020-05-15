module ContainerGroup

open Expecto
open Farmer
open Farmer.ContainerGroup
open Farmer.Builders
open Microsoft.Azure.Management.ContainerInstance
open Microsoft.Azure.Management.ContainerInstance.Models
open Microsoft.Rest
open System

let nginx = container {
    group_name "appWithHttpFrontend"
    os_type Linux
    add_tcp_port 80us
    add_tcp_port 443us
    restart_policy Always

    name "nginx"
    image "nginx:1.17.6-alpine"
    ports [ 80us; 443us ]
    memory 0.5<Gb>
    cpu_cores 1
}

let fsharpApp = container {
    link_to_container_group nginx
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
        let group =
            arm {
                add_resource nginx
            }
            |> findAzureResources<ContainerGroup> dummyClient.SerializationSettings
            |> List.head

        group.Validate()

        Expect.equal group.Name "appWithHttpFrontend" ""
        Expect.equal group.IpAddress.Ports.[0].PortProperty 443 ""
        Expect.equal group.IpAddress.Ports.[1].PortProperty 80 ""
        Expect.equal group.OsType "Linux" ""

        Expect.equal group.Containers.[0].Image "nginx:1.17.6-alpine" ""
        Expect.equal group.Containers.[0].Name "nginx" ""
        Expect.equal group.Containers.[0].Resources.Requests.MemoryInGB 0.5 ""
        Expect.equal group.Containers.[0].Resources.Requests.Cpu 1.0 ""
    }

    test "Multiple containers correctly link to a common container group" {
        let group =
            arm {
                add_resource nginx
                add_resource fsharpApp
            }
            |> findAzureResources<ContainerGroup> dummyClient.SerializationSettings
            |> List.head
        group.Validate()
        Expect.equal group.Containers.Count 2 ""
        Expect.equal group.Containers.[0].Name "nginx" ""
        Expect.equal group.Containers.[1].Name "fsharpapp" ""
        Expect.equal group.Containers.[1].Resources.Requests.MemoryInGB 1.5 ""
        Expect.equal group.Containers.[1].Resources.Requests.Cpu 2.0 ""
        Expect.equal group.Containers.[1].Ports.[0].Port 8080 ""
    }
]


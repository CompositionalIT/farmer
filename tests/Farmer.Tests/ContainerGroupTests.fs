module Tests

open System
open Xunit
open Farmer
open Farmer.Resources
open Microsoft.Rest
open Newtonsoft.Json.Linq

let myContainerGroup = containerGroup {
    name "appWithHttpFrontend"
    os_type Models.ContainerGroups.ContainerGroupOsType.Linux
    add_tcp_port 80us
    add_tcp_port 443us
    restart_policy Models.ContainerGroups.ContainerGroupRestartPolicy.Always
    add_containers [
        containerInstance {
            name "nginx"
            image "nginx:1.17.6-alpine"
            ports [ 80us; 443us ]
            memory 0.5<Models.ContainerGroups.Gb>
            cpu 1
        }
        containerInstance {
            name "fsharpApp"
            image "myapp:1.7.2"
            ports [ 8080us ]
            memory 1.5<Models.ContainerGroups.Gb>
            cpu 2
        }
    ]
}

let deployment = arm {
    location NorthEurope

    add_resource myContainerGroup
}

/// Client instance needed to get the serializer settings.
let dummyClient = new Microsoft.Azure.Management.ContainerInstance.ContainerInstanceManagementClient(Uri("http://management.azure.com"), TokenCredentials("NotNullOrWhiteSpace") )

[<Fact>]
let ``Validate Container Group Template Generation`` () =
    let containerGroupTemplate =
        deployment.Template
        |> Writer.toJson
        
    let firstResource = JObject.Parse(containerGroupTemplate).["resources"].[0]
    
    let containerGroupDeserialized = Microsoft.Rest.Serialization.SafeJsonConvert.DeserializeObject<Microsoft.Azure.Management.ContainerInstance.Models.ContainerGroup>(firstResource.ToString(), dummyClient.SerializationSettings )
    Assert.Equal("appWithHttpFrontend", containerGroupDeserialized.Name)
    Assert.NotNull(containerGroupDeserialized.IpAddress)
    Assert.NotNull(containerGroupDeserialized.IpAddress.Ports)
    Assert.Equal(2, containerGroupDeserialized.IpAddress.Ports.Count)
    Assert.True(containerGroupDeserialized.IpAddress.Ports |> Seq.exists (fun p -> p.PortProperty = 80))
    Assert.True(containerGroupDeserialized.IpAddress.Ports |> Seq.exists (fun p -> p.PortProperty = 443))
    Assert.Equal("Linux", containerGroupDeserialized.OsType)
    Assert.Equal(2, containerGroupDeserialized.Containers.Count)
    Assert.Equal("nginx:1.17.6-alpine", containerGroupDeserialized.Containers.[0].Image)
    Assert.Equal("fsharpApp".ToLower(), containerGroupDeserialized.Containers.[1].Name)
    Assert.Equal(1.5, containerGroupDeserialized.Containers.[1].Resources.Requests.MemoryInGB)
    Assert.Equal(2., containerGroupDeserialized.Containers.[1].Resources.Requests.Cpu)

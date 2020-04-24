module ContainerGroupTests

open Farmer
open Farmer.Resources
open Microsoft.Azure.Management.ContainerInstance
open Microsoft.Azure.Management.ContainerInstance.Models
open Microsoft.Rest
open Microsoft.Rest.Serialization
open Newtonsoft.Json.Linq
open System
open Xunit

let nginx = container {
    group_name "appWithHttpFrontend"
    os_type Models.ContainerGroups.ContainerGroupOsType.Linux
    add_tcp_port 80us
    add_tcp_port 443us
    restart_policy Models.ContainerGroups.ContainerGroupRestartPolicy.Always

    name "nginx"
    image "nginx:1.17.6-alpine"
    ports [ 80us; 443us ]
    memory 0.5<Models.ContainerGroups.Gb>
    cpu_cores 1
}

let fsharpApp = container {
    link_to_container_group nginx
    name "fsharpApp"
    image "myapp:1.7.2"
    ports [ 8080us ]
    memory 1.5<Models.ContainerGroups.Gb>
    cpu_cores 2
}

/// Client instance needed to get the serializer settings.
let dummyClient = new ContainerInstanceManagementClient (Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let getContainerGroup deployment =
    let resource =
        deployment.Template
        |> Writer.TemplateGeneration.processTemplate
        |> fun containerGroup -> containerGroup.resources.[0]
        |> SafeJsonConvert.SerializeObject

    SafeJsonConvert.DeserializeObject<ContainerGroup> (resource, dummyClient.SerializationSettings)

[<Fact>]
let ``Single container in a group is correctly created`` () =
    let deployment = arm {
        location NorthEurope
        add_resource nginx
    }

    let group = getContainerGroup deployment
    Assert.Equal ("appWithHttpFrontend", group.Name)
    Assert.True (group.IpAddress.Ports.[0].PortProperty = 443)
    Assert.True (group.IpAddress.Ports.[1].PortProperty = 80)
    Assert.Equal ("Linux", group.OsType)

    Assert.Equal ("nginx:1.17.6-alpine", group.Containers.[0].Image)
    Assert.Equal ("nginx", group.Containers.[0].Name)
    Assert.Equal (0.5, group.Containers.[0].Resources.Requests.MemoryInGB)
    Assert.Equal (1., group.Containers.[0].Resources.Requests.Cpu)

[<Fact>]
let ``Multiple containers correctly link to a common container group`` () =
    let deployment = arm {
        location NorthEurope
        add_resource nginx
        add_resource fsharpApp
    }

    let group = getContainerGroup deployment

    Assert.Equal (2, group.Containers.Count)
    Assert.Equal ("nginx", group.Containers.[0].Name)
    Assert.Equal ("fsharpapp", group.Containers.[1].Name)
    Assert.Equal (1.5, group.Containers.[1].Resources.Requests.MemoryInGB)
    Assert.Equal (2., group.Containers.[1].Resources.Requests.Cpu)
    Assert.Equal (8080, group.Containers.[1].Ports.[0].Port)
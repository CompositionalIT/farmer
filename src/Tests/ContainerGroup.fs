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
    add_ports PublicPort [ 80us; 443us ]
    add_ports InternalPort [ 9090us; ]
    memory 0.5<Gb>
    cpu_cores 1
}
let fsharpApp = containerInstance {
    name "fsharpApp"
    image "myapp:1.7.2"
    add_ports PublicPort [ 8080us ]
    memory 1.5<Gb>
    cpu_cores 2
}

/// Client instance needed to get the serializer settings.
let dummyClient = new ContainerInstanceManagementClient (Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let asAzureResource (group:ContainerGroupConfig) =
    arm { add_resource group }
    |> findAzureResources<ContainerGroup> dummyClient.SerializationSettings
    |> List.head
    |> fun r ->
        r.Validate()
        r

let tests = testList "Container Group" [
    test "Single container in a group is correctly created" {
        let group =
            containerGroup {
                name "appWithHttpFrontend"
                operating_system Linux
                restart_policy AlwaysRestart
                add_udp_port 123us
                add_instances [ nginx ]
                network_profile "test"
            } |> asAzureResource

        Expect.equal group.Name "appWithHttpFrontend" "Group name is not set correctly."
        Expect.equal group.OsType "Linux" "OS should be Linux"
        Expect.equal group.IpAddress.Ports.[1].PortProperty 123 "Incorrect udp port"
        Expect.equal group.NetworkProfile.Id "[resourceId('Microsoft.Network/networkProfiles', 'test')]" "Incorrect network profile reference"

        let containerInstance = group.Containers.[0]
        Expect.equal containerInstance.Image "nginx:1.17.6-alpine" "Incorrect image"
        Expect.equal containerInstance.Name "nginx" "Incorrect instance name"
        Expect.equal containerInstance.Resources.Requests.MemoryInGB 0.5 "Incorrect memory"
        Expect.equal containerInstance.Resources.Requests.Cpu 1.0 "Incorrect CPU"
        let ports = containerInstance.Ports |> Seq.map(fun p -> p.Port) |> Seq.toList
        Expect.equal ports [ 80; 443; 9090 ] "Incorrect ports on container"
    }

    test "Multiple containers are correctly added" {
        let group = containerGroup { add_instances [ nginx; fsharpApp ] } |> asAzureResource

        Expect.hasLength group.Containers 2 "Should be two containers"
        Expect.equal group.Containers.[0].Name "nginx" "Incorrect container name"
        Expect.equal group.Containers.[1].Name "fsharpapp" "Incorrect container name"
        Expect.equal group.Containers.[1].Resources.Requests.MemoryInGB 1.5 "Incorrect memory"
        Expect.equal group.Containers.[1].Resources.Requests.Cpu 2.0 "Incorrect CPU count"
    }

    test "Implicitly creates public ports for group based on instances" {
        let group = containerGroup { add_instances [ nginx ] } |> asAzureResource

        let ports = group.IpAddress.Ports |> Seq.map(fun p -> p.PortProperty) |> Seq.toList
        Expect.equal ports ([ 80; 443 ]) "Incorrect implicitly created public ports"
        Expect.hasLength group.Containers.[0].Ports 3 "Incorrect number of private port"
    }

    test "Does not create two ports with the same number across public and private" {
        let group =
            containerGroup {
                add_instances [
                    containerInstance {
                        name "foo"
                        add_ports PublicPort [ 123us ]
                        add_ports InternalPort [ 123us ]
                    }
                ]
            } |> asAzureResource

        Expect.isEmpty group.IpAddress.Ports "Should not be any public ports"
        Expect.equal group.Containers.[0].Ports.[0].Port 123 "Incorrect private port"
        Expect.hasLength group.Containers.[0].Ports 1 "Should only be one port"
    }

    test "Adds container group with volumes mounted on each container." {
        let helloShared1 = containerInstance {
            name "hello-shared-dir1"
            image "mcr.microsoft.com/azuredocs/aci-helloworld:latest"
            add_ports PublicPort [ 80us ]
            add_volume_mount "shared-socket" "/var/lib/shared/hello"
            add_volume_mount "source-code" "/src/farmer"
            add_volume_mount "secret-files" "/config/secrets"
        }
        let helloShared2 = containerInstance {
            name "hello-shared-dir2"
            add_ports PublicPort [ 81us ]
            env_vars [
                env_var "testing" "environment variables"
            ]
            image "mcr.microsoft.com/azuredocs/aci-helloworld:latest"
            add_volume_mount "shared-socket" "/var/lib/shared/hello"
            add_volume_mount "azure-file" "/var/lib/files"
        }
        let group =
            containerGroup {
                name "containersWithFiles"
                add_instances [ helloShared1; helloShared2 ]
                add_volumes [
                    volume_mount.azureFile "azure-file" "fileShare1" "storageaccount1"
                    volume_mount.secret_string "secret-files" "secret1" "abcdefg"
                    volume_mount.empty_dir "shared-socket"
                    volume_mount.git_repo "source-code" (Uri "https://github.com/CompositionalIT/farmer")
                ]
            } |> asAzureResource

        Expect.equal group.Name "containersWithFiles" "Incorrect name on container group"
        Expect.equal group.Containers.[0].VolumeMounts.Count 3 "Incorrect number of volume mounts on container 1"
        Expect.equal group.Containers.[1].VolumeMounts.Count 2 "Incorrect number of volume mounts on container 1"
        Expect.hasLength group.Volumes 4 "Incorrect number of volumes in group"
        Expect.isNotNull group.Volumes.[0].AzureFile "Azure file volume should not be null"
        Expect.isNotNull group.Volumes.[1].Secret "Secret volume should not be null"
        Expect.isNotNull group.Volumes.[2].EmptyDir "Empty directory volume should not be null"
        Expect.isNotNull group.Volumes.[3].GitRepo "Git repo volume should not be null"
    }

    test "Container group with private registry" {
        let group =
            containerGroup {
                add_instances [ nginx ]
                add_registry_credentials [
                    registry "my-registry.azurecr.io" "user"
                ]
            } |> asAzureResource
        Expect.hasLength group.ImageRegistryCredentials 1 "Expected one image registry credential"
        let credentials = group.ImageRegistryCredentials.[0]
        Expect.equal credentials.Server "my-registry.azurecr.io" "Incorrect container image registry server"
        Expect.equal credentials.Username "user" "Incorrect container image registry user"
        Expect.equal credentials.Password "[parameters('my-registry.azurecr.io-password')]" "Container image registry password should be secure parameter"
    }
]


module ContainerGroup

open Expecto
open Farmer
open Farmer.Arm
open Farmer.Identity
open Farmer.ContainerGroup
open Farmer.Builders
open Farmer.Network
open Microsoft.Azure.Management.ContainerInstance
open Microsoft.Azure.Management.ContainerInstance.Models
open Microsoft.Rest
open System
open Newtonsoft.Json.Linq

let nginx =
    containerInstance {
        name "nginx"
        image "nginx:1.17.6-alpine"
        add_ports PublicPort [ 80us; 443us ]
        add_ports InternalPort [ 9090us ]
        memory 0.5<Gb>
        cpu_cores 1
    }

let fsharpApp =
    containerInstance {
        name "fsharpApp"
        image "myrepo/myapp:1.7.2"
        add_ports PublicPort [ 8080us ]
        memory 1.5<Gb>
        cpu_cores 2
    }

let appWithoutPorts =
    containerInstance {
        name "appWithoutPorts"
        image "myapp:1.7.2"
        memory 1.5<Gb>
        cpu_cores 2
    }

/// Client instance needed to get the serializer settings.
let dummyClient =
    new ContainerInstanceManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let asAzureResource (group: ContainerGroupConfig) =
    arm { add_resource group }
    |> findAzureResources<ContainerGroup> dummyClient.SerializationSettings
    |> List.head
    |> fun r ->
        r.Validate()
        r

let tests =
    testList
        "Container Group"
        [
            test "Single container in a group is correctly created" {
                let group =
                    containerGroup {
                        name "appWithHttpFrontend"
                        operating_system Linux
                        restart_policy AlwaysRestart
                        add_udp_port 123us
                        add_instances [ nginx ]
                        network_profile "test"
                    }
                    |> asAzureResource

                Expect.equal group.Name "appWithHttpFrontend" "Group name is not set correctly."
                Expect.equal group.OsType "Linux" "OS should be Linux"
                Expect.equal group.IpAddress.Ports.[1].PortProperty 123 "Incorrect udp port"

                let containerInstance = group.Containers.[0]
                Expect.equal containerInstance.Image "nginx:1.17.6-alpine" "Incorrect image"
                Expect.equal containerInstance.Name "nginx" "Incorrect instance name"
                Expect.equal containerInstance.Resources.Requests.MemoryInGB 0.5 "Incorrect memory"
                Expect.equal containerInstance.Resources.Requests.Cpu 1.0 "Incorrect CPU"
                let ports = containerInstance.Ports |> Seq.map (fun p -> p.Port) |> Seq.toList
                Expect.equal ports [ 80; 443; 9090 ] "Incorrect ports on container"
            }

            test "Container group with init containers" {
                let group =
                    let emptyDir1 = "emptyDir1"

                    containerGroup {
                        name "appWithInitContainers"
                        add_volumes [ volume_mount.empty_dir emptyDir1 ]

                        add_init_containers
                            [
                                initContainer {
                                    name "init"
                                    image "busybox"

                                    command_line
                                        [
                                            "/bin/sh"
                                            "-c"
                                            "sleep 60; echo python wordcount.py http://shakespeare.mit.edu/romeo_juliet/full.html > /mnt/emptydir/command_line.txt"
                                        ]

                                    add_volume_mount emptyDir1 "/mnt/emptydir"
                                }
                            ]

                        add_instances
                            [
                                containerInstance {
                                    name "hamlet"
                                    image "mcr.microsoft.com/azuredocs/aci-wordcount"
                                    add_volume_mount emptyDir1 "/mnt/emptydir"
                                    env_vars [ "NumWords", "3"; "MinLength", "5" ]
                                }
                            ]
                    }
                    |> asAzureResource

                let containerInstance = group.Containers.[0]

                Expect.equal
                    containerInstance.Image
                    "mcr.microsoft.com/azuredocs/aci-wordcount:latest"
                    "Incorrect containerInstance image"

                Expect.equal containerInstance.Name "hamlet" "Incorrect containerInstance name"
                let initContainer = group.InitContainers.[0]
                Expect.equal initContainer.Image "busybox:latest" "Incorrect initContainer image"
                Expect.equal initContainer.Name "init" "Incorrect initContainer name"
            }

            test "Group without public ip" {
                let group =
                    containerGroup {
                        name "myGroup"
                        operating_system Linux
                        restart_policy RestartOnFailure
                        add_instances [ appWithoutPorts ]
                    }

                Expect.isNone group.IpAddress "IpAddresses should be none"
            }

            test "Container with command line arguments" {
                let containerInstance =
                    containerInstance {
                        name "appWithCommand"
                        image "myapp:1.7.2"
                        memory 1.5<Gb>
                        cpu_cores 2
                        command_line [ "echo"; "hello world" ]
                    }

                Expect.equal
                    containerInstance.Command
                    [ "echo"; "hello world" ]
                    "Incorrect container command line arguments"
            }

            test "Multiple containers are correctly added" {
                let group = containerGroup { add_instances [ nginx; fsharpApp ] } |> asAzureResource

                Expect.hasLength group.Containers 2 "Should be two containers"
                Expect.equal group.Containers.[0].Name "nginx" "Incorrect container name"

                Expect.equal
                    group.Containers.[0].Image
                    "nginx:1.17.6-alpine"
                    "Incorrect image tag generated on first container instance"

                Expect.equal group.Containers.[1].Name "fsharpapp" "Incorrect container name"

                Expect.equal
                    group.Containers.[1].Image
                    "myrepo/myapp:1.7.2"
                    "Incorrect image tag generated on second container instance"

                Expect.equal group.Containers.[1].Resources.Requests.MemoryInGB 1.5 "Incorrect memory"
                Expect.equal group.Containers.[1].Resources.Requests.Cpu 2.0 "Incorrect CPU count"
            }

            test "Implicitly creates public ports for group based on instances" {
                let group = containerGroup { add_instances [ nginx ] } |> asAzureResource

                let ports = group.IpAddress.Ports |> Seq.map (fun p -> p.PortProperty) |> Seq.toList
                Expect.equal ports ([ 80; 443 ]) "Incorrect implicitly created public ports"
                Expect.hasLength group.Containers.[0].Ports 3 "Incorrect number of private port"
            }

            test "Does not create two ports with the same number across public and private" {
                let group =
                    containerGroup {
                        add_instances
                            [
                                containerInstance {
                                    name "foo"
                                    image "testrepo"
                                    add_ports PublicPort [ 123us ]
                                    add_ports InternalPort [ 123us ]
                                }
                            ]
                    }
                    |> asAzureResource

                Expect.equal group.IpAddress null "Should not be any public ports"
                Expect.equal group.Containers.[0].Ports.[0].Port 123 "Incorrect private port"
                Expect.hasLength group.Containers.[0].Ports 1 "Should only be one port"
            }

            test "Adds container group with volumes mounted on each container." {
                let helloShared1 =
                    containerInstance {
                        name "hello-shared-dir1"
                        image "mcr.microsoft.com/azuredocs/aci-helloworld:latest"
                        add_ports PublicPort [ 80us ]
                        add_volume_mount "shared-socket" "/var/lib/shared/hello"
                        add_volume_mount "source-code" "/src/farmer"
                        add_volume_mount "secret-files" "/config/secrets"
                    }

                let helloShared2 =
                    containerInstance {
                        name "hello-shared-dir2"
                        add_ports PublicPort [ 81us ]
                        env_vars [ "testing", "environment variables" ]
                        image "mcr.microsoft.com/azuredocs/aci-helloworld:latest"
                        add_volume_mount "shared-socket" "/var/lib/shared/hello"
                        add_volume_mount "azure-file" "/var/lib/files"
                    }

                let group =
                    containerGroup {
                        name "containersWithFiles"
                        add_instances [ helloShared1; helloShared2 ]

                        add_volumes
                            [
                                volume_mount.azureFile "azure-file" "fileShare1" "storageaccount1"
                                volume_mount.secret_string "secret-files" "secret1" "abcdefg"
                                volume_mount.empty_dir "shared-socket"
                                volume_mount.git_repo "source-code" (Uri "https://github.com/CompositionalIT/farmer")
                            ]
                    }
                    |> asAzureResource

                Expect.equal group.Name "containersWithFiles" "Incorrect name on container group"

                Expect.equal
                    group.Containers.[0].VolumeMounts.Count
                    3
                    "Incorrect number of volume mounts on container 1"

                Expect.equal
                    group.Containers.[1].VolumeMounts.Count
                    2
                    "Incorrect number of volume mounts on container 1"

                Expect.hasLength group.Volumes 4 "Incorrect number of volumes in group"
                Expect.isNotNull group.Volumes.[0].AzureFile "Azure file volume should not be null"
                Expect.isNotNull group.Volumes.[1].Secret "Secret volume should not be null"
                Expect.isNotNull group.Volumes.[2].EmptyDir "Empty directory volume should not be null"
                Expect.isNotNull group.Volumes.[3].GitRepo "Git repo volume should not be null"
            }

            test "Container group with private registry" {
                let managedIdentity = ManagedIdentity.Empty

                let group =
                    containerGroup {
                        add_instances [ nginx ]
                        add_registry_credentials [ registry "my-registry.azurecr.io" "user" managedIdentity ]
                    }
                    |> asAzureResource

                Expect.hasLength group.ImageRegistryCredentials 1 "Expected one image registry credential"
                let credentials = group.ImageRegistryCredentials.[0]
                Expect.equal credentials.Server "my-registry.azurecr.io" "Incorrect container image registry server"
                Expect.equal credentials.Username "user" "Incorrect container image registry user"

                Expect.equal
                    credentials.Password
                    "[parameters('my-registry.azurecr.io-password')]"
                    "Container image registry password should be secure parameter"
            }

            test "Container group with managed identity to private registry" {
                let userAssignedIdentity =
                    ResourceId.create (Arm.ManagedIdentity.userAssignedIdentities, ResourceName "user", "resourceGroup")
                    |> UserAssignedIdentity

                let managedIdentity =
                    { ManagedIdentity.Empty with
                        UserAssigned = [ userAssignedIdentity ]
                    }

                let group =
                    containerGroup {
                        add_instances [ nginx ]

                        add_identity (
                            ResourceId.create (
                                Arm.ManagedIdentity.userAssignedIdentities,
                                ResourceName "user",
                                "resourceGroup"
                            )
                            |> UserAssignedIdentity
                        )

                        add_managed_identity_registry_credentials
                            [ registry "my-registry.azurecr.io" "user" managedIdentity ]
                    }
                    |> asAzureResource

                Expect.hasLength
                    group.ImageRegistryCredentials
                    1
                    "Expected one image managed identity registry credential"

                let credentials = group.ImageRegistryCredentials.[0]
                Expect.equal credentials.Server "my-registry.azurecr.io" "Incorrect container image registry server"
                Expect.equal credentials.Username String.Empty "Container image registry user should be null"

                Expect.equal
                    credentials.Identity
                    (managedIdentity.Dependencies.Head.ArmExpression.Eval())
                    "Incorrect container image registry identity"

                Expect.equal credentials.Password null "Container image registry password should be null"
            }

            test "Container group with an link_to_identity to private registry" {
                let resourceId =
                    ResourceId.create (ManagedIdentity.userAssignedIdentities, ResourceName "user", "resourceGroup")

                let managedIdentity =
                    { ManagedIdentity.Empty with
                        UserAssigned = [ (LinkedUserAssignedIdentity resourceId) ]
                    }

                let containerGroupConfig =
                    containerGroup {
                        add_instances [ nginx ]
                        link_to_identity resourceId

                        add_managed_identity_registry_credentials
                            [ registry "my-registry.azurecr.io" "user" managedIdentity ]
                    }

                let group = containerGroupConfig |> asAzureResource

                Expect.hasLength
                    group.ImageRegistryCredentials
                    1
                    "Expected one image managed identity registry credential"

                let credentials = group.ImageRegistryCredentials.[0]
                Expect.equal credentials.Server "my-registry.azurecr.io" "Incorrect container image registry server"
                Expect.equal credentials.Username String.Empty "Container image registry user should be null"

                Expect.equal
                    credentials.Identity
                    (managedIdentity.UserAssigned.Head.ResourceId.ArmExpression.Eval())
                    "Incorrect container image registry identity"

                Expect.notEqual credentials.Identity null "Identity should not be null"
                Expect.notEqual credentials.Identity String.Empty "Identity should not be an empty string"

                Expect.equal
                    containerGroupConfig.Identity.Dependencies.Length
                    0
                    "Container Group Config Identity Dependencies should be 0"

                Expect.equal credentials.Password null "Container image registry password should be null"
            }

            test "Container group with reference to private registry" {
                let group =
                    containerGroup {
                        add_instances [ nginx ]

                        reference_registry_credentials
                            [
                                // Reference a container registry in a different resource group.
                                ResourceId.create (
                                    Arm.ContainerRegistry.registries,
                                    ResourceName "my-registry",
                                    "my-reg-group"
                                )
                            ]
                    }
                    |> asAzureResource

                Expect.hasLength group.ImageRegistryCredentials 1 "Expected one image registry credential"
                let credentials = group.ImageRegistryCredentials.[0]

                Expect.equal
                    credentials.Server
                    "[reference(resourceId('my-reg-group', 'Microsoft.ContainerRegistry/registries', 'my-registry'), '2019-05-01').loginServer]"
                    "Image registry server should come from 'reference'"

                Expect.equal
                    credentials.Username
                    "[listCredentials(resourceId('my-reg-group', 'Microsoft.ContainerRegistry/registries', 'my-registry'), '2019-05-01').username]"
                    "mage registry user should come from 'listCredentials'"

                Expect.equal
                    credentials.Password
                    "[listCredentials(resourceId('my-reg-group', 'Microsoft.ContainerRegistry/registries', 'my-registry'), '2019-05-01').passwords[0].value]"
                    "Image registry password should come from listCredentials"
            }

            test "Container group with system assigned identity" {
                let group =
                    containerGroup {
                        name "myapp"
                        add_instances [ nginx ]
                        system_identity
                    }
                    |> asAzureResource

                Expect.isTrue group.Identity.Type.HasValue "Expecting an assigned identity."

                Expect.equal
                    group.Identity.Type.Value
                    ResourceIdentityType.SystemAssigned
                    "Expecting a system assigned identity"
            }

            test "Container group with user assigned identity" {
                let group =
                    containerGroup {
                        name "myapp"
                        add_instances [ nginx ]

                        add_identity (
                            ResourceId.create (
                                Arm.ManagedIdentity.userAssignedIdentities,
                                ResourceName "user",
                                "resourceGroup"
                            )
                            |> UserAssignedIdentity
                        )
                    }
                    |> asAzureResource

                Expect.hasLength group.Identity.UserAssignedIdentities 1 "No user assigned identity."
            }

            test "Make container group with MSI" {
                let msi = createUserAssignedIdentity "aciUser"

                let group =
                    containerGroup {
                        name "myapp-with-msi"
                        add_instances [ nginx ]
                        add_identity msi
                    }

                let template =
                    arm {
                        location Location.EastUS
                        add_resource msi
                        add_resource group
                    }

                let containerGroup =
                    template.Template.Resources
                    |> List.find (fun r -> r.ResourceId.Name.Value = "myapp-with-msi")
                    :?> Farmer.Arm.ContainerInstance.ContainerGroup

                Expect.isNonEmpty containerGroup.Identity.UserAssigned "Container group did not have identity"

                Expect.equal
                    containerGroup.Identity.UserAssigned.[0]
                    (UserAssignedIdentity(
                        ResourceId.create (Arm.ManagedIdentity.userAssignedIdentities, ResourceName "aciUser")
                    ))
                    "Expected user identity named 'aciUser'."
            }
            test "Secure environment variables are generated correctly" {
                let cg =
                    containerGroup {
                        name "myapp"

                        add_instances
                            [
                                containerInstance {
                                    name "nginx"
                                    image "nginx:1.17.6-alpine"
                                    env_vars [ EnvVar.createSecure "foo" "secret-foo" ]
                                }
                            ]
                    }

                let deployment = arm { add_resource cg }

                Expect.hasLength
                    deployment.Template.Parameters
                    1
                    "Should have a secure parameter for environment variable"

                Expect.equal
                    (deployment.Template.Parameters.Head.ArmExpression.Eval())
                    "[parameters('secret-foo')]"
                    "Generated incorrect secure parameter."
            }
            test "Secure environment variables are generated for init containers" {
                let cg =
                    containerGroup {
                        name "myapp"

                        add_init_containers
                            [
                                initContainer {
                                    name "nginx"
                                    image "nginx:1.17.6-alpine"
                                    env_vars [ EnvVar.createSecure "foo" "secret-init" ]
                                }
                            ]
                    }

                let deployment = arm { add_resource cg }

                Expect.hasLength
                    deployment.Template.Parameters
                    1
                    "Should have a secure parameter for initContainer environment variable"

                Expect.equal
                    (deployment.Template.Parameters.Head.ArmExpression.Eval())
                    "[parameters('secret-init')]"
                    "Generated incorrect secure parameter."
            }
            test "Secure parameters for secret volume is generated correctly" {
                let cg =
                    containerGroup {
                        name "myapp"

                        add_instances
                            [
                                containerInstance {
                                    name "nginx"
                                    image "nginx:1.17.6-alpine"
                                    add_volume_mount "secrets" "/config/secrets"
                                }
                            ]

                        add_volumes [ volume_mount.secret_parameter "secrets" "foo" "secret-foo" ]
                    }

                let deployment =
                    arm {
                        location Location.EastUS
                        add_resource cg
                    }

                Expect.hasLength deployment.Template.Parameters 1 "Should have a secure parameter for secret volume"

                Expect.equal
                    (deployment.Template.Parameters.Head.ArmExpression.Eval())
                    "[parameters('secret-foo')]"
                    "Generated incorrect secure parameter."
            }
            /// Test creates a storage account and container group where the storage account connection string
            /// is passed as an ARM expression in a secure environment variable.
            test "Secure environment variables created from ARM expressions" {
                let script =
                    """
#r "nuget: Azure.Storage.Blobs"

open System
open Azure.Storage.Blobs

async {
    while true do
        try
            let connectionString = Environment.GetEnvironmentVariable ("AZURE_STORAGE_CONNECTION_STRING")
            let blobServiceClient = BlobServiceClient (connectionString)
            let containerName = "quickstartblobs" + Guid.NewGuid().ToString()
            do! blobServiceClient.CreateBlobContainerAsync (containerName) |> Async.AwaitTask |> Async.Ignore
            Console.WriteLine "Created container."
        with
        | ex -> Console.Error.WriteLine ex
        do! Async.Sleep 30_000
} |> Async.RunSynchronously
"""

                let storage = storageAccount { name "containerdata1234" }

                let app =
                    containerGroup {
                        name "myapp"
                        depends_on storage

                        add_instances
                            [
                                containerInstance {
                                    name "app"
                                    image "mcr.microsoft.com/dotnet/sdk:5.0"
                                    add_volume_mount "script" "/app/src"
                                    command_line ("dotnet fsi /app/src/main.fsx".Split null |> List.ofArray)

                                    env_vars
                                        [ EnvVar.createSecureExpression "AZURE_STORAGE_CONNECTION_STRING" storage.Key ]
                                }
                            ]

                        add_volumes [ volume_mount.secret_string "script" "main.fsx" script ]
                    }

                let deployment =
                    arm {
                        location Location.EastUS
                        add_resources [ storage; app ]
                    }

                let json = deployment.Template |> Writer.toJson
                let jobj = JObject.Parse json
                let parameters = jobj.["parameters"]
                Expect.hasLength parameters 0 "Expected no parameters emitted with a SecureEnvExpression"

                let envVars =
                    jobj.SelectToken(
                        "$.resources[?(@.name=='myapp')].properties.containers[?(@.name=='app')].properties.environmentVariables"
                    )
                    :?> JArray

                Expect.hasLength envVars 1 "Expected to have an environment variable on the 'app' container"
                let firstEnvVar = envVars.[0]
                Expect.equal (firstEnvVar.["name"] |> string) "AZURE_STORAGE_CONNECTION_STRING" "Incorrect env var name"

                Expect.equal
                    (firstEnvVar.["secureValue"] |> string)
                    "[concat('DefaultEndpointsProtocol=https;AccountName=containerdata1234;AccountKey=', listKeys(resourceId('Microsoft.Storage/storageAccounts', 'containerdata1234'), '2017-10-01').keys[0].value)]"
                    "Incorrect env var expression value"
            }
            test "Container with liveness and readiness probes" {

                let cg =
                    containerGroup {
                        name "myapp"

                        add_instances
                            [
                                containerInstance {
                                    name "nginx"
                                    image "nginx:1.17.6-alpine"

                                    probes
                                        [
                                            liveness {
                                                http "https://whatever.com:8080/healthcheck"
                                                period_seconds 30 // Wait 30 seconds between each liveness check
                                                failure_threshold 10 // After 10 tries, consider this unhealthy
                                            }
                                            readiness {
                                                http "https://whatever.com:8080/healthcheck"
                                                initial_delay_seconds 30 // Wait 30 seconds after the container is started before a readiness check
                                                failure_threshold 5 // Let it retry 5 times, giving another 50 seconds to try to start
                                            }
                                        ]
                                }
                            ]
                    }
                    |> asAzureResource

                let livenessProbe = cg.Containers.[0].LivenessProbe
                Expect.isNotNull livenessProbe "Resulting container should have a liveness probe"
                Expect.equal livenessProbe.HttpGet.Path "/healthcheck" "Incorrect path on liveness http probe"
                Expect.equal livenessProbe.HttpGet.Port 8080 "Incorrect port on liveness http probe"
                Expect.equal livenessProbe.HttpGet.Scheme "https" "Incorrect scheme on liveness http probe"
                Expect.equal livenessProbe.PeriodSeconds.Value 30 "Incorrect period on liveness probe"
                Expect.equal livenessProbe.FailureThreshold.Value 10 "Incorrect failure threshold on liveness probe"
                let readinessProbe = cg.Containers.[0].ReadinessProbe
                Expect.isNotNull readinessProbe "Resulting container should have a readiness probe"
                Expect.equal readinessProbe.HttpGet.Path "/healthcheck" "Incorrect path on readiness http probe"
                Expect.equal readinessProbe.HttpGet.Port 8080 "Incorrect port on readiness http probe"
                Expect.equal readinessProbe.HttpGet.Scheme "https" "Incorrect scheme on readiness http probe"

                Expect.equal
                    readinessProbe.InitialDelaySeconds.Value
                    30
                    "Incorrect initial delay threshold on readiness probe"

                Expect.equal readinessProbe.FailureThreshold.Value 5 "Incorrect failure threshold on readiness probe"
            }
            test "Container group with vnet and subnet has subnetIds and expected dependsOn" {
                let template =
                    arm {
                        add_resources
                            [
                                vnet {
                                    name "containernet"
                                    add_address_spaces [ "10.30.32.0/20" ]

                                    add_subnets
                                        [
                                            subnet {
                                                name "ContainerSubnet"
                                                prefix "10.30.41.0/24"
                                                add_delegations [ SubnetDelegationService.ContainerGroups ]
                                            }
                                        ]
                                }
                                containerGroup {
                                    name "appWithHttpFrontend"
                                    operating_system Linux
                                    restart_policy AlwaysRestart
                                    add_instances [ nginx ]
                                    vnet "containernet"
                                    subnet "ContainerSubnet"
                                }
                            ]
                    }

                let json = template.Template |> Writer.toJson
                let jobj = JObject.Parse json

                let containerGroupJson =
                    jobj.SelectToken("resources[?(@.name=='appWithHttpFrontend')]")

                let apiVersion = containerGroupJson.["apiVersion"] |> string
                let apiDate = DateOnly.Parse apiVersion

                Expect.isGreaterThanOrEqual
                    apiDate
                    (DateOnly.Parse "2021-07-01")
                    "Expecting minimum version of 2021-07-01 for 'subnetIds' support"

                let subnetIds = containerGroupJson.SelectToken("properties.subnetIds") :?> JArray
                Expect.hasLength subnetIds 1 "Incorrect number of subnetIds"

                let expectedSubnetId =
                    "[resourceId('Microsoft.Network/virtualNetworks/subnets', 'containernet', 'ContainerSubnet')]"

                let firstSubnetId = string subnetIds.First.["id"]
                Expect.equal firstSubnetId expectedSubnetId "Subnet ID not in 'subnetIds'"
                let dependsOn = containerGroupJson.SelectToken("dependsOn") :?> JArray

                let expectedContainerNetDeps =
                    "[resourceId('Microsoft.Network/virtualNetworks', 'containernet')]"

                Expect.hasLength dependsOn 1 "containerGroup has wrong number of dependencies"
                let actualContainerNetDeps = string dependsOn.First
                Expect.equal actualContainerNetDeps expectedContainerNetDeps "Dependencies didn't match"
            }
            test "Container groups with subnetIds and netprofile uses correct API versions" {
                let template =
                    arm {
                        add_resources
                            [
                                vnet {
                                    name "containernet"
                                    add_address_spaces [ "10.30.32.0/20" ]

                                    add_subnets
                                        [
                                            subnet {
                                                name "ContainerSubnet"
                                                prefix "10.30.41.0/24"
                                                add_delegations [ SubnetDelegationService.ContainerGroups ]
                                            }
                                        ]
                                }
                                networkProfile {
                                    name "netprofile"
                                    vnet "containernet"
                                    subnet "ContainerSubnet"
                                }
                                containerGroup {
                                    name "appWithNetProfile"
                                    operating_system Linux
                                    restart_policy AlwaysRestart

                                    add_instances
                                        [
                                            containerInstance {
                                                name "nginx"
                                                image "nginx:1.21.6-alpine"
                                            }
                                        ]

                                    network_profile "netprofile"
                                }
                                containerGroup {
                                    name "appWithSubnetIds"
                                    operating_system Linux
                                    restart_policy AlwaysRestart

                                    add_instances
                                        [
                                            containerInstance {
                                                name "nginx"
                                                image "nginx:1.21.6-alpine"
                                            }
                                        ]

                                    vnet "containernet"
                                    subnet "ContainerSubnet"
                                }
                            ]
                    }

                let jobj = template.Template |> Writer.toJson |> JObject.Parse

                let containerGroupNetProfile =
                    jobj.SelectToken("resources[?(@.name=='appWithNetProfile')]")

                let netProfileApiVersion =
                    containerGroupNetProfile.["apiVersion"] |> string |> DateOnly.Parse

                Expect.isLessThanOrEqual
                    netProfileApiVersion
                    (DateOnly.Parse "2021-03-01")
                    "Expecting maximum version of 2021-03-01 for 'networkProfile' support"

                let containerGroupSubnetIds =
                    jobj.SelectToken("resources[?(@.name=='appWithSubnetIds')]")

                let subnetIdsApiVersion =
                    containerGroupSubnetIds.["apiVersion"] |> string |> DateOnly.Parse

                Expect.isGreaterThanOrEqual
                    subnetIdsApiVersion
                    (DateOnly.Parse "2021-07-01")
                    "Expecting minimum version of 2021-07-01 for 'subnetIds' support"
            }
            test "Container network profile with vnet has expected dependsOn" {
                let template =
                    arm {
                        add_resources
                            [
                                vnet {
                                    name "containernet"
                                    add_address_spaces [ "10.30.32.0/20" ]

                                    add_subnets
                                        [
                                            subnet {
                                                name "ContainerSubnet"
                                                prefix "10.30.41.0/24"
                                                add_delegations [ SubnetDelegationService.ContainerGroups ]
                                            }
                                        ]
                                }
                                networkProfile {
                                    name "netprofile"
                                    vnet "containernet"
                                    subnet "ContainerSubnet"
                                }
                                containerGroup {
                                    name "appWithHttpFrontend"
                                    operating_system Linux
                                    restart_policy AlwaysRestart
                                    add_instances [ nginx ]
                                    network_profile "netprofile"
                                }
                            ]
                    }

                let jobj = template.Template |> Writer.toJson |> JObject.Parse

                let containerGroupJson =
                    jobj.SelectToken("resources[?(@.name=='appWithHttpFrontend')]")

                let apiVersion = containerGroupJson.["apiVersion"] |> string
                let apiDate = DateOnly.Parse apiVersion

                Expect.isLessThanOrEqual
                    apiDate
                    (DateOnly.Parse "2021-03-01")
                    "Expecting maximum version of 2021-03-01 for 'networkProfile' support"

                let expectedContainerNetDeps =
                    "[resourceId('Microsoft.Network/virtualNetworks', 'containernet')]"

                let dependsOn = jobj.SelectToken("resources[?(@.name=='netprofile')].dependsOn")
                Expect.hasLength dependsOn 1 "netprofile has wrong number of dependencies"

                let actualContainerNetDeps =
                    (dependsOn :?> Newtonsoft.Json.Linq.JArray).First.ToString()

                Expect.equal actualContainerNetDeps expectedContainerNetDeps "Dependencies didn't match"
            }
            test "Container network profile with linked vnet has empty dependsOn" {
                let template =
                    arm {
                        add_resources
                            [
                                networkProfile {
                                    name "netprofile"
                                    link_to_vnet "containernet"
                                    subnet "ContainerSubnet"
                                }
                                containerGroup {
                                    name "appWithHttpFrontend"
                                    operating_system Linux
                                    restart_policy AlwaysRestart
                                    add_instances [ nginx ]
                                    network_profile "netprofile"
                                }
                            ]
                    }

                let json = template.Template |> Writer.toJson
                let jobj = Newtonsoft.Json.Linq.JObject.Parse(json)
                let dependsOn = jobj.SelectToken("resources[?(@.name=='netprofile')].dependsOn")
                Expect.hasLength dependsOn 0 "network profile had dependencies when existing vnet was linked"
            }
            test "Container network profile with linked vnet in another resource group has empty dependsOn" {
                let template =
                    arm {
                        add_resources
                            [
                                networkProfile {
                                    name "netprofile"

                                    link_to_vnet (
                                        ResourceId.create (
                                            virtualNetworks,
                                            (ResourceName "containerNet"),
                                            group = "other-res-group"
                                        )
                                    )

                                    subnet "ContainerSubnet"
                                }
                            ]
                    }

                let json = template.Template |> Writer.toJson
                let jobj = Newtonsoft.Json.Linq.JObject.Parse(json)
                let subnetId = jobj.SelectToken("..subnet.id") |> string

                let expectedSubnetId =
                    "[resourceId('other-res-group', 'Microsoft.Network/virtualNetworks/subnets', 'containerNet', 'ContainerSubnet')]"

                Expect.equal subnetId expectedSubnetId "Generated incorrect subnet ID."
            }
            test "Container network profile allows naming of ip configs" {
                let template =
                    arm {
                        add_resources
                            [
                                vnet {
                                    name "containernet"
                                    add_address_spaces [ "10.30.32.0/20" ]

                                    add_subnets
                                        [
                                            subnet {
                                                name "ContainerSubnet"
                                                prefix "10.30.41.0/24"
                                                add_delegations [ SubnetDelegationService.ContainerGroups ]
                                            }
                                        ]
                                }
                                networkProfile {
                                    name "netprofile"
                                    vnet "containernet"
                                    ip_config "ipconfigProfile" "ContainerSubnet"
                                }
                            ]
                    }

                let json = template.Template |> Writer.toJson
                let jobj = Newtonsoft.Json.Linq.JObject.Parse(json)

                let ipConfigName =
                    jobj.SelectToken(
                        "resources[?(@.name=='netprofile')].properties.containerNetworkInterfaceConfigurations[0].properties.ipConfigurations[0].name"
                    )

                Expect.equal (string ipConfigName) "ipconfigProfile" "netprofile ipConfiguration has wrong name"
            }

            test "Support for additional dependencies" {
                let storage = storageAccount { name "containerstorage" }

                let myGroup =
                    containerGroup {
                        name "myContainerGroup"
                        depends_on storage
                    }

                let template = arm { add_resources [ storage; myGroup ] }
                let json = template.Template |> Writer.toJson
                let jobj = json |> Newtonsoft.Json.Linq.JObject.Parse

                let dependencies =
                    jobj.SelectToken "resources[?(@.name=='myContainerGroup')].dependsOn"

                Expect.sequenceEqual
                    dependencies
                    [
                        JValue "[resourceId('Microsoft.Storage/storageAccounts', 'containerstorage')]"
                    ]
                    "Did not have correct dependencies"
            }

            test "Adds GPU to container instance" {
                let group =
                    containerGroup {
                        add_instances
                            [
                                containerInstance {
                                    name "foo"
                                    image "myrepo/gpucontainers"

                                    gpu (
                                        containerInstanceGpu {
                                            count 1
                                            sku Gpu.V100
                                        }
                                    )
                                }
                            ]
                    }
                    |> asAzureResource

                let container = group.Containers |> Seq.head
                let gpu = container.Resources.Requests.Gpu
                Expect.equal gpu.Count 1 "Wrong amount of GPUs"
                Expect.equal gpu.Sku "V100" "Wrong SKU"
                Expect.equal container.Image "myrepo/gpucontainers:latest" "Incorrect image tag"
            }

            test "Container group created in a specific zone" {
                let deployment =
                    arm {
                        add_resources
                            [
                                containerGroup {
                                    name "zonal-container-group"

                                    add_instances
                                        [
                                            containerInstance {
                                                name "httpserver"
                                                image "nginx"
                                            }
                                        ]

                                    availability_zone "2"
                                }
                            ]
                    }

                let jobj = deployment.Template |> Writer.toJson |> JObject.Parse

                let containerGroupJson =
                    jobj.SelectToken("resources[?(@.name=='zonal-container-group')]")

                let zones = containerGroupJson.SelectToken "zones"
                let apiVersion = containerGroupJson.["apiVersion"] |> string
                let apiDate = DateOnly.Parse apiVersion

                Expect.isGreaterThanOrEqual
                    apiDate
                    (DateOnly.Parse "2021-09-01")
                    "Expecting minimum version of 2021-09-01 for 'zones' support"

                Expect.hasLength zones 1 "Incorrect number of zones"
                Expect.sequenceEqual zones [ JValue "2" ] "Incorrect value for zone"
            }

            test "Enable container logging workspace" {
                let deployment =
                    let workspace = logAnalytics { name "containergrouplogs1234" }

                    arm {
                        add_resources
                            [
                                workspace
                                containerGroup {
                                    name "container-group-with-insights"

                                    add_instances
                                        [
                                            containerInstance {
                                                name "httpserver"
                                                image "nginx"
                                            }
                                        ]

                                    diagnostics_workspace LogType.ContainerInstanceLogs workspace
                                }
                            ]
                    }

                let jobj = deployment.Template |> Writer.toJson |> JObject.Parse

                let logAnalytics =
                    jobj.SelectToken
                        "resources[?(@.name=='container-group-with-insights')].properties.diagnostics.logAnalytics"

                let workspaceId = logAnalytics.SelectToken "workspaceId"
                let workspaceKey = logAnalytics.SelectToken "workspaceKey"
                let logType = logAnalytics.SelectToken "logType"

                Expect.equal
                    (string workspaceId)
                    "[reference(resourceId('Microsoft.OperationalInsights/workspaces', 'containergrouplogs1234'), '2020-03-01-preview').customerId]"
                    "Incorrect value for workspaceId"

                Expect.equal
                    (string workspaceKey)
                    "[listkeys(resourceId('Microsoft.OperationalInsights/workspaces', 'containergrouplogs1234'), '2020-03-01-preview').primarySharedKey]"
                    "Incorrect value for workspaceKey"

                Expect.equal (string logType) "ContainerInstanceLogs" "Incorrect value for workspaceId"

                let cgDependencies =
                    jobj.SelectToken "resources[?(@.name=='container-group-with-insights')].dependsOn"

                Expect.hasLength cgDependencies 1 "Incorrect number of dependencies for diagnostics workspace"
            }

            test "Enable linking to container logging workspace" {
                let deployment =
                    let workspaceId = LogAnalytics.workspaces.resourceId "my-log-analytics-workspace"

                    arm {
                        add_resources
                            [
                                containerGroup {
                                    name "container-group-with-insights"

                                    add_instances
                                        [
                                            containerInstance {
                                                name "httpserver"
                                                image "nginx"
                                            }
                                        ]

                                    link_to_diagnostics_workspace LogType.ContainerInstanceLogs workspaceId
                                }
                            ]
                    }

                let jobj = deployment.Template |> Writer.toJson |> JObject.Parse

                let logAnalytics =
                    jobj.SelectToken
                        "resources[?(@.name=='container-group-with-insights')].properties.diagnostics.logAnalytics"

                let workspaceId = logAnalytics.SelectToken "workspaceId"
                let workspaceKey = logAnalytics.SelectToken "workspaceKey"
                let logType = logAnalytics.SelectToken "logType"

                Expect.equal
                    (string workspaceId)
                    "[reference(resourceId('Microsoft.OperationalInsights/workspaces', 'my-log-analytics-workspace'), '2020-03-01-preview').customerId]"
                    "Incorrect value for workspaceId"

                Expect.equal
                    (string workspaceKey)
                    "[listkeys(resourceId('Microsoft.OperationalInsights/workspaces', 'my-log-analytics-workspace'), '2020-03-01-preview').primarySharedKey]"
                    "Incorrect value for workspaceKey"

                Expect.equal (string logType) "ContainerInstanceLogs" "Incorrect value for workspaceId"

                let cgDependencies =
                    jobj.SelectToken "resources[?(@.name=='container-group-with-insights')].dependsOn"

                Expect.isEmpty cgDependencies "Should have no dependencies when linking to a workspace."
            }

            test "Enable passing key to container logging workspace" {
                let fakeWorkspaceId = Guid.NewGuid() |> string
                let fakeWorkspaceKey = Guid.NewGuid() |> string

                let deployment =
                    arm {
                        add_resources
                            [
                                containerGroup {
                                    name "container-group-with-insights"

                                    add_instances
                                        [
                                            containerInstance {
                                                name "httpserver"
                                                image "nginx"
                                            }
                                        ]

                                    diagnostics_workspace_key
                                        LogType.ContainerInstanceLogs
                                        fakeWorkspaceId
                                        fakeWorkspaceKey
                                }
                            ]
                    }

                let jobj = deployment.Template |> Writer.toJson |> JObject.Parse

                let logAnalytics =
                    jobj.SelectToken
                        "resources[?(@.name=='container-group-with-insights')].properties.diagnostics.logAnalytics"

                let workspaceId = logAnalytics.SelectToken "workspaceId"
                let workspaceKey = logAnalytics.SelectToken "workspaceKey"
                Expect.equal (string workspaceId) fakeWorkspaceId "Incorrect value for workspaceId"
                Expect.equal (string workspaceKey) fakeWorkspaceKey "Incorrect value for workspaceKey"

                let cgDependencies =
                    jobj.SelectToken "resources[?(@.name=='container-group-with-insights')].dependsOn"

                Expect.isEmpty cgDependencies "Should have no dependencies when linking to a workspace."
            }

            test "Specify DNS nameservers and search domains" {
                let deployment =
                    arm {
                        add_resources
                            [
                                vnet {
                                    name "mynetwork"
                                    add_address_spaces [ "10.30.32.0/20" ]

                                    add_subnets
                                        [
                                            subnet {
                                                name "containers"
                                                prefix "10.30.41.0/24"
                                                add_delegations [ SubnetDelegationService.ContainerGroups ]
                                            }
                                        ]
                                }
                                networkProfile {
                                    name "netprofile"
                                    vnet "mynetwork"
                                    subnet "containers"
                                }
                                containerGroup {
                                    name "container-group-with-custom-dns"
                                    dns_nameservers [ "8.8.8.8"; "1.1.1.1" ]
                                    dns_search_domains [ "example.com"; "example.local" ]

                                    add_instances
                                        [
                                            containerInstance {
                                                name "httpserver"
                                                image "nginx:1.17.6-alpine"
                                            }
                                        ]

                                    network_profile "netprofile"
                                }
                            ]
                    }

                let jobj = deployment.Template |> Writer.toJson |> JObject.Parse

                let dnsConfig =
                    jobj.SelectToken "resources[?(@.name=='container-group-with-custom-dns')].properties.dnsConfig"

                let nameservers = dnsConfig.SelectToken "nameServers"
                let searchDomains = dnsConfig.SelectToken "searchDomains"
                Expect.sequenceEqual nameservers [ JValue "8.8.8.8"; JValue "1.1.1.1" ] "Incorrect nameservers."
                Expect.equal searchDomains (JValue "example.com example.local") "Incorrect search domains."
            }

            test "Create container group created with a link_to_identity" {
                let resourceId =
                    ResourceId.create (ManagedIdentity.userAssignedIdentities, ResourceName "user", "resourceGroup")

                let managedIdentity: Identity.ManagedIdentity =
                    { ManagedIdentity.Empty with
                        UserAssigned = [ (LinkedUserAssignedIdentity resourceId) ]
                    }

                let containerGroup =
                    containerGroup {
                        name "container-group-with-link-to-identity"
                        link_to_identity resourceId

                        add_managed_identity_registry_credentials
                            [ registry "my-registry.azurecr.io" "user" managedIdentity ]

                        add_instances
                            [
                                containerInstance {
                                    name "httpserver"
                                    image "nginx:1.17.6-alpine"
                                }
                            ]
                    }

                let deployment =
                    arm {
                        add_resources
                            [
                                containerGroup

                            ]
                    }

                let jobj = deployment.Template |> Writer.toJson |> JObject.Parse

                let containerGroupJson =
                    jobj.SelectToken("resources[?(@.name=='container-group-with-link-to-identity')]")

                let dependsOn = containerGroupJson.SelectToken("dependsOn") :?> JArray
                Expect.equal dependsOn.Count 0 "Container group dependsOn list shall be empty"
            }

            test "Create container group created with a add_identity" {
                let resourceId =
                    ResourceId.create (ManagedIdentity.userAssignedIdentities, ResourceName "user", "resourceGroup")

                let userAssignedIdentity = resourceId |> UserAssignedIdentity

                let managedIdentity: Identity.ManagedIdentity =
                    { ManagedIdentity.Empty with
                        UserAssigned = [ userAssignedIdentity ]
                    }

                let containerGroup =
                    containerGroup {
                        name "container-group-with-add-identity"
                        add_identity userAssignedIdentity

                        add_managed_identity_registry_credentials
                            [ registry "my-registry.azurecr.io" "user" managedIdentity ]

                        add_instances
                            [
                                containerInstance {
                                    name "httpserver"
                                    image "nginx:1.17.6-alpine"
                                }
                            ]
                    }

                let deployment =
                    arm {
                        add_resources
                            [
                                containerGroup

                            ]
                    }

                let jobj = deployment.Template |> Writer.toJson |> JObject.Parse

                let containerGroupJson =
                    jobj.SelectToken("resources[?(@.name=='container-group-with-add-identity')]")

                let dependsOn = containerGroupJson.SelectToken("dependsOn") :?> JArray
                Expect.equal dependsOn.Count 1 "Container group dependsOn list shouldn't be empty"
            }
        ]

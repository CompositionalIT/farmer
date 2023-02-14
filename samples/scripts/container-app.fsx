#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.ContainerApp
open System

let queueName = "myqueue"
let storageName = $"{Guid.NewGuid().ToString().[0..5]}containerqueue"
let myStorageAccount = storageAccount {
    name storageName
    add_queue queueName
    add_file_share "certs"
}

let env =
    containerEnvironment {
        name $"containerenv{Guid.NewGuid().ToString().[0..5]}"
        add_containers [
            containerApp {
                name "aspnetsample"
                add_simple_container "mcr.microsoft.com/dotnet/samples" "aspnetapp"
                ingress_target_port 80us
                ingress_transport Auto
                add_http_scale_rule "http-scaler" { ConcurrentRequests = 10 }
                add_cpu_scale_rule "cpu-scaler" { Utilization = 50 }
            }
            containerApp {
                name "queuereaderapp"
                add_volumes [ Volume.emptyDir "empty-v"
                              Volume.azureFile "certs-v" (ResourceName "certs") myStorageAccount.Name StorageAccessMode.ReadOnly ]
                add_containers [
                    container {
                        name "queuereaderapp"
                        public_docker_image "mcr.microsoft.com/azuredocs/containerapps-queuereader" "latest"
                        cpu_cores 0.25<VCores>
                        memory 0.5<Gb>
                        ephemeral_storage 1.<Gb>
                        add_volume_mounts [ "empty-v", "/tmp"
                                            "certs-v", "/certs" ]
                    }
                ]
                replicas 1 10
                add_env_variable "QueueName" queueName
                add_secret_expression "queueconnectionstring" myStorageAccount.Key
            }
        ]
    }

let template =
    arm {
        location Location.NorthEurope
        add_resources [ env; myStorageAccount ]
    }

template
|> Deploy.execute "containerappsdemo" []
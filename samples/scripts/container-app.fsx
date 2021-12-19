#r @"..\..\src\Farmer\bin\Debug\net5.0\Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.ContainerApp
open System

let queueName = "myqueue"
let storageName = $"{Guid.NewGuid().ToString().[0..5]}containerqueue"
let myStorageAccount = storageAccount {
    name storageName
    add_queue queueName
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
                add_cpu_scale_rule "cpu-scaler" { Utilisation = 50 }
            }
            containerApp {
                name "queuereaderapp"
                add_containers [
                    container {
                        name "queuereaderapp"
                        public_docker_image "mcr.microsoft.com/azuredocs/containerapps-queuereader" ""
                        cpu_cores 1.0<VCores>
                        memory 1.0<Gb>
                    }
                ]
                replicas 1 10
                add_env_variable "QueueName" queueName
                add_secret_expression "queueconnectionstring" myStorageAccount.Key
                add_queue_scale_rule "queue-scaler" myStorageAccount queueName 10
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
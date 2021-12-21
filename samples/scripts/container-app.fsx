#r @"..\..\src\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.ContainerApp

let queueName = "myqueue"
let storageName = $"isaaccontainerqueue"
let myStorageAccount = storageAccount {
    name storageName
    add_queue queueName
}

let env =
    containerEnvironment {
        name $"containerenvisaac"
        add_containers [
            containerApp {
                name "queuereaderapp"
                allocate_resources ResourceLevels.``CPU = 1.75, RAM = 3.5``
                add_containers [
                    container {
                        name "aspnetsample"
                        public_docker_image "mcr.microsoft.com/dotnet/samples" "aspnetapp"
                    }
                    container {
                        name "queuereaderapp"
                        public_docker_image "mcr.microsoft.com/azuredocs/containerapps-queuereader" null
                    }
                ]
                ingress_target_port 80us
                ingress_transport Auto
                active_revision_mode ActiveRevisionsMode.Single
                add_http_scale_rule "http-scaler" { ConcurrentRequests = 10 }
                add_cpu_scale_rule "cpu-scaler" { Utilisation = 50 }
                replicas 1 10
                add_env_variable "QueueName" queueName
                add_secret_expression "queueconnectionstring" myStorageAccount.Key
                add_queue_scale_rule "queue-scaler" myStorageAccount queueName 10
            }
        ]
    }

env.ContainerApps.[0].Containers

let template =
    arm {
        location Location.NorthEurope
        add_resources [ env; myStorageAccount ]
    }

template
|> Deploy.execute "containerappsdemo" []
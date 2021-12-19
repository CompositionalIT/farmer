#r @"..\..\src\Farmer\bin\Debug\net5.0\Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.ContainerApp
open System

let queueName = "myqueue"
let storageName = "isaacsuperstore"
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
                ingress_visibility External
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
|> Writer.quickWrite "sample"



template
|> Deploy.execute "isaaccontainertest" []

//active_revision_mode ActiveRevisionsMode.Single
//replicas 1 5
//add_env_variable "ServiceBusQueueName" "wishrequests"
//add_app_secret "ServiceBusConnectionKey"
//dapr_app_id "http"
//add_scale_rule "http-rule" (ScaleRule.Http { ConcurrentRequests = 100 })

//containerApp {
//    name "servicebus"
//    active_revision_mode ActiveRevisionsMode.Single
//    docker_image containerRegistryDomain containerRegistry "servicebus" version
//    replicas 0 3
//    add_env_variable "ServiceBusQueueName" "wishrequests"
//    add_app_secret "ServiceBusConnectionKey"
//    add_scale_rule
//        "sb-keda-scale"
//        (ScaleRule.ServiceBus {
//            QueueName = "wishrequests"
//            MessageCount = 5
//            SecretRef = "servicebusconnectionkey" })
//}


#r "nuget:Azure.Storage.Queues"

open Azure.Storage.Queues

let queue = Azure.Storage.Queues.QueueClient("DefaultEndpointsProtocol=https;AccountName=isaacsuperstore;AccountKey=WreOm4w+5uI//c+3Xa1GWIc/Pw2g6guF56f/lCik259yOAP7ymlHPggMS4kODge05EleuiQpI48AHzUhbPU7DQ==;EndpointSuffix=core.windows.net", "myqueue", QueueClientOptions(MessageEncoding = QueueMessageEncoding.None))

for i in 1 .. 10000 do
    queue.SendMessageAsync $"test-{i}" |> ignore
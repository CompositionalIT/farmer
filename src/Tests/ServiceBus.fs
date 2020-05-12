module ServiceBus

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Arm.ServiceBus
open Microsoft.Azure.Management.ServiceBus
open Microsoft.Azure.Management.ServiceBus.Models
open Microsoft.Rest
open System

/// Client instance needed to get the serializer settings.
let dummyClient = new ServiceBusManagementClient (Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let tests = testList "Service Bus Tests" [
    test "Namespace is correctly created" {
        let sbNs =
            arm {
                add_resource (
                    serviceBus {
                        name "my-queue"
                        sku ServiceBusSku.Standard
                    })
            }
            |> findAzureResources<SBNamespace> dummyClient.SerializationSettings
            |> List.head

        sbNs.Validate()

        Expect.equal sbNs.Name "my-queue-ns" "Invalid namespace name"
        Expect.equal sbNs.Sku.Name SkuName.Standard "Invalid Sku"
    }

    test "Queue is correctly created" {
        let queue:SBQueue =
            serviceBus {
                name "my-queue"
                duplicate_detection_minutes 5
                enable_dead_letter_on_message_expiration
                enable_partition
                enable_session
                lock_duration_minutes 10
                max_delivery_count 3
            } |> convertResourceBuilder (fun (ns:{| resources:obj list |}) -> ns.resources.[0]) dummyClient.SerializationSettings

        Expect.equal queue.Name "my-queue" "Invalid queue name"
        Expect.isTrue (queue.RequiresDuplicateDetection.GetValueOrDefault false) "Duplicate detection should be enabled"
        Expect.equal queue.DuplicateDetectionHistoryTimeWindow (Nullable(TimeSpan(0, 5, 0))) "Duplicate detection window incorrect"
        Expect.isTrue (queue.DeadLetteringOnMessageExpiration.GetValueOrDefault false) "Dead lettering should be enabled"
        Expect.isTrue (queue.EnablePartitioning.GetValueOrDefault false) "Partitioning should be enabled"
        Expect.isTrue (queue.EnablePartitioning.GetValueOrDefault false) "Sessions should be enabled"
        Expect.equal queue.LockDuration (Nullable (TimeSpan(0, 10, 0))) "Lock duration incorrect"
        Expect.equal queue.MaxDeliveryCount (Nullable 3) "Max delivery count incorrect"
    }

    test "Correctly creates multiple queues" {
        let queueA = serviceBus { name "queue-a" }
        let queueB = serviceBus { name "queue-b"; link_to_namespace queueA }
        let deployment = arm { add_resource queueA; add_resource queueB }
        match deployment.Template.Resources with
        | [ :? Namespace as ns ] when ns.Queues.Length = 2 ->
            ()
        | _ ->
            failwith "Should have two queues in a single namespace."

    }
]
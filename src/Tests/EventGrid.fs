module EventGrid

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Arm
open Farmer.Arm.EventGrid
open Microsoft.Rest
open Newtonsoft.Json.Linq
open System

let tests =
    testList "Event Grid" [
        test "Creates topics correctly" {
            let b = eventGrid { topic_name "my-topic" } :> IBuilder
            let resources = b.BuildResources Location.WestEurope
            let t = resources.[0] :?> Topic
            Expect.equal t.Location Location.WestEurope "Incorrect location"
            Expect.equal t.Name (ResourceName "my-topic") "Incorrect name"
        }
        test "Defaults to resource group source" {
            let b = eventGrid { topic_name "my-topic" } :> IBuilder
            let resources = b.BuildResources Location.WestEurope
            let t = resources.[0] :?> Topic

            Expect.equal
                t.TopicType.ResourceType.Type
                (Arm.ResourceGroup.resourceGroups.Type)
                "Incorrect default topic type"

            Expect.equal t.Source (ResourceName "[resourceGroup().name]") "Incorrect default source name"
        }
        test "Creates a storage source correctly" {
            let storage = storageAccount { name "test" }

            let grid = eventGrid {
                topic_name "topic-test"
                source storage
            }

            Expect.equal grid.Source (ResourceName "test", Topics.StorageAccount) "Invalid Source"
        }
        test "Creates a function subscriber correctly" {
            let fnRef =
                Arm.Web.siteFunctions.resourceId (ResourceName "testFn", ResourceName "testHandler")
                |> Unmanaged

            let grid = eventGrid {
                add_function_subscriber
                    fnRef
                    {
                        MaxEventsPerBatch = 1u
                        PreferredBatchSizeInKilobytes = 64u
                    }
                    [ SystemEvents.Resources.ResourceWriteSuccess ]
            }

            let sub = grid.Subscriptions.[0]
            Expect.equal sub.Name (ResourceName "testFn-testHandler-fn") "Incorrect subscription name"

            Expect.equal
                sub.Endpoint
                (EndpointType.AzureFunction {
                    ResourceId = fnRef
                    MaxEventsPerBatch = 1u
                    PreferredBatchSizeInKilobytes = 64u
                })
                "Incorrect endpoint type"

            Expect.equal sub.SystemEvents [ SystemEvents.Resources.ResourceWriteSuccess ] "Incorrect system events"
        }
        test "Creates a queue subscriber correctly" {
            let storage = storageAccount { name "test" }

            let grid = eventGrid { add_queue_subscriber storage "thequeue" [ SystemEvents.Storage.BlobCreated ] }

            let sub = grid.Subscriptions.[0]
            Expect.equal sub.Name (ResourceName "test-thequeue-queue") "Incorrect subscription name"
            Expect.equal sub.Endpoint (EndpointType.StorageQueue(ResourceName "thequeue")) "Incorrect endpoint type"
            Expect.equal sub.Destination (ResourceName "test") "Incorrect destination"
            Expect.equal sub.SystemEvents [ SystemEvents.Storage.BlobCreated ] "Incorrect system events"
        }
        test "Creates a webhook subscriber correctly" {
            let app = webApp { name "test" }
            let grid = eventGrid { add_webhook_subscriber app "api/events" [] }
            let sub = grid.Subscriptions.[0]
            Expect.equal sub.Name (ResourceName "test-/api/events-webhook") "Incorrect subscription name"

            Expect.equal
                sub.Endpoint
                (EndpointType.WebHook(Uri "https://test.azurewebsites.net/api/events"))
                "Incorrect endpoint type"

            Expect.equal sub.Destination (ResourceName "test") "Incorrect destination"
        }
        test "Creates an eventhub subscriber correctly" {
            let hub = eventHub {
                name "hub"
                namespace_name "ns"
            }

            let grid = eventGrid { add_eventhub_subscriber hub [] }
            let sub = grid.Subscriptions.[0]
            Expect.equal sub.Name (ResourceName "ns-hub-eventhub") "Incorrect subscription name"
            Expect.equal sub.Endpoint (EndpointType.EventHub hub.Name) "Incorrect endpoint type"
            Expect.equal sub.Destination hub.EventHubNamespaceName "Incorrect destination"
        }
        test "Creates a service bus queue subscriber correctly" {
            let q = queue { name "queuequeue" }

            let bus = serviceBus {
                name "busbus"
                add_queues [ q ]
            }

            let grid = eventGrid { add_servicebus_queue_subscriber bus q [] }
            let sub = grid.Subscriptions.[0]
            Expect.equal sub.Name (ResourceName "queuequeue-busbus-servicebus-queue") "Incorrect subscription name"

            Expect.equal
                sub.Endpoint
                (EndpointType.ServiceBus(ServiceBusEndpointType.Queue { Queue = q.Name; Bus = bus.Name }))
                "Incorrect endpoint type"

            Expect.equal sub.Destination q.Name "Incorrect destination"
        }
        test "Creates a service bus topic subscriber correctly" {
            let t = topic { name "topictopic" }

            let bus = serviceBus {
                name "busbus"
                sku ServiceBus.Standard
                add_topics [ t ]
            }

            let grid = eventGrid { add_servicebus_topic_subscriber bus t [] }
            let sub = grid.Subscriptions.[0]
            Expect.equal sub.Name (ResourceName "topictopic-busbus-servicebus-topic") "Incorrect subscription name"

            Expect.equal
                sub.Endpoint
                (EndpointType.ServiceBus(ServiceBusEndpointType.Topic { Topic = t.Name; Bus = bus.Name }))
                "Incorrect endpoint type"

            Expect.equal sub.Destination t.Name "Incorrect destination"
        }
        test "Creates a monitor alert subscriber with ResourceId list correctly" {
            let actionGroupId =
                Arm.ActionGroups.actionGroups.resourceId (ResourceName "myActionGroup")

            let kv = keyVault { name "mykv" }

            let grid = eventGrid {
                topic_name "my-topic"
                source kv

                add_monitor_alert_subscriber [ actionGroupId ] MonitorAlertSeverity.Sev3 [
                    SystemEvents.KeyVault.SecretNearExpiry
                    SystemEvents.KeyVault.SecretExpired
                ]
            }

            let sub = grid.Subscriptions.[0]

            Expect.equal
                sub.Name
                (ResourceName "myActionGroup-myActionGroup-monitor-alert")
                "Incorrect subscription name"

            Expect.equal
                sub.Endpoint
                (EndpointType.MonitorAlert {
                    ActionGroups = [ actionGroupId ]
                    Severity = MonitorAlertSeverity.Sev3
                })
                "Incorrect endpoint type"
        }
        test "Creates a monitor alert subscriber with ActionGroupConfig correctly" {
            let ag = actionGroup {
                name "myActionGroup"
                short_name "myAG"
            }

            let kv = keyVault { name "mykv" }

            let grid = eventGrid {
                topic_name "my-topic"
                source kv
                add_monitor_alert_subscriber [ ag ] MonitorAlertSeverity.Sev2 [ SystemEvents.KeyVault.SecretExpired ]
            }

            let sub = grid.Subscriptions.[0]

            let expectedId =
                Arm.ActionGroups.actionGroups.resourceId (ResourceName "myActionGroup")

            Expect.equal
                sub.Endpoint
                (EndpointType.MonitorAlert {
                    ActionGroups = [ expectedId ]
                    Severity = MonitorAlertSeverity.Sev2
                })
                "Incorrect endpoint type"
        }
        test "Event delivery schema is set on all subscription ARM resources" {
            let kv = keyVault { name "mykv" }

            let actionGroupId =
                Arm.ActionGroups.actionGroups.resourceId (ResourceName "myActionGroup")

            let grid = eventGrid {
                topic_name "my-topic"
                source kv
                event_delivery_schema CloudEventSchemaV1_0

                add_monitor_alert_subscriber [ actionGroupId ] MonitorAlertSeverity.Sev3 [
                    SystemEvents.KeyVault.SecretNearExpiry
                ]
            }

            Expect.equal grid.EventDeliverySchema (Some CloudEventSchemaV1_0) "Incorrect delivery schema on config"

            let resources = (grid :> IBuilder).BuildResources Location.WestEurope

            let sub =
                resources
                |> List.pick (fun r ->
                    match r with
                    | :? Subscription<KeyVaultEvent> as s -> Some s
                    | _ -> None)

            Expect.equal sub.EventDeliverySchema (Some CloudEventSchemaV1_0) "Incorrect delivery schema on ARM resource"
        }
        test "Monitor alert subscriber generates correct JSON" {
            let kv = keyVault { name "mykv" }

            let actionGroupId =
                Arm.ActionGroups.actionGroups.resourceId (ResourceName "myActionGroup")

            let grid = eventGrid {
                topic_name "my-topic"
                source kv
                event_delivery_schema CloudEventSchemaV1_0

                add_monitor_alert_subscriber [ actionGroupId ] MonitorAlertSeverity.Sev3 [
                    SystemEvents.KeyVault.SecretNearExpiry
                    SystemEvents.KeyVault.SecretExpired
                ]
            }

            let json = (arm { add_resource grid }).Template |> Writer.toJson
            let jobj = JObject.Parse json

            let sub =
                jobj.SelectToken "resources[?(@.type=='Microsoft.EventGrid/systemTopics/eventSubscriptions')]"

            Expect.isNotNull sub "Event subscription resource not found"

            let endpointType = sub.SelectToken "properties.destination.endpointType"
            Expect.equal (endpointType.Value<string>()) "MonitorAlert" "Incorrect endpointType"

            let severity = sub.SelectToken "properties.destination.properties.severity"
            Expect.equal (severity.Value<string>()) "Sev3" "Incorrect severity"

            let schema = sub.SelectToken "properties.eventDeliverySchema"
            Expect.equal (schema.Value<string>()) "CloudEventSchemaV1_0" "Incorrect eventDeliverySchema"

            let actionGroups =
                sub.SelectToken "properties.destination.properties.actionGroups" :?> JArray

            Expect.isNotNull actionGroups "Action groups not found"
            Expect.equal actionGroups.Count 1 "Incorrect action group count"
        }
    ]
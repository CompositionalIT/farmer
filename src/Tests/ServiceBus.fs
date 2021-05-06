module ServiceBus

open Expecto
open Farmer
open Farmer.Arm.ServiceBus
open Farmer.Arm.ServiceBus.Namespaces.Topics
open Farmer.Builders
open Farmer.ServiceBus
open Namespaces
open Microsoft.Azure.Management.ServiceBus
open Microsoft.Azure.Management.ServiceBus.Models
open Microsoft.Rest
open System


let getResource<'T when 'T :> IArmResource> (data:IArmResource list) = data |> List.choose(function :? 'T as x -> Some x | _ -> None)
let getTopicResource = getResource<Topic>

let getResources (v:IBuilder) = v.BuildResources Location.WestUS

let getResourceDependsOnByName (template:Deployment) (resourceName:ResourceName) =
    let json = template.Template |> Writer.toJson
    let jobj = Newtonsoft.Json.Linq.JObject.Parse(json)
    let dependsOn = jobj.SelectToken($"resources[?(@.name=='{resourceName.Value}')].dependsOn")
    let jarray = dependsOn :?> Newtonsoft.Json.Linq.JArray
    [for jvalue in jarray do jvalue.ToString()]

/// Client instance needed to get the serializer settings.
let dummyClient = new ServiceBusManagementClient (Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")
let getResourceAtIndex o = o |> getResourceAtIndex dummyClient.SerializationSettings

let tests = testList "Service Bus Tests" [
    test "Namespace is correctly created" {
        let sbNs =
            arm {
                add_resource (
                    serviceBus {
                        name "serviceBus"
                        sku Standard
                    })
            }
            |> findAzureResources<SBNamespace> dummyClient.SerializationSettings
            |> List.head

        sbNs.Validate()

        Expect.equal sbNs.Name "serviceBus" "Invalid namespace name"
        Expect.equal sbNs.Sku.Name SkuName.Standard "Invalid Sku"
    }

    test "Namespace validation is respected" {
        Expect.throws(fun _ -> serviceBus { name "myns" } |> ignore) "Namespace length is too small"
        Expect.throws(fun _ -> serviceBus { name (String.replicate 51 "x") } |> ignore) "Namespace length is too long"
        Expect.throws(fun _ -> serviceBus { name "-abcdefghijk" } |> ignore) "Namespace starts with a dash"
        Expect.throws(fun _ -> serviceBus { name "abcdefghijk-" } |> ignore) "Namespace ends with a dash"
        Expect.throws(fun _ -> serviceBus { name "1abcdefghijk" } |> ignore) "Namespace starts with a number"
        Expect.throws(fun _ -> serviceBus { name "abcdefghijk-sb" } |> ignore) "Namespace ends with -sb"
        Expect.throws(fun _ -> serviceBus { name "abcdefghijk-mgmt" } |> ignore) "Namespace ends with management postifx"
        Expect.throws(fun _ -> serviceBus { name "c347834e-3f04-409c-b26b-c5ed702dea0b" } |> ignore) "Namespace is a guid"
    }

    testList "Queue Tests" [
        test "Queue is correctly created" {
            let queue =
                serviceBus {
                    name "my-bus"
                    sku ServiceBus.Standard
                    add_queues [
                        queue {
                            name "my-queue"
                            duplicate_detection_minutes 5
                            enable_dead_letter_on_message_expiration
                            enable_partition
                            enable_session
                            lock_duration_minutes 5
                            max_delivery_count 3
                            message_ttl 10<Days>
                        }
                    ]
                }
            let queue : SBQueue = queue |> getResourceAtIndex 1

            Expect.equal queue.Name "my-bus/my-queue" "Invalid queue name"
            Expect.isTrue (queue.RequiresDuplicateDetection.GetValueOrDefault false) "Duplicate detection should be enabled"
            Expect.equal queue.DuplicateDetectionHistoryTimeWindow (Nullable(TimeSpan(0, 5, 0))) "Duplicate detection window incorrect"
            Expect.isTrue (queue.DeadLetteringOnMessageExpiration.GetValueOrDefault false) "Dead lettering should be enabled"
            Expect.isTrue (queue.EnablePartitioning.GetValueOrDefault false) "Partitioning should be enabled"
            Expect.isTrue (queue.RequiresSession.GetValueOrDefault false) "Sessions should be enabled"
            Expect.equal queue.LockDuration (Nullable (TimeSpan(0, 5, 0))) "Lock duration incorrect"
            Expect.equal (queue.DefaultMessageTimeToLive.GetValueOrDefault TimeSpan.MinValue).TotalDays 10. "Default TTL incorrect"
            Expect.equal queue.MaxDeliveryCount (Nullable 3) "Max delivery count incorrect"
        }

        test "Cannot set duplicate detection on basic tier" {
            Expect.throws (fun () ->
                serviceBus {
                    name "serviceBus"
                    add_queues [
                        queue {
                            name "my-queue"
                            duplicate_detection_minutes 1
                        }
                    ]
                } |> ignore) "Duplicate detection isn't allowed on basic tier"
        }

        test "Cannot set lock duration more than 5 minutes" {
            Expect.throws (fun () ->
                serviceBus {
                    name "serviceBus"
                    add_queues [
                        queue {
                            name "my-queue"
                            lock_duration_minutes 6
                        }
                    ]
                } |> ignore) "Lock duration max should be 5 minutes"
        }

        test "Default TTL set for Basic queue" {
            let queue:SBQueue =
                serviceBus {
                    name "serviceBus"
                    add_queues [ queue { name "my-queue" } ]
                } |> getResourceAtIndex 1

            Expect.equal (queue.DefaultMessageTimeToLive.GetValueOrDefault TimeSpan.MinValue).TotalDays 14. "Default TTL should be 14 days"
        }

        test "Set TTL by timespan for Basic queue" {
            let queue:SBQueue =
                serviceBus {
                    name "serviceBus"
                    add_queues [
                        queue {
                            name "my-queue"
                            message_ttl "00:05:00"
                        }
                    ]
                } |> getResourceAtIndex 1

            Expect.equal (queue.DefaultMessageTimeToLive.GetValueOrDefault TimeSpan.MinValue).TotalMinutes 5. "TTL from TimeSpan should be 5 minutes"
        }

        test "Default TTL set for Standard queue" {
            let queue:SBQueue =
                serviceBus {
                    name "serviceBus"
                    sku ServiceBus.Standard
                    add_queues [ queue { name "my-queue" } ]
                } |> getResourceAtIndex 1

            Expect.equal (queue.DefaultMessageTimeToLive.GetValueOrDefault TimeSpan.MinValue).TotalDays TimeSpan.MaxValue.TotalDays "Default TTL should be max value"
        }

        test "Correctly creates multiple queues" {
            let theBus = serviceBus {
                name "serviceBus"
                add_queues [
                    queue { name "queue-a" }
                    queue { name "queue-b" }
                ]
            }
            let deployment = arm { add_resource theBus }
            let queues = deployment.Template.Resources |> List.choose(function :? Queue as q -> Some q | _ -> None)
            Expect.hasLength queues 2 "Should have two queues in a single namespace."
        }
    ]

    testList "Topic Tests" [
        test "Can create a basic topic" {
            let topic:SBTopic =
                serviceBus {
                    name "my-bus"
                    add_topics [
                        topic {
                            name "my-topic"
                            duplicate_detection_minutes 3
                            message_ttl 2<Days>
                            enable_partition
                        }
                    ]
                } |> getResourceAtIndex 1
            Expect.equal topic.Name "my-bus/my-topic" "Name not set"
            Expect.equal topic.RequiresDuplicateDetection (Nullable true) "Duplicate detection not set"
            Expect.equal topic.DuplicateDetectionHistoryTimeWindow (Nullable (TimeSpan.FromMinutes 3.)) "Duplicate detection time not set"
            Expect.equal topic.DefaultMessageTimeToLive (Nullable (TimeSpan.FromDays 2.)) "Time to live not set"
            Expect.equal topic.EnablePartitioning (Nullable true) "Paritition not set"
        }
        test "Can create a basic subscription" {
            let sub:SBSubscription =
                serviceBus {
                    name "my-bus"
                    add_topics [
                        topic {
                            name "my-topic"
                            add_subscriptions [
                                subscription { name "my-sub" }
                            ]
                        }
                    ]
                } |> getResourceAtIndex 2
            Expect.equal sub.Name "my-bus/my-topic/my-sub" "Name not set"
        }
        test "Creates a correlation filter rule" {
            let correlationRule =
                ServiceBus.CorrelationFilter(
                    ResourceName "CompletedStatus",
                    Some "xyz",
                    Map [
                        "Status", "Completed"
                        "Operation", "DoStuff"
                    ])
            let builtCorrelationRule = Rule.CreateCorrelationFilter("CompletedStatus", [ "Status", "Completed"; "Operation", "DoStuff" ], "xyz")
            Expect.equal correlationRule builtCorrelationRule "Built incorrect correlation filter"
        }
        test "Can create a subscription with different filters" {
            let sb =
                serviceBus {
                    name "my-bus"
                    sku Standard
                    add_topics [
                        topic {
                            name "my-topic"
                            add_subscriptions [
                                subscription {
                                    name "my-sub"
                                    add_filters [
                                        Rule.CreateCorrelationFilter("SuccessfulStatus", ["Status", "Success"])
                                        Rule.CreateSqlFilter("Thing", "Status = Success")
                                    ]
                                    add_correlation_filter "FailedStatus" [ "Status", "Fail" ]
                                    add_sql_filter "OtherSqlThing" "Status = Failed"
                                }
                            ]
                        }
                    ]
                }
            let template =
                arm {
                    location Location.EastUS
                    add_resource sb
                }
            let generatedTemplate = template.Template
            let genSubscription = generatedTemplate.Resources.Item 2 :?> Subscription
            Expect.hasLength genSubscription.Rules 4 "Expected subscription should have 4 rules"
            Expect.equal genSubscription.Rules.[0] (Rule.CreateCorrelationFilter("SuccessfulStatus", ["Status", "Success"])) "Rule 0 is incorrect"
            Expect.equal genSubscription.Rules.[1] (Rule.CreateSqlFilter("Thing", "Status = Success")) "Rule 1 is incorrect"
            Expect.equal genSubscription.Rules.[2] (Rule.CreateCorrelationFilter("FailedStatus", ["Status", "Fail"])) "Rule 2 is incorrect"
            Expect.equal genSubscription.Rules.[3] (Rule.CreateSqlFilter("OtherSqlThing", "Status = Failed")) "Rule 3 is incorrect"
        }
        test "Same subscription in different topic is ok" {
            let myServiceBus =
                let makeTopic topicName = topic {
                    name topicName
                    add_subscriptions [ subscription { name "debug" } ]
                }
                serviceBus {
                    name "mynamespace"
                    add_topics [
                        makeTopic "topicA"
                        makeTopic "topicB"
                    ]
                }

            let subscriptions =
                arm { add_resource myServiceBus }
                |> findAzureResources<SBSubscription> dummyClient.SerializationSettings
                |> List.filter(fun s -> s.Name.Contains "debug")

            Expect.hasLength subscriptions 2 "Subscription length"
            Expect.hasLength subscriptions 2 "Subscription length"
        }
        test "Topic does not create dependencies for unmanaged linked resources" {
            let resource =
                topic {
                    name "my-topic"
                    link_to_unmanaged_namespace "my-bus"
                }
                |> getResources |> getTopicResource |> List.head
            Expect.isEmpty resource.Dependencies ""
        }
        test "Topic creates dependencies for managed linked resources" {
            let resource =
                serviceBus {
                    name "my-bus"
                    add_topics [
                        topic {
                            name "my-topic"
                            link_to_unmanaged_namespace "my-namespace"
                        }        
                    ]
                }
                |> getResources |> getTopicResource |> List.head
            Expect.containsAll resource.Dependencies [
                ResourceId.create(namespaces, ResourceName "my-bus");]
                ""
        }
        test "Topic creates empty dependsOn in arm template json for unmanaged linked resources" {
            let template =
                arm {
                    add_resources [
                        topic {
                            name "my-topic"
                            link_to_unmanaged_namespace "my-bus"
                        }
                    ]
                }
            let dependsOn = getResourceDependsOnByName template (ResourceName "my-bus/my-topic")    
            Expect.hasLength dependsOn 0 ""
        }
        test "Topic creates dependsOn in arm template json for managed linked resources" {
            let template =
                arm {
                    add_resources [
                        serviceBus {
                            name "my-bus"
                            add_topics [
                                topic {
                                    name "my-topic"
                                    link_to_unmanaged_namespace "my-namespace"
                                }        
                            ]
                        }
                    ]
                }
            let dependsOn = getResourceDependsOnByName template (ResourceName "my-bus/my-topic")
            Expect.hasLength dependsOn 1 ""
            let expectedNamespaceDependency = "[resourceId('Microsoft.ServiceBus/namespaces', 'my-bus')]"
            Expect.equal dependsOn.Head expectedNamespaceDependency "" 
        }
        test "Topic IBuilder has correct resourceId for unmanaged namespace" {
            let resource = 
                topic {
                    name "my-topic"
                    link_to_unmanaged_namespace "my-bus"
                } :> IBuilder
            Expect.equal (resource.ResourceId.Eval()) "[resourceId('Microsoft.ServiceBus/namespaces/topics', 'my-bus', 'my-topic')]" ""
        }
        test "Topic IArmResource has correct resourceId for unmanaged namespace" {
            let resource = 
                topic {
                    name "my-topic"
                    link_to_unmanaged_namespace "my-bus"
                }
                |> getResources |> getTopicResource |> List.head :> IArmResource
            Expect.equal (resource.ResourceId.Eval()) "[resourceId('Microsoft.ServiceBus/namespaces/topics', 'my-bus', 'my-topic')]" ""
        }
        test "Topic IBuilder has correct resourceId for managed namespace" {
            let topicName = "my-topic"
            let svcBus =
                serviceBus {
                    name "my-bus"
                    add_topics [
                        topic {
                            name topicName
                            link_to_unmanaged_namespace "other-namespace"
                        }        
                    ]
                }
            let topicBuilder = svcBus.Topics |> Map.find (ResourceName topicName) :> IBuilder
            Expect.equal (topicBuilder.ResourceId.Eval()) $"[resourceId('Microsoft.ServiceBus/namespaces/topics', 'my-bus', '{topicName}')]" ""
        }
        test "Topic IArmResource has correct resourceId for managed namespace" {
            let topicName = "my-topic"
            let resource =
                serviceBus {
                    name "my-bus"
                    add_topics [
                        topic {
                            name topicName
                            link_to_unmanaged_namespace "other-namespace"
                        }        
                    ]
                }
                |> getResources |> getTopicResource |> List.head :> IArmResource
            Expect.equal (resource.ResourceId.Eval()) $"[resourceId('Microsoft.ServiceBus/namespaces/topics', 'my-bus', '{topicName}')]" ""
        }
    ]
]
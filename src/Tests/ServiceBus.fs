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


let getResource<'T when 'T :> IArmResource> (data: IArmResource list) =
    data
    |> List.choose (function
        | :? 'T as x -> Some x
        | _ -> None)

let getTopicResource = getResource<Topic>

let getResources (v: IBuilder) = v.BuildResources Location.WestUS

let getResourceDependsOnByName (template: IDeploymentSource) (resourceName: ResourceName) =
    let json = template.Deployment.Template |> Writer.toJson
    let jobj = Newtonsoft.Json.Linq.JObject.Parse(json)

    let dependsOn =
        jobj.SelectToken($"resources[?(@.name=='{resourceName.Value}')].dependsOn")

    let jarray = dependsOn :?> Newtonsoft.Json.Linq.JArray

    [
        for jvalue in jarray do
            jvalue.ToString()
    ]

/// Client instance needed to get the serializer settings.
let dummyClient =
    new ServiceBusManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let getResourceAtIndex o =
    o |> getResourceAtIndex dummyClient.SerializationSettings

let parseTemplate (arm: ResourceGroupConfig) =
    let json = arm.Template |> Writer.toJson
    Newtonsoft.Json.Linq.JObject.Parse(json)

let tests =
    testList
        "Service Bus Tests"
        [
            test "Namespace is correctly created" {
                let sbNs =
                    arm {
                        add_resource (
                            serviceBus {
                                name "serviceBus"
                                sku Standard
                            }
                        )
                    }
                    |> findAzureResources<SBNamespace> dummyClient.SerializationSettings
                    |> List.head

                sbNs.Validate()

                Expect.equal sbNs.Name "serviceBus" "Invalid namespace name"
                Expect.equal sbNs.Sku.Name SkuName.Standard "Invalid Sku"
            }

            test "Namespace validation is respected" {
                Expect.throws (fun _ -> serviceBus { name "myns" } |> ignore) "Namespace length is too small"

                Expect.throws
                    (fun _ -> serviceBus { name (String.replicate 51 "x") } |> ignore)
                    "Namespace length is too long"

                Expect.throws (fun _ -> serviceBus { name "-abcdefghijk" } |> ignore) "Namespace starts with a dash"
                Expect.throws (fun _ -> serviceBus { name "abcdefghijk-" } |> ignore) "Namespace ends with a dash"
                Expect.throws (fun _ -> serviceBus { name "1abcdefghijk" } |> ignore) "Namespace starts with a number"
                Expect.throws (fun _ -> serviceBus { name "abcdefghijk-sb" } |> ignore) "Namespace ends with -sb"

                Expect.throws
                    (fun _ -> serviceBus { name "abcdefghijk-mgmt" } |> ignore)
                    "Namespace ends with management postifx"

                Expect.throws
                    (fun _ -> serviceBus { name "c347834e-3f04-409c-b26b-c5ed702dea0b" } |> ignore)
                    "Namespace is a guid"
            }

            test "Public network access can be disabled" {
                let resourceGroup =
                    arm {
                        add_resource (
                            serviceBus {
                                name "serviceBus"
                                sku Standard
                                disable_public_network_access
                            }
                        )
                    }

                let jobj = parseTemplate resourceGroup

                Expect.equal
                    (jobj.SelectToken($"resources[0].properties.publicNetworkAccess").ToString())
                    "Disabled"
                    "Public network access should be disabled"
            }

            test "Public network access can be toggled" {
                let resourceGroup =
                    arm {
                        add_resource (
                            serviceBus {
                                name "serviceBus"
                                sku Standard
                                disable_public_network_access
                                disable_public_network_access FeatureFlag.Disabled
                            }
                        )
                    }

                let jobj = parseTemplate resourceGroup

                Expect.equal
                    (jobj.SelectToken($"resources[0].properties.publicNetworkAccess").ToString())
                    "Enabled"
                    "Public network access should be enabled"
            }

            test "Zone redundancy can be enabled" {
                let resourceGroup =
                    arm {
                        add_resource (
                            serviceBus {
                                name "serviceBus"
                                sku (Premium MessagingUnits.OneUnit)
                                enable_zone_redundancy
                            }
                        )
                    }

                let jobj = parseTemplate resourceGroup

                Expect.equal
                    (jobj.SelectToken($"resources[0].properties.zoneRedundant").ToString())
                    "true"
                    "Zone redundancy should be enabled"
            }

            test "Zone redundancy can be toggled" {
                let resourceGroup =
                    arm {
                        add_resource (
                            serviceBus {
                                name "serviceBus"
                                sku (Premium MessagingUnits.OneUnit)
                                enable_zone_redundancy
                                enable_zone_redundancy FeatureFlag.Disabled
                            }
                        )
                    }

                let jobj = parseTemplate resourceGroup

                Expect.equal
                    (jobj.SelectToken($"resources[0].properties.zoneRedundant").ToString())
                    "false"
                    "Zone redundancy should be disabled"
            }

            test "Zone redundancy cannot be set against standard SKU namespace" {
                Expect.throws
                    (fun () ->
                        serviceBus {
                            name "serviceBus"
                            sku Standard
                            enable_zone_redundancy
                        }
                        |> ignore)
                    "Zone redundancy can only be enabled against premium service bus namespaces"
            }

            test "Min TLS version can be set" {
                let resourceGroup =
                    arm {
                        add_resource (
                            serviceBus {
                                name "serviceBus"
                                sku Standard
                                min_tls_version TlsVersion.Tls12
                            }
                        )
                    }

                let jobj = parseTemplate resourceGroup

                Expect.equal
                    (jobj.SelectToken($"resources[0].properties.minimumTlsVersion").ToString())
                    "1.2"
                    "Min TLS should be 1.2"
            }

            testList
                "Queue Tests"
                [
                    test "Queue is correctly created" {
                        let queue =
                            serviceBus {
                                name "my-bus"
                                sku ServiceBus.Standard

                                add_queues
                                    [
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

                        let queue: SBQueue = queue |> getResourceAtIndex 1

                        Expect.equal queue.Name "my-bus/my-queue" "Invalid queue name"

                        Expect.isTrue
                            (queue.RequiresDuplicateDetection.GetValueOrDefault false)
                            "Duplicate detection should be enabled"

                        Expect.equal
                            queue.DuplicateDetectionHistoryTimeWindow
                            (Nullable(TimeSpan(0, 5, 0)))
                            "Duplicate detection window incorrect"

                        Expect.isTrue
                            (queue.DeadLetteringOnMessageExpiration.GetValueOrDefault false)
                            "Dead lettering should be enabled"

                        Expect.isTrue
                            (queue.EnablePartitioning.GetValueOrDefault false)
                            "Partitioning should be enabled"

                        Expect.isTrue (queue.RequiresSession.GetValueOrDefault false) "Sessions should be enabled"
                        Expect.equal queue.LockDuration (Nullable(TimeSpan(0, 5, 0))) "Lock duration incorrect"

                        Expect.equal
                            (queue.DefaultMessageTimeToLive.GetValueOrDefault TimeSpan.MinValue).TotalDays
                            10.
                            "Default TTL incorrect"

                        Expect.equal queue.MaxDeliveryCount (Nullable 3) "Max delivery count incorrect"
                    }
                    test "Can set duplicate dection from a TimeSpan" {
                        let queue =
                            serviceBus {
                                name "my-bus"
                                sku ServiceBus.Standard

                                add_queues
                                    [
                                        queue {
                                            name "my-queue"
                                            duplicate_detection (TimeSpan.FromSeconds(900.))
                                        }
                                    ]
                            }

                        let queue: SBQueue = queue |> getResourceAtIndex 1

                        Expect.isTrue
                            (queue.RequiresDuplicateDetection.GetValueOrDefault false)
                            "Duplicate detection should be enabled"

                        Expect.equal
                            queue.DuplicateDetectionHistoryTimeWindow
                            (Nullable(TimeSpan(0, 15, 0)))
                            "Duplicate detection window incorrect"
                    }
                    test "Can set duplicate detection to None" {
                        let queue =
                            serviceBus {
                                name "my-bus"
                                sku ServiceBus.Standard

                                add_queues
                                    [
                                        queue {
                                            name "my-queue"
                                            duplicate_detection None
                                        }
                                    ]
                            }

                        let queue: SBQueue = queue |> getResourceAtIndex 1

                        Expect.equal queue.RequiresDuplicateDetection (Nullable()) "Duplicate detection should be null"

                        Expect.equal
                            queue.DuplicateDetectionHistoryTimeWindow
                            (Nullable())
                            "Duplicate detection window incorrect"
                    }

                    test "Cannot set duplicate detection on basic tier" {
                        Expect.throws
                            (fun () ->
                                serviceBus {
                                    name "serviceBus"

                                    add_queues
                                        [
                                            queue {
                                                name "my-queue"
                                                duplicate_detection_minutes 1
                                            }
                                        ]
                                }
                                |> ignore)
                            "Duplicate detection isn't allowed on basic tier"
                    }

                    test "Cannot set lock duration more than 5 minutes" {
                        Expect.throws
                            (fun () ->
                                serviceBus {
                                    name "serviceBus"

                                    add_queues
                                        [
                                            queue {
                                                name "my-queue"
                                                lock_duration_minutes 6
                                            }
                                        ]
                                }
                                |> ignore)
                            "Lock duration max should be 5 minutes"
                    }

                    test "Default TTL set for Basic queue" {
                        let queue: SBQueue =
                            serviceBus {
                                name "serviceBus"
                                add_queues [ queue { name "my-queue" } ]
                            }
                            |> getResourceAtIndex 1

                        Expect.isNone
                            (Option.ofNullable queue.DefaultMessageTimeToLive)
                            "The default TTL should be null"
                    }

                    test "Set TTL by timespan for Basic queue" {
                        let queue: SBQueue =
                            serviceBus {
                                name "serviceBus"

                                add_queues
                                    [
                                        queue {
                                            name "my-queue"
                                            message_ttl "00:05:00"
                                        }
                                    ]
                            }
                            |> getResourceAtIndex 1

                        Expect.equal
                            (queue.DefaultMessageTimeToLive.GetValueOrDefault TimeSpan.MinValue)
                                .TotalMinutes
                            5.
                            "TTL from TimeSpan should be 5 minutes"
                    }

                    test "Default TTL set for Standard queue" {
                        let queue: SBQueue =
                            serviceBus {
                                name "serviceBus"
                                sku ServiceBus.Standard
                                add_queues [ queue { name "my-queue" } ]
                            }
                            |> getResourceAtIndex 1

                        Expect.isNone (Option.ofNullable queue.DefaultMessageTimeToLive) "Default TTL should be null"
                    }

                    test "Max size set for queue" {
                        let queue: SBQueue =
                            serviceBus {
                                name "serviceBus"

                                add_queues
                                    [
                                        queue {
                                            name "my-queue"
                                            max_queue_size 10240<Mb>
                                        }
                                    ]
                            }
                            |> getResourceAtIndex 1

                        Expect.equal queue.MaxSizeInMegabytes (Nullable 10240) "Incorrect max queue size"
                    }

                    test "Correctly creates multiple queues" {
                        let theBus =
                            serviceBus {
                                name "serviceBus"
                                add_queues [ queue { name "queue-a" }; queue { name "queue-b" } ]
                            }

                        let deployment = arm { add_resource theBus }

                        let queues =
                            deployment.Template.Resources
                            |> List.choose (function
                                | :? Queue as q -> Some q
                                | _ -> None)

                        Expect.hasLength queues 2 "Should have two queues in a single namespace."
                    }

                    test "No authorization rule by default" {
                        let sbAuthorizationRules =
                            arm {
                                add_resource (
                                    serviceBus {
                                        name "serviceBus"
                                        sku Standard
                                        add_queues [ queue { name "my-queue" } ]
                                    }
                                )
                            }
                            |> findAzureResources<SBAuthorizationRule> dummyClient.SerializationSettings
                            |> List.filter (fun x -> (=) x.Type queueAuthorizationRules.Type)

                        Expect.hasLength sbAuthorizationRules 0 "Should not have authorization rule by default"
                    }

                    test "Authorization Rule writes correct template" {
                        let thing =
                            arm {
                                add_resource (
                                    serviceBus {
                                        name "serviceBus"
                                        sku Standard

                                        add_queues
                                            [
                                                queue {
                                                    name "my-queue"
                                                    add_authorization_rule "my-rule" [ Manage ]
                                                }
                                            ]
                                    }
                                )
                            }
                            |> findAzureResources<SBAuthorizationRule> dummyClient.SerializationSettings

                        let sbAuthorizationRule =
                            thing
                            |> List.filter (fun x -> (=) x.Type queueAuthorizationRules.Type)
                            |> List.head

                        Expect.equal sbAuthorizationRule.Name "serviceBus/my-queue/my-rule" "Name is wrong"
                        Expect.equal sbAuthorizationRule.Rights.Count 1 "Wrong number of rights"
                        Expect.equal sbAuthorizationRule.Rights.[0] AccessRights.Manage "Wrong rights"
                    }

                    test "Queue IArmResource has correct resourceId for unmanaged namespace" {
                        let resource =
                            queue {
                                name "my-queue"
                                link_to_unmanaged_namespace "my-bus"
                            }
                            |> getResources
                            |> getResource
                            |> List.head
                            :> IArmResource

                        Expect.equal
                            (resource.ResourceId.Eval())
                            "[resourceId('Microsoft.ServiceBus/namespaces/queues', 'my-bus', 'my-queue')]"
                            ""
                    }
                ]

            testList
                "Topic Tests"
                [
                    test "Can create a basic topic" {
                        let topic: SBTopic =
                            serviceBus {
                                name "my-bus"

                                add_topics
                                    [
                                        topic {
                                            name "my-topic"
                                            duplicate_detection_minutes 3
                                            message_ttl 2<Days>
                                            enable_partition
                                        }
                                    ]
                            }
                            |> getResourceAtIndex 1

                        Expect.equal topic.Name "my-bus/my-topic" "Name not set"
                        Expect.equal topic.RequiresDuplicateDetection (Nullable true) "Duplicate detection not set"

                        Expect.equal
                            topic.DuplicateDetectionHistoryTimeWindow
                            (Nullable(TimeSpan.FromMinutes 3.))
                            "Duplicate detection time not set"

                        Expect.equal
                            topic.DefaultMessageTimeToLive
                            (Nullable(TimeSpan.FromDays 2.))
                            "Time to live not set"

                        Expect.equal topic.EnablePartitioning (Nullable true) "Paritition not set"
                    }
                    test "Can set duplicate detection to None" {
                        let topic: SBTopic =
                            serviceBus {
                                name "my-bus"

                                add_topics
                                    [
                                        topic {
                                            name "my-topic"
                                            duplicate_detection None
                                        }
                                    ]
                            }
                            |> getResourceAtIndex 1

                        Expect.equal topic.Name "my-bus/my-topic" "Name not set"
                        Expect.equal topic.RequiresDuplicateDetection (Nullable()) "Duplicate detection set"

                        Expect.equal
                            topic.DuplicateDetectionHistoryTimeWindow
                            (Nullable())
                            "Duplicate detection time not null"
                    }
                    test "Can set duplicate using a timespan" {
                        let topic: SBTopic =
                            serviceBus {
                                name "my-bus"

                                add_topics
                                    [
                                        topic {
                                            name "my-topic"
                                            duplicate_detection (TimeSpan.FromSeconds(900.))
                                        }
                                    ]
                            }
                            |> getResourceAtIndex 1

                        Expect.equal topic.Name "my-bus/my-topic" "Name not set"
                        Expect.equal topic.RequiresDuplicateDetection (Nullable true) "Duplicate detection not set"

                        Expect.equal
                            topic.DuplicateDetectionHistoryTimeWindow
                            (Nullable(TimeSpan.FromMinutes 15.))
                            "Duplicate detection time incorrect"
                    }
                    test "Can create a topic with a max message size" {
                        let topic: SBTopic =
                            serviceBus {
                                name "my-bus"

                                add_topics
                                    [
                                        topic {
                                            name "my-topic"
                                            max_message_size 1024<Kb>
                                        }
                                    ]
                            }
                            |> getResourceAtIndex 1

                        Expect.equal topic.Name "my-bus/my-topic" "Name not set"
                        Expect.equal topic.MaxMessageSizeInKilobytes (Nullable 1024) "Max message size not set"
                    }
                    test "Can create a topic with a max size" {
                        let topic: SBTopic =
                            serviceBus {
                                name "my-bus"

                                add_topics
                                    [
                                        topic {
                                            name "my-topic"
                                            max_topic_size 10240<Mb>
                                        }
                                    ]
                            }
                            |> getResourceAtIndex 1

                        Expect.equal topic.Name "my-bus/my-topic" "Name not set"
                        Expect.equal topic.MaxSizeInMegabytes (Nullable 10240) "Max size not set"
                    }
                    test "Can create a basic subscription" {
                        let sub: SBSubscription =
                            serviceBus {
                                name "my-bus"

                                add_topics
                                    [
                                        topic {
                                            name "my-topic"
                                            add_subscriptions [ subscription { name "my-sub" } ]
                                        }
                                    ]
                            }
                            |> getResourceAtIndex 2

                        Expect.equal sub.Name "my-bus/my-topic/my-sub" "Name not set"
                    }
                    test "Can create a forwarding subscription" {
                        let sub: SBSubscription =
                            serviceBus {
                                name "my-bus"

                                add_topics
                                    [
                                        topic {
                                            name "my-topic"

                                            add_subscriptions
                                                [
                                                    subscription {
                                                        name "my-sub"
                                                        forward_to "my-other-topic"
                                                    }
                                                ]
                                        }
                                        topic { name "my-other-topic" }
                                    ]
                            }
                            |> getResourceAtIndex 3

                        Expect.equal sub.ForwardTo "my-other-topic" "ForwardTo not set"
                    }
                    test "Can create a subscription with a message ttl" {
                        let sub: SBSubscription =
                            serviceBus {
                                name "my-bus"

                                add_topics
                                    [
                                        topic {
                                            name "my-topic"

                                            add_subscriptions
                                                [
                                                    subscription {
                                                        name "my-sub"
                                                        message_ttl (TimeSpan.FromHours 2.)
                                                    }
                                                ]
                                        }
                                    ]
                            }
                            |> getResourceAtIndex 2

                        Expect.equal sub.DefaultMessageTimeToLive (Nullable(TimeSpan.FromHours 2.)) "TTL not set"
                    }
                    test "Creates a correlation filter rule" {
                        let correlationRule =
                            ServiceBus.CorrelationFilter(
                                ResourceName "CompletedStatus",
                                Some "xyz",
                                Map [ "Status", "Completed"; "Operation", "DoStuff" ]
                            )

                        let builtCorrelationRule =
                            Rule.CreateCorrelationFilter(
                                "CompletedStatus",
                                [ "Status", "Completed"; "Operation", "DoStuff" ],
                                "xyz"
                            )

                        Expect.equal correlationRule builtCorrelationRule "Built incorrect correlation filter"
                    }
                    test "Can create a subscription with different filters" {
                        let sb =
                            serviceBus {
                                name "my-bus"
                                sku Standard

                                add_topics
                                    [
                                        topic {
                                            name "my-topic"

                                            add_subscriptions
                                                [
                                                    subscription {
                                                        name "my-sub"

                                                        add_filters
                                                            [
                                                                Rule.CreateCorrelationFilter(
                                                                    "SuccessfulStatus",
                                                                    [ "Status", "Success" ]
                                                                )
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

                        Expect.equal
                            genSubscription.Rules.[0]
                            (Rule.CreateCorrelationFilter("SuccessfulStatus", [ "Status", "Success" ]))
                            "Rule 0 is incorrect"

                        Expect.equal
                            genSubscription.Rules.[1]
                            (Rule.CreateSqlFilter("Thing", "Status = Success"))
                            "Rule 1 is incorrect"

                        Expect.equal
                            genSubscription.Rules.[2]
                            (Rule.CreateCorrelationFilter("FailedStatus", [ "Status", "Fail" ]))
                            "Rule 2 is incorrect"

                        Expect.equal
                            genSubscription.Rules.[3]
                            (Rule.CreateSqlFilter("OtherSqlThing", "Status = Failed"))
                            "Rule 3 is incorrect"
                    }
                    test "Same subscription in different topic is ok" {
                        let myServiceBus =
                            let makeTopic topicName =
                                topic {
                                    name topicName
                                    add_subscriptions [ subscription { name "debug" } ]
                                }

                            serviceBus {
                                name "mynamespace"
                                add_topics [ makeTopic "topicA"; makeTopic "topicB" ]
                            }

                        let subscriptions =
                            arm { add_resource myServiceBus }
                            |> findAzureResources<SBSubscription> dummyClient.SerializationSettings
                            |> List.filter (fun s -> s.Name.Contains "debug")

                        Expect.hasLength subscriptions 2 "Subscription length"
                        Expect.hasLength subscriptions 2 "Subscription length"
                    }
                    test "Topic does not create dependencies for unmanaged linked resources" {
                        let resource =
                            topic {
                                name "my-topic"
                                link_to_unmanaged_namespace "my-bus"
                            }
                            |> getResources
                            |> getTopicResource
                            |> List.head

                        Expect.isEmpty resource.Dependencies ""
                    }
                    test "Topic creates dependencies for managed linked resources" {
                        let resource =
                            serviceBus {
                                name "my-bus"

                                add_topics
                                    [
                                        topic {
                                            name "my-topic"
                                            link_to_unmanaged_namespace "my-namespace"
                                        }
                                    ]
                            }
                            |> getResources
                            |> getTopicResource
                            |> List.head

                        Expect.containsAll
                            resource.Dependencies
                            [ ResourceId.create (namespaces, ResourceName "my-bus") ]
                            ""
                    }
                    test "Topic creates empty dependsOn in arm template json for unmanaged linked resources" {
                        let template =
                            arm {
                                add_resources
                                    [
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
                                add_resources
                                    [
                                        serviceBus {
                                            name "my-bus"

                                            add_topics
                                                [
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

                        let expectedNamespaceDependency =
                            "[resourceId('Microsoft.ServiceBus/namespaces', 'my-bus')]"

                        Expect.equal dependsOn.Head expectedNamespaceDependency ""
                    }
                    test "Topic IBuilder has correct resourceId for unmanaged namespace" {
                        let resource =
                            topic {
                                name "my-topic"
                                link_to_unmanaged_namespace "my-bus"
                            }
                            :> IBuilder

                        Expect.equal
                            (resource.ResourceId.Eval())
                            "[resourceId('Microsoft.ServiceBus/namespaces/topics', 'my-bus', 'my-topic')]"
                            ""
                    }
                    test "Topic IArmResource has correct resourceId for unmanaged namespace" {
                        let resource =
                            topic {
                                name "my-topic"
                                link_to_unmanaged_namespace "my-bus"
                            }
                            |> getResources
                            |> getTopicResource
                            |> List.head
                            :> IArmResource

                        Expect.equal
                            (resource.ResourceId.Eval())
                            "[resourceId('Microsoft.ServiceBus/namespaces/topics', 'my-bus', 'my-topic')]"
                            ""
                    }
                    test "Topic IBuilder has correct resourceId for managed namespace" {
                        let topicName = "my-topic"

                        let svcBus =
                            serviceBus {
                                name "my-bus"

                                add_topics
                                    [
                                        topic {
                                            name topicName
                                            link_to_unmanaged_namespace "other-namespace"
                                        }
                                    ]
                            }

                        let topicBuilder = svcBus.Topics |> Map.find (ResourceName topicName) :> IBuilder

                        Expect.equal
                            (topicBuilder.ResourceId.Eval())
                            $"[resourceId('Microsoft.ServiceBus/namespaces/topics', 'my-bus', '{topicName}')]"
                            ""
                    }
                    test "Topic IArmResource has correct resourceId for managed namespace" {
                        let topicName = "my-topic"

                        let resource =
                            serviceBus {
                                name "my-bus"

                                add_topics
                                    [
                                        topic {
                                            name topicName
                                            link_to_unmanaged_namespace "other-namespace"
                                        }
                                    ]
                            }
                            |> getResources
                            |> getTopicResource
                            |> List.head
                            :> IArmResource

                        Expect.equal
                            (resource.ResourceId.Eval())
                            $"[resourceId('Microsoft.ServiceBus/namespaces/topics', 'my-bus', '{topicName}')]"
                            ""
                    }
                ]

            testList
                "Namespace AuthorizationRule Tests"
                [
                    test "AuthorizationRule should not be present by default" {
                        let sbAuthorizationRules =
                            arm {
                                add_resource (
                                    serviceBus {
                                        name "serviceBus"
                                        sku Standard
                                    }
                                )
                            }
                            |> findAzureResources<SBAuthorizationRule> dummyClient.SerializationSettings
                            |> List.filter (fun x -> (=) x.Type namespaceAuthorizationRules.Type)

                        Expect.equal sbAuthorizationRules.Length 0 "AuthorizationRule should not be present"
                    }
                    test "AuthorizationRule should write correct ARM template" {
                        let sbAuthorizationRule =
                            arm {
                                add_resource (
                                    serviceBus {
                                        name "serviceBus"
                                        sku Standard
                                        add_authorization_rule "my-rule" [ Manage ]
                                    }
                                )
                            }
                            |> findAzureResources<SBAuthorizationRule> dummyClient.SerializationSettings
                            |> List.filter (fun x -> (=) x.Type namespaceAuthorizationRules.Type)
                            |> List.head

                        sbAuthorizationRule.Validate()

                        Expect.equal sbAuthorizationRule.Name "serviceBus/my-rule" "Wrong name"
                        Expect.equal sbAuthorizationRule.Rights.Count 1 "Wrong number of rights"
                        Expect.equal sbAuthorizationRule.Rights.[0] AccessRights.Manage "Wrong rights"
                    }
                ]
        ]

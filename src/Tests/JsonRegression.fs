module JsonRegression

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Arm
open System.IO

let tests =
    testList "ARM Writer Regression Tests" [
        let compareResourcesToJson (resources:IBuilder list) jsonFile =
            let template = arm {
                location Location.NorthEurope
                add_resources resources
            }

            let path = __SOURCE_DIRECTORY__ + "/test-data/" + jsonFile
            let expected = File.ReadAllText path
            let actual = template.Template |> Writer.toJson
            Expect.equal actual expected (sprintf "ARM template generation has changed! Either fix the writer, or update the contents of the generated file (%s)" path)

        test "Generates lots of resources" {
            let number = string 1979

            let sql = sqlServer { name ("farmersql" + number); admin_username "farmersqladmin"; add_databases [ sqlDb { name "farmertestdb"; use_encryption } ]; enable_azure_firewall }
            let storage = storageAccount { name ("farmerstorage" + number) }
            let web = webApp { name ("farmerwebapp" + number); add_extension WebApp.Extensions.Logging }
            let fns = functions { name ("farmerfuncs" + number) }
            let svcBus = serviceBus { name ("farmerbus" + number); sku ServiceBus.Sku.Standard; add_queues [ queue { name "queue1" } ]; add_topics [ topic { name "topic1"; add_subscriptions [ subscription { name "sub1" } ] } ] }
            let cdn = cdn { name ("farmercdn" + number); add_endpoints [ endpoint { name ("farmercdnendpoint" + number); origin storage.WebsitePrimaryEndpointHost } ] }
            let containerGroup = containerGroup { name ("farmeraci" + number); add_instances [ containerInstance { name "webserver"; image "nginx:latest"; add_ports ContainerGroup.PublicPort [ 80us ]; add_volume_mount "source-code" "/src/farmer" } ]; add_volumes [ volume_mount.git_repo "source-code" (System.Uri "https://github.com/CompositionalIT/farmer") ] }
            let cosmos = cosmosDb {
                name "testdb"
                account_name "testaccount"
                throughput 400<CosmosDb.RU>
                failover_policy CosmosDb.NoFailover
                consistency_policy (CosmosDb.BoundedStaleness(500, 1000))
                add_containers [
                    cosmosContainer {
                        name "myContainer"
                        partition_key [ "/id" ] CosmosDb.Hash
                        add_index "/path" [ CosmosDb.Number, CosmosDb.Hash ]
                        exclude_path "/excluded/*"
                    }
                ]
            }

            compareResourcesToJson
                [ sql; storage; web; fns; svcBus; cdn; containerGroup; cosmos ]
                "lots-of-resources.json"
        }

        test "VM regression test" {
            let myVm = vm {
                name "isaacsVM"
                username "isaac"
                vm_size Vm.Standard_A2
                operating_system Vm.WindowsServer_2012Datacenter
                os_disk 128 Vm.StandardSSD_LRS
                add_ssd_disk 128
                add_slow_disk 512
                diagnostics_support
            }

            compareResourcesToJson [ myVm ] "vm.json"
        }

        test "Storage, Event Hub, Log Analytics and Diagnostics" {
            let data = storageAccount { name "isaacsuperdata" }
            let hub = eventHub { name "isaacsuperhub" }
            let logs = logAnalytics { name "isaacsuperlogs" }
            let web = webApp { name "isaacdiagsuperweb"; app_insights_off }

            let mydiagnosticSetting = diagnosticSettings {
                name "myDiagnosticSetting"
                metrics_source web

                add_destination data
                add_destination logs
                add_destination hub
                loganalytics_output_type Farmer.DiagnosticSettings.Dedicated
                capture_metrics [ "AllMetrics" ]
                capture_logs [
                    Farmer.DiagnosticSettings.Logging.Web.Sites.AppServicePlatformLogs
                    Farmer.DiagnosticSettings.Logging.Web.Sites.AppServiceAntivirusScanAuditLogs
                    Farmer.DiagnosticSettings.Logging.Web.Sites.AppServiceAppLogs
                    Farmer.DiagnosticSettings.Logging.Web.Sites.AppServiceHTTPLogs
                ]
            }
            compareResourcesToJson [ data; web; hub; logs; mydiagnosticSetting ] "diagnostics.json"
        }

        test "Event Grid" {
            let storageSource = storageAccount {
                name "isaacgriddevprac"
                add_private_container "data"
                add_queue "todo"
            }

            let eventQueue = queue { name "events" }
            let sb = serviceBus {
                name "farmereventpubservicebusns"
                add_queues [ eventQueue ]
            }

            let eventHubGrid = eventGrid {
                topic_name "newblobscreated"
                source storageSource
                add_queue_subscriber storageSource "todo" [ SystemEvents.Storage.BlobCreated ]
                add_servicebus_queue_subscriber sb eventQueue [ SystemEvents.Storage.BlobCreated ]
            }

            compareResourcesToJson [ storageSource; sb; eventHubGrid ] "event-grid.json"
        }
    ]

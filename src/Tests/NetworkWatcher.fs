module NetworkWatcher

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Arm.Network
open Newtonsoft.Json.Linq

let tests =
    testList "Network Watcher" [
        test "Creates a basic Network Watcher" {
            let watcher = networkWatcher { name "my-network-watcher" }

            let deployment = arm { add_resources [ watcher ] }
            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse
            let watcherResource = jobj.SelectToken("resources[?(@.type=='Microsoft.Network/networkWatchers')]")

            Expect.isNotNull watcherResource "Network Watcher resource should exist"
            Expect.equal (watcherResource.SelectToken("name").ToString()) "my-network-watcher" "Name should be correct"
        }

        test "Creates a flow log with NSG and storage" {
            let nsg = nsg { name "my-nsg" }
            let storage = storageAccount { name "flowlogsstorage" }

            let watcher = networkWatcher { name "my-watcher" }

            let flowlog =
                flowLog {
                    name "my-flow-log"
                    link_to_network_watcher watcher
                    link_to_nsg (nsg :> IBuilder).ResourceId
                    link_to_storage_account (storage :> IBuilder).ResourceId
                    retention_days 30
                }

            let deployment = arm {
                add_resources [ nsg; storage; watcher; flowlog ]
            }

            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse
            let flowLogResource =
                jobj.SelectToken("resources[?(@.type=='Microsoft.Network/networkWatchers/flowLogs')]")

            Expect.isNotNull flowLogResource "Flow log resource should exist"
            Expect.equal
                (flowLogResource.SelectToken("properties.retentionPolicy.days").ToObject<int>())
                30
                "Retention days should be 30"
        }

        test "Flow log with Traffic Analytics" {
            let nsg = nsg { name "my-nsg" }
            let storage = storageAccount { name "flowlogsstorage" }
            let workspace = logAnalytics { name "my-workspace" }
            let watcher = networkWatcher { name "my-watcher" }

            let flowlog =
                flowLog {
                    name "my-flow-log"
                    link_to_network_watcher watcher
                    link_to_nsg (nsg :> IBuilder).ResourceId
                    link_to_storage_account (storage :> IBuilder).ResourceId
                    enable_traffic_analytics (workspace :> IBuilder).ResourceId
                }

            let deployment = arm {
                add_resources [ nsg; storage; workspace; watcher; flowlog ]
            }

            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse
            let flowLogResource =
                jobj.SelectToken("resources[?(@.type=='Microsoft.Network/networkWatchers/flowLogs')]")

            let trafficAnalytics =
                flowLogResource.SelectToken(
                    "properties.flowAnalyticsConfiguration.networkWatcherFlowAnalyticsConfiguration"
                )

            Expect.isNotNull trafficAnalytics "Traffic Analytics should be configured"

            Expect.isTrue
                (trafficAnalytics.SelectToken("enabled").ToObject<bool>())
                "Traffic Analytics should be enabled"
        }

        test "Network Watcher has correct resource ID" {
            let watcher = networkWatcher { name "test-watcher" }
            let resourceId = (watcher :> IBuilder).ResourceId

            Expect.equal resourceId.Type.Type "Microsoft.Network/networkWatchers" "Type should be correct"
            Expect.equal resourceId.Name.Value "test-watcher" "Name should be correct"
        }
    ]

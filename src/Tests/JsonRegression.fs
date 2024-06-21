module JsonRegression

open Expecto
open DiffPlex.DiffBuilder
open DiffPlex.DiffBuilder.Model
open Farmer
open Farmer.Arm
open Farmer.AzureFirewall
open Farmer.Builders
open Farmer.Network
open Farmer.ServiceBus
open System
open System.IO

let private differ = SideBySideDiffBuilder(DiffPlex.Differ())

/// Prints a diff of the changes only, along with some surrounding lines to help with context.
/// Had to work around some limitations in Expecto.Diff for now.
let focusedDiffPrinter (numSurroundingLines: int) expected actual =
    let actual, expected =
        match box actual, box expected with
        | (:? string as f), (:? string as s) -> string f, string s
        | f, s -> sprintf "%A" f, sprintf "%A" s

    let colouredText typ text =
        match typ with
        | ChangeType.Inserted -> Logging.ColourText.colouriseText ConsoleColor.Green text
        | ChangeType.Deleted -> Logging.ColourText.colouriseText ConsoleColor.Red text
        | ChangeType.Modified -> Logging.ColourText.colouriseText ConsoleColor.Blue text
        | ChangeType.Imaginary -> Logging.ColourText.colouriseText ConsoleColor.Yellow text
        | ChangeType.Unchanged
        | _ -> text

    let colouriseLine (line: DiffPiece) =
        if line.SubPieces.Count = 0 then
            colouredText line.Type line.Text
        else
            let colouredPieces =
                line.SubPieces |> Seq.map (fun piece -> colouredText piece.Type piece.Text)

            String.Join("", colouredPieces)

    let colourisedAndFocusedDiff (lines: ResizeArray<#DiffPiece>) =
        /// Gets a range of lines around any changes.
        let focusedLineIndices =
            let changedLineIndices =
                lines
                |> Seq.mapi (fun idx line -> if line.Type <> ChangeType.Unchanged then Some idx else None)
                |> Seq.choose id

            seq {
                for idx in changedLineIndices do
                    for i in idx - numSurroundingLines .. idx + numSurroundingLines do
                        if idx >= 0 && idx < lines.Count then
                            yield i
            }
            |> Set.ofSeq

        /// Includes only the focused lines, necessary on large diffs.
        let focusedLines =
            lines
            |> Seq.mapi (fun idx line ->
                if focusedLineIndices |> Set.contains idx then
                    Some $"{idx + 1}: {colouriseLine line}"
                else
                    None)
            |> Seq.choose id

        String.Join("\n", focusedLines)

    let diff = differ.BuildDiffModel(expected, actual)

    sprintf
        "\n%s---------- Expected: ------------------\n%s\n---------- Actual: --------------------\n%s\n"
        (Logging.ColourText.colouriseText ConsoleColor.White "") // Reset colour.
        (colourisedAndFocusedDiff diff.OldText.Lines) // OldText and NewText are reversed in Expecto.Diff
        (colourisedAndFocusedDiff diff.NewText.Lines)

let tests =
    testList
        "ARM Writer Regression Tests"
        [
            let compareDeploymentToJson (deployment: ResourceGroupConfig) jsonFile =
                let path = __SOURCE_DIRECTORY__ + "/test-data/" + jsonFile
                let expected = File.ReadAllText path
                let actual = deployment.Template |> Writer.toJson
                let filename = Writer.toFile (path + "out") "deployment" actual

                Expect.equalWithDiffPrinter
                    (focusedDiffPrinter 8)
                    (actual.Trim())
                    (expected.Trim())
                    (sprintf
                        "ARM template generation has changed! Either fix the writer, or update the contents of the generated file (%s)"
                        path)

            let compareResourcesToJson (resources: IBuilder list) jsonFile =
                let template =
                    arm {
                        location Location.NorthEurope
                        add_resources resources
                    }

                compareDeploymentToJson template jsonFile

            test "Generates lots of resources" {
                let number = string 1979

                let sql =
                    sqlServer {
                        name ("farmersql" + number)
                        admin_username "farmersqladmin"

                        add_databases
                            [
                                sqlDb {
                                    name "farmertestdb"
                                    use_encryption
                                }
                            ]

                        enable_azure_firewall
                    }

                let storage = storageAccount { name ("farmerstorage" + number) }

                let web =
                    webApp {
                        name ("farmerwebapp" + number)
                        add_extension WebApp.Extensions.Logging
                    }

                let fns =
                    functions {
                        name ("farmerfuncs" + number)
                        use_extension_version V1
                    }

                let svcBus =
                    serviceBus {
                        name ("farmerbus" + number)
                        sku ServiceBus.Sku.Standard
                        add_queues [ queue { name "queue1" } ]

                        add_topics
                            [
                                topic {
                                    name "topic1"
                                    add_subscriptions [ subscription { name "sub1" } ]
                                }
                            ]
                    }

                let cdn =
                    cdn {
                        name ("farmercdn" + number)

                        add_endpoints
                            [
                                endpoint {
                                    name ("farmercdnendpoint" + number)
                                    origin storage.WebsitePrimaryEndpointHost

                                    add_rule (
                                        cdnRule {
                                            name ("farmerrule" + number)
                                            order 1

                                            when_device_type
                                                DeliveryPolicy.EqualityOperator.Equals
                                                DeliveryPolicy.DeviceType.Mobile

                                            url_rewrite "/pattern" "/destination" true
                                        }
                                    )
                                }
                            ]
                    }

                let containerGroup =
                    containerGroup {
                        name ("farmeraci" + number)

                        add_instances
                            [
                                containerInstance {
                                    name "webserver"
                                    image "nginx:latest"
                                    add_ports ContainerGroup.PublicPort [ 80us ]
                                    add_volume_mount "source-code" "/src/farmer"
                                }
                            ]

                        add_volumes
                            [
                                volume_mount.git_repo
                                    "source-code"
                                    (System.Uri "https://github.com/CompositionalIT/farmer")
                            ]
                    }

                let vm =
                    vm {
                        name "farmervm"
                        username "farmer-admin"
                    }

                let dockerFunction =
                    functions {
                        name "docker-func"

                        publish_as (
                            DockerContainer
                                {
                                    Url = new Uri("http://www.farmer.io")
                                    User = "Robert Lewandowski"
                                    Password = SecureParameter "secure_pass_param"
                                    StartupCommand = "do it"
                                }
                        )

                        app_insights_off
                    }

                let cosmos =
                    cosmosDb {
                        name "testdb"
                        account_name "testaccount"
                        throughput 400<CosmosDb.RU>
                        failover_policy CosmosDb.NoFailover
                        consistency_policy (CosmosDb.BoundedStaleness(500, 1000))

                        add_containers
                            [
                                cosmosContainer {
                                    name "myContainer"
                                    partition_key [ "/id" ] CosmosDb.Hash
                                    add_index "/path" [ CosmosDb.Number, CosmosDb.Hash ]
                                    exclude_path "/excluded/*"
                                }
                            ]
                    }

                let cosmosMongo =
                    cosmosDb {
                        name "testdbmongo"
                        account_name "testaccountmongo"
                        kind Mongo
                        throughput 400<CosmosDb.RU>
                        failover_policy CosmosDb.NoFailover
                        consistency_policy (CosmosDb.BoundedStaleness(500, 1000))
                    }

                let nestedResourceGroup =
                    resourceGroup {
                        name "nested-resources"
                        deployment_name "nested-resources"
                        location Location.UKSouth
                        add_resources [ cosmos; cosmosMongo; vm ]
                    }

                let communicationServices =
                    communicationService {
                        name "test"
                        add_tags [ "a", "b" ]
                        data_location DataLocation.Australia
                    }

                compareResourcesToJson
                    [
                        sql
                        storage
                        web
                        fns
                        svcBus
                        cdn
                        containerGroup
                        communicationServices
                        nestedResourceGroup
                        dockerFunction
                    ]
                    "lots-of-resources.json"
            }

            test "VM regression test" {
                let myVm =
                    vm {
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

                let web =
                    webApp {
                        name "isaacdiagsuperweb"
                        app_insights_off
                    }

                let mydiagnosticSetting =
                    diagnosticSettings {
                        name "myDiagnosticSetting"
                        metrics_source web

                        add_destination data
                        add_destination logs
                        add_destination hub
                        loganalytics_output_type Farmer.DiagnosticSettings.Dedicated
                        capture_metrics [ "AllMetrics" ]

                        capture_logs
                            [
                                Farmer.DiagnosticSettings.Logging.Web.Sites.AppServicePlatformLogs
                                Farmer.DiagnosticSettings.Logging.Web.Sites.AppServiceAntivirusScanAuditLogs
                                Farmer.DiagnosticSettings.Logging.Web.Sites.AppServiceAppLogs
                                Farmer.DiagnosticSettings.Logging.Web.Sites.AppServiceHTTPLogs
                            ]
                    }

                compareResourcesToJson [ data; web; hub; logs; mydiagnosticSetting ] "diagnostics.json"
            }

            test "Event Grid" {
                let storageSource =
                    storageAccount {
                        name "isaacgriddevprac"
                        add_private_container "data"
                        add_queue "todo"
                    }

                let eventQueue = queue { name "events" }

                let sb =
                    serviceBus {
                        name "farmereventpubservicebusns"
                        add_queues [ eventQueue ]
                    }

                let eventHubGrid =
                    eventGrid {
                        topic_name "newblobscreated"
                        source storageSource
                        add_queue_subscriber storageSource "todo" [ SystemEvents.Storage.BlobCreated ]
                        add_servicebus_queue_subscriber sb eventQueue [ SystemEvents.Storage.BlobCreated ]
                    }

                compareResourcesToJson [ storageSource; sb; eventHubGrid ] "event-grid.json"
            }

            test "Can parse JSON into an ARM template" {
                let json =
                    """    {
      "apiVersion": "2019-06-01",
      "dependsOn": [],
      "kind": "StorageV2",
      "location": "northeurope",
      "name": "jsontest",
      "properties": {},
      "sku": {
        "name": "Standard_LRS"
      },
      "tags": {},
      "type": "Microsoft.Storage/storageAccounts"
    }
"""

                let resource =
                    arm { add_resource (Resource.ofJson json) } |> Storage.getStorageResource

                Expect.equal resource.Name "jsontest" "Account name is wrong"
                Expect.equal resource.Sku.Name "Standard_LRS" "SKU is wrong"
                Expect.equal resource.Kind "StorageV2" "Kind"
            }

            test "ServiceBus" {
                let svcBus =
                    serviceBus {
                        name "farmer-bus"
                        sku (ServiceBus.Sku.Premium MessagingUnits.OneUnit)
                        add_queues [ queue { name "queue1" } ]

                        add_topics
                            [
                                topic {
                                    name "topic1"

                                    add_subscriptions
                                        [
                                            subscription {
                                                name "sub1"

                                                add_filters
                                                    [
                                                        Rule.CreateCorrelationFilter(
                                                            "filter1",
                                                            [ "header1", "headervalue1" ]
                                                        )
                                                    ]
                                            }
                                        ]
                                }
                            ]
                    }

                let topicWithUnmanagedNamespace =
                    topic {
                        name "unmanaged-topic"
                        link_to_unmanaged_namespace "farmer-bus"

                        add_subscriptions
                            [
                                subscription {
                                    name "sub1"

                                    add_filters
                                        [ Rule.CreateCorrelationFilter("filter1", [ "header1", "headervalue1" ]) ]
                                }
                            ]
                    }

                compareResourcesToJson [ svcBus; topicWithUnmanagedNamespace ] "service-bus.json"
            }

            test "VirtualWan" {
                let vwan =
                    vwan {
                        name "farmer-vwan"
                        disable_vpn_encryption
                        allow_branch_to_branch_traffic
                        office_365_local_breakout_category Office365LocalBreakoutCategory.None
                        standard_vwan
                    }

                compareResourcesToJson [ vwan ] "virtual-wan.json"
            }

            test "LoadBalancer" {
                let mySubnet =
                    subnet {
                        name "my-subnet"
                        prefix "10.0.1.0/24"
                        add_delegations [ SubnetDelegationService.ContainerGroups ]
                    }

                let myVnet =
                    vnet {
                        name "my-vnet"
                        add_address_spaces [ "10.0.1.0/24" ]

                        add_subnets [ mySubnet ]
                    }


                let lb =
                    loadBalancer {
                        name "lb"
                        sku Farmer.LoadBalancer.Sku.Standard

                        add_frontends
                            [
                                frontend {
                                    name "lb-frontend"
                                    public_ip "lb-pip"
                                }
                            ]

                        add_backend_pools
                            [
                                backendAddressPool {
                                    name "lb-backend"
                                    subnet mySubnet
                                    add_ip_addresses [ "10.0.1.4"; "10.0.1.5" ]
                                }
                            ]

                        add_probes
                            [
                                loadBalancerProbe {
                                    name "httpGet"
                                    protocol Farmer.LoadBalancer.LoadBalancerProbeProtocol.HTTP
                                    port 8080
                                    request_path "/"
                                }
                            ]

                        add_rules
                            [
                                loadBalancingRule {
                                    name "rule1"
                                    frontend_ip_config "lb-frontend"
                                    backend_address_pool "lb-backend"
                                    frontend_port 80
                                    backend_port 8080
                                    protocol TransmissionProtocol.TCP
                                    probe "httpGet"
                                }
                            ]
                    }

                compareResourcesToJson [ mySubnet; lb ] "load-balancer.json"
            }

            test "AzureFirewall" {
                let vwan =
                    vwan {
                        name "farmer-vwan"
                        disable_vpn_encryption
                        allow_branch_to_branch_traffic
                        office_365_local_breakout_category Office365LocalBreakoutCategory.None
                        standard_vwan
                    }

                let vhub =
                    vhub {
                        name "farmer_vhub"
                        address_prefix (IPAddressCidr.parse "100.73.255.0/24")
                        link_to_vwan vwan
                    }

                let firewall =
                    azureFirewall {
                        name "farmer_firewall"
                        sku SkuName.AZFW_Hub SkuTier.Standard
                        public_ip_reservation_count 2
                        link_to_vhub vhub
                        availability_zones [ "1"; "2" ]
                        depends_on [ (vhub :> IBuilder).ResourceId ]
                    }

                compareResourcesToJson [ firewall; vhub; vwan ] "azure-firewall.json"
            }

            test "AKS" {
                let kubeletMsi = createUserAssignedIdentity "kubeletIdentity"
                let clusterMsi = createUserAssignedIdentity "clusterIdentity"

                let assignMsiRoleNameExpr =
                    ArmExpression.create (
                        $"guid(concat(resourceGroup().id, '{clusterMsi.ResourceId.Name.Value}', '{Roles.ManagedIdentityOperator.Id}'))"
                    )

                let assignMsiRole =
                    {
                        Name = assignMsiRoleNameExpr.Eval() |> ResourceName
                        RoleDefinitionId = Roles.ManagedIdentityOperator
                        PrincipalId = clusterMsi.PrincipalId
                        PrincipalType = PrincipalType.ServicePrincipal
                        Scope = ResourceGroup
                        Dependencies = Set [ clusterMsi.ResourceId ]
                    }

                let myAcr = containerRegistry { name "farmercontainerregistry1234" }
                let myAcrResId = (myAcr :> IBuilder).ResourceId

                let acrPullRoleNameExpr =
                    ArmExpression.create (
                        $"guid(concat(resourceGroup().id, '{kubeletMsi.ResourceId.Name.Value}', '{Roles.AcrPull.Id}'))"
                    )

                let acrPullRole =
                    {
                        Name = acrPullRoleNameExpr.Eval() |> ResourceName
                        RoleDefinitionId = Roles.AcrPull
                        PrincipalId = kubeletMsi.PrincipalId
                        PrincipalType = PrincipalType.ServicePrincipal
                        Scope = AssignmentScope.SpecificResource myAcrResId
                        Dependencies = Set [ kubeletMsi.ResourceId ]
                    }

                let myAks =
                    aks {
                        name "aks-cluster"
                        dns_prefix "aks-cluster-223d2976"
                        add_identity clusterMsi
                        service_principal_use_msi
                        kubelet_identity kubeletMsi
                        depends_on clusterMsi
                        depends_on myAcr
                        depends_on_expression assignMsiRoleNameExpr
                        depends_on_expression acrPullRoleNameExpr
                    }

                let template =
                    arm {
                        location Location.EastUS
                        add_resource kubeletMsi
                        add_resource clusterMsi
                        add_resource myAcr
                        add_resource myAks
                        add_resource assignMsiRole
                        add_resource acrPullRole
                    }

                compareDeploymentToJson template "aks-with-acr.json"
            }
        ]

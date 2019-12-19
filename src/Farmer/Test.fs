module Test

open Farmer
open Farmer.Resources

let template (environment:string) storageSku webAppSku =
    let environment = environment.ToLower()
    let generateResourceName = sprintf "safe-%s-%s" environment

    let myStorageAccount = storageAccount {
        name (sprintf "safe%sstorage" environment)
        sku storageSku
    }

    let mySqlDb = sql {
        server_name "isaacsupersql"
        db_name "mydb"
        sku SqlAzure.Sku.Free
        admin_username "isaac"
        enable_azure_firewall
        use_encryption
        add_firewall_rule "My Firewall Rule" "192.168.1.1" "192.168.1.1"
    }

    let myCosmosDb = cosmosDb {
        name "isaacsappdb"
        server_name "isaacscosmosdb"
        throughput 400
        failover_policy NoFailover
        consistency_policy (BoundedStaleness(500, 1000))
        add_containers [
            container {
                name "myContainer"
                partition_key [ "/id" ] Hash
                add_index "/path" [ Number, Hash ]
                exclude_path "/excluded/*"
            }
        ]
    }

    let myFunctions = functions {
        name "isaacsuperfun"
        service_plan_name "isaacsuperfunhost"
        storage_account_link "isaacsuperstorage"
        app_insights_auto_name "isaacsuperai"
        operating_system Windows
        use_runtime DotNet
        setting "myDbName" myCosmosDb.DbName.Value
        depends_on myCosmosDb
    }

    let myWebApp = webApp {
        name (generateResourceName "web")
        service_plan_name (generateResourceName "webhost")
        sku webAppSku
        website_node_default_version "8.1.4"
        setting "public_path" "./public"
        setting "STORAGE_CONNECTIONSTRING" myStorageAccount.Key
        runtime_stack DotNetCore20
        runtime_stack (Java8 JavaSE)
        runtime_stack Python37
        runtime_stack Php71
        runtime_stack AspNet47
        runtime_stack Ruby24
        runtime_stack Node
        operating_system Linux

        depends_on myStorageAccount
        depends_on myCosmosDb
        depends_on mySqlDb
    }

    let myCustomAi = appInsights {
        name "myAppInsights"
    }

    let myVm = vm {
        name "isaacsVM"
        username "isaac"
        vm_size Size.Standard_A2
        operating_system CommonImages.WindowsServer_2012Datacenter
        os_disk 128 StandardSSD_LRS
        add_ssd_disk 128
        add_slow_disk 1024
    }

    let myContainerGroup = containerGroup {
        name "appWithHttpFrontend"
        os_type Models.ContainerGroups.ContainerGroupOsType.Linux
        add_tcp_port 80us
        add_tcp_port 443us
        restart_policy Models.ContainerGroups.ContainerGroupRestartPolicy.Always
        add_containers [
            containerInstance {
                name "nginx"
                image "nginx:1.17.6-alpine"
                ports [ 80us; 443us ]
                memory 0.5<Models.ContainerGroups.Gb>
                cpu 1
            }
            containerInstance {
                name "fsharpApp"
                image "myapp:1.7.2"
                ports [ 8080us ]
                memory 1.5<Models.ContainerGroups.Gb>
                cpu 2
            }
        ]
    }

    let mySearch = search {
        name "isaacsSearch"
        sku Sku.Basic
    }

    arm {
        location NorthEurope
        add_resource myStorageAccount
        add_resource myCosmosDb
        add_resource myWebApp
        add_resource mySqlDb
        add_resource myFunctions
        add_resource myVm
        add_resource mySearch
        add_resource myCustomAi
        add_resource myContainerGroup

        output "webAppName" myWebApp.Name
        output "webAppPassword" myWebApp.PublishingPassword
        output "functionsPassword" myFunctions.PublishingPassword
        output "functionsAIKey" myFunctions.AppInsightsKey
        output "storageAccountKey" myFunctions.StorageAccountKey
        output "customAiKey" myCustomAi.InstrumentationKey
    }

template "dev" Storage.Sku.StandardLRS WebApp.Sku.F1
|> Writer.generateDeployScript "deleteme"
|> ignore
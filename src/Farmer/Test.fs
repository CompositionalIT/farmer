module Test

open Farmer

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
        db_edition SqlAzure.Sku.Free
        admin_username "isaac"
        use_azure_firewall
        use_encryption
        firewall_rule "My Firewall Rule" "192.168.1.1" "192.168.1.1"
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
                include_index "/path" [ Number, Hash ]
                exclude_path "/excluded/*"
            }
        ]
    }

    let myFunctions = functions {
        name "isaacsuperfun"
        service_plan_name "isaacsuperfunhost"
        storage_account_name "isaacsuperstorage"
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

    let mySearch = search {
        name "isaacsSearch"
        sku Sku.BasicSearch
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
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
        server_name "isaacsql"
        db_name "mydb"
        db_edition SqlAzure.Sku.Free
        admin_username "isaac"
        use_azure_firewall
        use_encryption
        firewall_rule "192.168.1.1" "192.168.1.100"
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

    let myWebApp = webApp {
        name (generateResourceName "web")
        service_plan_name (generateResourceName "webhost")
        sku webAppSku

        use_app_insights (generateResourceName "insights")

        website_node_default_version "8.1.4"
        setting "public_path" "./public"
        setting "STORAGE_CONNECTIONSTRING" myStorageAccount.Key

        depends_on myStorageAccount
        depends_on myCosmosDb
        depends_on mySqlDb
    }

    arm {
        resource myStorageAccount
        resource cosmosDb
        resource myWebApp
        resource mySqlDb

        output "webAppName" myWebApp.Name
        output "webAppPassword" myWebApp.PublishingPassword        
    }

template "dev" Storage.Sku.StandardLRS WebApp.Sku.F1
|> Writer.toJson
|> Writer.toFile @"safe-template.json"
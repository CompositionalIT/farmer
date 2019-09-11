module Test

open Farmer

let template (environment:string) storageSku webAppSku =
    let environment = environment.ToLower()
    let generateResourceName = sprintf "safe-%s-%s" environment 
    let myStorageAccount = storageAccount {
        name (sprintf "safe%sstorage" environment)
        sku storageSku
    }

    let myCosmosDb = cosmosDb {    
        name (generateResourceName "cosmosdbsql")
        server_name (generateResourceName "cosmosdb")
        throughput 400
        failover_policy NoFailover
        consistency_policy (BoundedStaleness(100, 5))
        add_containers [
            container {
                name "myContainer"
                partition_key [ "/id" ] Hash
                include_index "/*" [ Number, Hash ]
                exclude_path "/excluded/*"
            }
        ]
    }

    let web = webApp {
        name (generateResourceName "web")
        service_plan_name (generateResourceName "webhost")
        sku webAppSku

        use_app_insights (generateResourceName "insights")

        website_node_default_version "8.1.4"
        setting "public_path" "./public"
        setting "STORAGE_CONNECTIONSTRING" myStorageAccount.Key

        depends_on myStorageAccount
    }

    arm {
        resource myStorageAccount
        resource web
        resource myCosmosDb

        output "webAppName" web.Name
        output "webAppPassword" web.PublishingPassword        
    }

template "ice" Storage.Sku.StandardLRS WebApp.Sku.F1
|> Writer.toJson
|> Writer.toFile @"safe-template.json"
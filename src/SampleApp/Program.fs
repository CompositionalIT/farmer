open Farmer
open Helpers


let myCosmosDb = cosmosDb {
    name "isaacsappdb"
    server_name "isaacscosmosdb"
    throughput 400
    write_model Standard
    consistency_policy (BoundedStaleness(500, 1000))
}

/// A web app with app insights
let myWebApp = webApp {
    name "mysuperwebapp"
    service_plan_name "myserverfarm"
    sku WebApp.Sku.F1
    use_app_insights "myappinsights"
    depends_on myCosmosDb
}

/// The overall ARM template which has the app as a resource.
let template = arm {
    location Locations.``North Europe``
    resource myWebApp
    resource myCosmosDb
}

// Now export it as an ARM template.
printf "Writing the ARM template..."

template
|> Writer.toJson
|> Writer.toFile @"webapp-appinsights.json"

printfn " all done!"
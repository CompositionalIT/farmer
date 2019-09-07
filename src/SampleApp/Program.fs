open Farmer
open Helpers

/// A web app with app insights
let myWebApp = webApp {
    name "mysuperwebapp"
    service_plan_name "myserverfarm"
    sku WebApp.Sku.F1
    use_app_insights "myappinsights"
}

/// The overall ARM template which has the app as a resource.
let template = arm {
    location Locations.``North Europe``
    resource myWebApp
}

// Now export it as an ARM template.
printf "Writing the ARM template..."

template
|> Writer.toJson
|> Writer.toFile @"webapp-appinsights.json"

printfn " all done!"
open Farmer
open Farmer.Builders
open Farmer.WebApp

//TODO: Create resources here!

let deploySlot = appSlot{
    setting "deployTime" (System.DateTime.Now.ToString("O"))
}
let app = webApp{
    name "rsp-test-app"
    sku Sku.P1V3
    add_slot deploySlot
}

let deployment = arm {
    name "rsp-test"
    location Location.NorthEurope
    add_resource app
    //TODO: Assign resources here using the add_resource keyword
}

// Generate the ARM template here...
deployment
//|> Writer.quickWrite @"generated-template"

// Or deploy it directly to Azure here... (required Azure CLI installed!)
// deployment
 |> Deploy.execute "rsp-test" Deploy.NoParameters
 |> printf "%A"

#r @"..\..\src\Farmer\bin\Debug\net5.0\Farmer.dll"
//#r "nuget:Farmer"

open Farmer
open Farmer.Builders

let deploySlot = appSlot{
    setting "originally" "deploy"
}

let myWebApp = webApp {
    name "rsp-test-app"
    sku WebApp.Sku.S1
    app_insights_off
    add_slot deploySlot
    deploy_production_slot Disabled
}

let deployment = arm {
    name "rsp-test2"
    location Location.NorthEurope
    add_resource myWebApp
}

deployment
|> Deploy.execute "rsp-test2" Deploy.NoParameters

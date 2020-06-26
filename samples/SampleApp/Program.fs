open Farmer
open Farmer.Builders
open Farmer.CoreTypes

let myApp = functions {
    name "isaacswebapp"
    secret_setting "thesecret"
    setting "thepublic" "value"
    app_insights_off
    enable_managed_identity
}

let users = [
    "isaac@compositional-it.com"
    "prashant@compositional-it.com"
    "alican@compositional-it.com" ]

let kv = keyVault {
    name "isaacskeyvault"
    add_access_policies [
        for user in AccessPolicy.findUsers users do
            AccessPolicy.create user.ObjectId
        for group in AccessPolicy.findGroups [ "Developers" ] do
            AccessPolicy.create group.ObjectId
    ]
}

let deployment = arm {
    location Location.NorthEurope
    add_resource myApp
    add_resource kv
}

// Generate the ARM template here...
deployment
|> Writer.quickWrite @"generated-template"

// Or deploy it directly to Azure here... (required Azure CLI installed!)
deployment
|> Deploy.execute "my-resource-group" [ "thesecret", "thevalue" ]
|> printfn "%A"
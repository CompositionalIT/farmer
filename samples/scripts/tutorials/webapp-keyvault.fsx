#r "nuget:Farmer"

open Farmer
open Farmer.Builders

let secretName = "storagekey"
let vaultName = "isaacsupersecret"

let datastore = storageAccount {
    name "isaacsuperstore"
}

let webapplication = webApp {
    name "isaacsuperweb"
    system_identity
    link_to_keyvault (ResourceName vaultName)
    secret_setting secretName
}

let secretsvault = keyVault {
    name vaultName
    add_secret (secretName, datastore.Key)
    add_access_policy (AccessPolicy.create webapplication.SystemIdentity)
}

let template = arm {
    location Location.WestEurope
    add_resources [
        secretsvault
        datastore
        webapplication
    ]
}

// Generate the ARM template here...
template
|> Writer.quickWrite (__SOURCE_DIRECTORY__ + @"/generated-template")

// Or deploy it directly to Azure here... (required Azure CLI installed!)
// template
// |> Deploy.execute "my-resource-group" Deploy.NoParameters
// |> printfn "%A"
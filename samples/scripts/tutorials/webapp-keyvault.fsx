#r "nuget:Farmer"

fsi.ShowDeclarationValues <- false

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
    app_insights_off
    use_managed_keyvault (Arm.KeyVault.vaults.resourceId vaultName)
    secret_setting secretName
}

let isaac = AccessPolicy.findUsers [ "isaac@compositional-it.com" ] |> Array.head

let secretsvault = keyVault {
    name vaultName
    add_secret (secretName, datastore.Key)
    add_access_policy (AccessPolicy.create webapplication.SystemIdentity)
    add_access_policy (AccessPolicy.create isaac.ObjectId)
}

let template = arm {
    location Location.WestEurope
    add_resources [
        secretsvault
        datastore
        webapplication
    ]
}

// // Generate the ARM template here...
template
|> Writer.quickWrite (__SOURCE_DIRECTORY__ + @"/generated-template")

// // Or deploy it directly to Azure here... (required Azure CLI installed!)
// template
// |> Deploy.execute "my-resource-group" Deploy.NoParameters
// |> printfn "%A"
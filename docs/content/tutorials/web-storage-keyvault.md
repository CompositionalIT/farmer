---
title: "Web App Secrets with KeyVault"
date: 2020-10-24
draft: false
---

#### Introduction
This tutorial shows how to create the infrastructure required to host a web app which can retrieve secrets from a secure store (Keyvault) using Azure *identity*. In this tutorial, we'll store the key for a storage account in Keyvault, but it could be anything. We'll cover the following steps:

1. Creating a Web App, Storage Account and a KeyVault instance.
1. Safely adding the Storage Account key into KeyVault.
1. Granting a read-only trust between KeyVault and the Web App.
1. Referencing the KeyVault setting from the Web App.

{{< figure src="../../images/tutorials/webapp-keyvault.png" caption="[Full code available here](https://github.com/CompositionalIT/farmer/blob/master/samples/scripts/tutorials/webapp-keyvault.fsx)">}}

#### Create basic resources
Start by creating the three main resources we need: a web app, storage account and key vault in the following ordwer:

```fsharp
open Farmer
open Farmer.Builders

let datastore = storageAccount {
    name "<storage name goes here>"
}

let webapplication = webApp {
    name "<web app name goes here>"
}

let secretsvault = keyVault {
    name "<key vault name goes here>"
}
```

#### Configure KeyVault with the Storage Key
Let's now add the storage account key to the key vault:

```fsharp
let secretsvault = keyVault {
    name "isaacsupersecret"
    add_secret ("storagekey", datastore.Key)
}
```

#### Grant secret access to the web app
Now we have KeyVault grant permission to the web app:

```fsharp
let webapplication = webApp {
    ...
    system_identity
}

let secretsvault = keyVault {
    ...
    add_access_policy (AccessPolicy.create webapplication.SystemIdentity)
}
```

The `AccessPolicy.create` builder method has several overloads; this one grants basic GET and LIST permissions to the web application's built-in system identity, which we have just activated above. You can also supply other permissions as a secondary argument to the `create` method.

#### Connect secret to the web app
Now, we use Farmer's Web App / Keyvault integration to seamlessly provide access to key vault secrets:

```fsharp
let webapplication = webApp {
    ...
    link_to_keyvault (ResourceName "<key vault name goes here>")
    secret_setting "storagekey"
}
```

or

```fsharp
let kv = keyVault {
    name "keyvault"
}

let webapplication = webApp {
    ...
    link_to_keyvault kv
    secret_setting "storagekey"
}
```

The first keyword "links" the vault with the web app, and tells Farmer that all "secret" settings should now be read from this vault. We cannot reference the keyvault directly in this case because it's declared *after* the web application, so we construct a `ResourceId` reference ourselves.

The second keyword actually adds the secret to the web app. If you hadn't added the `link_to_keyvault` keyword, this would be rendered into ARM as a secret parameter, but in this case because we've linked the vault in, it gets redirected to point there instead.

#### Adding extra type safety for sharing resources
To prevent accidentally mistyping the secret or vault names, you should bind the magic strings into symbols at the top of the template and replace usages in the template.

```fsharp
let secretName = "storagekey"
let vaultName = "<key vault name goes here>"
```

#### Add all resources to your ARM template
Add all the resources into an `arm` builder and then deploy the template as normal.

```fsharp
let template = arm {
    location Location.WestEurope
    add_resources [
        secretsvault
        datastore
        webapplication
    ]
}
```

#### That's it!
You now have a web application that can read the secret from the vault without using any keyvault connection string. If you log into the portal, you'll see that the secret setting is indeed in the Configuration section, but will have a green tick and a "Key vault reference" message next to it:

{{< figure src="../../images/tutorials/webapp-keyvault-connection.png">}}

The value of the secret will look something like this:

```
@Microsoft.KeyVault(SecretUri=https://<key vault name goes here>.vault.azure.net/secrets/storagekey)
```

The App Service will transparently retrieve the secret from the vault for you when you try to access the setting.

> By default, even *you* will not be able to access the secret from key vault! If you want to grant yourself access to the secret in the vault, you can use the `AccessPolicy.findUsers` method in code to retrieve your principal and grant access to the secret through Farmer. Alternatively, you can manually grant yourself access through the Portal etc.
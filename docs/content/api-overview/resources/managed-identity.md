---
title: "Managed Identity"
date: 2020-10-11T19:10:03.8036860-04:00
chapter: false
weight: 13
---

#### Overview
Managed Identity is used to create an identity that resources can run under automatically. This is similar to a service principal except that there is no credential to manage and the authorization token is retrieved through a secure internal handshake between the resource and the identity service in Azure.

* User Assigned Identity (`Microsoft.ManagedIdentity/userAssignedIdentities`)
* Federated Identity Credential (`Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials`)

Using a managed identity as opposed to e.g. connection strings brings several benefits:

* There is no client secret or certificate to configure and rotate because this is handled implicitly by Azure infrastructure.
* The identity is tied to one or more specific resources, so cannot be used by anything else, like a user.
* Many services allow more granular permissions than e.g. a connection string.
* You can grant and revoke access completely independently of the application attempting to gain access to the resource.

Once created, the managed identity resource can be referenced by other resources both in order to:

* Enable a resource to run *as* that identity
* Enable a resource to grant permissions *to* that identity

For example, you may wish to run a Virtual Machine or an Web App under a identity that you create, and then to *grant permissions* to that identity to allow reading from a storage account. You can define the permissions completely independently of the Virtual Machine or Web App.

{{<mermaid align="left">}}
graph LR

A(identity)
B(virtual machine)-. runs as .->A
D(web app)-. runs as .->A
C(storage account)-. grants permissions .->A
A -. request made in this identiy .->C

{{< /mermaid >}}

> See [here](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview) and [here](https://docs.microsoft.com/en-us/azure/app-service/overview-managed-identity) for the official documentation on the Microsoft Azure docs website.

#### Identity Types in Azure
Identities come in two flavours in Azure: *System* and *User* assigned.
* **System Identities** are available whenever you create a resource, such as a VM. Each resource has its own system identity, and they cannot be shared across resources.
* **User Identities** are created *by you*; they exist idependently of any resources and thus can be shared across them. In Farmer, the `userAssignedIdentity` builder can be used for this. You also need to "link" a user identity to the resource that you wish to be able to "run as" it.

> User Assigned Identities are themselves ARM resources and need to be added to your Farmer `arm {}` blocks!

{{<mermaid align="left">}}
graph LR

A(user assigned identity)

subgraph Web App
B(system identity)
end

C(storage account)

C -.grants permissions .-> A
C -.grants permissions .-> B

{{< /mermaid >}}

#### User Assigned Identity Builder
The `userAssignedIdentity` builder constructs user assigned managed identities which can be created and then assigned
to one or more resources.

| Keyword | Purpose |
|-|-|
| name | Sets the name of the user assigned identity. |
| add_tag | Adds a tag to the user assigned identity resource. |
| add_tags | Adds multiple tags to the user assigned identity resource. |

#### Helper Methods
Because the User Assigned Identity builder is so simple, we also provide a simple builder function to create identities as an alternative to using the standard builder syntax:

```fsharp
open Farmer.Builders

let uai = createUserAssignedIdentity "mytestidentity"
```

#### Example: System Identity
In this example, a web app needs a secret from a key vault. By using the system identity on the web app, application code can be granted access to the key vault with no need to provide it a client secret.

```fsharp
open Farmer
open Farmer.Builders

let wa = webApp {
    name "myApp"
    system_identity // turn on the system identity of the web app
}

let vault = keyVault {
    name "my-vault"
    add_access_policies [
        // grant access to the web app's system identity to key vault.
        // by default GET and LIST permissions are granted.
        AccessPolicy.create wa.SystemIdentity
    ]
}

let template = arm {
    add_resources [ wa; vault ]
}
```

There is no need to add a specific identity resource to Farmer in this case because the System Identity is created along with the web app itself.

#### Example: User Assigned Identity
In this example, a web app needs access to a Storage Account with a specific role. By assigning an identity to the web app, the application code can be granted access to the storage account; we also provide the Client Id to the application as a public setting in order for the application to correctly impersonate as the identity within code.

By creating a user assigned identity, unlike a system identity, we can also apply this identity onto other resources so that they, too, can "share" the permissions and identity. In this example, we also apply the identity onto a container group.

```fsharp
// Create a user assigned identity
let sharedIdentity =
    userAssignedIdentity {
        name "container-group-identity"
    }

// Apply it onto the web app
let myWeb = webApp {
    name "myApp"
    // Add the identity to the web app
    add_identity sharedIdentity
    // Provide the client id to the app for use in code
    setting "ClientId" sharedIdentity.ClientId
}

let group =
    containerGroup {
        name "myapp-with-identity"
        add_instances [
            containerInstance {
                name "my-app"
                image "myregistry.azurecr.io/myapp:latest"
            }
        ]
        // Also apply it here. All of the containers in this group share this managed identity.
        add_identity sharedIdentity
    }

let data = storageAccount {
    name "dataidentity"
    // Allow the shared identity blob data reader access to storage.
    grant_access sharedIdentity Roles.StorageBlobDataReader
}


let deployment = arm {
    add_resources [
        sharedIdentity
        myWeb
        group
        data
    ]
}
```

In this example, notice that we explicitly add the `sharedIdentity` resource to the `arm {}` block.

#### Example: Federated Identity Credentials

A federated identity credential allows the exchange of an OpenID Connect (OIDC) token for an Azure Entra ID token. The audience, issuer, and subject of the OIDC token are registered as a federated identity credential so that Entra ID will issue the access token. Federated identity credentials are a foundation for enabling workload identity federation and removes the need to manage client secrets when connecting to Azure resources for an OIDC identity provider.

The example below create a user assigned identity and then adds a federated identity credential to associate that identity with pull requests from a github repository. This can be used to enable GitHub Actions to access Azure infrastructure under this identity.

```fsharp
open Farmer.Builders

arm {
    add_resources [
        userAssignedIdentity {
            name "cicd-msi"
            add_federated_identity_credentials [
                federatedIdentityCredential {
                    name "gh-actions-cred"
                    audience EntraIdAudience
                    issuer "https://token.actions.githubusercontent.com"
                    subject "repo:compositionalit/farmer:pull_request"
                }
            ]
        }
    ]
}
```

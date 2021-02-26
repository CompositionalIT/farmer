---
title: "Declarative Steps in ARM Deployment"
date: 2021-02-25
draft: false
---

#### Introduction

An ARM deployment typically represents the infrastructure you want deployed as a result of it, referred to as the "goal state". For instance, you may want to have a web application with a database, so you'll define your goal state that includes database and web application resources. However, it's not always that simple - there are various reasons that some of the steps of a deployment cannot be represented by the goal state resource model.

* Creating a backup of a database before a deployment.
* Interacting with API's that aren't represented in ARM itself (Kubernetes control plane, networking devices, or the API's of the application you're deploying).
* Performing ARM _operations_ such as creating a certificate.

In any of these scenarios, there is some imperative logic that needs to be included in the ARM deployment. To keep the ARM deployment itself repeatable, it's often best to ensure this imperative logic is idempotent - that is, it can run repeatedly without incurring side effects.

In this tutorial, we will handle the third case, running an ARM operation to create a certificate in a key vault. We'll create a `deploymentScript` resource for this, which creates a temporary ARM resource for the purposes of executing this imperative logic.

1. Create a user assigned identity.
1. Create a storage account for making the certificate available to the web app.
1. Create a key vault for generating the certificate.
1. Run an imperative script to create a certificate in the key vault and copy it to the storage account.
1. Create a web application in a container group that attaches to the storage account to uses the certificate.

{{< figure src="../images/tutorials/imperative-resource.png" caption="[Full code available here](https://github.com/CompositionalIT/farmer/blob/master/samples/scripts/tutorials/keyvault-certs.fsx)">}}

#### Create a user assigned identity

A deployment script runs in a temporary container group and needs an identity, as does the container group. This identity will be a contributor over the resources in the resource group where this deployment runs so that it has permissions to upload the certificate to the storage account.

```fsharp
let appIdentity = userAssignedIdentity {
    name "my-app-user"
}
```

#### Create the Certificate Storage Account

Depending on the type of compute resource used, you may be able to retrieve the certificate directly from the key vault on startup. However, for a container group, a good solution is to have a file share on a storage account. After creating the certificate, we will download it from the key vault and upload it to this storage account file share to make it available to the container group.

```fsharp
let certStorage = storageAccount {
    name "myappcertstorage123"
    sku Storage.Sku.Standard_LRS
    add_file_share "certs"
}
```

#### Create the Key Vault

We need to create a key vault to generate the certificate. When a certificate is created in a key vault, the public key is stored as a certificate and the full certificate with private and public key is stored as a secret of the same name. To enable this access, we will need to define an `accessPolicy` on the key vault that allows our `appIdentity` to create and retrieve certificates and secrets.

```fsharp
let kv = keyVault {
    name "myappcertificates"
    add_access_policies [
        accessPolicy {
            object_id appIdentity.PrincipalId
            secret_permissions [ KeyVault.Secret.Set; KeyVault.Secret.Get ]
            certificate_permissions [ KeyVault.Certificate.Create; KeyVault.Certificate.Get ]
        }
    ]
}
```

#### The Imperative Part: Creating the Certificate

Creating a certificate is an imperative operation because this is a multiple step process where a key pair is created, then a certificate signing request is created from the key pair and signed by a certificate authority. Certificates can also be "self-signed", meaning they have no certificate authority and must be individually trusted. This whole process means you cannot simply repeat it without side effects, so it is represented in ARM as an _operation_ rather than a resource.

Creating a certificate in an Azure key vault requires that you provide a policy for the certificate which defines the various settings such as the key size, issuer, and subject name (what identifies the host when presenting the certificate).

You can use the default policy directly, but this doesn't let you set the subject name, so instead, we will build our own policy. To get a reference on what a valid policy contains, use the following Azure CLI command to "scaffold" a policy:

`az keyvault certificate get-default-policy --scaffold`

We want a policy.json that roughly matches this, with a few adjustments for our scenario. F# anonymous records are very handy for creating JSON directly, so we'll use one here to create a policy JSON string similar to the scaffold. Because we need to embed this in our ARM template so it can run in the deployment script, we'll convert it to base64 and avoid any issues with trying to embed JSON in another JSON file.

```fsharp
let policy =
    {|
        keyProperties =
            {|
                exportable = true
                keyType = "RSA"
                keySize = 2048
                reuseKey = false
            |}
        secretProperties =
            {|
                contentType = "application/x-pkcs12"
            |}
        x509CertificateProperties =
            {|
                subject = "CN=my-web-app.eastus.azurecontainer.io"
                subjectAlternativeNames =
                    {|
                        dnsNames = [ "my-web-app.eastus.azurecontainer.io" ]
                    |}
            |}
        issuerParameters =
            {|
                name = "Self"
            |}
    |}
let policyJsonB64 =
    policy
    |> System.Text.Json.JsonSerializer.Serialize // serialize to JSON
    |> System.Text.Encoding.UTF8.GetBytes // and then encode it for easy embedding
    |> System.Convert.ToBase64String
```

Now for the deployment script itself. This will run the Azure CLI within a temporary container. It needs to perform the following steps:

1. Take the embedded base64 policy.json string and write it to the file system where the deployment script runs.
1. Create a certificate, using that policy.json file.
1. Download the secret containing the public and private key pair in a .pfx file.
1. Upload the .pfx file to the storage account file share to make it available to the container group.

The string interpolation in F# 5.0 is very handy for embedding F# values in the bash script statements.

```fsharp
let script =
    [
    "set -e"
    // Write the encoded policy to a file in the deployment script resource.
    $"echo {policyJsonB64} | base64 -d > policy.json"
    // Run imperative az CLI commands to create the certificate.
    $"az keyvault certificate create --vault-name {kv.Name.Value} -n my-app-cert -p @policy.json"
    // Download the cert
    $"az keyvault certificate download --file cert.pem --vault-name {kv.Name.Value} -n my-app-cert"
    // Download the pfx with cert and private key
    $"az keyvault secret show --vault-name {kv.Name.Value} -n my-app-cert | jq .value -r | base64 -d > key.pfx"
    // Upload to storage file
    $"az storage file upload --account-name {certStorage.Name.ResourceName.Value} --share-name certs --source key.pfx"
    ] |> String.concat ";\n"
```

With the hard part out of the way, we can define the `deploymentScript` resource, which is a temporary ARM resource that represents running these imperative steps. Because we don't want this to run until the key vault and storage account are available, we need to use `depends_on` and reference these two resources. Also, notice this uses the `appIdentity` that was granted access to the key vault secrets and certificates.

```fsharp
deploymentScript {
    name "create-certificate"
    identity appIdentity
    depends_on kv
    depends_on certStorage
    force_update
    cleanup_on_success
    retention_interval 1<Hours>
    script_content script
}
```

#### Creating the Web Application

Our web application will be a simple "hello world" service, as the interesting part is that it listens on HTTPS. Doing this requires the key pair be loaded by the service when it creates the binding to an HTTPS port. Here is the script content. Notice we need to add the certificate to the `X509Store`. This avoids some SSL warnings within the service itself due to using a self-signed certificate. If you are using a trusted third party CA, this may not be necessary.

```fsharp
#r "nuget: Suave, Version=2.6.0"

open Suave
open System.Security.Cryptography.X509Certificates

let certWithKey = new X509Certificate2("/certs/key.pfx", "")
let store = new X509Store(StoreName.Root, StoreLocation.CurrentUser)
store.Open(OpenFlags.ReadWrite)
store.Add(certWithKey)
store.Close()

let config = { defaultConfig with bindings = [ HttpBinding.createSimple (HTTPS certWithKey) "0.0.0.0" 443 ] }
startWebServer config (Successful.OK "Hello Secure Farmers!")
```

We will read this short script into a string that we can pass to our container group. In real life, you probably have a full application published in a container image, but for illustrative purposes, we are just embedding the script.

```fsharp
let webAppMain = System.IO.File.ReadAllText "keyvault-certs-app.fsx"
```

Now we create the container group. It uses a .NET 5.0 SDK image to run the script and has two volume mounts. One is for the embedded script itself, and the other is for the volume mount from the Azure storage account file share where the certificate itself is stored.

```fsharp
let webApp = containerGroup {
    name "my-web-app"
    add_identity appIdentity
    add_instances [
        containerInstance {
            name "fsi"
            image "mcr.microsoft.com/dotnet/sdk:5.0"
            command_line ("dotnet fsi /src/main.fsx".Split null |> List.ofArray)
            add_volume_mount "script-source" "/src"
            add_volume_mount "cert-volume" "/certs"
            add_public_ports [ 443us ]
            cpu_cores 0.2
            memory 0.5<Gb>
        }
    ]
    public_dns "my-web-app" [ TCP, 443us ]
    add_volumes [
        volume_mount.secret_string "script-source" "main.fsx" webAppMain
        volume_mount.azureFile "cert-volume" "certs" certStorage.Name.ResourceName.Value
    ]
}
```

#### The ARM Template

With all of these resources, we can create an ARM template. It contains four declarative resources: the user assigned identity, a key vault, a storage account, and a container group. It also contains a deployment script resource for the imperative logic.

```fsharp
arm {
    location Location.EastUS
    add_resources [
        appIdentity
        kv
        certStorage
        createCertificate
        webApp
    ]
} |> Writer.quickWrite "keyvault-certs"
```

Deploying the resulting template through ARM will result in ARM attempting to reach the goal state with as much concurrency as dependencies allow. It will deploy the user assigned identity first, then both the key vault and the storage account at the same time, and then finally it will run the deployment script and deploy the container group.

The end result is a container group running an HTTPS service using a certificate that was created in the newly provisioned key vault.

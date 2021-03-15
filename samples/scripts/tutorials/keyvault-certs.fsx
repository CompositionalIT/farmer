#r "nuget: Farmer"

open Farmer
open Farmer.Builders

let appIdentity = userAssignedIdentity {
    name "my-app-user"
}

let certStorage = storageAccount {
    name "myappcertstorage123"
    sku Storage.Sku.Standard_LRS
    add_file_share "certs"
}

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

(*
az keyvault certificate get-default-policy --scaffold
{
  "issuerParameters": {
    "certificateTransparency": null,
    "certificateType": "(optional) DigiCert, GlobalSign or WoSign",
    "name": "Unknown, Self, or {IssuerName}"
  },
  "keyProperties": {
    "curve": null,
    "exportable": true,
    "keySize": 2048,
    "keyType": "(optional) RSA or RSA-HSM (default RSA)",
    "reuseKey": true
  },
  "lifetimeActions": [
    {
      "action": {
        "actionType": "AutoRenew"
      },
      "trigger": {
        "daysBeforeExpiry": 90,
        "lifetimePercentage": null
      }
    }
  ],
  "secretProperties": {
    "contentType": "application/x-pkcs12 or application/x-pem-file"
  },
  "x509CertificateProperties": {
    "ekus": [
      "1.3.6.1.5.5.7.3.1"
    ],
    "keyUsage": [
      "cRLSign",
      "dataEncipherment",
      "digitalSignature",
      "keyEncipherment",
      "keyAgreement",
      "keyCertSign"
    ],
    "subject": "C=US, ST=WA, L=Redmond, O=Contoso, OU=Contoso HR, CN=www.contoso.com",
    "subjectAlternativeNames": {
      "dnsNames": [
        "hr.contoso.com",
        "m.contoso.com"
      ],
      "emails": [
        "hello@contoso.com"
      ],
      "upns": []
    },
    "validityInMonths": 24
  }
}
*)
let createCertificate =
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
    
    // Define the deployment script resource, encapsulating the imperative steps as an ARM resource.
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

// The F# script will load the cert with the key, add it to the store to trust it, 
// and then bind it to the HTTPS port for the service.
let webAppMain = System.IO.File.ReadAllText "keyvault-certs-app.fsx"

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
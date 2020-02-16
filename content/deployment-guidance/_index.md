---
title: "Deployment Guidance"
date: 2020-02-05T08:45:15+01:00
weight: 4
---

You can deploy Farmer templates in a number of ways, depending on how you would prefer to work with ARM templates and tooling.

#### ARM templates are just a means to an end to me
If you don't use ARM templates today, or don't need to edit them directly, you can opt to do away with them completely. You'll create Farmer applications which use a simple F# SDK to interact with Azure; Farmer will create ARM templates in the background for you transparently, so you'll never see or interact with them.

In such a case, you can opt to deploy them using one of two modes:

* An [interactive mode](../api-overview/writer/#interactive-deploy-to-azure), perfect for when testing out templates from your development environment. This mode provides an F# wrapper around the Azure CLI which captures your credentials during the deployment process.
* An [automated mode](../api-overview/writer/#non-interactive-deploy-to-azure), in which you create a *service principal* in Azure (effectively, an account in Azure Active Directory that has permissions to create and amend resources without human intervention), and securely use the credentials of this account to deploy the ARM template.

If you're looking to stay within F# and e.g. respond to outcomes from the deployment such as using deployment outputs, this is an excellent option because Farmer is just a dotnet application and the deployment call is a simple function call.

Another benefit of this is because Farmer is a simple .NET Standard library, you can use it natively within .NET build tools such as FAKE or CAKE.

#### I already have an ARM deployment strategy
If you already use ARM templates, you'll probably already have a strategy for working with templates and deploying them to Azure, such as [PowerShell](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/deploy-powershell), the [Azure CLI](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/deploy-cli) or a build system such as [Azure DevOps](https://docs.microsoft.com/en-us/azure/devops/pipelines/tasks/deploy/azure-resource-group-deployment?view=azure-devops) or [Octopus Deploy](https://octopus.com/docs/deployment-examples/azure-deployments/resource-groups). In such a case, you may want to use Farmer to *generate*, but not *deploy*, your ARM templates.

#### I want to hard-craft my ARM templates
If you want to retain fine-grained control over ARM templates, you can use Farmer to create a one-off task to rapidly generate an ARM template which you then take ownership of. In this case, Farmer itself won't be a part of your build / deploy chain, which will remain the same as today - you'll use Farmer just as an edit-time task to create the ARM template itself.

The choice is yours.

##### How do I create a Service Principal?
If you're trying to deploy to Azure in an automated fashion, you'll need to create a Service Principal account that has permissions in Azure to deploy ARM templates on your behalf.

The Azure CLI provides a simple way to create one using the [az ad sp](https://docs.microsoft.com/en-us/cli/azure/ad/sp?view=azure-cli-latest#az-ad-sp-create-for-rbac) command:

```cmd
az ad sp create-for-rbac --name farmer-deploy
```

This will provide output similar to the following:

```cmd
{
  "appId": "1181c21b-78f3-42b3-a26d-03ba75c7b674",        
  "displayName": "farmer-deploy",
  "name": "http://farmer-deploy",
  "password": "4aa3b120-f2b2-4ea9-941b-5891fef0ef11",     
  "tenant": "aa7f7453-15af-4ab0-5d41-aeb4a25293bc"        
}
```

The mapping from these fields to the credentials used in Farmer are:

| Azure CLI | Farmer |
|-|-|
| appId | ClientId |
| password | ClientSecret |
| tenant | TenantId |

You should store these credentials in a secure store, such as your CI/CD service or e.g. Azure KeyVault and should avoid committing them into source control.
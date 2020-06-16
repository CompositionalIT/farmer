---
title: "ARM Basics"
draft: false
weight: 1
---

> Skip this section if you are already familiar with ARM Templates

This won't be an introduction to [Azure Resource Manager](https://docs.microsoft.com/en-us/azure/azure-resource-manager/management/overview) or [ARM templates](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/overview). Instead let's go through the main parts that are important for creating a new resource.

The main parts of ARM Templates can be broken into resources, outputs, variables, and parameters. Farmer has [limited support for parameters and no support for variables](https://compositionalit.github.io/farmer/api-overview/parameters/), so we will not cover them.

So a generated ARM Template from Farmer will have the following structure.

```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
  "contentVersion": "1.0.0.0",
  "outputs": {},
  "parameters": {},
  "resources": []
}
```

When building a new resource in Farmer you are providing the means for a user of Farmer to generate a new *resource type*, or configure a new property on an existing resource. These resources are added by Farmer to the `resources` array you can see above.

When building up a resource it will have a schema that looks something like this.

```json
{
  "name": "my-example-resource",
  "type": "Microsoft.ContainerRegistry/registries",
  "apiVersion": "2019-05-01",
  "location": "westeurope",
  "tags": {},
  "sku": {
    "name": "S1"
  },
  "properties": {
    "adminUserEnabled": true,
  },
  "resources": []
}
```

Your resource you create will have a [type of service](https://docs.microsoft.com/en-us/azure/templates/microsoft.containerregistry/allversions) that it represents. Each service has many versions, represented by a date. Typically your builder will at first focus on adding properties to the `properties` field to configure a service to be deployed in a certain state.

### Where can I find docs on ARM templates schemas themselves?
There are three good sources to learning about specific ARM resources and what parts need to be used in creating an equivalent Farmer resource:

* **Reference Docs**: The reference documentation contains details on the schema for every resource and every version e.g. [Container Registry](https://docs.microsoft.com/en-us/azure/templates/microsoft.containerregistry/2017-10-01/registries) reference.
* **Sample Template**: The Azure Quickstart Templates github repository contains many examples of real-world ARM templates e.g. [Container Registry with Geo Replication](https://github.com/Azure/azure-quickstart-templates/tree/master/101-container-registry-geo-replication) sample.
* **Reverse engineer**: You can manually create a required resource in Azure, and then use Azure's [export ARM template](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/export-template-portal) functionality to create an ARM template. It's important that you test out the exported template yourself before porting it to Farmer, because Azure sometimes exports invalid templates!
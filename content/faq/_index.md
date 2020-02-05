---
title: "FAQs"
date: 2020-02-04T22:38:23+01:00
weight: 5
---

#### How can I help?
Try out Farmer and see what you think.
* Create as many issues as you can for both bugs, discussions and features
* Create suggestions for features and the most important elements you would like to see added

#### I have an Azure subscription, but I'm not an expert. I like the look of this - how do I "use" it?
1. Create an [ARM template](https://docs.microsoft.com/en-us/azure/azure-resource-manager/template-deployment-overview) using the Farmer sample app.
1. Follow the steps [here](#deploying-arm-templates) to deploy the generated template into Azure.
1. Log any issues or ideas that you find [here](https://github.com/CompositionalIT/farmer/issues/new).

#### I don't know F#. Would you consider writing a C# version of this?
I'm afraid not. F# isn't hard to learn (especially for simple DSLs such as this), and you can easily integrate F# applications as part of a dotnet solution, since F# is a first-class citizen of the dotnet core ecosystem.

#### Are you trying to replace ARM templates?
No, we're not. Farmer *generates* ARM templates that can be used just as normal; Farmer can be used simply to make the process of getting started much simpler, or incorporated into your build pipeline as a way to avoid managing difficult-to-manage ARM templates and instead use them as the final part of your build / release pipeline.

#### Are you trying to compete with Pulumi?
No, we're not. Farmer has (at least currently) a specific goal in mind, which is to lower the barrier to entry for creating and working with ARM templates that are non-complex. We're not looking to create a cross-platform DSL to also support things like Terraform etc. or support deployment of code along with infrastructure (or, at least, only to the extent that ARM templates do).

#### There's no support for variables or parameters!
Farmer intentionally has limited support for ARM parameters and variables. Read [here](../api-overview/parameters.md) to find out the alternatives.
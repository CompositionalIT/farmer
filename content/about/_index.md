---
title: "About"
date: 2020-02-04T22:44:07+01:00
weight: 1
---

#### About Farmer
Farmer is an open source, free to use .NET domain-specific-language (DSL) for rapidly generating non-complex Azure Resource Manager (ARM) templates.

For those of you working with Azure today, you may already be aware that one of the most useful features is the ability to generate entire infrastructure architectures as code via ARM Template files. These templates contain a declarative model that allows repeatable deployments and idempotent releases (among other things).

#### What's wrong with ARM?
Unfortunately, ARM templates have several limitations. For example, templates are authored in a JSON dialect. This means not only that it can be verbose, but also contains very limited type checking and support which makes creating templates difficult. It also requires "embedded", difficult-to-maintain stringly-typed code in order to achieve what might be trivial in a "proper" programming language, such as references, variables and parameters - or writing elements such as loops.

In other words, whilst working with ARM templates that have already been created is relatively straightforward, the *authoring* of the templates themselves is time-consuming and error-prone.

Whilst there have been some recent improvements to ARM - including tooling improvements in VS Code through an extension - we think that we can do much better than relying on tooling for a specific IDE, and this means looking at something apart from JSON when directly authoring ARM templates.

#### How does Farmer work?
Farmer templates are simple .NET Core applications which reference the [Farmer NuGet package](https://www.nuget.org/packages/Farmer/). This package contains a set of types to model ARM resources in a strongly-typed and succinct fashion, as well as functionality to create ARM templates and even deploy directly to Azure.

#### What can I use Farmer for?
Farmer currently has support for the following resources.

* Storage
* App Service
* Functions
* Azure Search
* Application Insights
* Cosmos DB
* Azure SQL
* Virtual Machines
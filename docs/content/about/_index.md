---
title: "About"
date: 2020-02-04T22:44:07+01:00
weight: 1
---

#### About Farmer
Farmer is a .NET domain-specific-language (DSL) for rapidly generating non-complex Azure Resource Manager (ARM) templates. Farmer is [commercially supported](../support/), open source and free-to-use.

For those of you working with Azure today, you may already be aware that one of the most useful features is the ability to generate entire infrastructure architectures as code via ARM Template files. These templates contain a declarative model that allows repeatable deployments and idempotent releases (among other things).

#### What's wrong with ARM?
Unfortunately, ARM templates have some limitations caused by the fact that they must be authored in a verbose JSON dialect:
* They provide very limited type checking and support, which makes creating discovery and creation of template features difficult.
* Templates need a lot of boilerplate to be created for even relatively simple and common resources.
* It requires "embedded", difficult-to-maintain stringly-typed code in order to achieve what might be trivial in a "proper" programming language, such as references, variables and parameters - or writing elements such as loops.
* The documentation for ARM templates is not always kept up-to-date, so understanding and learning how to properly use them can involve a lot of searching and trial-and-error.

In other words, whilst working with ARM templates that have already been created is relatively straightforward, the *authoring* of the templates themselves is time-consuming and error-prone.

Whilst there have been some recent improvements to ARM - including tooling improvements in VS Code through an extension - we think that we can do much better than relying on tooling for a specific IDE, which means using something different than JSON when directly authoring ARM templates.

#### What does Farmer do to fix this?
Farmer templates are simple .NET Core applications which reference the [Farmer NuGet package](https://www.nuget.org/packages/Farmer/). This package contains a set of *types* that model Azure resources in a strongly-typed and succinct fashion, as well as functionality to create ARM templates from this model - and even deploy directly to Azure.

#### What can I use Farmer for?
Farmer currently has support for a [large number of common resources](../api-overview/resources) including web apps, sql and storage, with more being added over time.
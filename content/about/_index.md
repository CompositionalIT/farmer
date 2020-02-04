+++
title = "About"
date = 2020-02-04T22:44:07+01:00
weight = 1
chapter = false
pre = ""
+++

#### About Farmer

Farmer is an open source, free to use .NET domain-specific-language (DSL) for rapidly generating non-complex Azure Resource Manager (ARM) templates.

For those of you working with Azure today, one of the most useful features is the ability to generate entire infrastructure architectures as code through ARM Templates, with a declarative model that allows repeatable deployments and idempotent releases among other things.

#### What's wrong with ARM?
Unfortunately, ARM templates have several limitations including the fact that it is essentially a JSON dialect. This means not only that it can be verbose, but also that is requires "embedded", difficult-to-maintain stringly-typed code in order to achieve what might be trivial in a "proper" programming language, such as references, variables and parameters - or writing elements such as loops.

This means that, whilst working with ARM templates once created is relatively straightforward, the *authoring* of the templates themselves is time-consuming and error-prone.

Whilst there have been some recent improvements to ARM - including tooling improvements in VS Code through an extension, we think that we can do much better than relying on tooling for a specific IDE. However, this means looking at something apart from JSON when directly authoring ARM templates themselves.
    
#### How does Farmer work?
Farmer templates are .NET Core applications which reference the [Farmer NuGet package](https://www.nuget.org/packages/Farmer/), which contains a set of types to model ARM resources in a strongly-typed and succinct fashion, as well as functionality to create ARM templates and even deploy to Azure.

#### What can I use Farmer for?
Farmer fixes these issues, by making it easy to author ARM tempaltes. You can use Farmer in a number of ways:

* As a way to quickly generate your ARM template, which is then committed into source control and deployed as normal by e.g. Azure Dev Ops.
* Creating a basic ARM template which generates 90% of what you need, after which you will then manually make further changes to the template, and deploy or commit into source control as normal.
* As a build step in your CD process to generate and deploy your ARM template. In this model, you commit your Farmer code into source control; the ARM template is a transient file that is generated during the build process and deployed into Azure, similar to the relationship between e.g. Typescript and Javascript or C# and a DLL.

The choice is yours.
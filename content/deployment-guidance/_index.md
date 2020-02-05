---
title: "Deployment Guidance"
date: 2020-02-05T08:45:15+01:00
weight: 4
---

You can use Farmer in a number of ways:

* As a way to quickly generate your ARM template, which is then committed into source control and deployed as normal by e.g. Azure Dev Ops.
* Creating a basic ARM template which generates 90% of what you need, after which you will then manually make further changes to the template, and deploy or commit into source control as normal.
* As a build step in your CD process to generate and deploy your ARM template. In this model, you commit your Farmer code into source control; the ARM template is a transient file that is generated during the build process and deployed into Azure, similar to the relationship between e.g. Typescript and Javascript or C# and a DLL.

The choice is yours.
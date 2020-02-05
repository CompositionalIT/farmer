---
title: "API Overview"
date: 2020-02-05T08:53:46+01:00
weight: 3
chapter: false
---

#### API aims
The key guiding principles of the Farmer API are (in order):

* Simplicity: Make it as easy as possible to do the most common tasks.
* Type safety: Where possible, use F#'s type system to make it impossible to create invalid templates.
* Flexibility: Provide users with the ability to override the defaults where needed.

#### How do I use Farmer?
Farmer works on a simple, consistent process:

1. You create **Farmer resources**, such as Storage Accounts and Web Apps.
1. Each Farmer resource can represent *one or many* ARM resources. For example, a Farmer `webApp` resource represents both the `Microsoft.Web/sites` and `Microsoft.Web/serverfarms` resources. In addition, it optionally also provides simplified access to an `Microsoft.Insights/components`.
1. You configure each resource using simple, human-readable custom keywords in a strongly-typed environment.
1. You link together resources as required.
1. Once you have created all resources, you bundle them up together into an **ARM deployment resource**.
1. You then generate (and optionally deploy) an ARM template.
1. The rest of your deployment pipeline stays the same.
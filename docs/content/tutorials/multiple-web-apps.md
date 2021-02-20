---
title: "Multiple web apps"
date: 2020-10-24
draft: false
weight: 5
---

#### Introduction
This tutorial walks you through creating multiple web applications that will share a common web server. We'll cover the following steps:

1. Creating a web app.
1. Creating multiple web apps and "sharing" the first web app's service plan and Application Insights instances.
1. How to use F#'s list comprehensions to rapidly creating multiple websites.

{{< figure src="../../images/tutorials/multiple-web-apps.png" caption="[Full code available here](https://github.com/CompositionalIT/farmer/blob/master/samples/scripts/tutorials/multiple-web-apps.fsx)">}}


#### Creating a single web app
Create a standard web app as normal:

```fsharp
let primaryWebApp = webApp {
    name "primarywebapp"
    sku WebApp.Sku.F1
}
```

#### Creating secondary web apps
Create a second web app, but this time link to the service plan that is part of the *first* web app:

```fsharp
let secondaryWebApp = webApp {
    name "secondarywebapp"
    link_to_service_plan primaryWebApp.ServicePlanName
    link_to_app_insights primaryWebApp.AppInsightsName
}
```

You can now add both web apps to the `arm { }` block for deployment:

```fsharp
let template = arm {
    location Location.NorthEurope
    add_resource primaryWebApp
    add_resource secondaryWebApp
}
```

#### Creating dedicated Service Plan and App Insights instances
Rather than "piggy back" on a "primary" web app, you can also opt to create dedicated service plan and app insights instances and configure all web apps to use them. This is a slightly more verbose option, but you may find it clearer, and as we'll see shortly, it can sometimes be useful to declare these instances outside of the web app:

```fsharp
let plan = servicePlan {
    name "theFarm"
    sku WebApp.Sku.F1
}

let ai = appInsights {
    name "insights"
}

let aWebApp = webApp {
    name "primarywebapp"
    link_to_service_plan plan
    link_to_app_insights ai
}

let anotherWebApp = webApp {
    name "secondarywebapp"
    link_to_service_plan plan
    link_to_app_insights ai
}
```

> As you are creating the plan and AI instances yourself, you also need to remember to add them to the `arm { }` block!

#### Rapidly creating multiple web apps
F# has excellent support for working with collections of data, including *creating* data. Let's assume we wanted to create four web apps, each with a name "mywebapp-{index}" e.g. "mywebapp-1" etc. We can use F#'s [list](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/lists) comprehensions to create four web apps quickly and easily.

```fsharp
let webApps : IBuilder list = [
    for i in 1 .. 4 do
        webApp {
            name ("mywebapp-" + string i)
            link_to_service_plan plan
            link_to_app_insights ai
        }
]
```

The key parts to note are:

1. Use of `[ ]`, which in F# signify a *list* of some data.
2. Use of `for .. in .. do` syntax to iterate over numbers 1 to 4, assigning each value to `i`.
3. Creating unique names for each web app using simple string concatentation.
4. An explicit *type annotation* (: IBuilder list). Without getting into too much detail, this is needed because F# is somewhat stricter about implicit type conversions than other languages, particularly around list contra/covariance.

List comprehensions in F# are very powerful. You can use this approach with a specific set of names that are themselves a list as well e.g.

```fsharp
let planets = [ "jupiter"; "mars"; "pluto"; "venus" ]
let webApps = [
    for planet in planets do
        ...
]
```

#### Adding multiple resources to the template

Once you have created a list of web apps, you can add them all at once to the ARM builder using the `add_resources` keyword:

```fsharp
let template = arm {
    ...
    add_resources webApps
}
```

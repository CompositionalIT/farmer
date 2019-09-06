# Farmer

An F# DSL for rapidly generating non-complex ARM templates. This isn't a replacement for ARM templates,
nor some sort of compete offering with Pulumi or similar. It's designed mostly as an experiment at this stage -
things will change rapidly and there will be lots of breaking changes both in terms of namespace and API design.

**THIS IS PROTOTYPE CODE. USE AT YOUR OWN RISK.**

## What does it look like?

This is an example Farmer value:

```fsharp
/// Create a web application resource
let myWebApp = webApp {
    name (Literal "mysuperwebapp")
    service_plan_name (Literal "myserverfarm")
    sku WebApp.Skus.F1
    use_app_insights (Literal "myappinsights")
}

/// The overall ARM template which has the webapp as a resource.
let template = arm {
    location Locations.``North Europe``
    resource myWebApp
}

/// Export the template to a file.
template
|> Writer.toJson
|> Writer.toFile @"webapp-appinsights.json"
```

It does the following:

1. Creates a Web Application called `mysuperwebapp`.
2. Creates and links a service plan called `myserverfarm` with the F1 service tier.
4. Creates and links a fully configured Application Insights resource called `myappinsights`, and adds an app setting with the instrumentation key.
5. Embeds the web app into an ARM Template with location set to North Europe.
6. Converts the template into JSON and then writes it to disk.

## How can I help?
Try out the DSL and see what you think.

* Create as many issues as you can for both bugs
* Create suggestions for features and the most important elements you would like to see added

The is prototype code. There **will** be massive breaking changes on a regular basis.

## Getting started
1. Clone this repo
2. Build the Farmer project.
3. Try one of the sample scripts in the Samples folder.
4. Alternatively, use the SampleApp to generate your ARM templates from a console app.
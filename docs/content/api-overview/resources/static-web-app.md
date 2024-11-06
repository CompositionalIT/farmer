---
title: "Static Web Apps"
date: 2020-02-05T08:53:46+01:00
chapter: false
weight: 18
---

#### Overview
The Static Web App builder is used to create [Static Web Apps](https://azure.microsoft.com/en-us/services/app-service/static/). The Static Web App service is a modern web app service that offers streamlined full-stack development from source code to global high availability. You can use it to host static web applications and Azure Functions in a single resource, using GitHub native workflows to build and deploy your application.

* Static Site (`Microsoft.Web/staticSites`)

> At the time of writing, Static Web Apps are in public preview. Not all Azure locations support them.

#### Static Web App Builder Keywords
| Keyword | Purpose |
|-|-|
| name | Sets the name of the static web app. |
| repository | The URI of the GitHub repository containing your static web app. |
| artifact_location | The folder where the built web app is copied to e.g. `build` (optional) |
| api_location | The path containing your Azure Functions (optional) |
| app_location | The path containing your application code (optional) |
| branch | The branch that you which to use for the static web app (optional, defaults to 'master') |
| app_settings | Accepts a list of tuple strings representing key/value pairs for the app setting of the static web app |

#### Configuration Members
| Name | Purpose |
|-|-|
| RepositoryParameter | Provides the generated name for the repository token parameter name.

#### Parameters
| Name | Purpose |
|-|-|
| repositorytoken-for-`name` | Provides the Github Personal Access Token (PAT) required to authenticate and create the appropriate Github Action. |

#### Example

```fsharp
open Farmer
open Farmer.Builders

let myApp = staticWebApp {
    name "isaacsstatic"
    repository "https://github.com/isaacabraham/staticwebreact"
    artifact_location "build"
    api_location "api"
    app_settings [
        "key1", "value1"
        "key2", "value2"
    ]
}

let deployment = arm {
    location Location.WestEurope
    add_resource myApp
}

deployment
|> Deploy.execute "my-resource-group" [ myApp.RepositoryParameter, "Github personal access token goes here..." ]

```

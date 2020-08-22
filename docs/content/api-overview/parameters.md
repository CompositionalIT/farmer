---
title: "Parameters and Variables"
date: 2020-02-05T09:13:36+01:00
draft: false
weight: 2
---

ARM templates support the idea both of *parameterisation* of templates and of the use of *variables* within a template for e.g. placeholders and re-using values.

Farmer, by design, has only *limited support for parameters* and *no support for variables*. We don't plan on adding rich support for either of these for the following reasons:

* We want to keep the Farmer codebase simple for maintainers
* We want to keep the Farmer API simple for users
* We want to keep the generated ARM templates as readable as possible
* We feel that instead of trying to embed conditional logic and program flow directly inside ARM templates in JSON, if you wish to parameterise your template that you should use a real programming language to do that: in this case, F#.

You can read more on this issue [here](https://github.com/CompositionalIT/farmer/issues/8)

#### Secure Parameters
Farmer **does** support `securestring` parameters for e.g. SQL and Virtual Machine passwords - these are automatically generated based on the contents of the template rather than explicitly by yourself.

For example, assume the following Farmer SQL resource:

```fsharp
let db = sql {
    server_name "myserver"
    db_name "mydatabase"
    admin_username "mradmin"
}
```

This will generate an ARM template which looks as follows (irrelevant content is elided for clarity):

```json
{
  "$schema": "https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#",
  "parameters": {
    "password-for-myserver": {
      "type": "securestring"
    }
  },
  "resources": [
    {
      "apiVersion": "2014-04-01-preview",
      "name": "myserver",
      "properties": {
        "administratorLogin": "mradmin",
        "administratorLoginPassword": "[parameters('password-for-myserver')]",
      },
      "type": "Microsoft.Sql/servers"
    }
  ]
}
```

#### Working with variables
ARM templates allow you to declare [variables](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-variables) inside a template to reuse a value across a template. ARM templates also allow the use of a custom set of a commands which are embedded within strings to generate program logic, using *expressions* which contain [*template functions*](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions). For example, to concatenate a string inside a ARM template made up of two variables and put into a third variable, you might use something like the following:

```json
{
    "variables": {
        "first": "Hello",
        "second": "World",
        "serverName": "[concat(variables('first'), ' ', variables('second'), '!')]"
    }
}
```

In F#, you have access to the full power of .NET, rather than a limited set of weakly-typed functions:

```fsharp
let first = "Hello"
let second = "World"
let serverName = first + " " + second + "!"
let dbName = sprintf "%s %s!" first second

let db = sql {
    server_name serverName
    db_name dbName
    admin_username "mradmin"
}
```

#### Rapidly creating multiple resources
You can also use F# *list comprehensions* to rapidly create several resources of the same type:

```fsharp
// Create five SQL servers and databases
let myDatabases =
    [ for i in 1 .. 5 ->
        sql {
            server_name (sprintf "server%d" i)
            db_name (sprintf "database%d" i)
            admin_username "mradmin"
        }
    ]

// Add all five databases to the deployment
let deployment = arm {
    location Location.NorthEurope
    add_resources myDatabases
}
```
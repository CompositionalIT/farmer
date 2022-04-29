---
title: "Logic Apps"
date: 2022-04-27T00:55:30+02:00
chapter: false
weight: 4
---

#### Overview
The Logic App builder is used to create Azure Logic App Workflows.

* Workflows (`Microsoft.Logic/workflows`)

#### Builder keywords
| Keyword | Purpose |
|-|-|
| name | Sets the name of the workflow. |
| definition | Sets the file path (via `FileDefinition path`) or the definition directly (via `ValueDefinition value`) |
| add_tags | Adds tags to the script runtime resource. |
| add_tag | Adds a tag to the script runtime resource. |

#### Example

```fsharp
open Farmer
open Farmer.Builders

let emptyLogicApp =
    """
    {
        "definition": {
            "$schema": "https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#",
            "actions": {},
            "contentVersion": "1.0.0.0",
            "outputs": {},
            "parameters": {},
            "triggers": {}
        },
        "parameters": {}
    }
    """

let myValueLogicApp = logicApp {
    name "value-test-logic-app"
    definition (ValueDefinition emptyLogicApp)
    add_tags [("created-by", "farmer")]
}

let filepath = "./logicAppDefinition.json"

let myFileLogicApp = logicApp {
    name "file-test-logic-app"
    definition (FileDefinition filepath)
    add_tags [("created-by", "farmer")]
}

let deployment = arm {
    location Location.NorthCentralUS
    add_resource myValueLogicApp
    add_resource myFileLogicApp
}
```
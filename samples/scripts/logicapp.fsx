#r "../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.Arm.LogicApps

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
    add_tags [ ("environment", "dev"); ("created-by", "farmer") ]
}

let deployment = arm {
    location Location.CentralUS
    add_resource myValueLogicApp
}

deployment |> Writer.quickWrite "logicApp"
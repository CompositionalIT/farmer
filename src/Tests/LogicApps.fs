module LogicApps

open Expecto
open Farmer
open Farmer.Arm
open Farmer.Builders
open Farmer.Helpers
open Microsoft.Azure.Management.Logic
open Microsoft.Azure.Management.Logic.Models
open Microsoft.Rest
open System
open System.Text.Json

let dummyClient = new LogicManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let asAzureResource (lac: LogicAppConfig) =
  arm { add_resource lac }
  |> findAzureResources<Workflow> dummyClient.SerializationSettings
  |> List.head
  |> fun r ->
      r.Validate()
      r

let tests = testList "Logic Apps" [
    test "Creates a logic app workflow" {
        let config =
            logicApp {
                name "test-logic-app"
            }
        let workflow = asAzureResource config

        Expect.equal workflow.Name "test-logic-app" "Incorrect workflow name"
    }
    test "Populates a value-based logic app definition" {
        // this is the required bare minimum for an empty logic app
        // for it to not be set to "null" after parsing
        let value =
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
        let config = 
            logicApp {
                name "test-logic-app"
                definition (ValueDefinition value)
            }
        let workflow = asAzureResource config

        Expect.isNotNull workflow.Definition "Did not set logic app definition"
    }
    test "Populates a file-based logic app definition" {
        let path = "./test-data/blank-logic-app.json"
        let config = 
            logicApp {
                name "test-logic-app"
                definition (FileDefinition path)
            }
        let workflow = asAzureResource config
        Expect.isNotNull workflow.Definition "Did not load definition from file"
    }
]
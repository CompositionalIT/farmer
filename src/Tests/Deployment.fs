module Deployment

open Expecto
open Farmer
open Farmer.Builders.AppInsights
open Farmer.CoreTypes
open Newtonsoft.Json.Linq

let tests = testList "Deployment" [
    test "Defaults to ResourceGroup Scope" {
        let deployment = arm { location Location.WestEurope }
        Expect.equal deployment.Template.DeploymentScope ResourceGroup "Incorrect scope"
    }
    
    test "Can set subscription scope" {
        let deployment = arm { location Location.WestEurope; scope Subscription }
        Expect.equal deployment.Template.DeploymentScope Subscription "Incorrect scope"
    }
    test "Has correct schema for ResourceGroup" {
        let deployment = arm { location Location.WestEurope; scope ResourceGroup }
        let template = 
            deployment.Template
            |> Writer.toJson
            |> JObject.Parse
        Expect.equal (template.SelectToken("$.$schema").Value<string> ()) "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#" "Incorrect schema"
    }

    test "Has correct schema for Subscription" {
        let deployment = arm { location Location.WestEurope; scope Subscription }
        let template = 
            deployment.Template
            |> Writer.toJson
            |> JObject.Parse
        Expect.equal (template.SelectToken("$.$schema").Value<string> ()) "https://schema.management.azure.com/schemas/2018-05-01/subscriptionDeploymentTemplate.json#" "Incorrect schema"
    }
]
module ResourceGroup

open Expecto
open Farmer
open Farmer.Arm.ResourceGroup
open Newtonsoft.Json.Linq
open Farmer.Builders

let private getJObject (r:#IBuilder) = 
    r.BuildResources Location.WestEurope
    |> List.map (fun x -> x.JsonModel)
    |> JArray.FromObject

let private getValue<'a> path (jo:JToken) = 
    jo.SelectToken(path).Value<'a>()

let tests = testList "ResourceGroup" [
    test "Generates resource group" {
        let rg = resourceGroup { name "my-resgroup"; location Location.CentralUS } |> getJObject
        Expect.hasLength (rg.SelectTokens "$.[*]") 1 "Incorrect number of resources"
        Expect.equal (getValue<string> "$.[0].name" rg) "my-resgroup" "Incorrect name"
        Expect.equal (getValue<string> "$.[0].type" rg) resourceGroups.Type "Incorrect resource type"
        Expect.equal (getValue<string> "$.[0].apiVersion" rg) resourceGroups.ApiVersion"Incorrect api version"
        Expect.equal (getValue<string> "$.[0].location" rg) "centralus" "Incorrect location"
    }
    test "Generates resource group deployment" {
        let stg = storageAccount { name "stg1" }
        let rg = 
            resourceGroup { 
              name "my-resgroup"
              location Location.CentralUS
              add_resource stg }
            |> getJObject
        Expect.hasLength (rg.SelectTokens "$.[*]") 2 "Incorrect number of resources"
        Expect.equal (getValue<string> "$.[1].type" rg) deployments.Type "Incorrect type"
        Expect.equal (getValue<string> "$.[1].apiVersion" rg) deployments.ApiVersion "Incorrect api version"
        Expect.equal (getValue<string> "$.[1].resourceGroup" rg) "my-resgroup" "Incorrect api version"
        Expect.hasLength (getValue<JArray> "$.[1].dependsOn" rg) 1 "Incorrect dependsOn count"
        Expect.equal (getValue<string> "$.[1].dependsOn.[0]" rg) "[resourceId('Microsoft.Resources/resourceGroups', 'my-resgroup')]" "Incorrect dependsOn"
        Expect.equal (getValue<string> "$.[1].properties.mode" rg) "Incremental" "Incorrect mode"
        Expect.equal (getValue<string> "$.[1].properties.template.$schema" rg) "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#" "Incorrect nested template schema"
        Expect.equal (getValue<string> "$.[1].properties.template.contentVersion" rg) "1.0.0.0" "Incorrect nested content version"
        Expect.equal (getValue<string> "$.[1].properties.template.resources[0].type" rg) "Microsoft.Storage/storageAccounts" "Incorrect api version"
        Expect.equal (getValue<string> "$.[1].properties.template.resources[0].name" rg) "stg1" "Incorrect storage account name"
    }
]
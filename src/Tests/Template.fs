module Template

open Farmer
open Farmer.Builders
open Expecto
open Newtonsoft.Json

module TestHelpers =
    let createSimpleDeployment parameters =
        { Location = NorthEurope
          PostDeployTasks = []
          Template = {
              Outputs = []
              Parameters = parameters |> List.map SecureParameter
              Resources = []
          }
        }
    let convertTo<'T> = JsonConvert.SerializeObject >> JsonConvert.DeserializeObject<'T>

open TestHelpers

let toTemplate (deployment:Deployment) =
    deployment.Template
    |> Writer.TemplateGeneration.processTemplate

let tests = testList "Template" [
    test "Can create a basic template" {
        let template = arm { location NorthEurope } |> toTemplate
        Expect.equal template.``$schema`` "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#" ""
        Expect.isEmpty template.outputs ""
        Expect.isEmpty template.parameters ""
        Expect.isEmpty template.resources ""
    }
    test "Correctly generates outputs" {
        let template =
            arm { location NorthEurope; output "p1" "v1"; output "p2" "v2" }
            |> toTemplate
        Expect.equal template.outputs.["p1"].value "v1" ""
        Expect.equal template.outputs.["p2"].value "v2" ""
        Expect.equal template.outputs.Count 2 ""
    }
    test "Processes parameters correctly" {
        let template = createSimpleDeployment [ "p1"; "p2" ] |> toTemplate

        Expect.equal template.parameters.["p1"].``type`` "securestring" ""
        Expect.equal template.parameters.["p2"].``type`` "securestring" ""
        Expect.equal template.parameters.Count 2 ""
    }

    test "Can create a single resource" {
        let template = arm {
            add_resource (storageAccount { name "test" })
        }

        Expect.equal template.Template.Resources.Length 1 "Should be a single resource"
    }

    test "Can create multiple resources simultaneously" {
        let template = arm {
            add_resources [
                storageAccount { name "test" }
                storageAccount { name "test2" }
            ]
        }

        Expect.equal template.Template.Resources.Length 2 "Should be two resources"
    }

    test "De-dupes the same resource name and type" {
        let template = arm {
            add_resources [
                storageAccount { name "test" }
                storageAccount { name "test" }
            ]
        }

        Expect.equal template.Template.Resources.Length 1 "Should be a single resource"
    }

    test "Does not de-dupe the same resource name but different type" {
        let template = arm {
            add_resources [
                storageAccount { name "test" }
                cognitiveServices { name "test" }
            ]
        }

        Expect.equal template.Template.Resources.Length 2 "Should be two resources"
    }

    test "Location is cascaded to all resources" {
        let template = arm {
            location NorthCentralUS
            add_resources [
                storageAccount { name "test" }
                storageAccount { name "test2" }
            ]
        }

        let allLocations = template.Template.Resources |> List.map (fun r -> r.ToArmObject() |> convertTo<{| Location : string |}>)
        Expect.sequenceEqual allLocations [ {| Location = NorthCentralUS.ArmValue |}; {| Location = NorthCentralUS.ArmValue |} ] "Incorrect Location"
    }

    test "Secure parameter is correctly added" {
        let template = arm {
            add_resource (vm { name "isaacvm" })
        }
        Expect.sequenceEqual template.Template.Parameters [ SecureParameter "password-for-isaacvm" ] "Missing parameter for VM."
    }

    test "Outputs are correctly added" {
        let template = arm {
            output "foo" "bar"
            output "foo" "baz"
            output "bar" "bop"
        }
        Expect.sequenceEqual template.Template.Outputs [ "bar", "bop"; "foo", "baz" ] "Outputs should work like a key/value store"
    }
]
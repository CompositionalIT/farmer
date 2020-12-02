module Template

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Arm
open Newtonsoft.Json

[<AutoOpen>]
module TestHelpers =
    let createSimpleDeployment parameters =
        { Location = Location.NorthEurope
          PostDeployTasks = []
          Template = {
              Outputs = []
              Parameters = parameters |> List.map SecureParameter
              Resources = []
          }
        }
    let convertTo<'T> = JsonConvert.SerializeObject >> JsonConvert.DeserializeObject<'T>


let toTemplate (deployment:Deployment) =
    deployment.Template
    |> Writer.TemplateGeneration.processTemplate

let tests = testList "Template" [
    test "Can create a basic template" {
        let template = arm { location Location.NorthEurope } |> toTemplate
        Expect.equal template.``$schema`` "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#" ""
        Expect.isEmpty template.outputs ""
        Expect.isEmpty template.parameters ""
        Expect.isEmpty template.resources ""
    }
    test "Correctly generates outputs" {
        let template =
            arm { location Location.NorthEurope; output "p1" "v1"; output "p2" "v2" }
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
            location Location.NorthCentralUS
            add_resources [
                storageAccount { name "test" }
                storageAccount { name "test2" }
            ]
        }

        let allLocations = template.Template.Resources |> List.map (fun r -> r.JsonModel |> convertTo<{| Location : string |}>)
        Expect.sequenceEqual allLocations [ {| Location = Location.NorthCentralUS.ArmValue |}; {| Location = Location.NorthCentralUS.ArmValue |} ] "Incorrect Location"
    }

    test "Secure parameter is correctly added" {
        let template = arm {
            add_resource (vm { name "isaacvm"; username "foo" })
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

    test "Can add a list of resources types together" {
        let resources : IBuilder list = [
            storageAccount { name "test" }
            storageAccount { name "test2" }
        ]
        let template = arm {
            add_resources resources
        }
        Expect.hasLength template.Template.Resources 2 "Should be two resources added"
    }

    test "Can add dependency through IBuilder" {
        let a = storageAccount { name "aaa" }
        let b = webApp { name "b"; depends_on a }

        Expect.equal b.Dependencies (Set [ storageAccounts.resourceId "aaa" ]) "Dependency should have been set"
    }

    test "Can add dependencies through IBuilder" {
        let a = storageAccount { name "aaa" } :> IBuilder
        let b = storageAccount { name "bbb" } :> IBuilder
        let b = webApp { name "b"; depends_on [ a; b ] }

        Expect.equal b.Dependencies (Set [ storageAccounts.resourceId "aaa"; storageAccounts.resourceId "bbb" ]) "Dependencies should have been set"
    }

    test "Generates untyped Resource Id" {
        let rid = ResourceId.create (ResourceType.ResourceType("", ""), ResourceName "test")
        let id = rid.Eval()
        Expect.equal id "test" "resourceId template function should match"
    }

    test "Generates typed Resource Id" {
        let rid = connections.resourceId "test"
        let id = rid.Eval()
        Expect.equal id "[resourceId('Microsoft.Network/connections', 'test')]" "resourceId template function should match"
    }

    test "Generates typed Resource Id with group" {
        let rid = ResourceId.create (Arm.Network.connections, ResourceName "test", "myGroup")
        let id = rid.Eval()
        Expect.equal id "[resourceId('myGroup', 'Microsoft.Network/connections', 'test')]" "resourceId template function should match"
    }

    test "Generates typed Resource Id with segments" {
        let rid = ResourceId.create (Arm.Network.connections, ResourceName "test", ResourceName "segment1", ResourceName "segment2")
        let id = rid.Eval()
        Expect.equal id "[resourceId('Microsoft.Network/connections', 'test', 'segment1', 'segment2')]" "resourceId template function should match"
    }

    test "Fails if ARM expression is already quoted" {
        Expect.throws(fun () -> ArmExpression.create "[test]" |> ignore ) ""
    }

    test "Correctly strips a literal expression" {
        Expect.equal ((ArmExpression.literal "test").Eval()) "test" ""
    }

    test "Does not fail if ARM expression contains an inner quote" {
        Expect.equal "[foo[test]]" ((ArmExpression.create "foo[test]").Eval()) ""
    }
    test "Does not create empty nodes for core resource fields when nothing is supplied" {
        let createdResource = ResourceType("Test", "2017-01-01").Create(ResourceName "Name")
        Expect.equal
            createdResource
            {| name = "Name"; ``type`` = "Test"; apiVersion = "2017-01-01"; dependsOn = null; location = null; tags = null |}
            "Default values don't match"
    }

    testList "ARM Writer Regression Tests" [
        test "Generates lots of resources" {
            let number = string 1979

            let sql = sqlServer { name ("farmersql" + number); admin_username "farmersqladmin"; add_databases [ sqlDb { name "farmertestdb"; use_encryption } ]; enable_azure_firewall }
            let storage = storageAccount { name ("farmerstorage" + number) }
            let web = webApp { name ("farmerwebapp" + number); add_extension WebApp.Extensions.Logging }
            let fns = functions { name ("farmerfuncs" + number) }
            let svcBus = serviceBus { name ("farmerbus" + number); sku ServiceBus.Sku.Standard; add_queues [ queue { name "queue1" } ]; add_topics [ topic { name "topic1"; add_subscriptions [ subscription { name "sub1" } ] } ] }
            let cdn = cdn { name ("farmercdn" + number); add_endpoints [ endpoint { name ("farmercdnendpoint" + number); origin storage.WebsitePrimaryEndpointHost } ] }

            let deployment = arm {
                location Location.NorthEurope
                add_resources [ sql; storage; web; fns; svcBus; cdn ]
            }

            let path = "./test-data/farmer-integration-test-1.json"
            let expected = System.IO.File.ReadAllText path
            let actual = deployment.Template |> Writer.toJson

            Expect.equal expected actual (sprintf "ARM template generation has changed! Either fix the writer, or update the contents of the generated file (%s)" path)
        }
    ]
]
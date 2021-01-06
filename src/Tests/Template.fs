module Template

open System
open Expecto
open Farmer
open Farmer.Builders
open Farmer.Arm
open Newtonsoft.Json
open Microsoft.Rest
open Microsoft.Azure.Management.ResourceManager
open Microsoft.Azure.Management.ResourceManager.Models
open Microsoft.Azure.Management.Resources.Models
open Newtonsoft.Json
open System.IO

[<AutoOpen>]
module TestHelpers =
    let createSimpleDeployment parameters =
        { Location = Location.NorthEurope
          PostDeployTasks = []
          Template = {
              Schema = Arm.ResourceGroup.schema
              Outputs = []
              Parameters = parameters |> List.map SecureParameter
              Resources = []
          }
        }
    let convertTo<'T> = JsonConvert.SerializeObject >> JsonConvert.DeserializeObject<'T>

let dummyClient = new ResourceManagementClient (Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let createSimpleTemplate parameters = 
    (createSimpleDeployment parameters).Template
    |> Writer.TemplateGeneration.processTemplate

let toTemplate deployment =
    Deployment.getTemplate "farmer-resources"  deployment
    |> Writer.TemplateGeneration.processTemplate

let isType<'t> (o:obj) =
    match o with
    | :? 't -> true
    | _ -> false

let tests = testList "Template" [
    test "Can create a basic template" {
        let arm = arm { location Location.NorthEurope } 
        let template = arm |> toTemplate
        let resources = arm |> findAzureResources<ResourceGroup> dummyClient.SerializationSettings
        Expect.hasLength resources 1 ""
        Expect.equal resources.[0].Location Location.NorthEurope.ArmValue ""
        Expect.isEmpty template.outputs "outputs should be empty"
        Expect.isEmpty template.parameters "parameters should be empty"
    }
    test "Correctly generates outputs" {
        let arm = arm { location Location.NorthEurope; output "p1" "v1"; output "p2" "v2"; add_resource (storageAccount{name "stg"}) }
        let template = arm |> toTemplate |> Newtonsoft.Json.Linq.JObject.FromObject
        let getValue = template.SelectToken >> string
        let getTokens = template.SelectTokens
        Expect.equal (getValue "$.resources.[1].properties.template.outputs.p1.value") "v1" ""
        Expect.equal (getValue "$.resources.[1].properties.template.outputs.p2.value")  "v2" ""
        Expect.hasLength (getTokens "$.resources.[1].properties.template.outputs.*") 2 ""

        let nestedDeplName = getValue "$.resources.[1].name"
        Expect.equal (getValue "$.outputs.p1.type") "string" ""
        Expect.equal (getValue "$.outputs.p1.value") (sprintf "[reference('%s').outputs.p1.value]" nestedDeplName) ""
        Expect.equal (getValue "$.outputs.p2.type") "string" ""
        Expect.equal (getValue "$.outputs.p2.value") (sprintf "[reference('%s').outputs.p2.value]" nestedDeplName)  ""
    }
    test "Processes parameters correctly" {
        let template = createSimpleTemplate [ "p1"; "p2" ]

        Expect.equal template.parameters.["p1"].``type`` "securestring" ""
        Expect.equal template.parameters.["p2"].``type`` "securestring" ""
        Expect.equal template.parameters.Count 2 ""
    }

    test "Can create a single resource" {
        let resources =
            arm {
                add_resource (storageAccount { name "test" })
            }
            |> getResources
        Expect.hasLength resources 2 "Should have two resources"
        Expect.isTrue (isType<Arm.ResourceGroup.ResourceGroup> resources.[0]) "Should be ResourceGroup"
        Expect.isTrue (isType<Arm.Storage.StorageAccount> resources.[1]) "Should be StorageAccount"
    }

    test "Can create multiple resources simultaneously" {
        let template = arm {
            add_resources [
                storageAccount { name "test" }
                storageAccount { name "test2" }
            ]
        }
        let storages = template |> findAzureResourcesByType<obj> Arm.Storage.storageAccounts (JsonSerializerSettings())

        Expect.hasLength storages 2 "Should be two resources"
    }

    test "De-dupes the same resource name and type" {
        let template = arm {
            add_resources [
                storageAccount { name "test" }
                storageAccount { name "test" }
            ]
        }
        let storages = template |> findAzureResourcesByType<obj> Arm.Storage.storageAccounts (JsonSerializerSettings())

        Expect.hasLength storages 1 "Should be a single resource"
    }

    test "Does not de-dupe the same resource name but different type" {
        let template = arm {
            add_resources [
                storageAccount { name "test" }
                cognitiveServices { name "test" }
            ]
        }
        let resources = 
            template 
            |> getResources
            |> List.filter (function | :? Arm.ResourceGroup.ResourceGroup -> false | _ -> true)

        Expect.hasLength resources 2 "Should be two resources"
    }

    test "Location is cascaded to all resources" {
        let template = arm {
            location Location.NorthCentralUS
            add_resources [
                storageAccount { name "test" }
                storageAccount { name "test2" }
            ]
        }
        let allLocations = 
            template
            |> getResources 
            |> List.map (fun r -> r.JsonModel |> convertTo<{| Location : string |}>)
        
        Expect.allEqual allLocations {| Location = Location.NorthCentralUS.ArmValue |} "Incorrect Location"
    }

    test "Secure parameter is correctly added" {
        let template = arm {
            add_resource (vm { name "isaacvm"; username "foo" })
        }
        Expect.sequenceEqual (Deployment.getTemplate "farmer-resources" template).Parameters [ SecureParameter "password-for-isaacvm" ] "Missing parameter for VM."
    }

    test "Outputs overwrite earlier outputs of the same name" {
        let arm = arm {
            output "foo" "bar"
            output "foo" "baz"
            output "bar" "bop"
        }
        let template = arm |> toTemplate |> Newtonsoft.Json.Linq.JObject.FromObject
        let getValue = template.SelectToken >> string
        let getTokens = template.SelectTokens
        
        Expect.equal (getValue "$.resources.[1].properties.template.outputs.foo.value") "baz" ""
        Expect.equal (getValue "$.resources.[1].properties.template.outputs.bar.value")  "bop" ""
    }

    test "Can add a list of resources types together" {
        let resources : IBuilder list = [
            storageAccount { name "test" }
            storageAccount { name "test2" }
        ]
        let template = arm {
            add_resources resources
        }
        Expect.hasLength (Deployment.getTemplate "farmer-resources" template).Resources 2 "Should be two resources added"
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
]
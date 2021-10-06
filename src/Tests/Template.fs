module Template

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Arm
open TestHelpers
open Microsoft.Azure.Management.ResourceManager
open Microsoft.Rest
open System
open Newtonsoft.Json.Linq

let toTemplate (deployment:IDeploymentSource) =
    deployment.Deployment.Template
    |> Writer.TemplateGeneration.processTemplate

let dummyClient = new ResourceManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

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

    test "Can add a list of arm resources types together" {
        let web = webApp {
            name "test"
            system_identity
        }
        let roleAssignment =
            {  Name = "r2" |> ResourceName
               RoleDefinitionId = Roles.DNSZoneContributor
               PrincipalId = web.SystemIdentity.PrincipalId
               PrincipalType = Arm.RoleAssignment.PrincipalType.MSI
               Scope = Arm.RoleAssignment.AssignmentScope.ResourceGroup 
               Dependencies = Set.ofList [ web.ResourceId ] }

        let roleAssignment2 =
            {  Name = "r1" |> ResourceName
               RoleDefinitionId = Roles.DNSZoneContributor
               PrincipalId = web.SystemIdentity.PrincipalId
               PrincipalType = Arm.RoleAssignment.PrincipalType.ServicePrincipal
               Scope = Arm.RoleAssignment.AssignmentScope.ResourceGroup 
               Dependencies = Set.ofList [ web.ResourceId ] }
        
        let resources : IArmResource list = [
            roleAssignment
            roleAssignment2
        ]

        let template = arm {
            add_arm_resources resources
        }

        Expect.hasLength template.Template.Resources 2 "Should be two resources added"
    }

    test "Can add dependency through IBuilder" {
        let a = storageAccount { name "aaa" }
        let b = webApp { name "testweb"; depends_on a }

        Expect.equal b.Dependencies (Set [ storageAccounts.resourceId "aaa" ]) "Dependency should have been set"
    }

    test "Can add dependencies through IBuilder" {
        let a = storageAccount { name "aaa" } :> IBuilder
        let b = storageAccount { name "bbb" } :> IBuilder
        let b = webApp { name "testweb"; depends_on [ a; b ] }

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
    test "Can nest resource groups" {
        let template =
            arm  {
                add_resource (resourceGroup {
                    name "inner"
                    add_resource (storageAccount { name "storage" })
                    add_tag "deployment-tag" "inner-rg"
                })
            }

        Expect.hasLength template.Template.Resources 1 "Outer template should contain only nested deployment"
        Expect.isTrue (template.Template.Resources.[0] :? Arm.ResourceGroup.ResourceGroupDeployment) "The only resource should be a resourceGroupDeployment"
        let innerDeployment = template.Template.Resources.[0] :?> Arm.ResourceGroup.ResourceGroupDeployment
        Expect.hasLength innerDeployment.Resources 1 "Inner template should have 1 resource"
        Expect.equal innerDeployment.TargetResourceGroup.Value "inner" "Inner template name is incorrect"
        Expect.isTrue (innerDeployment.Template.Resources.[0] :? Arm.Storage.StorageAccount) "The only resource in the inner deployment should be a storageAccount"
    }
    test "Nested resource group outputs are copied to outer deployments" {
        let inner1 = resourceGroup { name "inner1"; output "foo" "bax" }
        let inner2 = resourceGroup { name "inner2"; output "foo" "bay" }
        let outer = arm  {
            add_resource inner1
            add_resource inner2
            output "foo" "baz" 
        }

        Expect.hasLength outer.Template.Outputs 3 "inner outputs should copy to outer template"
        Expect.equal outer.Template.Outputs.[0] ("foo","baz") "output expression was incorrect"
        Expect.equal outer.Template.Outputs.[1] ("inner1.foo","[reference('inner1').outputs['foo'].value]") "output expression was incorrect"
        Expect.equal outer.Template.Outputs.[2] ("inner2.foo","[reference('inner2').outputs['foo'].value]") "output expression was incorrect"
    }
    test "Nested resource group can accept parameters" {
        let inner1 = resourceGroup { 
            name "inner1"
            add_resource (vm { name "vm"; username "foo" })
        }
        let outer = arm  {
            add_resource inner1
        }

        Expect.hasLength inner1.Template.Parameters 1 "inner template should have a parameter"
        Expect.hasLength outer.Template.Parameters 1 "inner parameters should copy to outer template"
        Expect.equal outer.Template.Parameters.[0] (SecureParameter "password-for-vm") "Parameter specification was incorrect"
    }
    test "Parameter value are copied to nested resource group deployment" {
        let inner1 = resourceGroup { 
            name "inner1"
            add_resource (vm { name "vm"; username "foo" })
        }
        let outer = arm  {
            add_resource inner1
        }
        
        let deployment = outer |> findAzureResources<Models.Deployment> dummyClient.SerializationSettings
        let nestedParamsObj = deployment.[0].Properties.Parameters :?> JObject
        let nestedParams = 
            nestedParamsObj.Properties()
            |> Seq.map (fun x -> x.Name, x.Value.SelectToken(".value").ToString())
            |> Map.ofSeq

        Expect.equal nestedParams.["password-for-vm"] "[parameters('password-for-vm')]" "Parameters not correctly proxied to nested template"
    }
]
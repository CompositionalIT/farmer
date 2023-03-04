module ResourceGroup

open Expecto
open Farmer
open Farmer.Arm.ResourceGroup
open Farmer.Builders

let tests =
    testList "Resource Group" [
        test "Creates a resource group" {
            let rg = createResourceGroup "myRg" Location.EastUS
            Expect.equal rg.Name.Value "myRg" "Incorrect name on resource group"
            Expect.equal rg.Location Location.EastUS "Incorrect location on resource group"
            Expect.equal rg.Dependencies Set.empty "Resource group should have no dependencies"
            Expect.equal rg.Tags Map.empty "Resource group should have no tags"
        }
        test "Supports multiple nested deployments to the same resource group" {
            let nestedRgs =
                [ 1..3 ]
                |> List.map (fun i ->
                    resourceGroup {
                        name "target-rg"
                        add_resource (storageAccount { name $"stg{i}" })
                    }
                    :> IBuilder)

            let rg = resourceGroup {
                name "outer-rg"
                add_resources nestedRgs
            }

            Expect.hasLength rg.Template.Resources 3 "all three resource groups should be added"

            let nestedResources =
                rg.Template.Resources
                |> List.map (fun x -> x :?> ResourceGroupDeployment)
                |> List.collect (fun x -> x.Resources)
                |> List.map (fun x -> x.ResourceId.Name.Value)

            Expect.equal nestedResources [ "stg1"; "stg2"; "stg3" ] "all three storage accounts should be nested"
        }
        test "zip_deploy should be performed when declared in a nested resource" {
            let webApp = webApp {
                name "webapp"
                zip_deploy "deploy"
            }

            let oneNestedLevel = resourceGroup { add_resource webApp }
            let twoNestedLevels = resourceGroup { add_resource oneNestedLevel }
            let threeNestedLevels = arm { add_resource twoNestedLevels }

            Expect.isNonEmpty
                (threeNestedLevels :> IDeploymentSource).Deployment.PostDeployTasks
                "The zip_deploy should create a post deployment task"
        }
    ]

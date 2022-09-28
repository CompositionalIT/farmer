module DedicatedHosts

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Vm
open Microsoft.Azure.Management.Compute
open Microsoft.Azure.Management.Compute.Models
open Microsoft.Rest
open System
open Newtonsoft.Json.Linq

/// Client instance needed to get the serializer settings.
let client =
    new ComputeManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let tests =
    testList
        "Dedicated Hosts"
        [
            test "Can create a basic dedicated host group" {
                let deployment =
                    arm {
                        location Location.EastUS
            
                        add_resources
                            [
                                hostGroup {
                                    name "myhostgroup"
                                }
                            ]
                    }
            
                let jobj = deployment.Template |> Writer.toJson |> JObject.Parse
                let a = jobj.ToString()
            
                let hostGroup =
                    jobj.SelectToken "resources[?(@.type=='Microsoft.Compute/hostGroups')]"
            
                let hostGroupProps = hostGroup.["properties"]
            
                let supportAutomaticPlacement: bool =
                    JToken.op_Explicit hostGroupProps.["supportAutomaticPlacement"]
                let ultraSsdEnabled: bool =
                    JToken.op_Explicit hostGroupProps.["supportAutomaticPlacement"]
                    
                Expect.equal supportAutomaticPlacement false "Incorrect default value for supportAutomaticPlacement"
                Expect.equal ultraSsdEnabled false "Incorrect default value for ultraSsdEnabled"
            }
        ]
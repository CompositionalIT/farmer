module OperationsManagement

open System
open Expecto
open Farmer
open Farmer.Builders
open Newtonsoft.Json.Linq

let tests =
    testList "Operations Management Solution" [
        test "Generates an operations management soluution" {

            let sentinelWorkspace = logAnalytics {
                name "my-sentinel-workspace"
                retention_period 30<Days>
                enable_query
                daily_cap 5<Gb>
            }

            let omsName = $"SecurityInsights({sentinelWorkspace.Name.Value})"

            let sentinelSolution = oms {
                name omsName

                plan (
                    omsPlan {
                        name omsName
                        publisher "Microsoft"
                        product "OMSGallery/SecurityInsights"
                    }
                )

                properties (omsProperties { workspace sentinelWorkspace })
            }

            let deployment = arm {
                location Location.EastUS
                add_resources [ sentinelWorkspace; sentinelSolution ]
            }

            let jobj = deployment.Template |> Writer.toJson |> JObject.Parse

            let workspaceResource =
                jobj.SelectToken("resources[?(@.name=='my-sentinel-workspace')]")

            Expect.equal
                (string (workspaceResource.["type"]))
                "Microsoft.OperationalInsights/workspaces"
                "Incorrect type for OMS workspace"

            let workspaceProperties = workspaceResource.["properties"]

            Expect.equal
                (string (workspaceProperties.["publicNetworkAccessForQuery"]))
                "Enabled"
                "Incorrect public network access"

            Expect.equal (int (workspaceProperties.["retentionInDays"])) 30 "Incorrect retention"

            let solutionResource =
                jobj.SelectToken("resources[?(@.name=='SecurityInsights(my-sentinel-workspace)')]")

            Expect.equal
                (string (solutionResource.["type"]))
                "Microsoft.OperationsManagement/solutions"
                "Incorrect type for OMS solution"

            Expect.hasLength
                (solutionResource.["dependsOn"] :?> JArray)
                1
                "oms solution has incorrect number of dependencies"

            Expect.equal
                (string (solutionResource.["dependsOn"].[0]))
                "[resourceId('Microsoft.OperationalInsights/workspaces', 'my-sentinel-workspace')]"
                "oms solution has incorrect dependency"

            let solutionProperties = solutionResource.["properties"]

            Expect.equal
                (string (solutionProperties.["workspaceResourceId"]))
                "[resourceId('Microsoft.OperationalInsights/workspaces', 'my-sentinel-workspace')]"
                "Incorrect solution workspace resource Id"
        }
    ]

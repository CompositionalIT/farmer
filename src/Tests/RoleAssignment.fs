module RoleAssignment

open Expecto
open Farmer
open Farmer.Arm

let tests =
    testList
        "RoleAssignment"
        [
            test "Produces opaque resource scope" {
                let actual : IArmResource = 
                    { Name = ResourceName "assignment"
                      RoleDefinitionId = Roles.Contributor
                      PrincipalId = ArmExpression.create "1" |> PrincipalId
                      PrincipalType = PrincipalType.User
                      Scope = privateClouds.resourceId "mySDDC" |> UnmanagedResource 
                      Dependencies = Set.empty }

                "Expected matching scope"
                |> Expect.stringContains (Newtonsoft.Json.JsonConvert.SerializeObject actual.JsonModel) "\"scope\":\"[resourceId('Microsoft.AVS/privateClouds', 'mySDDC')]\""
            }
        ]

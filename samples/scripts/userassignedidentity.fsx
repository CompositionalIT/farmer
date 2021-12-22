#r @"..\..\src\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.Arm.Network

let myIdentity =
    userAssignedIdentity {
        name "my-test-identity"
        add_to_ad_group "my-aad-group"
    }


arm {
    add_resource myIdentity
    //add_resource bastion
}
|> Deploy.execute "aad-identity-group" []
//|> Writer.quickWrite "hub-and-spoke"

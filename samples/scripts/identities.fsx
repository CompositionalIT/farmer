#r @"..\..\src\Farmer\bin\Debug\net5.0\Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.Arm.Network

let myIdentity =
    userAssignedIdentity {
        name "rsp-test-identity"
        add_to_group "codat-integration-apps"
    }


arm {
    add_resource myIdentity
    //add_resource bastion
}
|> Deploy.execute "rsp-test-group" []
//|> Writer.quickWrite "hub-and-spoke"

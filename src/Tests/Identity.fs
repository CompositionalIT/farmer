module Identity

open Expecto
open Farmer.Identity
open Farmer.CoreTypes

let tests = testList "Identity" [
    test "Can add two identities together" {
        let systemOnly = { ManagedIdentity.Empty with SystemAssigned = Enabled }
        let userOnlyA = { ManagedIdentity.Empty with UserAssigned = [ UserAssignedIdentity(ResourceId.create "a") ] }
        let userOnlyB = { ManagedIdentity.Empty with UserAssigned = [ UserAssignedIdentity(ResourceId.create "b") ] }

        Expect.isTrue (userOnlyA + systemOnly).SystemAssigned.AsBoolean "Should have System Assigned on"
        Expect.sequenceEqual
            (userOnlyA + userOnlyB).UserAssigned
            [ UserAssignedIdentity(ResourceId.create "a"); UserAssignedIdentity(ResourceId.create "b") ]
            "User Assigned not added correctly"
        Expect.sequenceEqual
            (userOnlyA + userOnlyA).UserAssigned
            [ UserAssignedIdentity(ResourceId.create "a") ]
            "User Assigned duplicates exist"
    }
]
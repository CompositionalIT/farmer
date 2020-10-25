module Identity

open Expecto
open Farmer
open Farmer.Arm
open Farmer.Identity

let tests = testList "Identity" [
    test "Can add two identities together" {
        let systemOnly = { ManagedIdentity.Empty with SystemAssigned = Enabled }
        let userOnlyA = { ManagedIdentity.Empty with UserAssigned = [ UserAssignedIdentity(userAssignedIdentities.createResourceId "a") ] }
        let userOnlyB = { ManagedIdentity.Empty with UserAssigned = [ UserAssignedIdentity(userAssignedIdentities.createResourceId "b") ] }

        Expect.isTrue (userOnlyA + systemOnly).SystemAssigned.AsBoolean "Should have System Assigned on"
        Expect.sequenceEqual
            (userOnlyA + userOnlyB).UserAssigned
            [ UserAssignedIdentity(userAssignedIdentities.createResourceId "a"); UserAssignedIdentity(userAssignedIdentities.createResourceId "b") ]
            "User Assigned not added correctly"
        Expect.sequenceEqual
            (userOnlyA + userOnlyA).UserAssigned
            [ UserAssignedIdentity(userAssignedIdentities.createResourceId "a") ]
            "User Assigned duplicates exist"
    }
    test "Creates ARM JSON correctly" {
        let json = ManagedIdentity.Empty |> ManagedIdentity.toArmJson
        Expect.equal json.``type`` "None" "Should be empty json"
        Expect.isNull json.userAssignedIdentities "Should be empty json"

        let testIdentity = userAssignedIdentities.createResourceId "test" |> ManagedIdentity.create

        let json = testIdentity |> ManagedIdentity.toArmJson
        Expect.equal json.``type`` "UserAssigned" "Should be user assigned"
        Expect.sequenceEqual (json.userAssignedIdentities |> Seq.map(fun s -> s.Key)) [ "[resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', 'test')]" ]  "Should be single UAI"
        Expect.equal (json.userAssignedIdentities |> Seq.map(fun r -> r.Value.GetType()) |> Seq.head) typeof<obj> "Should be an object"

        let json = { SystemAssigned = Enabled; UserAssigned = [] } |> ManagedIdentity.toArmJson
        Expect.equal json.``type`` "SystemAssigned" "Wrong type"
        Expect.isNull json.userAssignedIdentities "Wrong identities"

        let json =
            let testIdentity2 = userAssignedIdentities.createResourceId "test2" |> ManagedIdentity.create
            { ManagedIdentity.Empty with SystemAssigned = Enabled } + testIdentity + testIdentity2 + testIdentity2
            |> ManagedIdentity.toArmJson
        Expect.equal json.``type`` "SystemAssigned, UserAssigned" "Wrong type"
        Expect.hasLength json.userAssignedIdentities 2 "Wrong identities"
    }
]
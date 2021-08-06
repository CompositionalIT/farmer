module ResourceGroup

open Expecto
open Farmer
open Farmer.Arm.ResourceGroup
open Farmer.Builders

let tests = testList "Resource Group" [
    test "Creates a resource group" {
        let rg = createResourceGroup "myRg" Location.EastUS
        Expect.equal rg.Name.Value "myRg" "Incorrect name on resource group"
        Expect.equal rg.Location Location.EastUS "Incorrect location on resource group"
        Expect.equal rg.Dependencies Set.empty "Resource group should have no dependencies"
        Expect.equal rg.Tags Map.empty "Resource group should have no tags"
    }
]

module Farmer.Arm.ResourceTypes.Compute

open Farmer

let virtualMachines =
    ResourceType("Microsoft.Compute/virtualMachines", "2023-03-01")

let virtualMachineScaleSets =
    ResourceType("Microsoft.Compute/virtualMachineScaleSets", "2023-03-01")

let extensions =
    ResourceType("Microsoft.Compute/virtualMachines/extensions", "2019-12-01")

let virtualMachineScaleSetsExtensions =
    ResourceType("Microsoft.Compute/virtualMachineScaleSets/extensions", "2023-03-01")

let hostGroups = ResourceType("Microsoft.Compute/hostGroups", "2021-03-01")
let hosts = ResourceType("Microsoft.Compute/hostGroups/hosts", "2021-03-01")

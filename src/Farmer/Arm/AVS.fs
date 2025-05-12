[<AutoOpen>]
module Farmer.Arm.AVS

open Farmer
let privateClouds = ResourceType("Microsoft.AVS/privateClouds", "2021-12-01")

let privateCloudsScriptPackages =
    ResourceType("Microsoft.AVS/privateClouds/scriptPackages", "2021-12-01")

let privateCloudsScriptCmdlets =
    ResourceType("Microsoft.AVS/privateClouds/scriptPackages/scriptCmdlets", "2021-12-01")
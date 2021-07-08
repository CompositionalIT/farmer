[<AutoOpen>]
module Farmer.Arm.VirtualHub

open Farmer

// Further examples and information can be found at https://docs.microsoft.com/en-us/azure/templates/microsoft.network/virtualhubs
let virtualHubs = ResourceType ("Microsoft.Network/virtualHubs", "2020-07-01")

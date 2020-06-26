#r @"./libs/Newtonsoft.Json.dll"
#r @"../../src/Farmer/bin/Debug/netstandard2.0/Farmer.dll"

open Farmer
open Farmer.Builders
open Farmer.Vm

let myVm = vm {
    name "isaacsVM"
    username "isaac"
    vm_size Standard_A2
    operating_system WindowsServer_2012Datacenter
    os_disk 128 StandardSSD_LRS
    add_ssd_disk 128
    add_slow_disk 512
    diagnostics_support
}

let deployment = arm {
    location Location.NorthEurope
    add_resource myVm
}

deployment
|> Deploy.execute "my-resource-group-name" Deploy.NoParameters
#r "nuget:Farmer"

open Farmer
open Farmer.Builders
open Farmer.Vm

let myVm = vm {
    name "isaacsVM"
    username "isaac"
    spot_instance Deallocate
    vm_size Standard_A2
    operating_system WindowsServer_2012Datacenter
    os_disk 128 StandardSSD_LRS
    add_ssd_disk 128
    add_slow_disk 512
    diagnostics_support
    system_identity
}

let deployment = arm {
    location Location.NorthEurope
    add_resource myVm
}

deployment |> Deploy.execute "my-resource-group-name" Deploy.NoParameters
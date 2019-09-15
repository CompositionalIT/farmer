#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer

let myVm = vm {
    name "isaacsVM"
    username "isaac"
    vm_size Size.Standard_A2
    operating_system ("CentOS", "OpenLogic", "7.5")
    os_disk 128 StandardSSD_LRS
    add_ssd_disk 128
    add_slow_disk 512
}

let template = arm {
    location Helpers.Locations.NorthEurope
    resource myVm
}

template
|> Writer.toJson
|> Writer.toFile @"vms.json"
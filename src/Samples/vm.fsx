#r @"..\Farmer\bin\Debug\netstandard2.0\Farmer.dll"

open Farmer

let theVm = vm {
    name "isaacsVM"
    username "isaac"
    vm_size Size.Standard_A2
    image CommonImages.WindowsServer_2012Datacenter
}

let template = arm {
    location Helpers.Locations.NorthEurope
    resource theVm
}

template
|> Writer.toJson
|> Writer.toFile @"vms.json"
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



module DeterministicGuid =
    open System
    open System.Security.Cryptography
    open System.Text

    let namespaceGuid = Guid.Parse "92f3929f-622a-4149-8f39-83a4bcd385c8"
    let namespaceBytes = namespaceGuid.ToByteArray()

    let private swapBytes(guid:byte array, left, right) =
        let temp = guid.[left]
        guid.[left] <- guid.[right]
        guid.[right] <- temp

    let private swapByteOrder guid =
        swapBytes(guid, 0, 3)
        swapBytes(guid, 1, 2)
        swapBytes(guid, 4, 5)
        swapBytes(guid, 6, 7)

    let create(source:string) =
        let source = Encoding.UTF8.GetBytes source

        let hash =
            use algorithm = SHA1.Create()
            algorithm.TransformBlock(namespaceBytes, 0, namespaceBytes.Length, null, 0) |> ignore
            algorithm.TransformFinalBlock(source, 0, source.Length) |> ignore
            algorithm.Hash

        let newGuid = Array.zeroCreate<byte> 16
        Array.Copy(hash, 0, newGuid, 0, 16)

        newGuid.[6] <- ((newGuid.[6] &&& 0x0Fuy) ||| (5uy <<< 4))
        newGuid.[8] <- ((newGuid.[8] &&& 0x3Fuy) ||| 0x80uy)

        swapByteOrder newGuid
        Guid newGuid



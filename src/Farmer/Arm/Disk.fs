[<AutoOpen>]
module Farmer.Arm.Disk

open System
open Farmer

let disks = ResourceType("Microsoft.Compute/disks", "2022-07-02")

type DiskCreation =
    | Import of SourceUri: Uri * StorageAccountId: ResourceId
    | Empty of Size: int<Gb>

type Disk = {
    Name: ResourceName
    Location: Location
    Sku: Vm.DiskType option
    Zones: string list
    OsType: OS
    CreationData: DiskCreation
    Tags: Map<string, string>
    Dependencies: ResourceId Set
} with

    interface IArmResource with
        member this.ResourceId = disks.resourceId this.Name

        member this.JsonModel = {|
            disks.Create(this.Name, this.Location, dependsOn = this.Dependencies, tags = this.Tags) with
                sku =
                    this.Sku
                    |> Option.map (fun sku -> {| name = sku.ArmValue |} :> obj)
                    |> Option.toObj
                zones = if this.Zones.IsEmpty then null else ResizeArray(this.Zones)
                properties = {|
                    creationData =
                        match this.CreationData with
                        | Empty _ -> {| createOption = "Empty" |} :> obj
                        | Import(sourceUri, storageAccountId) ->
                            {|
                                createOption = "Import"
                                sourceUri = sourceUri.AbsoluteUri
                                storageAccountId = storageAccountId.Eval()
                            |}
                            :> obj
                    diskSizeGB =
                        match this.CreationData with
                        | Empty size -> size / 1<Gb> :> obj
                        | _ -> null
                    osType =
                        this.OsType
                        |> function
                            | Linux -> "Linux"
                            | Windows -> "Windows"
                |}
        |}
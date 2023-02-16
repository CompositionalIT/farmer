[<AutoOpen>]
module Farmer.Builders.Disk

open Farmer
open Farmer.Arm.Disk

type DiskConfig =
    {
        Name: ResourceName
        Sku: Vm.DiskType option
        Zones: string list
        OsType: OS
        CreationData: DiskCreation
        Tags: Map<string, string>
        Dependencies: ResourceId Set
    }

    interface IBuilder with
        member this.ResourceId = disks.resourceId this.Name

        member this.BuildResources location =
            [
                {
                    Name = this.Name
                    Location = location
                    Sku = this.Sku
                    Zones = this.Zones
                    OsType = this.OsType
                    CreationData = this.CreationData
                    Tags = this.Tags
                    Dependencies = this.Dependencies
                }
            ]

type DiskBuilder() =

    // Default yields a 1 terabyte disk partitioned for Windows.
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            Sku = None
            Zones = []
            OsType = OS.Windows
            CreationData = Empty 1024<Gb>
            Tags = Map.empty
            Dependencies = Set.empty
        }

    [<CustomOperation "name">]
    member _.Name(config: DiskConfig, name) =
        { config with Name = ResourceName name }

    [<CustomOperation "sku">]
    member _.Name(config: DiskConfig, diskType) = { config with Sku = Some diskType }

    [<CustomOperation "add_availability_zone">]
    member _.AddAvailabilityZone(state: DiskConfig, az: string) =
        { state with
            Zones = state.Zones @ [ az ]
        }

    [<CustomOperation "os_type">]
    member _.OsType(config: DiskConfig, os) = { config with OsType = os }

    [<CustomOperation "create_empty">]
    member _.CreateEmpty(config: DiskConfig, size: int<Gb>) =
        { config with
            CreationData = Empty size
        }

    [<CustomOperation "import">]
    member _.Import(config: DiskConfig, sourceVhd: System.Uri, storageAccountId: ResourceId) =
        { config with
            CreationData = Import(sourceVhd, storageAccountId)
        }

    member _.Import(config: DiskConfig, sourceVhd: System.Uri, storageAccount: StorageAccountConfig) =
        { config with
            CreationData = Import(sourceVhd, (storageAccount :> IBuilder).ResourceId)
        }

    member _.Import(config: DiskConfig, sourceVhd: System.Uri, storageAccountName: ResourceName) =
        { config with
            CreationData = Import(sourceVhd, Farmer.Arm.Storage.storageAccounts.resourceId storageAccountName)
        }

    interface ITaggable<DiskConfig> with
        member _.Add state tags =
            { state with
                Tags = state.Tags |> Map.merge tags
            }

    interface IDependable<DiskConfig> with
        member _.Add state newDeps =
            { state with
                Dependencies = state.Dependencies + newDeps
            }

let disk = DiskBuilder()

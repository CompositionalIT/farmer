[<AutoOpen>]
module Farmer.Arm.VirtualWan

open Farmer

let virtualWans = ResourceType("Microsoft.Network/virtualWans", "2020-07-01")

[<RequireQualifiedAccess>]
type Office365LocalBreakoutCategory =
    | Optimize
    | OptimizeAndAllow
    | All
    | None

    member this.ArmValue =
        match this with
        | Optimize -> "Optimize"
        | OptimizeAndAllow -> "OptimizeAndAllow"
        | All -> "All"
        | None -> "None"

[<RequireQualifiedAccess>]
type VwanType =
    | Standard
    | Basic

    member this.ArmValue =
        match this with
        | Standard -> "Standard"
        | Basic -> "Basic"

type VirtualWan = {
    Name: ResourceName
    Location: Location
    AllowBranchToBranchTraffic: bool option
    DisableVpnEncryption: bool option
    Office365LocalBreakoutCategory: Office365LocalBreakoutCategory option
    VwanType: VwanType
} with

    interface IArmResource with
        member this.ResourceId = virtualWans.resourceId this.Name

        member this.JsonModel = {|
            virtualWans.Create(this.Name, this.Location) with
                properties = {|
                    allowBranchToBranchTraffic = this.AllowBranchToBranchTraffic |> Option.defaultValue false
                    disableVpnEncryption = this.DisableVpnEncryption |> Option.defaultValue false
                    office365LocalBreakoutCategory =
                        (this.Office365LocalBreakoutCategory
                         |> Option.defaultValue Office365LocalBreakoutCategory.None)
                            .ArmValue
                    ``type`` = this.VwanType.ArmValue
                |}
        |}

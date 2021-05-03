[<AutoOpen>]
module Farmer.Arm.VirtualWAN

open Farmer

let virtualWans = ResourceType ("Microsoft.Network/virtualWans", "2020-07-01")

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

type VirtualWAN =
    { /// It's recommended to use resource group + -vwan.
      /// e.g. "name": "[concat(resourceGroup().name,'-vwan')]"
      Name : ResourceName
      /// The Azure Region where this resource should be deployed.
      Location : Location
      /// Set boolean for whether you want to allow branch to branch traffic through VWAN
      AllowBranchToBranchTraffic : bool option
      /// Property on VWAN either true or false for VPN Encrpytion
      DisableVpnEncryption : bool option
      /// The office local breakout category (enum) - allowed options are Optimize, OptimizeAndAllow, All and None
      Office365LocalBreakoutCategory : Office365LocalBreakoutCategory option
      /// This is the type of VWAN deployment - only option is Basic or Standard
      VwanType : VwanType }
    interface IArmResource with
        member this.ResourceId = virtualWans.resourceId this.Name
        member this.JsonModel =
            {| virtualWans.Create(this.Name, this.Location) with
                properties =
                    {|
                       allowBranchToBranchTraffic = this.AllowBranchToBranchTraffic |> Option.defaultValue false
                       disableVpnEncryption = this.DisableVpnEncryption |> Option.defaultValue false
                       office365LocalBreakoutCategory = (this.Office365LocalBreakoutCategory |> Option.defaultValue Office365LocalBreakoutCategory.None).ArmValue
                       ``type`` = this.VwanType.ArmValue |}
            |}:> _
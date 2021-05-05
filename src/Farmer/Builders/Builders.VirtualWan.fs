[<AutoOpen>]
module Farmer.Builders.VirtualWan


open Farmer
open Farmer.Arm.VirtualWan

type VirtualWanConfig =
    { Name : ResourceName
      /// Set boolean for whether you want to allow branch to branch traffic through VWAN
      AllowBranchToBranchTraffic : bool option
      /// Property on VWAN either true or false for VPN Encrpytion
      DisableVpnEncryption : bool option
      /// The office local breakout category (enum) - allowed options are Optimize, OptimizeAndAllow, All and None
      Office365LocalBreakoutCategory : Office365LocalBreakoutCategory option
      /// This is the type of VWAN deployment - only option is Basic or Standard
      VwanType : VwanType }
    interface IBuilder with
        member this.ResourceId = virtualWans.resourceId this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              AllowBranchToBranchTraffic = this.AllowBranchToBranchTraffic
              DisableVpnEncryption = this.DisableVpnEncryption
              Office365LocalBreakoutCategory = this.Office365LocalBreakoutCategory
              VwanType = this.VwanType }
        ]

type VirtualWanBuilder() =
    /// Yield sets everything to sane defaults.
    member _.Yield _ : VirtualWanConfig =
        { Name = ResourceName.Empty
          AllowBranchToBranchTraffic = None
          DisableVpnEncryption = None
          Office365LocalBreakoutCategory = None
          VwanType = VwanType.Basic }
    /// Sets the name to a ResourceName from the given string.
    [<CustomOperation "name">]
    member _.Name(state:VirtualWanConfig, name) = { state with Name = ResourceName name }
    /// Sets the VWAN type to "standard" instead of the default "basic".
    [<CustomOperation "standard_vwan">]
    member _.StandardVwanType(state:VirtualWanConfig) = { state with VwanType = VwanType.Standard }
    /// Allow branch to branch traffic.
    [<CustomOperation "allow_branch_to_branch_traffic">]
    member _.AllowBranchToBranchTraffic(state:VirtualWanConfig) = { state with AllowBranchToBranchTraffic = Some true }
    /// Disable vpn encryption
    [<CustomOperation "disable_vpn_encryption">]
    member _.DisableVpnEncryption(state:VirtualWanConfig) = { state with DisableVpnEncryption = Some true }
    /// Sets the office local breakout category
    [<CustomOperation "office_365_local_breakout_category">]
    member _.Office365LocalBreakoutCategory(state:VirtualWanConfig, category) = { state with Office365LocalBreakoutCategory = Some category }

/// This creates the keyword for a builder, such as `vwan { name "my-vwan" }
let vwan = VirtualWanBuilder()

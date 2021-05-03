[<AutoOpen>]
module Farmer.Builders.VirtualWAN


open Farmer
open Farmer.Arm.VirtualWAN

/// This is a thin abstraction over the VirtualHub resource that is
/// made to support the `virtualHub` builder syntax.
type VirtualWanConfig =
    { /// Name of the VirtualWAN resource.
      Name : ResourceName
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
        /// This emits the resource or resources that should go in the template.
        /// Depending on the configuration, it may make sense to emit multiple resources,
        /// but at the very least, this will generate a VirtualWAN resource from this
        /// VirtualWanConfig with the location set.
        member this.BuildResources location = [
            // Emit a VirtualHub resource with the location
            {
                Name = this.Name
                Location = location
                AllowBranchToBranchTraffic = this.AllowBranchToBranchTraffic
                DisableVpnEncryption = this.DisableVpnEncryption
                Office365LocalBreakoutCategory = this.Office365LocalBreakoutCategory
                VwanType = this.VwanType
            }
        ]

/// The builder implements the DSL to simplify create and configure the VirtualHub resource.
/// Custom operations define the builder DSL syntax.
type VirtualWanBuilder() =
    /// Yield sets everything to sane defaults.
    member _.Yield _ : VirtualWanConfig =
        {
            Name = ResourceName.Empty
            AllowBranchToBranchTraffic = None
            DisableVpnEncryption = None
            Office365LocalBreakoutCategory = None
            VwanType = VwanType.Basic
        }
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

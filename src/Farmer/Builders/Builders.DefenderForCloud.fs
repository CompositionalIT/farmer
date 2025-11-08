[<AutoOpen>]
module Farmer.Builders.DefenderForCloud

open Farmer
open Farmer.Arm.Security

type DefenderForCloudConfig = {
    Plan: DefenderPlan
    Tier: PricingTier
} with

    interface IBuilder with
        member this.ResourceId = pricings.resourceId (ResourceName this.Plan.ArmValue)

        member this.BuildResources _ = [
            {
                DefenderPricing.Plan = this.Plan
                Tier = this.Tier
            }
        ]

type DefenderForCloudBuilder() =
    member _.Yield _ = {
        Plan = DefenderPlan.VirtualMachines
        Tier = PricingTier.Standard
    }

    /// Sets the Defender plan to enable (VirtualMachines, SqlServers, AppServices, etc.).
    [<CustomOperation "plan">]
    member _.Plan(state: DefenderForCloudConfig, plan: DefenderPlan) = { state with Plan = plan }

    /// Sets the pricing tier (Standard for enabled, Free for disabled). Default is Standard.
    [<CustomOperation "tier">]
    member _.Tier(state: DefenderForCloudConfig, tier: PricingTier) = { state with Tier = tier }

    /// Enables the Defender plan (sets tier to Standard).
    [<CustomOperation "enable">]
    member _.Enable(state: DefenderForCloudConfig) = { state with Tier = PricingTier.Standard }

    /// Disables the Defender plan (sets tier to Free).
    [<CustomOperation "disable">]
    member _.Disable(state: DefenderForCloudConfig) = { state with Tier = PricingTier.Free }

/// Enables Microsoft Defender for Cloud (formerly Security Center) plans.
let defenderForCloud = DefenderForCloudBuilder()

[<AutoOpen>]
module Farmer.Arm.SecurityInsights

open Farmer

let onboardingStates =
    ResourceType("Microsoft.SecurityInsights/onboardingStates", "2024-03-01")

type SentinelOnboarding = {
    WorkspaceName: ResourceName
    Dependencies: ResourceId Set
} with

    interface IArmResource with
        member this.ResourceId =
            { onboardingStates.resourceId (this.WorkspaceName, ResourceName "default") with
                Type = onboardingStates
            }

        member this.JsonModel =
            let dependencies = this.Dependencies

            {|
                onboardingStates.Create(this.WorkspaceName / ResourceName "default", dependsOn = dependencies) with
                    properties = {||}
            |}

[<AutoOpen>]
module Farmer.Builders.DdosProtectionPlan

open Farmer
open Farmer.Arm.Network

type DdosProtectionPlanConfig = {
    Name: ResourceName
    Tags: Map<string, string>
} with

    interface IBuilder with
        member this.ResourceId = ddosProtectionPlans.resourceId this.Name

        member this.BuildResources location = [
            {
                DdosProtectionPlan.Name = this.Name
                Location = location
                Tags = this.Tags
            }
        ]

type DdosProtectionPlanBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        Tags = Map.empty
    }

    /// Sets the name of the DDoS Protection Plan.
    [<CustomOperation "name">]
    member _.Name(state: DdosProtectionPlanConfig, name: string) = { state with Name = ResourceName name }

    interface ITaggable<DdosProtectionPlanConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

/// Builds a DDoS Protection Plan resource.
let ddosProtectionPlan = DdosProtectionPlanBuilder()

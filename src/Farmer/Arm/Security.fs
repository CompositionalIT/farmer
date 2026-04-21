[<AutoOpen>]
module Farmer.Arm.Security

open Farmer

let pricings = ResourceType("Microsoft.Security/pricings", "2024-01-01")

[<RequireQualifiedAccess>]
type DefenderPlan =
    | VirtualMachines
    | SqlServers
    | AppServices
    | StorageAccounts
    | SqlServerVirtualMachines
    | KubernetesService
    | ContainerRegistry
    | KeyVaults
    | Dns
    | Arm
    | OpenSourceRelationalDatabases
    | Containers
    | CosmosDbs
    | CloudPosture

    member this.ArmValue =
        match this with
        | VirtualMachines -> "VirtualMachines"
        | SqlServers -> "SqlServers"
        | AppServices -> "AppServices"
        | StorageAccounts -> "StorageAccounts"
        | SqlServerVirtualMachines -> "SqlServerVirtualMachines"
        | KubernetesService -> "KubernetesService"
        | ContainerRegistry -> "ContainerRegistry"
        | KeyVaults -> "KeyVaults"
        | Dns -> "Dns"
        | Arm -> "Arm"
        | OpenSourceRelationalDatabases -> "OpenSourceRelationalDatabases"
        | Containers -> "Containers"
        | CosmosDbs -> "CosmosDbs"
        | CloudPosture -> "CloudPosture"

[<RequireQualifiedAccess>]
type PricingTier =
    | Free
    | Standard

    member this.ArmValue =
        match this with
        | Free -> "Free"
        | Standard -> "Standard"

type DefenderPricing = {
    Plan: DefenderPlan
    Tier: PricingTier
} with

    interface IArmResource with
        member this.ResourceId = pricings.resourceId (ResourceName this.Plan.ArmValue)

        member this.JsonModel =
            {|
                pricings.Create(ResourceName this.Plan.ArmValue) with
                    properties = {| pricingTier = this.Tier.ArmValue |}
            |}

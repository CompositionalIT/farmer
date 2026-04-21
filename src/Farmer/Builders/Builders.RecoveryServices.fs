[<AutoOpen>]
module Farmer.Builders.RecoveryServices

open Farmer
open Farmer.Arm.RecoveryServices
open Farmer.Arm.RecoveryServices.RecoveryServicesVaults

type RecoveryServicesVaultConfig = {
    Name: ResourceName
    Sku: SkuName
    Tags: Map<string, string>
} with

    interface IBuilder with
        member this.ResourceId = RecoveryServicesVaults.vaults.resourceId this.Name

        member this.BuildResources location = [
            {
                RecoveryServicesVault.Name = this.Name
                Location = location
                Sku = this.Sku
                Tags = this.Tags
            }
        ]

type RecoveryServicesVaultBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        Sku = SkuName.Standard
        Tags = Map.empty
    }

    /// Sets the name of the Recovery Services Vault.
    [<CustomOperation "name">]
    member _.Name(state: RecoveryServicesVaultConfig, name: string) = { state with Name = ResourceName name }

    /// Sets the SKU (RS0 for free tier, Standard for production). Default is Standard.
    [<CustomOperation "sku">]
    member _.Sku(state: RecoveryServicesVaultConfig, sku: SkuName) = { state with Sku = sku }

    interface ITaggable<RecoveryServicesVaultConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

type VmBackupPolicyConfig = {
    Name: ResourceName
    VaultName: ResourceName
    Vault: RecoveryServicesVaultConfig option
    ScheduleFrequency: BackupScheduleFrequency
    ScheduleTime: string
    RetentionDays: int
    WeeklyRetentionWeeks: int option
    MonthlyRetentionMonths: int option
    Dependencies: ResourceId Set
} with

    interface IBuilder with
        member this.ResourceId =
            let vaultName =
                match this.Vault with
                | Some vault -> vault.Name
                | None -> this.VaultName

            RecoveryServicesVaults.backupPolicies.resourceId (vaultName, this.Name)

        member this.BuildResources _ =
            let vaultName =
                match this.Vault with
                | Some vault -> vault.Name
                | None -> this.VaultName

            [
                {
                    VmBackupPolicy.Name = this.Name
                    VaultName = vaultName
                    ScheduleFrequency = this.ScheduleFrequency
                    ScheduleTime = this.ScheduleTime
                    RetentionDays = this.RetentionDays
                    WeeklyRetentionWeeks = this.WeeklyRetentionWeeks
                    MonthlyRetentionMonths = this.MonthlyRetentionMonths
                    Dependencies = this.Dependencies
                }
            ]

type VmBackupPolicyBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        VaultName = ResourceName.Empty
        Vault = None
        ScheduleFrequency = BackupScheduleFrequency.Daily
        ScheduleTime = "2023-01-01T03:00:00Z"
        RetentionDays = 30
        WeeklyRetentionWeeks = None
        MonthlyRetentionMonths = None
        Dependencies = Set.empty
    }

    /// Sets the name of the backup policy.
    [<CustomOperation "name">]
    member _.Name(state: VmBackupPolicyConfig, name: string) = { state with Name = ResourceName name }

    /// Links to a Recovery Services Vault.
    [<CustomOperation "link_to_vault">]
    member _.LinkToVault(state: VmBackupPolicyConfig, vault: RecoveryServicesVaultConfig) = {
        state with
            Vault = Some vault
    }

    /// Sets the vault name directly (for existing vaults).
    [<CustomOperation "vault_name">]
    member _.VaultName(state: VmBackupPolicyConfig, vaultName: string) = {
        state with
            VaultName = ResourceName vaultName
    }

    /// Sets the backup schedule frequency (Daily or Weekly). Default is Daily.
    [<CustomOperation "schedule_frequency">]
    member _.ScheduleFrequency(state: VmBackupPolicyConfig, frequency: BackupScheduleFrequency) = {
        state with
            ScheduleFrequency = frequency
    }

    /// Sets the backup schedule time in ISO format (e.g., "2023-01-01T03:00:00Z"). Default is 3 AM UTC.
    [<CustomOperation "schedule_time">]
    member _.ScheduleTime(state: VmBackupPolicyConfig, time: string) = { state with ScheduleTime = time }

    /// Sets daily retention in days (7-9999). Default is 30 days.
    [<CustomOperation "retention_days">]
    member _.RetentionDays(state: VmBackupPolicyConfig, days: int) = { state with RetentionDays = days }

    /// Sets weekly retention in weeks (1-5163).
    [<CustomOperation "weekly_retention_weeks">]
    member _.WeeklyRetentionWeeks(state: VmBackupPolicyConfig, weeks: int) = {
        state with
            WeeklyRetentionWeeks = Some weeks
    }

    /// Sets monthly retention in months (1-1188).
    [<CustomOperation "monthly_retention_months">]
    member _.MonthlyRetentionMonths(state: VmBackupPolicyConfig, months: int) = {
        state with
            MonthlyRetentionMonths = Some months
    }

    /// Adds a dependency to this backup policy.
    [<CustomOperation "add_dependency">]
    member _.AddDependency(state: VmBackupPolicyConfig, dependency: ResourceId) = {
        state with
            Dependencies = state.Dependencies.Add dependency
    }

/// Builds a Recovery Services Vault.
let recoveryServicesVault = RecoveryServicesVaultBuilder()

/// Builds a VM backup policy.
let vmBackupPolicy = VmBackupPolicyBuilder()

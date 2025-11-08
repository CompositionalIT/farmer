module Farmer.Arm.RecoveryServices

open Farmer

module RecoveryServicesVaults =
    let vaults =
        ResourceType("Microsoft.RecoveryServices/vaults", "2024-04-01")

    let backupPolicies =
        ResourceType("Microsoft.RecoveryServices/vaults/backupPolicies", "2024-04-01")

[<RequireQualifiedAccess>]
type SkuName =
    | RS0
    | Standard

    member this.ArmValue =
        match this with
        | RS0 -> "RS0"
        | Standard -> "Standard"

[<RequireQualifiedAccess>]
type BackupScheduleFrequency =
    | Daily
    | Weekly

    member this.ArmValue =
        match this with
        | Daily -> "Daily"
        | Weekly -> "Weekly"

type RecoveryServicesVault = {
    Name: ResourceName
    Location: Location
    Sku: SkuName
    Tags: Map<string, string>
} with

    interface IArmResource with
        member this.ResourceId = RecoveryServicesVaults.vaults.resourceId this.Name

        member this.JsonModel =
            {|
                RecoveryServicesVaults.vaults.Create(this.Name, this.Location, tags = this.Tags) with
                    sku = {| name = this.Sku.ArmValue |}
                    properties = {||}
            |}

type VmBackupPolicy = {
    Name: ResourceName
    VaultName: ResourceName
    ScheduleFrequency: BackupScheduleFrequency
    ScheduleTime: string
    RetentionDays: int
    WeeklyRetentionWeeks: int option
    MonthlyRetentionMonths: int option
    Dependencies: ResourceId Set
} with

    interface IArmResource with
        member this.ResourceId =
            RecoveryServicesVaults.backupPolicies.resourceId (this.VaultName, this.Name)

        member this.JsonModel =
            let dependencies = this.Dependencies + Set [ RecoveryServicesVaults.vaults.resourceId this.VaultName ]

            {|
                RecoveryServicesVaults.backupPolicies.Create(this.VaultName / this.Name, dependsOn = dependencies) with
                    properties = {|
                        backupManagementType = "AzureIaasVM"
                        schedulePolicy = {|
                            schedulePolicyType = "SimpleSchedulePolicy"
                            scheduleRunFrequency = this.ScheduleFrequency.ArmValue
                            scheduleRunTimes = [ this.ScheduleTime ]
                        |}
                        retentionPolicy = {|
                            retentionPolicyType = "LongTermRetentionPolicy"
                            dailySchedule = {|
                                retentionTimes = [ this.ScheduleTime ]
                                retentionDuration = {|
                                    count = this.RetentionDays
                                    durationType = "Days"
                                |}
                            |}
                            weeklySchedule =
                                match this.WeeklyRetentionWeeks with
                                | Some weeks ->
                                    {|
                                        daysOfTheWeek = [ "Sunday" ]
                                        retentionTimes = [ this.ScheduleTime ]
                                        retentionDuration = {|
                                            count = weeks
                                            durationType = "Weeks"
                                        |}
                                    |}
                                    :> obj
                                | None -> null
                            monthlySchedule =
                                match this.MonthlyRetentionMonths with
                                | Some months ->
                                    {|
                                        retentionScheduleFormatType = "Weekly"
                                        retentionScheduleWeekly = {|
                                            daysOfTheWeek = [ "Sunday" ]
                                            weeksOfTheMonth = [ "First" ]
                                        |}
                                        retentionTimes = [ this.ScheduleTime ]
                                        retentionDuration = {|
                                            count = months
                                            durationType = "Months"
                                        |}
                                    |}
                                    :> obj
                                | None -> null
                        |}
                        timeZone = "UTC"
                    |}
            |}

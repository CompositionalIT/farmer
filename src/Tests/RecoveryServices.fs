module RecoveryServices

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Arm.RecoveryServices
open Newtonsoft.Json.Linq

let tests =
    testList "Recovery Services" [
        test "Creates a basic Recovery Services Vault" {
            let vault = recoveryServicesVault { name "my-backup-vault" }
            let deployment = arm { add_resources [ vault ] }

            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse

            let vaultResource =
                jobj.SelectToken("resources[?(@.type=='Microsoft.RecoveryServices/vaults')]")

            Expect.isNotNull vaultResource "Vault resource should exist"
            Expect.equal (vaultResource.SelectToken("name").ToString()) "my-backup-vault" "Name should be correct"
            Expect.equal (vaultResource.SelectToken("sku.name").ToString()) "Standard" "SKU should be Standard by default"
        }

        test "Recovery Services Vault can have tags" {
            let vault =
                recoveryServicesVault {
                    name "tagged-vault"
                    add_tags [ "environment", "production"; "backup", "critical" ]
                }

            let deployment = arm { add_resources [ vault ] }
            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse
            let vaultResource = jobj.SelectToken("resources[?(@.type=='Microsoft.RecoveryServices/vaults')]")

            let tags = vaultResource.SelectToken("tags")
            Expect.isNotNull tags "Tags should exist"
            Expect.equal (tags.SelectToken("environment").ToString()) "production" "Environment tag should be correct"
            Expect.equal (tags.SelectToken("backup").ToString()) "critical" "Backup tag should be correct"
        }

        test "Creates a VM backup policy" {
            let vault = recoveryServicesVault { name "backup-vault" }

            let policy =
                vmBackupPolicy {
                    name "daily-vm-backup"
                    link_to_vault vault
                    schedule_frequency BackupScheduleFrequency.Daily
                    retention_days 30
                }

            let deployment = arm { add_resources [ vault; policy ] }
            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse

            let policyResource =
                jobj.SelectToken("resources[?(@.type=='Microsoft.RecoveryServices/vaults/backupPolicies')]")

            Expect.isNotNull policyResource "Backup policy resource should exist"
            Expect.equal (policyResource.SelectToken("name").ToString()) "backup-vault/daily-vm-backup" "Name should be correct"

            Expect.equal
                (policyResource.SelectToken("properties.backupManagementType").ToString())
                "AzureIaasVM"
                "Backup type should be VM"

            Expect.equal
                (policyResource.SelectToken("properties.schedulePolicy.scheduleRunFrequency").ToString())
                "Daily"
                "Schedule frequency should be Daily"

            Expect.equal
                (policyResource.SelectToken("properties.retentionPolicy.dailySchedule.retentionDuration.count")
                    .ToString())
                "30"
                "Retention days should be 30"
        }

        test "VM backup policy with weekly and monthly retention" {
            let vault = recoveryServicesVault { name "backup-vault" }

            let policy =
                vmBackupPolicy {
                    name "comprehensive-backup"
                    link_to_vault vault
                    retention_days 30
                    weekly_retention_weeks 12
                    monthly_retention_months 6
                }

            let deployment = arm { add_resources [ vault; policy ] }
            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse

            let policyResource =
                jobj.SelectToken("resources[?(@.type=='Microsoft.RecoveryServices/vaults/backupPolicies')]")

            Expect.equal
                (policyResource
                    .SelectToken("properties.retentionPolicy.weeklySchedule.retentionDuration.count")
                    .ToString())
                "12"
                "Weekly retention should be 12 weeks"

            Expect.equal
                (policyResource
                    .SelectToken("properties.retentionPolicy.monthlySchedule.retentionDuration.count")
                    .ToString())
                "6"
                "Monthly retention should be 6 months"
        }

        test "Backup policy depends on vault" {
            let vault = recoveryServicesVault { name "vault" }

            let policy =
                vmBackupPolicy {
                    name "policy"
                    link_to_vault vault
                }

            let deployment = arm { add_resources [ vault; policy ] }
            let jobj = deployment.Template |> Writer.toJson |> JToken.Parse

            let policyResource =
                jobj.SelectToken("resources[?(@.type=='Microsoft.RecoveryServices/vaults/backupPolicies')]")

            let dependsOn = policyResource.SelectToken("dependsOn")
            Expect.isNotNull dependsOn "DependsOn should exist"
            Expect.isTrue (dependsOn.ToString().Contains("vault")) "Should depend on vault"
        }

        test "Recovery Services Vault has correct resource ID" {
            let vault = recoveryServicesVault { name "test-vault" }
            let resourceId = (vault :> IBuilder).ResourceId

            Expect.equal resourceId.Type.Type "Microsoft.RecoveryServices/vaults" "Type should be correct"
            Expect.equal resourceId.Name.Value "test-vault" "Name should be correct"
        }
    ]

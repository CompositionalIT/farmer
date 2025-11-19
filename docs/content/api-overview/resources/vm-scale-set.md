---
title: "Virtual Machine Scale Set"
date: 2023-11-05T21:10:14-05:00
chapter: false
weight: 22
---

#### Overview
The Virtual Machine Scale Set builder (`vmss`) creates a virtual machine scale set that can grow and shrink it's capacity by creating virtual machines and their dependent resources from a common profile.

* Virtual Machine Scale Sets (`Microsoft.Compute/virtualMachineScaleSets`)

#### Builder Keywords

| Builder                    | Keyword                               | Purpose                                                                                                                                                                                                   |
|----------------------------|---------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| vmss                       | name                                  | Sets the name of the VM scale set.                                                                                                                                                                        |
| vmss                       | vm_profile                            | Defines a profile for VM's in the scale set using the `vm` builder to support all the same functionality as a single VM.                                                                                  |
| vmss                       | add_availability_zones                | Adds one or more availability zones so VM resources will be distributed across those zones.                                                                                                               |
| vmss                       | pick_zones                            | Picks availability zones within a region.                                                                                                                                                                                                          |
| vmss                       | add_extensions                        | Adds extensions that will be automatically installed on VMs when scaling out.                                                                                                                             |
| vmss                       | automatic_repair_policy               | Enables automatically replacing VMs in the scale set that report as unhealthy. Requires adding the Application Health Extension.                                                                          |
| vmss                       | automatic_repair_enabled_after        | Defines a grace period after becoming unhealthy before replacing the instance.                                                                                                                            |
| vmss                       | capacity                              | The number of VM instances in the scale set.                                                                                                                                                              |
| vmss                       | overprovision                         | Specifies whether the Virtual Machine Scale Set should be overprovisioned.                                                                                                                                                   |
| vmss                       | run_extensions_on_overprovisioned_vms | When Overprovision is enabled, extensions are launched on all VMs, unless disabled.                                                                                                                                                   |
| vmss                       | health_probe                          | If not using an application health extension, this refers to a load balancer health probe that can indicate instance health.                                                                              |
| vmss                       | scale_in_policy                       | Specify the policy for determining which VMs to remove when scaling in.                                                                                                                                   |
| vmss                       | scale_in_force_deletion               | Indicates the VMs should be force deleted so they free the resources more quickly.                                                                                                                        |
| vmss                       | upgrade_mode                          | Specify Manual, Automatic, or Rolling upgrades. Rolling upgrades require the Application Health Extension or a Health Probe to ensure newly replaced instances are healthy before replacing more of them. |
| vmss                       | osupgrade_automatic                   | Indicates whether OS upgrades should automatically be applied to scale set instances in a rolling fashion when a newer version of the OS image becomes available. Default value is false. If this is set to true for Windows based scale sets, enableAutomaticUpdates is automatically set to false and cannot be set to true. |
| vmss                       | osupgrade_automatic_rollback          | Whether OS image rollback feature should be enabled. Enabled by default. |
| vmss                       | osupgrade_rolling_upgrade             | Indicates whether rolling upgrade policy should be used during Auto OS Upgrade. Default value is false. Auto OS Upgrade will fallback to the default policy if no policy is defined on the VMSS. |
| vmss                       | osupgrade_rolling_upgrade_deferral    | Indicates whether Auto OS Upgrade should undergo deferral. Deferred OS upgrades will send advanced notifications on a per-VM basis that an OS upgrade from rolling upgrades is incoming, via the IMDS tag 'Platform.PendingOSUpgrade'. The upgrade then defers until the upgrade is approved via an ApproveRollingUpgrade call. |
| vmss                       | rolling_upgrade_enable_cross_zone_upgrade | Allow VMSS to ignore Availability Zone boundaries when constructing upgrade batches. Allows Azure to spread out batches across different zones. |
| vmss                       | rolling_upgrade_max_batch_instance_percent | The maximum percentage of total virtual machine instances that will be upgraded simultaneously by the rolling upgrade in one batch. Should be between 5 and 100. |
| vmss                       | rolling_upgrade_max_surge              | Create new instances temporarily to replace old ones during upgrade. Helps maximize availability during rolling upgrades. |
| vmss                       | rolling_upgrade_max_unhealthy_instance_percent | The maximum percentage of the total virtual machine instances in the scale set that can be simultaneously unhealthy before the rolling upgrade aborts. |
| vmss                       | rolling_upgrade_max_unhealthy_upgraded_instance_percent | The maximum percentage of upgraded virtual machine instances that can be found to be in an unhealthy state. |
| vmss                       | rolling_upgrade_pause_time_between_batches | The wait time between completing the update for all virtual machines in one batch and starting the next batch. The time duration should be specified in ISO 8601 format (e.g. PT5M for 5 minutes). |
| vmss                       | rolling_upgrade_prioritize_unhealthy_instances | Upgrade all unhealthy instances in a scale set before any healthy instances. |
| vmss                       | rolling_upgrade_rollback_failed_instances_on_policy_breach | Rollback failed instances to previous model if the Rolling Upgrade policy is violated. |
| applicationHealthExtension | vmss                                  | When adding the extension as a resource, this specifies the VM scale set it should be applied to.                                                                                                         |
| applicationHealthExtension | os                                    | Operating system (Linux or Windows) to install the correct extension for that OS.                                                                                                                         |
| applicationHealthExtension | protocol                              | Protocol (TCP, HTTP, or HTTPS) to probe, and if specifying HTTP or HTTPS, include the path.                                                                                                               |
| applicationHealthExtension | port                                  | TCP port to probe.                                                                                                                                                                                        |
| applicationHealthExtension | interval                              | Interval to probe for health.                                                                                                                                                                             |
| applicationHealthExtension | number_of_probes                      | Sets the number of times the probe must fail to consider this instance a failure.                                                                                                                            |
| applicationHealthExtension | enable_automatic_upgrade              | Enable/Disable automatic extension upgrade (not enabled by default)         |
| applicationHealthExtension | type_handler_version                  | Extension version (default: "1.0")         |


#### Example

This example creates a scale set with 3 VM instances and includes the Application Health Extension to support rolling updates and automatic repairs.

```fsharp
open Farmer
open Farmer.Builders
open Farmer.Vm
open Farmer.VmScaleSet

vmss {
    name "my-scale-set"
    capacity 3

    vm_profile (
        vm {
            username "azureuser"
            operating_system UbuntuServer_2204LTS
            vm_size Standard_B1s
            os_disk 128 StandardSSD_LRS
            diagnostics_support_managed
            custom_script "sudo apt update && sudo apt install -y nginx"
        }
    )
    scale_in_policy OldestVM
    upgrade_mode Rolling
    automatic_repair_enabled_after (System.TimeSpan.FromMinutes 10)
    add_extensions [
        applicationHealthExtension {
            protocol (ApplicationHealthExtensionProtocol.HTTP "/healthcheck")
            port 80
            os Linux
        }
    ]

}
```

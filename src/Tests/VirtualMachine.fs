module VirtualMachine

open Expecto
open Farmer
open Farmer.Resources
open Microsoft.Azure.Management.Compute
open Microsoft.Azure.Management.Compute.Models
open Microsoft.Rest
open System

/// Client instance needed to get the serializer settings.
let client = new ComputeManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let tests = testList "Virtual Machine Tests" [
    test "Can create a basic virtual machine" {
        let resource =
            let myVm = vm {
                name "isaacsVM"
                username "isaac"
                vm_size Standard_A2
                operating_system WindowsServer_2012Datacenter
                os_disk 128 StandardSSD_LRS
                add_ssd_disk 128
                add_slow_disk 512
                diagnostics_support
            }
            arm { add_resource myVm }
            |> findAzureResources<VirtualMachine> client.SerializationSettings
            |> List.head

        resource.Validate()

        Expect.equal resource.StorageProfile.OsDisk.DiskSizeGB (Nullable 128) "Incorrect OS disk size"
        Expect.equal resource.StorageProfile.ImageReference.Offer WindowsServer_2012Datacenter.Offer.ArmValue "Incorrect Offer"
        Expect.equal resource.StorageProfile.DataDisks.Count 2 "Incorrect number of data disks"
        Expect.equal resource.OsProfile.AdminUsername "isaac" "Incorrect username"
        Expect.isTrue (resource.DiagnosticsProfile.BootDiagnostics.Enabled.GetValueOrDefault false) "Boot Diagnostics should be enabled"
    }
]
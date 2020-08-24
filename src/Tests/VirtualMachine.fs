module VirtualMachine

open Expecto
open Farmer
open Farmer.Builders
open Farmer.Vm
open Microsoft.Azure.Management.Compute
open Microsoft.Azure.Management.Compute.Models
open Microsoft.Rest
open System

/// Client instance needed to get the serializer settings.
let client = new ComputeManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let tests = testList "Virtual Machine" [
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
            |> List.find(fun r -> r.StorageProfile |> isNull |> not)

        resource.Validate()

        Expect.equal resource.StorageProfile.OsDisk.DiskSizeGB (Nullable 128) "Incorrect OS disk size"
        Expect.equal resource.StorageProfile.ImageReference.Offer WindowsServer_2012Datacenter.Offer.ArmValue "Incorrect Offer"
        Expect.equal resource.StorageProfile.DataDisks.Count 2 "Incorrect number of data disks"
        Expect.equal resource.OsProfile.AdminUsername "isaac" "Incorrect username"
        Expect.equal resource.NetworkProfile.NetworkInterfaces.[0].Id "[resourceId('Microsoft.Network/networkInterfaces', 'isaacsVM-nic')]" "Incorrect NIC reference"
        Expect.isTrue (resource.DiagnosticsProfile.BootDiagnostics.Enabled.GetValueOrDefault false) "Boot Diagnostics should be enabled"
    }
    test "Creates a parameter for the password" {
        let deployment =
            arm {
                add_resource
                    (vm { name "isaac"; username "foo" })
            }
        let template = deployment.Template |> Writer.TemplateGeneration.processTemplate
        Expect.isTrue (template.parameters.ContainsKey "password-for-isaac") "Missing parameter"
        Expect.equal template.parameters.Count 1 "Should only be one parameter"
    }
    test "Throws an error if you upload script files but no script" {
        let createVm () = arm { add_resource (vm { name "foo"; username "foo"; custom_script_files [ "http://test.fsx" ] }) } |> ignore
        Expect.throws createVm "No script was supplied"
    }
    test "Does not throws an error if you provide a script" {
        arm { add_resource (vm { name "foo"; username "foo"; custom_script "foo"; custom_script_files [ "http://test.fsx" ] }) } |> ignore
        arm { add_resource (vm { name "foo"; username "foo"; custom_script "foo" }) } |> ignore
    }
]
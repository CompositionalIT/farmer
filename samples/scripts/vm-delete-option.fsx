// This sample demonstrates how to set delete options for VM resources
// When set to Delete, the associated resources will be automatically
// removed when the VM is deleted from Azure.

open Farmer
open Farmer.Builders

// Example 1: Using the convenience method to delete all resources
let myVmSimple = vm {
    name "my-vm-simple"
    username "azureuser"
    vm_size Vm.Standard_A2
    operating_system Vm.UbuntuServer_2204LTS
    os_disk 128 Vm.StandardSSD_LRS
    add_ssd_disk 256

    // Convenience method - sets all delete options to Delete at once
    delete_all_on_vm_delete
}

// Example 2: Setting delete options individually for fine-grained control
let myVmDetailed = vm {
    name "my-vm-detailed"
    username "azureuser"
    vm_size Vm.Standard_A2
    operating_system Vm.UbuntuServer_2204LTS
    os_disk 128 Vm.StandardSSD_LRS
    add_ssd_disk 256

    // Set delete option to automatically remove disks when VM is deleted
    disk_delete_option Vm.DiskDeleteOption.Delete

    // Set delete option to automatically remove NIC when VM is deleted
    nic_delete_option Vm.NicDeleteOption.Delete

    // Set delete option to automatically remove public IP when VM is deleted
    public_ip_delete_option Vm.PublicIpDeleteOption.Delete
}

let deployment = arm {
    location Location.EastUS
    add_resources [ myVmSimple; myVmDetailed ]
}

// Generate the ARM template
deployment |> Writer.quickWrite "vm-delete-option"

// Or deploy directly to Azure
// deployment |> Deploy.execute "my-resource-group-name" Deploy.NoParameters
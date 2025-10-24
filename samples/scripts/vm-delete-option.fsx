// This sample demonstrates how to set delete options for VM resources
// When set to Delete, the associated resources will be automatically 
// removed when the VM is deleted from Azure.

open Farmer
open Farmer.Builders
open Farmer.Vm

let myVm = vm {
    name "my-vm"
    username "azureuser"
    vm_size Standard_B2s
    operating_system UbuntuServer_2204LTS
    os_disk 128 StandardSSD_LRS
    add_ssd_disk 256
    
    // Set delete option to automatically remove disks when VM is deleted
    disk_delete_option DiskDeleteOption.Delete
    
    // Set delete option to automatically remove NIC when VM is deleted
    nic_delete_option NicDeleteOption.Delete
    
    // Set delete option to automatically remove public IP when VM is deleted
    public_ip_delete_option PublicIpDeleteOption.Delete
}

let deployment = arm {
    location Location.EastUS
    add_resource myVm
}

// Generate the ARM template
deployment |> Writer.quickWrite "vm-delete-option"

// Or deploy directly to Azure
// deployment |> Deploy.execute "my-resource-group-name" Deploy.NoParameters

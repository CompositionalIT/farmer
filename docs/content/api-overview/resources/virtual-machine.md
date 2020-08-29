---
title: "Virtual Machine"
date: 2020-02-05T08:53:46+01:00
chapter: false
weight: 24
---

#### Overview
The Virtual Machine builder creates a fully configured virtual machine and all its required child resources.

* Virtual Machines (`Microsoft.Compute/virtualMachines`)
* Virtual Networks (`Microsoft.Network/virtualNetworks`)
* IP Addresses (`Microsoft.Network/publicIPAddresses`)
* Network Interfaces (`Microsoft.Network/networkInterfaces`)
* Storage Accounts (`Microsoft.Storage/storageAccounts`)

In addition, every VM you create will add a SecureString parameter to the ARM template, whose name follows the pattern **password-for-[virtual machine name]**.

#### Builder Keywords

| Keyword | Purpose |
|-|-|
|name | Sets the name of the VM. |
|diagnostics_support | Turns on diagnostics support using an automatically created created storage account. |
|diagnostics_support_external | Turns on diagnostics support using an existing storage account. |
|vm_size | Sets the size of the VM. |
|username | Sets the admin username of the VM (note: the password is supplied as a securestring parameter to the generated ARM template). |
|operating_system | Sets the operating system of the VM. A set of samples is provided in the `CommonImages` module. |
|os_disk | Sets the size and type of the OS disk for the VM. |
|add_disk | Adds a data disk to the VM with a specific size and type. |
|add_ssd_disk | Adds a SSD data disk to the VM with a specific size. |
|add_slow_disk | Adds a conventional (non-SSD) data disk to the VM with a specific size. |
|domain_name_prefix | Sets the prefix for the domain name of the VM. |
|address_prefix | Sets the IP address prefix of the VM. |
|subnet_prefix | Sets the subnet prefix of the VM. |
|custom_script | Executes the supplied inline custom script on the VM. |
|custom_script_files | Uploads the supplied set of files, specified by URI, to the VM on creation. |

#### Configuration Members

| Member | Purpose |
|-|-|
| NicName | Provides the resource name of the Network Interface Card (NIC) |
| VnetName | Provides the resource name of the Virtual Network (VNet) |
| SubnetName | Provides the resource name of the subnet |
| IpName | Provides the resource name of the IP Address |
| Hostname | Returns an ARM expression to retrieve the fully-qualified domain name from the virtual machine's DNS settings." |

#### Example

```fsharp
open Farmer
open Farmer.Builders

let myVm = vm {
    name "myFarmerVm"
    username "yourUsername"
    vm_size Vm.Standard_A2
    operating_system Vm.WindowsServer_2012Datacenter
    os_disk 128 Vm.StandardSSD_LRS
    add_ssd_disk 128
    add_slow_disk 512
    custom_script "powershell setup-vm.ps1" // you have to actually *call* the script
    custom_script_files [ "https://foo.bar/foo/setup-vm.ps1" ] 
}
```
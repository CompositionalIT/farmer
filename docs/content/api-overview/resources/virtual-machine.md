---
title: "Virtual Machine"
date: 2022-03-17T09:33:27+05:00
chapter: false
weight: 21
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

|Keyword|Purpose|
|-|-|
|name|Sets the name of the VM.|
|diagnostics_support|Turns on diagnostics support using an automatically created storage account.|
|diagnostics_support_managed|Turns on diagnostics support using an Azure-managed storage account.|
|diagnostics_support_external|Turns on diagnostics support using an existing storage account.|
|vm_size|Sets the size of the VM.|
|priority|Sets the VM Priority. Only one `spot_instance` or `priority` setting is allowed per VM. No priority is set by default. |
|spot_instance|Makes the VM a spot instance. Shorthand for `priority (Spot (<EvictionPolicy>, <maxPrice>)`. Only one `spot_instance` or `priority` setting is allowed per VM.|
|username|Sets the admin username of the VM (note: the password is supplied as a securestring parameter to the generated ARM template).|
|password_parameter|Sets the name of the parameter which contains the admin password for this VM. defaults to "password-for-<VM-name>"|
|operating_system|Sets the operating system of the VM. A set of samples is provided in the `CommonImages` module.|
|os_disk|Sets the size and type of the OS disk for the VM. Note: The default is non-SSD.|
|add_disk|Adds a data disk to the VM with a specific size and type.|
|add_ssd_disk|Adds a SSD data disk to the VM with a specific size.|
|add_slow_disk|Adds a conventional (non-SSD) data disk to the VM with a specific size.|
|no_disk|Excludes a data disk (only an OS disk) - common when mounting cloud storage.|
|domain_name_prefix|Sets the prefix for the domain name of the VM.|
|address_prefix|Sets the IP address prefix of the VM.|
|subnet_prefix|Sets the subnet prefix of the VM.|
|custom_script|Executes the supplied inline custom script on the VM. Supports only one command. Alternatively you can connect VM e.g. with Powershell Invoke-AzVMRunCommand.|
|custom_script_files|Uploads the supplied set of files, specified by URI, to the VM on creation.|
|aad_ssh_login|Adds the `AADSSHLoginForLinux` extension on Linux VM's (requires `system_identity`).|
|custom_data|Sets the custom data field for the VM.|
|public_ip|Specifies or removes the public IP for this VM|
|ip_allocation|Sets the public IP as Dynamic or Static. Default is Dynamic.|
|disable_password_authentication|Disables password authentication on the VM. Must include at least one key if true|
|add_authorized_key|adds one authorized key|
|add_authorized_keys|adds a list of authorized keys|
|add_identity|Adds a managed identity to the Virtual Machine.|
|system_identity|Activates the system identity of the Virtual Machine.|
|private_ip_allocation| Sets the private ip as Dynamic or Static default is dynamic.|
|network_security_group| Sets the Network Security Group (NSG) for VM/NIC. Enables you to create and share firewall rule sets.|
|link_to_network_security_group| Specify an existing Network Security Group (NSG) for VM/NIC.             |
|link_to_vnet|Attaches the VM NIC to a vnet that is deployed in this same template|
|link_to_unmanaged_vnet|Attaches the VM NIC to a vnet that is already deployed|
|link_to_backend_address_pool|Adds the VM network interface to a load balancer backend address pool that is deployed with this VM.|
|link_to_unmanaged_backend_address_pool|Adds the VM network interface to an existing load balancer backend address pool.|

#### Configuration Members

|Member|Purpose|
|-|-|
|NicName|Provides the resource name of the Network Interface Card (NIC)|
|VnetName|Provides the resource name of the Virtual Network (VNet)|
|SubnetName|Provides the resource name of the subnet|
|IpName|Provides the resource name of the IP Address|
|PublicIpAddress|Returns an ARM expression to retrieve public IP address of the virtual machine.|
|Hostname|Returns an ARM expression to retrieve the fully-qualified domain name from the virtual machine's DNS settings."|

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
    custom_data "customData"
    disable_password_authentication true
    add_authorized_key "fooPath" "fooKey"
    add_authorized_keys [("fooPath", "fooKey");("fooPath1", "fooKey1")]
    private_ip_allocation (PrivateIpAddress.StaticPrivateIp (Net.IPAddress.Parse("10.0.0.10")))
}
```

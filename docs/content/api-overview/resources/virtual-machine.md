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

| Keyword                                | Purpose                                                                                                                                                                                                                                                                                                                                                               |
|----------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| name                                   | Sets the name of the VM.                                                                                                                                                                                                                                                                                                                                              |
| diagnostics_support                    | Turns on diagnostics support using an automatically created storage account.                                                                                                                                                                                                                                                                                          |
| diagnostics_support_managed            | Turns on diagnostics support using an Azure-managed storage account.                                                                                                                                                                                                                                                                                                  |
| diagnostics_support_external           | Turns on diagnostics support using an existing storage account.                                                                                                                                                                                                                                                                                                       |
| encryption_at_host                     | This property can be used by user in the request to enable or disable the Host Encryption for the virtual machine or virtual machine scale set. This will enable the encryption for all the disks including Resource/Temp disk at host itself. The default behavior is: The Encryption at host will be disabled unless this property is set to true for the resource. |
| encryption_identity                    | Specifies the Managed Identity used by ADE to get access token for keyvault operations.                                                                                                                                                                                                                                                                               |
| proxy_agent                            | Specifies ProxyAgent settings while creating the virtual machine.                                                                                                                                                                                                                                                                                                     |
| secure_boot                            | UEFI security settings for secure boot.                                                                                                                                                                                                                                                                                                                               |
| vtpm                                   | UEFI security settings for vTPM.                                                                                                                                                                                                                                                                                                                                      |
| security_type                          | Specifies the SecurityType of the virtual machine. It has to be set to any specified value to enable UefiSettings. The default behavior is: UefiSettings will not be enabled unless this property is set.                                                                                                                                                             |
| vm_size                                | Sets the size of the VM.                                                                                                                                                                                                                                                                                                                                              |
| priority                               | Sets the VM Priority. Only one `spot_instance` or `priority` setting is allowed per VM. No priority is set by default.                                                                                                                                                                                                                                                |
| spot_instance                          | Makes the VM a spot instance. Shorthand for `priority (Spot (<EvictionPolicy>, <maxPrice>)`. Only one `spot_instance` or `priority` setting is allowed per VM.                                                                                                                                                                                                        |
| username                               | Sets the admin username of the VM (note: the password is supplied as a securestring parameter to the generated ARM template).                                                                                                                                                                                                                                         |
| password_parameter                     | Sets the name of the parameter which contains the admin password for this VM. defaults to "password-for-<VM-name>"                                                                                                                                                                                                                                                    |
| add_availability_zone                  | Sets the availability zone for the VM.                                                                                                                                                                                                                                                                                                                                |
| operating_system                       | Sets the operating system of the VM. A set of samples is provided in the `CommonImages` module.                                                                                                                                                                                                                                                                       |
| os_disk                                | Sets the size and type of the OS disk for the VM. Note: The default is non-SSD.                                                                                                                                                                                                                                                                                       |
| add_disk                               | Adds a data disk to the VM with a specific size and type.                                                                                                                                                                                                                                                                                                             |
| add_ssd_disk                           | Adds an SSD data disk to the VM with a specific size.                                                                                                                                                                                                                                                                                                                 |
| add_slow_disk                          | Adds a conventional (non-SSD) data disk to the VM with a specific size.                                                                                                                                                                                                                                                                                               |
| attach_os_disk                         | Attaches a newly imported managed disk to the VM as the OS disk. The OS (Windows or Linux) for the image must be specified. When attaching an OS disk, the OS settings such as username, password, and `configData` cannot be set.                                                                                                                                    |
| attach_existing_os_disk                | Attaches an existing managed disk to the VM as the OS disk.                                                                                                                                                                                                                                                                                                           |
| attach_data_disk                       | Attaches a newly imported managed disk to the VM as a data disk.                                                                                                                                                                                                                                                                                                      |
| attach_existing_data_disk              | Attaches an existing managed disk to the VM as a data disk.                                                                                                                                                                                                                                                                                                           |
| no_data_disk                           | Excludes a data disk (only an OS disk) - common when mounting cloud storage.                                                                                                                                                                                                                                                                                          |
| disk_delete_option                     | Sets the delete option for VM disks (OS and data disks). When set to `Delete`, disks will be automatically deleted when the VM is deleted. When set to `Detach` (default), disks are detached but not deleted. Note: Does not apply to Virtual Machine Scale Sets.                                                                                                    |
| nic_delete_option                      | Sets the delete option for network interfaces. When set to `Delete`, NICs will be automatically deleted when the VM is deleted. When set to `Detach` (default), NICs are detached but not deleted. Note: Does not apply to Virtual Machine Scale Sets.                                                                                                                |
| public_ip_delete_option                | Sets the delete option for public IP addresses. When set to `Delete`, public IPs will be automatically deleted when the VM is deleted. When set to `Detach` (default), public IPs are detached but not deleted. Note: Does not apply to Virtual Machine Scale Sets.                                                                                                   |
| delete_attached                        | Convenience method that sets all delete options (disks, NICs, and public IPs) to `Delete` at once. Recommended for most use cases where automatic cleanup of all VM resources is desired. Note: Does not apply to Virtual Machine Scale Sets.                                                                                                                         |
| domain_name_prefix                     | Sets the prefix for the domain name of the VM.                                                                                                                                                                                                                                                                                                                        |
| address_prefix                         | Sets the IP address prefix of the VM.                                                                                                                                                                                                                                                                                                                                 |
| subnet_prefix                          | Sets the subnet prefix of the VM.                                                                                                                                                                                                                                                                                                                                     |
| custom_script                          | Executes the supplied inline custom script on the VM. Supports only one command. Alternatively, you can connect VM e.g. with Powershell Invoke-AzVMRunCommand.                                                                                                                                                                                                        |
| custom_script_files                    | Uploads the supplied set of files, specified by URI, to the VM on creation.                                                                                                                                                                                                                                                                                           |
| aad_ssh_login                          | Adds the `AADSSHLoginForLinux` extension on Linux VM's (requires `system_identity`).                                                                                                                                                                                                                                                                                  |
| custom_data                            | Sets the custom data field for the VM.                                                                                                                                                                                                                                                                                                                                |
| disable_password_authentication        | Disables password authentication on the VM. Must include at least one key if true                                                                                                                                                                                                                                                                                     |
| add_application_security_groups        | Assign this VM to one or more application security groups                                                                                                                                                                                                                                                                                                             |
| add_authorized_key                     | adds one authorized key                                                                                                                                                                                                                                                                                                                                               |
| add_authorized_keys                    | adds a list of authorized keys                                                                                                                                                                                                                                                                                                                                        |
| add_gallery_applications               | Adds one or more gallery applications to this VM.                                                                                                                                                                                                                                                                                                                     |
| add_gallery_applications_install_order | Adds one or more gallery applications and sets the install order in the order they are added.                                                                                                                                                                                                                                                                         |
| add_identity                           | Adds a managed identity to the Virtual Machine.                                                                                                                                                                                                                                                                                                                       |
| system_identity                        | Activates the system identity of the Virtual Machine.                                                                                                                                                                                                                                                                                                                 |
| public_ip                              | Specifies or removes the public IP for this VM                                                                                                                                                                                                                                                                                                                        |
| public_ip_sku                          | Specify the Public IP SKU for the generated Public IP resource.                                                                                                                                                                                                                                                                                                       |
| ip_allocation                          | Sets the *public* IP as Dynamic or Static. The default is dynamic.                                                                                                                                                                                                                                                                                                    |
| private_ip_allocation                  | Sets the *private* IP as Dynamic or Static. The default is dynamic.                                                                                                                                                                                                                                                                                                   |
| ip_forwarding                          | Enable or disable IP forwarding on the primary network interface. Secondary NICs will leave it undefined.                                                                                                                                                                                                                                                             |
| accelerated_networking                 | Enable or disable accelerated networking on all network interfaces generated for the VM.                                                                                                                                                                                                                                                                              |
| add_ip_configuration                   | Add `ipConfig` definitions to add additional IP addresses or connect to multiple subnets. Connecting to additional subnets will generate a NIC for each subnet.                                                                                                                                                                                                       |
| network_security_group                 | Sets the Network Security Group (NSG) for VM/NIC. Enables you to create and share firewall rule sets.                                                                                                                                                                                                                                                                 |
| link_to_network_security_group         | Specify an existing Network Security Group (NSG) for VM/NIC.                                                                                                                                                                                                                                                                                                          |
| link_application_security_groups       | Link this VM to one or more application security groups (no dependency generated).                                                                                                                                                                                                                                                                                    |
| link_to_vnet                           | Attaches the VM NIC to a vnet that is deployed in this same template                                                                                                                                                                                                                                                                                                  |
| link_to_unmanaged_vnet                 | Attaches the VM NIC to a vnet that is already deployed                                                                                                                                                                                                                                                                                                                |
| link_to_backend_address_pool           | Adds the VM network interface to a load balancer backend address pool that is deployed with this VM.                                                                                                                                                                                                                                                                  |
| link_to_unmanaged_backend_address_pool | Adds the VM network interface to an existing load balancer backend address pool.                                                                                                                                                                                                                                                                                      |

##### VM Gallery Application Builder

The `vmGalleryApplicationBuilder` is used to add a gallery application to a VM.

| Keyword                             | Purpose                                                                                         |
|-------------------------------------|-------------------------------------------------------------------------------------------------|
| enable_automatic_upgrade            | Optional. Enables automatic upgrade of the application when a new version is released.          |
| order                               | Optional. The order in which applications should be installed.                                  |
| package_reference_id                | Required. References an existing gallery application version to install on the VM.              |
| tags                                | Optional. Specifies a passthrough value for a more generic context.	                              |                                                                      |
| treat_failure_as_deployment_failure | Optional. If true, any failure for any operation in the VmApplication will fail the deployment  |                                                                                 |


#### Configuration Members

|Member|Purpose|
|-|-|
|NicName|Provides the resource name of the Network Interface Card (NIC)|
|VnetName|Provides the resource name of the Virtual Network (VNet)|
|SubnetName|Provides the resource name of the subnet|
|IpName|Provides the resource name of the IP Address|
|PublicIpAddress|Returns an ARM expression to retrieve the public IP address of the virtual machine.|
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

#### Automatic Resource Cleanup Example

Use the `delete_attached` keyword to automatically clean up all VM-associated resources when the VM is deleted:

```fsharp
open Farmer
open Farmer.Builders

let myVm = vm {
    name "myFarmerVm"
    username "yourUsername"
    vm_size Vm.Standard_A2
    operating_system Vm.UbuntuServer_2204LTS
    
    // All attached resources (disks, NICs, public IPs) will be deleted with the VM
    delete_attached
}
```

For fine-grained control over which resources should be deleted:

```fsharp
let myVm = vm {
    name "myFarmerVm"
    username "yourUsername"
    vm_size Vm.Standard_A2
    operating_system Vm.UbuntuServer_2204LTS
    
    // Only delete disks and NICs, but keep public IPs
    disk_delete_option Vm.DiskDeleteOption.Delete
    nic_delete_option Vm.NicDeleteOption.Delete
    public_ip_delete_option Vm.PublicIpDeleteOption.Detach
}
```

> **Note**: The `deleteOption` feature is not supported on Virtual Machine Scale Sets. If you use `delete_attached` or any of the `*_delete_option` keywords with a VM Scale Set, the delete options will be ignored in the generated ARM template.

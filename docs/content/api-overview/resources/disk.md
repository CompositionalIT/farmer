---
title: "Disk"
date: 2023-02-16T09:19:00+00:00
chapter: false
weight: 22
---

#### Overview
The `disk` builder creates managed disks that may be attached to a virtual machine. With managed disks, the storage account backing the disk is handled by Azure. Disks can be created as empty disks or by importing a virtual hard disk from an existing storage account.

* Disks (`Microsoft.Compute/disks`)

#### Builder Keywords

|Keyword|Purpose|
|-|-|
|name|Sets the name of the managed disk.|
|sku|Sets the type of disk, such as `Standard_LRS`, `StandardSSD_LRS`, `Premium_LRS`, or `UltraSSD_LRS`.|
|add_availability_zone|When a disk will be attached to a VM in an availability zone, the same availability zone should be set here.|
|os_type|Sets the OS for the managed disk - `Windows` or `Linux`.|
|create_empty|Creates an empty disk of the given size.|
|import|Imports a disk from an existing virtual hard drive (.vhd) file.|

#### Example

```fsharp
open Farmer
open Farmer.Builders

let emptyDisk =
    disk {
        name "empty-disk"
        os_type Linux
        create_empty 128<Gb>
    }

let importedDisk =
    disk {
        name "imported-disk-image"
        sku Vm.DiskType.Premium_LRS
        os_type Linux

        // Provide the URI for the disk image and the ARM resource Id for the storage account.
        import
            (System.Uri
                "https://mystorageaccount.blob.core.windows.net/vhds/MyVirtualHardDisk.vhd")
            (Arm.Storage.storageAccounts.resourceId "mystorageaccount")
    }
```

Multiple disks can be created and then attached to a virtual machine:

```fsharp
let disk0 =
    disk {
        name "disk-0"
        sku Vm.DiskType.Premium_LRS
        os_type Linux
        create_empty 1024<Gb>
        add_availability_zone "1"
    }

let disk1 =
    disk {
        name "disk-1"
        sku Vm.DiskType.Standard_LRS
        os_type Linux
        import
            (System.Uri
                "https://mystorageaccount.blob.core.windows.net/vhds/MyVirtualHardDisk.vhd")
            (Arm.Storage.storageAccounts.resourceId "mystorageaccount")
        add_availability_zone "1"
    }

let vmWithAttachedDisks =
    vm {
        name "vm-with-attached-disks"
        vm_size Standard_B1ms
        operating_system UbuntuServer_2204LTS
        username "azureuser"
        add_availability_zone "1"
        attach_data_disk disk0
        attach_data_disk disk1
    }

let deployment =
    arm {
        add_resources
            [
                disk0
                disk1
                vmWithAttachedDisks
            ]
    }

```

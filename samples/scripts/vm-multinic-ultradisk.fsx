#r "nuget: Farmer"

open System
open Farmer
open Farmer.Builders

let privateNet = vnet {
    name "private-net"

    build_address_spaces [
        addressSpace {
            space "10.28.0.0/16"
            build_subnet "app-subnet" 27
            build_subnet "database-subnet" 28
        }
    ]
}

let appServerZone1 = vm {
    name "app-server-z1"
    add_availability_zone "1"
    vm_size Vm.Standard_B1ms
    username "azureuser"
    operating_system Vm.UbuntuServer_2004LTS
    os_disk 128 Vm.Premium_LRS
    no_data_disk
    disable_password_authentication true
    link_to_vnet "private-net"
    subnet_name "app-subnet"
    // Static IP in the app subnet
    private_ip_allocation (StaticPrivateIp(Net.IPAddress.Parse "10.28.0.4"))

    add_ip_configurations [
        // This will generate an additional static IP in the VM's subnet (app-subnet)
        ipConfig { private_ip_allocation (StaticPrivateIp(Net.IPAddress.Parse "10.28.0.5")) }
        // This will generate an additional NIC for the database subnet
        ipConfig {
            // Assign static IP for the database subnet since database IP's are static
            private_ip_allocation (StaticPrivateIp(Net.IPAddress.Parse "10.28.0.38"))
            subnet_name (ResourceName "database-subnet")
        }
    ]

    add_authorized_keys [
        "/home/azureuser/.ssh/authorized_keys",
        (IO.File.ReadAllText(
            IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh/id_rsa.pub")
        ))
    ]
}

// Second VM in the other availability zone.
let appServerZone2 = vm {
    name "app-server-z2"
    add_availability_zone "2"
    vm_size Vm.Standard_B1ms
    username "azureuser"
    operating_system Vm.UbuntuServer_2004LTS
    os_disk 128 Vm.Premium_LRS
    no_data_disk
    disable_password_authentication true
    link_to_vnet "private-net"
    private_ip_allocation (StaticPrivateIp(Net.IPAddress.Parse "10.28.0.6"))

    add_ip_configurations [
        ipConfig { private_ip_allocation (StaticPrivateIp(Net.IPAddress.Parse "10.28.0.7")) }
        ipConfig {
            private_ip_allocation (StaticPrivateIp(Net.IPAddress.Parse "10.28.0.39"))
            subnet_name (ResourceName "database-subnet")
        }
    ]

    add_authorized_keys [
        "/home/azureuser/.ssh/authorized_keys",
        (IO.File.ReadAllText(
            IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh/id_rsa.pub")
        ))
    ]
}

// Database server in zone 1 with ultra disk
let databaseServerZone1 = vm {
    name "database-server-z1"
    add_availability_zone "1"
    vm_size Vm.Standard_D2s_v5
    username "azureuser"
    operating_system Vm.UbuntuServer_2004LTS
    os_disk 128 Vm.Premium_LRS
    // Ultra disk for high performance database needs.
    add_disk 4096 Vm.UltraSSD_LRS
    disable_password_authentication true
    link_to_vnet "private-net"
    subnet_name "database-subnet"
    // Static IP in the database subnet
    private_ip_allocation (StaticPrivateIp(Net.IPAddress.Parse "10.28.0.36"))
    public_ip None

    add_authorized_keys [
        "/home/azureuser/.ssh/authorized_keys",
        (IO.File.ReadAllText(
            IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh/id_rsa.pub")
        ))
    ]
}

// Database server in zone 2 with ultra disk
let databaseServerZone2 = vm {
    name "database-server-z2"
    add_availability_zone "2"
    vm_size Vm.Standard_D2s_v5
    username "azureuser"
    operating_system Vm.UbuntuServer_2004LTS
    os_disk 128 Vm.Premium_LRS
    // Ultra disk for high performance database needs.
    add_disk 4096 Vm.UltraSSD_LRS
    disable_password_authentication true
    link_to_vnet "private-net"
    subnet_name "database-subnet"
    // Static IP in the database subnet
    private_ip_allocation (StaticPrivateIp(Net.IPAddress.Parse "10.28.0.37"))
    public_ip None

    add_authorized_keys [
        "/home/azureuser/.ssh/authorized_keys",
        (IO.File.ReadAllText(
            IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh/id_rsa.pub")
        ))
    ]
}

let deployment = arm {
    // West US 3 - https://azure.microsoft.com/en-us/blog/expanding-cloud-services-microsoft-launches-its-sustainable-datacenter-region-in-arizona/
    location Location.WestUS3

    add_resources [
        privateNet
        appServerZone1
        appServerZone2
        databaseServerZone1
        databaseServerZone2
    ]
}

deployment |> Writer.quickWrite "vm-multinic-ultradisk"

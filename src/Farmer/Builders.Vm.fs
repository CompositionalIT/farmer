[<AutoOpen>]
module Farmer.Resources.VirtualMachine

open Farmer.Helpers
open Farmer.Models
open Farmer

let makeName (vmName:ResourceName) elementType = sprintf "%s-%s" vmName.Value elementType
let makeResourceName vmName = makeName vmName >> ResourceName
type VmConfig =
    { Name : ResourceName
      DiagnosticsStorageAccount : ResourceRef option

      Username : string
      Image : ImageDefinition
      Size : VMSize
      OsDisk : DiskInfo
      DataDisks : DiskInfo list

      DomainNamePrefix : string option
      AddressPrefix : string
      SubnetPrefix : string }

    member this.NicName = makeResourceName this.Name "nic"
    member this.VnetName = makeResourceName this.Name "vnet"
    member this.SubnetName = makeResourceName this.Name "subnet"
    member this.IpName = makeResourceName this.Name "ip"
    member this.Hostname = sprintf "reference('%s').dnsSettings.fqdn" this.IpName.Value |> ArmExpression
type VirtualMachineBuilder() =
    member __.Yield _ =
        { Name = ResourceName.Empty
          DiagnosticsStorageAccount = None
          Size = VMSize.Basic_A0
          Username = "admin"
          Image = CommonImages.WindowsServer_2012Datacenter
          DataDisks = [ ]
          DomainNamePrefix = None
          OsDisk = { Size = 128; DiskType = Standard_LRS }
          AddressPrefix = "10.0.0.0/16"
          SubnetPrefix = "10.0.0.0/24" }

    member __.Run (state:VmConfig) =
        { state with
            DiagnosticsStorageAccount =
                state.DiagnosticsStorageAccount
                |> Option.map(fun account ->
                    match account with
                    | AutomaticPlaceholder ->
                        state.Name
                        |> sanitiseStorage
                        |> sprintf "%sstorage"
                        |> ResourceName
                        |> AutomaticallyCreated
                    | External _
                    | AutomaticallyCreated _ ->
                        account)
            DataDisks = match state.DataDisks with [] -> [ { Size = 1024; DiskType = Standard_LRS } ] | other -> other
        }

    /// Sets the name of the VM.
    [<CustomOperation "name">]
    member __.Name(state:VmConfig, name) = { state with Name = name }
    member this.Name(state:VmConfig, name) = this.Name(state, ResourceName name)
    /// Turns on diagnostics support using an automatically created storage account.
    [<CustomOperation "diagnostics_support">]
    member __.StorageAccountName(state:VmConfig) = { state with DiagnosticsStorageAccount = Some AutomaticPlaceholder }
    /// Turns on diagnostics support using an externally managed storage account.
    [<CustomOperation "diagnostics_support_external">]
    member __.StorageAccountNameExternal(state:VmConfig, name) = { state with DiagnosticsStorageAccount = Some (External name) }
    /// Sets the size of the VM.
    [<CustomOperation "vm_size">]
    member __.VmSize(state:VmConfig, size) = { state with Size = size }
    /// Sets the admin username of the VM (note: the password is supplied as a securestring parameter to the generated ARM template).
    [<CustomOperation "username">]
    member __.Username(state:VmConfig, username) = { state with Username = username }
    /// Sets the operating system of the VM. A set of samples is provided in the `CommonImages` module.
    [<CustomOperation "operating_system">]
    member __.ConfigureOs(state:VmConfig, image) =
        { state with Image = image }
    member __.ConfigureOs(state:VmConfig, (offer, publisher, sku)) =
        { state with Image = { Offer = offer; Publisher = publisher; Sku = sku } }
    /// Sets the size and type of the OS disk for the VM.
    [<CustomOperation "os_disk">]
    member __.OsDisk(state:VmConfig, size, diskType) =
        { state with OsDisk = { Size = size; DiskType = diskType } }
    /// Adds a data disk to the VM with a specific size and type.
    [<CustomOperation "add_disk">]
    member __.AddDisk(state:VmConfig, size, diskType) = { state with DataDisks = { Size = size; DiskType = diskType } :: state.DataDisks }
    /// Adds a SSD data disk to the VM with a specific size.
    [<CustomOperation "add_ssd_disk">]
    member this.AddSsd(state:VmConfig, size) = this.AddDisk(state, size, StandardSSD_LRS)
    /// Adds a conventional (non-SSD) data disk to the VM with a specific size.
    [<CustomOperation "add_slow_disk">]
    member this.AddSlowDisk(state:VmConfig, size) = this.AddDisk(state, size, Standard_LRS)
    /// Sets the prefix for the domain name of the VM.
    [<CustomOperation "domain_name_prefix">]
    member __.DomainNamePrefix(state:VmConfig, prefix) = { state with DomainNamePrefix = prefix }
    /// Sets the IP address prefix of the VM.
    [<CustomOperation "address_prefix">]
    member __.AddressPrefix(state:VmConfig, prefix) = { state with AddressPrefix = prefix }
    /// Sets the subnet prefix of the VM.
    [<CustomOperation "subnet_prefix">]
    member __.SubnetPrefix(state:VmConfig, prefix) = { state with SubnetPrefix = prefix }

module Converters =
    open VM

    let vm location (config:VmConfig) =
        let storage =
            match config.DiagnosticsStorageAccount with
            | Some (AutomaticallyCreated account) ->
                Some
                    { StorageAccount.Name = account
                      Location = location
                      Sku = Storage.Sku.StandardLRS
                      Containers = [] }
            | Some AutomaticPlaceholder
            | Some (External _)
            | None ->
                None
        let vm =
            { Name = config.Name
              Location = location
              StorageAccount =
                config.DiagnosticsStorageAccount
                |> Option.bind(function
                    | AutomaticPlaceholder -> None
                    | AutomaticallyCreated r -> Some r
                    | External r -> Some r)
              NetworkInterfaceName = config.NicName
              Size = config.Size
              Credentials = {| Username = config.Username; Password = SecureParameter (sprintf "password-for-%s" config.Name.Value) |}
              Image = config.Image
              OsDisk = config.OsDisk
              DataDisks = config.DataDisks }
        let nic =
            { Name = config.NicName
              Location = location
              IpConfigs = [
                {| SubnetName = config.SubnetName
                   PublicIpName = config.IpName |} ]
              VirtualNetwork = config.VnetName }
        let vnet =
            { Name = config.VnetName
              Location = location
              AddressSpacePrefixes = [ config.AddressPrefix ]
              Subnets = [
                  {| Name = config.SubnetName
                     Prefix = config.SubnetPrefix |}
              ] }
        let ip =
            { Name = config.IpName
              Location = location
              DomainNameLabel = config.DomainNamePrefix }
        {| Storage = storage; Vm = vm; Nic = nic; Vnet = vnet; Ip = ip |}
    module Outputters =
        let publicIpAddress (ipAddress:VM.PublicIpAddress) = {|
            ``type`` = "Microsoft.Network/publicIPAddresses"
            apiVersion = "2018-11-01"
            name = ipAddress.Name.Value
            location = ipAddress.Location.ArmValue
            properties =
               match ipAddress.DomainNameLabel with
               | Some label ->
                   box
                       {| publicIPAllocationMethod = "Dynamic"
                          dnsSettings = {| domainNameLabel = label.ToLower() |}
                       |}
               | None ->
                   box {| publicIPAllocationMethod = "Dynamic" |}
        |}
        let virtualNetwork (vnet:VM.VirtualNetwork) = {|
            ``type`` = "Microsoft.Network/virtualNetworks"
            apiVersion = "2018-11-01"
            name = vnet.Name.Value
            location = vnet.Location.ArmValue
            properties =
                 {| addressSpace = {| addressPrefixes = vnet.AddressSpacePrefixes |}
                    subnets =
                     vnet.Subnets
                     |> List.map(fun subnet ->
                        {| name = subnet.Name.Value
                           properties = {| addressPrefix = subnet.Prefix |}
                        |})
                 |}
        |}
        let networkInterface (nic:VM.NetworkInterface) = {|
            ``type`` = "Microsoft.Network/networkInterfaces"
            apiVersion = "2018-11-01"
            name = nic.Name.Value
            location = nic.Location.ArmValue
            dependsOn = [
                nic.VirtualNetwork.Value
                for config in nic.IpConfigs do
                    config.PublicIpName.Value
            ]
            properties =
                {| ipConfigurations =
                     nic.IpConfigs
                     |> List.mapi(fun index ipConfig ->
                         {| name = sprintf "ipconfig%i" (index + 1)
                            properties =
                             {| privateIPAllocationMethod = "Dynamic"
                                publicIPAddress = {| id = sprintf "[resourceId('Microsoft.Network/publicIPAddresses','%s')]" ipConfig.PublicIpName.Value |}
                                subnet = {| id = sprintf "[resourceId('Microsoft.Network/virtualNetworks/subnets', '%s', '%s')]" nic.VirtualNetwork.Value ipConfig.SubnetName.Value |}
                             |}
                         |})
                |}
        |}
        let virtualMachine (vm:VM.VirtualMachine) = {|
            ``type`` = "Microsoft.Compute/virtualMachines"
            apiVersion = "2018-10-01"
            name = vm.Name.Value
            location = vm.Location.ArmValue
            dependsOn = [
                vm.NetworkInterfaceName.Value
                match vm.StorageAccount with
                | Some s -> s.Value
                | None -> ()
            ]
            properties =
             {| hardwareProfile = {| vmSize = vm.Size |}
                osProfile =
                 {|
                    computerName = vm.Name.Value
                    adminUsername = vm.Credentials.Username
                    adminPassword = vm.Credentials.Password.AsArmRef.Eval()
                 |}
                storageProfile =
                    let vmNameLowerCase = vm.Name.Value.ToLower()
                    {| imageReference =
                        {| publisher = vm.Image.Publisher
                           offer = vm.Image.Offer
                           sku = vm.Image.Sku
                           version = "latest" |}
                       osDisk =
                        {| createOption = "FromImage"
                           name = sprintf "%s-osdisk" vmNameLowerCase
                           diskSizeGB = vm.OsDisk.Size
                           managedDisk = {| storageAccountType = string vm.OsDisk.DiskType |}
                        |}
                       dataDisks =
                        vm.DataDisks
                        |> List.mapi(fun lun dataDisk ->
                            {| createOption = "Empty"
                               name = sprintf "%s-datadisk-%i" vmNameLowerCase lun
                               diskSizeGB = dataDisk.Size
                               lun = lun
                               managedDisk = {| storageAccountType = string dataDisk.DiskType |} |})
                    |}
                networkProfile =
                    {| networkInterfaces = [
                        {| id = sprintf "[resourceId('Microsoft.Network/networkInterfaces','%s')]" vm.NetworkInterfaceName.Value |}
                       ]
                    |}
                diagnosticsProfile =
                    match vm.StorageAccount with
                    | Some storageAccount ->
                        box
                            {| bootDiagnostics =
                                {| enabled = true
                                   storageUri = sprintf "[reference('%s').primaryEndpoints.blob]" storageAccount.Value
                                |}
                            |}
                    | None ->
                        box {| bootDiagnostics = {| enabled = false |} |}
            |}
        |}


type ArmBuilder.ArmBuilder with
    member this.AddResource(state:ArmConfig, config:VmConfig) =
        let output = Converters.vm state.Location config
        let resources = [
            Vm output.Vm
            Vnet output.Vnet
            Ip output.Ip
            Nic output.Nic
            match output.Storage with Some storage -> StorageAccount storage | None -> ()
        ]
        { state with Resources = state.Resources @ resources }
    member this.AddResources (state, configs) = addResources<VmConfig> this.AddResource state configs

let vm = VirtualMachineBuilder()

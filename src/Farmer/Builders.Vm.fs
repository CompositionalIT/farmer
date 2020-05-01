[<AutoOpen>]
module Farmer.Resources.VirtualMachine

open Farmer.Helpers
open Farmer

type PublicIpAddress =
    { Name : ResourceName
      Location : Location
      DomainNameLabel : string option }
    interface IResource with
        member this.ResourceName = this.Name
        member this.ToArmObject() =
            {| ``type`` = "Microsoft.Network/publicIPAddresses"
               apiVersion = "2018-11-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               properties =
                  match this.DomainNameLabel with
                  | Some label ->
                      box
                          {| publicIPAllocationMethod = "Dynamic"
                             dnsSettings = {| domainNameLabel = label.ToLower() |}
                          |}
                  | None ->
                      box {| publicIPAllocationMethod = "Dynamic" |}
            |} :> _
type VirtualNetwork =
    { Name : ResourceName
      Location : Location
      AddressSpacePrefixes : string list
      Subnets : {| Name : ResourceName; Prefix : string |} list }
    interface IResource with
        member this.ResourceName = this.Name
        member this.ToArmObject() =
            {| ``type`` = "Microsoft.Network/virtualNetworks"
               apiVersion = "2018-11-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               properties =
                    {| addressSpace = {| addressPrefixes = this.AddressSpacePrefixes |}
                       subnets =
                        this.Subnets
                        |> List.map(fun subnet ->
                           {| name = subnet.Name.Value
                              properties = {| addressPrefix = subnet.Prefix |}
                           |})
                    |}
            |} :> _
type NetworkInterface =
    { Name : ResourceName
      Location : Location
      IpConfigs :
        {| SubnetName : ResourceName
           PublicIpName : ResourceName |} list
      VirtualNetwork : ResourceName }
    interface IResource with
        member this.ResourceName = this.Name
        member this.ToArmObject() =
            {| ``type`` = "Microsoft.Network/networkInterfaces"
               apiVersion = "2018-11-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               dependsOn = [
                   this.VirtualNetwork.Value
                   for config in this.IpConfigs do
                       config.PublicIpName.Value
               ]
               properties =
                   {| ipConfigurations =
                        this.IpConfigs
                        |> List.mapi(fun index ipConfig ->
                            {| name = sprintf "ipconfig%i" (index + 1)
                               properties =
                                {| privateIPAllocationMethod = "Dynamic"
                                   publicIPAddress = {| id = sprintf "[resourceId('Microsoft.Network/publicIPAddresses','%s')]" ipConfig.PublicIpName.Value |}
                                   subnet = {| id = sprintf "[resourceId('Microsoft.Network/virtualNetworks/subnets', '%s', '%s')]" this.VirtualNetwork.Value ipConfig.SubnetName.Value |}
                                |}
                            |})
                   |}
            |} :> _
type VirtualMachine =
    { Name : ResourceName
      Location : Location
      StorageAccount : ResourceName option
      Size : VMSize
      Credentials : {| Username : string; Password : SecureParameter |}
      Image : ImageDefinition
      OsDisk : DiskInfo
      DataDisks : DiskInfo list
      NetworkInterfaceName : ResourceName }
    interface IResource with
        member this.ResourceName = this.Name
        member this.ToArmObject() =
            {| ``type`` = "Microsoft.Compute/virtualMachines"
               apiVersion = "2018-10-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               dependsOn = [
                   this.NetworkInterfaceName.Value
                   match this.StorageAccount with
                   | Some s -> s.Value
                   | None -> ()
               ]
               properties =
                {| hardwareProfile = {| vmSize = this.Size.ArmValue |}
                   osProfile =
                    {|
                       computerName = this.Name.Value
                       adminUsername = this.Credentials.Username
                       adminPassword = this.Credentials.Password.AsArmRef.Eval()
                    |}
                   storageProfile =
                       let vmNameLowerCase = this.Name.Value.ToLower()
                       {| imageReference =
                           {| publisher = this.Image.Publisher.ArmValue
                              offer = this.Image.Offer.ArmValue
                              sku = this.Image.Sku.ArmValue
                              version = "latest" |}
                          osDisk =
                           {| createOption = "FromImage"
                              name = sprintf "%s-osdisk" vmNameLowerCase
                              diskSizeGB = this.OsDisk.Size
                              managedDisk = {| storageAccountType = this.OsDisk.DiskType.ArmValue |}
                           |}
                          dataDisks =
                           this.DataDisks
                           |> List.mapi(fun lun dataDisk ->
                               {| createOption = "Empty"
                                  name = sprintf "%s-datadisk-%i" vmNameLowerCase lun
                                  diskSizeGB = dataDisk.Size
                                  lun = lun
                                  managedDisk = {| storageAccountType = string dataDisk.DiskType |} |})
                       |}
                   networkProfile =
                       {| networkInterfaces = [
                           {| id = sprintf "[resourceId('Microsoft.Network/networkInterfaces','%s')]" this.NetworkInterfaceName.Value |}
                          ]
                       |}
                   diagnosticsProfile =
                       match this.StorageAccount with
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
            |} :> _

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
    interface IResourceBuilder with
        member this.BuildResources location _ = [
            let storage =
                match this.DiagnosticsStorageAccount with
                | Some (AutomaticallyCreated account) ->
                    Some
                        { StorageAccount.Name = account
                          Location = location
                          Sku = Standard_LRS
                          Containers = [] }
                | Some AutomaticPlaceholder
                | Some (External _)
                | None ->
                    None
            let vm =
                { Name = this.Name
                  Location = location
                  StorageAccount =
                    this.DiagnosticsStorageAccount
                    |> Option.bind(function
                        | AutomaticPlaceholder -> None
                        | AutomaticallyCreated r -> Some r
                        | External r -> Some r)
                  NetworkInterfaceName = this.NicName
                  Size = this.Size
                  Credentials = {| Username = this.Username; Password = SecureParameter (sprintf "password-for-%s" this.Name.Value) |}
                  Image = this.Image
                  OsDisk = this.OsDisk
                  DataDisks = this.DataDisks }
            let nic =
                { Name = this.NicName
                  Location = location
                  IpConfigs = [
                    {| SubnetName = this.SubnetName
                       PublicIpName = this.IpName |} ]
                  VirtualNetwork = this.VnetName }
            let vnet =
                { Name = this.VnetName
                  Location = location
                  AddressSpacePrefixes = [ this.AddressPrefix ]
                  Subnets = [
                      {| Name = this.SubnetName
                         Prefix = this.SubnetPrefix |}
                  ] }
            let ip =
                { Name = this.IpName
                  Location = location
                  DomainNameLabel = this.DomainNamePrefix }
            match storage with Some storage -> NewResource storage | None -> ()
            NewResource vm
            NewResource nic
            NewResource vnet
            NewResource ip
        ]

type VirtualMachineBuilder() =
    member __.Yield _ =
        { Name = ResourceName.Empty
          DiagnosticsStorageAccount = None
          Size = Basic_A0
          Username = "admin"
          Image = WindowsServer_2012Datacenter
          DataDisks = [ ]
          DomainNamePrefix = None
          OsDisk = { Size = 128; DiskType = DiskType.Standard_LRS }
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
            DataDisks = match state.DataDisks with [] -> [ { Size = 1024; DiskType = DiskType.Standard_LRS } ] | other -> other
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
    member this.AddSlowDisk(state:VmConfig, size) = this.AddDisk(state, size, DiskType.Standard_LRS)
    /// Sets the prefix for the domain name of the VM.
    [<CustomOperation "domain_name_prefix">]
    member __.DomainNamePrefix(state:VmConfig, prefix) = { state with DomainNamePrefix = prefix }
    /// Sets the IP address prefix of the VM.
    [<CustomOperation "address_prefix">]
    member __.AddressPrefix(state:VmConfig, prefix) = { state with AddressPrefix = prefix }
    /// Sets the subnet prefix of the VM.
    [<CustomOperation "subnet_prefix">]
    member __.SubnetPrefix(state:VmConfig, prefix) = { state with SubnetPrefix = prefix }

let vm = VirtualMachineBuilder()

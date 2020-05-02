[<AutoOpen>]
module Farmer.Resources.VirtualMachine

open Farmer
open Farmer.Helpers
open Arm.Compute
open Arm.Network
open Arm.Storage

let makeName (vmName:ResourceName) elementType = sprintf "%s-%s" vmName.Value elementType
let makeResourceName vmName = makeName vmName >> ResourceName

/// The type of disk to use.
type DiskType =
    | StandardSSD_LRS
    | Standard_LRS
    | Premium_LRS
    member this.ArmValue = match this with x -> x.ToString()

/// Represents a disk in a VM.
type DiskInfo = { Size : int; DiskType : DiskType }

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
                        { Name = account
                          Location = location
                          Sku = StorageSku.Standard_LRS
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
                  OsDisk = {| Size = this.OsDisk.Size; DiskType = string this.OsDisk.DiskType |}
                  DataDisks = [
                    for dataDisk in this.DataDisks do
                        {| Size = dataDisk.Size
                           DiskType = string dataDisk.DiskType |}
                  ] }
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

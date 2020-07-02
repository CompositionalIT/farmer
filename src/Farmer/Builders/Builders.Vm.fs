[<AutoOpen>]
module Farmer.Builders.VirtualMachine

open Farmer
open Farmer.CoreTypes
open Farmer.Vm
open Farmer.Helpers
open Farmer.Arm.Compute
open Farmer.Arm.Network
open Farmer.Arm.Storage
open System

let makeName (vmName:ResourceName) elementType = sprintf "%s-%s" vmName.Value elementType
let makeResourceName vmName = makeName vmName >> ResourceName

type VmConfig =
    { Name : ResourceName
      DiagnosticsStorageAccount : ResourceRef option

      Username : string option
      Image : ImageDefinition
      Size : VMSize
      OsDisk : DiskInfo
      DataDisks : DiskInfo list

      CustomScript : string option
      CustomScriptFiles : Uri list

      DomainNamePrefix : string option

      VNetName : ResourceRef
      AddressPrefix : string
      SubnetPrefix : string
      SubnetName : ResourceName option

      DependsOn : ResourceName list }

    member this.NicName = makeResourceName this.Name "nic"
    member this.IpName = makeResourceName this.Name "ip"
    member this.Hostname = sprintf "reference('%s').dnsSettings.fqdn" this.IpName.Value |> ArmExpression.create

    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            // VM itself
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
              Credentials =
                match this.Username with
                | Some username ->
                    {| Username = username
                       Password = SecureParameter (sprintf "password-for-%s" this.Name.Value) |}
                | None ->
                    failwithf "You must specify a username for virtual machine %s" this.Name.Value
              Image = this.Image
              OsDisk = this.OsDisk
              DataDisks = this.DataDisks }

            // Custom Script
            match this.CustomScript with
            | Some script ->
                { Name = this.Name.Map(sprintf "%s-custom-script")
                  Location = location
                  VirtualMachine = this.Name
                  OS = this.Image.OS
                  ScriptContents = script
                  FileUris = this.CustomScriptFiles }
            | None ->
                ()

            let vnetName =
                this.VNetName.ResourceNameOpt
                |> Option.defaultValue (makeResourceName this.Name "vnet")

            let subnetName =
                this.SubnetName
                |> Option.defaultValue (makeResourceName this.Name "subnet")

            // NIC
            { Name = this.NicName
              Location = location
              IpConfigs = [
                {| SubnetName = subnetName
                   PublicIpName = this.IpName |} ]
              VirtualNetwork = vnetName }

            // VNET
            match this.VNetName with
            | AutomaticallyCreated _
            | AutomaticPlaceholder ->
                { Name = vnetName
                  Location = location
                  AddressSpacePrefixes = [ this.AddressPrefix ]
                  Subnets = [
                      {| Name = subnetName
                         Prefix = this.SubnetPrefix
                         Delegations = [] |}
                  ]
                }
            | External _ ->
                ()

            // IP Address
            { Name = this.IpName
              Location = location
              DomainNameLabel = this.DomainNamePrefix }

            // Storage account - optional
            match this.DiagnosticsStorageAccount with
            | Some (AutomaticallyCreated account) ->
                { Name = account
                  Location = location
                  Sku = Storage.Standard_LRS }
            | Some AutomaticPlaceholder
            | Some (External _)
            | None ->
                ()
        ]

type VirtualMachineBuilder() =
    member __.Yield _ =
        { Name = ResourceName.Empty
          DiagnosticsStorageAccount = None
          Size = Basic_A0
          Username = None
          Image = WindowsServer_2012Datacenter
          DataDisks = []
          CustomScript = None
          CustomScriptFiles = []
          DomainNamePrefix = None
          OsDisk = { Size = 128; DiskType = DiskType.Standard_LRS }
          AddressPrefix = "10.0.0.0/16"
          SubnetPrefix = "10.0.0.0/24"
          VNetName = AutomaticPlaceholder
          SubnetName = None//makeResourceName ResourceName.Empty "subnet"
          DependsOn = [] }

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
            DataDisks =
                match state.DataDisks with
                | [] -> [ { Size = 1024; DiskType = DiskType.Standard_LRS } ]
                | other -> other
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
    member __.Username(state:VmConfig, username) = { state with Username = Some username }
    /// Sets the operating system of the VM. A set of samples is provided in the `CommonImages` module.
    [<CustomOperation "operating_system">]
    member __.ConfigureOs(state:VmConfig, image) =
        { state with Image = image }
    member __.ConfigureOs(state:VmConfig, (os, offer, publisher, sku)) =
        { state with Image = { OS = os; Offer = Offer offer; Publisher = Publisher publisher; Sku = ImageSku sku } }
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
    /// Sets the subnet name of the VM.
    [<CustomOperation "subnet_name">]
    member __.SubnetName(state:VmConfig, name) = { state with SubnetName = Some name }
    member this.SubnetName(state:VmConfig, name) = this.SubnetName(state, ResourceName name)
    /// Uses an external VNet instead of creating a new one.
    [<CustomOperation "link_to_vnet">]
    member __.VNetName(state:VmConfig, name) = { state with VNetName = External (ResourceName name) }
    member __.VNetName(state:VmConfig, vnet:Arm.Network.VirtualNetwork) = { state with VNetName = External vnet.Name }
    [<CustomOperation "depends_on">]
    member __.DependsOn(state:VmConfig, resourceName) = { state with DependsOn = resourceName :: state.DependsOn }
    member __.DependsOn(state:VmConfig, resource:IBuilder) = { state with DependsOn = resource.DependencyName :: state.DependsOn }
    member __.DependsOn(state:VmConfig, resource:IArmResource) = { state with DependsOn = resource.ResourceName :: state.DependsOn }
    [<CustomOperation "custom_script">]
    member _.CustomScript(state:VmConfig, script:string) = { state with CustomScript = Some script }
    [<CustomOperation "custom_script_files">]
    member _.CustomScriptFiles(state:VmConfig, uris:string list) = { state with CustomScriptFiles = uris |> List.map Uri }

let vm = VirtualMachineBuilder()
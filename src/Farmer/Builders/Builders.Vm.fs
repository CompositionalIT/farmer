[<AutoOpen>]
module Farmer.Builders.VirtualMachine

open Farmer
open Farmer.CoreTypes
open Farmer.PublicIpAddress
open Farmer.Vm
open Farmer.Helpers
open Farmer.Arm.Compute
open Farmer.Arm.Network
open Farmer.Arm.Storage
open System

let makeName (vmName:ResourceName) elementType = sprintf "%s-%s" vmName.Value elementType

type VmConfig =
    { Name : ResourceName
      DiagnosticsStorageAccount : ResourceRef<VmConfig> option

      Username : string option
      Image : ImageDefinition
      Size : VMSize
      OsDisk : DiskInfo
      DataDisks : DiskInfo list

      CustomScript : string option
      CustomScriptFiles : Uri list

      DomainNamePrefix : string option

      VNet : ResourceRef<VmConfig>
      AddressPrefix : string
      SubnetPrefix : string
      Subnet : AutoCreationKind<VmConfig>

      Tags: Map<string,string> }

    member internal this.deriveResourceName = makeName this.Name >> ResourceName
    member this.NicName = this.deriveResourceName "nic"
    member this.IpName = this.deriveResourceName "ip"
    member this.Hostname = sprintf "reference('%s').dnsSettings.fqdn" this.IpName.Value |> ArmExpression.create

    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            // VM itself
            { Name = this.Name
              Location = location
              StorageAccount =
                this.DiagnosticsStorageAccount
                |> Option.map(fun r -> r.CreateResourceId(this).Name)
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
              DataDisks = this.DataDisks
              Tags = this.Tags }

            let vnetName = this.VNet.CreateResourceId(this).Name
            let subnetName = this.Subnet.CreateResourceName this

            // NIC
            { Name = this.NicName
              Location = location
              IpConfigs = [
                {| SubnetName = subnetName
                   PublicIpName = this.IpName |} ]
              VirtualNetwork = vnetName
              Tags = this.Tags }

            // VNET
            match this.VNet with
            | DeployableResource this _ ->
                { Name = vnetName
                  Location = location
                  AddressSpacePrefixes = [ this.AddressPrefix ]
                  Subnets = [
                      {| Name = subnetName
                         Prefix = this.SubnetPrefix
                         Delegations = [] |}
                  ]
                  Tags = this.Tags
                }
            | _ ->
                ()

            // IP Address
            { Name = this.IpName
              Location = location
              AllocationMethod = AllocationMethod.Dynamic
              Sku = PublicIpAddress.Sku.Basic
              DomainNameLabel = this.DomainNamePrefix
              Tags = this.Tags }

            // Storage account - optional
            match this.DiagnosticsStorageAccount with
            | Some (DeployableResource this resourceName) ->
                { Name = Storage.StorageAccountName.Create(resourceName).OkValue
                  Location = location
                  Sku = Storage.Standard_LRS
                  StaticWebsite = None
                  EnableHierarchicalNamespace = None
                  Tags = this.Tags }
            | Some _
            | None ->
                ()

            // Custom Script - optional
            match this.CustomScript, this.CustomScriptFiles with
            | Some script, files ->
                { Name = this.Name.Map(sprintf "%s-custom-script")
                  Location = location
                  VirtualMachine = this.Name
                  OS = this.Image.OS
                  ScriptContents = script
                  FileUris = files
                  Tags = this.Tags }
            | None, [] ->
                ()
            | None, _ ->
                failwithf "You have supplied custom script files %A but no script. Custom script files are not automatically executed; you must provide an inline script which acts as a bootstrapper using the custom_script keyword." this.CustomScriptFiles
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
          OsDisk = { Size = 128; DiskType = Standard_LRS }
          AddressPrefix = "10.0.0.0/16"
          SubnetPrefix = "10.0.0.0/24"
          VNet = derived (fun config -> config.deriveResourceName "vnet")
          Subnet = Derived(fun config -> config.deriveResourceName "subnet")
          Tags = Map.empty }

    member __.Run (state:VmConfig) =
        { state with
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
    member __.StorageAccountName(state:VmConfig) =
        let storageResourceRef = derived (fun config ->
            config.Name.Map (sprintf "%sstorage")
            |> sanitiseStorage
            |> ResourceName)

        { state with DiagnosticsStorageAccount = Some storageResourceRef }
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
    member __.SubnetName(state:VmConfig, name) = { state with Subnet = Named name }
    member this.SubnetName(state:VmConfig, name) = this.SubnetName(state, ResourceName name)
    /// Uses an external VNet instead of creating a new one.
    [<CustomOperation "link_to_vnet">]
    member __.LinkToVNet(state:VmConfig, name) = { state with VNet = External (Managed name) }
    member this.LinkToVNet(state:VmConfig, name) = this.LinkToVNet(state, ResourceName name)
    member this.LinkToVNet(state:VmConfig, vnet:Arm.Network.VirtualNetwork) = this.LinkToVNet(state, vnet.Name)
    member this.LinkToVNet(state:VmConfig, vnet:VirtualNetworkConfig) = this.LinkToVNet(state, vnet.Name)

    [<CustomOperation "custom_script">]
    member _.CustomScript(state:VmConfig, script:string) = { state with CustomScript = Some script }
    [<CustomOperation "custom_script_files">]
    member _.CustomScriptFiles(state:VmConfig, uris:string list) = { state with CustomScriptFiles = uris |> List.map Uri }
    [<CustomOperation "add_tags">]
    member _.Tags(state:VmConfig, pairs) =
        { state with
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
    member this.Tag(state:VmConfig, key, value) = this.Tags(state, [ (key,value) ])

let vm = VirtualMachineBuilder()
[<AutoOpen>]
module Farmer.Resources.VirtualMachine

open Farmer.Helpers
open Farmer.Models
open Farmer

module Size =
    let Basic_A0 = "Basic_A0"
    let Basic_A1 = "Basic_A1"
    let Basic_A2 = "Basic_A2"
    let Basic_A3 = "Basic_A3"
    let Basic_A4 = "Basic_A4"
    let Standard_A0 = "Standard_A0"
    let Standard_A1 = "Standard_A1"
    let Standard_A2 = "Standard_A2"
    let Standard_A3 = "Standard_A3"
    let Standard_A4 = "Standard_A4"
    let Standard_A5 = "Standard_A5"
    let Standard_A6 = "Standard_A6"
    let Standard_A7 = "Standard_A7"
    let Standard_A8 = "Standard_A8"
    let Standard_A9 = "Standard_A9"
    let Standard_A10 = "Standard_A10"
    let Standard_A11 = "Standard_A11"
    let Standard_A1_v2 = "Standard_A1_v2"
    let Standard_A2_v2 = "Standard_A2_v2"
    let Standard_A4_v2 = "Standard_A4_v2"
    let Standard_A8_v2 = "Standard_A8_v2"
    let Standard_A2m_v2 = "Standard_A2m_v2"
    let Standard_A4m_v2 = "Standard_A4m_v2"
    let Standard_A8m_v2 = "Standard_A8m_v2"
    let Standard_B1s = "Standard_B1s"
    let Standard_B1ms = "Standard_B1ms"
    let Standard_B2s = "Standard_B2s"
    let Standard_B2ms = "Standard_B2ms"
    let Standard_B4ms = "Standard_B4ms"
    let Standard_B8ms = "Standard_B8ms"
    let Standard_D1 = "Standard_D1"
    let Standard_D2 = "Standard_D2"
    let Standard_D3 = "Standard_D3"
    let Standard_D4 = "Standard_D4"
    let Standard_D11 = "Standard_D11"
    let Standard_D12 = "Standard_D12"
    let Standard_D13 = "Standard_D13"
    let Standard_D14 = "Standard_D14"
    let Standard_D1_v2 = "Standard_D1_v2"
    let Standard_D2_v2 = "Standard_D2_v2"
    let Standard_D3_v2 = "Standard_D3_v2"
    let Standard_D4_v2 = "Standard_D4_v2"
    let Standard_D5_v2 = "Standard_D5_v2"
    let Standard_D2_v3 = "Standard_D2_v3"
    let Standard_D4_v3 = "Standard_D4_v3"
    let Standard_D8_v3 = "Standard_D8_v3"
    let Standard_D16_v3 = "Standard_D16_v3"
    let Standard_D32_v3 = "Standard_D32_v3"
    let Standard_D64_v3 = "Standard_D64_v3"
    let Standard_D2s_v3 = "Standard_D2s_v3"
    let Standard_D4s_v3 = "Standard_D4s_v3"
    let Standard_D8s_v3 = "Standard_D8s_v3"
    let Standard_D16s_v3 = "Standard_D16s_v3"
    let Standard_D32s_v3 = "Standard_D32s_v3"
    let Standard_D64s_v3 = "Standard_D64s_v3"
    let Standard_D11_v2 = "Standard_D11_v2"
    let Standard_D12_v2 = "Standard_D12_v2"
    let Standard_D13_v2 = "Standard_D13_v2"
    let Standard_D14_v2 = "Standard_D14_v2"
    let Standard_D15_v2 = "Standard_D15_v2"
    let Standard_DS1 = "Standard_DS1"
    let Standard_DS2 = "Standard_DS2"
    let Standard_DS3 = "Standard_DS3"
    let Standard_DS4 = "Standard_DS4"
    let Standard_DS11 = "Standard_DS11"
    let Standard_DS12 = "Standard_DS12"
    let Standard_DS13 = "Standard_DS13"
    let Standard_DS14 = "Standard_DS14"
    let Standard_DS1_v2 = "Standard_DS1_v2"
    let Standard_DS2_v2 = "Standard_DS2_v2"
    let Standard_DS3_v2 = "Standard_DS3_v2"
    let Standard_DS4_v2 = "Standard_DS4_v2"
    let Standard_DS5_v2 = "Standard_DS5_v2"
    let Standard_DS11_v2 = "Standard_DS11_v2"
    let Standard_DS12_v2 = "Standard_DS12_v2"
    let Standard_DS13_v2 = "Standard_DS13_v2"
    let Standard_DS14_v2 = "Standard_DS14_v2"
    let Standard_DS15_v2 = "Standard_DS15_v2"
    let Standard_DS13_4_v2 = "Standard_DS13-4_v2"
    let Standard_DS13_2_v2 = "Standard_DS13-2_v2"
    let Standard_DS14_8_v2 = "Standard_DS14-8_v2"
    let Standard_DS14_4_v2 = "Standard_DS14-4_v2"
    let Standard_E2_v3_v3 = "Standard_E2_v3"
    let Standard_E4_v3 = "Standard_E4_v3"
    let Standard_E8_v3 = "Standard_E8_v3"
    let Standard_E16_v3 = "Standard_E16_v3"
    let Standard_E32_v3 = "Standard_E32_v3"
    let Standard_E64_v3 = "Standard_E64_v3"
    let Standard_E2s_v3 = "Standard_E2s_v3"
    let Standard_E4s_v3 = "Standard_E4s_v3"
    let Standard_E8s_v3 = "Standard_E8s_v3"
    let Standard_E16s_v3 = "Standard_E16s_v3"
    let Standard_E32s_v3 = "Standard_E32s_v3"
    let Standard_E64s_v3 = "Standard_E64s_v3"
    let Standard_E32_16_v3 = "Standard_E32-16_v3"
    let Standard_E32_8s_v3 = "Standard_E32-8s_v3"
    let Standard_E64_32s_v3 = "Standard_E64-32s_v3"
    let Standard_E64_16s_v3 = "Standard_E64-16s_v3"
    let Standard_F1 = "Standard_F1"
    let Standard_F2 = "Standard_F2"
    let Standard_F4 = "Standard_F4"
    let Standard_F8 = "Standard_F8"
    let Standard_F16 = "Standard_F16"
    let Standard_F1s = "Standard_F1s"
    let Standard_F2s = "Standard_F2s"
    let Standard_F4s = "Standard_F4s"
    let Standard_F8s = "Standard_F8s"
    let Standard_F16s = "Standard_F16s"
    let Standard_F2s_v2 = "Standard_F2s_v2"
    let Standard_F4s_v2 = "Standard_F4s_v2"
    let Standard_F8s_v2 = "Standard_F8s_v2"
    let Standard_F16s_v2 = "Standard_F16s_v2"
    let Standard_F32s_v2 = "Standard_F32s_v2"
    let Standard_F64s_v2 = "Standard_F64s_v2"
    let Standard_F72s_v2 = "Standard_F72s_v2"
    let Standard_G1 = "Standard_G1"
    let Standard_G2 = "Standard_G2"
    let Standard_G3 = "Standard_G3"
    let Standard_G4 = "Standard_G4"
    let Standard_G5 = "Standard_G5"
    let Standard_GS1 = "Standard_GS1"
    let Standard_GS2 = "Standard_GS2"
    let Standard_GS3 = "Standard_GS3"
    let Standard_GS4 = "Standard_GS4"
    let Standard_GS5 = "Standard_GS5"
    let Standard_GS4_8 = "Standard_GS4-8"
    let Standard_GS4_4 = "Standard_GS4-4"
    let Standard_GS5_16 = "Standard_GS5-16"
    let Standard_GS5_8 = "Standard_GS5-8"
    let Standard_H8 = "Standard_H8"
    let Standard_H16 = "Standard_H16"
    let Standard_H8m = "Standard_H8m"
    let Standard_H16m = "Standard_H16m"
    let Standard_H16r = "Standard_H16r"
    let Standard_H16mr = "Standard_H16mr"
    let Standard_L4s = "Standard_L4s"
    let Standard_L8s = "Standard_L8s"
    let Standard_L16s = "Standard_L16s"
    let Standard_L32s = "Standard_L32s"
    let Standard_M64s = "Standard_M64s"
    let Standard_M64ms = "Standard_M64ms"
    let Standard_M128s = "Standard_M128s"
    let Standard_M128ms = "Standard_M128ms"
    let Standard_M64_32ms = "Standard_M64-32ms"
    let Standard_M64_16ms = "Standard_M64-16ms"
    let Standard_M128_64ms = "Standard_M128-64ms"
    let Standard_M128_32ms = "Standard_M128-32ms"
    let Standard_NC6 = "Standard_NC6"
    let Standard_NC12 = "Standard_NC12"
    let Standard_NC24 = "Standard_NC24"
    let Standard_NC24r = "Standard_NC24r"
    let Standard_NC6s_v2 = "Standard_NC6s_v2"
    let Standard_NC12s_v2 = "Standard_NC12s_v2"
    let Standard_NC24s_v2 = "Standard_NC24s_v2"
    let Standard_NC24rs_v2 = "Standard_NC24rs_v2"
    let Standard_NC6s_v3 = "Standard_NC6s_v3"
    let Standard_NC12s_v3 = "Standard_NC12s_v3"
    let Standard_NC24s_v3 = "Standard_NC24s_v3"
    let Standard_NC24rs_v3 = "Standard_NC24rs_v3"
    let Standard_ND6s = "Standard_ND6s"
    let Standard_ND12s = "Standard_ND12s"
    let Standard_ND24s = "Standard_ND24s"
    let Standard_ND24rs = "Standard_ND24rs"
    let Standard_NV6 = "Standard_NV6"
    let Standard_NV12 = "Standard_NV12"
    let Standard_NV24 = "Standard_NV24"
module CommonImages =
    let CentOS_75 = {| Offer = "CentOS"; Publisher = "OpenLogic"; Sku = "7.5" |}
    let CoreOS_Stable = {| Offer = "CoreOS"; Publisher = "CoreOS"; Sku = "Stable" |}
    let debian_10 = {| Offer = "debian-10"; Publisher = "Debian"; Sku = "10" |}
    let openSUSE_423 = {| Offer = "openSUSE-Leap"; Publisher = "SUSE"; Sku = "42.3" |}
    let RHEL_7RAW = {| Offer = "RHEL"; Publisher = "RedHat"; Sku = "7-RAW" |}
    let SLES_15 = {| Offer = "SLES"; Publisher = "SUSE"; Sku = "15" |}
    let UbuntuServer_1804LTS = {| Offer = "UbuntuServer"; Publisher = "Canonical"; Sku = "18.04-LTS" |}
    let WindowsServer_2019Datacenter = {| Offer = "WindowsServer"; Publisher = "MicrosoftWindowsServer"; Sku = "2019-Datacenter" |}
    let WindowsServer_2016Datacenter = {| Offer = "WindowsServer"; Publisher = "MicrosoftWindowsServer"; Sku = "2016-Datacenter" |}
    let WindowsServer_2012R2Datacenter = {| Offer = "WindowsServer"; Publisher = "MicrosoftWindowsServer"; Sku = "2012-R2-Datacenter" |}
    let WindowsServer_2012Datacenter = {| Offer = "WindowsServer"; Publisher = "MicrosoftWindowsServer"; Sku = "2012-Datacenter" |}
    let WindowsServer_2008R2SP1 = {| Offer = "WindowsServer"; Publisher = "MicrosoftWindowsServer"; Sku = "2008-R2-SP1" |}
let makeName (vmName:ResourceName) elementType = sprintf "%s-%s" vmName.Value elementType
let makeResourceName vmName = makeName vmName >> ResourceName
type VmConfig =
    { Name : ResourceName
      DiagnosticsStorageAccount : ResourceRef option
      
      Username : string
      Image : {| Publisher : string; Offer : string; Sku : string |}
      Size : string
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
          Size = Size.Basic_A0
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
        { state with Image = {| Offer = offer; Publisher = publisher; Sku = sku |} }
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

let vm = VirtualMachineBuilder()

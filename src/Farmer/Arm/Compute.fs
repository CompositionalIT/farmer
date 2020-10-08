[<AutoOpen>]
module Farmer.Arm.Compute

open Farmer
open Farmer.CoreTypes
open Farmer.Vm
open System
open System.Text

let virtualMachines = ResourceType ("Microsoft.Compute/virtualMachines", "2018-10-01")
let extensions = ResourceType ("Microsoft.Compute/virtualMachines/extensions", "2019-12-01")

type CustomScriptExtension =
    { Name : ResourceName
      Location : Location
      VirtualMachine : ResourceName
      FileUris : Uri list
      ScriptContents : string
      OS : OS
      Tags: Map<string,string>  }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| extensions.Create(this.VirtualMachine/this.Name, this.Location, [ ResourceId.create this.VirtualMachine ], this.Tags) with
                   properties =
                    match this.OS with
                    | Windows ->
                        {| publisher = "Microsoft.Compute"
                           ``type`` = "CustomScriptExtension"
                           typeHandlerVersion = "1.10"
                           autoUpgradeMinorVersion = true
                           settings = {| fileUris = this.FileUris |> List.map string |}
                           protectedSettings = {| commandToExecute = this.ScriptContents |}
                        |} |> box
                    | Linux ->
                        {| publisher = "Microsoft.Azure.Extensions"
                           ``type`` = "CustomScript"
                           typeHandlerVersion = "2.1"
                           autoUpgradeMinorVersion = true
                           protectedSettings =
                            {| fileUris =
                                this.FileUris
                                |> List.map string
                               script =
                                this.ScriptContents
                                |> Encoding.UTF8.GetBytes
                                |> Convert.ToBase64String |}
                        |} :> _
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
      NetworkInterfaceName : ResourceName
      Tags: Map<string,string>  }
    interface IParameters with
        member this.SecureParameters = [ this.Credentials.Password ]
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            let dependsOn = [
                   ResourceId.create this.NetworkInterfaceName
                   match this.StorageAccount with
                   | Some s -> ResourceId.create s
                   | None -> ()
            ]
            {| virtualMachines.Create(this.Name, this.Location, dependsOn, this.Tags) with
                properties =
                 {| hardwareProfile = {| vmSize = this.Size.ArmValue |}
                    osProfile =
                     {| computerName = this.Name.Value
                        adminUsername = this.Credentials.Username
                        adminPassword = this.Credentials.Password.ArmExpression.Eval() |}
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
                                   managedDisk = {| storageAccountType = dataDisk.DiskType.ArmValue |} |})
                        |}
                    networkProfile =
                        {| networkInterfaces = [
                            {| id = ResourceId.create(networkInterfaces, this.NetworkInterfaceName).Eval() |}
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
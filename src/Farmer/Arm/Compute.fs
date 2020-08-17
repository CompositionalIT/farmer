[<AutoOpen>]
module Farmer.Arm.Compute

open Farmer
open Farmer.CoreTypes
open Farmer.Vm
open System
open System.Text

let virtualMachines = ResourceType "Microsoft.Compute/virtualMachines"
let extensions = ResourceType "Microsoft.Compute/virtualMachines/extensions"

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
            {| ``type`` = extensions.ArmValue
               apiVersion = "2019-12-01"
               name = this.VirtualMachine.Value + "/" + this.Name.Value
               location = this.Location.ArmValue
               dependsOn = [
                   this.VirtualMachine.Value
               ]
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
               tags = this.Tags
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
            {| ``type`` = virtualMachines.ArmValue
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
                                  managedDisk = {| storageAccountType = dataDisk.DiskType.ArmValue |} |})
                       |}
                   networkProfile =
                       {| networkInterfaces = [
                           {| id = ArmExpression.resourceId(Network.networkInterfaces, this.NetworkInterfaceName).Eval() |}
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
               tags = this.Tags
            |} :> _
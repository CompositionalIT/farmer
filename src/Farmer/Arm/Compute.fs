[<AutoOpen>]
module Farmer.Arm.Compute

open Farmer
open Farmer.CoreTypes
open Farmer.Vm

let virtualMachines = ResourceType "Microsoft.Compute/virtualMachines"

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
            |} :> _
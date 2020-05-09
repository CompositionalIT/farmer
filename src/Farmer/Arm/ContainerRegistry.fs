[<AutoOpen>]
module Farmer.Arm.ContainerRegistry

open Farmer

type Registries =
    { Name : ResourceName
      Sku : ContainerRegistrySku
      AdminUserEnabled : bool }
    member this.LoginServer =
        (sprintf "reference(resourceId('Microsoft.ContainerRegistry/registries', '%s'),'2019-05-01').loginServer" this.Name.Value)
        |> ArmExpression
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.ToArmObject location =
            {| name = this.Name.Value
               ``type`` = "Microsoft.ContainerRegistry/registries"
               apiVersion = "2019-05-01"
               sku = {| name = this.Sku.ToString().Replace("_", ".") |}
               location = location.ArmValue
               tags = {||}
               properties = {| adminUserEnabled = this.AdminUserEnabled |}
            |} :> _
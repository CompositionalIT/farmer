[<AutoOpen>]
module Farmer.Arm.ContainerRegistry

open Farmer
open Farmer.ContainerRegistry
open Farmer.CoreTypes

let registries = ResourceType "Microsoft.ContainerRegistry/registries"

type Registries =
    { Name : ResourceName<RegistryName>
      Location : Location
      Sku : Sku
      AdminUserEnabled : bool
      Tags: Map<string,string> }
    interface IArmResource with
        member this.ResourceName = this.Name.Untyped
        member this.JsonModel =
            {| name = this.Name.Value
               ``type`` = registries.ArmValue
               apiVersion = "2019-05-01"
               sku = {| name = this.Sku.ToString() |}
               location = this.Location.ArmValue
               tags = this.Tags
               properties = {| adminUserEnabled = this.AdminUserEnabled |}
            |} :> _
[<AutoOpen>]
module Farmer.Arm.ContainerRegistry

open Farmer
open Farmer.ContainerRegistry
open Farmer.CoreTypes

let registries = ResourceType ("Microsoft.ContainerRegistry/registries", "2019-05-01")

type Registries =
    { Name : ResourceName
      Location : Location
      Sku : Sku
      AdminUserEnabled : bool
      Tags: Map<string,string> }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| registries.Create(this.Name, this.Location, tags = this.Tags) with
                 sku = {| name = this.Sku.ToString() |}
                 properties = {| adminUserEnabled = this.AdminUserEnabled |}
            |} :> _
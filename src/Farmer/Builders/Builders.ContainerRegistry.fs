[<AutoOpen>]
module Farmer.Builders.ContainerRegistry

open Farmer
open Farmer.CoreTypes
open Farmer.ContainerRegistry
open Farmer.Arm.ContainerRegistry

type ContainerRegistryConfig =
    { Name : ResourceName
      Sku : Sku
      AdminUserEnabled : bool
      Tags: Map<string,string>  }
    member this.LoginServer =
        (sprintf "reference(resourceId('Microsoft.ContainerRegistry/registries', '%s'),'2019-05-01').loginServer" this.Name.Value)
        |> ArmExpression.create
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              Sku = this.Sku
              AdminUserEnabled = this.AdminUserEnabled
              Tags = this.Tags }
        ]
type ContainerRegistryBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Sku = Basic
          AdminUserEnabled = false
          Tags = Map.empty }

    [<CustomOperation "name">]
    /// Sets the name of the Azure Container Registry instance.
    member _.Name (state:ContainerRegistryConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "sku">]
    /// Sets the name of the SKU/Tier for the Container Registry instance.
    member _.Sku (state:ContainerRegistryConfig, sku) = { state with Sku = sku }

    [<CustomOperation "enable_admin_user">]
    /// Enables the admin user on the Azure Container Registry.
    member _.EnableAdminUser (state:ContainerRegistryConfig) = { state with AdminUserEnabled = true }
    [<CustomOperation "add_tags">]
    member _.Tags(state:ContainerRegistryConfig, pairs) = 
        { state with 
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
    member this.Tag(state:ContainerRegistryConfig, key, value) = this.Tags(state, [ (key,value) ])

let containerRegistry = ContainerRegistryBuilder()
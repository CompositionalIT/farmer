[<AutoOpen>]
module Farmer.Resources.ContainerRegistry

open Farmer

type ContainerRegistry =
    { Name : ResourceName
      Location : Location
      Sku : string
      AdminUserEnabled : bool }
    interface IResource with
        member this.ResourceName = this.Name
        member this.ToArmObject() =
            {| name = this.Name.Value
               ``type`` = "Microsoft.ContainerRegistry/registries"
               apiVersion = "2019-05-01"
               sku = {| name = this.Sku |}
               location = this.Location.ArmValue
               tags = {||}
               properties = {| adminUserEnabled = this.AdminUserEnabled |}
            |} :> _

/// Container Registry SKU
type ContainerRegistrySku =
    | Basic
    | Standard
    | Premium

// TODO: networkRuleSet
// TODO: policies
// TODO: encryption
// TODO: dataEndpointEnabled

type ContainerRegistryConfig =
    { Name : ResourceName
      Sku : ContainerRegistrySku
      AdminUserEnabled : bool }
    member this.LoginServer =
        (sprintf "reference(resourceId('Microsoft.ContainerRegistry/registries', '%s'),'2019-05-01').loginServer" this.Name.Value)
        |> ArmExpression
    interface IResourceBuilder with
        member this.BuildResources location _ = [
            NewResource { Name = this.Name
                          Location = location
                          Sku = this.Sku.ToString().Replace("_", ".")
                          AdminUserEnabled = this.AdminUserEnabled }
        ]
type ContainerRegistryBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Sku = Basic
          AdminUserEnabled = false }

    [<CustomOperation "name">]
    /// Sets the name of the Azure Container Registry instance.
    member _.Name (state:ContainerRegistryConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "sku">]
    /// Sets the name of the SKU/Tier for the Container Registry instance.
    member _.Sku (state:ContainerRegistryConfig, sku) = { state with Sku = sku }

    [<CustomOperation "enable_admin_user">]
    /// Enables the admin user on the Azure Container Registry.
    member _.EnableAdminUser (state:ContainerRegistryConfig) = { state with AdminUserEnabled = true }

let containerRegistry = ContainerRegistryBuilder()
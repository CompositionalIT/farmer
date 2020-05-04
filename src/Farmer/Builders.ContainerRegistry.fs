[<AutoOpen>]
module Farmer.Resources.ContainerRegistry

open Farmer
open Farmer.Models

[<RequireQualifiedAccess>]
/// Container Registry SKU
type ContainerRegistrySku =
    | Basic
    | Standard
    | Premium

type ContainerRegistryConfig =
    { Name : ResourceName
      Sku : ContainerRegistrySku
      AdminUserEnabled : bool }
    member this.LoginServer =
        (sprintf "reference(resourceId('Microsoft.ContainerRegistry/registries', '%s'),'2019-05-01').loginServer" this.Name.Value)
        |> ArmExpression

type ContainerRegistryBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Sku = ContainerRegistrySku.Basic
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

module Converters =
    let containerRegistry location (config:ContainerRegistryConfig) : ContainerRegistry =
        { Name = config.Name
          Location = location
          Sku = config.Sku.ToString().Replace("_", ".")
          AdminUserEnabled = config.AdminUserEnabled }

    module Outputters =
        let containerRegistry (service:Farmer.Models.ContainerRegistry) =
            {| name = service.Name.Value
               ``type`` = "Microsoft.ContainerRegistry/registries"
               apiVersion = "2019-05-01"
               sku = {| name = service.Sku |}
               location = service.Location.ArmValue
               tags = {||}
               properties = {| adminUserEnabled = service.AdminUserEnabled |}
            |}

let containerRegistry = ContainerRegistryBuilder()

type Farmer.ArmBuilder.ArmBuilder with
    member this.AddResource(state:ArmConfig, config) =
        { state with
            Resources = ContainerRegistry (Converters.containerRegistry state.Location config) :: state.Resources
        }
    member this.AddResources (state, configs) = addResources<ContainerRegistryConfig> this.AddResource state configs

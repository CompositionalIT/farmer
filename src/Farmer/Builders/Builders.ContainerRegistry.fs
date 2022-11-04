[<AutoOpen>]
module Farmer.Builders.ContainerRegistry

open Farmer
open Farmer.ContainerRegistry
open Farmer.Arm.ContainerRegistry

type ContainerRegistryConfig =
    {
        Name: ResourceName
        Sku: Sku
        AdminUserEnabled: bool
        Tags: Map<string, string>
    }

    member this.LoginServer =
        $"reference(resourceId('Microsoft.ContainerRegistry/registries', '{this.Name.Value}'),'2019-05-01').loginServer"
        |> ArmExpression.create

    /// Returns first Admin password if AdminUserEnabled
    member this.Password =
        $"listCredentials(resourceId('Microsoft.ContainerRegistry/registries','{this.Name.Value}'),'2019-05-01').passwords[0].value"
        |> ArmExpression.create

    /// Returns second Admin password if AdminUserEnabled
    member this.Password2 =
        $"listCredentials(resourceId('Microsoft.ContainerRegistry/registries','{this.Name.Value}'),'2019-05-01').passwords[1].value"
        |> ArmExpression.create

    /// Returns Admin username if AdminUserEnabled
    member this.Username =
        $"listCredentials(resourceId('Microsoft.ContainerRegistry/registries','{this.Name.Value}'),'2019-05-01').username"
        |> ArmExpression.create

    interface IBuilder with
        member this.ResourceId = registries.resourceId this.Name

        member this.BuildResources location =
            [
                {
                    Name = this.Name
                    Location = location
                    Sku = this.Sku
                    AdminUserEnabled = this.AdminUserEnabled
                    Tags = this.Tags
                }
            ]

type ContainerRegistryBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            Sku = Basic
            AdminUserEnabled = false
            Tags = Map.empty
        }

    [<CustomOperation "name">]
    /// Sets the name of the Azure Container Registry instance.
    member _.Name(state: ContainerRegistryConfig, name: ResourceName) =
        { state with
            Name =
                ContainerRegistryValidation
                    .ContainerRegistryName
                    .Create(
                        name
                    )
                    .OkValue
                    .ResourceName
        }

    member this.Name(state: ContainerRegistryConfig, name: string) = this.Name(state, ResourceName name)


    [<CustomOperation "sku">]
    /// Sets the name of the SKU/Tier for the Container Registry instance.
    member _.Sku(state: ContainerRegistryConfig, sku) = { state with Sku = sku }

    [<CustomOperation "enable_admin_user">]
    /// Enables the admin user on the Azure Container Registry.
    member _.EnableAdminUser(state: ContainerRegistryConfig) = { state with AdminUserEnabled = true }

    interface ITaggable<ContainerRegistryConfig> with
        member _.Add state tags =
            { state with
                Tags = state.Tags |> Map.merge tags
            }

let containerRegistry = ContainerRegistryBuilder()

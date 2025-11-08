[<AutoOpen>]
module Farmer.Builders.ContainerRegistry

open Farmer
open Farmer.ContainerRegistry
open Farmer.Arm.ContainerRegistry

type ContainerRegistryConfig = {
    Name: ResourceName
    Sku: Sku
    AdminUserEnabled: bool
    PublicNetworkAccess: ContainerRegistry.PublicNetworkAccess option
    NetworkRuleSet: ContainerRegistry.NetworkRuleSet option
    Tags: Map<string, string>
} with

    member this.LoginServer =
        $"reference(resourceId('Microsoft.ContainerRegistry/registries', '{this.Name.Value}'),'2023-07-01').loginServer"
        |> ArmExpression.create

    /// Returns first Admin password if AdminUserEnabled
    member this.Password =
        $"listCredentials(resourceId('Microsoft.ContainerRegistry/registries','{this.Name.Value}'),'2023-07-01').passwords[0].value"
        |> ArmExpression.create

    /// Returns second Admin password if AdminUserEnabled
    member this.Password2 =
        $"listCredentials(resourceId('Microsoft.ContainerRegistry/registries','{this.Name.Value}'),'2023-07-01').passwords[1].value"
        |> ArmExpression.create

    /// Returns Admin username if AdminUserEnabled
    member this.Username =
        $"listCredentials(resourceId('Microsoft.ContainerRegistry/registries','{this.Name.Value}'),'2023-07-01').username"
        |> ArmExpression.create

    interface IBuilder with
        member this.ResourceId = registries.resourceId this.Name

        member this.BuildResources location = [
            {
                Name = this.Name
                Location = location
                Sku = this.Sku
                AdminUserEnabled = this.AdminUserEnabled
                PublicNetworkAccess = this.PublicNetworkAccess
                NetworkRuleSet = this.NetworkRuleSet
                Tags = this.Tags
            }
        ]

type ContainerRegistryBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        Sku = Basic
        AdminUserEnabled = false
        PublicNetworkAccess = None
        NetworkRuleSet = None
        Tags = Map.empty
    }

    [<CustomOperation "name">]
    /// Sets the name of the Azure Container Registry instance.
    member _.Name(state: ContainerRegistryConfig, name: ResourceName) = {
        state with
            Name = ContainerRegistryValidation.ContainerRegistryName.Create(name).OkValue.ResourceName
    }

    member this.Name(state: ContainerRegistryConfig, name: string) = this.Name(state, ResourceName name)

    [<CustomOperation "sku">]
    /// Sets the name of the SKU/Tier for the Container Registry instance.
    member _.Sku(state: ContainerRegistryConfig, sku) = { state with Sku = sku }

    [<CustomOperation "enable_admin_user">]
    /// Enables the admin user on the Azure Container Registry.
    member _.EnableAdminUser(state: ContainerRegistryConfig) = { state with AdminUserEnabled = true }

    [<CustomOperation "enable_public_network_access">]
    /// Enables public network access to the registry.
    member _.EnablePublicNetworkAccess(state: ContainerRegistryConfig) = {
        state with
            PublicNetworkAccess = Some ContainerRegistry.PublicNetworkAccess.Enabled
    }

    [<CustomOperation "disable_public_network_access">]
    /// Disables public network access to the registry (Premium SKU only).
    member _.DisablePublicNetworkAccess(state: ContainerRegistryConfig) = {
        state with
            PublicNetworkAccess = Some ContainerRegistry.PublicNetworkAccess.Disabled
    }

    [<CustomOperation "add_ip_rule">]
    /// Adds an IP address or CIDR range to the allow list (Premium SKU only).
    member _.AddIpRule(state: ContainerRegistryConfig, ipAddressOrCidr: string) =
        let currentRules =
            state.NetworkRuleSet
            |> Option.defaultValue {
                DefaultAction = ContainerRegistry.NetworkRuleAction.Deny
                IpRules = []
            }

        {
            state with
                NetworkRuleSet =
                    Some {
                        currentRules with
                            IpRules = currentRules.IpRules @ [ { Value = ipAddressOrCidr } ]
                    }
        }

    [<CustomOperation "add_ip_rules">]
    /// Adds multiple IP addresses or CIDR ranges to the allow list (Premium SKU only).
    member _.AddIpRules(state: ContainerRegistryConfig, ipAddressesOrCidrs: string list) =
        let currentRules =
            state.NetworkRuleSet
            |> Option.defaultValue {
                DefaultAction = ContainerRegistry.NetworkRuleAction.Deny
                IpRules = []
            }

        {
            state with
                NetworkRuleSet =
                    Some {
                        currentRules with
                            IpRules =
                                currentRules.IpRules
                                @ (ipAddressesOrCidrs |> List.map (fun ip -> { Value = ip }))
                    }
        }

    interface ITaggable<ContainerRegistryConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

let containerRegistry = ContainerRegistryBuilder()
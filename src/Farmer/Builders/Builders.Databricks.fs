[<AutoOpen>]
module Farmer.Builders.Databricks

open Farmer
open Farmer.Databricks
open Farmer.Arm.Databricks
open Farmer.Arm.Network
open System

type WorkspaceConfig =
    { Name : ResourceName
      ManagedResourceGroupId : ResourceName
      Sku : Sku
      EnablePublicIp : FeatureFlag
      EncryptionMode : EncryptionMode option
      ByovConfig : ByovConfig option
      Tags : Map<string,string> }
    interface IBuilder with
        member this.ResourceId = workspaces.resourceId this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              ManagedResourceGroupId = this.ManagedResourceGroupId
              Sku = this.Sku
              EnablePublicIp = this.EnablePublicIp
              EncryptionMode = this.EncryptionMode
              ByovConfig = this.ByovConfig
              Tags = this.Tags  }
        ]

type WorkspaceBuilder() =
    member __.Yield _ =
      { Name = ResourceName.Empty
        ManagedResourceGroupId = ResourceName.Empty
        Sku = Standard
        EnablePublicIp = Enabled
        EncryptionMode = None
        ByovConfig = None
        Tags = Map.empty }

    member _.Run(state:WorkspaceConfig) =
        match state with
        | { ByovConfig = Some { PublicSubnet = EmptyResourceName } } -> failwithf "Public subnet must be set. Use public_subnet to set it"
        | { ByovConfig = Some { PrivateSubnet = EmptyResourceName } } -> failwithf "Private subnet must be set. Use private_subnet to set it"
        | _ -> ()

        state

    /// Sets the name of the workspace
    [<CustomOperation "name">]
    member _.Name (state:WorkspaceConfig, name) =
        { state with Name = ResourceName name }
    /// Sets the managed resource group
    [<CustomOperation "managed_resource_group_id">]
    member _.ManagedResourceGroupId (state:WorkspaceConfig, resourceGroupName) =
        { state with ManagedResourceGroupId = ResourceName resourceGroupName}
    /// Sets the workspace pricing tier
    [<CustomOperation "sku">]
    member _.PricingTier (state:WorkspaceConfig, sku) =
        { state with Sku = sku }
    /// Enabled Public IP
    [<CustomOperation "use_public_ip">]
    member _.UsePublicIp (state:WorkspaceConfig, flag) =
        { state with EnablePublicIp = flag }
    /// Use key vault for storing and retrieving databricks secrets using the specified key vault and key.
    [<CustomOperation "key_vault_secret_management">]
    member _.KeyVault (state:WorkspaceConfig, keyVault, keyName) =
        { state with
            EncryptionMode =
                Some (KeyVaultEncryption {| Vault = keyVault
                                            Key = keyName
                                            KeyVersion = None |}) }
    member this.KeyVault (state:WorkspaceConfig, config:KeyVaultConfig, keyName:string) = this.KeyVault (state, config.Name, keyName)
    member this.KeyVault (state:WorkspaceConfig, vaultName, keyName) = this.KeyVault (state, ResourceName vaultName, keyName)
    /// Specifies the version of the key vault key to use; if this is not specified, the latest version of the key is used.
    [<CustomOperation "key_vault_key_version">]
    member _.KeyVaultKeyVersion (state:WorkspaceConfig, keyVersion) =
        { state with
            EncryptionMode =
                match state.EncryptionMode with
                | Some (KeyVaultEncryption config) -> Some (KeyVaultEncryption {| config with KeyVersion = Some keyVersion |})
                | Some DataBricksEncryption -> failwith "You cannot set the key vault key version if you have specified DataBricks internal encryption."
                | None -> failwith "No key vault has been specified. First activate keyvault secret integration using key_vault_secret_management." }

    /// Sets the vnet
    [<CustomOperation "byov_vnet">]
    member _.ByovVnet (state:WorkspaceConfig, vnet:ResourceName) =
        let config =
            state.ByovConfig
            |> Option.map(fun vnetConfig -> { vnetConfig with Vnet = External(Unmanaged(virtualNetworks.resourceId vnet)) })
            |> Option.orElse
                (Some { Vnet = External(Unmanaged(virtualNetworks.resourceId vnet))
                        PublicSubnet = ResourceName.Empty
                        PrivateSubnet = ResourceName.Empty })
        { state with ByovConfig = config }
    member this.ByovVnet(state:WorkspaceConfig, name:string) = this.ByovVnet(state, ResourceName name)
    member this.ByovVnet(state:WorkspaceConfig, vnet:Arm.Network.VirtualNetwork) = this.ByovVnet(state, vnet.Name)
    member this.ByovVnet(state:WorkspaceConfig, vnet:VirtualNetworkConfig) = this.ByovVnet(state, vnet.Name)

    /// Set the existing public subnet
    [<CustomOperation "byov_public_subnet">]
    member _.ByovPublicSubnet (state:WorkspaceConfig, publicSubnet:ResourceName) =
        let config =
            state.ByovConfig
            |> Option.map(fun vnetConfig -> { vnetConfig with PublicSubnet = publicSubnet })
            |> Option.orElse
                (Some { Vnet = External(Unmanaged(virtualNetworks.resourceId ResourceName.Empty))
                        PublicSubnet = publicSubnet
                        PrivateSubnet = ResourceName.Empty })
        { state with ByovConfig = config}
    member this.ByovPublicSubnet(state:WorkspaceConfig, publicSubnet) = this.ByovPublicSubnet(state, ResourceName publicSubnet)

    /// Sets the existing private subnet
    [<CustomOperation "byov_private_subnet">]
    member _.ByovPrivateSubnet (state:WorkspaceConfig, privateSubnet:ResourceName) =
        let config =
            state.ByovConfig
            |> Option.map(fun vnetConfig -> { vnetConfig with PrivateSubnet = privateSubnet })
            |> Option.orElse
                (Some { Vnet = External(Unmanaged(virtualNetworks.resourceId ResourceName.Empty))
                        PublicSubnet = ResourceName.Empty
                        PrivateSubnet = privateSubnet })
        { state with ByovConfig = config }
    member this.ByovPrivateSubnet(state:WorkspaceConfig, privateSubnet) = this.ByovPrivateSubnet(state, ResourceName privateSubnet)

    /// Add the list of tags
    [<CustomOperation "add_tags">]
    member _.Tags(state:WorkspaceConfig, pairs) =
        { state with
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }

    /// Adds a single tag
    [<CustomOperation "add_tag">]
    member this.Tag(state:WorkspaceConfig, key, value) = this.Tags(state, [ (key,value) ])

let databricks = WorkspaceBuilder()
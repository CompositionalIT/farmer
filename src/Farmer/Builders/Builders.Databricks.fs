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
      SecretScope : SecretScope option
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
              SecretScope = this.SecretScope
              ByovConfig = this.ByovConfig
              Tags = this.Tags  }
        ]

type WorkspaceBuilder() =
    member __.Yield _ =
      { Name = ResourceName.Empty
        ManagedResourceGroupId = ResourceName.Empty
        Sku = Standard
        EnablePublicIp = Enabled
        SecretScope = None
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
    [<CustomOperation "allow_public_ip">]
    member _.AllowPublicIp (state:WorkspaceConfig, flag) =
        { state with EnablePublicIp = flag }
    /// Use Azure Key Vault for the secret scope on the workspace.
    [<CustomOperation "key_vault_secret_scope">]
    member _.KeyVault (state:WorkspaceConfig, keyVault, keyName) =
        { state with
            SecretScope =
                Some (KeyVaultSecretScope {| Vault = keyVault
                                             Key = keyName
                                             KeyVersion = None |}) }
    member this.KeyVault (state:WorkspaceConfig, config:KeyVaultConfig, keyName:string) = this.KeyVault (state, config.Name, keyName)
    member this.KeyVault (state:WorkspaceConfig, vaultName, keyName) = this.KeyVault (state, ResourceName vaultName, keyName)
    /// Specifies the version of the key vault key to use; if this is not specified, the latest version of the key is used.
    [<CustomOperation "key_vault_key_version">]
    member _.KeyVaultKeyVersion (state:WorkspaceConfig, keyVersion) =
        { state with
            SecretScope =
                match state.SecretScope with
                | Some (KeyVaultSecretScope config) -> Some (KeyVaultSecretScope {| config with KeyVersion = Some keyVersion |})
                | Some DataBricksSecretScope -> failwith "You cannot set the key vault key version if you have specified DataBricks internal encryption."
                | None -> failwith "No key vault has been specified. First activate keyvault secret integration using key_vault_secret_management." }
    /// Use Databricks itself for the secret scope on the workspace.
    [<CustomOperation "databricks_secret_scope">]
    member _.DatabricksSecretScope (state:WorkspaceConfig) = { state with SecretScope = Some DataBricksSecretScope }
    /// Specify the secret scope of the workspace programmatically.
    [<CustomOperation "secret_scope">]
    member _.SecretScope (state:WorkspaceConfig, scope) = { state with SecretScope = Some scope }
    [<CustomOperation "attach_to_vnet">]
    member _.Byovnet (state:WorkspaceConfig, vnet:ResourceId, publicSubnet, privateSubnet) =
        { state with
            ByovConfig =
                Some { Vnet = vnet
                       PublicSubnet = publicSubnet
                       PrivateSubnet = privateSubnet } }
    member this.Byovnet (state, vnet:ResourceName, publicSubnet:SubnetBuildSpec, privateSubnet:SubnetBuildSpec) = this.Byovnet(state, virtualNetworks.resourceId vnet, ResourceName publicSubnet.Name, ResourceName privateSubnet.Name)
    member this.Byovnet (state, vnet:VirtualNetworkConfig, publicSubnet, privateSubnet) = this.Byovnet(state, vnet.Name, publicSubnet, privateSubnet)
    member this.Byovnet (state, vnet:string, publicSubnet, privateSubnet) = this.Byovnet(state, virtualNetworks.resourceId vnet, ResourceName publicSubnet, ResourceName privateSubnet)

    /// Add the list of tags
    [<CustomOperation "add_tags">]
    member _.Tags(state:WorkspaceConfig, pairs) =
        { state with
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }

    /// Adds a single tag
    [<CustomOperation "add_tag">]
    member this.Tag(state:WorkspaceConfig, key, value) = this.Tags(state, [ (key,value) ])

let databricks = WorkspaceBuilder()
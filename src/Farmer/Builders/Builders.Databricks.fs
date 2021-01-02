[<AutoOpen>]
module Farmer.Builders.Databricks

open Farmer
open Farmer.Databricks
open Farmer.Arm.Databricks
open Farmer.Arm.Network
open Farmer.Arm.KeyVault
open System

type DatabricksConfig =
    { Name : ResourceName
      ManagedResourceGroupId : ResourceName option
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
              ManagedResourceGroupId = this.ManagedResourceGroupId |> Option.defaultWith (fun () -> this.Name-"rg")
              Sku = this.Sku
              EnablePublicIp = this.EnablePublicIp
              SecretScope = this.SecretScope
              ByovConfig = this.ByovConfig
              Tags = this.Tags
              Dependencies = Set [
                  match this.SecretScope with
                  | Some (KeyVaultSecretScope config) -> config.Vault
                  | Some DataBricksSecretScope | None -> ()

                  yield! this.ByovConfig |> Option.mapList(fun vnet -> vnet.Vnet)
              ] }
        ]

type WorkspaceBuilder() =
    member __.Yield _ =
      { Name = ResourceName.Empty
        ManagedResourceGroupId = None
        Sku = Standard
        EnablePublicIp = Enabled
        SecretScope = None
        ByovConfig = None
        Tags = Map.empty }

    member _.Run(state:DatabricksConfig) =
        match state with
        | { ByovConfig = Some { PublicSubnet = EmptyResourceName } } -> failwithf "Public subnet must be set. Use public_subnet to set it"
        | { ByovConfig = Some { PrivateSubnet = EmptyResourceName } } -> failwithf "Private subnet must be set. Use private_subnet to set it"
        | _ -> ()

        state

    /// Sets the name of the workspace
    [<CustomOperation "name">]
    member _.Name (state:DatabricksConfig, name) = { state with Name = ResourceName name }
    /// Sets the managed resource group name. If not set, defaults to the name of the databricks resource + "-rg".
    [<CustomOperation "managed_resource_group_id">]
    member _.ManagedResourceGroupId (state:DatabricksConfig, resourceGroupName) =
        { state with ManagedResourceGroupId = Some (ResourceName resourceGroupName) }
    /// Sets the workspace pricing tier. Defaults to Standard.
    [<CustomOperation "sku">]
    member _.PricingTier (state:DatabricksConfig, sku) = { state with Sku = sku }
    /// Enabled Public IP
    [<CustomOperation "allow_public_ip">]
    member _.AllowPublicIp (state:DatabricksConfig, flag) = { state with EnablePublicIp = flag }
    /// Use Azure Key Vault for the secret scope on the workspace.
    [<CustomOperation "key_vault_secret_scope">]
    member _.KeyVault (state:DatabricksConfig, keyVault:ResourceId, keyName:string) =
        { state with
            SecretScope =
                Some (KeyVaultSecretScope {| Vault = keyVault
                                             Key = keyName
                                             KeyVersion = None |}) }
    member this.KeyVault (state, config:KeyVaultConfig, keyName) =
        this.KeyVault (state, vaults.resourceId config.Name, keyName)
    member this.KeyVault (state, vaultName:string, keyName) =
        this.KeyVault (state, vaults.resourceId vaultName, keyName)
    /// Specifies the version of the key vault key to use; if this is not specified, the latest version of the key is used.
    [<CustomOperation "key_vault_key_version">]
    member _.KeyVaultKeyVersion (state:DatabricksConfig, keyVersion) =
        { state with
            SecretScope =
                match state.SecretScope with
                | Some (KeyVaultSecretScope config) -> Some (KeyVaultSecretScope {| config with KeyVersion = Some keyVersion |})
                | Some DataBricksSecretScope -> failwith "You cannot set the key vault key version if you have specified DataBricks internal encryption."
                | None -> failwith "No key vault has been specified. First activate keyvault secret integration using key_vault_secret_management." }
    /// Use Databricks itself for the secret scope on the workspace.
    [<CustomOperation "databricks_secret_scope">]
    member _.DatabricksSecretScope (state:DatabricksConfig) = { state with SecretScope = Some DataBricksSecretScope }
    /// Specify the secret scope of the workspace programmatically.
    [<CustomOperation "secret_scope">]
    member _.SecretScope (state:DatabricksConfig, scope) = { state with SecretScope = Some scope }
    [<CustomOperation "attach_to_vnet">]
    member _.AttachToVnet (state:DatabricksConfig, vnet:ResourceId, publicSubnet, privateSubnet) =
        { state with
            ByovConfig =
                Some { Vnet = vnet
                       PublicSubnet = publicSubnet
                       PrivateSubnet = privateSubnet } }
    member this.AttachToVnet (state, vnet:ResourceName, publicSubnet:SubnetBuildSpec, privateSubnet:SubnetBuildSpec) = this.AttachToVnet(state, virtualNetworks.resourceId vnet, ResourceName publicSubnet.Name, ResourceName privateSubnet.Name)
    member this.AttachToVnet (state, vnet:VirtualNetworkConfig, publicSubnet, privateSubnet) = this.AttachToVnet(state, vnet.Name, publicSubnet, privateSubnet)
    member this.AttachToVnet (state, vnet:string, publicSubnet, privateSubnet) = this.AttachToVnet(state, virtualNetworks.resourceId vnet, ResourceName publicSubnet, ResourceName privateSubnet)

    interface ITaggable<DatabricksConfig> with member _.Add state tags = { state with Tags = state.Tags |> Map.merge tags }
let databricks = WorkspaceBuilder()
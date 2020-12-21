[<AutoOpen>]
module Farmer.Builders.Databricks

open Farmer
open Farmer.Databricks
open Farmer.Arm.Databricks
open Farmer.Arm.Network
open System

type WorkspaceConfig =
    { Name: ResourceName
      ManagedResourceGroupId: ResourceName
      Sku: Sku
      EnablePublicIp: FeatureFlag
      PrepareEncryption: FeatureFlag
      Encryption: EncryptionConfig option
      ByovConfig: ByovConfig option
      Tags: Map<string,string> }
    interface IBuilder with
        member this.ResourceId = workspaces.resourceId this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              ManagedResourceGroupId = this.ManagedResourceGroupId
              Sku = this.Sku
              EnablePublicIp = this.EnablePublicIp
              PrepareEncryption = this.PrepareEncryption
              Encryption = this.Encryption
              ByovConfig = this.ByovConfig
              Tags = this.Tags  }
        ]

type WorkspaceBuilder() =
    member __.Yield _ =
      { Name = ResourceName.Empty
        ManagedResourceGroupId = ResourceName.Empty
        Sku = StandardTier
        EnablePublicIp = Enabled
        PrepareEncryption = Disabled
        Encryption = None
        ByovConfig = None
        Tags = Map.empty }
    member _.Run(state:WorkspaceConfig) =
        match state.Encryption with
        | Some config ->
            if System.String.IsNullOrEmpty(config.KeyName) then
                failwithf "Encryption key name must not be empty when using encryption. Set with encryption_key operation"
            if config.KeyVault = ResourceName.Empty then
                failwithf "Key vault must be set when using encryption. Set with key_vault operation"
            match config.KeyVersion with
                | "latest" ->
                    ()
                | maybeGuid ->
                    let isValidGuid, _ = Guid.TryParse(maybeGuid)
                    if not isValidGuid then
                        failwithf "Key version must either be latest or a valid guid"
        | None ->
            ()

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
    member _.DisablePublicIp (state:WorkspaceConfig, flag) =
        { state with EnablePublicIp = flag}
    /// Sets the key vault for the encryption key
    [<CustomOperation "key_vault">]
    member _.KeyVault (state:WorkspaceConfig, keyVault, ?keySource:KeyVault.KeySource) =
        let keySource = defaultArg keySource KeyVault.KeySource.Default
        let encryption =
            state.Encryption
            |> Option.map(fun encryptionConfig -> { encryptionConfig with KeyVault = keyVault })
            |> Option.orElse
                (Some { KeyVault = keyVault
                        KeyName = ""
                        KeyVersion = ""
                        KeySource = keySource })
        { state with Encryption = encryption }
    member this.KeyVault(state:WorkspaceConfig, keyVault, keySource:KeyVault.KeySource) = 
        this.KeyVault(state, ResourceName keyVault, keySource)
    member this.KeyVault(state:WorkspaceConfig, keyVault:Arm.KeyVault.Vault, keySource:KeyVault.KeySource) = 
        this.KeyVault(state, keyVault.Name, keySource)
    member this.KeyVault(state:WorkspaceConfig, keyVaultConfig:KeyVaultConfig, keySource:KeyVault.KeySource) = 
        this.KeyVault(state, keyVaultConfig.Name, keySource)
    /// Sets the encryption key configuration
    [<CustomOperation "encryption_key">]
    member _.EncryptionKey (state:WorkspaceConfig, keyName, ?keyVersion) =
        let keyVersion = defaultArg keyVersion "latest"
        let encryption =
            state.Encryption
            |> Option.map(fun encryptionConfig ->
                { encryptionConfig with
                    KeyName = keyName
                    KeyVersion = keyVersion})
            |> Option.orElse
                (Some { KeyVault = ResourceName.Empty
                        KeyName = keyName
                        KeyVersion = keyVersion
                        KeySource = KeyVault.KeySource.Default })
        { state with
            PrepareEncryption = Enabled
            Encryption = encryption }

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

let dataBricks = WorkspaceBuilder()
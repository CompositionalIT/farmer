[<AutoOpen>]
module Farmer.Builders.Databricks

open Farmer
open Farmer.Databricks
open Farmer.Arm.Databricks
open Farmer.Arm.Network

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
      { ///Name of the workspace
        Name = ResourceName.Empty
        ///Azure managed resource group name
        ManagedResourceGroupId = ResourceName.Empty
        ///Workspace pricing tier
        Sku = StandardTier
        ///Should public IPs be used
        EnablePublicIp = Enabled
        ///Prepare workspace for encryption
        PrepareEncryption = Disabled
        ///Encryption configuration
        Encryption = None
        ///Bring Your Own VNet configuration
        ByovConfig = None
        ///List of tags
        Tags = Map.empty }
    ///Set workspace name  
    [<CustomOperation "name">]
    member _.Name (state:WorkspaceConfig, name) =
        { state with Name = ResourceName name }
    ///Set managed resource group
    [<CustomOperation "managed_resource_group_id">]
    member _.ManagedResourceGroupId (state:WorkspaceConfig, resourceGroupName) =
        { state with ManagedResourceGroupId = ResourceName resourceGroupName}
    ///Set workspace pricing tier
    [<CustomOperation "sku">]
    member _.PricingTier (state:WorkspaceConfig, sku) =
        { state with Sku = sku }
    ///Set public IP enablement
    [<CustomOperation "use_public_ip">]
    member _.DisablePublicIp(state:WorkspaceConfig, flag) =
        { state with EnablePublicIp = flag}
    ///Set the key vault for encryption key
    [<CustomOperation "key_vault">]
    member _.KeyVault(state:WorkspaceConfig, keyVault:ResourceName) =
        let encryption =
            match state.Encryption with
            | Some _ -> 
                state.Encryption
                |> Option.map(fun encryptionConfig -> { encryptionConfig with KeyVault = keyVault })
            | None ->
                Some({ KeyVault = keyVault
                       KeyName = ""
                       KeyVersion = ""
                       KeySource = "Microsoft.Keyvault" })
        { state with Encryption = encryption }
    member this.KeyVault(state:WorkspaceConfig, keyVault) = this.KeyVault(state, ResourceName keyVault)
    member this.KeyVault(state:WorkspaceConfig, keyVault:Arm.KeyVault.Vault) = this.KeyVault(state, keyVault.Name.Value)
    member this.KeyVault(state:WorkspaceConfig, keyVaultConfig:KeyVaultConfig) = this.KeyVault(state, keyVaultConfig.Name.Value)
    ///Set encryption key configuration
    [<CustomOperation "encryption_key">]
    member _.EncryptionKey(state:WorkspaceConfig, keyName, ?keyVersion) =
        let keyVersion = defaultArg keyVersion "latest"
        let encryption = 
            match state.Encryption with
            | Some _ ->
                state.Encryption
                |> Option.map(fun encryptionConfig ->
                    { encryptionConfig with
                        KeyName = keyName
                        KeyVersion = keyVersion})
            | None ->
                Some({ KeyVault = ResourceName.Empty
                       KeyName = keyName 
                       KeyVersion = keyVersion
                       KeySource = "Microsoft.Keyvault" })
        
        { state with 
            PrepareEncryption = Enabled
            Encryption = encryption }
    ///Set existing vnet
    [<CustomOperation "byov_vnet">]
    member _.ByovVnet (state:WorkspaceConfig, vnet:ResourceName) =
        let config =
            match state.ByovConfig with
            | Some config ->
                state.ByovConfig
                |> Option.map(fun vnetConfig -> 
                    { vnetConfig with 
                        Vnet = External(Unmanaged(virtualNetworks.resourceId vnet)) })
            | None -> 
                Some({ Vnet = External(Unmanaged(virtualNetworks.resourceId vnet))
                       PublicSubnet = ResourceName.Empty
                       PrivateSubnet = ResourceName.Empty })
        { state with ByovConfig = config }
    member this.ByovVnet(state:WorkspaceConfig, name:string) = this.ByovVnet(state, ResourceName name)
    member this.ByovVnet(state:WorkspaceConfig, vnet:Arm.Network.VirtualNetwork) = this.ByovVnet(state, vnet.Name)
    member this.ByovVnet(state:WorkspaceConfig, vnet:VirtualNetworkConfig) = this.ByovVnet(state, vnet.Name)
    ///Set existing public subnet
    [<CustomOperation "byov_public_subnet">]
    member _.ByovPublicSubnet (state:WorkspaceConfig, publicSubnet:ResourceName) =
        let config =
            match state.ByovConfig with
            | Some _ -> state.ByovConfig |> Option.map(fun vnetConfig -> { vnetConfig with PublicSubnet = publicSubnet })
            | None ->
                Some({ Vnet = External(Unmanaged(virtualNetworks.resourceId ResourceName.Empty))
                       PublicSubnet = publicSubnet
                       PrivateSubnet = ResourceName.Empty })

        { state with ByovConfig = config}
    member this.ByovPublicSubnet(state:WorkspaceConfig, publicSubnet) = this.ByovPublicSubnet(state, ResourceName publicSubnet)        
    ///Set existing private subnet
    [<CustomOperation "byov_private_subnet">]
    member _.ByovPrivateSubnet (state:WorkspaceConfig, privateSubnet:ResourceName) =
        let config =
            match state.ByovConfig with
            | Some _ -> state.ByovConfig |> Option.map(fun vnetConfig -> { vnetConfig with PrivateSubnet = privateSubnet })
            | None ->
                Some({ Vnet = External(Unmanaged(virtualNetworks.resourceId ResourceName.Empty))
                       PublicSubnet = ResourceName.Empty
                       PrivateSubnet = privateSubnet })

        { state with ByovConfig = config }
    member this.ByovPrivateSubnet(state:WorkspaceConfig, privateSubnet) = this.ByovPrivateSubnet(state, ResourceName privateSubnet)
    ///Add list of tags to workspace resource
    [<CustomOperation "add_tags">]
    member _.Tags(state:WorkspaceConfig, pairs) =
        { state with
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    ///Add single tag to workspace resource
    [<CustomOperation "add_tag">]
    member this.Tag(state:WorkspaceConfig, key, value) = this.Tags(state, [ (key,value) ])

let databricksWorkspace = WorkspaceBuilder()
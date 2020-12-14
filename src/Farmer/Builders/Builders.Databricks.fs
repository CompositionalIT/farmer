[<AutoOpen>]
module Farmer.Builders.Databricks

open Farmer
open Farmer.Databricks
open Farmer.Arm.Databricks

type WorkspaceConfig =
    { Name: ResourceName
      ManagedResourceGroupId: string
      PricingTier: PricingTier
      DisablePublicIp: bool option
      PrepareEncryption: bool option
      Encryption: EncryptionConfig option
      ByovMode: bool option
      ByovConfig: ByovConfig option
      Tags: Map<string,string> }
    interface IBuilder with
        member this.ResourceId = workspaces.resourceId this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              ManagedResourceGroupId = this.ManagedResourceGroupId
              PricingTier = this.PricingTier
              DisablePublicIp = this.DisablePublicIp
              PrepareEncryption = this.PrepareEncryption
              Encryption = this.Encryption
              ByovMode = this.ByovMode
              ByovConfig = this.ByovConfig
              Tags = this.Tags  }
        ]        

type WorkspaceBuilder() =
    member __.Yield _ =
      { 
        ///Name of the workspace
        Name = ResourceName ""
        ///Azure managed resource group name
        ManagedResourceGroupId = ""
        ///Workspace pricing tier
        PricingTier = StandardTier
        ///Should public IPs be used
        DisablePublicIp = None
        ///Prepare workspace for encryption
        PrepareEncryption = None
        ///Encryption configuration
        Encryption = None
        ///Should use Bring Your Own VNet networking model
        ByovMode = None
        ///Bring Your Own VNet configuration
        ByovConfig = None
        ///List of tags
        Tags = Map.empty
      }
    ///Set workspace name  
    [<CustomOperation "name">]
    member __.Name (state:WorkspaceConfig, name) =
        { state with Name = ResourceName name }
    ///Set managed resource group
    [<CustomOperation "managed_resource_group_id">]
    member _.ManagedResourceGroupId (state:WorkspaceConfig, resourceGroupName) =
        { state with ManagedResourceGroupId = resourceGroupName}
    ///Set workspace pricing tier
    [<CustomOperation "pricing_tier">]
    member _.PricingTier (state:WorkspaceConfig, sku) =
        { state with PricingTier = sku }
    ///Set public IP enablement
    [<CustomOperation "disable_public_ip">]
    member _.DisablePublicIp(state:WorkspaceConfig) =
        { state with DisablePublicIp = Some true}
    ///Set encryption preparation
    [<CustomOperation "enable_encryption">]
    member _.PrepareEncryption(state:WorkspaceConfig) =
        { state with 
            PrepareEncryption = Some true
            Encryption = Some {
                KeySource = "Microsoft.Keyvault";
                KeyName = "";
                KeyVersion = "latest";
                KeyVaultUri = ""} }
    ///Set encryption key name
    [<CustomOperation "key_name">]
    member _.KeyName(state:WorkspaceConfig, keyName) =
        {state with Encryption = state.Encryption |> Option.map(fun encryptionConfig -> { encryptionConfig with KeyName = keyName })}
    ///Set encryption key version
    [<CustomOperation "key_version">]
    member _.KeyVersion(state:WorkspaceConfig, keyVersion) =
        {state with Encryption = state.Encryption |> Option.map(fun encryptionConfig -> { encryptionConfig with KeyVersion = keyVersion })}
    ///Set encryption key vault uri
    [<CustomOperation "key_vault">]
    member _.KeyVaultUri(state:WorkspaceConfig, keyVault) =
        let keyVaultUri = 
            sprintf
                "https://%s.vault.azure.net"
                keyVault
        {state with Encryption = state.Encryption |> Option.map(fun encryptionConfig -> { encryptionConfig with KeyVaultUri = keyVaultUri })}
    ///Set use Bring Your Own VNet networking model
    [<CustomOperation "enable_byov_mode">]
    member _.VirtualNetworkName (state:WorkspaceConfig) =
        { state with 
            ByovMode = Some true
            ByovConfig = Some { Vnet = ""; PublicSubnet = ""; PrivateSubnet = "" } }    
    ///Set existing vnet
    [<CustomOperation "byov_vnet">]
    member _.ByovVnet (state:WorkspaceConfig, vnet) =
        { state with ByovConfig = state.ByovConfig |> Option.map(fun vnetConfig -> { vnetConfig with Vnet = vnet })}
    ///Set existing public subnet
    [<CustomOperation "byov_public_subnet">]
    member _.ByovPublicSubnet (state:WorkspaceConfig, publicSubnet) =
        { state with ByovConfig = state.ByovConfig |> Option.map(fun vnetConfig -> { vnetConfig with PublicSubnet = publicSubnet })}
    ///Set existing private subnet
    [<CustomOperation "byov_private_subnet">]
    member _.ByovPrivateSubnet (state:WorkspaceConfig, privateSubnet) =
        { state with ByovConfig = state.ByovConfig |> Option.map(fun vnetConfig -> { vnetConfig with PrivateSubnet = privateSubnet })}
    ///Add list of tags to workspace resource
    [<CustomOperation "add_tags">]
    member _.Tags(state:WorkspaceConfig, pairs) =
        { state with
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    ///Add single tag to workspace resource
    [<CustomOperation "add_tag">]
    member this.Tag(state:WorkspaceConfig, key, value) = this.Tags(state, [ (key,value) ])

let databricksWorkspace = WorkspaceBuilder()
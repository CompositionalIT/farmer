[<AutoOpen>]
module Farmer.Builders.MachineLearning

open Farmer
open Farmer.Arm
open Farmer.CoreTypes

// TODO: Implement AML workspace config
type WorkspaceConfig = 
    { WorkspaceName : ResourceName
      ResourceGroupName : ResourceName
      Sku : MachineLearning.Sku
      StorageAccountOption : MachineLearning.StorageAccountOption
      StorageAccountName : ResourceName
      StorageAccountType : Storage.Sku
      StorageAccountBehindVNet : bool
      StorageAccountResourceName : ResourceName
      KeyVaultOption : MachineLearning.KeyVaultOption
      KeyVaultName : ResourceName
      KeyVaultBehindVNet : bool
      KeyVaultResourceGroupName : ResourceName
      ApplicationInsightsOption : MachineLearning.ApplicationInsightsOption
      ApplicationInsightsName : ResourceName
      ApplicationInsightsResourceGroupName : ResourceName
      ContainerRegistryOption : MachineLearning.ContainerRegistryOption
      ContainerRegistryName : ResourceName
      ContainerRegistrySku : ContainerRegistry.Sku
      ContainerRegistryBehindVNet : bool
      VnetOption : MachineLearning.VnetOption
      VnetName : ResourceName
      VnetResourceGroupName : ResourceName
      AddressPrefixes : IPAddressCidr array
      SubnetOption : MachineLearning.SubnetOption
      SubnetName : ResourceName
      SubnetPrefix : IPAddressCidr
      AdbWorkspace : ResourceName
      ConfidentialData : bool
      EncryptionStatus : MachineLearning.EncryptionStatus
      CmkKeyVault : MachineLearning.CmkId
      ResouceCmkUri : MachineLearning.ResourceCmkUri
      PrivateEndpointType : MachineLearning.PrivateEndpointType
      PrivateEndpointName : ResourceName
      PrivateEndpointResourceGroupName : ResourceName
      Tags : Map<string,string> }
    interface IBuilder with
        member this.DependencyName = this.WorkspaceName
        member this.BuildResources location = []

// TODO: Implement AML workspace builder
type WorkspaceBuilder() = 
    member _.Yield _ = {||}
    member _.Run (state:WorkspaceConfig) = 
        state
    
    [<CustomOperation "workspace_name">]
    member _.WorkspaceName(state:WorkspaceConfig) = 
        state

let machineLearning = WorkspaceBuilder()
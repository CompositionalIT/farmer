[<AutoOpen>]
module Farmer.Arm.MachineLearning

open Farmer
open Farmer.CoreTypes

let workspaces = ResourceType("Microsoft.MachineLearningServices/workspaces","2020-08-01")

type AzureMachineLearningWorkspace = 
    { WorkspaceName : ResourceName
      Location : Location 
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
    interface IArmResource with
        member this.ResourceName = this.WorkspaceName
        member this.JsonModel = {| |} :> _ // TODO : Create JsonModel



[<AutoOpen>]
module Farmer.Arm.Databricks

open Farmer
open Farmer.Databricks
open Farmer.Arm.Network
open System

let workspaces = ResourceType ("Microsoft.Databricks/workspaces", "2018-04-01")

type ByovConfig =
    { Vnet: ResourceRef<ByovConfig>
      PublicSubnet: ResourceName
      PrivateSubnet: ResourceName }

type EncryptionConfig =
    { KeySource: string
      KeyName: string
      KeyVersion: string
      KeyVault: ResourceName }

type Databricks =
    { Name: ResourceName
      Location: Location
      ManagedResourceGroupId: ResourceName
      Sku: Sku
      EnablePublicIp: FeatureFlag
      PrepareEncryption: FeatureFlag
      Encryption: EncryptionConfig option
      ByovConfig: ByovConfig option
      Tags: Map<string,string> }
    member internal this.toProperty value = box {| value = value |}
    interface IArmResource with 
      member this.ResourceId = workspaces.resourceId this.Name
      member this.JsonModel = 
            {| workspaces.Create(this.Name, this.Location, tags = this.Tags) with
                sku = {| name =
                            match this.Sku with
                                | StandardTier -> "standard"
                                | PremiumTier -> "premium" |}
                properties = 
                    {| managedResourceGroupId = 
                        let expr = 
                            sprintf
                                "concat(subscription().id, '/resourceGroups/', '%s')"
                                this.ManagedResourceGroupId.Value
                        ArmExpression.create(expr).Eval()
                       parameters = 
                        {| customVirtualNetworkId = 
                                this.ByovConfig
                                |> Option.map(fun config -> 
                                    this.toProperty(config.Vnet.resourceId(config).Eval()))
                                |> Option.toObj
                           customPublicSubnetName = 
                                this.ByovConfig
                                |> Option.map(fun config -> this.toProperty(config.PublicSubnet.Value))
                                |> Option.toObj
                           customPrivateSubnetName =
                                this.ByovConfig
                                |> Option.map(fun config -> this.toProperty(config.PrivateSubnet.Value))
                                |> Option.toObj
                           enableNoPublicIp = 
                                match this.EnablePublicIp with
                                | Enabled -> this.toProperty false
                                | Disabled -> this.toProperty true
                           prepareEncryption = 
                                match this.PrepareEncryption with
                                | Enabled -> this.toProperty true
                                | Disabled -> this.toProperty false
                           encryption =
                                this.Encryption
                                |> Option.map(fun config ->
                                    {| keySource = config.KeySource
                                       keyName = config.KeyName
                                       Keyversion = config.KeyVersion
                                       keyvaultiri = sprintf "https://%s.vault.azure.net" config.KeyVault.Value |}
                                    |> this.toProperty)
                                |> Option.toObj
                        |}
                    |}
            |} :> _
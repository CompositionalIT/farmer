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
    { KeySource: KeySource
      KeyName: string
      KeyVersion: string
      KeyVault: ResourceName }

type Workspace =
    { Name: ResourceName
      Location: Location
      ManagedResourceGroupId: ResourceName
      Sku: Sku
      EnablePublicIp: FeatureFlag
      PrepareEncryption: FeatureFlag
      Encryption: EncryptionConfig option
      ByovConfig: ByovConfig option
      Tags: Map<string,string> }
    interface IArmResource with
        member this.ResourceId = workspaces.resourceId this.Name
        member this.JsonModel =
            {| workspaces.Create(this.Name, this.Location, tags = this.Tags) with
                sku =
                    {| name =
                        match this.Sku with
                        | StandardTier -> "standard"
                        | PremiumTier -> "premium" |}
                properties =
                    {| managedResourceGroupId =
                        let expr = sprintf "concat(subscription().id, '/resourceGroups/', '%s')" this.ManagedResourceGroupId.Value
                        ArmExpression.create(expr).Eval()
                       parameters = Map [
                        "enableNoPublicIp", box this.EnablePublicIp.AsBoolean
                        "prepareEncryption", box this.PrepareEncryption.AsBoolean
                        match this.ByovConfig with
                        | Some config ->
                            "customVirtualNetworkId", box {| value = config.Vnet.resourceId(config).Eval() |}
                            "customPublicSubnetName", box {| value = config.PublicSubnet.Value |}
                            "customPrivateSubnetName", box {| value = config.PrivateSubnet.Value |}
                        | None ->
                            ()
                        "encryption",
                            this.Encryption
                            |> Option.mapBoxed(fun config ->
                                {| value =
                                    {| keySource = config.KeySource
                                       keyName = config.KeyName
                                       keyversion = config.KeyVersion
                                       keyvaulturi = sprintf "https://%s.vault.azure.net" config.KeyVault.Value |} |})
                        ]
                    |}
            |} :> _
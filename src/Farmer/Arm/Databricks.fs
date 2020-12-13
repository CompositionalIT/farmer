[<AutoOpen>]
module Farmer.Arm.Databricks

open Farmer
open Farmer.Databricks
open Farmer.Arm.Network
open System

let workspaces = ResourceType ("Microsoft.Databricks/workspaces", "2018-04-01")

type ByovConfig =
    { Vnet: string
      PublicSubnet: string
      PrivateSubnet: string }

type EncryptionConfig =
    { KeySource: string
      KeyName: string
      KeyVersion: string
      KeyVaultUri: string }

type Databricks =
    { Name: ResourceName
      Location: Location
      ManagedResourceGroupId: string
      PricingTier: PricingTier
      DisablePublicIp: bool option
      PrepareEncryption: bool option
      Encryption: EncryptionConfig option
      ByovMode: bool option
      ByovConfig: ByovConfig option
      Tags: Map<string,string> }
    member internal this.toProperty value = {|value = value|} |> box
    interface IArmResource with 
      member this.ResourceId = workspaces.resourceId this.Name
      member this.JsonModel = 
            {| workspaces.Create(this.Name, this.Location, tags = this.Tags) with
                sku = {| name =
                            match this.PricingTier with
                                | StandardTier -> "standard"
                                | PremiumTier -> "premium" |}
                properties = 
                    {| managedResourceGroupId = 
                        let expr = 
                            sprintf
                                "concat(subscription().id, '/resourceGroups/', '%s')"
                                this.ManagedResourceGroupId
                        ArmExpression.create(expr).Eval()
                       parameters = 
                        {| customVirtualNetworkId = 
                                match this.ByovMode with
                                    | Some _ ->
                                        match this.ByovConfig with
                                            | Some config -> 
                                                {| value = 
                                                    let expr = 
                                                        sprintf 
                                                            "resourceId('Microsoft.Network/virtualNetworks', '%s')"
                                                            config.Vnet
                                                    ArmExpression.create(expr).Eval() |} |> box
                                            | None -> null
                                    | None -> null
                           customPublicSubnetName = 
                                match this.ByovMode with
                                    | Some _ -> 
                                        match this.ByovConfig with
                                            | Some config -> {| value = config.PublicSubnet |} |> box
                                            | None -> null
                                    | None -> null
                           customPrivsteSubnetName =
                                match this.ByovMode with
                                    | Some _ ->
                                        match this.ByovConfig with
                                            | Some config -> {| value = config.PrivateSubnet |} |> box
                                            | None -> null
                                    | None -> null
                           enableNoPublicIp = 
                                match this.DisablePublicIp with
                                    | Some _ -> {| value = "true" |} |> box
                                    | None -> null
                           prepareEncryption = 
                                match this.PrepareEncryption with
                                    | Some _ -> {| value = "true" |} |> box
                                    | None -> null
                           encryption =
                                match this.PrepareEncryption with
                                    | Some _ ->
                                        match this.Encryption with
                                            | Some config -> 
                                                {| 
                                                    value = 
                                                        {| keySource = config.KeySource
                                                           keyName = config.KeyName
                                                           Keyversion = config.KeyVersion
                                                           keyvaultiri = config.KeyVaultUri |}
                                                |} |> box
                                            | None -> null
                                    | None -> null
                        |}
                    |}
            |} :> _
[<AutoOpen>]
module Farmer.Arm.Databricks

open Farmer
open Farmer.Databricks
open Farmer.Arm.Network
open System

let workspaces = ResourceType ("Microsoft.Databricks/workspaces", "2018-04-01")

type ByovConfig =
    { Vnet : ResourceRef<ByovConfig>
      PublicSubnet : ResourceName
      PrivateSubnet : ResourceName }

type EncryptionMode =
    | DataBricksEncryption
    | KeyVaultEncryption of {| Vault : ResourceName; Key : string; KeyVersion : Guid option |}

type Workspace =
    { Name : ResourceName
      Location : Location
      ManagedResourceGroupId : ResourceName
      Sku : Sku
      EnablePublicIp : FeatureFlag
      EncryptionMode : EncryptionMode option
      ByovConfig : ByovConfig option
      Tags : Map<string,string> }

    interface IArmResource with
        member this.ResourceId = workspaces.resourceId this.Name
        member this.JsonModel =
            {| workspaces.Create(this.Name, this.Location, tags = this.Tags) with
                sku = {| name = this.Sku.ArmValue |}
                properties =
                    {| managedResourceGroupId =
                        let expr = sprintf "concat(subscription().id, '/resourceGroups/', '%s')" this.ManagedResourceGroupId.Value
                        ArmExpression.create(expr).Eval()
                       parameters = Map [
                        "enableNoPublicIp", box (not this.EnablePublicIp.AsBoolean)
                        "prepareEncryption", this.EncryptionMode |> Option.isSome |> box
                        match this.ByovConfig with
                        | Some config ->
                            "customVirtualNetworkId", box {| value = config.Vnet.resourceId(config).Eval() |}
                            "customPublicSubnetName", box {| value = config.PublicSubnet.Value |}
                            "customPrivateSubnetName", box {| value = config.PrivateSubnet.Value |}
                        | None ->
                            ()
                        "encryption",
                            this.EncryptionMode
                            |> Option.mapBoxed(fun config ->
                                {| value =
                                    match config with
                                    | KeyVaultEncryption config ->
                                        {| keySource = "MicrosoftKeyVault"
                                           keyName = config.Key
                                           keyversion = config.KeyVersion |> Option.map string |> Option.defaultValue "latest"
                                           keyvaulturi = sprintf "https://%s.vault.azure.net" config.Vault.Value |}
                                    | DataBricksEncryption ->
                                        {| keySource = "Default"
                                           keyName = null
                                           keyversion = null
                                           keyvaulturi = null |}
                                |})
                        ]
                    |}
            |} :> _
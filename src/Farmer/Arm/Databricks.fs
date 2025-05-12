[<AutoOpen>]
module Farmer.Arm.Databricks

open Farmer
open Farmer.Databricks
open System

let workspaces = ResourceType("Microsoft.Databricks/workspaces", "2018-04-01")

type VnetConfig = {
    Vnet: ResourceId
    PublicSubnet: ResourceName
    PrivateSubnet: ResourceName
}

type KeyEncryption =
    | InfrastructureManaged
    | CustomerManaged of
        {|
            Vault: ResourceId
            Key: string
            KeyVersion: Guid option
        |}

type Workspace = {
    Name: ResourceName
    Location: Location
    ManagedResourceGroupId: ResourceName
    Sku: Sku
    EnablePublicIp: FeatureFlag
    KeyEncryption: KeyEncryption option
    VnetConfig: VnetConfig option
    Tags: Map<string, string>
    Dependencies: ResourceId Set
} with

    interface IArmResource with
        member this.ResourceId = workspaces.resourceId this.Name

        member this.JsonModel = {|
            workspaces.Create(this.Name, this.Location, this.Dependencies, this.Tags) with
                sku = {| name = this.Sku.ArmValue |}
                properties = {|
                    managedResourceGroupId =
                        ArmExpression
                            .create(
                                $"concat(subscription().id, '/resourceGroups/', '{this.ManagedResourceGroupId.Value}')"
                            )
                            .Eval()
                    parameters =
                        Map [
                            "enableNoPublicIp",
                            box {|
                                value = (not this.EnablePublicIp.AsBoolean)
                            |}
                            "prepareEncryption",
                            box {|
                                value = Option.isSome this.KeyEncryption
                            |}
                            match this.VnetConfig with
                            | Some config ->
                                "customVirtualNetworkId", box {| value = config.Vnet.Eval() |}
                                "customPublicSubnetName", box {| value = config.PublicSubnet.Value |}
                                "customPrivateSubnetName", box {| value = config.PrivateSubnet.Value |}
                            | None -> ()
                            match this.KeyEncryption with
                            | Some config ->
                                "encryption",
                                {|
                                    value =
                                        match config with
                                        | CustomerManaged config -> {|
                                            keySource = "Microsoft.Keyvault"
                                            keyName = config.Key
                                            keyversion = config.KeyVersion |> Option.map string |> Option.toObj
                                            keyvaulturi = $"https://{config.Vault.Name.Value}.vault.azure.net"
                                          |}
                                        | InfrastructureManaged -> {|
                                            keySource = "Default"
                                            keyName = null
                                            keyversion = null
                                            keyvaulturi = null
                                          |}
                                |}
                                |> box
                            | None -> ()
                        ]
                |}
        |}
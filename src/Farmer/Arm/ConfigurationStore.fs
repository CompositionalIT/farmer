[<AutoOpen>]
module Farmer.Arm.ConfigurationStore

open Farmer

let configurationStores =
    ResourceType("Microsoft.AppConfiguration/configurationStores", "2024-05-01")

let keyValues =
    ResourceType("Microsoft.AppConfiguration/configurationStores/keyValues", "2024-05-01")

type AzFeatureFlag = {
    Name: string
    Description: string
    Label: string
    State: bool
    ConfigurationStoreId: ResourceId
} with
    member this.ResourceName = ResourceName $"[format('{{0}}/{{1}}', '{this.ConfigurationStoreId.Name.Value}', format('.appconfig.featureflag~2F{{0}}${{1}}', '{this.Name}', '{this.Label}'))]"
    interface IArmResource with
        member this.ResourceId = keyValues.resourceId this.ResourceName
        member this.JsonModel =
            let enabled = this.State |> string |> _.ToLower()
            let featureFlagValue = $"""{{"id":"{this.Name}","description":"{this.Description}","enabled":{enabled}}}"""
            {|
                keyValues.Create(this.ResourceName, dependsOn = [ this.ConfigurationStoreId ]) with
                    properties = {|
                        value = featureFlagValue
                        contentType = "application/vnd.microsoft.appconfig.ff+json;charset=utf-8"
                    |}
            |}

type ConfigSku =
    | Free
    | Developer
    | Standard
    | Premium
    member this.ArmValue =
        match this with
        | Free -> "free"
        | Developer -> "developer"
        | Standard -> "standard"
        | Premium -> "premium"

type DataPlaneAuthenticationMode =
    | Local
    | Passthrough
    member this.ArmValue =
        match this with
        | Local -> "Local"
        | Passthrough -> "Pass-through"

type ConfigurationStore = {
    Name: ResourceName
    Location: Location
    Sku: ConfigSku
    DisableLocalAuth: bool
    DataPlaneAuthenticationMode: DataPlaneAuthenticationMode
    Tags: Map<string, string>
} with
    
    interface IArmResource with
        member this.ResourceId = configurationStores.resourceId this.Name

        member this.JsonModel = {|
            configurationStores.Create(this.Name, this.Location, tags = this.Tags) with
                sku =  {|
                    name = this.Sku.ArmValue
                |}
                properties = {|
                    disableLocalAuth = this.DisableLocalAuth
                    dataPlaneProxy = {|
                        authenticationMode = this.DataPlaneAuthenticationMode.ArmValue
                    |}
                |}
        |}
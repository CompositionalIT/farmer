[<AutoOpen>]
module Farmer.Arm.ConfigurationStore

open Farmer
open Farmer.ConfigurationStore

let configurationStores =
    ResourceType("Microsoft.AppConfiguration/configurationStores", "2024-05-01")

let keyValues =
    ResourceType("Microsoft.AppConfiguration/configurationStores/keyValues", "2024-05-01")

type ConfigFeatureFlag = {
    Name: string
    Description: string
    Label: string
    State: bool
    ConfigurationStoreId: ResourceId
} with

    member this.ResourceName =
        let labelSuffix = if this.Label = "" then "" else $"${this.Label}"
        ResourceName $"{this.ConfigurationStoreId.Name.Value}/.appconfig.featureflag~2F{this.Name}{labelSuffix}"

    interface IArmResource with
        member this.ResourceId = keyValues.resourceId this.ResourceName

        member this.JsonModel =
            let enabled = this.State |> string |> _.ToLower()

            let featureFlagValue =
                $"""{{"id":"{this.Name}","description":"{this.Description}","enabled":{enabled}}}"""

            {|
                keyValues.Create(this.ResourceName, dependsOn = [ this.ConfigurationStoreId ]) with
                    properties = {|
                        value = featureFlagValue
                        contentType = "application/vnd.microsoft.appconfig.ff+json;charset=utf-8"
                    |}
            |}

type KeyValue = {
    /// The name of the key value in the format `{storeName}/{key}` or `{storeName}/{key}$label`.
    Name: ResourceName
    Value: string
    ContentType: string option
    /// Tags applied to the App Configuration key-value item (not ARM resource tags).
    KeyValueTags: Map<string, string>
    Dependencies: ResourceId Set
} with

    interface IArmResource with
        member this.ResourceId = keyValues.resourceId this.Name

        member this.JsonModel = {|
            keyValues.Create(this.Name, dependsOn = this.Dependencies) with
                properties = {|
                    value = this.Value
                    contentType = this.ContentType |> Option.toObj
                    tags =
                        if this.KeyValueTags.IsEmpty then
                            null
                        else
                            box this.KeyValueTags
                |}
        |}

type ConfigurationStore = {
    Name: ResourceName
    Location: Location
    Sku: Sku
    DisableLocalAuth: bool option
    EnablePurgeProtection: bool option
    PublicNetworkAccess: Farmer.FeatureFlag option
    SoftDeleteRetentionInDays: int option
    DataPlaneAuthenticationMode: DataPlaneAuthenticationMode option
    Tags: Map<string, string>
    Dependencies: ResourceId Set
} with

    interface IArmResource with
        member this.ResourceId = configurationStores.resourceId this.Name

        member this.JsonModel = {|
            configurationStores.Create(this.Name, this.Location, this.Dependencies, this.Tags) with
                sku = {|
                    name =
                        match this.Sku with
                        | Free -> "free"
                        | Developer -> "developer"
                        | Standard -> "standard"
                        | Premium -> "premium"
                |}
                properties = {|
                    disableLocalAuth = this.DisableLocalAuth |> Option.toNullable
                    enablePurgeProtection = this.EnablePurgeProtection |> Option.toNullable
                    publicNetworkAccess =
                        this.PublicNetworkAccess
                        |> Option.map (fun f ->
                            match f with
                            | Enabled -> "Enabled"
                            | Disabled -> "Disabled")
                        |> Option.toObj
                    softDeleteRetentionInDays = this.SoftDeleteRetentionInDays |> Option.toNullable
                    dataPlaneProxy =
                        this.DataPlaneAuthenticationMode
                        |> Option.map (fun mode ->
                            box {|
                                authenticationMode =
                                    match mode with
                                    | Local -> "Local"
                                    | Passthrough -> "Pass-through"
                            |})
                        |> Option.toObj
                |}
        |}
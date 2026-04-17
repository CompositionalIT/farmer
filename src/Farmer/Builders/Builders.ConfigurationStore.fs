[<AutoOpen>]
module Farmer.Builders.ConfigurationStore

open Farmer
open Farmer.ConfigurationStore
open Farmer.Arm.ConfigurationStore

type FeatureFlagConfig = {
    Name: string
    Description: string
    Label: string
    State: bool
}

type KeyValueConfig = {
    Key: string
    Label: string option
    Value: string
    ContentType: string option
    /// Tags applied to the App Configuration key-value item (not ARM resource tags).
    KeyValueTags: Map<string, string>
}

type ConfigurationStoreConfig = {
    Name: ResourceName
    Sku: Sku
    DisableLocalAuth: bool option
    EnablePurgeProtection: bool option
    PublicNetworkAccess: FeatureFlag option
    SoftDeleteRetentionInDays: int option
    DataPlaneAuthenticationMode: DataPlaneAuthenticationMode option
    FeatureFlags: FeatureFlagConfig list
    KeyValues: KeyValueConfig list
    Tags: Map<string, string>
    Dependencies: ResourceId Set
} with

    member this.ResourceId = configurationStores.resourceId this.Name

    /// Gets an ARM expression for the endpoint of this App Configuration store.
    member this.Endpoint =
        ArmExpression
            .create($"reference({this.ResourceId.ArmExpression.Value}, '{configurationStores.ApiVersion}').endpoint")
            .WithOwner(this.ResourceId)

    interface IBuilder with
        member this.ResourceId = this.ResourceId

        member this.BuildResources location = [
            {
                ConfigurationStore.Name = this.Name
                Location = location
                Sku = this.Sku
                DisableLocalAuth = this.DisableLocalAuth
                EnablePurgeProtection = this.EnablePurgeProtection
                PublicNetworkAccess = this.PublicNetworkAccess
                SoftDeleteRetentionInDays = this.SoftDeleteRetentionInDays
                DataPlaneAuthenticationMode = this.DataPlaneAuthenticationMode
                Tags = this.Tags
                Dependencies = this.Dependencies
            }
            let storeId = configurationStores.resourceId this.Name

            for ff in this.FeatureFlags do
                {
                    ConfigFeatureFlag.Name = ff.Name
                    Description = ff.Description
                    Label = ff.Label
                    State = ff.State
                    ConfigurationStoreId = storeId
                }

            for kv in this.KeyValues do
                let kvName =
                    match kv.Label with
                    | Some label -> ResourceName $"{this.Name.Value}/{kv.Key}${label}"
                    | None -> ResourceName $"{this.Name.Value}/{kv.Key}"

                {
                    KeyValue.Name = kvName
                    Value = kv.Value
                    ContentType = kv.ContentType
                    KeyValueTags = kv.KeyValueTags
                    Dependencies = Set.singleton storeId
                }
        ]

    interface ITaggable<ConfigurationStoreConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

    interface IDependable<ConfigurationStoreConfig> with
        member _.Add state newDeps = {
            state with
                Dependencies = state.Dependencies + newDeps
        }

type ConfigurationStoreBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        Sku = Free
        DisableLocalAuth = None
        EnablePurgeProtection = None
        PublicNetworkAccess = None
        SoftDeleteRetentionInDays = None
        DataPlaneAuthenticationMode = None
        FeatureFlags = []
        KeyValues = []
        Tags = Map.empty
        Dependencies = Set.empty
    }

    /// Sets the name of the App Configuration store.
    [<CustomOperation "name">]
    member _.Name(state: ConfigurationStoreConfig, name) = { state with Name = ResourceName name }

    /// Sets the SKU of the App Configuration store. Defaults to Free.
    [<CustomOperation "sku">]
    member _.Sku(state: ConfigurationStoreConfig, sku) = { state with Sku = sku }

    /// Disables local (access-key) authentication for the App Configuration store.
    [<CustomOperation "disable_local_auth">]
    member _.DisableLocalAuth(state: ConfigurationStoreConfig) = {
        state with
            DisableLocalAuth = Some true
    }

    /// Enables purge protection for the App Configuration store (Standard SKU only).
    [<CustomOperation "enable_purge_protection">]
    member _.EnablePurgeProtection(state: ConfigurationStoreConfig) = {
        state with
            EnablePurgeProtection = Some true
    }

    /// Sets the public network access for the App Configuration store.
    [<CustomOperation "public_network_access">]
    member _.PublicNetworkAccess(state: ConfigurationStoreConfig, access) = {
        state with
            PublicNetworkAccess = Some access
    }

    /// Sets the soft-delete retention period in days (Standard SKU only, 1-7 days).
    [<CustomOperation "soft_delete_retention_in_days">]
    member _.SoftDeleteRetentionInDays(state: ConfigurationStoreConfig, days) = {
        state with
            SoftDeleteRetentionInDays = Some days
    }

    /// Sets the data plane authentication mode for the App Configuration store.
    [<CustomOperation "data_plane_authentication_mode">]
    member _.DataPlaneAuthenticationMode(state: ConfigurationStoreConfig, mode) = {
        state with
            DataPlaneAuthenticationMode = Some mode
    }

    /// Adds feature flags to the App Configuration store.
    [<CustomOperation "add_feature_flags">]
    member _.AddFeatureFlags(state: ConfigurationStoreConfig, featureFlags: FeatureFlagConfig list) = {
        state with
            FeatureFlags = state.FeatureFlags @ featureFlags
    }

    /// Adds a single feature flag to the App Configuration store.
    [<CustomOperation "add_feature_flag">]
    member _.AddFeatureFlag(state: ConfigurationStoreConfig, featureFlag: FeatureFlagConfig) = {
        state with
            FeatureFlags = state.FeatureFlags @ [ featureFlag ]
    }

    /// Adds key-value items to the App Configuration store.
    [<CustomOperation "add_key_values">]
    member _.AddKeyValues(state: ConfigurationStoreConfig, keyValues: KeyValueConfig list) = {
        state with
            KeyValues = state.KeyValues @ keyValues
    }

    /// Adds a single key-value item to the App Configuration store.
    [<CustomOperation "add_key_value">]
    member _.AddKeyValue(state: ConfigurationStoreConfig, keyValue: KeyValueConfig) = {
        state with
            KeyValues = state.KeyValues @ [ keyValue ]
    }

    interface ITaggable<ConfigurationStoreConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

    interface IDependable<ConfigurationStoreConfig> with
        member _.Add state newDeps = {
            state with
                Dependencies = state.Dependencies + newDeps
        }

let configurationStore = ConfigurationStoreBuilder()
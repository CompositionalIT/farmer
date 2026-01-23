[<AutoOpen>]
module Farmer.Builders.ConfigurationStore

open Farmer
open Farmer.Arm.ConfigurationStore

type FeatureFlagConfig =
    {
        Name: string
        Description: string
        Label: string
        State: bool
    }
    member this.BuildResource (configurationStore: ResourceId) =
        {
            Name = this.Name
            Description = this.Description
            Label = this.Label
            State = this.State
            ConfigurationStoreId = configurationStore
        }
        :> IArmResource

type ConfigurationStoreConfig =
    {
        Name: ResourceName
        Location: Location
        Sku: ConfigSku
        DisableLocalAuth: bool
        DataPlaneAuthenticationMode: DataPlaneAuthenticationMode
        FeatureFlags: FeatureFlagConfig list
        Tags: Map<string, string>
    }
    interface IBuilder with
        member this.ResourceId = configurationStores.resourceId this.Name
        member this.BuildResources location  = [
            {
                Name = this.Name
                Location = location
                Sku = this.Sku
                DisableLocalAuth = this.DisableLocalAuth
                DataPlaneAuthenticationMode = this.DataPlaneAuthenticationMode
                Tags = this.Tags
            }
            yield! this.FeatureFlags |> List.map (fun ff -> ff.BuildResource (configurationStores.resourceId this.Name))
        ]

type ConfigurationStoreBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        Location = Location.NorthEurope
        Sku = ConfigSku.Free
        DisableLocalAuth = true
        DataPlaneAuthenticationMode = DataPlaneAuthenticationMode.Passthrough
        FeatureFlags = []
        Tags = Map.empty
    }

    /// Sets the name of the configuration store.
    [<CustomOperation "name">]
    member _.Name(state: ConfigurationStoreConfig, name) = { state with Name = ResourceName name }

    /// Sets the location of the configuration store.
    [<CustomOperation "location">]
    member _.Location(state: ConfigurationStoreConfig, location) = { state with Location = location }

    /// Sets the SKU of the configuration store.
    [<CustomOperation "sku">]
    member _.Sku(state: ConfigurationStoreConfig, sku) = { state with Sku = sku }

    /// Disables local authentication for the configuration store.
    [<CustomOperation "disable_local_auth">]
    member _.DisableLocalAuth(state: ConfigurationStoreConfig, disable: bool) = { state with DisableLocalAuth = disable }

    /// Sets the data plane authentication mode for the configuration store.
    [<CustomOperation "data_plane_authentication_mode">]
    member _.DataPlaneAuthenticationMode(state: ConfigurationStoreConfig, mode) = { state with DataPlaneAuthenticationMode = mode }

    /// Adds a feature flag to the configuration store.
    [<CustomOperation "feature_flags">]
    member _.FeatureFlags(state: ConfigurationStoreConfig, featureFlags: FeatureFlagConfig list) = { state with FeatureFlags = featureFlags }

    interface ITaggable<ConfigurationStoreConfig> with
        /// Adds a tag to this Configuration Store.
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

let configurationStore = ConfigurationStoreBuilder()
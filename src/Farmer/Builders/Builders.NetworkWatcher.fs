[<AutoOpen>]
module Farmer.Builders.NetworkWatcher

open Farmer
open Farmer.Arm.Network

type NetworkWatcherConfig = {
    Name: ResourceName
    Tags: Map<string, string>
} with

    interface IBuilder with
        member this.ResourceId = networkWatchers.resourceId this.Name

        member this.BuildResources location = [
            {
                NetworkWatcher.Name = this.Name
                Location = location
                Tags = this.Tags
            }
        ]

type NetworkWatcherBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        Tags = Map.empty
    }

    /// Sets the name of the Network Watcher.
    [<CustomOperation "name">]
    member _.Name(state: NetworkWatcherConfig, name: string) = { state with Name = ResourceName name }

    interface ITaggable<NetworkWatcherConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

type FlowLogConfig = {
    Name: ResourceName
    NetworkWatcher: ResourceName
    TargetNsg: ResourceId option
    StorageAccount: ResourceId option
    RetentionDays: int
    LogAnalytics: ResourceId option
    Tags: Map<string, string>
} with

    interface IBuilder with
        member this.ResourceId =
            match this.NetworkWatcher with
            | name when name <> ResourceName.Empty -> flowLogs.resourceId (name, this.Name)
            | _ -> raiseFarmer "Flow log must be linked to a Network Watcher"

        member this.BuildResources location =
            match this.TargetNsg, this.StorageAccount with
            | Some nsg, Some storage -> [
                {
                    FlowLog.Name = this.Name
                    Location = location
                    NetworkWatcher = this.NetworkWatcher
                    TargetResourceId = nsg
                    StorageAccountId = storage
                    Enabled = true
                    RetentionDays = this.RetentionDays
                    WorkspaceId = this.LogAnalytics
                    Tags = this.Tags
                }
              ]
            | None, _ -> raiseFarmer "Flow log must have a target NSG (use link_to_nsg)"
            | _, None -> raiseFarmer "Flow log must have a storage account (use link_to_storage_account)"

type FlowLogBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        NetworkWatcher = ResourceName.Empty
        TargetNsg = None
        StorageAccount = None
        RetentionDays = 7
        LogAnalytics = None
        Tags = Map.empty
    }

    /// Sets the name of the flow log.
    [<CustomOperation "name">]
    member _.Name(state: FlowLogConfig, name: string) = { state with Name = ResourceName name }

    /// Links the flow log to a Network Watcher.
    [<CustomOperation "link_to_network_watcher">]
    member _.LinkToNetworkWatcher(state: FlowLogConfig, networkWatcher: NetworkWatcherConfig) = {
        state with
            NetworkWatcher = (networkWatcher :> IBuilder).ResourceId.Name
    }

    member _.LinkToNetworkWatcher(state: FlowLogConfig, networkWatcherName: string) = {
        state with
            NetworkWatcher = ResourceName networkWatcherName
    }

    /// Links the flow log to an NSG.
    [<CustomOperation "link_to_nsg">]
    member _.LinkToNsg(state: FlowLogConfig, nsgId: ResourceId) = { state with TargetNsg = Some nsgId }

    /// Links the flow log to a storage account for storing logs.
    [<CustomOperation "link_to_storage_account">]
    member _.LinkToStorageAccount(state: FlowLogConfig, storageId: ResourceId) = {
        state with
            StorageAccount = Some storageId
    }

    /// Sets the retention period in days (0 = unlimited, default 7 days).
    [<CustomOperation "retention_days">]
    member _.RetentionDays(state: FlowLogConfig, days: int) = { state with RetentionDays = days }

    /// Enables Traffic Analytics by linking to a Log Analytics Workspace.
    [<CustomOperation "enable_traffic_analytics">]
    member _.EnableTrafficAnalytics(state: FlowLogConfig, workspaceId: ResourceId) = {
        state with
            LogAnalytics = Some workspaceId
    }

    interface ITaggable<FlowLogConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

/// Builds a Network Watcher resource.
let networkWatcher = NetworkWatcherBuilder()

/// Builds a Flow Log resource for NSG monitoring.
let flowLog = FlowLogBuilder()

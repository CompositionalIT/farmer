[<AutoOpen>]
module Farmer.Arm.LogAnalytics

open Farmer

let workspaces =
    ResourceType("Microsoft.OperationalInsights/workspaces", "2020-03-01-preview")

type Workspace = {
    Name: ResourceName
    Location: Location
    RetentionPeriod: int<Days> option
    IngestionSupport: FeatureFlag option
    QuerySupport: FeatureFlag option
    DailyCap: int<Gb> option
    Tags: Map<string, string>
} with

    interface IArmResource with
        member this.ResourceId = workspaces.resourceId this.Name

        member this.JsonModel = {|
            workspaces.Create(this.Name, this.Location, tags = this.Tags) with
                properties = {|
                    sku = {| name = "PerGB2018" |}
                    retentionInDays = this.RetentionPeriod |> Option.toNullable
                    workspaceCapping =
                        match this.DailyCap with
                        | None -> null
                        | Some cap -> {| dailyQuotaGb = cap |} |> box
                    publicNetworkAccessForIngestion = this.IngestionSupport |> Option.map _.ArmValue |> Option.toObj
                    publicNetworkAccessForQuery = this.QuerySupport |> Option.map _.ArmValue |> Option.toObj
                |}
        |}

type LogAnalytics =
    static member getCustomerId resourceId =
        ArmExpression.reference(workspaces, resourceId).Map(fun r -> r + ".customerId").WithOwner(resourceId)

    static member getCustomerId(name, ?resourceGroup) =
        LogAnalytics.getCustomerId (ResourceId.create (workspaces, name, ?group = resourceGroup))

    static member getPrimarySharedKey resourceId =
        ArmExpression.listKeys(workspaces, resourceId).Map(fun r -> r + ".primarySharedKey").WithOwner(resourceId)

    static member getPrimarySharedKey(name, ?resourceGroup) =
        LogAnalytics.getPrimarySharedKey (ResourceId.create (workspaces, name, ?group = resourceGroup))
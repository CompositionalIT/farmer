[<AutoOpen>]
module Farmer.Arm.LogAnalytics

open Farmer

let workspaces =
    ResourceType("Microsoft.OperationalInsights/workspaces", "2020-03-01-preview")

let tables =
    ResourceType("Microsoft.OperationalInsights/workspaces/tables", "2023-09-01")

type Plan =
    | Analytics of RetentionInDays: int<Days> option
    | Auxiliary
    | Basic

    member this.ArmValue =
        match this with
        | Analytics _ -> "Analytics"
        | Auxiliary -> "Auxiliary"
        | Basic -> "Basic"

    member this.RetentionInDays =
        match this with
        | Analytics days ->
            match days with
            | Some d -> Some d
            | None -> Some -1<Days>
        | Auxiliary -> None
        | Basic -> None

type Table = {
    Name: ResourceName
    Plan: Plan
    Columns: Column list
    TotalRetentionInDays: int<Days> option
    LogAnalyticsWorkspace: ResourceId
} with

    interface IArmResource with
        member this.ResourceId =
            tables.resourceId (this.LogAnalyticsWorkspace.Name / this.Name)

        member this.JsonModel = {|
            tables.Create(this.LogAnalyticsWorkspace.Name / this.Name, dependsOn = [ this.LogAnalyticsWorkspace ]) with
                properties = {|
                    plan = this.Plan.ArmValue
                    retentionInDays = this.Plan.RetentionInDays |> Option.toNullable
                    totalRetentionInDays = this.TotalRetentionInDays |> Option.defaultValue -1<Days>
                    schema = {|
                        name = this.Name.Value
                        columns =
                            this.Columns
                            |> List.map (fun c -> {|
                                name = c.Name
                                ``type`` = c.Type.ArmValue
                            |})
                    |}
                |}
        |}

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
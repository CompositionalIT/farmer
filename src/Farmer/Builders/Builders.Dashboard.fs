[<AutoOpen>]
module Farmer.Builders.Dashboard

open Farmer
open Farmer.Arm.Dashboard

type DashboardConfig =
    { Name : ResourceName
      Title : string option
      Metadata : DashboardMetadata
      LensParts : LensPart list
      Dependencies : Set<ResourceId> }
    interface IBuilder with
        member this.ResourceId = dashboard.resourceId this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Title = this.Title
              Location = location
              Metadata = this.Metadata
              LensParts = this.LensParts
              Dependencies = this.Dependencies }
        ]

type DashboardBuilder() =
    member __.Yield _ =
        { Name = ResourceName.Empty
          Title = None
          Metadata = DashboardMetadata.EmptyMetadata
          LensParts = List.empty
          Dependencies = Set.empty }
    [<CustomOperation "name">]
    /// Sets the name of the dashboard.
    member __.Name(state:DashboardConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "title">]
    /// Sets the visible title for the dashboard. Default: Same as name.
    member __.Title(state:DashboardConfig, title) = { state with Title = Some title }

    [<CustomOperation "metadata">]
    /// Sets the metadata for the dashboard. Pre-defined DashboardMetadata objects: EmptyMetadata, CustomMetadata and Cache24h
    member __.Metadata(state:DashboardConfig, metadata) = { state with Metadata = metadata }

    [<CustomOperation "add_custom_lens">]
    /// Create your own lens part for the dashboard
    member __.CustomLens(state:DashboardConfig, lens) = { state with LensParts = lens :: state.LensParts }

    [<CustomOperation "add_markdown_part">]
    /// Create markdown lens part for the dashboard
    member __.MarkdownPart(state:DashboardConfig, (position, markdownPart)) = 
        let markdown = generateMarkdownPart markdownPart
        { state with LensParts = ({ position = position; metadata = markdown}) :: state.LensParts }

    [<CustomOperation "add_video_part">]
    /// Create video part for the dashboard
    member __.VideoPart(state:DashboardConfig, (position, videoPart)) = 
        let videopart = generateVideoPart videoPart
        { state with LensParts = ({ position = position; metadata = videopart}) :: state.LensParts }

    [<CustomOperation "add_virtual_machine_icon">]
    /// Create virtual machine status part for the dashboard
    member __.VirtualMachinePart(state:DashboardConfig, (position, virtualMachineId)) = 
        let vmPart = generateVirtualMachinePart virtualMachineId
        { state with LensParts = ({ position = position; metadata = vmPart}) :: state.LensParts }

    [<CustomOperation "add_webtest_results_part">]
    /// Create webtest results part for the dashboard
    member __.WebtestResultPart(state:DashboardConfig, (position, virtualMachineName)) = 
        let vmPart = generateWebtestResultPart virtualMachineName
        { state with LensParts = ({ position = position; metadata = vmPart}) :: state.LensParts }

    [<CustomOperation "add_metrics_chart">]
    /// Create metrics results part for the resource given in parameters
    member __.MetricsChartPart(state:DashboardConfig, (position, metricsChart)) = 
        let metricsChartPart = generateMetricsChartPart metricsChart
        { state with LensParts = ({ position = position; metadata = metricsChartPart}) :: state.LensParts }

    [<CustomOperation "add_monitor_chart">]
    /// Create metrics results part for the resource given in parameters
    member __.MonitorChartPart(state:DashboardConfig, (position, monitorChart)) = 
        let monitorChartPart = generateMonitorChartPart monitorChart
        { state with LensParts = ({ position = position; metadata = monitorChartPart}) :: state.LensParts }

    /// Enable support for additional dependencies.
    interface IDependable<DashboardConfig> with member _.Add state newDeps = { state with Dependencies = state.Dependencies + newDeps }

let dashboard = DashboardBuilder()
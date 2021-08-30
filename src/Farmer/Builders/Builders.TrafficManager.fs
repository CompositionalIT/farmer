[<AutoOpen>]
module Farmer.Builders.TrafficManager

open Farmer
open Farmer.TrafficManager
open Farmer.Arm.TrafficManager
open System
open System.Net
open Farmer.Arm

type EndpointConfig =
    { Name : ResourceName
      Status : FeatureFlag
      Target : EndpointTarget
      Weight : int option
      Priority : int option
      Dependencies: Set<ResourceId> }

type TrafficManagerConfig =
    { Name : ResourceName
      DnsTtl : int<Seconds>
      Status : FeatureFlag
      RoutingMethod : RoutingMethod
      MonitorConfig : MonitorConfig
      EndpointConfigs : EndpointConfig list
      TrafficViewEnrollmentStatus : FeatureFlag
      Dependencies: Set<ResourceId>
      Tags: Map<string,string> }
    interface IBuilder with
        member this.ResourceId = profiles.resourceId this.Name
        member this.BuildResources location =
            let dependencies = this.EndpointConfigs
                               |> List.map (fun e -> e.Dependencies |> Set.toList)
                               |> List.concat
                               |> Set.ofList
                               |> Set.union this.Dependencies

            [ { Name = this.Name
                Status = this.Status
                RoutingMethod = this.RoutingMethod
                DnsTtl = this.DnsTtl
                MonitorConfig = this.MonitorConfig
                TrafficViewEnrollmentStatus = this.TrafficViewEnrollmentStatus
                Dependencies = dependencies
                Tags = this.Tags
                Endpoints = this.EndpointConfigs
                            |> List.map (fun e -> { Name = e.Name
                                                    Status = e.Status
                                                    Target = e.Target
                                                    Weight = e.Weight
                                                    Priority = e.Priority
                                                    Location = match e.Target with
                                                                | External (_, l) -> Some l
                                                                | _ -> None } : Endpoint) } ]

type EndpointBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Status = Enabled
          Target = EndpointTarget.Website ResourceName.Empty
          Weight = None
          Priority = None
          Dependencies = Set.empty }

    member _.Run (state:EndpointConfig) =
        state

    /// Sets the name of the Endpoint
    [<CustomOperation "name">]
    member _.Name(state:EndpointConfig, name) = { state with Name = name }
    member this.Name(state:EndpointConfig, name) = this.Name(state, ResourceName name)

    /// Sets the weight of the Endpoint
    [<CustomOperation "weight">]
    member _.Weight(state:EndpointConfig, weight) = { state with Weight = Some weight }

    /// Disables the Endpoint
    [<CustomOperation "disable_endpoint">]
    member _.DisableEndpoint(state:EndpointConfig) = { state with Status = Disabled }

    /// Enables the Endpoint
    [<CustomOperation "enable_endpoint">]
    member _.EnableEndpoint(state:EndpointConfig) = { state with Status = Enabled }

    /// Sets the priority of the Endpoint
    [<CustomOperation "priority">]
    member _.Priority(state:EndpointConfig, priority) = { state with Priority = Some priority }

    /// Sets the target of the Endpoint to a web app
    [<CustomOperation "target_webapp">]
    member _.TargetWebApp(state:EndpointConfig, name) =
        { state with
            Target = Website name
            Dependencies = state.Dependencies |> Set.add (sites.resourceId(name)) }
    member _.TargetWebApp(state:EndpointConfig, (webApp: WebAppConfig)) =
        { state with
            Target = Website webApp.Name.ResourceName
            Dependencies = state.Dependencies |> Set.add webApp.ResourceId }

    /// Sets the target of the Endpoint to an external domain/IP and location
    [<CustomOperation "target_external">]
    member _.TargetExternal(state:EndpointConfig, domain, location) =
        { state with Target = External (domain, location) }
    member _.TargetExternal(state:EndpointConfig, ipAddress: IPAddress, location) =
        { state with Target = External (string ipAddress, location) }

type TrafficManagerBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          DnsTtl = 30<Seconds>
          Status = Enabled
          RoutingMethod = RoutingMethod.Performance
          TrafficViewEnrollmentStatus = Disabled
          EndpointConfigs = []
          MonitorConfig =
            { Protocol = MonitorProtocol.Http
              Port = 80
              Path = "/"
              IntervalInSeconds = 30<Seconds>
              ToleratedNumberOfFailures = 3
              TimeoutInSeconds = 10<Seconds> }
          Dependencies = Set.empty
          Tags = Map.empty }

    member _.Run (state:TrafficManagerConfig) =
        state

    /// Sets the name of the Traffic Manager profile
    [<CustomOperation "name">]
    member _.Name(state:TrafficManagerConfig, name) = { state with Name = name }
    member this.Name(state:TrafficManagerConfig, name) = this.Name(state, ResourceName name)

    /// Adds Endpoints to the Traffic Manager profile
    [<CustomOperation "add_endpoints">]
    member _.AddEndpoints(state:TrafficManagerConfig, endpoints:EndpointConfig list) =
        { state with EndpointConfigs = state.EndpointConfigs @ endpoints }
    member this.AddEndpoints(state:TrafficManagerConfig, endpoint:EndpointConfig) =
        this.AddEndpoints(state, [endpoint])

    /// Sets the DNS TTL of the Traffic Manager profile, in seconds (default 30)
    [<CustomOperation "dns_ttl">]
    member _.DnsTtl(state:TrafficManagerConfig, ttl: int<Seconds>) = { state with DnsTtl = ttl }
    member this.DnsTtl(state:TrafficManagerConfig, ttl: TimeSpan) = { state with DnsTtl = (int ttl.TotalSeconds) * 1<Seconds> }

    /// Disables the Traffic Manager profile
    [<CustomOperation "disable_profile">]
    member _.DisableProfileStatus(state:TrafficManagerConfig) = { state with Status = Disabled }

    /// Enables the Traffic Manager profile
    [<CustomOperation "enable_profile">]
    member _.EnableProfileStatus(state:TrafficManagerConfig) = { state with Status = Enabled }

    /// Sets the routing method of the Traffic Manager profile (default Performance)
    [<CustomOperation "routing_method">]
    member _.RoutingMethod(state:TrafficManagerConfig, routingMethod) = { state with RoutingMethod = routingMethod }

    /// Enables the Traffic View of the Traffic Manager profile
    [<CustomOperation "enable_traffic_view">]
    member _.EnableTrafficView(state:TrafficManagerConfig) =
        { state with TrafficViewEnrollmentStatus = Enabled }

    /// Disables the Traffic View of the Traffic Manager profile
    [<CustomOperation "disable_traffic_view">]
    member _.DisableTrafficView(state:TrafficManagerConfig) =
        { state with TrafficViewEnrollmentStatus = Disabled }

    /// Sets the monitoring protocol of the Traffic Manager profile (default Http)
    [<CustomOperation "monitor_protocol">]
    member _.MonitorProtocol(state:TrafficManagerConfig, protocol) =
        { state with MonitorConfig = { state.MonitorConfig with Protocol = protocol } }

    /// Sets the monitoring port of the Traffic Manager profile (default 80)
    [<CustomOperation "monitor_port">]
    member _.MonitorPort(state:TrafficManagerConfig, port) =
        { state with MonitorConfig = { state.MonitorConfig with Port = port } }

    /// Sets the monitoring path of the Traffic Manager profile (default /)
    [<CustomOperation "monitor_path">]
    member _.MonitorPath(state:TrafficManagerConfig, path) =
        { state with MonitorConfig = { state.MonitorConfig with Path = path } }

    /// Sets the monitoring interval, in seconds, of the Traffic Manager profile (default 30)
    [<CustomOperation "monitor_interval">]
    member _.MonitorInterval(state:TrafficManagerConfig, interval) =
        { state with MonitorConfig = { state.MonitorConfig with IntervalInSeconds = interval } }
    member this.MonitorInterval(state:TrafficManagerConfig, interval: TimeSpan) =
        this.MonitorInterval(state, (int interval.TotalSeconds) * 1<Seconds>)

    /// Sets the monitoring timeout, in seconds, of the Traffic Manager profile (default 10)
    [<CustomOperation "monitor_timeout">]
    member _.MonitorTimeout(state:TrafficManagerConfig, timeout) =
        { state with MonitorConfig = { state.MonitorConfig with TimeoutInSeconds = timeout } }
    member this.MonitorTimeout(state:TrafficManagerConfig, timeout: TimeSpan) =
        this.MonitorTimeout(state, (int timeout.TotalSeconds) * 1<Seconds>)

    /// Sets the monitoring tolerated number of failures, of the Traffic Manager profile (default 3)
    [<CustomOperation "monitor_tolerated_failures">]
    member _.MonitorToleratedFailures(state:TrafficManagerConfig, failures) =
        { state with MonitorConfig = { state.MonitorConfig with ToleratedNumberOfFailures = failures } }

    interface ITaggable<TrafficManagerConfig> with
        member _.Add state tags = { state with Tags = state.Tags |> Map.merge tags }

    interface IDependable<TrafficManagerConfig> with
        member _.Add state newDeps = { state with Dependencies = state.Dependencies + newDeps }


// Expose Builders
let endpoint = EndpointBuilder()
let trafficManager = TrafficManagerBuilder()
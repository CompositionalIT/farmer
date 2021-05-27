[<AutoOpen>]
module Farmer.Builders.TrafficManager

open Farmer
open Farmer.TrafficManager
open Farmer.Arm.TrafficManager

type EndpointConfig =
    { Name : ResourceName
      Status : EndpointStatus
      Target : EndpointTarget
      Weight : int
      Priority : int }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Status = this.Status
              Target = this.Target
              Weight = this.Weight
              Priority = this.Priority
              Location = location }
        ]

type TrafficManagerConfig =
    { Name : ResourceName
      DnsTtl : int
      Status : FeatureFlag
      RoutingMethod : RoutingMethod
      MonitorConfig : MonitorConfig
      Endpoints : EndpointConfig list
      TrafficViewEnrollmentStatus : FeatureFlag
      Tags: Map<string,string> }
    interface IBuilder with
        member this.ResourceId = profiles.resourceId this.Name
        member this.BuildResources _ = [
            { Name = this.Name
              Status = this.Status
              RoutingMethod = this.RoutingMethod
              DnsTtl = this.DnsTtl
              MonitorConfig = this.MonitorConfig
              TrafficViewEnrollmentStatus = this.TrafficViewEnrollmentStatus
              DependsOn = []
              Tags = this.Tags
              Endpoints = this.Endpoints
                          |> List.map (fun e -> { Name = e.Name
                                                  Status = e.Status
                                                  Target = e.Target
                                                  Weight = e.Weight
                                                  Priority = e.Priority
                                                  Location = location })}
        ]

type EndpointBuilder() =
    member __.Yield _ =
        { Name = ResourceName.Empty
          Status = EndpointStatus.Enabled
          Target = EndpointTarget.Website ResourceName.Empty
          Weight = 1
          Priority = 1 }

    member __.Run (state:EndpointConfig) =
        state

    [<CustomOperation "name">]
    member __.Name(state:EndpointConfig, name) = { state with Name = name }
    member this.Name(state:EndpointConfig, name) = this.Name(state, ResourceName name)

    [<CustomOperation "weight">]
    member __.Weight(state:EndpointConfig, weight) = { state with Weight = weight }

    [<CustomOperation "disable_endpoint">]
    member __.DisableEndpoint(state:EndpointConfig) = { state with Status = EndpointStatus.Disabled }

    [<CustomOperation "enable_endpoint">]
    member __.EnableEndpoint(state:EndpointConfig) = { state with Status = EndpointStatus.Enabled }

    [<CustomOperation "priority">]
    member __.Priority(state:EndpointConfig, priority) = { state with Priority = priority }

    [<CustomOperation "target_webapp">]
    member __.TargetWebSite(state:EndpointConfig, name) = { state with Target = Website name }
    member __.TargetWebSite(state:EndpointConfig, (webApp: WebAppConfig)) = { state with Target = Website webApp.Name }

    [<CustomOperation "target_external_domain">]
    member __.TargetExternalDomain(state:EndpointConfig, domain) = { state with Target = ExternalDomain domain }

type TrafficManagerBuilder() =
    member __.Yield _ =
        { Name = ResourceName.Empty
          DnsTtl = 30
          Status = Enabled
          RoutingMethod = RoutingMethod.Performance
          TrafficViewEnrollmentStatus = Disabled
          Endpoints = []
          MonitorConfig =
            { Protocol = MonitorProtocol.Https
              Port = 443
              Path = "/"
              IntervalInSeconds = 30
              ToleratedNumberOfFailures = 3
              TimeoutInSeconds = 10 }
          Tags = Map.empty }

    member __.Run (state:TrafficManagerConfig) =
        state

    /// Sets the name of the TrafficManager instance.
    [<CustomOperation "name">]
    member __.Name(state:TrafficManagerConfig, name) = { state with Name = name }
    member this.Name(state:TrafficManagerConfig, name) = this.Name(state, ResourceName name)

    [<CustomOperation "add_tags">]
    member _.Tags(state:TrafficManagerConfig, pairs) =
        { state with
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }

    [<CustomOperation "add_tag">]
    member this.Tag(state:TrafficManagerConfig, key, value) = this.Tags(state, [ (key,value) ])

    [<CustomOperation "add_endpoint">]
    member __.AddEndpoint(state:TrafficManagerConfig, endpoint:EndpointConfig) =
        { state with Endpoints = endpoint :: state.Endpoints }

    [<CustomOperation "dns_ttl">]
    member __.DnsTtl(state:TrafficManagerConfig, ttl) = { state with DnsTtl = ttl }

    [<CustomOperation "disable_profile">]
    member __.DisableProfileStatus(state:TrafficManagerConfig) = { state with Status = ProfileStatus.Disabled }

    [<CustomOperation "enable_profile">]
    member __.EnableProfileStatus(state:TrafficManagerConfig) = { state with Status = ProfileStatus.Enabled }

    [<CustomOperation "routing_method">]
    member __.RoutingMethod(state:TrafficManagerConfig, routingMethod) = { state with RoutingMethod = routingMethod }

    [<CustomOperation "enable_traffic_view">]
    member __.EnableTrafficView(state:TrafficManagerConfig) =
        { state with TrafficViewEnrollmentStatus = TrafficViewEnrollmentStatus.Enabled }

    [<CustomOperation "disable_traffic_view">]
    member __.DisableTrafficView(state:TrafficManagerConfig) =
        { state with TrafficViewEnrollmentStatus = TrafficViewEnrollmentStatus.Disabled }

    [<CustomOperation "monitor_protocol">]
    member __.MonitorProtocol(state:TrafficManagerConfig, protocol) =
        { state with MonitorConfig = { state.MonitorConfig with Protocol = protocol } }

    [<CustomOperation "monitor_port">]
    member __.MonitorPort(state:TrafficManagerConfig, port) =
        { state with MonitorConfig = { state.MonitorConfig with Port = port } }

    [<CustomOperation "monitor_path">]
    member __.MonitorPath(state:TrafficManagerConfig, path) =
        { state with MonitorConfig = { state.MonitorConfig with Path = path } }

    [<CustomOperation "monitor_interval">]
    member __.MonitorInterval(state:TrafficManagerConfig, interval) =
        { state with MonitorConfig = { state.MonitorConfig with IntervalInSeconds = interval } }

    [<CustomOperation "monitor_timeout">]
    member __.MonitorTimeout(state:TrafficManagerConfig, timeout) =
        { state with MonitorConfig = { state.MonitorConfig with TimeoutInSeconds = timeout } }

    [<CustomOperation "monitor_tolerated_failures">]
    member __.MonitorToleratedFailures(state:TrafficManagerConfig, failures) =
        { state with MonitorConfig = { state.MonitorConfig with ToleratedNumberOfFailures = failures } }

// Expose Builders
let endpoint = EndpointBuilder()
let trafficManager = TrafficManagerBuilder()
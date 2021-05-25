[<AutoOpen>]
module Farmer.Builders.TrafficManager

open Farmer
open Farmer.Arm.TrafficManager

type TrafficManagerConfig =
    { Name : ResourceName
      DnsTtl : int
      Status : FeatureFlag
      RoutingMethod : RoutingMethod
      MonitorConfig : MonitorConfig
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
              Tags = this.Tags }
        ]

type TrafficManagerBuilder() =
    member __.Yield _ =
        { Name = ResourceName.Empty
          DnsTtl = 30
          Status = Enabled
          RoutingMethod = RoutingMethod.Performance
          TrafficViewEnrollmentStatus = Disabled
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

    [<CustomOperation "monitor_config">]
    member __.MonitorConfig(state:TrafficManagerConfig, monitorConfig:MonitorConfig) = { state with MonitorConfig = monitorConfig }

    [<CustomOperation "dns_ttl">]
    member __.DnsTtl(state:TrafficManagerConfig, ttl) = { state with DnsTtl = ttl }

    [<CustomOperation "status">]
    member __.Status(state:TrafficManagerConfig, status) = { state with Status = status }

    [<CustomOperation "routing_method">]
    member __.RoutingMethod(state:TrafficManagerConfig, routingMethod) = { state with RoutingMethod = routingMethod }

    [<CustomOperation "traffic_view_enrollment_status">]
    member __.TrafficViewEnrollmentStatus(state:TrafficManagerConfig, status) = { state with TrafficViewEnrollmentStatus = status }


let trafficManager = TrafficManagerBuilder()
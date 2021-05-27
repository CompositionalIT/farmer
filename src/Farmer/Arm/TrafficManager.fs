[<AutoOpen>]
module Farmer.Arm.TrafficManager

open Farmer

let profiles = ResourceType ("Microsoft.Network/trafficManagerProfiles", "2018-04-01")
let endpoints = ResourceType ("Microsoft.Network/trafficManagerProfiles/azureEndpoints", "2018-04-01")
let externalEndpoints = ResourceType ("Microsoft.Network/trafficManagerProfiles/externalEndpoints", "2018-04-01")

type Endpoint =
    { Name : ResourceName
      Status: FeatureFlag
      Target : string
      Weight: int
      Priority: int
      Location: Location }
    interface IArmResource with
        member this.ResourceId = endpoints.resourceId (this.Name)
        member this.JsonModel =
            {| location = this.Location
               properties =
                {| endpointStatus = this.Status.ArmValue
                   weight = this.Weight
                   priority = this.Priority
                   endpointLocation = this.Location.ArmValue
                   targetResourceId = match this.Target with
                                      | ExternalDomain _ -> null
                                      | Website resource -> ArmExpression.resourceId(sites, resource).Eval()
                   target = this.Target.ArmValue |} |} :> _

type Profile =
    { Name : ResourceName
      DnsTtl: int
      Status : FeatureFlag
      RoutingMethod : RoutingMethod
      MonitorConfig : MonitorConfig
      TrafficViewEnrollmentStatus : FeatureFlag
      Endpoints : Endpoint list
      DependsOn : ResourceName list
      Tags: Map<string,string> }
    interface IArmResource with
        member this.ResourceId = profiles.resourceId (this.Name)
        member this.JsonModel =
            {| name = this.Name.Value
               location = "global"
               dependsOn = this.DependsOn |> List.map (fun r -> r.Value)
               tags = this.Tags
               properties =
                   {| profileStatus = this.Status.ArmValue
                      trafficRoutingMethod = this.RoutingMethod.ArmValue
                      trafficViewEnrollmentStatus = this.TrafficViewEnrollmentStatus.ArmValue
                      dependsOn = this.DependsOn |> List.map(fun p -> p.Value)
                      dnsConfig = {| relativeName = this.Name.Value
                                     ttl = this.DnsTtl |}
                      monitorConfig = {| protocol = this.MonitorConfig.Protocol.ArmValue
                                         port = this.MonitorConfig.Port
                                         path = this.MonitorConfig.Path
                                         intervalInSeconds = this.MonitorConfig.IntervalInSeconds
                                         toleratedNumberOfFailures = this.MonitorConfig.ToleratedNumberOfFailures
                                         timeoutInSeconds = this.MonitorConfig.TimeoutInSeconds |}

                      endpoints = this.Endpoints |> List.map (fun e -> (e:>IArmResource).JsonModel) |}
            |} :> _
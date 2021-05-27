[<AutoOpen>]
module Farmer.Arm.TrafficManager

open Farmer
open Farmer.TrafficManager

let profiles = ResourceType ("Microsoft.Network/trafficManagerProfiles", "2018-04-01")
let azureEndpoints = ResourceType ("Microsoft.Network/trafficManagerProfiles/azureEndpoints", "2018-04-01")
let externalEndpoints = ResourceType ("Microsoft.Network/trafficManagerProfiles/externalEndpoints", "2018-04-01")

type Endpoint =
    { Name : ResourceName
      Status: FeatureFlag
      Target : EndpointTarget
      Weight: int
      Priority: int
      Location: Location option }
    interface IArmResource with
        member this.ResourceId = azureEndpoints.resourceId (this.Name)
        member this.JsonModel =
            {| name = this.Name.Value
               properties =
                {| endpointStatus = this.Status.ArmValue
                   weight = this.Weight
                   priority = this.Priority
                   endpointLocation = this.Location |> Option.map (fun l -> l.ArmValue)
                                                    |> Option.defaultValue null
                   targetResourceId = match this.Target with
                                      | External _ -> null
                                      | Website resourceName -> sites.resourceId(resourceName).Eval()
                   target = this.Target.ArmValue |} |} :> _

type Profile =
    { Name : ResourceName
      DnsTtl: int
      Status : FeatureFlag
      RoutingMethod : RoutingMethod
      MonitorConfig : MonitorConfig
      TrafficViewEnrollmentStatus : FeatureFlag
      Endpoints : Endpoint list
      Dependencies : ResourceId Set
      Tags: Map<string,string> }
    interface IArmResource with
        member this.ResourceId = profiles.resourceId (this.Name)
        member this.JsonModel =
            {| profiles.Create(this.Name, Location.Global, this.Dependencies, tags = this.Tags) with
                   properties =
                       {| profileStatus = this.Status.ArmValue
                          trafficRoutingMethod = this.RoutingMethod.ArmValue
                          trafficViewEnrollmentStatus = this.TrafficViewEnrollmentStatus.ArmValue
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
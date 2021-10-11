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
      Weight: int option
      Priority: int option
      Location: Location option }
    member this.JsonModel =
        {| name = this.Name.Value
           ``type`` =
                match this.Target with
                | External _ -> externalEndpoints.Type
                | Website _ -> azureEndpoints.Type
           properties =
                {| endpointStatus = this.Status.ArmValue
                   weight = this.Weight |> Option.toNullable
                   priority = this.Priority |> Option.toNullable
                   endpointLocation =
                        this.Location
                        |> Option.map (fun l -> l.ArmValue)
                        |> Option.toObj
                   targetResourceId =
                        match this.Target with
                        | External _ -> null
                        | Website resourceName -> sites.resourceId(resourceName).Eval()
                   target = this.Target.ArmValue
                |}
        |}

type Profile =
    { Name : ResourceName
      DnsTtl: int<Seconds>
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
                          dnsConfig =
                            {| relativeName = this.Name.Value
                               ttl = this.DnsTtl |}
                          monitorConfig =
                            {| protocol = this.MonitorConfig.Protocol.ArmValue
                               port = this.MonitorConfig.Port
                               path = this.MonitorConfig.Path
                               intervalInSeconds = int this.MonitorConfig.IntervalInSeconds
                               toleratedNumberOfFailures = this.MonitorConfig.ToleratedNumberOfFailures
                               timeoutInSeconds = int this.MonitorConfig.TimeoutInSeconds |}
                          endpoints = this.Endpoints |> List.map (fun e -> e.JsonModel) |}
            |} :> _
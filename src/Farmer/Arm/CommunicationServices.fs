[<AutoOpen>]
module Farmer.Arm.CommunicationServices

open Farmer

let resource = ResourceType ("Microsoft.Communication/communicationServices", "2020-08-20-preview")

type Resource =
    { Name: ResourceName
      Location: Location
      DataLocation: DataLocation
      Tags: Map<string,string> }
    interface IArmResource with
        member this.ResourceId = resource.resourceId this.Name
        member this.JsonModel =
            {| resource.Create(this.Name, this.Location, tags = this.Tags) with
                properties = {| dataLocation = this.DataLocation.ArmValue |}
            |}

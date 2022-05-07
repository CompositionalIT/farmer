[<AutoOpen>]
module Farmer.Arm.Communication

open Farmer

let communicationServices = ResourceType ("Microsoft.Communication/communicationServices", "2020-08-20-preview")

type CommunicationService =
    { Name: ResourceName
      DataLocation: DataLocation
      Tags: Map<string,string> }
    interface IArmResource with
        member this.ResourceId = communicationServices.resourceId this.Name
        member this.JsonModel =
            {| communicationServices.Create(this.Name, Location.Global, tags = this.Tags) with
                properties = {| dataLocation = this.DataLocation.ArmValue |}
            |}
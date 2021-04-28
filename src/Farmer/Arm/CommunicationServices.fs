[<AutoOpen>]
module Farmer.Arm.CommunicationServices

open Farmer

let resource = ResourceType ("Microsoft.Communication/communicationServices", "2020-08-20-preview")

type Resource =
    { Name: ResourceName
      Location: Location
      DataLocation: Location
      Tags: Map<string,string> }
    interface IArmResource with
        member this.ResourceId = accounts.resourceId this.Name
        member this.JsonModel =
            {| accounts.Create(this.Name, this.Location, tags = this.Tags) with
                properties = {| dataLocation = this.DataLocation.ArmValue |}
            |} :> _

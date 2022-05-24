[<AutoOpen>]
module Farmer.Arm.LogicApps

open Farmer
open System.Text.Json

let workflows = ResourceType("Microsoft.Logic/workflows", "2019-05-01")

type LogicApp =
    { Name: ResourceName
      Location: Location
      Definition: JsonDocument
      Tags: Map<string, string> }

    interface IArmResource with
        member this.ResourceId = workflows.resourceId this.Name

        member this.JsonModel =
            {| workflows.Create(this.Name, this.Location, tags = this.Tags) with
                properties = this.Definition |}

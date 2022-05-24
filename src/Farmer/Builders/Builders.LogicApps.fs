[<AutoOpen>]
module Farmer.Builders.LogicApps

open Farmer
open Farmer.Arm.LogicApps
open System.IO
open System.Text.Json

type Definition =
    | FileDefinition of path: string
    | ValueDefinition of definition: string

type LogicAppConfig =
    { WorkflowName: ResourceName
      Definition: Definition
      Tags: Map<string, string> }

    member this.LogicAppWorkflowName = workflows.resourceId(this.WorkflowName).Name

    interface IBuilder with
        member this.ResourceId = workflows.resourceId this.WorkflowName

        member this.BuildResources location =
            [ { Name = this.LogicAppWorkflowName
                Location = location
                Definition =
                  match this.Definition with
                  | FileDefinition path ->
                      let fileContent = File.ReadAllText(path)
                      JsonDocument.Parse(fileContent)
                  | ValueDefinition value -> JsonDocument.Parse(value)
                Tags = this.Tags } ]

type LogicAppBuilder() =
    member _.Yield _ =
        { WorkflowName = ResourceName "logic-app-workflow"
          Definition = ValueDefinition """{"name":"logic-app-workflow"}"""
          Tags = Map.empty }

    [<CustomOperation "name">]
    member _.Name(state: LogicAppConfig, name) =
        { state with WorkflowName = ResourceName name }

    [<CustomOperation "definition">]
    member _.Definition(state: LogicAppConfig, definition: Definition) = { state with Definition = definition }

    interface ITaggable<LogicAppConfig> with
        member _.Add state tags =
            { state with Tags = state.Tags |> Map.merge tags }

let logicApp = LogicAppBuilder()

﻿[<AutoOpen>]
module Farmer.Builders.ServicePlan

open Farmer
open Farmer.WebApp
open Arm.Web

type ServicePlanConfig =
    { Name : ResourceName
      Sku : Sku
      WorkerSize : WorkerSize
      WorkerCount : int
      OperatingSystem : OS
      Tags : Map<string,string> }
    interface IBuilder with
        member this.ResourceId = serverFarms.resourceId this.Name
        member this.BuildResources location = [
          { Name = this.Name
            Location = location
            Sku = this.Sku
            WorkerSize = this.WorkerSize
            OperatingSystem = this.OperatingSystem
            WorkerCount = this.WorkerCount
            Tags = this.Tags }
        ]

type ServicePlanBuilder() =
    member __.Yield _ : ServicePlanConfig=
        { Name = ResourceName.Empty
          Sku = Free
          WorkerSize = Small
          WorkerCount = 1
          OperatingSystem = Windows
          Tags = Map.empty }
    [<CustomOperation "name">]
    /// Sets the name of the Server Farm.
    member __.Name(state:ServicePlanConfig, name) = { state with Name = ResourceName name }
    /// Sets the sku of the service plan.
    [<CustomOperation "sku">]
    member __.Sku(state:ServicePlanConfig, sku) = { state with Sku = sku }
    /// Sets the size of the service plan worker.
    [<CustomOperation "worker_size">]
    member __.WorkerSize(state:ServicePlanConfig, workerSize) = { state with WorkerSize = workerSize }
    /// Sets the number of instances on the service plan.
    [<CustomOperation "number_of_workers">]
    member __.NumberOfWorkers(state:ServicePlanConfig, workerCount) = { state with WorkerCount = workerCount }
    [<CustomOperation "operating_system">]
    /// Sets the operating system
    member __.OperatingSystem(state:ServicePlanConfig, os) = { state with OperatingSystem = os }
    [<CustomOperation "serverless">]
    /// Configures this server farm to host serverless functions, not web apps.
    member __.Serverless(state:ServicePlanConfig) = { state with Sku = Dynamic; WorkerSize = Serverless }
    [<CustomOperation "add_tags">]
    member _.Tags(state:ServicePlanConfig, pairs) =
        { state with
            Tags = pairs |> List.fold (fun map (key,value) -> Map.add key value map) state.Tags }
    [<CustomOperation "add_tag">]
    member this.Tag(state:ServicePlanConfig, key, value) = this.Tags(state, [ (key,value) ])
let servicePlan = ServicePlanBuilder()
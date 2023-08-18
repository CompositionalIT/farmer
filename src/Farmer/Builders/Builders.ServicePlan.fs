[<AutoOpen>]
module Farmer.Builders.ServicePlan

open Farmer
open Farmer.WebApp
open Arm.Web

type ServicePlanConfig = {
    Name: ResourceName
    Sku: Sku
    WorkerSize: WorkerSize
    WorkerCount: int
    MaximumElasticWorkerCount: int option
    OperatingSystem: OS
    ZoneRedundant: FeatureFlag option
    Tags: Map<string, string>
} with

    interface IBuilder with
        member this.ResourceId = serverFarms.resourceId this.Name

        member this.BuildResources location = [
            {
                Name = this.Name
                Location = location
                Sku = this.Sku
                WorkerSize = this.WorkerSize
                OperatingSystem = this.OperatingSystem
                WorkerCount = this.WorkerCount
                MaximumElasticWorkerCount = this.MaximumElasticWorkerCount
                ZoneRedundant = this.ZoneRedundant
                Tags = this.Tags
            }
        ]

type ServicePlanBuilder() =
    member _.Yield _ : ServicePlanConfig = {
        Name = ResourceName.Empty
        Sku = Free
        WorkerSize = Small
        WorkerCount = 1
        MaximumElasticWorkerCount = None
        OperatingSystem = Windows
        ZoneRedundant = None
        Tags = Map.empty
    }

    [<CustomOperation "name">]
    /// Sets the name of the Server Farm.
    member _.Name(state: ServicePlanConfig, name) = { state with Name = ResourceName name }

    /// Sets the sku of the service plan.
    [<CustomOperation "sku">]
    member _.Sku(state: ServicePlanConfig, sku) = { state with Sku = sku }

    /// Sets the size of the service plan worker.
    [<CustomOperation "worker_size">]
    member _.WorkerSize(state: ServicePlanConfig, workerSize) = { state with WorkerSize = workerSize }

    /// Sets the number of instances on the service plan.
    [<CustomOperation "number_of_workers">]
    member _.NumberOfWorkers(state: ServicePlanConfig, workerCount) = { state with WorkerCount = workerCount }

    /// Sets the maximum number of elastic workers
    [<CustomOperation "max_elastic_workers">]
    member _.MaximumElasticWorkerCount(state: ServicePlanConfig, maxElasticWorkerCount) = {
        state with
            MaximumElasticWorkerCount = Some maxElasticWorkerCount
    }

    [<CustomOperation "operating_system">]
    /// Sets the operating system
    member _.OperatingSystem(state: ServicePlanConfig, os) = { state with OperatingSystem = os }

    [<CustomOperation "serverless">]
    /// Configures this server farm to host serverless functions, not web apps.
    member _.Serverless(state: ServicePlanConfig) = {
        state with
            Sku = Dynamic
            WorkerSize = Serverless
    }

    [<CustomOperation "zone_redundant">]
    member _.ZoneRedundant(state: ServicePlanConfig, flag: FeatureFlag) = { state with ZoneRedundant = Some flag }

    interface ITaggable<ServicePlanConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

let servicePlan = ServicePlanBuilder()

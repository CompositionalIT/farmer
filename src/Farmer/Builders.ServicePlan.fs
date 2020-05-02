[<AutoOpen>]
module Farmer.Resources.ServicePlan

open Farmer
open Arm.Web

type WorkerSize = Small | Medium | Large | Serverless
type WebAppSku = Shared | Free | Basic of string | Standard of string | Premium of string | PremiumV2 of string | Isolated of string | Functions

module WebAppSkus =
    let D1 = Shared
    let F1 = Free
    let B1 = Basic "B1"
    let B2 = Basic "B2"
    let B3 = Basic "B3"
    let S1 = Standard "S1"
    let S2 = Standard "S2"
    let S3 = Standard "S3"
    let P1 = Premium "P1"
    let P2 = Premium "P2"
    let P3 = Premium "P3"
    let P1V2 = PremiumV2 "P1V2"
    let P2V2 = PremiumV2 "P2V2"
    let P3V2 = PremiumV2 "P3V2"
    let I1 = Isolated "I1"
    let I2 = Isolated "I2"
    let I3 = Isolated "I3"
    let Y1 = Isolated "Y1"

type ServicePlanConfig =
    { Name : ResourceName
      Sku : WebAppSku
      WorkerSize : WorkerSize
      WorkerCount : int
      OperatingSystem : OS }
    interface IResourceBuilder with
        member this.BuildResources location _ = [
            NewResource
              { Location = location
                Name = this.Name
                Sku =
                  match this.Sku with
                  | Free ->
                      "F1"
                  | Shared ->
                      "D1"
                  | Basic sku
                  | Standard sku
                  | Premium sku
                  | PremiumV2 sku
                  | Isolated sku ->
                      sku
                  | Functions ->
                      "Y1"
                WorkerSize =
                  match this.WorkerSize with
                  | Small -> "0"
                  | Medium -> "1"
                  | Large -> "2"
                  | Serverless -> "Y1"
                IsDynamic =
                  match this.Sku, this.WorkerSize with
                  | Functions, Serverless -> true
                  | _ -> false
                Kind =
                  match this.OperatingSystem with
                  | Linux -> Some "linux"
                  | _ -> None
                Tier =
                  match this.Sku with
                  | Free -> "Free"
                  | Shared -> "Shared"
                  | Basic _ -> "Basic"
                  | Standard _ -> "Standard"
                  | Premium _ -> "Premium"
                  | PremiumV2 _ -> "PremiumV2"
                  | Isolated _ -> "Isolated"
                  | Functions -> "Dynamic"
                IsLinux =
                  match this.OperatingSystem with
                  | Linux -> true
                  | Windows -> false
                WorkerCount =
                  this.WorkerCount }
        ]


type ServicePlanBuilder() =
    member __.Yield _ : ServicePlanConfig=
        { Name = ResourceName.Empty
          Sku = Free
          WorkerSize = Small
          WorkerCount = 1
          OperatingSystem = Windows }
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
    member __.Serverless(state:ServicePlanConfig) = { state with Sku = Functions; WorkerSize = Serverless }

let servicePlan = ServicePlanBuilder()
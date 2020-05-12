[<AutoOpen>]
module Farmer.Builders.ServicePlan

open Farmer
open Farmer.Arm.Web

type WorkerSize = Small | Medium | Large | Serverless
[<RequireQualifiedAccess>]
type WebAppSku = Shared | Free | Basic of string | Standard of string | Premium of string | PremiumV2 of string | Isolated of string | Functions

[<RequireQualifiedAccess>]
module WebAppSkus =
    let D1 = WebAppSku.Shared
    let F1 = WebAppSku.Free
    let B1 = WebAppSku.Basic "B1"
    let B2 = WebAppSku.Basic "B2"
    let B3 = WebAppSku.Basic "B3"
    let S1 = WebAppSku.Standard "S1"
    let S2 = WebAppSku.Standard "S2"
    let S3 = WebAppSku.Standard "S3"
    let P1 = WebAppSku.Premium "P1"
    let P2 = WebAppSku.Premium "P2"
    let P3 = WebAppSku.Premium "P3"
    let P1V2 = WebAppSku.PremiumV2 "P1V2"
    let P2V2 = WebAppSku.PremiumV2 "P2V2"
    let P3V2 = WebAppSku.PremiumV2 "P3V2"
    let I1 = WebAppSku.Isolated "I1"
    let I2 = WebAppSku.Isolated "I2"
    let I3 = WebAppSku.Isolated "I3"
    let Y1 = WebAppSku.Isolated "Y1"

type ServicePlanConfig =
    { Name : ResourceName
      Sku : WebAppSku
      WorkerSize : WorkerSize
      WorkerCount : int
      OperatingSystem : OS }
    interface IBuilder with
        member this.BuildResources location _ = [
          { Location = location
            Name = this.Name
            Sku =
              match this.Sku with
              | WebAppSku.Free ->
                  "F1"
              | WebAppSku.Shared ->
                  "D1"
              | WebAppSku.Basic sku
              | WebAppSku.Standard sku
              | WebAppSku.Premium sku
              | WebAppSku.PremiumV2 sku
              | WebAppSku.Isolated sku ->
                  sku
              | WebAppSku.Functions ->
                  "Y1"
            WorkerSize =
              match this.WorkerSize with
              | Small -> "0"
              | Medium -> "1"
              | Large -> "2"
              | Serverless -> "Y1"
            IsDynamic =
              match this.Sku, this.WorkerSize with
              | WebAppSku.Functions, Serverless -> true
              | _ -> false
            Kind =
              match this.OperatingSystem with
              | Linux -> Some "linux"
              | _ -> None
            Tier =
              match this.Sku with
              | WebAppSku.Free -> "Free"
              | WebAppSku.Shared -> "Shared"
              | WebAppSku.Basic _ -> "Basic"
              | WebAppSku.Standard _ -> "Standard"
              | WebAppSku.Premium _ -> "Premium"
              | WebAppSku.PremiumV2 _ -> "PremiumV2"
              | WebAppSku.Isolated _ -> "Isolated"
              | WebAppSku.Functions -> "Dynamic"
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
          Sku = WebAppSku.Free
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
    member __.Serverless(state:ServicePlanConfig) = { state with Sku = WebAppSku.Functions; WorkerSize = Serverless }

let servicePlan = ServicePlanBuilder()
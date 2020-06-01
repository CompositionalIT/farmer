[<AutoOpen>]
module Farmer.Builders.SignalR

open System.Text.RegularExpressions
open Farmer.CoreTypes
open Farmer.SignalR
open Farmer.Arm.SignalR
open Farmer.Helpers

type SignalRConfig =
    { Name : ResourceName
      Sku : Sku
      Capacity : int option
      AllowedOrigins : string list }
    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources location _ = [
            { Name = this.Name
              Location = location
              Sku = this.Sku
              Capacity = this.Capacity
              AllowedOrigins = this.AllowedOrigins }
        ]
            
type SignalRBuilder() =
    member this.Yield _ =
        { Name = ResourceName.Empty
          Sku = Free
          Capacity = None
          AllowedOrigins = [] }
    member this.Run(state:SignalRConfig) =
        { state with Name = state.Name |> sanitiseSignalR |> ResourceName }
        
    /// Sets the name of the Azure SignalR instance.
    [<CustomOperation("name")>]
    member this.Name(state:SignalRConfig, name) = { state with Name = name }
    member this.Name(state:SignalRConfig, name) = this.Name(state, ResourceName name)
    /// Sets the SKU of the Azure SignalR instance.
    [<CustomOperation("sku")>]
    member this.Sku(state:SignalRConfig, sku) = { state with Sku = sku }
    /// Sets the capacity of the Azure SignalR instance.
    [<CustomOperation("capacity")>]
    member this.Capacity(state:SignalRConfig, capacity) = { state with Capacity = Some capacity }
    /// Sets the allowed origins of the Azure SignalR instance.
    [<CustomOperation("allowed_origins")>]
    member this.AllowedOrigins(state:SignalRConfig, allowedOrigins) = { state with AllowedOrigins = allowedOrigins}

let signalR = SignalRBuilder()
    

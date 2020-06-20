[<AutoOpen>]
module Farmer.Builders.ResourceId

open Farmer

type ResourceIdBuilder () =
    member __.Yield _ =
      { Name = ResourceName.Empty
        Type = ""
        Group = None
        SubscriptionId = None }
    /// Sets the name of the resource
    [<CustomOperation "name">]
    member __.Name(state:ResourceId, name) = { state with Name = ResourceName name }
    /// Sets the type of the resource
    [<CustomOperation "resource_type">]
    member __.ResourceType(state:ResourceId, t) = { state with Type = t }
    /// Sets the group of the resource
    [<CustomOperation "group">]
    member __.Group(state:ResourceId, group) = { state with Group = Some group }
    /// Sets the subscription of the resource
    [<CustomOperation "subscription_id">]
    member __.SubscriptionId(state:ResourceId, sub) = { state with SubscriptionId = Some sub }
let resourceId = ResourceIdBuilder()

type ResourceTypes =
    static member Connection = "Microsoft.Network/connections"
    static member VNetGateway = "Microsoft.Network/virtualNetworkGateways"

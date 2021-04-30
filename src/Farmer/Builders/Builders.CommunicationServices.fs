[<AutoOpen>]
module Farmer.Builders.CommunicationServices

open Farmer
open Farmer.Arm.CommunicationServices

type CommunicationServices =
    /// Gets an ARM Expression key for any Bing Search instance.
    static member getKey (resourceId: ResourceId) =
        ArmExpression.create($"listKeys({resourceId.ArmExpression.Value}, '{resource.ApiVersion}').key1", resourceId)
    static member getKey (name: ResourceName) = CommunicationServices.getKey (resource.resourceId name)

type CommunicationServicesConfig =
    { Name: ResourceName
      Tags: Map<string,string>
      DataLocation: DataLocation }
    /// Gets an ARM expression to the key of this Bing Search instance.
    member this.Key = CommunicationServices.getKey (resource.resourceId this.Name)
    interface IBuilder with
        member this.ResourceId = resource.resourceId this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              Tags = this.Tags
              DataLocation = this.DataLocation }
        ]

type CommunicationServicesBuilder () =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Tags = Map.empty
          // We default to UnitedStates because all of the features are available there.
          DataLocation = DataLocation.UnitedStates }
    [<CustomOperation "name">]
    member _.Name (state: CommunicationServicesConfig, name) = { state with Name = ResourceName name }
    [<CustomOperation "data_location">]
    member _.Sku (state: CommunicationServicesConfig, dataLocation) = { state with DataLocation = dataLocation }
    interface ITaggable<CommunicationServicesConfig> with member _.Add state tags = { state with Tags = state.Tags |> Map.merge tags }

let communicationServices = CommunicationServicesBuilder()
[<AutoOpen>]
module Farmer.Builders.CommunicationServices

open Farmer
open Farmer.Arm.Communication

type CommunicationServices =
    /// Gets an ARM Expression key for any Communication Services instance.
    static member getKey(resourceId: ResourceId) =
        ArmExpression.create (
            $"listKeys({resourceId.ArmExpression.Value}, '{communicationServices.ApiVersion}').primaryKey",
            resourceId
        )

    static member getKey(name: ResourceName) =
        CommunicationServices.getKey (communicationServices.resourceId name)

    static member getConnectionString(resourceId: ResourceId) =
        ArmExpression.create (
            $"listKeys({resourceId.ArmExpression.Value}, '{communicationServices.ApiVersion}').primaryConnectionString",
            resourceId
        )

    static member getConnectionString(name: ResourceName) =
        CommunicationServices.getConnectionString (communicationServices.resourceId name)

type CommunicationServicesConfig =
    { Name: ResourceName
      Tags: Map<string, string>
      DataLocation: DataLocation }
    /// Gets an ARM expression to the key of this Communication Services instance.
    member this.Key =
        CommunicationServices.getKey (communicationServices.resourceId this.Name)

    /// Gets an ARM expression to the connection string of this Communication Services instance.
    member this.ConnectionString =
        CommunicationServices.getConnectionString (communicationServices.resourceId this.Name)

    interface IBuilder with
        member this.ResourceId = communicationServices.resourceId this.Name

        member this.BuildResources _ =
            [ { CommunicationService.Name = this.Name
                Tags = this.Tags
                DataLocation = this.DataLocation } ]

type CommunicationServicesBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Tags = Map.empty
          // We default to UnitedStates because all of the features are available there.
          DataLocation = DataLocation.UnitedStates }

    [<CustomOperation "name">]
    member _.Name(state: CommunicationServicesConfig, name) = { state with Name = ResourceName name }

    [<CustomOperation "data_location">]
    member _.DataLocation(state: CommunicationServicesConfig, dataLocation) =
        { state with DataLocation = dataLocation }

    interface ITaggable<CommunicationServicesConfig> with
        member _.Add state tags =
            { state with Tags = state.Tags |> Map.merge tags }

let communicationService = CommunicationServicesBuilder()

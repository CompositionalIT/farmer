[<AutoOpen>]
module Farmer.Builders.ServiceEndpointPolicy

open Farmer
open Farmer.Arm
open Farmer.Network

/// A single service endpoint policy definition.
type ServiceEndpointPolicyDefinitionConfig = {
    Name: ResourceName
    Description: string option
    Service: EndpointServiceType option
    ServiceResources: LinkedResource list
}

/// A service endpoint policy resource.
type ServiceEndpointPolicyConfig = {
    Name: ResourceName
    Definitions: ServiceEndpointPolicyDefinitionConfig list
    Dependencies: ResourceId Set
    Tags: Map<string, string>
} with

    interface IBuilder with
        member this.ResourceId = serviceEndpointPolicies.resourceId this.Name

        member this.BuildResources location =
            let definitions =
                this.Definitions
                |> List.map (fun definition ->
                    let service =
                        match definition.Service with
                        | Some service -> service
                        | None -> raiseFarmer "You must set a service on every service endpoint policy definition."

                    {
                        ServiceEndpointPolicyDefinition.Name = definition.Name
                        Description = definition.Description
                        Service = service
                        ServiceResources = definition.ServiceResources
                    })

            [
                {
                    ServiceEndpointPolicy.Name = this.Name
                    Location = location
                    ServiceEndpointPolicyDefinitions = definitions
                    Dependencies = this.Dependencies
                    Tags = this.Tags
                }
            ]

type ServiceEndpointPolicyDefinitionBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        Description = None
        Service = None
        ServiceResources = []
    }

    [<CustomOperation "name">]
    member _.Name(state: ServiceEndpointPolicyDefinitionConfig, name: string) = { state with Name = ResourceName name }

    [<CustomOperation "description">]
    member _.Description(state: ServiceEndpointPolicyDefinitionConfig, description: string) = {
        state with
            Description = Some description
    }

    [<CustomOperation "service">]
    member _.Service(state: ServiceEndpointPolicyDefinitionConfig, service: EndpointServiceType) = {
        state with
            Service = Some service
    }

    member _.Service(state: ServiceEndpointPolicyDefinitionConfig, service: string) = {
        state with
            Service = Some(EndpointServiceType service)
    }

    /// Adds Farmer-managed service resources to this definition.
    [<CustomOperation "add_service_resources">]
    member _.AddServiceResources<'T when 'T :> IBuilder>
        (state: ServiceEndpointPolicyDefinitionConfig, resources: 'T list)
        =
        {
            state with
                ServiceResources =
                    state.ServiceResources
                    @ (resources |> List.map (fun resource -> Managed resource.ResourceId))
        }

    member _.AddServiceResources(state: ServiceEndpointPolicyDefinitionConfig, resources: ResourceId list) = {
        state with
            ServiceResources = state.ServiceResources @ (resources |> List.map Managed)
    }

    member _.AddServiceResources(state: ServiceEndpointPolicyDefinitionConfig, resources: LinkedResource list) = {
        state with
            ServiceResources = state.ServiceResources @ resources
    }

    /// Adds externally-managed service resources to this definition.
    [<CustomOperation "link_to_service_resources">]
    member _.LinkToServiceResources<'T when 'T :> IBuilder>
        (state: ServiceEndpointPolicyDefinitionConfig, resources: 'T list)
        =
        {
            state with
                ServiceResources =
                    state.ServiceResources
                    @ (resources |> List.map (fun resource -> Unmanaged resource.ResourceId))
        }

    member _.LinkToServiceResources(state: ServiceEndpointPolicyDefinitionConfig, resources: ResourceId list) = {
        state with
            ServiceResources = state.ServiceResources @ (resources |> List.map Unmanaged)
    }

    member _.LinkToServiceResources(state: ServiceEndpointPolicyDefinitionConfig, resources: LinkedResource list) = {
        state with
            ServiceResources =
                state.ServiceResources
                @ (resources
                   |> List.map (fun resource ->
                       match resource with
                       | Managed resourceId
                       | Unmanaged resourceId -> Unmanaged resourceId))
    }

let serviceEndpointPolicyDefinition = ServiceEndpointPolicyDefinitionBuilder()

type ServiceEndpointPolicyBuilder() =
    member _.Yield _ = {
        Name = ResourceName.Empty
        Definitions = []
        Dependencies = Set.empty
        Tags = Map.empty
    }

    [<CustomOperation "name">]
    member _.Name(state: ServiceEndpointPolicyConfig, name: string) = { state with Name = ResourceName name }

    [<CustomOperation "add_definitions">]
    member _.AddDefinitions
        (state: ServiceEndpointPolicyConfig, definitions: ServiceEndpointPolicyDefinitionConfig list)
        =
        {
            state with
                Definitions = state.Definitions @ definitions
        }

    interface IDependable<ServiceEndpointPolicyConfig> with
        member _.Add state newDeps = {
            state with
                Dependencies = state.Dependencies + newDeps
        }

    interface ITaggable<ServiceEndpointPolicyConfig> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

let serviceEndpointPolicy = ServiceEndpointPolicyBuilder()
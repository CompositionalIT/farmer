[<AutoOpen>]
module Farmer.Builders.SignalR

open Farmer
open Farmer.Arm.SignalRService
open Farmer.Builders
open Farmer.Helpers
open Farmer.SignalR

type UpstreamTemplateConfig =
    {
        UrlTemplate: string
        HubPattern: string
        CategoryPattern: string
        EventPattern: string
    }

type SignalRConfig =
    {
        Name: ResourceName
        Sku: Sku
        Capacity: int option
        AllowedOrigins: string list
        ServiceMode: ServiceMode
        Tags: Map<string, string>
        UpstreamTemplateConfigs: UpstreamTemplateConfig list
    }

    member this.ResourceId = signalR.resourceId this.Name

    interface IBuilder with
        member this.ResourceId = this.ResourceId

        member this.BuildResources location =
            [
                {
                    Name = this.Name
                    Location = location
                    Sku = this.Sku
                    Capacity = this.Capacity
                    AllowedOrigins = this.AllowedOrigins
                    ServiceMode = this.ServiceMode
                    Tags = this.Tags
                    UpstreamTemplateConfigs = 
                        this.UpstreamTemplateConfigs
                        |> List.map(fun config ->
                            UpstreamTemplate {
                                CategoryPattern = config.CategoryPattern
                                eventPattern = config.EventPattern
                                hubPattern = config.HubPattern
                                urlTemplate = config.UrlTemplate
                            })    
                }                            
            ]

    member private this.GetKeyExpr field =
        let expr =
            $"listKeys(resourceId('Microsoft.SignalRService/SignalR', '{this.Name.Value}'), providers('Microsoft.SignalRService', 'SignalR').apiVersions[0]).{field}"

        ArmExpression.create (expr, this.ResourceId)

    member this.Key = this.GetKeyExpr "primaryKey"
    member this.ConnectionString = this.GetKeyExpr "primaryConnectionString"

    member this.ConnectionStringInResourceGroup rg =
        let expr =
            $"listKeys(resourceId('{rg}', 'Microsoft.SignalRService/SignalR', '{this.Name.Value}'), providers('Microsoft.SignalRService', 'SignalR').apiVersions[0]).primaryConnectionString"

        ArmExpression.create (expr, this.ResourceId)

type SignalRBuilder() =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            Sku = Free
            Capacity = None
            AllowedOrigins = []
            ServiceMode = Default
            Tags = Map.empty
            UpstreamTemplateConfigs = []
        }

    member _.Run(state: SignalRConfig) =
        { state with
            Name = state.Name |> sanitiseSignalR |> ResourceName
        }

    /// Sets the name of the Azure SignalR instance.
    [<CustomOperation("name")>]
    member _.Name(state: SignalRConfig, name) = { state with Name = name }

    member this.Name(state: SignalRConfig, name) = this.Name(state, ResourceName name)

    /// Sets the SKU of the Azure SignalR instance.
    [<CustomOperation("sku")>]
    member _.Sku(state: SignalRConfig, sku) = { state with Sku = sku }

    /// Sets the capacity of the Azure SignalR instance.
    [<CustomOperation("capacity")>]
    member _.Capacity(state: SignalRConfig, capacity) = { state with Capacity = Some capacity }

    /// Sets the allowed origins of the Azure SignalR instance.
    [<CustomOperation("allowed_origins")>]
    member _.AllowedOrigins(state: SignalRConfig, allowedOrigins) =
        { state with
            AllowedOrigins = allowedOrigins
        }

    /// Sets the service mode of the Azure SignalR instance.
    [<CustomOperation("service_mode")>]
    member _.ServiceMode(state: SignalRConfig, serviceMode) =
        { state with ServiceMode = serviceMode }

    /// Sets any upstream settings on the Azure SignalR instance
    [<CustomOperation("upstream_configs")>]
    member _.UpstreamConfigs(state: SignalRConfig, upstreamTemplateConfigs: UpstreamTemplateConfig list) =
        { state with UpstreamTemplateConfigs = state.UpstreamTemplateConfigs @ upstreamTemplateConfigs}

    interface ITaggable<SignalRConfig> with
        member _.Add state tags =
            { state with
                Tags = state.Tags |> Map.merge tags
            }

type UpstreamTemplateBuilder() =
    member _.Yield _ : UpstreamTemplateConfig =
        {
            UrlTemplate = ""
            HubPattern = ""
            CategoryPattern = ""
            EventPattern = ""
        }

    [<CustomOperation "url_template">]
    member _.UrlTemplate(state: UpstreamTemplateConfig, urlTemplate: string) =
        { state with UrlTemplate = urlTemplate }

    [<CustomOperation "hub_pattern">]
    member _.HubPattern(state: UpstreamTemplateConfig, hubPattern: string) =
        { state with HubPattern = hubPattern }

    [<CustomOperation "category_pattern">]
    member _.CategoryPattern(state: UpstreamTemplateConfig, categoryPattern: string) =
        { state with CategoryPattern = categoryPattern }

    [<CustomOperation "event_pattern">]
    member _.EventPattern(state: UpstreamTemplateConfig, eventPattern: string) =
        { state with EventPattern = eventPattern }

let signalR = SignalRBuilder()

let upstreamTemplateConfig = UpstreamTemplateBuilder()

[<AutoOpen>]
module Farmer.Arm.SignalRService

open Farmer
open Farmer.SignalR

let signalR = ResourceType("Microsoft.SignalRService/signalR", "2022-02-01")

type SignalRFilterPattern =
    | Any
    | List of string list
    member this.ArmValue =
        match this with
        | Any -> "*"
        | List values -> String.concat "," values

type UpstreamTemplate =
    {
        UrlTemplate: string
        HubPattern: SignalRFilterPattern
        CategoryPattern: SignalRFilterPattern
        EventPattern: SignalRFilterPattern        
    }

type SignalR =
    {
        Name: ResourceName
        Location: Location
        Sku: Sku
        Capacity: int option
        AllowedOrigins: string list
        ServiceMode: ServiceMode
        Tags: Map<string, string>
        UpstreamTemplates: UpstreamTemplate list
    }

    interface IArmResource with
        member this.ResourceId = signalR.resourceId this.Name

        member this.JsonModel =
            {| signalR.Create(this.Name, this.Location, tags = this.Tags) with
                sku =
                    {|
                        name =
                            match this.Sku with
                            | Free -> "Free_F1"
                            | Standard -> "Standard_S1"
                        capacity =
                            match this.Capacity with
                            | Some c -> c.ToString()
                            | None -> null
                    |}
                properties =
                    {|
                        cors =
                            match this.AllowedOrigins with
                            | [] -> null
                            | aos -> box {| allowedOrigins = aos |}
                        features =
                            [
                                {|
                                    flag = "ServiceMode"
                                    value = this.ServiceMode.ToString()
                                |}
                            ]
                        upstream = 
                            {| 
                                templates = 
                                    this.UpstreamTemplates
                                    |> List.map(fun config ->
                                        {|
                                            urlTemplate = config.UrlTemplate
                                            hubPattern = config.HubPattern.ArmValue
                                            categoryPattern = config.CategoryPattern.ArmValue
                                            eventPattern = config.EventPattern.ArmValue
                                        |})    
                            |}
                    |}
            |}

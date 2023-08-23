/// THIS IS A STUB
/// https://learn.microsoft.com/en-us/azure/templates/microsoft.automation/automationaccounts/webhooks
[<AutoOpen>]
module Farmer.Arm.Webhooks

open Farmer
open System

let webhooks =
    Farmer.ResourceType("Microsoft.Automation/automationAccounts/webhooks", "2015-10-31")

type Webhook =
    {
        Name: Farmer.ResourceName
    }

    interface Farmer.IArmResource with

        member this.ResourceId = webhooks.resourceId this.Name

        member this.JsonModel = "{}"

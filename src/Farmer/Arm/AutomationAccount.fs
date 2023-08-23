/// THIS IS A STUB
/// https://learn.microsoft.com/en-us/azure/templates/microsoft.automation/automationaccounts
[<AutoOpen>]
module Farmer.Arm.AutomationAccounts

open Farmer
open System

let automationAccounts =
    Farmer.ResourceType("Microsoft.Automation/automationAccounts", "2022-08-08")

type AutomationAccount =
    {
        Name: Farmer.ResourceName
    }

    interface Farmer.IArmResource with

        member this.ResourceId = automationAccounts.resourceId this.Name

        member this.JsonModel = "{}"

/// https://learn.microsoft.com/en-us/azure/templates/microsoft.insights/actiongroups
[<AutoOpen>]
module Farmer.Arm.ActionGroups

open Farmer
open System
open Farmer

let actionGroups =
    Farmer.ResourceType("microsoft.insights/actionGroups", "2022-06-01")

type ArmRoleReceiver =
    {
        /// The name of the arm role receiver. Names must be unique across all receivers within an action group.
        Name: string
        /// The arm role id.
        RoleId: Guid
        /// Indicates whether to use common alert schema.
        UseCommonAlertSchema: bool
    }

    static member Create(name, (armRole: RoleId), ?useCommonAlertSchema) =
        {
            Name = name
            RoleId = armRole.Id
            UseCommonAlertSchema = useCommonAlertSchema |> Option.defaultValue true
        }

type AutomationRunbookReceiver =
    {
        /// The Azure automation account Id which holds this runbook and authenticate to Azure resource.
        AutomationAccountId: ResourceId
        /// Indicates whether this instance is global runbook.
        IsGlobalRunbook: bool
        /// Indicates name of the webhook.
        Name: string
        /// The name for this runbook.
        RunbookName: string
        /// The URI where webhooks should be sent.
        ServiceUri: string
        /// Indicates whether to use common alert schema.
        UseCommonAlertSchema: bool
        /// The resource id for webhook linked to this runbook.
        WebhookResourceId: string
    }

    static member Create
        (
            automationAccountId,
            isGlobalRunbook,
            runbookName,
            webhookResourceId,
            ?name,
            ?serviceUri,
            ?useCommonAlertSchema
        ) =
        {
            AutomationAccountId = automationAccountId
            IsGlobalRunbook = isGlobalRunbook
            RunbookName = runbookName
            WebhookResourceId = webhookResourceId
            ServiceUri = serviceUri |> Option.defaultValue ""
            Name = name |> Option.defaultValue ""
            UseCommonAlertSchema = useCommonAlertSchema |> Option.defaultValue true
        }

type AzureAppPushReceiver =
    {
        /// The email address registered for the Azure mobile app.
        EmailAddress: string
        /// The name of the Azure mobile app push receiver. Names must be unique across all receivers within an action group.
        Name: string
    }

    static member Create(email, name) = { Name = name; EmailAddress = email }

type AzureFunctionReceiver =
    {
        /// The azure resource id of the function app.
        FunctionAppResourceId: ResourceId
        /// The function name in the function app.
        FunctionName: string
        /// The http trigger url where http request sent to.
        HttpTriggerUrl: string
        /// The name of the azure function receiver. Names must be unique across all receivers within an action group.
        Name: string
        /// Indicates whether to use common alert schema.
        UseCommonAlertSchema: bool
    }

    static member Create(functionAppResourceId, functionName, httpTriggerUrl, name, ?useCommonAlertSchema) =
        {
            FunctionAppResourceId = functionAppResourceId
            FunctionName = functionName
            HttpTriggerUrl = httpTriggerUrl
            Name = name
            UseCommonAlertSchema = useCommonAlertSchema |> Option.defaultValue true
        }

type EmailReceiver =
    {
        /// The email address of this receiver.
        Name: string
        /// The name of the email receiver. Names must be unique across all receivers within an action group.
        EmailAddress: string
        /// Indicates whether to use common alert schema.
        UseCommonAlertSchema: bool
    }

    static member Create(name, email, ?useCommonAlertSchema) =
        {
            Name = name
            EmailAddress = email
            UseCommonAlertSchema = useCommonAlertSchema |> Option.defaultValue true
        }

type EventHubReceiver =
    {
        /// The name of the specific Event Hub queue.
        EventHubName: string
        /// The Event Hub namespace.
        EventHubNameSpace: string
        /// The name of the Event hub receiver. Names must be unique across all receivers within an action group.
        Name: string
        /// The Id for the subscription containing this event hub.
        SubscriptionId: string
        /// The tenant Id for the subscription containing this event hub.
        TenantId: string
        /// Indicates whether to use common alert schema.
        UseCommonAlertSchema: bool
    }

    static member Create(eventHubName, eventHubNameSpace, subscriptionId, name, ?tenantId, ?useCommonAlertSchema) =
        {
            EventHubName = eventHubName
            EventHubNameSpace = eventHubNameSpace
            Name = name
            SubscriptionId = subscriptionId
            TenantId = tenantId |> Option.defaultValue ""
            UseCommonAlertSchema = useCommonAlertSchema |> Option.defaultValue true
        }

type ItsmReceiver =
    {
        /// Unique identification of ITSM connection among multiple defined in above workspace.
        ConnectionId: string
        /// The name of the Itsm receiver. Names must be unique across all receivers within an action group.
        Name: string
        /// Region in which workspace resides. Supported values:
        /// 'centralindia', 'japaneast', 'southeastasia', 'australiasoutheast', 'uksouth', 'westcentralus', 'canadacentral', 'eastus', 'westeurope'
        Region: string
        /// JSON blob for the configurations of the ITSM action. CreateMultipleWorkItems option will be part of this blob as well.
        TicketConfiguration: string
        /// OMS LA instance identifier.
        WorkspaceId: string
    }

    static member Create(connectionid, name, region, ticketConfiguration, workspaceId) =
        {
            ConnectionId = connectionid
            Name = name
            Region = region
            TicketConfiguration = ticketConfiguration
            WorkspaceId = workspaceId
        }

type LogicAppReceiver =
    {
        /// The callback url where http request sent to.
        CallbackUrl: string
        /// The name of the logic app receiver. Names must be unique across all receivers within an action group.
        Name: string
        /// The azure resource id of the logic app receiver.
        ResourceId: ResourceId
        /// Indicates whether to use common alert schema.
        UseCommonAlertSchema: bool
    }

    static member Create(callbackUrl, name, resourceId, ?useCommonAlertSchema) =
        {
            CallbackUrl = callbackUrl
            Name = name
            ResourceId = resourceId
            UseCommonAlertSchema = useCommonAlertSchema |> Option.defaultValue true
        }

type SMSReceiver =
    {
        /// The country code of the receiver.
        CountryCode: string
        /// The name of the receiver. Names must be unique across all receivers within an action group.
        Name: string
        /// The phone number of the receiver.
        PhoneNumber: string
    }

    static member Create(countryCode, name, phoneNumber) =
        {
            CountryCode = countryCode
            Name = name
            PhoneNumber = phoneNumber
        }

type VoiceReceiver = SMSReceiver

type WebhookReceiver =
    {
        /// Indicates the identifier uri for aad auth.
        IdentifierUri: string
        /// The name of the webhook receiver. Names must be unique across all receivers within an action group.
        Name: string
        /// Indicates the webhook app object Id for aad auth.
        ObjectId: string
        /// The URI where webhooks should be sent.
        ServiceUri: string
        /// Indicates the tenant id for aad auth.
        TenantId: string
        /// Indicates whether or not use AAD authentication.
        UseAadAuth: bool
        /// Indicates whether to use common alert schema.
        UseCommonAlertSchema: bool
    }

    static member Create(name, serviceUri, ?identifierUri, ?objectId, ?tenantId, ?useAadAuth, ?useCommonAlertSchema) =
        {
            Name = name
            ServiceUri = serviceUri
            IdentifierUri = identifierUri |> Option.defaultValue ""
            ObjectId = objectId |> Option.defaultValue ""
            TenantId = tenantId |> Option.defaultValue ""
            UseAadAuth =
                match useAadAuth with
                | Some b -> b
                | None -> false
            UseCommonAlertSchema = useCommonAlertSchema |> Option.defaultValue true
        }

type ActionGroup =
    {
        Name: Farmer.ResourceName
        GroupShortName: Farmer.ResourceName
        Location: Location
        Enabled: bool
        ArmRoleReceivers: ArmRoleReceiver list
        AutomationRunbookReceivers: AutomationRunbookReceiver list
        AzureAppPushReceivers: AzureAppPushReceiver list
        AzureFunctionReceivers: AzureFunctionReceiver list
        EventHubReceivers: EventHubReceiver list
        ItsmReceivers: ItsmReceiver list
        LogicAppReceivers: LogicAppReceiver list
        VoiceReceivers: VoiceReceiver list
        SMSReceivers: SMSReceiver list
        EmailReceivers: EmailReceiver list
        WebhookReceivers: WebhookReceiver list
    }

    interface Farmer.IArmResource with

        member this.ResourceId = actionGroups.resourceId this.Name

        member this.JsonModel =
            // Location fixed to Global as the list of available locations is currently limited to:
            // global, swedencentral, germanywestcentral, northcentralus, southcentralus, eastus2euap, centraluseuap
            {| actionGroups.Create(this.Name, Location.Global) with
                properties =
                    {|
                        enabled = true
                        GroupShortName = this.GroupShortName.Value
                        ArmRoleReceivers = this.ArmRoleReceivers
                        AutomationRunbookReceivers = this.AutomationRunbookReceivers
                        AzureAppPushReceivers = this.AzureAppPushReceivers
                        AzureFunctionReceivers =
                            this.AzureFunctionReceivers
                            |> List.map (fun r ->
                                {|
                                    FunctionAppResourceId = r.FunctionAppResourceId.Eval() |> box
                                    Name = r.Name
                                    FunctionName = r.FunctionName
                                    HttpTriggerUrl = r.HttpTriggerUrl
                                    UseCommonAlertSchema = r.UseCommonAlertSchema
                                |})
                        EventHubReceivers = this.EventHubReceivers
                        ItsmReceivers = this.ItsmReceivers
                        LogicAppReceivers =
                            this.LogicAppReceivers
                            |> List.map (fun r ->
                                {|
                                    ResourceId = r.ResourceId.Eval() |> box
                                    Name = r.Name
                                    CallbackUrl = r.CallbackUrl
                                    UseCommonAlertSchema = r.UseCommonAlertSchema
                                |})
                        VoiceReceivers = this.VoiceReceivers
                        SMSReceivers = this.SMSReceivers
                        EmailReceivers = this.EmailReceivers
                        WebhookReceivers = this.WebhookReceivers
                    |}
            |}

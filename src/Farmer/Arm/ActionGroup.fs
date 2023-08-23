/// https://learn.microsoft.com/en-us/azure/templates/microsoft.insights/actiongroups
[<AutoOpen>]
module Farmer.Arm.ActionGroups

open Farmer
open System

let actionGroups =
    Farmer.ResourceType("microsoft.insights/actionGroups", "2022-06-01")

type ActionGroupLocation =
    | ActionGroupLocation of string

    member this.ArmValue =
        match this with
        | ActionGroupLocation location -> location.ToLower()

    static member CentralIndia = ActionGroupLocation "CentralIndia"
    static member JapanEast = ActionGroupLocation "JapanEast"
    static member SoutheastAsia = ActionGroupLocation "SoutheastAsia"
    static member AustraliaSoutheast = ActionGroupLocation "AustraliaSoutheast"
    static member UKSouth = ActionGroupLocation "UKSouth"
    static member WestCentralUS = ActionGroupLocation "WestCentralUS"
    static member CanadaCentral = ActionGroupLocation "CanadaCentral"
    static member EastUS = ActionGroupLocation "EastUS"
    static member WestEurope = ActionGroupLocation "WestEurope"

type ArmRoleReceiver =
    {
        /// The name of the arm role receiver. Names must be unique across all receivers within an action group.
        name: string
        /// The arm role id.
        roleId: Guid
        /// Indicates whether to use common alert schema.
        useCommonAlertSchema: bool option
    }

    static member Create(name, (armRole: RoleId), ?useCommonAlertSchema) =
        {
            name = name
            roleId = armRole.Id
            useCommonAlertSchema = useCommonAlertSchema
        }

type AutomationRunbookReceiver =
    {
        /// The Azure automation account Id which holds this runbook and authenticate to Azure resource.
        automationAccountId: ResourceId
        /// Indicates whether this instance is global runbook.
        isGlobalRunbook: bool
        /// Indicates name of the webhook.
        name: string option
        /// The name for this runbook.
        runbookName: string
        /// The URI where webhooks should be sent.
        serviceUri: Uri option
        /// Indicates whether to use common alert schema.
        useCommonAlertSchema: bool option
        /// The resource id for webhook linked to this runbook.
        webhookResourceId: ResourceId
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
            automationAccountId = automationAccountId
            isGlobalRunbook = isGlobalRunbook
            runbookName = runbookName
            webhookResourceId = webhookResourceId
            serviceUri = serviceUri
            name = name
            useCommonAlertSchema = useCommonAlertSchema
        }

type AzureAppPushReceiver =
    {
        /// The email address registered for the Azure mobile app.
        emailAddress: string
        /// The name of the Azure mobile app push receiver. Names must be unique across all receivers within an action group.
        name: string
    }

    static member Create(email, name) = { name = name; emailAddress = email }

type AzureFunctionReceiver =
    {
        /// The azure resource id of the function app.
        functionAppResourceId: ResourceId
        /// The function name in the function app.
        functionName: string
        /// The http trigger url where http request sent to.
        httpTriggerUrl: string
        /// The name of the azure function receiver. Names must be unique across all receivers within an action group.
        name: string
        /// Indicates whether to use common alert schema.
        useCommonAlertSchema: bool option
    }

    static member Create(functionAppResourceId, functionName, httpTriggerUrl, name, ?useCommonAlertSchema) =
        {
            functionAppResourceId = functionAppResourceId
            functionName = functionName
            httpTriggerUrl = httpTriggerUrl
            name = name
            useCommonAlertSchema = useCommonAlertSchema
        }

type EmailReceiver =
    {
        /// The email address of this receiver.
        name: string
        /// The name of the email receiver. Names must be unique across all receivers within an action group.
        emailAddress: string
        /// Indicates whether to use common alert schema.
        useCommonAlertSchema: bool option
    }

    static member Create(name, email, ?useCommonAlertSchema) =
        {
            name = name
            emailAddress = email
            useCommonAlertSchema = useCommonAlertSchema
        }

type EventHubReceiver =
    {
        /// The name of the specific Event Hub queue.
        eventHubName: string
        /// The Event Hub namespace.
        eventHubNameSpace: string
        /// The name of the Event hub receiver. Names must be unique across all receivers within an action group.
        name: string
        /// The Id for the subscription containing this event hub.
        subscriptionId: string
        /// The tenant Id for the subscription containing this event hub.
        tenantId: string option
        /// Indicates whether to use common alert schema.
        useCommonAlertSchema: bool option
    }

    static member Create(eventHubName, eventHubNameSpace, subscriptionId, name, ?tenantId, ?useCommonAlertSchema) =
        {
            eventHubName = eventHubName
            eventHubNameSpace = eventHubNameSpace
            name = name
            subscriptionId = subscriptionId
            tenantId = tenantId
            useCommonAlertSchema = useCommonAlertSchema
        }

type ItsmReceiver =
    {
        /// Unique identification of ITSM connection among multiple defined in above workspace.
        connectionId: string
        /// The name of the Itsm receiver. Names must be unique across all receivers within an action group.
        name: string
        /// Region in which workspace resides. Supported values:
        /// 'centralindia', 'japaneast', 'southeastasia', 'australiasoutheast', 'uksouth', 'westcentralus', 'canadacentral', 'eastus', 'westeurope'
        region: ActionGroupLocation
        /// JSON blob for the configurations of the ITSM action. CreateMultipleWorkItems option will be part of this blob as well.
        ticketConfiguration: string
        /// OMS LA instance identifier.
        workspaceId: string
    }

    static member Create(connectionid, name, region, ticketConfiguration, workspaceId) =
        {
            connectionId = connectionid
            name = name
            region = region
            ticketConfiguration = ticketConfiguration
            workspaceId = workspaceId
        }

type LogicAppReceiver =
    {
        /// The callback url where http request sent to.
        callbackUrl: Uri
        /// The name of the logic app receiver. Names must be unique across all receivers within an action group.
        name: string
        /// The azure resource id of the logic app receiver.
        resourceId: ResourceId
        /// Indicates whether to use common alert schema.
        useCommonAlertSchema: bool option
    }

    static member Create(callbackUrl, name, resourceId, ?useCommonAlertSchema) =
        {
            callbackUrl = callbackUrl
            name = name
            resourceId = resourceId
            useCommonAlertSchema = useCommonAlertSchema
        }

type SMSReceiver =
    {
        /// The country code of the receiver.
        countryCode: string
        /// The name of the receiver. Names must be unique across all receivers within an action group.
        name: string
        /// The phone number of the receiver.
        phoneNumber: string
    }

    static member Create(countryCode, name, phoneNumber) =
        {
            countryCode = countryCode
            name = name
            phoneNumber = phoneNumber
        }

type VoiceReceiver = SMSReceiver

type WebhookReceiver =
    {
        /// Indicates the identifier uri for aad auth.
        identifierUri: Uri option
        /// The name of the webhook receiver. Names must be unique across all receivers within an action group.
        name: string
        /// Indicates the webhook app object Id for aad auth.
        objectId: string option
        /// The URI where webhooks should be sent.
        serviceUri: Uri
        /// Indicates the tenant id for aad auth.
        tenantId: string option
        /// Indicates whether or not use AAD authentication.
        useAadAuth: bool option
        /// Indicates whether to use common alert schema.
        useCommonAlertSchema: bool option
    }

    static member Create(name, serviceUri, ?identifierUri, ?objectId, ?tenantId, ?useAadAuth, ?useCommonAlertSchema) =
        {
            name = name
            serviceUri = serviceUri
            identifierUri = identifierUri
            objectId = objectId
            tenantId = tenantId
            useAadAuth = useAadAuth
            useCommonAlertSchema = useCommonAlertSchema
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
                        groupShortName = this.GroupShortName.Value
                        armRoleReceivers = this.ArmRoleReceivers
                        automationRunbookReceivers =
                            this.AutomationRunbookReceivers
                            |> List.map (fun r ->
                                {|
                                    automationAccountId = r.automationAccountId.Eval()
                                    isGlobalRunbook = r.isGlobalRunbook
                                    name = r.name
                                    runbookName = r.runbookName
                                    serviceUri = r.serviceUri
                                    useCommonAlertSchema = r.useCommonAlertSchema
                                    webhookResourceId = r.webhookResourceId.Eval()
                                |})
                        azureAppPushReceivers = this.AzureAppPushReceivers
                        azureFunctionReceivers =
                            this.AzureFunctionReceivers
                            |> List.map (fun r ->
                                {|
                                    functionAppResourceId = r.functionAppResourceId.Eval()
                                    name = r.name
                                    functionName = r.functionName
                                    httpTriggerUrl = r.httpTriggerUrl
                                    useCommonAlertSchema = r.useCommonAlertSchema
                                |})
                        eventHubReceivers = this.EventHubReceivers
                        itsmReceivers =
                            this.ItsmReceivers
                            |> List.map (fun r ->
                                {|
                                    connectionId = r.connectionId
                                    name = r.name
                                    region = r.region.ArmValue
                                    ticketConfiguration = r.ticketConfiguration
                                    workspaceId = r.workspaceId
                                |})
                        logicAppReceivers =
                            this.LogicAppReceivers
                            |> List.map (fun r ->
                                {|
                                    resourceId = r.resourceId.Eval()
                                    name = r.name
                                    callbackUrl = r.callbackUrl
                                    useCommonAlertSchema = r.useCommonAlertSchema
                                |})
                        voiceReceivers = this.VoiceReceivers
                        smsReceivers = this.SMSReceivers
                        emailReceivers = this.EmailReceivers
                        webhookReceivers = this.WebhookReceivers
                    |}
            |}

---
title: "App Insights - Action Group"
date: 2023-08-08T13:00:00+01:00
chapter: false
weight: 1
---

#### Overview
The Action Group builder is used to create Action Groups. Action Groups are collections of notification preferences that any Alert can be associated with.

* Application Insights Action Groups (`Microsoft.Insights/actionGroups`)

#### Action Group Builder Keywords
The Action Group builder (`actionGroup`) constructs Action Groups.

| Keyword | Purpose |
|-|-|
| name | Sets the name of Action Group. |
| short_name | Sets the short name of the Action Group. |
| enabled | Enables the Action Group. |
| add_arm_role_receivers | Adds ARM Role Receivers. |
| add_automation_runbook_receivers | Adds Automation Runbook Receivers. |
| add_azure_app_push_receivers | Adds ARM Role Receivers. |
| add_azure_function_receivers | Adds ARM Role Receivers. |
| add_event_hub_receivers | Adds Event Hub Receivers. |
| add_itsm_receivers | Adds ITSM Receivers. |
| add_logic_app_receivers | Adds Logic App Receivers. |
| add_voice_receivers | Adds Voice Receivers. |
| add_sms_receivers | Adds SMS Receivers. |
| add_email_receivers | Adds Email Receivers. |
| add_webhook_receivers | Adds Webhook Receivers. |

More detailed documentation: https://learn.microsoft.com/en-us/azure/templates/microsoft.insights/actiongroups?pivots=deployment-language-arm-template#property-values-1

#### Example

Action Group with all types of receivers:

```fsharp
let myArmRoleReceiver =
    ArmRoleReceiver.Create(
        name="...",
        armRole=Roles.Contributor
    )

let myAutomationRunbookReceiver =
    AutomationRunbookReceiver.Create(
        automationAccountId=myAutomationAccount.ResourceId,
        isGlobalRunbook=true,
        runbookName="...",
        webhookResourceId=myWebhook.ResourceId
    )

let myAzureAppPushReceiver =
    AzureAppPushReceiver.Create(
        name="...",
        email="..."
    )

let myAzureFunctionReceiver =
    AzureFunctionReceiver.Create(
        name="...",
        functionAppResourceId=myFunc.ResourceId,
        functionName="...",
        httpTriggerUrl="..."
    )

let myEventHubReceiver =
    EventHubReceiver.Create(
        name="...",
        eventHubName="...",
        eventHubNameSpace="...",
        subscriptionId="..."
    )

let myItsmReceiver =
    ItsmReceiver.Create(
        name="...",
        connectionid="...",
        region="...",
        ticketConfiguration="...",
        workspaceId="..."
    )

let myLogicAppReceiver =
    LogicAppReceiver.Create(
        name="...",
        callbackUrl=myUri,
        resourceId=(myLogicApp :> IBuilder).ResourceId
    )

let myVoiceReceiver =
    VoiceReceiver.Create(
        name="...",
        countryCode="...",
        phoneNumber="..."
    )

let mySmsReceiver =
    SMSReceiver.Create(
        name="...",
        countryCode="...",
        phoneNumber="..."
    )

let myEmailReceiver =
    EmailReceiver.Create(
        name="...",
        email="..."
    )

let myWebhookReceiver =
    WebhookReceiver.Create(
        name="...",
        serviceUri=myUri
    )

let myActionGroup = actionGroup {
    name "My Action Group"
    short_name "ag1"
    enabled true
    add_arm_role_receivers [ myArmRoleReceiver ]
    add_automation_runbook_receivers [ myAutomationRunbookReceiver ]
    add_azure_app_push_receivers [ myAzureAppPushReceiver ]
    add_azure_function_receivers [ myAzureFunctionReceiver ]
    add_event_hub_receivers [ myEventHubReceiver ]
    add_itsm_receivers [ myItsmReceiver ]
    add_logic_app_receivers [ myLogicAppReceiver ]
    add_voice_receivers [ myVoiceReceiver ]
    add_sms_receivers [ mySmsReceiver ]
    add_email_receivers [ myEmailReceiver ]
    add_webhook_receivers [ myWebhookReceiver ]
}
```
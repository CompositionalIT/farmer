[<AutoOpen>]
module Farmer.Builders.ActionGroup

open Farmer
open Farmer.Arm.ActionGroups

type ActionGroupConfig =
    {
        Name: Farmer.ResourceName
        GroupShortName: Farmer.ResourceName
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

    member this.ActionGroupId = $"[resourceId('{actionGroup.Type}', '{this.Name.Value}')]"

    interface IBuilder with
        member this.ResourceId = actionGroup.resourceId this.Name

        member this.BuildResources location =
            let a: ActionGroup =
                {
                    Name = this.Name
                    Enabled = this.Enabled
                    GroupShortName = this.GroupShortName
                    Location = location
                    ArmRoleReceivers = this.ArmRoleReceivers
                    AutomationRunbookReceivers = this.AutomationRunbookReceivers
                    AzureAppPushReceivers = this.AzureAppPushReceivers
                    AzureFunctionReceivers = this.AzureFunctionReceivers
                    EventHubReceivers = this.EventHubReceivers
                    ItsmReceivers = this.ItsmReceivers
                    LogicAppReceivers = this.LogicAppReceivers
                    VoiceReceivers = this.VoiceReceivers
                    SMSReceivers = this.SMSReceivers
                    EmailReceivers = this.EmailReceivers
                    WebhookReceivers = this.WebhookReceivers
                }

            [ a ]

type ActionGroupBuilder() =
    member __.Yield _ =
        {
            Name = ResourceName.Empty
            GroupShortName = ResourceName.Empty
            Enabled = true
            ArmRoleReceivers = List.empty
            AutomationRunbookReceivers = List.empty
            AzureAppPushReceivers = List.empty
            AzureFunctionReceivers = List.empty
            EventHubReceivers = List.empty
            ItsmReceivers = List.empty
            LogicAppReceivers = List.empty
            VoiceReceivers = List.empty
            SMSReceivers = List.empty
            EmailReceivers = List.empty
            WebhookReceivers = List.empty
        }

    /// Sets the name of the action group.
    [<CustomOperation "name">]
    member __.Name(state: ActionGroupConfig, name) = { state with Name = ResourceName name }

    /// Sets the short name of the action group.
    [<CustomOperation "short_name">]
    member __.GroupShortName(state: ActionGroupConfig, shortName) =
        { state with
            GroupShortName = ResourceName shortName
        }

    /// Enables the Action Group.
    [<CustomOperation "enabled">]
    member _.Enabled(state: ActionGroupConfig, enabled) = { state with Enabled = enabled }

    /// Add ARM Role Receivers.
    [<CustomOperation "add_arm_role_receivers">]
    member __.ArmRoleReceivers(state: ActionGroupConfig, armRoleReceivers) =
        { state with
            ArmRoleReceivers = (armRoleReceivers @ state.ArmRoleReceivers)
        }

    /// Add Automation Runbook Receivers.
    [<CustomOperation "add_automation_runbook_receivers">]
    member __.AutomationRunbookReceivers
        (
            state: ActionGroupConfig,
            automationRunbookReceivers: AutomationRunbookReceiver list
        ) =
        { state with
            AutomationRunbookReceivers = (automationRunbookReceivers @ state.AutomationRunbookReceivers)
        }

    /// Add Azure App Push Receivers.
    [<CustomOperation "add_azure_app_push_receivers">]
    member __.AzureAppPushReceivers(state: ActionGroupConfig, azureAppPushReceivers) =
        { state with
            AzureAppPushReceivers = (azureAppPushReceivers @ state.AzureAppPushReceivers)
        }

    /// Add Azure Function Receivers.
    [<CustomOperation "add_azure_function_receivers">]
    member __.AzureFunctionReceivers(state: ActionGroupConfig, azureFunctionReceivers) =
        { state with
            AzureFunctionReceivers = (azureFunctionReceivers @ state.AzureFunctionReceivers)
        }

    /// Add Event Hub Receivers.
    [<CustomOperation "add_event_hub_receivers">]
    member __.EventHubReceivers(state: ActionGroupConfig, eventHubReceivers) =
        { state with
            EventHubReceivers = (eventHubReceivers @ state.EventHubReceivers)
        }

    /// Add ITSM Receivers.
    [<CustomOperation "add_itsm_receivers">]
    member __.ItsmReceivers(state: ActionGroupConfig, itsmReceivers) =
        { state with
            ItsmReceivers = (itsmReceivers @ state.ItsmReceivers)
        }

    /// Add Logic App Receivers.
    [<CustomOperation "add_logic_app_receivers">]
    member __.LogicAppReceivers(state: ActionGroupConfig, logicAppReceivers) =
        { state with
            LogicAppReceivers = (logicAppReceivers @ state.LogicAppReceivers)
        }

    /// Add Voice Receivers.
    [<CustomOperation "add_voice_receivers">]
    member __.VoiceReceivers(state: ActionGroupConfig, voiceReceivers) =
        { state with
            VoiceReceivers = (voiceReceivers @ state.VoiceReceivers)
        }

    /// Add SMS Receivers.
    [<CustomOperation "add_sms_receivers">]
    member __.SMSReceivers(state: ActionGroupConfig, smsReceivers) =
        { state with
            SMSReceivers = (smsReceivers @ state.SMSReceivers)
        }

    /// Add Email Receivers.
    [<CustomOperation "add_email_receivers">]
    member __.EmailReceivers(state: ActionGroupConfig, emailReceivers) =
        { state with
            EmailReceivers = (emailReceivers @ state.EmailReceivers)
        }

    /// Add Webhook Receivers.
    [<CustomOperation "add_webhook_receivers">]
    member __.WebhookReceivers(state: ActionGroupConfig, webhookReceivers) =
        { state with
            WebhookReceivers = (webhookReceivers @ state.WebhookReceivers)
        }

let actionGroup = ActionGroupBuilder()

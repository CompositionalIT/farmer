module ActionGroup

open Expecto
open Farmer
open Farmer.Insights
open Farmer.Arm.ActionGroups
open Farmer.Builders

let tests =
    testList
        "ActionGroups"
        [
            test "Create an ActionGroup" {
                
                let azureFunctionName = "MyFunction"
                let logicAppName = "MyLogicApp"
                let actionGroupName = "MyActionGroup"

                let myFunc = functions {
                    name azureFunctionName
                    operating_system OS.Linux
                }

                let myLogicApp = logicApp {
                    name logicAppName
                }

                let myArmRoleReceiver =
                    ArmRoleReceiver.Create(
                        name="...",
                        armRole=ArmRole.Contributor
                    )

                let myAutomationRunbookReceiver =
                    AutomationRunbookReceiver.Create(
                        automationAccountId="...",
                        isGlobalRunbook=true,
                        runbookName="...",
                        webhookResourceId="..."
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
                        callbackUrl="...",
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
                        serviceUri="..."
                    )

                let ag = actionGroup {
                    name actionGroupName
                    short_name "ag1"
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

                let template = arm { add_resources [ ag ] }
                let jsn = template.Template |> Writer.toJson
                let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

                let props = jobj.SelectToken($"resources[?(@.name=='{actionGroupName}')].properties")
                let isenabled =
                    props.SelectToken($".enabled").ToString()

                let funcReceiverResourceId =
                    props.SelectToken(".AzureFunctionReceivers[0].FunctionAppResourceId").ToString()

                let logicAppReceiverResourceId =
                    props.SelectToken(".LogicAppReceivers[0].ResourceId").ToString()

                let armRoleReceiverRoleId =
                    props.SelectToken(".ArmRoleReceivers[0].RoleId").ToString()

                Expect.equal isenabled "True" "ActionGroup not enabled"
                Expect.equal funcReceiverResourceId $"[resourceId('Microsoft.Web/sites', '{azureFunctionName}')]" "Azure Function ResourceId incorrect"
                Expect.equal logicAppReceiverResourceId $"[resourceId('Microsoft.Logic/workflows', '{logicAppName}')]" "Logic App ResourceId incorrect"
                Expect.equal armRoleReceiverRoleId "b24988ac-6180-42a0-ab88-20f7382dd24c" "RoleId incorrect"

            }

        ]

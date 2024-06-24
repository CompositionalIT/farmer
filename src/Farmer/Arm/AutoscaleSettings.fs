[<AutoOpen>]
module Farmer.Arm.AutoscaleSettings

open System.Xml
open Farmer
open Farmer.Insights
open System

let autoscaleSettings =
    ResourceType("Microsoft.Insights/autoscalesettings", "2022-10-01")

// Have avoided SRTPs in the past but end up with a lot of repetitive code, so trying them.

module internal Option =
    let defaultUnchecked<'t> = Option.defaultValue Unchecked.defaultof<'t>

    let inline toArmJson resourceOpt =
        resourceOpt
        |> Option.map (fun resource -> (^Resource: (member ToArmJson: 't) resource))
        |> defaultUnchecked

module internal List =
    let inline mapToArmJson (list: List<_>) =
        if list.IsEmpty then
            null
        else
            list
            |> List.map (fun resource -> (^Resource: (member ToArmJson: 't) resource))
            |> Seq.ofList


// Let ChatGPT to the really boring stuff
// https://chat.openai.com/share/d6ef3c5e-869c-469d-bf8b-e6488675407c
module Json =

    type Email = {
        customEmails: string seq
        sendToSubscriptionAdministrator: bool
        sendToSubscriptionCoAdministrators: bool
    }

    type Webhook = { properties: obj; serviceUri: string }

    type Notification = {
        email: Email
        operation: string
        webhooks: Webhook seq
    }

    type Capacity = {
        ``default``: string
        maximum: string
        minimum: string
    }

    type Schedule = {
        days: string list
        hours: int list
        minutes: int list
        timeZone: string
    }

    type Dimension = {
        dimensionName: string
        operator: string
        values: string list
    }

    type MetricTrigger = {
        dimensions: Dimension seq
        dividePerInstance: bool
        metricName: string
        metricNamespace: string
        metricResourceLocation: string
        metricResourceUri: string
        operator: string
        statistic: string
        threshold: int
        timeAggregation: string
        timeGrain: string
        timeWindow: string
    }

    type ScaleAction = {
        cooldown: string
        direction: string
        ``type``: string
        value: string
    }

    type Rule = {
        metricTrigger: MetricTrigger
        scaleAction: ScaleAction
    }

    type Recurrence = {
        frequency: string
        schedule: Schedule
    }

    type FixedDate = {
        ``end``: string
        start: string
        timeZone: string
    }

    type Profile = {
        capacity: Capacity
        fixedDate: FixedDate
        name: string
        recurrence: Recurrence
        rules: Rule seq
    }

    type PredictiveAutoscalePolicy = {
        scaleLookAheadTime: string
        scaleMode: string
    }

    type AutoscaleSettingsProperties = {
        enabled: bool
        name: string
        notifications: Notification seq
        predictiveAutoscalePolicy: PredictiveAutoscalePolicy
        profiles: Profile seq
        targetResourceLocation: string
        targetResourceUri: string
    }

open Json

type Email = {
    CustomEmails: string list
    SendToSubscriptionAdministrator: bool
    SendToSubscriptionCoAdministrators: bool
} with

    member this.ToArmJson = {
        customEmails = this.CustomEmails
        sendToSubscriptionAdministrator = this.SendToSubscriptionAdministrator
        sendToSubscriptionCoAdministrators = this.SendToSubscriptionCoAdministrators
    }

type Webhook = {
    Properties: obj
    ServiceUri: Uri
} with

    member this.ToArmJson = {
        properties = this.Properties
        serviceUri = this.ServiceUri.AbsoluteUri
    }

type Notification = {
    Email: Email
    Webhooks: Webhook list
} with

    member this.ToArmJson = {
        email = this.Email.ToArmJson
        operation = "Scale"
        webhooks = this.Webhooks |> List.mapToArmJson
    }


type Capacity = {
    Default: int
    Maximum: int
    Minimum: int
} with

    member this.ToArmJson = {
        ``default`` = string this.Default
        maximum = string this.Maximum
        minimum = string this.Minimum
    }

type Schedule = {
    Days: DayOfWeek list
    Hours: int list
    Minutes: int list
    TimeZone: string
} with

    member this.ToArmJson = {
        days = this.Days |> List.map string
        hours = this.Hours
        minutes = this.Minutes
        timeZone = this.TimeZone
    }

type Dimension = {
    DimensionName: string
    Operator: DimensionOperator
    Values: string list
} with

    member this.ToArmJson = {
        dimensionName = this.DimensionName
        operator = this.Operator.ArmValue
        values = this.Values
    }

type MetricTrigger = {
    Dimensions: Dimension list
    DividePerInstance: bool option
    MetricName: string
    MetricNamespace: string option
    MetricResourceLocation: string option
    MetricResourceUri: ResourceId
    Operator: MetricTriggerOperator
    Statistic: MetricTriggerStatistic
    Threshold: int
    TimeAggregation: MetricTriggerTimeAggregation
    TimeGrain: TimeSpan // Between 12 hours and 1 minute
    TimeWindow: TimeSpan // Between 12 hours and 5 minutes
} with

    member this.ToArmJson = {
        dimensions = this.Dimensions |> List.mapToArmJson
        dividePerInstance = this.DividePerInstance |> Option.defaultValue false
        metricName = this.MetricName
        metricNamespace = this.MetricNamespace |> Option.toObj
        metricResourceLocation = this.MetricResourceLocation |> Option.toObj
        metricResourceUri = this.MetricResourceUri.Eval()
        operator = this.Operator.ArmValue
        statistic = this.Statistic.ArmValue
        threshold = this.Threshold
        timeAggregation = this.TimeAggregation.ArmValue
        timeGrain = XmlConvert.ToString this.TimeGrain
        timeWindow = XmlConvert.ToString this.TimeWindow
    }

type ScaleAction = {
    Cooldown: TimeSpan // from one week to one minute
    Direction: ScaleActionDirection
    Type: ScaleActionType
    Value: int
} with

    member this.ToArmJson = {
        cooldown = XmlConvert.ToString this.Cooldown
        direction = this.Direction.ArmValue
        ``type`` = this.Type.ArmValue
        value = string this.Value
    }

type Rule = {
    MetricTrigger: MetricTrigger
    ScaleAction: ScaleAction
} with

    member this.ToArmJson = {
        metricTrigger = this.MetricTrigger.ToArmJson
        scaleAction = this.ScaleAction.ToArmJson
    }

type Recurrence = {
    Frequency: string
    Schedule: Schedule
} with

    member this.ToArmJson = {
        frequency = this.Frequency
        schedule = this.Schedule.ToArmJson
    }

type FixedDate = {
    End: DateTimeOffset
    Start: DateTimeOffset
    TimeZone: string option
} with

    member this.ToArmJson = {
        ``end`` = this.End.ToString("o")
        start = this.Start.ToString("o")
        timeZone = this.TimeZone |> Option.toObj
    }

type Profile = {
    Capacity: Capacity
    FixedDate: FixedDate option
    Name: string
    Recurrence: Recurrence option
    Rules: Rule list
} with

    member this.ToArmJson = {
        capacity = this.Capacity.ToArmJson
        fixedDate = this.FixedDate |> Option.toArmJson
        name = this.Name
        recurrence = this.Recurrence |> Option.toArmJson
        rules = this.Rules |> List.mapToArmJson
    }

type PredictiveAutoscalePolicy = {
    ScaleLookAheadTime: string
    ScaleMode: string
} with

    member this.ToArmJson = {
        scaleLookAheadTime = this.ScaleLookAheadTime
        scaleMode = this.ScaleMode
    }

type AutoscaleSettingsProperties = {
    Enabled: bool
    Name: string
    Notifications: Notification list
    PredictiveAutoscalePolicy: PredictiveAutoscalePolicy option
    Profiles: Profile list
    TargetResourceLocation: string
    TargetResourceUri: LinkedResource
} with

    member this.ToArmJson = {
        enabled = this.Enabled
        name = this.Name
        notifications = this.Notifications |> List.mapToArmJson
        predictiveAutoscalePolicy = this.PredictiveAutoscalePolicy |> Option.toArmJson
        profiles = this.Profiles |> List.mapToArmJson
        targetResourceLocation = this.TargetResourceLocation
        targetResourceUri = this.TargetResourceUri.ResourceId.Eval()
    }

type AutoscaleSettings = {
    Name: ResourceName
    Location: Location
    Tags: Map<string, string>
    Properties: AutoscaleSettingsProperties
    Dependencies: ResourceId Set
} with

    interface IArmResource with
        member this.JsonModel =
            let dependencies =
                this.Dependencies
                |> LinkedResource.addToSetIfManaged this.Properties.TargetResourceUri

            {|
                autoscaleSettings.Create(this.Name, this.Location, dependsOn = dependencies, tags = this.Tags) with
                    properties = this.Properties.ToArmJson
            |}

        member this.ResourceId = autoscaleSettings.resourceId this.Name

    interface IBuilder with
        member this.BuildResources location =
            if (this.Properties.TargetResourceUri.ResourceId = ResourceId.Empty) then
                raiseFarmer "Must set 'target_resource_uri' for autoscale_settings."

            [ this ]

        member this.ResourceId = autoscaleSettings.resourceId this.Name
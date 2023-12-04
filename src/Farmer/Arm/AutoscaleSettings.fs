[<AutoOpen>]
module Farmer.Arm.AutoscaleSettings

open Farmer

let autoscaleSettings = ResourceType("Microsoft.Insights/autoscalesettings", "2022-10-01")

// Let ChatGPT to the really boring stuff
// https://chat.openai.com/share/d6ef3c5e-869c-469d-bf8b-e6488675407c
module internal Json =

    type Email = {
        customEmails: string list
        sendToSubscriptionAdministrator: bool
        sendToSubscriptionCoAdministrators: bool
    }

    type Webhook = {
        properties: obj
        serviceUri: string
    }

    type Notification = {
        email: Email
        operation: string
        webhooks: Webhook list
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
        DimensionName: string
        Operator: string
        Values: string list
    }

    type MetricTrigger = {
        dimensions: Dimension list
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
        type_: string
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
        rules: Rule list
    }

    type PredictiveAutoscalePolicy = {
        scaleLookAheadTime: string
        scaleMode: string
    }

    type AutoscaleSettingsProperties = {
        enabled: bool
        name: string
        notifications: Notification list
        predictiveAutoscalePolicy: PredictiveAutoscalePolicy
        profiles: Profile list
        targetResourceLocation: string
        targetResourceUri: string
    }

    type AutoscaleSettings = {
        name: string
        location: string
        tags: Tags
        properties: AutoscaleSettingsProperties
    }

type Tags = {
    TagName1: string
    TagName2: string
}

type Email = {
    CustomEmails: string list
    SendToSubscriptionAdministrator: bool
    SendToSubscriptionCoAdministrators: bool
}

type Webhook = {
    Properties: obj
    ServiceUri: string
}

type Notification = {
    Email: Email
    Operation: string
    Webhooks: Webhook list
}

type Capacity = {
    Default: string
    Maximum: string
    Minimum: string
}

type Schedule = {
    Days: string list
    Hours: int list
    Minutes: int list
    TimeZone: string
}

type Dimension = {
    DimensionName: string
    Operator: string
    Values: string list
}

type MetricTrigger = {
    Dimensions: Dimension list
    DividePerInstance: bool
    MetricName: string
    MetricNamespace: string
    MetricResourceLocation: string
    MetricResourceUri: string
    Operator: string
    Statistic: string
    Threshold: int
    TimeAggregation: string
    TimeGrain: string
    TimeWindow: string
}

type ScaleAction = {
    Cooldown: string
    Direction: string
    Type_: string
    Value: string
}

type Rule = {
    MetricTrigger: MetricTrigger
    ScaleAction: ScaleAction
}

type Recurrence = {
    Frequency: string
    Schedule: Schedule
}

type FixedDate = {
    End: string
    Start: string
    TimeZone: string
}

type Profile = {
    Capacity: Capacity
    FixedDate: FixedDate
    Name: string
    Recurrence: Recurrence
    Rules: Rule list
}

type PredictiveAutoscalePolicy = {
    ScaleLookAheadTime: string
    ScaleMode: string
}

type AutoscaleSettingsProperties = {
    Enabled: bool
    Name: string
    Notifications: Notification list
    PredictiveAutoscalePolicy: PredictiveAutoscalePolicy
    Profiles: Profile list
    TargetResourceLocation: string
    TargetResourceUri: string
}

type AutoscaleSettings = {
    Name: ResourceName
    Location: Location
    Tags: Map<string, string>
    Properties: AutoscaleSettingsProperties
}

[<AutoOpen>]
module Farmer.Builders.AutoscaleSettings

open System
open Farmer
open Farmer.Arm.AutoscaleSettings
open Farmer.Insights

type NotificationEmailBuilder() =
    member _.Yield _ =
        {
            CustomEmails = []
            SendToSubscriptionAdministrator = false
            SendToSubscriptionCoAdministrators = false
        }

    [<CustomOperation("custom_emails")>]
    member __.CustomEmails(email: Email, emails: string list) = { email with CustomEmails = emails }

    [<CustomOperation("send_to_subscription_administrator")>]
    member __.SendToSubscriptionAdministrator(email: Email, value: bool) =
        { email with
            SendToSubscriptionAdministrator = value
        }

    [<CustomOperation("send_to_subscription_co_administrators")>]
    member __.SendToSubscriptionCoAdministrators(email: Email, value: bool) =
        { email with
            SendToSubscriptionCoAdministrators = value
        }

let autoscaleNotificationEmail = NotificationEmailBuilder()

type WebhookBuilder() =
    [<CustomOperation("properties")>]
    member __.Properties(webhook: Webhook, props: obj) = { webhook with Properties = props }

    [<CustomOperation("service_uri")>]
    member __.ServiceUri(webhook: Webhook, uri: Uri) = { webhook with ServiceUri = uri }

    member _.Yield _ =
        {
            Properties = null
            ServiceUri = Uri("https://example.com")
        } // Default Uri value

let autoscaleWebhook = WebhookBuilder()

type NotificationBuilder() =
    [<CustomOperation("email")>]
    member _.Email(notification: Notification, email: Email) = { notification with Email = email }

    [<CustomOperation("webhooks")>]
    member _.Webhooks(notification: Notification, webhooks: Webhook list) =
        { notification with
            Webhooks = webhooks
        }

    member _.Yield _ =
        {
            Email =
                {
                    CustomEmails = []
                    SendToSubscriptionAdministrator = false
                    SendToSubscriptionCoAdministrators = false
                }
            Webhooks = []
        }

let autoscaleNotification = NotificationBuilder()

type CapacityBuilder() =
    [<CustomOperation("default")>]
    member _.Default(capacity: Capacity, value: int) = { capacity with Default = value }

    [<CustomOperation("maximum")>]
    member _.Maximum(capacity: Capacity, value: int) = { capacity with Maximum = value }

    [<CustomOperation("minimum")>]
    member _.Minimum(capacity: Capacity, value: int) = { capacity with Minimum = value }

    member _.Yield _ =
        {
            Default = 1
            Maximum = 3
            Minimum = 1
        }

let autoscaleCapacity = CapacityBuilder()

type ScheduleBuilder() =
    [<CustomOperation("days")>]
    member _.Days(schedule: Schedule, days: string list) = { schedule with Days = days }

    [<CustomOperation("hours")>]
    member _.Hours(schedule: Schedule, hours: int list) = { schedule with Hours = hours }

    [<CustomOperation("minutes")>]
    member _.Minutes(schedule: Schedule, minutes: int list) = { schedule with Minutes = minutes }

    [<CustomOperation("timeZone")>]
    member _.TimeZone(schedule: Schedule, timeZone: string) = { schedule with TimeZone = timeZone }

    member _.Yield _ =
        {
            Days = []
            Hours = []
            Minutes = []
            TimeZone = ""
        }

let autoscaleSchedule = ScheduleBuilder()

type DimensionBuilder() =
    [<CustomOperation("dimensionName")>]
    member _.DimensionName(dimension: Dimension, name: string) = { dimension with DimensionName = name }

    [<CustomOperation("operator")>]
    member _.Operator(dimension: Dimension, op: DimensionOperator) = { dimension with Operator = op }

    [<CustomOperation("values")>]
    member _.Values(dimension: Dimension, values: string list) = { dimension with Values = values }

    member _.Yield _ =
        {
            DimensionName = ""
            Operator = DimensionOperator.Equals
            Values = []
        }

let autoscaleDimension = DimensionBuilder()

type ScaleActionBuilder() =
    [<CustomOperation("cooldown")>]
    member _.Cooldown(scaleAction: ScaleAction, cooldown: TimeSpan) =
        { scaleAction with Cooldown = cooldown }

    [<CustomOperation("direction")>]
    member _.Direction(scaleAction: ScaleAction, direction: ScaleActionDirection) =
        { scaleAction with
            Direction = direction
        }

    [<CustomOperation("type")>]
    member _.Type(scaleAction: ScaleAction, actionType: ScaleActionType) = { scaleAction with Type = actionType }

    [<CustomOperation("value")>]
    member _.Value(scaleAction: ScaleAction, value: int) = { scaleAction with Value = value }

    member _.Yield _ =
        {
            Cooldown = TimeSpan.Zero
            Direction = ScaleActionDirection.Increase
            Type = ScaleActionType.ChangeCount
            Value = 1
        }

let scaleAction = ScaleActionBuilder()

type RecurrenceBuilder() =
    [<CustomOperation("frequency")>]
    member _.Frequency(recurrence: Recurrence, frequency: string) =
        { recurrence with
            Frequency = frequency
        }

    [<CustomOperation("schedule")>]
    member _.Schedule(recurrence: Recurrence, schedule: Schedule) = { recurrence with Schedule = schedule }

    member _.Yield _ =
        {
            Frequency = ""
            Schedule = ScheduleBuilder().Yield()
        }

let recurrence = RecurrenceBuilder()

type PredictiveAutoscalePolicyBuilder() =
    [<CustomOperation("scale_look_ahead_time")>]
    member _.ScaleLookAheadTime(policy: PredictiveAutoscalePolicy, lookAheadTime: string) =
        { policy with
            ScaleLookAheadTime = lookAheadTime
        }

    [<CustomOperation("scale_mode")>]
    member _.ScaleMode(policy: PredictiveAutoscalePolicy, mode: string) = { policy with ScaleMode = mode }

    member _.Yield _ =
        {
            ScaleLookAheadTime = ""
            ScaleMode = ""
        }

let predictiveAutoscalePolicy = PredictiveAutoscalePolicyBuilder()

type FixedDateBuilder() =
    [<CustomOperation("end")>]
    member _.End(fixedDate: FixedDate, endValue: string) = { fixedDate with End = endValue }

    [<CustomOperation("start")>]
    member _.Start(fixedDate: FixedDate, startValue: string) = { fixedDate with Start = startValue }

    [<CustomOperation("time_zone")>]
    member _.TimeZone(fixedDate: FixedDate, timeZone: string) = { fixedDate with TimeZone = timeZone }

    member _.Yield _ = { End = ""; Start = ""; TimeZone = "" }

let fixedDate = FixedDateBuilder()

type MetricTriggerBuilder() =
    [<CustomOperation("dimensions")>]
    member _.Dimensions(metricTrigger: MetricTrigger, dimensions: Dimension list) =
        { metricTrigger with
            Dimensions = dimensions
        }

    [<CustomOperation("divide_per_instance")>]
    member _.DividePerInstance(metricTrigger: MetricTrigger, divide: bool option) =
        { metricTrigger with
            DividePerInstance = divide
        }

    [<CustomOperation("metric_name")>]
    member _.MetricName(metricTrigger: MetricTrigger, metricName: string) =
        { metricTrigger with
            MetricName = metricName
        }

    [<CustomOperation("metric_namespace")>]
    member _.MetricNamespace(metricTrigger: MetricTrigger, metricNamespace: string option) =
        { metricTrigger with
            MetricNamespace = metricNamespace
        }

    [<CustomOperation("metric_resource_location")>]
    member _.MetricResourceLocation(metricTrigger: MetricTrigger, metricResourceLocation: string option) =
        { metricTrigger with
            MetricResourceLocation = metricResourceLocation
        }

    [<CustomOperation("metric_resource_rri")>]
    member _.MetricResourceUri(metricTrigger: MetricTrigger, metricResourceUri: ResourceId) =
        { metricTrigger with
            MetricResourceUri = metricResourceUri
        }

    // Add CustomOperation annotations and methods for other fields

    member _.Yield _ =
        {
            Dimensions = []
            DividePerInstance = None
            MetricName = ""
            MetricNamespace = None
            MetricResourceLocation = None
            MetricResourceUri =
                {
                    Name = ResourceName.Empty
                    Type = ResourceType("", "")
                    ResourceGroup = None
                    Subscription = None
                    Segments = []
                }
            Operator = MetricTriggerOperator.GreaterThan
            Statistic = MetricTriggerStatistic.Average
            Threshold = 0
            TimeAggregation = MetricTriggerTimeAggregation.Average
            TimeGrain = TimeSpan.FromMinutes 5
            TimeWindow = TimeSpan.FromMinutes 10
        }

let autoscaleMetricTrigger = MetricTriggerBuilder()

type RuleBuilder() =
    [<CustomOperation("metric_trigger")>]
    member _.MetricTrigger(rule: Rule, trigger: MetricTrigger) = { rule with MetricTrigger = trigger }

    [<CustomOperation("scale_action")>]
    member _.ScaleAction(rule: Rule, action: ScaleAction) = { rule with ScaleAction = action }

    member _.Yield _ =
        {
            MetricTrigger = MetricTriggerBuilder().Yield()
            ScaleAction = ScaleActionBuilder().Yield()
        }

let autoscaleRule = RuleBuilder()

type ProfileBuilder() =
    [<CustomOperation("capacity")>]
    member _.Capacity(profile: Profile, capacity: Capacity) = { profile with Capacity = capacity }

    [<CustomOperation("fixed_date")>]
    member _.FixedDate(profile: Profile, fixedDate: FixedDate option) = { profile with FixedDate = fixedDate }

    [<CustomOperation("name")>]
    member _.Name(profile: Profile, name: string) = { profile with Name = name }

    [<CustomOperation("recurrence")>]
    member _.Recurrence(profile: Profile, recurrence: Recurrence option) =
        { profile with Recurrence = recurrence }

    [<CustomOperation("rules")>]
    member _.Rules(profile: Profile, rules: Rule list) = { profile with Rules = rules }

    member _.Yield _ =
        {
            Capacity = CapacityBuilder().Yield()
            FixedDate = None
            Name = null
            Recurrence = None
            Rules = []
        }

let autoscaleProfile = ProfileBuilder()

[<AutoOpen>]
module Farmer.Builders.AutoscaleSettings

open System
open Farmer
open Farmer.Arm.AutoscaleSettings
open Farmer.Insights


// Written during a good conversation with ChatGPT.
// https://chat.openai.com/share/821d8f25-f37c-4cfc-8ca5-0ff7200e3f04

type NotificationEmailBuilder() =
    member _.Yield _ = {
        CustomEmails = []
        SendToSubscriptionAdministrator = false
        SendToSubscriptionCoAdministrators = false
    }

    [<CustomOperation("custom_emails")>]
    member __.CustomEmails(email: Email, emails: string list) = { email with CustomEmails = emails }

    [<CustomOperation("send_to_subscription_administrator")>]
    member __.SendToSubscriptionAdministrator(email: Email, value: bool) = {
        email with
            SendToSubscriptionAdministrator = value
    }

    [<CustomOperation("send_to_subscription_co_administrators")>]
    member __.SendToSubscriptionCoAdministrators(email: Email, value: bool) = {
        email with
            SendToSubscriptionCoAdministrators = value
    }

let autoscaleNotificationEmail = NotificationEmailBuilder()

type WebhookBuilder() =
    [<CustomOperation("properties")>]
    member __.Properties(webhook: Webhook, props: obj) = { webhook with Properties = props }

    [<CustomOperation("service_uri")>]
    member __.ServiceUri(webhook: Webhook, uri: Uri) = { webhook with ServiceUri = uri }

    member _.Yield _ = {
        Properties = null
        ServiceUri = Uri("https://example.com")
    } // Default Uri value

let autoscaleWebhook = WebhookBuilder()

type NotificationBuilder() =
    [<CustomOperation("email")>]
    member _.Email(notification: Notification, email: Email) = { notification with Email = email }

    [<CustomOperation("webhooks")>]
    member _.Webhooks(notification: Notification, webhooks: Webhook list) = {
        notification with
            Webhooks = webhooks
    }

    member _.Yield _ = {
        Email = {
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

    member _.Yield _ = {
        Default = 1
        Maximum = 3
        Minimum = 1
    }

let autoscaleCapacity = CapacityBuilder()

type ScheduleBuilder() =
    [<CustomOperation("days")>]
    member _.Days(schedule: Schedule, days: DayOfWeek list) = { schedule with Days = days }

    [<CustomOperation("hours")>]
    member _.Hours(schedule: Schedule, hours: int list) = { schedule with Hours = hours }

    [<CustomOperation("minutes")>]
    member _.Minutes(schedule: Schedule, minutes: int list) = { schedule with Minutes = minutes }

    [<CustomOperation("timeZone")>]
    member _.TimeZone(schedule: Schedule, timeZone: string) = { schedule with TimeZone = timeZone }

    member _.Yield _ = {
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

    member _.Yield _ = {
        DimensionName = ""
        Operator = DimensionOperator.Equals
        Values = []
    }

let autoscaleDimension = DimensionBuilder()

type ScaleActionBuilder() =
    [<CustomOperation("cooldown")>]
    member _.Cooldown(scaleAction: ScaleAction, cooldown: TimeSpan) = { scaleAction with Cooldown = cooldown }

    [<CustomOperation("direction")>]
    member _.Direction(scaleAction: ScaleAction, direction: ScaleActionDirection) = {
        scaleAction with
            Direction = direction
    }

    [<CustomOperation("action_type")>]
    member _.Type(scaleAction: ScaleAction, actionType: ScaleActionType) = { scaleAction with Type = actionType }

    [<CustomOperation("value")>]
    member _.Value(scaleAction: ScaleAction, value: int) = { scaleAction with Value = value }

    member _.Yield _ = {
        Cooldown = TimeSpan.FromMinutes 10
        Direction = ScaleActionDirection.Increase
        Type = ScaleActionType.ChangeCount
        Value = 1
    }

let scaleAction = ScaleActionBuilder()

type RecurrenceBuilder() =
    [<CustomOperation("frequency")>]
    member _.Frequency(recurrence: Recurrence, frequency: string) = {
        recurrence with
            Frequency = frequency
    }

    [<CustomOperation("schedule")>]
    member _.Schedule(recurrence: Recurrence, schedule: Schedule) = { recurrence with Schedule = schedule }

    member _.Yield _ = {
        Frequency = ""
        Schedule = ScheduleBuilder().Yield()
    }

let recurrence = RecurrenceBuilder()

type PredictiveAutoscalePolicyBuilder() =
    [<CustomOperation("scale_look_ahead_time")>]
    member _.ScaleLookAheadTime(policy: PredictiveAutoscalePolicy, lookAheadTime: string) = {
        policy with
            ScaleLookAheadTime = lookAheadTime
    }

    [<CustomOperation("scale_mode")>]
    member _.ScaleMode(policy: PredictiveAutoscalePolicy, mode: string) = { policy with ScaleMode = mode }

    member _.Yield _ = {
        ScaleLookAheadTime = ""
        ScaleMode = ""
    }

let predictiveAutoscalePolicy = PredictiveAutoscalePolicyBuilder()

type FixedDateBuilder() =
    [<CustomOperation("end")>]
    member _.End(fixedDate: FixedDate, endValue: DateTimeOffset) = { fixedDate with End = endValue }

    [<CustomOperation("start")>]
    member _.Start(fixedDate: FixedDate, startValue: DateTimeOffset) = { fixedDate with Start = startValue }

    [<CustomOperation("time_zone")>]
    member _.TimeZone(fixedDate: FixedDate, timeZone: string) = {
        fixedDate with
            TimeZone = Some timeZone
    }

    member _.Yield _ = {
        End = DateTimeOffset.MinValue
        Start = DateTimeOffset.MinValue
        TimeZone = None
    }

let fixedDate = FixedDateBuilder()

type MetricTriggerBuilder() =
    [<CustomOperation("dimensions")>]
    member _.Dimensions(metricTrigger: MetricTrigger, dimensions: Dimension list) = {
        metricTrigger with
            Dimensions = dimensions
    }

    [<CustomOperation("divide_per_instance")>]
    member _.DividePerInstance(metricTrigger: MetricTrigger, divide: bool) = {
        metricTrigger with
            DividePerInstance = Some divide
    }

    [<CustomOperation("metric_name")>]
    member _.MetricName(metricTrigger: MetricTrigger, metricName: string) = {
        metricTrigger with
            MetricName = metricName
    }

    [<CustomOperation("metric_namespace")>]
    member _.MetricNamespace(metricTrigger: MetricTrigger, metricNamespace: string) = {
        metricTrigger with
            MetricNamespace = Some metricNamespace
    }

    [<CustomOperation("metric_resource_location")>]
    member _.MetricResourceLocation(metricTrigger: MetricTrigger, metricResourceLocation: string) = {
        metricTrigger with
            MetricResourceLocation = Some metricResourceLocation
    }

    [<CustomOperation("metric_resource_uri")>]
    member _.MetricResourceUri(metricTrigger: MetricTrigger, metricResourceUri: ResourceId) = {
        metricTrigger with
            MetricResourceUri = metricResourceUri
    }

    [<CustomOperation("operator")>]
    member _.Operator(metricTrigger: MetricTrigger, op: MetricTriggerOperator) = { metricTrigger with Operator = op }

    [<CustomOperation("statistic")>]
    member _.Statistic(metricTrigger: MetricTrigger, statistic: MetricTriggerStatistic) = {
        metricTrigger with
            Statistic = statistic
    }

    [<CustomOperation("threshold")>]
    member _.Threshold(metricTrigger: MetricTrigger, threshold: int) = {
        metricTrigger with
            Threshold = threshold
    }

    [<CustomOperation("time_aggregation")>]
    member _.TimeAggregation(metricTrigger: MetricTrigger, timeAggregation: MetricTriggerTimeAggregation) = {
        metricTrigger with
            TimeAggregation = timeAggregation
    }

    [<CustomOperation("time_grain")>]
    member _.TimeGrain(metricTrigger: MetricTrigger, timeGrain: TimeSpan) = {
        metricTrigger with
            TimeGrain = timeGrain
    }

    [<CustomOperation("time_window")>]
    member _.TimeWindow(metricTrigger: MetricTrigger, timeWindow: TimeSpan) = {
        metricTrigger with
            TimeWindow = timeWindow
    }

    member _.Yield _ = {
        Dimensions = []
        DividePerInstance = None
        MetricName = ""
        MetricNamespace = None
        MetricResourceLocation = None
        MetricResourceUri = ResourceId.Empty
        Operator = MetricTriggerOperator.Equals
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

    member _.Yield _ = {
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
    member _.Recurrence(profile: Profile, recurrence: Recurrence option) = { profile with Recurrence = recurrence }

    [<CustomOperation("rules")>]
    member _.Rules(profile: Profile, rules: Rule list) = { profile with Rules = rules }

    member _.Yield _ = {
        Capacity = CapacityBuilder().Yield()
        FixedDate = None
        Name = "DefaultAutoscaleProfile"
        Recurrence = None
        Rules = []
    }

let autoscaleProfile = ProfileBuilder()

type AutoscaleSettingsPropertiesBuilder() =
    [<CustomOperation("enabled")>]
    member _.Enabled(props: AutoscaleSettingsProperties, enabled: bool) = { props with Enabled = enabled }

    [<CustomOperation("name")>]
    member _.Name(props: AutoscaleSettingsProperties, name: string) = { props with Name = name }

    [<CustomOperation("notifications")>]
    member _.Notifications(props: AutoscaleSettingsProperties, notifications: Notification list) = {
        props with
            Notifications = notifications
    }

    [<CustomOperation("predictive_autoscale_policy")>]
    member _.PredictiveAutoscalePolicy(props: AutoscaleSettingsProperties, policy: PredictiveAutoscalePolicy option) = {
        props with
            PredictiveAutoscalePolicy = policy
    }

    [<CustomOperation("profiles")>]
    member _.Profiles(props: AutoscaleSettingsProperties, profiles: Profile list) = { props with Profiles = profiles }

    [<CustomOperation("target_resource_location")>]
    member _.TargetResourceLocation(props: AutoscaleSettingsProperties, location: string) = {
        props with
            TargetResourceLocation = location
    }

    [<CustomOperation("target_resource_uri")>]
    member _.TargetResourceUri(props: AutoscaleSettingsProperties, resourceId: ResourceId) = {
        props with
            TargetResourceUri = Managed resourceId
    }

    member _.Yield _ = {
        Enabled = true
        Name = ""
        Notifications = []
        PredictiveAutoscalePolicy = None
        Profiles = []
        TargetResourceLocation = ""
        TargetResourceUri = Unmanaged ResourceId.Empty
    }

let autoscaleSettingsProperties = AutoscaleSettingsPropertiesBuilder()

type AutoscaleSettingsBuilder() =
    [<CustomOperation("name")>]
    member _.Name(state: AutoscaleSettings, name: string) = { state with Name = ResourceName name }

    [<CustomOperation("location")>]
    member _.Name(state: AutoscaleSettings, location: Location) = { state with Location = location }

    [<CustomOperation("properties")>]
    member _.Properties(state: AutoscaleSettings, properties) = { state with Properties = properties }

    member _.Yield _ = {
        AutoscaleSettings.Name = ResourceName.Empty
        Location = Location.Location "[resourceGroup().Location]"
        Dependencies = Set.empty
        Tags = Map.empty
        Properties = AutoscaleSettingsPropertiesBuilder().Yield()
    }

    member _.Run(state: AutoscaleSettings) = {
        state with
            Properties = {
                state.Properties with
                    Name = state.Name.Value
            }
    }

    interface ITaggable<AutoscaleSettings> with
        member _.Add state tags = {
            state with
                Tags = state.Tags |> Map.merge tags
        }

    interface IDependable<AutoscaleSettings> with
        member _.Add state newDeps = {
            state with
                Dependencies = state.Dependencies + newDeps
        }

let autoscaleSettings = AutoscaleSettingsBuilder()
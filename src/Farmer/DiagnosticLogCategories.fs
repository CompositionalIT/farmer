namespace Farmer.DiagnosticSettings.Logging.AAD

open Farmer.DiagnosticSettings

module DomainServices =
    /// AccountLogon
    let AccountLogon = LogCategory "AccountLogon"
    /// AccountManagement
    let AccountManagement = LogCategory "AccountManagement"
    /// DetailTracking
    let DetailTracking = LogCategory "DetailTracking"
    /// DirectoryServiceAccess
    let DirectoryServiceAccess = LogCategory "DirectoryServiceAccess"
    /// LogonLogoff
    let LogonLogoff = LogCategory "LogonLogoff"
    /// ObjectAccess
    let ObjectAccess = LogCategory "ObjectAccess"
    /// PolicyChange
    let PolicyChange = LogCategory "PolicyChange"
    /// PrivilegeUse
    let PrivilegeUse = LogCategory "PrivilegeUse"
    /// SystemSecurity
    let SystemSecurity = LogCategory "SystemSecurity"

namespace Farmer.DiagnosticSettings.Logging.AnalysisServices

open Farmer.DiagnosticSettings

module Servers =
    /// Engine
    let Engine = LogCategory "Engine"
    /// Service
    let Service = LogCategory "Service"

namespace Farmer.DiagnosticSettings.Logging.ApiManagement

open Farmer.DiagnosticSettings

module Service =
    /// Logs related to ApiManagement Gateway
    let GatewayLogs = LogCategory "GatewayLogs"

namespace Farmer.DiagnosticSettings.Logging.AppConfiguration

open Farmer.DiagnosticSettings

module ConfigurationStores =
    /// HTTP Requests
    let HttpRequest = LogCategory "HttpRequest"

namespace Farmer.DiagnosticSettings.Logging.AppPlatform

open Farmer.DiagnosticSettings

module Spring =
    /// Application Console
    let ApplicationConsole = LogCategory "ApplicationConsole"
    /// System Logs
    let SystemLogs = LogCategory "SystemLogs"

namespace Farmer.DiagnosticSettings.Logging.Attestation

open Farmer.DiagnosticSettings

module AttestationProviders =
    /// AuditEvent message log category.
    let AuditEvent = LogCategory "AuditEvent"
    /// Error message log category.
    let ERR = LogCategory "ERR"
    /// Informational message log category.
    let INF = LogCategory "INF"
    /// Warning message log category.
    let WRN = LogCategory "WRN"

namespace Farmer.DiagnosticSettings.Logging.Automation

open Farmer.DiagnosticSettings

module AutomationAccounts =
    /// Dsc Node Status
    let DscNodeStatus = LogCategory "DscNodeStatus"
    /// Job Logs
    let JobLogs = LogCategory "JobLogs"
    /// Job Streams
    let JobStreams = LogCategory "JobStreams"

namespace Farmer.DiagnosticSettings.Logging.Batch

open Farmer.DiagnosticSettings

module BatchAccounts =
    /// Service Logs
    let ServiceLog = LogCategory "ServiceLog"

namespace Farmer.DiagnosticSettings.Logging.BatchAI

open Farmer.DiagnosticSettings

module Workspaces =
    /// BaiClusterEvent
    let BaiClusterEvent = LogCategory "BaiClusterEvent"
    /// BaiClusterNodeEvent
    let BaiClusterNodeEvent = LogCategory "BaiClusterNodeEvent"
    /// BaiJobEvent
    let BaiJobEvent = LogCategory "BaiJobEvent"

namespace Farmer.DiagnosticSettings.Logging.Blockchain

open Farmer.DiagnosticSettings

module BlockchainMembers =
    /// Blockchain Application
    let BlockchainApplication = LogCategory "BlockchainApplication"
    /// Fabric Orderer
    let FabricOrderer = LogCategory "FabricOrderer"
    /// Fabric Peer
    let FabricPeer = LogCategory "FabricPeer"
    /// Proxy
    let Proxy = LogCategory "Proxy"

module CordaMembers =
    /// Blockchain Application
    let BlockchainApplication = LogCategory "BlockchainApplication"

namespace Farmer.DiagnosticSettings.Logging.Otservice

open Farmer.DiagnosticSettings

module Botservices =
    /// Requests from the channels to the bot
    let BotRequest = LogCategory "BotRequest"
    /// Requests to dependencies
    let DependencyRequest = LogCategory "DependencyRequest"

namespace Farmer.DiagnosticSettings.Logging.Cdn

open Farmer.DiagnosticSettings

module Cdnwebapplicationfirewallpolicies =
    /// Web Appliation Firewall Logs
    let WebApplicationFirewallLogs = LogCategory "WebApplicationFirewallLogs"

module Profiles =
    /// Azure Cdn Access Log
    let AzureCdnAccessLog = LogCategory "AzureCdnAccessLog"
    /// FrontDoor Access Log
    let FrontDoorAccessLog = LogCategory "FrontDoorAccessLog"
    /// FrontDoor Health Probe Log
    let FrontDoorHealthProbeLog = LogCategory "FrontDoorHealthProbeLog"

    /// FrontDoor WebApplicationFirewall Log
    let FrontDoorWebApplicationFirewallLog =
        LogCategory "FrontDoorWebApplicationFirewallLog"

    module Endpoints =
        /// Gets the metrics of the endpoint, e.g., bandwidth, egress, etc.
        let CoreAnalytics = LogCategory "CoreAnalytics"

namespace Farmer.DiagnosticSettings.Logging.ClassicNetwork

open Farmer.DiagnosticSettings

module Networksecuritygroups =
    /// Security Group Rule Flow Event|Network Security Group Rule Flow Event
    let Network = LogCategory "Network"

namespace Farmer.DiagnosticSettings.Logging.CognitiveServices

open Farmer.DiagnosticSettings

module Accounts =
    /// Audit Logs
    let Audit = LogCategory "Audit"
    /// Request and Response Logs
    let RequestResponse = LogCategory "RequestResponse"
    /// Trace Logs
    let Trace = LogCategory "Trace"

namespace Farmer.DiagnosticSettings.Logging.Communication

open Farmer.DiagnosticSettings

module CommunicationServices =
    /// Operational Chat Logs
    let ChatOperational = LogCategory "ChatOperational"
    /// Operational SMS Logs
    let SMSOperational = LogCategory "SMSOperational"
    /// Usage Records
    let Usage = LogCategory "Usage"

namespace Farmer.DiagnosticSettings.Logging.ContainerRegistry

open Farmer.DiagnosticSettings

module Registries =
    /// Login Events
    let ContainerRegistryLoginEvents = LogCategory "ContainerRegistryLoginEvents"

    /// RepositoryEvent logs
    let ContainerRegistryRepositoryEvents =
        LogCategory "ContainerRegistryRepositoryEvents"

namespace Farmer.DiagnosticSettings.Logging.ContainerService

open Farmer.DiagnosticSettings

module ManagedClusters =
    /// autoscaler|Kubernetes Cluster Autoscaler
    let cluster = LogCategory "cluster"
    /// guard
    let guard = LogCategory "guard"
    /// Kubernetes API Server
    let kubeapiserver = LogCategory "kube"
    /// Kubernetes Audit
    let kubeaudit = LogCategory "kube"
    /// Kubernetes Audit Admin Logs
    let kubeauditadmin = LogCategory "kube"
    /// Kubernetes Controller Manager
    let kubecontrollermanager = LogCategory "kube"
    /// Kubernetes Scheduler
    let kubescheduler = LogCategory "kube"

namespace Farmer.DiagnosticSettings.Logging.CustomProviders

open Farmer.DiagnosticSettings

module Resourceproviders =
    /// Audit logs for MiniRP calls
    let AuditLogs = LogCategory "AuditLogs"

namespace Farmer.DiagnosticSettings.Logging.D365CustomerInsights

open Farmer.DiagnosticSettings

module Instances =
    /// Audit events
    let Audit = LogCategory "Audit"
    /// Operational events
    let Operational = LogCategory "Operational"

namespace Farmer.DiagnosticSettings.Logging.Databricks

open Farmer.DiagnosticSettings

module Workspaces =
    /// Databricks Accounts
    let accounts = LogCategory "accounts"
    /// Databricks Clusters
    let clusters = LogCategory "clusters"
    /// Databricks File System
    let dbfs = LogCategory "dbfs"
    /// Instance Pools
    let instancePools = LogCategory "instancePools"
    /// Databricks Jobs
    let jobs = LogCategory "jobs"
    /// Databricks Notebook
    let notebook = LogCategory "notebook"
    /// Databricks Secrets
    let secrets = LogCategory "secrets"
    /// Databricks SQLPermissions
    let sqlPermissions = LogCategory "sqlPermissions"
    /// Databricks SSH
    let ssh = LogCategory "ssh"
    /// Databricks Workspace
    let workspace = LogCategory "workspace"

namespace Farmer.DiagnosticSettings.Logging.DataCollaboration

open Farmer.DiagnosticSettings

module Workspaces =
    /// Collaboration Audit
    let CollaborationAudit = LogCategory "CollaborationAudit"
    /// Data Assets
    let DataAssets = LogCategory "DataAssets"
    /// Pipelines
    let Pipelines = LogCategory "Pipelines"
    /// Proposals
    let Proposals = LogCategory "Proposals"
    /// Scripts
    let Scripts = LogCategory "Scripts"

namespace Farmer.DiagnosticSettings.Logging.DataFactory

open Farmer.DiagnosticSettings

module Factories =
    /// Pipeline activity runs log
    let ActivityRuns = LogCategory "ActivityRuns"
    /// Pipeline runs log
    let PipelineRuns = LogCategory "PipelineRuns"
    /// SSIS integration runtime logs
    let SSISIntegrationRuntimeLogs = LogCategory "SSISIntegrationRuntimeLogs"
    /// SSIS package event message context
    let SSISPackageEventMessageContext = LogCategory "SSISPackageEventMessageContext"
    /// SSIS package event messages
    let SSISPackageEventMessages = LogCategory "SSISPackageEventMessages"
    /// SSIS package executable statistics
    let SSISPackageExecutableStatistics = LogCategory "SSISPackageExecutableStatistics"

    /// SSIS package execution component phases
    let SSISPackageExecutionComponentPhases =
        LogCategory "SSISPackageExecutionComponentPhases"

    /// SSIS package exeution data statistics
    let SSISPackageExecutionDataStatistics =
        LogCategory "SSISPackageExecutionDataStatistics"

    /// Trigger runs log
    let TriggerRuns = LogCategory "TriggerRuns"

namespace Farmer.DiagnosticSettings.Logging.DataLakeAnalytics

open Farmer.DiagnosticSettings

module Accounts =
    /// Audit Logs
    let Audit = LogCategory "Audit"
    /// Request Logs
    let Requests = LogCategory "Requests"

namespace Farmer.DiagnosticSettings.Logging.DataLakeStore

open Farmer.DiagnosticSettings

module Accounts =
    /// Audit Logs
    let Audit = LogCategory "Audit"
    /// Request Logs
    let Requests = LogCategory "Requests"

namespace Farmer.DiagnosticSettings.Logging.DataShare

open Farmer.DiagnosticSettings

module Accounts =
    /// Received Share Snapshots
    let ReceivedShareSnapshots = LogCategory "ReceivedShareSnapshots"
    /// Sent Share Snapshots
    let SentShareSnapshots = LogCategory "SentShareSnapshots"
    /// Shares
    let Shares = LogCategory "Shares"
    /// Share Subscriptions
    let ShareSubscriptions = LogCategory "ShareSubscriptions"

namespace Farmer.DiagnosticSettings.Logging.DBforMariaDB

open Farmer.DiagnosticSettings

module Servers =
    /// MariaDB Audit Logs
    let MySqlAuditLogs = LogCategory "MySqlAuditLogs"
    /// MariaDB Server Logs
    let MySqlSlowLogs = LogCategory "MySqlSlowLogs"

namespace Farmer.DiagnosticSettings.Logging.DBforMySQL

open Farmer.DiagnosticSettings

module FlexibleServers =
    /// MySQL Audit Logs
    let MySqlAuditLogs = LogCategory "MySqlAuditLogs"
    /// MySQL Slow Logs
    let MySqlSlowLogs = LogCategory "MySqlSlowLogs"

module Servers =
    /// MySQL Audit Logs
    let MySqlAuditLogs = LogCategory "MySqlAuditLogs"
    /// MySQL Server Logs
    let MySqlSlowLogs = LogCategory "MySqlSlowLogs"

namespace Farmer.DiagnosticSettings.Logging.DBforPostgreSQL

open Farmer.DiagnosticSettings

module FlexibleServers =
    /// PostgreSQL Server Logs
    let PostgreSQLLogs = LogCategory "PostgreSQLLogs"

module Servers =
    /// PostgreSQL Server Logs
    let PostgreSQLLogs = LogCategory "PostgreSQLLogs"
    /// PostgreSQL Query Store Runtime Statistics
    let QueryStoreRuntimeStatistics = LogCategory "QueryStoreRuntimeStatistics"
    /// PostgreSQL Query Store Wait Statistics
    let QueryStoreWaitStatistics = LogCategory "QueryStoreWaitStatistics"

module Serversv2 =
    /// PostgreSQL Server Logs
    let PostgreSQLLogs = LogCategory "PostgreSQLLogs"

namespace Farmer.DiagnosticSettings.Logging.DesktopVirtualization

open Farmer.DiagnosticSettings

module Applicationgroups =
    /// Checkpoint
    let Checkpoint = LogCategory "Checkpoint"
    /// Error
    let Error = LogCategory "Error"
    /// Management
    let Management = LogCategory "Management"

module Hostpools =
    /// AgentHealthStatus
    let AgentHealthStatus = LogCategory "AgentHealthStatus"
    /// Checkpoint
    let Checkpoint = LogCategory "Checkpoint"
    /// Connection
    let Connection = LogCategory "Connection"
    /// Error
    let Error = LogCategory "Error"
    /// HostRegistration
    let HostRegistration = LogCategory "HostRegistration"
    /// Management
    let Management = LogCategory "Management"

module Workspaces =
    /// Checkpoint
    let Checkpoint = LogCategory "Checkpoint"
    /// Error
    let Error = LogCategory "Error"
    /// Feed
    let Feed = LogCategory "Feed"
    /// Management
    let Management = LogCategory "Management"

namespace Farmer.DiagnosticSettings.Logging.Devices.ElasticPools

open Farmer.DiagnosticSettings

module IotHubTenants =
    /// C2D Commands
    let C2DCommands = LogCategory "C2DCommands"
    /// C2D Twin Operations
    let C2DTwinOperations = LogCategory "C2DTwinOperations"
    /// Configurations
    let Configurations = LogCategory "Configurations"
    /// Connections
    let Connections = LogCategory "Connections"
    /// D2CTwinOperations
    let D2CTwinOperations = LogCategory "D2CTwinOperations"
    /// Device Identity Operations
    let DeviceIdentityOperations = LogCategory "DeviceIdentityOperations"
    /// Device Streams (Preview)
    let DeviceStreams = LogCategory "DeviceStreams"
    /// Device Telemetry
    let DeviceTelemetry = LogCategory "DeviceTelemetry"
    /// Direct Methods
    let DirectMethods = LogCategory "DirectMethods"
    /// Distributed Tracing (Preview)
    let DistributedTracing = LogCategory "DistributedTracing"
    /// File Upload Operations
    let FileUploadOperations = LogCategory "FileUploadOperations"
    /// Jobs Operations
    let JobsOperations = LogCategory "JobsOperations"
    /// Routes
    let Routes = LogCategory "Routes"
    /// Twin Queries
    let TwinQueries = LogCategory "TwinQueries"

namespace Farmer.DiagnosticSettings.Logging.Devices

open Farmer.DiagnosticSettings

module IotHubs =
    /// C2D Commands
    let C2DCommands = LogCategory "C2DCommands"
    /// C2D Twin Operations
    let C2DTwinOperations = LogCategory "C2DTwinOperations"
    /// Configurations
    let Configurations = LogCategory "Configurations"
    /// Connections
    let Connections = LogCategory "Connections"
    /// D2CTwinOperations
    let D2CTwinOperations = LogCategory "D2CTwinOperations"
    /// Device Identity Operations
    let DeviceIdentityOperations = LogCategory "DeviceIdentityOperations"
    /// Device Streams (Preview)
    let DeviceStreams = LogCategory "DeviceStreams"
    /// Device Telemetry
    let DeviceTelemetry = LogCategory "DeviceTelemetry"
    /// Direct Methods
    let DirectMethods = LogCategory "DirectMethods"
    /// Distributed Tracing (Preview)
    let DistributedTracing = LogCategory "DistributedTracing"
    /// File Upload Operations
    let FileUploadOperations = LogCategory "FileUploadOperations"
    /// Jobs Operations
    let JobsOperations = LogCategory "JobsOperations"
    /// Routes
    let Routes = LogCategory "Routes"
    /// Twin Queries
    let TwinQueries = LogCategory "TwinQueries"

module ProvisioningServices =
    /// Device Operations
    let DeviceOperations = LogCategory "DeviceOperations"
    /// Service Operations
    let ServiceOperations = LogCategory "ServiceOperations"

namespace Farmer.DiagnosticSettings.Logging.DigitalTwins

open Farmer.DiagnosticSettings

module DigitalTwinsInstances =
    /// DigitalTwinsOperation
    let DigitalTwinsOperation = LogCategory "DigitalTwinsOperation"
    /// EventRoutesOperation
    let EventRoutesOperation = LogCategory "EventRoutesOperation"
    /// ModelsOperation
    let ModelsOperation = LogCategory "ModelsOperation"
    /// QueryOperation
    let QueryOperation = LogCategory "QueryOperation"

namespace Farmer.DiagnosticSettings.Logging.DocumentDB

open Farmer.DiagnosticSettings

module DatabaseAccounts =
    /// CassandraRequests
    let CassandraRequests = LogCategory "CassandraRequests"
    /// ControlPlaneRequests
    let ControlPlaneRequests = LogCategory "ControlPlaneRequests"
    /// DataPlaneRequests
    let DataPlaneRequests = LogCategory "DataPlaneRequests"
    /// GremlinRequests
    let GremlinRequests = LogCategory "GremlinRequests"
    /// MongoRequests
    let MongoRequests = LogCategory "MongoRequests"
    /// PartitionKeyRUConsumption
    let PartitionKeyRUConsumption = LogCategory "PartitionKeyRUConsumption"
    /// PartitionKeyStatistics
    let PartitionKeyStatistics = LogCategory "PartitionKeyStatistics"
    /// QueryRuntimeStatistics
    let QueryRuntimeStatistics = LogCategory "QueryRuntimeStatistics"

namespace Farmer.DiagnosticSettings.Logging.EventGrid

open Farmer.DiagnosticSettings

module Domains =
    /// Delivery Failure Logs
    let DeliveryFailures = LogCategory "DeliveryFailures"
    /// Publish Failure Logs
    let PublishFailures = LogCategory "PublishFailures"

module PartnerNamespaces =
    /// Delivery Failure Logs
    let DeliveryFailures = LogCategory "DeliveryFailures"
    /// Publish Failure Logs
    let PublishFailures = LogCategory "PublishFailures"

module PartnerTopics =
    /// Delivery Failure Logs
    let DeliveryFailures = LogCategory "DeliveryFailures"

module SystemTopics =
    /// Delivery Failure Logs
    let DeliveryFailures = LogCategory "DeliveryFailures"

module Topics =
    /// Delivery Failure Logs
    let DeliveryFailures = LogCategory "DeliveryFailures"
    /// Publish Failure Logs
    let PublishFailures = LogCategory "PublishFailures"

namespace Farmer.DiagnosticSettings.Logging.EventHub

open Farmer.DiagnosticSettings

module Namespaces =
    /// Archive Logs
    let ArchiveLogs = LogCategory "ArchiveLogs"
    /// Auto Scale Logs
    let AutoScaleLogs = LogCategory "AutoScaleLogs"
    /// Customer Managed Key Logs
    let CustomerManagedKeyUserLogs = LogCategory "CustomerManagedKeyUserLogs"
    /// VNet/IP Filtering Connection Logs
    let EventHubVNetConnectionEvent = LogCategory "EventHubVNetConnectionEvent"
    /// Kafka Coordinator Logs
    let KafkaCoordinatorLogs = LogCategory "KafkaCoordinatorLogs"
    /// Kafka User Error Logs
    let KafkaUserErrorLogs = LogCategory "KafkaUserErrorLogs"
    /// Operational Logs
    let OperationalLogs = LogCategory "OperationalLogs"

namespace Farmer.DiagnosticSettings.Logging.Experimentation

open Farmer.DiagnosticSettings

module ExperimentWorkspaces =
    /// Request
    let Request = LogCategory "Request"

namespace Farmer.DiagnosticSettings.Logging.HealthcareApis

open Farmer.DiagnosticSettings

module Services =
    /// Audit logs
    let AuditLogs = LogCategory "AuditLogs"

namespace Farmer.DiagnosticSettings.Logging.Insights

open Farmer.DiagnosticSettings

module Autoscalesettings =
    /// Autoscale Evaluations
    let AutoscaleEvaluations = LogCategory "AutoscaleEvaluations"
    /// Autoscale Scale Actions
    let AutoscaleScaleActions = LogCategory "AutoscaleScaleActions"

module Components =
    /// Availability results
    let AppAvailabilityResults = LogCategory "AppAvailabilityResults"
    /// Browser timings
    let AppBrowserTimings = LogCategory "AppBrowserTimings"
    /// Dependencies
    let AppDependencies = LogCategory "AppDependencies"
    /// Events
    let AppEvents = LogCategory "AppEvents"
    /// Exceptions
    let AppExceptions = LogCategory "AppExceptions"
    /// Metrics
    let AppMetrics = LogCategory "AppMetrics"
    /// Page views
    let AppPageViews = LogCategory "AppPageViews"
    /// Performance counters
    let AppPerformanceCounters = LogCategory "AppPerformanceCounters"
    /// Requests
    let AppRequests = LogCategory "AppRequests"
    /// System events
    let AppSystemEvents = LogCategory "AppSystemEvents"
    /// Traces
    let AppTraces = LogCategory "AppTraces"

namespace Farmer.DiagnosticSettings.Logging.IoTSpaces

open Farmer.DiagnosticSettings

module Graph =
    /// Audit
    let Audit = LogCategory "Audit"
    /// Egress
    let Egress = LogCategory "Egress"
    /// Ingress
    let Ingress = LogCategory "Ingress"
    /// Operational
    let Operational = LogCategory "Operational"
    /// Trace
    let Trace = LogCategory "Trace"
    /// UserDefinedFunction
    let UserDefinedFunction = LogCategory "UserDefinedFunction"

namespace Farmer.DiagnosticSettings.Logging.KeyVault

open Farmer.DiagnosticSettings

module Managedhsms =
    /// Audit Event
    let AuditEvent = LogCategory "AuditEvent"

module Vaults =
    /// Audit Logs
    let AuditEvent = LogCategory "AuditEvent"

namespace Farmer.DiagnosticSettings.Logging.Kusto

open Farmer.DiagnosticSettings

module Clusters =
    /// Command
    let Command = LogCategory "Command"
    /// Failed ingest operations
    let FailedIngestion = LogCategory "FailedIngestion"
    /// Ingestion batching
    let IngestionBatching = LogCategory "IngestionBatching"
    /// Query
    let Query = LogCategory "Query"
    /// Successful ingest operations
    let SucceededIngestion = LogCategory "SucceededIngestion"
    /// Table details
    let TableDetails = LogCategory "TableDetails"
    /// Table usage statistics
    let TableUsageStatistics = LogCategory "TableUsageStatistics"

namespace Farmer.DiagnosticSettings.Logging.Logic

open Farmer.DiagnosticSettings

module IntegrationAccounts =
    /// Integration Account track events
    let IntegrationAccountTrackingEvents =
        LogCategory "IntegrationAccountTrackingEvents"

module Workflows =
    /// Workflow runtime diagnostic events
    let WorkflowRuntime = LogCategory "WorkflowRuntime"

namespace Farmer.DiagnosticSettings.Logging.MachineLearningServices

open Farmer.DiagnosticSettings

module Workspaces =
    /// AmlComputeClusterEvent
    let AmlComputeClusterEvent = LogCategory "AmlComputeClusterEvent"
    /// AmlComputeClusterNodeEvent
    let AmlComputeClusterNodeEvent = LogCategory "AmlComputeClusterNodeEvent"
    /// AmlComputeCpuGpuUtilization
    let AmlComputeCpuGpuUtilization = LogCategory "AmlComputeCpuGpuUtilization"
    /// AmlComputeJobEvent
    let AmlComputeJobEvent = LogCategory "AmlComputeJobEvent"
    /// AmlRunStatusChangedEvent
    let AmlRunStatusChangedEvent = LogCategory "AmlRunStatusChangedEvent"

namespace Farmer.DiagnosticSettings.Logging.Media

open Farmer.DiagnosticSettings

module Mediaservices =
    /// Key Delivery Requests
    let KeyDeliveryRequests = LogCategory "KeyDeliveryRequests"

namespace Farmer.DiagnosticSettings.Logging.Network

open Farmer.DiagnosticSettings

module ApplicationGateways =
    /// Application Gateway Access Log
    let ApplicationGatewayAccessLog = LogCategory "ApplicationGatewayAccessLog"
    /// Application Gateway Firewall Log
    let ApplicationGatewayFirewallLog = LogCategory "ApplicationGatewayFirewallLog"

    /// Application Gateway Performance Log
    let ApplicationGatewayPerformanceLog =
        LogCategory "ApplicationGatewayPerformanceLog"

module Azurefirewalls =
    /// Azure Firewall Application Rule
    let AzureFirewallApplicationRule = LogCategory "AzureFirewallApplicationRule"
    /// Azure Firewall DNS Proxy
    let AzureFirewallDnsProxy = LogCategory "AzureFirewallDnsProxy"
    /// Azure Firewall Network Rule
    let AzureFirewallNetworkRule = LogCategory "AzureFirewallNetworkRule"

module BastionHosts =
    /// Bastion Audit Logs
    let BastionAuditLogs = LogCategory "BastionAuditLogs"

module ExpressRouteCircuits =
    /// Peering Route Table Logs
    let PeeringRouteLog = LogCategory "PeeringRouteLog"

module Frontdoors =
    /// Frontdoor Access Log
    let FrontdoorAccessLog = LogCategory "FrontdoorAccessLog"

    /// Frontdoor Web Application Firewall Log
    let FrontdoorWebApplicationFirewallLog =
        LogCategory "FrontdoorWebApplicationFirewallLog"

module LoadBalancers =
    /// Load Balancer Alert Events
    let LoadBalancerAlertEvent = LogCategory "LoadBalancerAlertEvent"
    /// Load Balancer Probe Health Status
    let LoadBalancerProbeHealthStatus = LogCategory "LoadBalancerProbeHealthStatus"

module Networksecuritygroups =
    /// Network Security Group Event
    let NetworkSecurityGroupEvent = LogCategory "NetworkSecurityGroupEvent"
    /// Network Security Group Rule Flow Event
    let NetworkSecurityGroupFlowEvent = LogCategory "NetworkSecurityGroupFlowEvent"
    /// Network Security Group Rule Counter
    let NetworkSecurityGroupRuleCounter = LogCategory "NetworkSecurityGroupRuleCounter"

module P2sVpnGateways =
    /// Gateway Diagnostic Logs
    let GatewayDiagnosticLog = LogCategory "GatewayDiagnosticLog"
    /// IKE Diagnostic Logs
    let IKEDiagnosticLog = LogCategory "IKEDiagnosticLog"
    /// P2S Diagnostic Logs
    let P2SDiagnosticLog = LogCategory "P2SDiagnosticLog"

module PublicIPAddresses =
    /// Flow logs of DDoS mitigation decisions
    let DDoSMitigationFlowLogs = LogCategory "DDoSMitigationFlowLogs"
    /// Reports of DDoS mitigations
    let DDoSMitigationReports = LogCategory "DDoSMitigationReports"
    /// DDoS protection notifications
    let DDoSProtectionNotifications = LogCategory "DDoSProtectionNotifications"

module TrafficManagerProfiles =
    /// Traffic Manager Probe Health Results Event
    let ProbeHealthStatusEvents = LogCategory "ProbeHealthStatusEvents"

module VirtualNetworkGateways =
    /// Gateway Diagnostic Logs
    let GatewayDiagnosticLog = LogCategory "GatewayDiagnosticLog"
    /// IKE Diagnostic Logs
    let IKEDiagnosticLog = LogCategory "IKEDiagnosticLog"
    /// P2S Diagnostic Logs
    let P2SDiagnosticLog = LogCategory "P2SDiagnosticLog"
    /// Route Diagnostic Logs
    let RouteDiagnosticLog = LogCategory "RouteDiagnosticLog"
    /// Tunnel Diagnostic Logs
    let TunnelDiagnosticLog = LogCategory "TunnelDiagnosticLog"

module VirtualNetworks =
    /// VM protection alerts
    let VMProtectionAlerts = LogCategory "VMProtectionAlerts"

module VpnGateways =
    /// Gateway Diagnostic Logs
    let GatewayDiagnosticLog = LogCategory "GatewayDiagnosticLog"
    /// IKE Diagnostic Logs
    let IKEDiagnosticLog = LogCategory "IKEDiagnosticLog"
    /// Route Diagnostic Logs
    let RouteDiagnosticLog = LogCategory "RouteDiagnosticLog"
    /// Tunnel Diagnostic Logs
    let TunnelDiagnosticLog = LogCategory "TunnelDiagnosticLog"

namespace Farmer.DiagnosticSettings.Logging.NotificationHubs

open Farmer.DiagnosticSettings

module Namespaces =
    /// Operational Logs
    let OperationalLogs = LogCategory "OperationalLogs"

namespace Farmer.DiagnosticSettings.Logging.OperationalInsights

open Farmer.DiagnosticSettings

module Workspaces =
    /// Audit Logs
    let Audit = LogCategory "Audit"

namespace Farmer.DiagnosticSettings.Logging.PowerBI

open Farmer.DiagnosticSettings

module Tenants =
    /// Engine
    let Engine = LogCategory "Engine"

    module Workspaces =
        /// Engine
        let Engine = LogCategory "Engine"

namespace Farmer.DiagnosticSettings.Logging.PowerBIDedicated

open Farmer.DiagnosticSettings

module Capacities =
    /// Engine
    let Engine = LogCategory "Engine"

namespace Farmer.DiagnosticSettings.Logging.ProjectBabylon

open Farmer.DiagnosticSettings

module Accounts =
    /// ScanStatus
    let ScanStatusLogEvent = LogCategory "ScanStatusLogEvent"

namespace Farmer.DiagnosticSettings.Logging.Purview

open Farmer.DiagnosticSettings

module Accounts =
    /// ScanStatus
    let ScanStatusLogEvent = LogCategory "ScanStatusLogEvent"

namespace Farmer.DiagnosticSettings.Logging.RecoveryServices

open Farmer.DiagnosticSettings

module Vaults =
    /// Addon Azure Backup Alert Data
    let AddonAzureBackupAlerts = LogCategory "AddonAzureBackupAlerts"
    /// Addon Azure Backup Job Data
    let AddonAzureBackupJobs = LogCategory "AddonAzureBackupJobs"
    /// Addon Azure Backup Policy Data
    let AddonAzureBackupPolicy = LogCategory "AddonAzureBackupPolicy"

    /// Addon Azure Backup Protected Instance Data
    let AddonAzureBackupProtectedInstance =
        LogCategory "AddonAzureBackupProtectedInstance"

    /// Addon Azure Backup Storage Data
    let AddonAzureBackupStorage = LogCategory "AddonAzureBackupStorage"
    /// Azure Backup Reporting Data
    let AzureBackupReport = LogCategory "AzureBackupReport"
    /// Azure Site Recovery Events
    let AzureSiteRecoveryEvents = LogCategory "AzureSiteRecoveryEvents"
    /// Azure Site Recovery Jobs
    let AzureSiteRecoveryJobs = LogCategory "AzureSiteRecoveryJobs"

    /// Azure Site Recovery Protected Disk Data Churn
    let AzureSiteRecoveryProtectedDiskDataChurn =
        LogCategory "AzureSiteRecoveryProtectedDiskDataChurn"

    /// Azure Site Recovery Recovery Points
    let AzureSiteRecoveryRecoveryPoints = LogCategory "AzureSiteRecoveryRecoveryPoints"

    /// Azure Site Recovery Replicated Items
    let AzureSiteRecoveryReplicatedItems =
        LogCategory "AzureSiteRecoveryReplicatedItems"

    /// Azure Site Recovery Replication Data Upload Rate
    let AzureSiteRecoveryReplicationDataUploadRate =
        LogCategory "AzureSiteRecoveryReplicationDataUploadRate"

    /// Azure Site Recovery Replication Stats
    let AzureSiteRecoveryReplicationStats =
        LogCategory "AzureSiteRecoveryReplicationStats"

    /// Core Azure Backup Data
    let CoreAzureBackup = LogCategory "CoreAzureBackup"

namespace Farmer.DiagnosticSettings.Logging.Relay

open Farmer.DiagnosticSettings

module Namespaces =
    /// HybridConnections Events
    let HybridConnectionsEvent = LogCategory "HybridConnectionsEvent"
    /// HybridConnectionsLogs
    let HybridConnectionsLogs = LogCategory "HybridConnectionsLogs"

namespace Farmer.DiagnosticSettings.Logging.Search

open Farmer.DiagnosticSettings

module SearchServices =
    /// Operation Logs
    let OperationLogs = LogCategory "OperationLogs"

namespace Farmer.DiagnosticSettings.Logging.ServiceBus

open Farmer.DiagnosticSettings

module Namespaces =
    /// Operational Logs
    let OperationalLogs = LogCategory "OperationalLogs"

namespace Farmer.DiagnosticSettings.Logging.SignalRService

open Farmer.DiagnosticSettings

module SignalR =
    /// Azure SignalR Service Logs.
    let AllLogs = LogCategory "AllLogs"

namespace Farmer.DiagnosticSettings.Logging.Sql

open Farmer.DiagnosticSettings

module ManagedInstances =
    /// Devops operations Audit Logs
    let DevOpsOperationsAudit = LogCategory "DevOpsOperationsAudit"
    /// Resource Usage Statistics
    let ResourceUsageStats = LogCategory "ResourceUsageStats"
    /// SQL Security Audit Event
    let SQLSecurityAuditEvents = LogCategory "SQLSecurityAuditEvents"

    module Databases =
        /// Errors
        let Errors = LogCategory "Errors"
        /// Query Store Runtime Statistics
        let QueryStoreRuntimeStatistics = LogCategory "QueryStoreRuntimeStatistics"
        /// Query Store Wait Statistics
        let QueryStoreWaitStatistics = LogCategory "QueryStoreWaitStatistics"
        /// SQL Insights
        let SQLInsights = LogCategory "SQLInsights"

namespace Farmer.DiagnosticSettings.Logging.Sql.Servers

open Farmer.DiagnosticSettings

module Databases =
    /// Automatic tuning
    let AutomaticTuning = LogCategory "AutomaticTuning"
    /// Blocks
    let Blocks = LogCategory "Blocks"
    /// Database Wait Statistics
    let DatabaseWaitStatistics = LogCategory "DatabaseWaitStatistics"
    /// Deadlocks
    let Deadlocks = LogCategory "Deadlocks"
    /// Devops operations Audit Logs
    let DevOpsOperationsAudit = LogCategory "DevOpsOperationsAudit"
    /// Dms Workers
    let DmsWorkers = LogCategory "DmsWorkers"
    /// Errors
    let Errors = LogCategory "Errors"
    /// Exec Requests
    let ExecRequests = LogCategory "ExecRequests"
    /// Query Store Runtime Statistics
    let QueryStoreRuntimeStatistics = LogCategory "QueryStoreRuntimeStatistics"
    /// Query Store Wait Statistics
    let QueryStoreWaitStatistics = LogCategory "QueryStoreWaitStatistics"
    /// Request Steps
    let RequestSteps = LogCategory "RequestSteps"
    /// SQL Insights
    let SQLInsights = LogCategory "SQLInsights"
    /// Sql Requests
    let SqlRequests = LogCategory "SqlRequests"
    /// SQL Security Audit Event
    let SQLSecurityAuditEvents = LogCategory "SQLSecurityAuditEvents"
    /// Timeouts
    let Timeouts = LogCategory "Timeouts"
    /// Waits
    let Waits = LogCategory "Waits"

namespace Farmer.DiagnosticSettings.Logging.Storage.StorageAccounts

open Farmer.DiagnosticSettings

module Storage =
    /// Transaction
    let Transaction = LogCategory "Transaction"

module BlobServices =
    /// StorageDelete
    let StorageDelete = LogCategory "StorageDelete"
    /// StorageRead
    let StorageRead = LogCategory "StorageRead"
    /// StorageWrite
    let StorageWrite = LogCategory "StorageWrite"

module FileServices =
    /// StorageDelete
    let StorageDelete = LogCategory "StorageDelete"
    /// StorageRead
    let StorageRead = LogCategory "StorageRead"
    /// StorageWrite
    let StorageWrite = LogCategory "StorageWrite"

module QueueServices =
    /// StorageDelete
    let StorageDelete = LogCategory "StorageDelete"
    /// StorageRead
    let StorageRead = LogCategory "StorageRead"
    /// StorageWrite
    let StorageWrite = LogCategory "StorageWrite"

module TableServices =
    /// StorageDelete
    let StorageDelete = LogCategory "StorageDelete"
    /// StorageRead
    let StorageRead = LogCategory "StorageRead"
    /// StorageWrite
    let StorageWrite = LogCategory "StorageWrite"

namespace Farmer.DiagnosticSettings.Logging.StreamAnalytics

open Farmer.DiagnosticSettings

module Streamingjobs =
    /// Authoring
    let Authoring = LogCategory "Authoring"
    /// Execution
    let Execution = LogCategory "Execution"

namespace Farmer.DiagnosticSettings.Logging.Synapse

open Farmer.DiagnosticSettings

module Workspaces =
    /// Built-in Sql Pool Requests Ended
    let BuiltinSqlReqsEnded = LogCategory "BuiltinSqlReqsEnded"
    /// Synapse Gateway Api Requests
    let GatewayApiRequests = LogCategory "GatewayApiRequests"
    /// SQL Security Audit Event
    let SQLSecurityAuditEvents = LogCategory "SQLSecurityAuditEvents"
    /// Synapse RBAC Operations
    let SynapseRbacOperations = LogCategory "SynapseRbacOperations"

    module BigDataPools =
        /// Big Data Pool Applications Ended
        let BigDataPoolAppsEnded = LogCategory "BigDataPoolAppsEnded"

module SqlPools =
    /// Dms Workers
    let DmsWorkers = LogCategory "DmsWorkers"
    /// Exec Requests
    let ExecRequests = LogCategory "ExecRequests"
    /// Request Steps
    let RequestSteps = LogCategory "RequestSteps"
    /// Sql Requests
    let SqlRequests = LogCategory "SqlRequests"
    /// Sql Security Audit Event
    let SQLSecurityAuditEvents = LogCategory "SQLSecurityAuditEvents"
    /// Waits
    let Waits = LogCategory "Waits"

namespace Farmer.DiagnosticSettings.Logging.TimeSeriesInsights

open Farmer.DiagnosticSettings

module Environments =
    /// Ingress
    let Ingress = LogCategory "Ingress"
    /// Management
    let Management = LogCategory "Management"

    module Eventsources =
        /// Ingress
        let Ingress = LogCategory "Ingress"
        /// Management
        let Management = LogCategory "Management"

namespace Farmer.DiagnosticSettings.Logging.Web

open Farmer.DiagnosticSettings

module Hostingenvironments =
    /// App Service Environment Platform Logs
    let AppServiceEnvironmentPlatformLogs =
        LogCategory "AppServiceEnvironmentPlatformLogs"

module Sites =
    /// Report Antivirus Audit Logs
    let AppServiceAntivirusScanAuditLogs =
        LogCategory "AppServiceAntivirusScanAuditLogs"

    /// App Service Application Logs
    let AppServiceAppLogs = LogCategory "AppServiceAppLogs"
    /// Access Audit Logs
    let AppServiceAuditLogs = LogCategory "AppServiceAuditLogs"
    /// App Service Console Logs
    let AppServiceConsoleLogs = LogCategory "AppServiceConsoleLogs"
    /// Site Content Change Audit Logs
    let AppServiceFileAuditLogs = LogCategory "AppServiceFileAuditLogs"
    /// HTTP logs
    let AppServiceHTTPLogs = LogCategory "AppServiceHTTPLogs"
    /// IPSecurity Audit logs
    let AppServiceIPSecAuditLogs = LogCategory "AppServiceIPSecAuditLogs"
    /// App Service Platform logs
    let AppServicePlatformLogs = LogCategory "AppServicePlatformLogs"
    /// Function Application Logs
    let FunctionAppLogs = LogCategory "FunctionAppLogs"

    module Slots =
        /// Report Antivirus Audit Logs
        let AppServiceAntivirusScanAuditLogs =
            LogCategory "AppServiceAntivirusScanAuditLogs"

        /// App Service Application Logs
        let AppServiceAppLogs = LogCategory "AppServiceAppLogs"
        /// Access Audit Logs
        let AppServiceAuditLogs = LogCategory "AppServiceAuditLogs"
        /// App Service Console Logs
        let AppServiceConsoleLogs = LogCategory "AppServiceConsoleLogs"
        /// Site Content Change Audit Logs
        let AppServiceFileAuditLogs = LogCategory "AppServiceFileAuditLogs"
        /// HTTP logs
        let AppServiceHTTPLogs = LogCategory "AppServiceHTTPLogs"
        /// IPSecurity Audit Logs
        let AppServiceIPSecAuditLogs = LogCategory "AppServiceIPSecAuditLogs"
        /// App Service Platform logs
        let AppServicePlatformLogs = LogCategory "AppServicePlatformLogs"
        /// Function Application Logs
        let FunctionAppLogs = LogCategory "FunctionAppLogs"
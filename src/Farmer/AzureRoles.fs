// Azure Roles:   07/08/2023 https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles
// AzureAD Roles: 26/07/2023 https://learn.microsoft.com/en-us/azure/active-directory/roles/permissions-reference
namespace Farmer
open System

module Roles =
    type Azure =
    // General
    /// Grants full access to manage all resources, but does not allow you to assign roles in Azure RBAC, manage assignments in Azure Blueprints, or share image galleries.
    | Contributor
    /// Grants full access to manage all resources, including the ability to assign roles in Azure RBAC.
    | Owner
    /// View all resources, but does not allow you to make any changes.
    | Reader
    /// Lets you manage user access to Azure resources.
    | UserAccessAdministrator
    // Compute
    /// Lets you manage classic virtual machines, but not access to them, and not the virtual network or storage account they're connected to.
    | ClassicVirtualMachineContributor
    /// Provides permissions to upload data to empty managed disks, read, or export data of managed disks (not attached to running VMs) and snapshots using SAS URIs and Azure AD authentication.
    | DataOperatorforManagedDisks
    /// Provides permission to backup vault to perform disk backup.
    | DiskBackupReader
    /// Provide permission to StoragePool Resource Provider to manage disks added to a disk pool.
    | DiskPoolOperator
    /// Provides permission to backup vault to perform disk restore.
    | DiskRestoreOperator
    /// Provides permission to backup vault to manage disk snapshots.
    | DiskSnapshotContributor
    /// View Virtual Machines in the portal and login as administrator
    | VirtualMachineAdministratorLogin
    /// Create and manage virtual machines, manage disks, install and run software, reset password of the root user of the virtual machine using VM extensions, and manage local user accounts using VM extensions. This role does not grant you management access to the virtual network or storage account the virtual machines are connected to. This role does not allow you to assign roles in Azure RBAC.
    | VirtualMachineContributor
    /// View Virtual Machines in the portal and login as a regular user.
    | VirtualMachineUserLogin
    /// Let's you manage the OS of your resource via Windows Admin Center as an administrator.
    | WindowsAdminCenterAdministratorLogin
    // Networking
    /// Can manage CDN endpoints, but can't grant access to other users.
    | CDNEndpointContributor
    /// Can view CDN endpoints, but can't make changes.
    | CDNEndpointReader
    /// Can manage CDN profiles and their endpoints, but can't grant access to other users.
    | CDNProfileContributor
    /// Can view CDN profiles and their endpoints, but can't make changes.
    | CDNProfileReader
    /// Lets you manage classic networks, but not access to them.
    | ClassicNetworkContributor
    /// Lets you manage DNS zones and record sets in Azure DNS, but does not let you control who has access to them.
    | DNSZoneContributor
    /// Lets you manage networks, but not access to them.
    | NetworkContributor
    /// Lets you manage private DNS zone resources, but not the virtual networks they are linked to.
    | PrivateDNSZoneContributor
    /// Lets you manage Traffic Manager profiles, but does not let you control who has access to them.
    | TrafficManagerContributor
    // Storage
    /// Can create and manage an Avere vFXT cluster.
    | AvereContributor
    /// Used by the Avere vFXT cluster to manage the cluster
    | AvereOperator
    /// Lets you manage backup service, but can't create vaults and give access to others
    | BackupContributor
    /// Lets you manage backup services, except removal of backup, vault creation and giving access to others
    | BackupOperator
    /// Can view backup services, but can't make changes
    | BackupReader
    /// Lets you manage classic storage accounts, but not access to them.
    | ClassicStorageAccountContributor
    /// Classic Storage Account Key Operators are allowed to list and regenerate keys on Classic Storage Accounts
    | ClassicStorageAccountKeyOperatorServiceRole
    /// Lets you manage everything under Data Box Service except giving access to others.
    | DataBoxContributor
    /// Lets you manage Data Box Service except creating order or editing order details and giving access to others.
    | DataBoxReader
    /// Lets you submit, monitor, and manage your own jobs but not create or delete Data Lake Analytics accounts.
    | DataLakeAnalyticsDeveloper
    /// Allows for full access to all resources under Azure Elastic SAN including changing network security policies to unblock data path access
    | ElasticSANOwner
    /// Allows for control path read access to Azure Elastic SAN
    | ElasticSANReader
    /// Allows for full access to a volume group in Azure Elastic SAN including changing network security policies to unblock data path access
    | ElasticSANVolumeGroupOwner
    /// Lets you view everything but will not let you delete or create a storage account or contained resource. It will also allow read/write access to all data contained in a storage account via access to storage account keys.
    | ReaderandDataAccess
    /// Lets you perform backup and restore operations using Azure Backup on the storage account.
    | StorageAccountBackupContributor
    /// Permits management of storage accounts. Provides access to the account key, which can be used to access data via Shared Key authorization.
    | StorageAccountContributor
    /// Permits listing and regenerating storage account access keys.
    | StorageAccountKeyOperatorServiceRole
    /// Read, write, and delete Azure Storage containers and blobs. To learn which actions are required for a given data operation, see Permissions for calling blob and queue data operations.
    | StorageBlobDataContributor
    /// Provides full access to Azure Storage blob containers and data, including assigning POSIX access control. To learn which actions are required for a given data operation, see Permissions for calling blob and queue data operations.
    | StorageBlobDataOwner
    /// Read and list Azure Storage containers and blobs. To learn which actions are required for a given data operation, see Permissions for calling blob and queue data operations.
    | StorageBlobDataReader
    /// Get a user delegation key, which can then be used to create a shared access signature for a container or blob that is signed with Azure AD credentials. For more information, see Create a user delegation SAS.
    | StorageBlobDelegator
    /// Allows for read, write, delete, and modify ACLs on files/directories in Azure file shares by overriding existing ACLs/NTFS permissions. This role has no built-in equivalent on Windows file servers.
    | StorageFileDataPrivilegedContributor
    /// Allows for read access on files/directories in Azure file shares by overriding existing ACLs/NTFS permissions. This role has no built-in equivalent on Windows file servers.
    | StorageFileDataPrivilegedReader
    /// Allows for read, write, and delete access on files/directories in Azure file shares. This role has no built-in equivalent on Windows file servers.
    | StorageFileDataSMBShareContributor
    /// Allows for read, write, delete, and modify ACLs on files/directories in Azure file shares. This role is equivalent to a file share ACL of change on Windows file servers.
    | StorageFileDataSMBShareElevatedContributor
    /// Allows for read access on files/directories in Azure file shares. This role is equivalent to a file share ACL of read on Windows file servers.
    | StorageFileDataSMBShareReader
    /// Read, write, and delete Azure Storage queues and queue messages. To learn which actions are required for a given data operation, see Permissions for calling blob and queue data operations.
    | StorageQueueDataContributor
    /// Peek, retrieve, and delete a message from an Azure Storage queue. To learn which actions are required for a given data operation, see Permissions for calling blob and queue data operations.
    | StorageQueueDataMessageProcessor
    /// Add messages to an Azure Storage queue. To learn which actions are required for a given data operation, see Permissions for calling blob and queue data operations.
    | StorageQueueDataMessageSender
    /// Read and list Azure Storage queues and queue messages. To learn which actions are required for a given data operation, see Permissions for calling blob and queue data operations.
    | StorageQueueDataReader
    /// Allows for read, write and delete access to Azure Storage tables and entities
    | StorageTableDataContributor
    /// Allows for read access to Azure Storage tables and entities
    | StorageTableDataReader
    // Web
    /// Grants access to read, write, and delete access to map related data from an Azure maps account.
    | AzureMapsDataContributor
    /// Grants access to read map related data from an Azure maps account.
    | AzureMapsDataReader
    /// Allow read, write and delete access to Azure Spring Cloud Config Server
    | AzureSpringCloudConfigServerContributor
    /// Allow read access to Azure Spring Cloud Config Server
    | AzureSpringCloudConfigServerReader
    /// Allow read access to Azure Spring Cloud Data
    | AzureSpringCloudDataReader
    /// Allow read, write and delete access to Azure Spring Cloud Service Registry
    | AzureSpringCloudServiceRegistryContributor
    /// Allow read access to Azure Spring Cloud Service Registry
    | AzureSpringCloudServiceRegistryReader
    /// Create, read, modify, and delete Media Services accounts; read-only access to other Media Services resources.
    | MediaServicesAccountAdministrator
    /// Create, read, modify, and delete Live Events, Assets, Asset Filters, and Streaming Locators; read-only access to other Media Services resources.
    | MediaServicesLiveEventsAdministrator
    /// Create, read, modify, and delete Assets, Asset Filters, Streaming Locators, and Jobs; read-only access to other Media Services resources.
    | MediaServicesMediaOperator
    /// Create, read, modify, and delete Account Filters, Streaming Policies, Content Key Policies, and Transforms; read-only access to other Media Services resources. Cannot create Jobs, Assets or Streaming resources.
    | MediaServicesPolicyAdministrator
    /// Create, read, modify, and delete Streaming Endpoints; read-only access to other Media Services resources.
    | MediaServicesStreamingEndpointsAdministrator
    /// Grants full access to Azure Cognitive Search index data.
    | SearchIndexDataContributor
    /// Grants read access to Azure Cognitive Search index data.
    | SearchIndexDataReader
    /// Lets you manage Search services, but not access to them.
    | SearchServiceContributor
    /// Read SignalR Service Access Keys
    | SignalRAccessKeyReader
    /// Lets your app server access SignalR Service with AAD auth options.
    | SignalRAppServer
    /// Full access to Azure SignalR Service REST APIs
    | SignalRRESTAPIOwner
    /// Read-only access to Azure SignalR Service REST APIs
    | SignalRRESTAPIReader
    /// Full access to Azure SignalR Service REST APIs
    | SignalRServiceOwner
    /// Create, Read, Update, and Delete SignalR service resources
    | SignalR_WebPubSubContributor
    /// Manage the web plans for websites. Does not allow you to assign roles in Azure RBAC.
    | WebPlanContributor
    /// Manage websites, but not web plans. Does not allow you to assign roles in Azure RBAC.
    | WebsiteContributor
    // Containers
    /// Delete repositories, tags, or manifests from a container registry.
    | AcrDelete
    /// Push trusted images to or pull trusted images from a container registry enabled for content trust.
    | AcrImageSigner
    /// Pull artifacts from a container registry.
    | AcrPull
    /// Push artifacts to or pull artifacts from a container registry.
    | AcrPush
    /// Pull quarantined images from a container registry.
    | AcrQuarantineReader
    /// Push quarantined images to or pull quarantined images from a container registry.
    | AcrQuarantineWriter
    /// This role grants admin access - provides write permissions on most objects within a namespace, with the exception of ResourceQuota object and the namespace object itself. Applying this role at cluster scope will give access across all namespaces.
    | AzureKubernetesFleetManagerRBACAdmin
    /// Lets you manage all resources in the fleet manager cluster.
    | AzureKubernetesFleetManagerRBACClusterAdmin
    /// Allows read-only access to see most objects in a namespace. It does not allow viewing roles or role bindings. This role does not allow viewing Secrets, since reading the contents of Secrets enables access to ServiceAccount credentials in the namespace, which would allow API access as any ServiceAccount in the namespace (a form of privilege escalation). Applying this role at cluster scope will give access across all namespaces.
    | AzureKubernetesFleetManagerRBACReader
    /// Allows read/write access to most objects in a namespace. This role does not allow viewing or modifying roles or role bindings. However, this role allows accessing Secrets as any ServiceAccount in the namespace, so it can be used to gain the API access levels of any ServiceAccount in the namespace. Applying this role at cluster scope will give access across all namespaces.
    | AzureKubernetesFleetManagerRBACWriter
    /// List cluster admin credential action.
    | AzureKubernetesServiceClusterAdminRole
    /// List cluster user credential action.
    | AzureKubernetesServiceClusterUserRole
    /// Grants access to read and write Azure Kubernetes Service clusters
    | AzureKubernetesServiceContributorRole
    /// Lets you manage all resources under cluster/namespace, except update or delete resource quotas and namespaces.
    | AzureKubernetesServiceRBACAdmin
    /// Lets you manage all resources in the cluster.
    | AzureKubernetesServiceRBACClusterAdmin
    /// Allows read-only access to see most objects in a namespace. It does not allow viewing roles or role bindings. This role does not allow viewing Secrets, since reading the contents of Secrets enables access to ServiceAccount credentials in the namespace, which would allow API access as any ServiceAccount in the namespace (a form of privilege escalation). Applying this role at cluster scope will give access across all namespaces.
    | AzureKubernetesServiceRBACReader
    /// Allows read/write access to most objects in a namespace. This role does not allow viewing or modifying roles or role bindings. However, this role allows accessing Secrets and running Pods as any ServiceAccount in the namespace, so it can be used to gain the API access levels of any ServiceAccount in the namespace. Applying this role at cluster scope will give access across all namespaces.
    | AzureKubernetesServiceRBACWriter
    // Databases
    /// Allows for read and write access to Azure resources for SQL Server on Arc-enabled servers.
    | AzureConnectedSQLServerOnboarding
    /// Can read Azure Cosmos DB account data. See DocumentDB Account Contributor for managing Azure Cosmos DB accounts.
    | CosmosDBAccountReaderRole
    /// Lets you manage Azure Cosmos DB accounts, but not access data in them. Prevents access to account keys and connection strings.
    | CosmosDBOperator
    /// Can submit restore request for a Cosmos DB database or a container for an account
    | CosmosBackupOperator
    /// Can perform restore action for Cosmos DB database account with continuous backup mode
    | CosmosRestoreOperator
    /// Can manage Azure Cosmos DB accounts. Azure Cosmos DB is formerly known as DocumentDB.
    | DocumentDBAccountContributor
    /// Lets you manage Redis caches, but not access to them.
    | RedisCacheContributor
    /// Lets you manage SQL databases, but not access to them. Also, you can't manage their security-related policies or their parent SQL servers.
    | SQLDBContributor
    /// Lets you manage SQL Managed Instances and required network configuration, but can't give access to others.
    | SQLManagedInstanceContributor
    /// Lets you manage the security-related policies of SQL servers and databases, but not access to them.
    | SQLSecurityManager
    /// Lets you manage SQL servers and databases, but not access to them, and not their security-related policies.
    | SQLServerContributor
    // Analytics
    /// Allows for full access to Azure Event Hubs resources.
    | AzureEventHubsDataOwner
    /// Allows receive access to Azure Event Hubs resources.
    | AzureEventHubsDataReceiver
    /// Allows send access to Azure Event Hubs resources.
    | AzureEventHubsDataSender
    /// Create and manage data factories, as well as child resources within them.
    | DataFactoryContributor
    /// Delete private data from a Log Analytics workspace.
    | DataPurger
    /// Lets you read and modify HDInsight cluster configurations.
    | HDInsightClusterOperator
    /// Can Read, Create, Modify and Delete Domain Services related operations needed for HDInsight Enterprise Security Package
    | HDInsightDomainServicesContributor
    /// Log Analytics Contributor can read all monitoring data and edit monitoring settings. Editing monitoring settings includes adding the VM extension to VMs; reading storage account keys to be able to configure collection of logs from Azure Storage; adding solutions; and configuring Azure diagnostics on all Azure resources.
    | LogAnalyticsContributor
    /// Log Analytics Reader can view and search all monitoring data as well as and view monitoring settings, including viewing the configuration of Azure diagnostics on all Azure resources.
    | LogAnalyticsReader
    /// Read, write, and delete Schema Registry groups and schemas.
    | SchemaRegistryContributor_Preview
    /// Read and list Schema Registry groups and schemas.
    | SchemaRegistryReader_Preview
    /// Lets you perform query testing without creating a stream analytics job first
    | StreamAnalyticsQueryTester
    // AI+machinelearning
    /// Can perform all actions within an Azure Machine Learning workspace, except for creating or deleting compute resources and modifying the workspace itself.
    | AzureMLDataScientist
    /// Lets you create, read, update, delete and manage keys of Cognitive Services.
    | CognitiveServicesContributor
    /// Full access to the project, including the ability to view, create, edit, or delete projects.
    | CognitiveServicesCustomVisionContributor
    /// Publish, unpublish or export models. Deployment can view the project but can't update.
    | CognitiveServicesCustomVisionDeployment
    /// View, edit training images and create, add, remove, or delete the image tags. Labelers can view the project but can't update anything other than training images and tags.
    | CognitiveServicesCustomVisionLabeler
    /// Read-only actions in the project. Readers can't create or update the project.
    | CognitiveServicesCustomVisionReader
    /// View, edit projects and train the models, including the ability to publish, unpublish, export the models. Trainers can't create or delete the project.
    | CognitiveServicesCustomVisionTrainer
    /// Lets you read Cognitive Services data.
    | CognitiveServicesDataReader_Preview
    /// Lets you perform detect, verify, identify, group, and find similar operations on Face API. This role does not allow create or delete operations, which makes it well suited for endpoints that only need inferencing capabilities, following 'least privilege' best practices.
    | CognitiveServicesFaceRecognizer
    /// Full access to the project, including the system level configuration.
    | CognitiveServicesMetricsAdvisorAdministrator
    /// Full access including the ability to fine-tune, deploy and generate text
    | CognitiveServicesOpenAIContributor
    /// Read access to view files, models, deployments. The ability to create completion and embedding calls.
    | CognitiveServicesOpenAIUser
    /// Let's you create, edit, import and export a KB. You cannot publish or delete a KB.
    | CognitiveServicesQnAMakerEditor
    /// Let's you read and test a KB only.
    | CognitiveServicesQnAMakerReader
    /// Lets you read and list keys of Cognitive Services.
    | CognitiveServicesUser
    // Internetofthings
    /// Gives you full access to management and content operations
    | DeviceUpdateAdministrator
    /// Gives you full access to content operations
    | DeviceUpdateContentAdministrator
    /// Gives you read access to content operations, but does not allow making changes
    | DeviceUpdateContentReader
    /// Gives you full access to management operations
    | DeviceUpdateDeploymentsAdministrator
    /// Gives you read access to management operations, but does not allow making changes
    | DeviceUpdateDeploymentsReader
    /// Gives you read access to management and content operations, but does not allow making changes
    | DeviceUpdateReader
    /// Allows for full access to IoT Hub data plane operations.
    | IoTHubDataContributor
    /// Allows for full read access to IoT Hub data-plane properties
    | IoTHubDataReader
    /// Allows for full access to IoT Hub device registry.
    | IoTHubRegistryContributor
    /// Allows for read and write access to all IoT Hub device and module twins.
    | IoTHubTwinContributor
    // Mixedreality
    /// Provides user with conversion, manage session, rendering and diagnostics capabilities for Azure Remote Rendering
    | RemoteRenderingAdministrator
    /// Provides user with manage session, rendering and diagnostics capabilities for Azure Remote Rendering.
    | RemoteRenderingClient
    /// Lets you manage spatial anchors in your account, but not delete them
    | SpatialAnchorsAccountContributor
    /// Lets you manage spatial anchors in your account, including deleting them
    | SpatialAnchorsAccountOwner
    /// Lets you locate and read properties of spatial anchors in your account
    | SpatialAnchorsAccountReader
    // Integration
    /// Can manage service and the APIs
    | APIManagementServiceContributor
    /// Can manage service but not the APIs
    | APIManagementServiceOperatorRole
    /// Read-only access to service and APIs
    | APIManagementServiceReaderRole
    /// Has read access to tags and products and write access to allow: assigning APIs to products, assigning tags to products and APIs. This role should be assigned on the service scope.
    | APIManagementServiceWorkspaceAPIDeveloper
    /// Has the same access as API Management Service Workspace API Developer as well as read access to users and write access to allow assigning users to groups. This role should be assigned on the service scope.
    | APIManagementServiceWorkspaceAPIProductManager
    /// Has read access to entities in the workspace and read and write access to entities for editing APIs. This role should be assigned on the workspace scope.
    | APIManagementWorkspaceAPIDeveloper
    /// Has read access to entities in the workspace and read and write access to entities for publishing APIs. This role should be assigned on the workspace scope.
    | APIManagementWorkspaceAPIProductManager
    /// Can manage the workspace and view, but not modify its members. This role should be assigned on the workspace scope.
    | APIManagementWorkspaceContributor
    /// Has read-only access to entities in the workspace. This role should be assigned on the workspace scope.
    | APIManagementWorkspaceReader
    /// Allows full access to App Configuration data.
    | AppConfigurationDataOwner
    /// Allows read access to App Configuration data.
    | AppConfigurationDataReader
    /// Allows for listen access to Azure Relay resources.
    | AzureRelayListener
    /// Allows for full access to Azure Relay resources.
    | AzureRelayOwner
    /// Allows for send access to Azure Relay resources.
    | AzureRelaySender
    /// Allows for full access to Azure Service Bus resources.
    | AzureServiceBusDataOwner
    /// Allows for receive access to Azure Service Bus resources.
    | AzureServiceBusDataReceiver
    /// Allows for send access to Azure Service Bus resources.
    | AzureServiceBusDataSender
    /// Lets you manage Azure Stack registrations.
    | AzureStackRegistrationOwner
    /// Lets you manage EventGrid operations.
    | EventGridContributor
    /// Allows send access to event grid events.
    | EventGridDataSender
    /// Lets you manage EventGrid event subscription operations.
    | EventGridEventSubscriptionContributor
    /// Lets you read EventGrid event subscriptions.
    | EventGridEventSubscriptionReader
    /// Role allows user or principal full access to FHIR Data
    | FHIRDataContributor
    /// Role allows user or principal to read and export FHIR Data
    | FHIRDataExporter
    /// Role allows user or principal to read and import FHIR Data
    | FHIRDataImporter
    /// Role allows user or principal to read FHIR Data
    | FHIRDataReader
    /// Role allows user or principal to read and write FHIR Data
    | FHIRDataWriter
    /// Lets you manage integration service environments, but not access to them.
    | IntegrationServiceEnvironmentContributor
    /// Allows developers to create and update workflows, integration accounts and API connections in integration service environments.
    | IntegrationServiceEnvironmentDeveloper
    /// Lets you manage Intelligent Systems accounts, but not access to them.
    | IntelligentSystemsAccountContributor
    /// Lets you manage logic apps, but not change access to them.
    | LogicAppContributor
    /// Lets you read, enable, and disable logic apps, but not edit or update them.
    | LogicAppOperator
    // Identity
    /// Can manage Azure AD Domain Services and related network configurations
    | DomainServicesContributor
    /// Can view Azure AD Domain Services and related network configurations
    | DomainServicesReader
    /// Create, Read, Update, and Delete User Assigned Identity
    | ManagedIdentityContributor
    /// Read and Assign User Assigned Identity
    | ManagedIdentityOperator
    // Security
    /// Create, read, download, modify and delete reports objects and related other resource objects.
    | AppComplianceAutomationAdministrator
    /// Read, download the reports objects and related other resource objects.
    | AppComplianceAutomationReader
    /// Can read write or delete the attestation provider instance
    | AttestationContributor
    /// Can read the attestation provider properties
    | AttestationReader
    /// Perform all data plane operations on a key vault and all objects in it, including certificates, keys, and secrets. Cannot manage key vault resources or manage role assignments. Only works for key vaults that use the 'Azure role-based access control' permission model.
    | KeyVaultAdministrator
    /// Perform any action on the certificates of a key vault, except manage permissions. Only works for key vaults that use the 'Azure role-based access control' permission model.
    | KeyVaultCertificatesOfficer
    /// Manage key vaults, but does not allow you to assign roles in Azure RBAC, and does not allow you to access secrets, keys, or certificates.
    | KeyVaultContributor
    /// Perform any action on the keys of a key vault, except manage permissions. Only works for key vaults that use the 'Azure role-based access control' permission model.
    | KeyVaultCryptoOfficer
    /// Read metadata of keys and perform wrap/unwrap operations. Only works for key vaults that use the 'Azure role-based access control' permission model.
    | KeyVaultCryptoServiceEncryptionUser
    /// Perform cryptographic operations using keys. Only works for key vaults that use the 'Azure role-based access control' permission model.
    | KeyVaultCryptoUser
    /// Read metadata of key vaults and its certificates, keys, and secrets. Cannot read sensitive values such as secret contents or key material. Only works for key vaults that use the 'Azure role-based access control' permission model.
    | KeyVaultReader
    /// Perform any action on the secrets of a key vault, except manage permissions. Only works for key vaults that use the 'Azure role-based access control' permission model.
    | KeyVaultSecretsOfficer
    /// Read secret contents. Only works for key vaults that use the 'Azure role-based access control' permission model.
    | KeyVaultSecretsUser
    /// Lets you manage managed HSM pools, but not access to them.
    | ManagedHSMcontributor
    /// Microsoft Sentinel Automation Contributor
    | MicrosoftSentinelAutomationContributor
    /// Microsoft Sentinel Contributor
    | MicrosoftSentinelContributor
    /// Microsoft Sentinel Playbook Operator
    | MicrosoftSentinelPlaybookOperator
    /// Microsoft Sentinel Reader
    | MicrosoftSentinelReader
    /// Microsoft Sentinel Responder
    | MicrosoftSentinelResponder
    /// View and update permissions for Microsoft Defender for Cloud. Same permissions as the Security Reader role and can also update the security policy and dismiss alerts and recommendations. For Microsoft Defender for IoT, see Azure user roles for OT and Enterprise IoT monitoring.
    | SecurityAdmin
    /// Lets you push assessments to Microsoft Defender for Cloud
    | SecurityAssessmentContributor
    /// This is a legacy role. Please use Security Admin instead.
    | SecurityManager_Legacy
    /// View permissions for Microsoft Defender for Cloud. Can view recommendations, alerts, a security policy, and security states, but cannot make changes. For Microsoft Defender for IoT, see Azure user roles for OT and Enterprise IoT monitoring.
    | SecurityReader
    // DevOps
    /// Lets you connect, start, restart, and shutdown your virtual machines in your Azure DevTest Labs.
    | DevTestLabsUser
    /// Enables you to view an existing lab, perform actions on the lab VMs and send invitations to the lab.
    | LabAssistant
    /// Applied at lab level, enables you to manage the lab. Applied at a resource group, enables you to create and manage labs.
    | LabContributor
    /// Lets you create new labs under your Azure Lab Accounts.
    | LabCreator
    /// Gives you limited ability to manage existing labs.
    | LabOperator
    /// Enables you to fully control all Lab Services scenarios in the resource group.
    | LabServicesContributor
    /// Enables you to view, but not change, all lab plans and lab resources.
    | LabServicesReader
    // Monitor
    /// Can manage Application Insights components
    | ApplicationInsightsComponentContributor
    /// Gives user permission to view and download debug snapshots collected with the Application Insights Snapshot Debugger. Note that these permissions are not included in the Owner or Contributor roles. When giving users the Application Insights Snapshot Debugger role, you must grant the role directly to the user. The role is not recognized when it is added to a custom role.
    | ApplicationInsightsSnapshotDebugger
    /// Can read all monitoring data and edit monitoring settings. See also Get started with roles, permissions, and security with Azure Monitor.
    | MonitoringContributor
    /// Enables publishing metrics against Azure resources
    | MonitoringMetricsPublisher
    /// Can read all monitoring data (metrics, logs, etc.). See also Get started with roles, permissions, and security with Azure Monitor.
    | MonitoringReader
    /// Can save shared workbooks.
    | WorkbookContributor
    /// Can read workbooks.
    | WorkbookReader
    // Managementandgovernance
    /// Manage Azure Automation resources and other resources using Azure Automation.
    | AutomationContributor
    /// Create and Manage Jobs using Automation Runbooks.
    | AutomationJobOperator
    /// Automation Operators are able to start, stop, suspend, and resume jobs
    | AutomationOperator
    /// Read Runbook properties - to be able to create Jobs of the runbook.
    | AutomationRunbookOperator
    /// List cluster user credentials action.
    | AzureArcEnabledKubernetesClusterUserRole
    /// Lets you manage all resources under cluster/namespace, except update or delete resource quotas and namespaces.
    | AzureArcKubernetesAdmin
    /// Lets you manage all resources in the cluster.
    | AzureArcKubernetesClusterAdmin
    /// Lets you view all resources in cluster/namespace, except secrets.
    | AzureArcKubernetesViewer
    /// Lets you update everything in cluster/namespace, except (cluster)roles and (cluster)role bindings.
    | AzureArcKubernetesWriter
    /// Can onboard Azure Connected Machines.
    | AzureConnectedMachineOnboarding
    /// Can read, write, delete and re-onboard Azure Connected Machines.
    | AzureConnectedMachineResourceAdministrator
    /// Allows read access to billing data
    | BillingReader
    /// Can manage blueprint definitions, but not assign them.
    | BlueprintContributor
    /// Can assign existing published blueprints, but cannot create new blueprints. Note that this only works if the assignment is done with a user-assigned managed identity.
    | BlueprintOperator
    /// Can view costs and manage cost configuration (e.g. budgets, exports)
    | CostManagementContributor
    /// Can view cost data and configuration (e.g. budgets, exports)
    | CostManagementReader
    /// Allows users to edit and delete Hierarchy Settings
    | HierarchySettingsAdministrator
    /// Role definition to authorize any user/service to create connectedClusters resource
    | KubernetesCluster_AzureArcOnboarding
    /// Can create, update, get, list and delete Kubernetes Extensions, and get extension async operations
    | KubernetesExtensionContributor
    /// Allows for creating managed application resources.
    | ManagedApplicationContributorRole
    /// Lets you read and perform actions on Managed Application resources
    | ManagedApplicationOperatorRole
    /// Lets you read resources in a managed app and request JIT access.
    | ManagedApplicationsReader
    /// Managed Services Registration Assignment Delete Role allows the managing tenant users to delete the registration assignment assigned to their tenant.
    | ManagedServicesRegistrationassignmentDeleteRole
    /// Management Group Contributor Role
    | ManagementGroupContributor
    /// Management Group Reader Role
    | ManagementGroupReader
    /// Lets you manage New Relic Application Performance Management accounts and applications, but not access to them.
    | NewRelicAPMAccountContributor
    /// Allows read access to resource policies and write access to resource component policy events.
    | PolicyInsightsDataWriter_Preview
    /// Read and create quota requests, get quota request status, and create support tickets.
    | QuotaRequestOperator
    /// Lets you purchase reservations
    | ReservationPurchaser
    /// Users with rights to create/modify resource policy, create support ticket and read resources/hierarchy.
    | ResourcePolicyContributor
    /// Lets you manage Site Recovery service except vault creation and role assignment
    | SiteRecoveryContributor
    /// Lets you failover and failback but not perform other Site Recovery management operations
    | SiteRecoveryOperator
    /// Lets you view Site Recovery status but not perform other management operations
    | SiteRecoveryReader
    /// Lets you create and manage Support requests
    | SupportRequestContributor
    /// Lets you manage tags on entities, without providing access to the entities themselves.
    | TagContributor
    /// Allows full access to Template Spec operations at the assigned scope.
    | TemplateSpecContributor
    /// Allows read access to Template Specs at the assigned scope.
    | TemplateSpecReader
    // Virtualdesktopinfrastructure
    /// Contributor of the Desktop Virtualization Application Group.
    | DesktopVirtualizationApplicationGroupContributor
    /// Reader of the Desktop Virtualization Application Group.
    | DesktopVirtualizationApplicationGroupReader
    /// Contributor of Desktop Virtualization.
    | DesktopVirtualizationContributor
    /// Contributor of the Desktop Virtualization Host Pool.
    | DesktopVirtualizationHostPoolContributor
    /// Reader of the Desktop Virtualization Host Pool.
    | DesktopVirtualizationHostPoolReader
    /// Reader of Desktop Virtualization.
    | DesktopVirtualizationReader
    /// Operator of the Desktop Virtualization Session Host.
    | DesktopVirtualizationSessionHostOperator
    /// Allows user to use the applications in an application group.
    | DesktopVirtualizationUser
    /// Operator of the Desktop Virtualization User Session.
    | DesktopVirtualizationUserSessionOperator
    /// Contributor of the Desktop Virtualization Workspace.
    | DesktopVirtualizationWorkspaceContributor
    /// Reader of the Desktop Virtualization Workspace.
    | DesktopVirtualizationWorkspaceReader
    // Other
    /// Full access role for Digital Twins data-plane
    | AzureDigitalTwinsDataOwner
    /// Read-only role for Digital Twins data-plane properties
    | AzureDigitalTwinsDataReader
    /// Lets you manage BizTalk services, but not access to them.
    | BizTalkContributor
    /// Perform all Grafana operations, including the ability to manage data sources, create dashboards, and manage role assignments within Grafana.
    | GrafanaAdmin
    /// View and edit a Grafana instance, including its dashboards and alerts.
    | GrafanaEditor
    /// View a Grafana instance, including its dashboards and alerts.
    | GrafanaViewer
    /// View, create, update, delete and execute load tests. View and list load test resources but can not make any changes.
    | LoadTestContributor
    /// Execute all operations on load test resources and load tests
    | LoadTestOwner
    /// View and list all load tests and load test resources but can not make any changes
    | LoadTestReader
    /// Lets you manage Scheduler job collections, but not access to them.
    | SchedulerJobCollectionsContributor
    /// Services Hub Operator allows you to perform all read, write, and deletion operations related to Services Hub Connectors.
    | ServicesHubOperator

    with
        member x.Guid =
            match x with
            // General
            | Contributor -> Guid "b24988ac-6180-42a0-ab88-20f7382dd24c"
            | Owner -> Guid "8e3af657-a8ff-443c-a75c-2fe8c4bcb635"
            | Reader -> Guid "acdd72a7-3385-48ef-bd42-f606fba81ae7"
            | UserAccessAdministrator -> Guid "18d7d88d-d35e-4fb5-a5c3-7773c20a72d9"
            // Compute
            | ClassicVirtualMachineContributor -> Guid "d73bb868-a0df-4d4d-bd69-98a00b01fccb"
            | DataOperatorforManagedDisks -> Guid "959f8984-c045-4866-89c7-12bf9737be2e"
            | DiskBackupReader -> Guid "3e5e47e6-65f7-47ef-90b5-e5dd4d455f24"
            | DiskPoolOperator -> Guid "60fc6e62-5479-42d4-8bf4-67625fcc2840"
            | DiskRestoreOperator -> Guid "b50d9833-a0cb-478e-945f-707fcc997c13"
            | DiskSnapshotContributor -> Guid "7efff54f-a5b4-42b5-a1c5-5411624893ce"
            | VirtualMachineAdministratorLogin -> Guid "1c0163c0-47e6-4577-8991-ea5c82e286e4"
            | VirtualMachineContributor -> Guid "9980e02c-c2be-4d73-94e8-173b1dc7cf3c"
            | VirtualMachineUserLogin -> Guid "fb879df8-f326-4884-b1cf-06f3ad86be52"
            | WindowsAdminCenterAdministratorLogin -> Guid "a6333a3e-0164-44c3-b281-7a577aff287f"
            // Networking
            | CDNEndpointContributor -> Guid "426e0c7f-0c7e-4658-b36f-ff54d6c29b45"
            | CDNEndpointReader -> Guid "871e35f6-b5c1-49cc-a043-bde969a0f2cd"
            | CDNProfileContributor -> Guid "ec156ff8-a8d1-4d15-830c-5b80698ca432"
            | CDNProfileReader -> Guid "8f96442b-4075-438f-813d-ad51ab4019af"
            | ClassicNetworkContributor -> Guid "b34d265f-36f7-4a0d-a4d4-e158ca92e90f"
            | DNSZoneContributor -> Guid "befefa01-2a29-4197-83a8-272ff33ce314"
            | NetworkContributor -> Guid "4d97b98b-1d4f-4787-a291-c67834d212e7"
            | PrivateDNSZoneContributor -> Guid "b12aa53e-6015-4669-85d0-8515ebb3ae7f"
            | TrafficManagerContributor -> Guid "a4b10055-b0c7-44c2-b00f-c7b5b3550cf7"
            // Storage
            | AvereContributor -> Guid "4f8fab4f-1852-4a58-a46a-8eaf358af14a"
            | AvereOperator -> Guid "c025889f-8102-4ebf-b32c-fc0c6f0c6bd9"
            | BackupContributor -> Guid "5e467623-bb1f-42f4-a55d-6e525e11384b"
            | BackupOperator -> Guid "00c29273-979b-4161-815c-10b084fb9324"
            | BackupReader -> Guid "a795c7a0-d4a2-40c1-ae25-d81f01202912"
            | ClassicStorageAccountContributor -> Guid "86e8f5dc-a6e9-4c67-9d15-de283e8eac25"
            | ClassicStorageAccountKeyOperatorServiceRole -> Guid "985d6b00-f706-48f5-a6fe-d0ca12fb668d"
            | DataBoxContributor -> Guid "add466c9-e687-43fc-8d98-dfcf8d720be5"
            | DataBoxReader -> Guid "028f4ed7-e2a9-465e-a8f4-9c0ffdfdc027"
            | DataLakeAnalyticsDeveloper -> Guid "47b7735b-770e-4598-a7da-8b91488b4c88"
            | ElasticSANOwner -> Guid "80dcbedb-47ef-405d-95bd-188a1b4ac406"
            | ElasticSANReader -> Guid "af6a70f8-3c9f-4105-acf1-d719e9fca4ca"
            | ElasticSANVolumeGroupOwner -> Guid "a8281131-f312-4f34-8d98-ae12be9f0d23"
            | ReaderandDataAccess -> Guid "c12c1c16-33a1-487b-954d-41c89c60f349"
            | StorageAccountBackupContributor -> Guid "e5e2a7ff-d759-4cd2-bb51-3152d37e2eb1"
            | StorageAccountContributor -> Guid "17d1049b-9a84-46fb-8f53-869881c3d3ab"
            | StorageAccountKeyOperatorServiceRole -> Guid "81a9662b-bebf-436f-a333-f67b29880f12"
            | StorageBlobDataContributor -> Guid "ba92f5b4-2d11-453d-a403-e96b0029c9fe"
            | StorageBlobDataOwner -> Guid "b7e6dc6d-f1e8-4753-8033-0f276bb0955b"
            | StorageBlobDataReader -> Guid "2a2b9908-6ea1-4ae2-8e65-a410df84e7d1"
            | StorageBlobDelegator -> Guid "db58b8e5-c6ad-4a2a-8342-4190687cbf4a"
            | StorageFileDataPrivilegedContributor -> Guid "69566ab7-960f-475b-8e7c-b3118f30c6bd"
            | StorageFileDataPrivilegedReader -> Guid "b8eda974-7b85-4f76-af95-65846b26df6d"
            | StorageFileDataSMBShareContributor -> Guid "0c867c2a-1d8c-454a-a3db-ab2ea1bdc8bb"
            | StorageFileDataSMBShareElevatedContributor -> Guid "a7264617-510b-434b-a828-9731dc254ea7"
            | StorageFileDataSMBShareReader -> Guid "aba4ae5f-2193-4029-9191-0cb91df5e314"
            | StorageQueueDataContributor -> Guid "974c5e8b-45b9-4653-ba55-5f855dd0fb88"
            | StorageQueueDataMessageProcessor -> Guid "8a0f0c08-91a1-4084-bc3d-661d67233fed"
            | StorageQueueDataMessageSender -> Guid "c6a89b2d-59bc-44d0-9896-0f6e12d7b80a"
            | StorageQueueDataReader -> Guid "19e7f393-937e-4f77-808e-94535e297925"
            | StorageTableDataContributor -> Guid "0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3"
            | StorageTableDataReader -> Guid "76199698-9eea-4c19-bc75-cec21354c6b6"
            // Web
            | AzureMapsDataContributor -> Guid "8f5e0ce6-4f7b-4dcf-bddf-e6f48634a204"
            | AzureMapsDataReader -> Guid "423170ca-a8f6-4b0f-8487-9e4eb8f49bfa"
            | AzureSpringCloudConfigServerContributor -> Guid "a06f5c24-21a7-4e1a-aa2b-f19eb6684f5b"
            | AzureSpringCloudConfigServerReader -> Guid "d04c6db6-4947-4782-9e91-30a88feb7be7"
            | AzureSpringCloudDataReader -> Guid "b5537268-8956-4941-a8f0-646150406f0c"
            | AzureSpringCloudServiceRegistryContributor -> Guid "f5880b48-c26d-48be-b172-7927bfa1c8f1"
            | AzureSpringCloudServiceRegistryReader -> Guid "cff1b556-2399-4e7e-856d-a8f754be7b65"
            | MediaServicesAccountAdministrator -> Guid "054126f8-9a2b-4f1c-a9ad-eca461f08466"
            | MediaServicesLiveEventsAdministrator -> Guid "532bc159-b25e-42c0-969e-a1d439f60d77"
            | MediaServicesMediaOperator -> Guid "e4395492-1534-4db2-bedf-88c14621589c"
            | MediaServicesPolicyAdministrator -> Guid "c4bba371-dacd-4a26-b320-7250bca963ae"
            | MediaServicesStreamingEndpointsAdministrator -> Guid "99dba123-b5fe-44d5-874c-ced7199a5804"
            | SearchIndexDataContributor -> Guid "8ebe5a00-799e-43f5-93ac-243d3dce84a7"
            | SearchIndexDataReader -> Guid "1407120a-92aa-4202-b7e9-c0e197c71c8f"
            | SearchServiceContributor -> Guid "7ca78c08-252a-4471-8644-bb5ff32d4ba0"
            | SignalRAccessKeyReader -> Guid "04165923-9d83-45d5-8227-78b77b0a687e"
            | SignalRAppServer -> Guid "420fcaa2-552c-430f-98ca-3264be4806c7"
            | SignalRRESTAPIOwner -> Guid "fd53cd77-2268-407a-8f46-7e7863d0f521"
            | SignalRRESTAPIReader -> Guid "ddde6b66-c0df-4114-a159-3618637b3035"
            | SignalRServiceOwner -> Guid "7e4f1700-ea5a-4f59-8f37-079cfe29dce3"
            | SignalR_WebPubSubContributor -> Guid "8cf5e20a-e4b2-4e9d-b3a1-5ceb692c2761"
            | WebPlanContributor -> Guid "2cc479cb-7b4d-49a8-b449-8c00fd0f0a4b"
            | WebsiteContributor -> Guid "de139f84-1756-47ae-9be6-808fbbe84772"
            // Containers
            | AcrDelete -> Guid "c2f4ef07-c644-48eb-af81-4b1b4947fb11"
            | AcrImageSigner -> Guid "6cef56e8-d556-48e5-a04f-b8e64114680f"
            | AcrPull -> Guid "7f951dda-4ed3-4680-a7ca-43fe172d538d"
            | AcrPush -> Guid "8311e382-0749-4cb8-b61a-304f252e45ec"
            | AcrQuarantineReader -> Guid "cdda3590-29a3-44f6-95f2-9f980659eb04"
            | AcrQuarantineWriter -> Guid "c8d4ff99-41c3-41a8-9f60-21dfdad59608"
            | AzureKubernetesFleetManagerRBACAdmin -> Guid "434fb43a-c01c-447e-9f67-c3ad923cfaba"
            | AzureKubernetesFleetManagerRBACClusterAdmin -> Guid "18ab4d3d-a1bf-4477-8ad9-8359bc988f69"
            | AzureKubernetesFleetManagerRBACReader -> Guid "30b27cfc-9c84-438e-b0ce-70e35255df80"
            | AzureKubernetesFleetManagerRBACWriter -> Guid "5af6afb3-c06c-4fa4-8848-71a8aee05683"
            | AzureKubernetesServiceClusterAdminRole -> Guid "0ab0b1a8-8aac-4efd-b8c2-3ee1fb270be8"
            | AzureKubernetesServiceClusterUserRole -> Guid "4abbcc35-e782-43d8-92c5-2d3f1bd2253f"
            | AzureKubernetesServiceContributorRole -> Guid "ed7f3fbd-7b88-4dd4-9017-9adb7ce333f8"
            | AzureKubernetesServiceRBACAdmin -> Guid "3498e952-d568-435e-9b2c-8d77e338d7f7"
            | AzureKubernetesServiceRBACClusterAdmin -> Guid "b1ff04bb-8a4e-4dc4-8eb5-8693973ce19b"
            | AzureKubernetesServiceRBACReader -> Guid "7f6c6a51-bcf8-42ba-9220-52d62157d7db"
            | AzureKubernetesServiceRBACWriter -> Guid "a7ffa36f-339b-4b5c-8bdf-e2c188b2c0eb"
            // Databases
            | AzureConnectedSQLServerOnboarding -> Guid "e8113dce-c529-4d33-91fa-e9b972617508"
            | CosmosDBAccountReaderRole -> Guid "fbdf93bf-df7d-467e-a4d2-9458aa1360c8"
            | CosmosDBOperator -> Guid "230815da-be43-4aae-9cb4-875f7bd000aa"
            | CosmosBackupOperator -> Guid "db7b14f2-5adf-42da-9f96-f2ee17bab5cb"
            | CosmosRestoreOperator -> Guid "5432c526-bc82-444a-b7ba-57c5b0b5b34f"
            | DocumentDBAccountContributor -> Guid "5bd9cd88-fe45-4216-938b-f97437e15450"
            | RedisCacheContributor -> Guid "e0f68234-74aa-48ed-b826-c38b57376e17"
            | SQLDBContributor -> Guid "9b7fa17d-e63e-47b0-bb0a-15c516ac86ec"
            | SQLManagedInstanceContributor -> Guid "4939a1f6-9ae0-4e48-a1e0-f2cbe897382d"
            | SQLSecurityManager -> Guid "056cd41c-7e88-42e1-933e-88ba6a50c9c3"
            | SQLServerContributor -> Guid "6d8ee4ec-f05a-4a1d-8b00-a9b17e38b437"
            // Analytics
            | AzureEventHubsDataOwner -> Guid "f526a384-b230-433a-b45c-95f59c4a2dec"
            | AzureEventHubsDataReceiver -> Guid "a638d3c7-ab3a-418d-83e6-5f17a39d4fde"
            | AzureEventHubsDataSender -> Guid "2b629674-e913-4c01-ae53-ef4638d8f975"
            | DataFactoryContributor -> Guid "673868aa-7521-48a0-acc6-0f60742d39f5"
            | DataPurger -> Guid "150f5e0c-0603-4f03-8c7f-cf70034c4e90"
            | HDInsightClusterOperator -> Guid "61ed4efc-fab3-44fd-b111-e24485cc132a"
            | HDInsightDomainServicesContributor -> Guid "8d8d5a11-05d3-4bda-a417-a08778121c7c"
            | LogAnalyticsContributor -> Guid "92aaf0da-9dab-42b6-94a3-d43ce8d16293"
            | LogAnalyticsReader -> Guid "73c42c96-874c-492b-b04d-ab87d138a893"
            | SchemaRegistryContributor_Preview -> Guid "5dffeca3-4936-4216-b2bc-10343a5abb25"
            | SchemaRegistryReader_Preview -> Guid "2c56ea50-c6b3-40a6-83c0-9d98858bc7d2"
            | StreamAnalyticsQueryTester -> Guid "1ec5b3c1-b17e-4e25-8312-2acb3c3c5abf"
            // AI+machinelearning
            | AzureMLDataScientist -> Guid "f6c7c914-8db3-469d-8ca1-694a8f32e121"
            | CognitiveServicesContributor -> Guid "25fbc0a9-bd7c-42a3-aa1a-3b75d497ee68"
            | CognitiveServicesCustomVisionContributor -> Guid "c1ff6cc2-c111-46fe-8896-e0ef812ad9f3"
            | CognitiveServicesCustomVisionDeployment -> Guid "5c4089e1-6d96-4d2f-b296-c1bc7137275f"
            | CognitiveServicesCustomVisionLabeler -> Guid "88424f51-ebe7-446f-bc41-7fa16989e96c"
            | CognitiveServicesCustomVisionReader -> Guid "93586559-c37d-4a6b-ba08-b9f0940c2d73"
            | CognitiveServicesCustomVisionTrainer -> Guid "0a5ae4ab-0d65-4eeb-be61-29fc9b54394b"
            | CognitiveServicesDataReader_Preview -> Guid "b59867f0-fa02-499b-be73-45a86b5b3e1c"
            | CognitiveServicesFaceRecognizer -> Guid "9894cab4-e18a-44aa-828b-cb588cd6f2d7"
            | CognitiveServicesMetricsAdvisorAdministrator -> Guid "cb43c632-a144-4ec5-977c-e80c4affc34a"
            | CognitiveServicesOpenAIContributor -> Guid "a001fd3d-188f-4b5d-821b-7da978bf7442"
            | CognitiveServicesOpenAIUser -> Guid "5e0bd9bd-7b93-4f28-af87-19fc36ad61bd"
            | CognitiveServicesQnAMakerEditor -> Guid "f4cc2bf9-21be-47a1-bdf1-5c5804381025"
            | CognitiveServicesQnAMakerReader -> Guid "466ccd10-b268-4a11-b098-b4849f024126"
            | CognitiveServicesUser -> Guid "a97b65f3-24c7-4388-baec-2e87135dc908"
            // Internetofthings
            | DeviceUpdateAdministrator -> Guid "02ca0879-e8e4-47a5-a61e-5c618b76e64a"
            | DeviceUpdateContentAdministrator -> Guid "0378884a-3af5-44ab-8323-f5b22f9f3c98"
            | DeviceUpdateContentReader -> Guid "d1ee9a80-8b14-47f0-bdc2-f4a351625a7b"
            | DeviceUpdateDeploymentsAdministrator -> Guid "e4237640-0e3d-4a46-8fda-70bc94856432"
            | DeviceUpdateDeploymentsReader -> Guid "49e2f5d2-7741-4835-8efa-19e1fe35e47f"
            | DeviceUpdateReader -> Guid "e9dba6fb-3d52-4cf0-bce3-f06ce71b9e0f"
            | IoTHubDataContributor -> Guid "4fc6c259-987e-4a07-842e-c321cc9d413f"
            | IoTHubDataReader -> Guid "b447c946-2db7-41ec-983d-d8bf3b1c77e3"
            | IoTHubRegistryContributor -> Guid "4ea46cd5-c1b2-4a8e-910b-273211f9ce47"
            | IoTHubTwinContributor -> Guid "494bdba2-168f-4f31-a0a1-191d2f7c028c"
            // Mixedreality
            | RemoteRenderingAdministrator -> Guid "3df8b902-2a6f-47c7-8cc5-360e9b272a7e"
            | RemoteRenderingClient -> Guid "d39065c4-c120-43c9-ab0a-63eed9795f0a"
            | SpatialAnchorsAccountContributor -> Guid "8bbe83f1-e2a6-4df7-8cb4-4e04d4e5c827"
            | SpatialAnchorsAccountOwner -> Guid "70bbe301-9835-447d-afdd-19eb3167307c"
            | SpatialAnchorsAccountReader -> Guid "5d51204f-eb77-4b1c-b86a-2ec626c49413"
            // Integration
            | APIManagementServiceContributor -> Guid "312a565d-c81f-4fd8-895a-4e21e48d571c"
            | APIManagementServiceOperatorRole -> Guid "e022efe7-f5ba-4159-bbe4-b44f577e9b61"
            | APIManagementServiceReaderRole -> Guid "71522526-b88f-4d52-b57f-d31fc3546d0d"
            | APIManagementServiceWorkspaceAPIDeveloper -> Guid "9565a273-41b9-4368-97d2-aeb0c976a9b3"
            | APIManagementServiceWorkspaceAPIProductManager -> Guid "d59a3e9c-6d52-4a5a-aeed-6bf3cf0e31da"
            | APIManagementWorkspaceAPIDeveloper -> Guid "56328988-075d-4c6a-8766-d93edd6725b6"
            | APIManagementWorkspaceAPIProductManager -> Guid "73c2c328-d004-4c5e-938c-35c6f5679a1f"
            | APIManagementWorkspaceContributor -> Guid "0c34c906-8d99-4cb7-8bb7-33f5b0a1a799"
            | APIManagementWorkspaceReader -> Guid "ef1c2c96-4a77-49e8-b9a4-6179fe1d2fd2"
            | AppConfigurationDataOwner -> Guid "5ae67dd6-50cb-40e7-96ff-dc2bfa4b606b"
            | AppConfigurationDataReader -> Guid "516239f1-63e1-4d78-a4de-a74fb236a071"
            | AzureRelayListener -> Guid "26e0b698-aa6d-4085-9386-aadae190014d"
            | AzureRelayOwner -> Guid "2787bf04-f1f5-4bfe-8383-c8a24483ee38"
            | AzureRelaySender -> Guid "26baccc8-eea7-41f1-98f4-1762cc7f685d"
            | AzureServiceBusDataOwner -> Guid "090c5cfd-751d-490a-894a-3ce6f1109419"
            | AzureServiceBusDataReceiver -> Guid "4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0"
            | AzureServiceBusDataSender -> Guid "69a216fc-b8fb-44d8-bc22-1f3c2cd27a39"
            | AzureStackRegistrationOwner -> Guid "6f12a6df-dd06-4f3e-bcb1-ce8be600526a"
            | EventGridContributor -> Guid "1e241071-0855-49ea-94dc-649edcd759de"
            | EventGridDataSender -> Guid "d5a91429-5739-47e2-a06b-3470a27159e7"
            | EventGridEventSubscriptionContributor -> Guid "428e0ff0-5e57-4d9c-a221-2c70d0e0a443"
            | EventGridEventSubscriptionReader -> Guid "2414bbcf-6497-4faf-8c65-045460748405"
            | FHIRDataContributor -> Guid "5a1fc7df-4bf1-4951-a576-89034ee01acd"
            | FHIRDataExporter -> Guid "3db33094-8700-4567-8da5-1501d4e7e843"
            | FHIRDataImporter -> Guid "4465e953-8ced-4406-a58e-0f6e3f3b530b"
            | FHIRDataReader -> Guid "4c8d0bbc-75d3-4935-991f-5f3c56d81508"
            | FHIRDataWriter -> Guid "3f88fce4-5892-4214-ae73-ba5294559913"
            | IntegrationServiceEnvironmentContributor -> Guid "a41e2c5b-bd99-4a07-88f4-9bf657a760b8"
            | IntegrationServiceEnvironmentDeveloper -> Guid "c7aa55d3-1abb-444a-a5ca-5e51e485d6ec"
            | IntelligentSystemsAccountContributor -> Guid "03a6d094-3444-4b3d-88af-7477090a9e5e"
            | LogicAppContributor -> Guid "87a39d53-fc1b-424a-814c-f7e04687dc9e"
            | LogicAppOperator -> Guid "515c2055-d9d4-4321-b1b9-bd0c9a0f79fe"
            // Identity
            | DomainServicesContributor -> Guid "eeaeda52-9324-47f6-8069-5d5bade478b2"
            | DomainServicesReader -> Guid "361898ef-9ed1-48c2-849c-a832951106bb"
            | ManagedIdentityContributor -> Guid "e40ec5ca-96e0-45a2-b4ff-59039f2c2b59"
            | ManagedIdentityOperator -> Guid "f1a07417-d97a-45cb-824c-7a7467783830"
            // Security
            | AppComplianceAutomationAdministrator -> Guid "0f37683f-2463-46b6-9ce7-9b788b988ba2"
            | AppComplianceAutomationReader -> Guid "ffc6bbe0-e443-4c3b-bf54-26581bb2f78e"
            | AttestationContributor -> Guid "bbf86eb8-f7b4-4cce-96e4-18cddf81d86e"
            | AttestationReader -> Guid "fd1bd22b-8476-40bc-a0bc-69b95687b9f3"
            | KeyVaultAdministrator -> Guid "00482a5a-887f-4fb3-b363-3b7fe8e74483"
            | KeyVaultCertificatesOfficer -> Guid "a4417e6f-fecd-4de8-b567-7b0420556985"
            | KeyVaultContributor -> Guid "f25e0fa2-a7c8-4377-a976-54943a77a395"
            | KeyVaultCryptoOfficer -> Guid "14b46e9e-c2b7-41b4-b07b-48a6ebf60603"
            | KeyVaultCryptoServiceEncryptionUser -> Guid "e147488a-f6f5-4113-8e2d-b22465e65bf6"
            | KeyVaultCryptoUser -> Guid "12338af0-0e69-4776-bea7-57ae8d297424"
            | KeyVaultReader -> Guid "21090545-7ca7-4776-b22c-e363652d74d2"
            | KeyVaultSecretsOfficer -> Guid "b86a8fe4-44ce-4948-aee5-eccb2c155cd7"
            | KeyVaultSecretsUser -> Guid "4633458b-17de-408a-b874-0445c86b69e6"
            | ManagedHSMcontributor -> Guid "18500a29-7fe2-46b2-a342-b16a415e101d"
            | MicrosoftSentinelAutomationContributor -> Guid "f4c81013-99ee-4d62-a7ee-b3f1f648599a"
            | MicrosoftSentinelContributor -> Guid "ab8e14d6-4a74-4a29-9ba8-549422addade"
            | MicrosoftSentinelPlaybookOperator -> Guid "51d6186e-6489-4900-b93f-92e23144cca5"
            | MicrosoftSentinelReader -> Guid "8d289c81-5878-46d4-8554-54e1e3d8b5cb"
            | MicrosoftSentinelResponder -> Guid "3e150937-b8fe-4cfb-8069-0eaf05ecd056"
            | SecurityAdmin -> Guid "fb1c8493-542b-48eb-b624-b4c8fea62acd"
            | SecurityAssessmentContributor -> Guid "612c2aa1-cb24-443b-ac28-3ab7272de6f5"
            | SecurityManager_Legacy -> Guid "e3d13bf0-dd5a-482e-ba6b-9b8433878d10"
            | SecurityReader -> Guid "39bc4728-0917-49c7-9d2c-d95423bc2eb4"
            // DevOps
            | DevTestLabsUser -> Guid "76283e04-6283-4c54-8f91-bcf1374a3c64"
            | LabAssistant -> Guid "ce40b423-cede-4313-a93f-9b28290b72e1"
            | LabContributor -> Guid "5daaa2af-1fe8-407c-9122-bba179798270"
            | LabCreator -> Guid "b97fb8bc-a8b2-4522-a38b-dd33c7e65ead"
            | LabOperator -> Guid "a36e6959-b6be-4b12-8e9f-ef4b474d304d"
            | LabServicesContributor -> Guid "f69b8690-cc87-41d6-b77a-a4bc3c0a966f"
            | LabServicesReader -> Guid "2a5c394f-5eb7-4d4f-9c8e-e8eae39faebc"
            // Monitor
            | ApplicationInsightsComponentContributor -> Guid "ae349356-3a1b-4a5e-921d-050484c6347e"
            | ApplicationInsightsSnapshotDebugger -> Guid "08954f03-6346-4c2e-81c0-ec3a5cfae23b"
            | MonitoringContributor -> Guid "749f88d5-cbae-40b8-bcfc-e573ddc772fa"
            | MonitoringMetricsPublisher -> Guid "3913510d-42f4-4e42-8a64-420c390055eb"
            | MonitoringReader -> Guid "43d0d8ad-25c7-4714-9337-8ba259a9fe05"
            | WorkbookContributor -> Guid "e8ddcd69-c73f-4f9f-9844-4100522f16ad"
            | WorkbookReader -> Guid "b279062a-9be3-42a0-92ae-8b3cf002ec4d"
            // Managementandgovernance
            | AutomationContributor -> Guid "f353d9bd-d4a6-484e-a77a-8050b599b867"
            | AutomationJobOperator -> Guid "4fe576fe-1146-4730-92eb-48519fa6bf9f"
            | AutomationOperator -> Guid "d3881f73-407a-4167-8283-e981cbba0404"
            | AutomationRunbookOperator -> Guid "5fb5aef8-1081-4b8e-bb16-9d5d0385bab5"
            | AzureArcEnabledKubernetesClusterUserRole -> Guid "00493d72-78f6-4148-b6c5-d3ce8e4799dd"
            | AzureArcKubernetesAdmin -> Guid "dffb1e0c-446f-4dde-a09f-99eb5cc68b96"
            | AzureArcKubernetesClusterAdmin -> Guid "8393591c-06b9-48a2-a542-1bd6b377f6a2"
            | AzureArcKubernetesViewer -> Guid "63f0a09d-1495-4db4-a681-037d84835eb4"
            | AzureArcKubernetesWriter -> Guid "5b999177-9696-4545-85c7-50de3797e5a1"
            | AzureConnectedMachineOnboarding -> Guid "b64e21ea-ac4e-4cdf-9dc9-5b892992bee7"
            | AzureConnectedMachineResourceAdministrator -> Guid "cd570a14-e51a-42ad-bac8-bafd67325302"
            | BillingReader -> Guid "fa23ad8b-c56e-40d8-ac0c-ce449e1d2c64"
            | BlueprintContributor -> Guid "41077137-e803-4205-871c-5a86e6a753b4"
            | BlueprintOperator -> Guid "437d2ced-4a38-4302-8479-ed2bcb43d090"
            | CostManagementContributor -> Guid "434105ed-43f6-45c7-a02f-909b2ba83430"
            | CostManagementReader -> Guid "72fafb9e-0641-4937-9268-a91bfd8191a3"
            | HierarchySettingsAdministrator -> Guid "350f8d15-c687-4448-8ae1-157740a3936d"
            | KubernetesCluster_AzureArcOnboarding -> Guid "34e09817-6cbe-4d01-b1a2-e0eac5743d41"
            | KubernetesExtensionContributor -> Guid "85cb6faf-e071-4c9b-8136-154b5a04f717"
            | ManagedApplicationContributorRole -> Guid "641177b8-a67a-45b9-a033-47bc880bb21e"
            | ManagedApplicationOperatorRole -> Guid "c7393b34-138c-406f-901b-d8cf2b17e6ae"
            | ManagedApplicationsReader -> Guid "b9331d33-8a36-4f8c-b097-4f54124fdb44"
            | ManagedServicesRegistrationassignmentDeleteRole -> Guid "91c1777a-f3dc-4fae-b103-61d183457e46"
            | ManagementGroupContributor -> Guid "5d58bcaf-24a5-4b20-bdb6-eed9f69fbe4c"
            | ManagementGroupReader -> Guid "ac63b705-f282-497d-ac71-919bf39d939d"
            | NewRelicAPMAccountContributor -> Guid "5d28c62d-5b37-4476-8438-e587778df237"
            | PolicyInsightsDataWriter_Preview -> Guid "66bb4e9e-b016-4a94-8249-4c0511c2be84"
            | QuotaRequestOperator -> Guid "0e5f05e5-9ab9-446b-b98d-1e2157c94125"
            | ReservationPurchaser -> Guid "f7b75c60-3036-4b75-91c3-6b41c27c1689"
            | ResourcePolicyContributor -> Guid "36243c78-bf99-498c-9df9-86d9f8d28608"
            | SiteRecoveryContributor -> Guid "6670b86e-a3f7-4917-ac9b-5d6ab1be4567"
            | SiteRecoveryOperator -> Guid "494ae006-db33-4328-bf46-533a6560a3ca"
            | SiteRecoveryReader -> Guid "dbaa88c4-0c30-4179-9fb3-46319faa6149"
            | SupportRequestContributor -> Guid "cfd33db0-3dd1-45e3-aa9d-cdbdf3b6f24e"
            | TagContributor -> Guid "4a9ae827-6dc8-4573-8ac7-8239d42aa03f"
            | TemplateSpecContributor -> Guid "1c9b6475-caf0-4164-b5a1-2142a7116f4b"
            | TemplateSpecReader -> Guid "392ae280-861d-42bd-9ea5-08ee6d83b80e"
            // Virtualdesktopinfrastructure
            | DesktopVirtualizationApplicationGroupContributor -> Guid "86240b0e-9422-4c43-887b-b61143f32ba8"
            | DesktopVirtualizationApplicationGroupReader -> Guid "aebf23d0-b568-4e86-b8f9-fe83a2c6ab55"
            | DesktopVirtualizationContributor -> Guid "082f0a83-3be5-4ba1-904c-961cca79b387"
            | DesktopVirtualizationHostPoolContributor -> Guid "e307426c-f9b6-4e81-87de-d99efb3c32bc"
            | DesktopVirtualizationHostPoolReader -> Guid "ceadfde2-b300-400a-ab7b-6143895aa822"
            | DesktopVirtualizationReader -> Guid "49a72310-ab8d-41df-bbb0-79b649203868"
            | DesktopVirtualizationSessionHostOperator -> Guid "2ad6aaab-ead9-4eaa-8ac5-da422f562408"
            | DesktopVirtualizationUser -> Guid "1d18fff3-a72a-46b5-b4a9-0b38a3cd7e63"
            | DesktopVirtualizationUserSessionOperator -> Guid "ea4bfff8-7fb4-485a-aadd-d4129a0ffaa6"
            | DesktopVirtualizationWorkspaceContributor -> Guid "21efdde3-836f-432b-bf3d-3e8e734d4b2b"
            | DesktopVirtualizationWorkspaceReader -> Guid "0fa44ee9-7a7d-466b-9bb2-2bf446b1204d"
            // Other
            | AzureDigitalTwinsDataOwner -> Guid "bcd981a7-7f74-457b-83e1-cceb9e632ffe"
            | AzureDigitalTwinsDataReader -> Guid "d57506d4-4c8d-48b1-8587-93c323f6a5a3"
            | BizTalkContributor -> Guid "5e3c6656-6cfa-4708-81fe-0de47ac73342"
            | GrafanaAdmin -> Guid "22926164-76b3-42b3-bc55-97df8dab3e41"
            | GrafanaEditor -> Guid "a79a5197-3a5c-4973-a920-486035ffd60f"
            | GrafanaViewer -> Guid "60921a7e-fef1-4a43-9b16-a26c52ad4769"
            | LoadTestContributor -> Guid "749a398d-560b-491b-bb21-08924219302e"
            | LoadTestOwner -> Guid "45bb0b16-2f0c-4e78-afaa-a07599b003f6"
            | LoadTestReader -> Guid "3ae3fb29-0000-4ccd-bf80-542e7b26e081"
            | SchedulerJobCollectionsContributor -> Guid "188a0f2f-5c9e-469b-ae67-2aa5ce574b94"
            | ServicesHubOperator -> Guid "82200a5b-e217-47a5-b665-6d8765ee745b"

    type AzureAD =
    /// Can create and manage all aspects of app registrations and enterprise apps.
    | ApplicationAdministrator
    /// Can create application registrations independent of the 'Users can register applications' setting.
    | ApplicationDeveloper
    /// Can create attack payloads that an administrator can initiate later.
    | AttackPayloadAuthor
    /// Can create and manage all aspects of attack simulation campaigns.
    | AttackSimulationAdministrator
    /// Assign custom security attribute keys and values to supported Azure AD objects.
    | AttributeAssignmentAdministrator
    /// Read custom security attribute keys and values for supported Azure AD objects.
    | AttributeAssignmentReader
    /// Define and manage the definition of custom security attributes.
    | AttributeDefinitionAdministrator
    /// Read the definition of custom security attributes.
    | AttributeDefinitionReader
    /// Can access to view, set and reset authentication method information for any non-admin user.
    | AuthenticationAdministrator
    /// Can create and manage the authentication methods policy, tenant-wide MFA settings, password protection policy, and verifiable credentials.
    | AuthenticationPolicyAdministrator
    /// Users assigned to this role are added to the local administrators group on Azure AD-joined devices.
    | AzureADJoinedDeviceLocalAdministrator
    /// Can manage Azure DevOps policies and settings.
    | AzureDevOpsAdministrator
    /// Can manage all aspects of the Azure Information Protection product.
    | AzureInformationProtectionAdministrator
    /// Can manage secrets for federation and encryption in the Identity Experience Framework (IEF).
    | B2CIEFKeysetAdministrator
    /// Can create and manage trust framework policies in the Identity Experience Framework (IEF).
    | B2CIEFPolicyAdministrator
    /// Can perform common billing related tasks like updating payment information.
    | BillingAdministrator
    /// Can manage all aspects of the Defender for Cloud Apps product.
    | CloudAppSecurityAdministrator
    /// Can create and manage all aspects of app registrations and enterprise apps except App Proxy.
    | CloudApplicationAdministrator
    /// Limited access to manage devices in Azure AD.
    | CloudDeviceAdministrator
    /// Can read and manage compliance configuration and reports in Azure AD and Microsoft 365.
    | ComplianceAdministrator
    /// Creates and manages compliance content.
    | ComplianceDataAdministrator
    /// Can manage Conditional Access capabilities.
    | ConditionalAccessAdministrator
    /// Can approve Microsoft support requests to access customer organizational data.
    | CustomerLockBoxAccessApprover
    /// Can access and manage Desktop management tools and services.
    | DesktopAnalyticsAdministrator
    /// Can read basic directory information. Commonly used to grant directory read access to applications and guests.
    | DirectoryReaders
    /// Only used by Azure AD Connect service.
    | DirectorySynchronizationAccounts
    /// Can read and write basic directory information. For granting access to applications, not intended for users.
    | DirectoryWriters
    /// Can manage domain names in cloud and on-premises.
    | DomainNameAdministrator
    /// Can manage all aspects of the Dynamics 365 product.
    | Dynamics365Administrator
    /// Manage all aspects of Microsoft Edge.
    | EdgeAdministrator
    /// Can manage all aspects of the Exchange product.
    | ExchangeAdministrator
    /// Can create or update Exchange Online recipients within the Exchange Online organization.
    | ExchangeRecipientAdministrator
    /// Can create and manage all aspects of user flows.
    | ExternalIDUserFlowAdministrator
    /// Can create and manage the attribute schema available to all user flows.
    | ExternalIDUserFlowAttributeAdministrator
    /// Can configure identity providers for use in direct federation.
    | ExternalIdentityProviderAdministrator
    /// Can manage all aspects of the Fabric and Power BI products.
    | FabricAdministrator
    /// Can manage all aspects of Azure AD and Microsoft services that use Azure AD identities.
    | GlobalAdministrator
    /// Can read everything that a Global Administrator can, but not update anything.
    | GlobalReader
    /// Create and manage all aspectsâ?_of Microsoft Entra Internet Access and Microsoft Entra Private Access, including managing access to public and private endpoints.
    | GlobalSecureAccessAdministrator
    /// Members of this role can create/manage groups, create/manage groups settings like naming and expiration policies, and view groups activity and audit reports.
    | GroupsAdministrator
    /// Can invite guest users independent of the 'members can invite guests' setting.
    | GuestInviter
    /// Can reset passwords for non-administrators and Helpdesk Administrators.
    | HelpdeskAdministrator
    /// Can manage AD to Azure AD cloud provisioning, Azure AD Connect, Pass-through Authentication (PTA), Password hash synchronization (PHS), Seamless Single sign-on (Seamless SSO), and federation settings.
    | HybridIdentityAdministrator
    /// Manage access using Azure AD for identity governance scenarios.
    | IdentityGovernanceAdministrator
    /// Has administrative access in the Microsoft 365 Insights app.
    | InsightsAdministrator
    /// Access the analytical capabilities in Microsoft Viva Insights and run custom queries.
    | InsightsAnalyst
    /// Can view and share dashboards and insights via the Microsoft 365 Insights app.
    | InsightsBusinessLeader
    /// Can manage all aspects of the Intune product.
    | IntuneAdministrator
    /// Can manage settings for Microsoft Kaizala.
    | KaizalaAdministrator
    /// Can configure knowledge, learning, and other intelligent features.
    | KnowledgeAdministrator
    /// Can organize, create, manage, and promote topics and knowledge.
    | KnowledgeManager
    /// Can manage product licenses on users and groups.
    | LicenseAdministrator
    /// Create and manage all aspects of workflows and tasks associated with Lifecycle Workflows in Azure AD.
    | LifecycleWorkflowsAdministrator
    /// Can read security messages and updates in Office 365 Message Center only.
    | MessageCenterPrivacyReader
    /// Can read messages and updates for their organization in Office 365 Message Center only.
    | MessageCenterReader
    /// Create and manage all aspects warranty claims and entitlements for Microsoft manufactured hardware, like Surface and HoloLens.
    | MicrosoftHardwareWarrantyAdministrator
    /// Create and read warranty claims for Microsoft manufactured hardware, like Surface and HoloLens.
    | MicrosoftHardwareWarrantySpecialist
    /// Can manage commercial purchases for a company, department or team.
    | ModernCommerceUser
    /// Can manage network locations and review enterprise network design insights for Microsoft 365 Software as a Service applications.
    | NetworkAdministrator
    /// Can manage Office apps cloud services, including policy and settings management, and manage the ability to select, unselect and publish 'what's new' feature content to end-user's devices.
    | OfficeAppsAdministrator
    /// Write, publish, manage, and review the organizational messages for end-users through Microsoft product surfaces.
    | OrganizationalMessagesWriter
    /// Do not use - not intended for general use.
    | PartnerTier1Support
    /// Do not use - not intended for general use.
    | PartnerTier2Support
    /// Can reset passwords for non-administrators and Password Administrators.
    | PasswordAdministrator
    /// Manage all aspects of Entra Permissions Management.
    | PermissionsManagementAdministrator
    /// Can create and manage all aspects of Microsoft Dynamics 365, Power Apps and Power Automate.
    | PowerPlatformAdministrator
    /// Can manage all aspects of printers and printer connectors.
    | PrinterAdministrator
    /// Can register and unregister printers and update printer status.
    | PrinterTechnician
    /// Can access to view, set and reset authentication method information for any user (admin or non-admin).
    | PrivilegedAuthenticationAdministrator
    /// Can manage role assignments in Azure AD, and all aspects of Privileged Identity Management.
    | PrivilegedRoleAdministrator
    /// Can read sign-in and audit reports.
    | ReportsReader
    /// Can create and manage all aspects of Microsoft Search settings.
    | SearchAdministrator
    /// Can create and manage the editorial content such as bookmarks, Q and As, locations, floorplan.
    | SearchEditor
    /// Can read security information and reports, and manage configuration in Azure AD and Office 365.
    | SecurityAdministrator
    /// Creates and manages security events.
    | SecurityOperator
    /// Can read security information and reports in Azure AD and Office 365.
    | SecurityReader
    /// Can read service health information and manage support tickets.
    | ServiceSupportAdministrator
    /// Can manage all aspects of the SharePoint service.
    | SharePointAdministrator
    /// Can manage all aspects of the Skype for Business product.
    | SkypeforBusinessAdministrator
    /// Can manage the Microsoft Teams service.
    | TeamsAdministrator
    /// Can manage calling and meetings features within the Microsoft Teams service.
    | TeamsCommunicationsAdministrator
    /// Can troubleshoot communications issues within Teams using advanced tools.
    | TeamsCommunicationsSupportEngineer
    /// Can troubleshoot communications issues within Teams using basic tools.
    | TeamsCommunicationsSupportSpecialist
    /// Can perform management related tasks on Teams certified devices.
    | TeamsDevicesAdministrator
    /// Create new Azure AD or Azure AD B2C tenants.
    | TenantCreator
    /// Can see only tenant level aggregates in Microsoft 365 Usage Analytics and Productivity Score.
    | UsageSummaryReportsReader
    /// Can manage all aspects of users and groups, including resetting passwords for limited admins.
    | UserAdministrator
    /// Manage and share Virtual Visits information and metrics from admin centers or the Virtual Visits app.
    | VirtualVisitsAdministrator
    /// Manage and configure all aspects of Microsoft Viva Goals.
    | VivaGoalsAdministrator
    /// Can manage all settings for Microsoft Viva Pulse app.
    | VivaPulseAdministrator
    /// Can provision and manage all aspects of Cloud PCs.
    | Windows365Administrator
    /// Can create and manage all aspects of Windows Update deployments through the Windows Update for Business deployment service.
    | WindowsUpdateDeploymentAdministrator
    /// Manage all aspects of the Yammer service.
    | YammerAdministrator

    with
        member x.Guid =
            match x with
            | ApplicationAdministrator -> Guid "9b895d92-2cd3-44c7-9d02-a6ac2d5ea5c3"
            | ApplicationDeveloper -> Guid "cf1c38e5-3621-4004-a7cb-879624dced7c"
            | AttackPayloadAuthor -> Guid "9c6df0f2-1e7c-4dc3-b195-66dfbd24aa8f"
            | AttackSimulationAdministrator -> Guid "c430b396-e693-46cc-96f3-db01bf8bb62a"
            | AttributeAssignmentAdministrator -> Guid "58a13ea3-c632-46ae-9ee0-9c0d43cd7f3d"
            | AttributeAssignmentReader -> Guid "ffd52fa5-98dc-465c-991d-fc073eb59f8f"
            | AttributeDefinitionAdministrator -> Guid "8424c6f0-a189-499e-bbd0-26c1753c96d4"
            | AttributeDefinitionReader -> Guid "1d336d2c-4ae8-42ef-9711-b3604ce3fc2c"
            | AuthenticationAdministrator -> Guid "c4e39bd9-1100-46d3-8c65-fb160da0071f"
            | AuthenticationPolicyAdministrator -> Guid "0526716b-113d-4c15-b2c8-68e3c22b9f80"
            | AzureADJoinedDeviceLocalAdministrator -> Guid "9f06204d-73c1-4d4c-880a-6edb90606fd8"
            | AzureDevOpsAdministrator -> Guid "e3973bdf-4987-49ae-837a-ba8e231c7286"
            | AzureInformationProtectionAdministrator -> Guid "7495fdc4-34c4-4d15-a289-98788ce399fd"
            | B2CIEFKeysetAdministrator -> Guid "aaf43236-0c0d-4d5f-883a-6955382ac081"
            | B2CIEFPolicyAdministrator -> Guid "3edaf663-341e-4475-9f94-5c398ef6c070"
            | BillingAdministrator -> Guid "b0f54661-2d74-4c50-afa3-1ec803f12efe"
            | CloudAppSecurityAdministrator -> Guid "892c5842-a9a6-463a-8041-72aa08ca3cf6"
            | CloudApplicationAdministrator -> Guid "158c047a-c907-4556-b7ef-446551a6b5f7"
            | CloudDeviceAdministrator -> Guid "7698a772-787b-4ac8-901f-60d6b08affd2"
            | ComplianceAdministrator -> Guid "17315797-102d-40b4-93e0-432062caca18"
            | ComplianceDataAdministrator -> Guid "e6d1a23a-da11-4be4-9570-befc86d067a7"
            | ConditionalAccessAdministrator -> Guid "b1be1c3e-b65d-4f19-8427-f6fa0d97feb9"
            | CustomerLockBoxAccessApprover -> Guid "5c4f9dcd-47dc-4cf7-8c9a-9e4207cbfc91"
            | DesktopAnalyticsAdministrator -> Guid "38a96431-2bdf-4b4c-8b6e-5d3d8abac1a4"
            | DirectoryReaders -> Guid "88d8e3e3-8f55-4a1e-953a-9b9898b8876b"
            | DirectorySynchronizationAccounts -> Guid "d29b2b05-8046-44ba-8758-1e26182fcf32"
            | DirectoryWriters -> Guid "9360feb5-f418-4baa-8175-e2a00bac4301"
            | DomainNameAdministrator -> Guid "8329153b-31d0-4727-b945-745eb3bc5f31"
            | Dynamics365Administrator -> Guid "44367163-eba1-44c3-98af-f5787879f96a"
            | EdgeAdministrator -> Guid "3f1acade-1e04-4fbc-9b69-f0302cd84aef"
            | ExchangeAdministrator -> Guid "29232cdf-9323-42fd-ade2-1d097af3e4de"
            | ExchangeRecipientAdministrator -> Guid "31392ffb-586c-42d1-9346-e59415a2cc4e"
            | ExternalIDUserFlowAdministrator -> Guid "6e591065-9bad-43ed-90f3-e9424366d2f0"
            | ExternalIDUserFlowAttributeAdministrator -> Guid "0f971eea-41eb-4569-a71e-57bb8a3eff1e"
            | ExternalIdentityProviderAdministrator -> Guid "be2f45a1-457d-42af-a067-6ec1fa63bc45"
            | FabricAdministrator -> Guid "a9ea8996-122f-4c74-9520-8edcd192826c"
            | GlobalAdministrator -> Guid "62e90394-69f5-4237-9190-012177145e10"
            | GlobalReader -> Guid "f2ef992c-3afb-46b9-b7cf-a126ee74c451"
            | GlobalSecureAccessAdministrator -> Guid "ac434307-12b9-4fa1-a708-88bf58caabc1"
            | GroupsAdministrator -> Guid "fdd7a751-b60b-444a-984c-02652fe8fa1c"
            | GuestInviter -> Guid "95e79109-95c0-4d8e-aee3-d01accf2d47b"
            | HelpdeskAdministrator -> Guid "729827e3-9c14-49f7-bb1b-9608f156bbb8"
            | HybridIdentityAdministrator -> Guid "8ac3fc64-6eca-42ea-9e69-59f4c7b60eb2"
            | IdentityGovernanceAdministrator -> Guid "45d8d3c5-c802-45c6-b32a-1d70b5e1e86e"
            | InsightsAdministrator -> Guid "eb1f4a8d-243a-41f0-9fbd-c7cdf6c5ef7c"
            | InsightsAnalyst -> Guid "25df335f-86eb-4119-b717-0ff02de207e9"
            | InsightsBusinessLeader -> Guid "31e939ad-9672-4796-9c2e-873181342d2d"
            | IntuneAdministrator -> Guid "3a2c62db-5318-420d-8d74-23affee5d9d5"
            | KaizalaAdministrator -> Guid "74ef975b-6605-40af-a5d2-b9539d836353"
            | KnowledgeAdministrator -> Guid "b5a8dcf3-09d5-43a9-a639-8e29ef291470"
            | KnowledgeManager -> Guid "744ec460-397e-42ad-a462-8b3f9747a02c"
            | LicenseAdministrator -> Guid "4d6ac14f-3453-41d0-bef9-a3e0c569773a"
            | LifecycleWorkflowsAdministrator -> Guid "59d46f88-662b-457b-bceb-5c3809e5908f"
            | MessageCenterPrivacyReader -> Guid "ac16e43d-7b2d-40e0-ac05-243ff356ab5b"
            | MessageCenterReader -> Guid "790c1fb9-7f7d-4f88-86a1-ef1f95c05c1b"
            | MicrosoftHardwareWarrantyAdministrator -> Guid "1501b917-7653-4ff9-a4b5-203eaf33784f"
            | MicrosoftHardwareWarrantySpecialist -> Guid "281fe777-fb20-4fbb-b7a3-ccebce5b0d96"
            | ModernCommerceUser -> Guid "d24aef57-1500-4070-84db-2666f29cf966"
            | NetworkAdministrator -> Guid "d37c8bed-0711-4417-ba38-b4abe66ce4c2"
            | OfficeAppsAdministrator -> Guid "2b745bdf-0803-4d80-aa65-822c4493daac"
            | OrganizationalMessagesWriter -> Guid "507f53e4-4e52-4077-abd3-d2e1558b6ea2"
            | PartnerTier1Support -> Guid "4ba39ca4-527c-499a-b93d-d9b492c50246"
            | PartnerTier2Support -> Guid "e00e864a-17c5-4a4b-9c06-f5b95a8d5bd8"
            | PasswordAdministrator -> Guid "966707d0-3269-4727-9be2-8c3a10f19b9d"
            | PermissionsManagementAdministrator -> Guid "af78dc32-cf4d-46f9-ba4e-4428526346b5"
            | PowerPlatformAdministrator -> Guid "11648597-926c-4cf3-9c36-bcebb0ba8dcc"
            | PrinterAdministrator -> Guid "644ef478-e28f-4e28-b9dc-3fdde9aa0b1f"
            | PrinterTechnician -> Guid "e8cef6f1-e4bd-4ea8-bc07-4b8d950f4477"
            | PrivilegedAuthenticationAdministrator -> Guid "7be44c8a-adaf-4e2a-84d6-ab2649e08a13"
            | PrivilegedRoleAdministrator -> Guid "e8611ab8-c189-46e8-94e1-60213ab1f814"
            | ReportsReader -> Guid "4a5d8f65-41da-4de4-8968-e035b65339cf"
            | SearchAdministrator -> Guid "0964bb5e-9bdb-4d7b-ac29-58e794862a40"
            | SearchEditor -> Guid "8835291a-918c-4fd7-a9ce-faa49f0cf7d9"
            | SecurityAdministrator -> Guid "194ae4cb-b126-40b2-bd5b-6091b380977d"
            | SecurityOperator -> Guid "5f2222b1-57c3-48ba-8ad5-d4759f1fde6f"
            | SecurityReader -> Guid "5d6b6bb7-de71-4623-b4af-96380a352509"
            | ServiceSupportAdministrator -> Guid "f023fd81-a637-4b56-95fd-791ac0226033"
            | SharePointAdministrator -> Guid "f28a1f50-f6e7-4571-818b-6a12f2af6b6c"
            | SkypeforBusinessAdministrator -> Guid "75941009-915a-4869-abe7-691bff18279e"
            | TeamsAdministrator -> Guid "69091246-20e8-4a56-aa4d-066075b2a7a8"
            | TeamsCommunicationsAdministrator -> Guid "baf37b3a-610e-45da-9e62-d9d1e5e8914b"
            | TeamsCommunicationsSupportEngineer -> Guid "f70938a0-fc10-4177-9e90-2178f8765737"
            | TeamsCommunicationsSupportSpecialist -> Guid "fcf91098-03e3-41a9-b5ba-6f0ec8188a12"
            | TeamsDevicesAdministrator -> Guid "3d762c5a-1b6c-493f-843e-55a3b42923d4"
            | TenantCreator -> Guid "112ca1a2-15ad-4102-995e-45b0bc479a6a"
            | UsageSummaryReportsReader -> Guid "75934031-6c7e-415a-99d7-48dbd49e875e"
            | UserAdministrator -> Guid "fe930be7-5e62-47db-91af-98c3a49a38b1"
            | VirtualVisitsAdministrator -> Guid "e300d9e7-4a2b-4295-9eff-f1c78b36cc98"
            | VivaGoalsAdministrator -> Guid "92b086b3-e367-4ef2-b869-1de128fb986e"
            | VivaPulseAdministrator -> Guid "87761b17-1ed2-4af3-9acd-92a150038160"
            | Windows365Administrator -> Guid "11451d60-acb2-45eb-a7d6-43d0f0125c13"
            | WindowsUpdateDeploymentAdministrator -> Guid "32696413-001a-46ae-978c-ce0f6b3620d2"
            | YammerAdministrator -> Guid "810a2642-a034-447f-a5e8-41beaa378541"
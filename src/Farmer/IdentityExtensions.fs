namespace Farmer

open Farmer.Arm
open Identity
open Farmer.Arm.ManagedIdentity
open System

[<AutoOpen>]
module ManagedIdentityExtensions =
    type ManagedIdentity with

        /// Creates a single User-Assigned ResourceIdentity from a ResourceId
        static member create(resourceId: ResourceId) =
            {
                SystemAssigned = Disabled
                UserAssigned = [ UserAssignedIdentity resourceId ]
            }

        static member create(identity: Identity.UserAssignedIdentity) =
            match identity with
            | LinkedUserAssignedIdentity rid ->
                {
                    SystemAssigned = Disabled
                    UserAssigned = [ LinkedUserAssignedIdentity rid ]
                }
            | UserAssignedIdentity rid ->
                {
                    SystemAssigned = Disabled
                    UserAssigned = [ UserAssignedIdentity rid ]
                }

        /// Creates a resource identity from a resource name
        static member create(name: ResourceName) =
            userAssignedIdentities.resourceId name |> ManagedIdentity.create

module Roles =
    type RoleAssignment =
        {
            Role: RoleId
            Principal: PrincipalId
            Owner: ResourceId option
        }

    let private makeRoleId name (roleId: string) =
        RoleId
            {|
                Name = name
                Id = Guid.Parse roleId
            |}

    /// acr push
    let AcrPush = makeRoleId "AcrPush" "8311e382-0749-4cb8-b61a-304f252e45ec"

    /// Can manage service and the APIs
    let APIManagementServiceContributor =
        makeRoleId "APIManagementServiceContributor" "312a565d-c81f-4fd8-895a-4e21e48d571c"

    /// acr pull
    let AcrPull = makeRoleId "AcrPull" "7f951dda-4ed3-4680-a7ca-43fe172d538d"

    /// acr image signer
    let AcrImageSigner =
        makeRoleId "AcrImageSigner" "6cef56e8-d556-48e5-a04f-b8e64114680f"

    /// acr delete
    let AcrDelete = makeRoleId "AcrDelete" "c2f4ef07-c644-48eb-af81-4b1b4947fb11"

    /// acr quarantine data reader
    let AcrQuarantineReader =
        makeRoleId "AcrQuarantineReader" "cdda3590-29a3-44f6-95f2-9f980659eb04"

    /// acr quarantine data writer
    let AcrQuarantineWriter =
        makeRoleId "AcrQuarantineWriter" "c8d4ff99-41c3-41a8-9f60-21dfdad59608"

    /// Can manage service but not the APIs
    let APIManagementServiceOperatorRole =
        makeRoleId "APIManagementServiceOperatorRole" "e022efe7-f5ba-4159-bbe4-b44f577e9b61"

    /// Read-only access to service and APIs
    let APIManagementServiceReaderRole =
        makeRoleId "APIManagementServiceReaderRole" "71522526-b88f-4d52-b57f-d31fc3546d0d"

    /// Can manage Application Insights components
    let ApplicationInsightsComponentContributor =
        makeRoleId "ApplicationInsightsComponentContributor" "ae349356-3a1b-4a5e-921d-050484c6347e"

    /// Gives user permission to use Application Insights Snapshot Debugger features
    let ApplicationInsightsSnapshotDebugger =
        makeRoleId "ApplicationInsightsSnapshotDebugger" "08954f03-6346-4c2e-81c0-ec3a5cfae23b"

    /// Can read the attestation provider properties
    let AttestationReader =
        makeRoleId "AttestationReader" "fd1bd22b-8476-40bc-a0bc-69b95687b9f3"

    /// Create and Manage Jobs using Automation Runbooks.
    let AutomationJobOperator =
        makeRoleId "AutomationJobOperator" "4fe576fe-1146-4730-92eb-48519fa6bf9f"

    /// Read Runbook properties - to be able to create Jobs of the runbook.
    let AutomationRunbookOperator =
        makeRoleId "AutomationRunbookOperator" "5fb5aef8-1081-4b8e-bb16-9d5d0385bab5"

    /// Automation Operators are able to start, stop, suspend, and resume jobs
    let AutomationOperator =
        makeRoleId "AutomationOperator" "d3881f73-407a-4167-8283-e981cbba0404"

    /// Can create and manage an Avere vFXT cluster.
    let AvereContributor =
        makeRoleId "AvereContributor" "4f8fab4f-1852-4a58-a46a-8eaf358af14a"

    /// Used by the Avere vFXT cluster to manage the cluster
    let AvereOperator =
        makeRoleId "AvereOperator" "c025889f-8102-4ebf-b32c-fc0c6f0c6bd9"

    /// List cluster admin credential action.
    let AzureKubernetesServiceClusterAdminRole =
        makeRoleId "AzureKubernetesServiceClusterAdminRole" "0ab0b1a8-8aac-4efd-b8c2-3ee1fb270be8"

    /// List cluster user credential action.
    let AzureKubernetesServiceClusterUserRole =
        makeRoleId "AzureKubernetesServiceClusterUserRole" "4abbcc35-e782-43d8-92c5-2d3f1bd2253f"

    /// Grants access to read map related data from an Azure maps account.
    let AzureMapsDataReader =
        makeRoleId "AzureMapsDataReader" "423170ca-a8f6-4b0f-8487-9e4eb8f49bfa"

    /// Lets you manage Azure Stack registrations.
    let AzureStackRegistrationOwner =
        makeRoleId "AzureStackRegistrationOwner" "6f12a6df-dd06-4f3e-bcb1-ce8be600526a"

    /// Lets you manage backup service,but can't create vaults and give access to others
    let BackupContributor =
        makeRoleId "BackupContributor" "5e467623-bb1f-42f4-a55d-6e525e11384b"

    /// Allows read access to billing data
    let BillingReader =
        makeRoleId "BillingReader" "fa23ad8b-c56e-40d8-ac0c-ce449e1d2c64"

    /// Lets you manage backup services, except removal of backup, vault creation and giving access to others
    let BackupOperator =
        makeRoleId "BackupOperator" "00c29273-979b-4161-815c-10b084fb9324"

    /// Can view backup services, but can't make changes
    let BackupReader = makeRoleId "BackupReader" "a795c7a0-d4a2-40c1-ae25-d81f01202912"

    /// Allows for access to Blockchain Member nodes
    let BlockchainMemberNodeAccess =
        makeRoleId "BlockchainMemberNodeAccess" "31a002a1-acaf-453e-8a5b-297c9ca1ea24"

    /// Lets you manage BizTalk services, but not access to them.
    let BizTalkContributor =
        makeRoleId "BizTalkContributor" "5e3c6656-6cfa-4708-81fe-0de47ac73342"

    /// Can manage CDN endpoints, but can’t grant access to other users.
    let CDNEndpointContributor =
        makeRoleId "CDNEndpointContributor" "426e0c7f-0c7e-4658-b36f-ff54d6c29b45"

    /// Can view CDN endpoints, but can’t make changes.
    let CDNEndpointReader =
        makeRoleId "CDNEndpointReader" "871e35f6-b5c1-49cc-a043-bde969a0f2cd"

    /// Can manage CDN profiles and their endpoints, but can’t grant access to other users.
    let CDNProfileContributor =
        makeRoleId "CDNProfileContributor" "ec156ff8-a8d1-4d15-830c-5b80698ca432"

    /// Can view CDN profiles and their endpoints, but can’t make changes.
    let CDNProfileReader =
        makeRoleId "CDNProfileReader" "8f96442b-4075-438f-813d-ad51ab4019af"

    /// Lets you manage classic networks, but not access to them.
    let ClassicNetworkContributor =
        makeRoleId "ClassicNetworkContributor" "b34d265f-36f7-4a0d-a4d4-e158ca92e90f"

    /// Lets you manage classic storage accounts, but not access to them.
    let ClassicStorageAccountContributor =
        makeRoleId "ClassicStorageAccountContributor" "86e8f5dc-a6e9-4c67-9d15-de283e8eac25"

    /// Classic Storage Account Key Operators are allowed to list and regenerate keys on Classic Storage Accounts
    let ClassicStorageAccountKeyOperatorServiceRole =
        makeRoleId "ClassicStorageAccountKeyOperatorServiceRole" "985d6b00-f706-48f5-a6fe-d0ca12fb668d"

    /// Lets you manage ClearDB MySQL databases, but not access to them.
    let ClearDBMySQLDBContributor =
        makeRoleId "ClearDBMySQLDBContributor" "9106cda0-8a86-4e81-b686-29a22c54effe"

    /// Lets you manage classic virtual machines, but not access to them, and not the virtual network or storage account they’re connected to.
    let ClassicVirtualMachineContributor =
        makeRoleId "ClassicVirtualMachineContributor" "d73bb868-a0df-4d4d-bd69-98a00b01fccb"

    /// Lets you read and list keys of Cognitive Services.
    let CognitiveServicesUser =
        makeRoleId "CognitiveServicesUser" "a97b65f3-24c7-4388-baec-2e87135dc908"

    /// Lets you read Cognitive Services data.
    let CognitiveServicesDataReader =
        makeRoleId "CognitiveServicesDataReader" "b59867f0-fa02-499b-be73-45a86b5b3e1c"

    /// Lets you create, read, update, delete and manage keys of Cognitive Services.
    let CognitiveServicesContributor =
        makeRoleId "CognitiveServicesContributor" "25fbc0a9-bd7c-42a3-aa1a-3b75d497ee68"

    /// Can submit restore request for a Cosmos DB database or a container for an account
    let CosmosBackupOperator =
        makeRoleId "CosmosBackupOperator" "db7b14f2-5adf-42da-9f96-f2ee17bab5cb"

    /// Grants full access to manage all resources, but does not allow you to assign roles in Azure RBAC.
    let Contributor = makeRoleId "Contributor" "b24988ac-6180-42a0-ab88-20f7382dd24c"

    /// Can read Azure Cosmos DB Accounts data
    let CosmosDBAccountReaderRole =
        makeRoleId "CosmosDBAccountReaderRole" "fbdf93bf-df7d-467e-a4d2-9458aa1360c8"

    /// Can view costs and manage cost configuration (e.g. budgets, exports)
    let CostManagementContributor =
        makeRoleId "CostManagementContributor" "434105ed-43f6-45c7-a02f-909b2ba83430"

    /// Can view cost data and configuration (e.g. budgets, exports)
    let CostManagementReader =
        makeRoleId "CostManagementReader" "72fafb9e-0641-4937-9268-a91bfd8191a3"

    /// Lets you manage everything under Data Box Service except giving access to others.
    let DataBoxContributor =
        makeRoleId "DataBoxContributor" "add466c9-e687-43fc-8d98-dfcf8d720be5"

    /// Lets you manage Data Box Service except creating order or editing order details and giving access to others.
    let DataBoxReader =
        makeRoleId "DataBoxReader" "028f4ed7-e2a9-465e-a8f4-9c0ffdfdc027"

    /// Create and manage data factories, as well as child resources within them.
    let DataFactoryContributor =
        makeRoleId "DataFactoryContributor" "673868aa-7521-48a0-acc6-0f60742d39f5"

    /// Can purge analytics data
    let DataPurger = makeRoleId "DataPurger" "150f5e0c-0603-4f03-8c7f-cf70034c4e90"

    /// Lets you submit, monitor, and manage your own jobs but not create or delete Data Lake Analytics accounts.
    let DataLakeAnalyticsDeveloper =
        makeRoleId "DataLakeAnalyticsDeveloper" "47b7735b-770e-4598-a7da-8b91488b4c88"

    /// Lets you connect, start, restart, and shutdown your virtual machines in your Azure DevTest Labs.
    let DevTestLabsUser =
        makeRoleId "DevTestLabsUser" "76283e04-6283-4c54-8f91-bcf1374a3c64"

    /// Lets you manage DocumentDB accounts, but not access to them.
    let DocumentDBAccountContributor =
        makeRoleId "DocumentDBAccountContributor" "5bd9cd88-fe45-4216-938b-f97437e15450"

    /// Lets you manage DNS zones and record sets in Azure DNS, but does not let you control who has access to them.
    let DNSZoneContributor =
        makeRoleId "DNSZoneContributor" "befefa01-2a29-4197-83a8-272ff33ce314"

    /// Lets you manage EventGrid event subscription operations.
    let EventGridEventSubscriptionContributor =
        makeRoleId "EventGridEventSubscriptionContributor" "428e0ff0-5e57-4d9c-a221-2c70d0e0a443"

    /// Lets you read EventGrid event subscriptions.
    let EventGridEventSubscriptionReader =
        makeRoleId "EventGridEventSubscriptionReader" "2414bbcf-6497-4faf-8c65-045460748405"

    /// Create and manage all aspects of the Enterprise Graph - Ontology, Schema mapping, Conflation and Conversational AI and Ingestions
    let GraphOwner = makeRoleId "GraphOwner" "b60367af-1334-4454-b71e-769d9a4f83d9"

    /// Can Read, Create, Modify and Delete Domain Services related operations needed for HDInsight Enterprise Security Package
    let HDInsightDomainServicesContributor =
        makeRoleId "HDInsightDomainServicesContributor" "8d8d5a11-05d3-4bda-a417-a08778121c7c"

    /// Lets you manage Intelligent Systems accounts, but not access to them.
    let IntelligentSystemsAccountContributor =
        makeRoleId "IntelligentSystemsAccountContributor" "03a6d094-3444-4b3d-88af-7477090a9e5e"

    /// Lets you manage key vaults, but not access to them.
    let KeyVaultContributor =
        makeRoleId "KeyVaultContributor" "f25e0fa2-a7c8-4377-a976-54943a77a395"

    /// Knowledge Read permission to consume Enterprise Graph Knowledge using entity search and graph query
    let KnowledgeConsumer =
        makeRoleId "KnowledgeConsumer" "ee361c5d-f7b5-4119-b4b6-892157c8f64c"

    /// Lets you create new labs under your Azure Lab Accounts.
    let LabCreator = makeRoleId "LabCreator" "b97fb8bc-a8b2-4522-a38b-dd33c7e65ead"

    /// Log Analytics Reader can view and search all monitoring data as well as and view monitoring settings, including viewing the configuration of Azure diagnostics on all Azure resources.
    let LogAnalyticsReader =
        makeRoleId "LogAnalyticsReader" "73c42c96-874c-492b-b04d-ab87d138a893"

    /// Log Analytics Contributor can read all monitoring data and edit monitoring settings. Editing monitoring settings includes adding the VM extension to VMs; reading storage account keys to be able to configure collection of logs from Azure Storage; creating and configuring Automation accounts; adding solutions; and configuring Azure diagnostics on all Azure resources.
    let LogAnalyticsContributor =
        makeRoleId "LogAnalyticsContributor" "92aaf0da-9dab-42b6-94a3-d43ce8d16293"

    /// Lets you read, enable and disable logic app.
    let LogicAppOperator =
        makeRoleId "LogicAppOperator" "515c2055-d9d4-4321-b1b9-bd0c9a0f79fe"

    /// Lets you manage logic app, but not access to them.
    let LogicAppContributor =
        makeRoleId "LogicAppContributor" "87a39d53-fc1b-424a-814c-f7e04687dc9e"

    /// Lets you read and perform actions on Managed Application resources
    let ManagedApplicationOperatorRole =
        makeRoleId "ManagedApplicationOperatorRole" "c7393b34-138c-406f-901b-d8cf2b17e6ae"

    /// Lets you read resources in a managed app and request JIT access.
    let ManagedApplicationsReader =
        makeRoleId "ManagedApplicationsReader" "b9331d33-8a36-4f8c-b097-4f54124fdb44"

    /// Read and Assign User Assigned Identity
    let ManagedIdentityOperator =
        makeRoleId "ManagedIdentityOperator" "f1a07417-d97a-45cb-824c-7a7467783830"

    /// Create, Read, Update, and Delete User Assigned Identity
    let ManagedIdentityContributor =
        makeRoleId "ManagedIdentityContributor" "e40ec5ca-96e0-45a2-b4ff-59039f2c2b59"

    /// Management Group Contributor Role
    let ManagementGroupContributor =
        makeRoleId "ManagementGroupContributor" "5d58bcaf-24a5-4b20-bdb6-eed9f69fbe4c"

    /// Management Group Reader Role
    let ManagementGroupReader =
        makeRoleId "ManagementGroupReader" "ac63b705-f282-497d-ac71-919bf39d939d"

    /// Enables publishing metrics against Azure resources
    let MonitoringMetricsPublisher =
        makeRoleId "MonitoringMetricsPublisher" "3913510d-42f4-4e42-8a64-420c390055eb"

    /// Can read all monitoring data.
    let MonitoringReader =
        makeRoleId "MonitoringReader" "43d0d8ad-25c7-4714-9337-8ba259a9fe05"

    /// Lets you manage networks, but not access to them.
    let NetworkContributor =
        makeRoleId "NetworkContributor" "4d97b98b-1d4f-4787-a291-c67834d212e7"

    /// Can read all monitoring data and update monitoring settings.
    let MonitoringContributor =
        makeRoleId "MonitoringContributor" "749f88d5-cbae-40b8-bcfc-e573ddc772fa"

    /// Lets you manage New Relic Application Performance Management accounts and applications, but not access to them.
    let NewRelicAPMAccountContributor =
        makeRoleId "NewRelicAPMAccountContributor" "5d28c62d-5b37-4476-8438-e587778df237"

    /// Grants full access to manage all resources, including the ability to assign roles in Azure RBAC.
    let Owner = makeRoleId "Owner" "8e3af657-a8ff-443c-a75c-2fe8c4bcb635"
    /// View all resources, but does not allow you to make any changes.
    let Reader = makeRoleId "Reader" "acdd72a7-3385-48ef-bd42-f606fba81ae7"

    /// Lets you manage Redis caches, but not access to them.
    let RedisCacheContributor =
        makeRoleId "RedisCacheContributor" "e0f68234-74aa-48ed-b826-c38b57376e17"

    /// Lets you view everything but will not let you delete or create a storage account or contained resource. It will also allow read/write access to all data contained in a storage account via access to storage account keys.
    let ReaderAndDataAccess =
        makeRoleId "ReaderAndDataAccess" "c12c1c16-33a1-487b-954d-41c89c60f349"

    /// Users with rights to create/modify resource policy, create support ticket and read resources/hierarchy.
    let ResourcePolicyContributor =
        makeRoleId "ResourcePolicyContributor" "36243c78-bf99-498c-9df9-86d9f8d28608"

    /// Lets you manage Scheduler job collections, but not access to them.
    let SchedulerJobCollectionsContributor =
        makeRoleId "SchedulerJobCollectionsContributor" "188a0f2f-5c9e-469b-ae67-2aa5ce574b94"

    /// Lets you manage Search services, but not access to them.
    let SearchServiceContributor =
        makeRoleId "SearchServiceContributor" "7ca78c08-252a-4471-8644-bb5ff32d4ba0"

    /// Security Admin Role
    let SecurityAdmin =
        makeRoleId "SecurityAdmin" "fb1c8493-542b-48eb-b624-b4c8fea62acd"

    /// This is a legacy role. Please use Security Administrator instead
    let SecurityManager =
        makeRoleId "SecurityManager" "e3d13bf0-dd5a-482e-ba6b-9b8433878d10"

    /// Security Reader Role
    let SecurityReader =
        makeRoleId "SecurityReader" "39bc4728-0917-49c7-9d2c-d95423bc2eb4"

    /// Lets you manage spatial anchors in your account, but not delete them
    let SpatialAnchorsAccountContributor =
        makeRoleId "SpatialAnchorsAccountContributor" "8bbe83f1-e2a6-4df7-8cb4-4e04d4e5c827"

    /// Lets you manage Site Recovery service except vault creation and role assignment
    let SiteRecoveryContributor =
        makeRoleId "SiteRecoveryContributor" "6670b86e-a3f7-4917-ac9b-5d6ab1be4567"

    /// Lets you failover and failback but not perform other Site Recovery management operations
    let SiteRecoveryOperator =
        makeRoleId "SiteRecoveryOperator" "494ae006-db33-4328-bf46-533a6560a3ca"

    /// Lets you locate and read properties of spatial anchors in your account
    let SpatialAnchorsAccountReader =
        makeRoleId "SpatialAnchorsAccountReader" "5d51204f-eb77-4b1c-b86a-2ec626c49413"

    /// Lets you view Site Recovery status but not perform other management operations
    let SiteRecoveryReader =
        makeRoleId "SiteRecoveryReader" "dbaa88c4-0c30-4179-9fb3-46319faa6149"

    /// Lets you manage spatial anchors in your account, including deleting them
    let SpatialAnchorsAccountOwner =
        makeRoleId "SpatialAnchorsAccountOwner" "70bbe301-9835-447d-afdd-19eb3167307c"

    /// Lets you manage SQL Managed Instances and required network configuration, but can’t give access to others.
    let SQLManagedInstanceContributor =
        makeRoleId "SQLManagedInstanceContributor" "4939a1f6-9ae0-4e48-a1e0-f2cbe897382d"

    /// Lets you manage SQL databases, but not access to them. Also, you can't manage their security-related policies or their parent SQL servers.
    let SQLDBContributor =
        makeRoleId "SQLDBContributor" "9b7fa17d-e63e-47b0-bb0a-15c516ac86ec"

    /// Lets you manage the security-related policies of SQL servers and databases, but not access to them.
    let SQLSecurityManager =
        makeRoleId "SQLSecurityManager" "056cd41c-7e88-42e1-933e-88ba6a50c9c3"

    /// Lets you manage storage accounts, including accessing storage account keys which provide full access to storage account data.
    let StorageAccountContributor =
        makeRoleId "StorageAccountContributor" "17d1049b-9a84-46fb-8f53-869881c3d3ab"

    /// Lets you manage SQL servers and databases, but not access to them, and not their security -related policies.
    let SQLServerContributor =
        makeRoleId "SQLServerContributor" "6d8ee4ec-f05a-4a1d-8b00-a9b17e38b437"

    /// Storage Account Key Operators are allowed to list and regenerate keys on Storage Accounts
    let StorageAccountKeyOperatorServiceRole =
        makeRoleId "StorageAccountKeyOperatorServiceRole" "81a9662b-bebf-436f-a333-f67b29880f12"

    /// Allows for read, write and delete access to Azure Storage blob containers and data
    let StorageBlobDataContributor =
        makeRoleId "StorageBlobDataContributor" "ba92f5b4-2d11-453d-a403-e96b0029c9fe"

    /// Allows for full access to Azure Storage blob containers and data, including assigning POSIX access control.
    let StorageBlobDataOwner =
        makeRoleId "StorageBlobDataOwner" "b7e6dc6d-f1e8-4753-8033-0f276bb0955b"

    /// Allows for read access to Azure Storage blob containers and data
    let StorageBlobDataReader =
        makeRoleId "StorageBlobDataReader" "2a2b9908-6ea1-4ae2-8e65-a410df84e7d1"

    /// Allows for read, write, and delete access to Azure Storage queues and queue messages
    let StorageQueueDataContributor =
        makeRoleId "StorageQueueDataContributor" "974c5e8b-45b9-4653-ba55-5f855dd0fb88"

    /// Allows for peek, receive, and delete access to Azure Storage queue messages
    let StorageQueueDataMessageProcessor =
        makeRoleId "StorageQueueDataMessageProcessor" "8a0f0c08-91a1-4084-bc3d-661d67233fed"

    /// Allows for sending of Azure Storage queue messages
    let StorageQueueDataMessageSender =
        makeRoleId "StorageQueueDataMessageSender" "c6a89b2d-59bc-44d0-9896-0f6e12d7b80a"

    /// Allows for read access to Azure Storage queues and queue messages
    let StorageQueueDataReader =
        makeRoleId "StorageQueueDataReader" "19e7f393-937e-4f77-808e-94535e297925"

    /// Lets you create and manage Support requests
    let SupportRequestContributor =
        makeRoleId "SupportRequestContributor" "cfd33db0-3dd1-45e3-aa9d-cdbdf3b6f24e"

    /// Lets you manage Traffic Manager profiles, but does not let you control who has access to them.
    let TrafficManagerContributor =
        makeRoleId "TrafficManagerContributor" "a4b10055-b0c7-44c2-b00f-c7b5b3550cf7"

    /// View Virtual Machines in the portal and login as administrator
    let VirtualMachineAdministratorLogin =
        makeRoleId "VirtualMachineAdministratorLogin" "1c0163c0-47e6-4577-8991-ea5c82e286e4"

    /// Lets you manage user access to Azure resources.
    let UserAccessAdministrator =
        makeRoleId "UserAccessAdministrator" "18d7d88d-d35e-4fb5-a5c3-7773c20a72d9"

    /// View Virtual Machines in the portal and login as a regular user.
    let VirtualMachineUserLogin =
        makeRoleId "VirtualMachineUserLogin" "fb879df8-f326-4884-b1cf-06f3ad86be52"

    /// Lets you manage virtual machines, but not access to them, and not the virtual network or storage account they're connected to.
    let VirtualMachineContributor =
        makeRoleId "VirtualMachineContributor" "9980e02c-c2be-4d73-94e8-173b1dc7cf3c"

    /// Lets you manage the web plans for websites, but not access to them.
    let WebPlanContributor =
        makeRoleId "WebPlanContributor" "2cc479cb-7b4d-49a8-b449-8c00fd0f0a4b"

    /// Lets you manage websites (not web plans), but not access to them.
    let WebsiteContributor =
        makeRoleId "WebsiteContributor" "de139f84-1756-47ae-9be6-808fbbe84772"

    /// Allows for full access to Azure Service Bus resources.
    let AzureServiceBusDataOwner =
        makeRoleId "AzureServiceBusDataOwner" "090c5cfd-751d-490a-894a-3ce6f1109419"

    /// Allows for full access to Azure Event Hubs resources.
    let AzureEventHubsDataOwner =
        makeRoleId "AzureEventHubsDataOwner" "f526a384-b230-433a-b45c-95f59c4a2dec"

    /// Can read write or delete the attestation provider instance
    let AttestationContributor =
        makeRoleId "AttestationContributor" "bbf86eb8-f7b4-4cce-96e4-18cddf81d86e"

    /// Lets you read and modify HDInsight cluster configurations.
    let HDInsightClusterOperator =
        makeRoleId "HDInsightClusterOperator" "61ed4efc-fab3-44fd-b111-e24485cc132a"

    /// Lets you manage Azure Cosmos DB accounts, but not access data in them. Prevents access to account keys and connection strings.
    let CosmosDBOperator =
        makeRoleId "CosmosDBOperator" "230815da-be43-4aae-9cb4-875f7bd000aa"

    /// Can read, write, delete, and re-onboard Hybrid servers to the Hybrid Resource Provider.
    let HybridServerResourceAdministrator =
        makeRoleId "HybridServerResourceAdministrator" "48b40c6e-82e0-4eb3-90d5-19e40f49b624"

    /// Can onboard new Hybrid servers to the Hybrid Resource Provider.
    let HybridServerOnboarding =
        makeRoleId "HybridServerOnboarding" "5d1e5ee4-7c68-4a71-ac8b-0739630a3dfb"

    /// Allows receive access to Azure Event Hubs resources.
    let AzureEventHubsDataReceiver =
        makeRoleId "AzureEventHubsDataReceiver" "a638d3c7-ab3a-418d-83e6-5f17a39d4fde"

    /// Allows send access to Azure Event Hubs resources.
    let AzureEventHubsDataSender =
        makeRoleId "AzureEventHubsDataSender" "2b629674-e913-4c01-ae53-ef4638d8f975"

    /// Allows for receive access to Azure Service Bus resources.
    let AzureServiceBusDataReceiver =
        makeRoleId "AzureServiceBusDataReceiver" "4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0"

    /// Allows for send access to Azure Service Bus resources.
    let AzureServiceBusDataSender =
        makeRoleId "AzureServiceBusDataSender" "69a216fc-b8fb-44d8-bc22-1f3c2cd27a39"

    /// Allows for read access to Azure File Share over SMB
    let StorageFileDataSMBShareReader =
        makeRoleId "StorageFileDataSMBShareReader" "aba4ae5f-2193-4029-9191-0cb91df5e314"

    /// Allows for read, write, and delete access in Azure Storage file shares over SMB
    let StorageFileDataSMBShareContributor =
        makeRoleId "StorageFileDataSMBShareContributor" "0c867c2a-1d8c-454a-a3db-ab2ea1bdc8bb"

    /// Lets you manage private DNS zone resources, but not the virtual networks they are linked to.
    let PrivateDNSZoneContributor =
        makeRoleId "PrivateDNSZoneContributor" "b12aa53e-6015-4669-85d0-8515ebb3ae7f"

    /// Allows for generation of a user delegation key which can be used to sign SAS tokens
    let StorageBlobDelegator =
        makeRoleId "StorageBlobDelegator" "db58b8e5-c6ad-4a2a-8342-4190687cbf4a"

    /// Allows user to use the applications in an application group.
    let DesktopVirtualizationUser =
        makeRoleId "DesktopVirtualizationUser" "1d18fff3-a72a-46b5-b4a9-0b38a3cd7e63"

    /// Allows for read, write, delete and modify NTFS permission access in Azure Storage file shares over SMB
    let StorageFileDataSMBShareElevatedContributor =
        makeRoleId "StorageFileDataSMBShareElevatedContributor" "a7264617-510b-434b-a828-9731dc254ea7"

    /// Can manage blueprint definitions, but not assign them.
    let BlueprintContributor =
        makeRoleId "BlueprintContributor" "41077137-e803-4205-871c-5a86e6a753b4"

    /// Can assign existing published blueprints, but cannot create new blueprints. NOTE: this only works if the assignment is done with a user-assigned managed identity.
    let BlueprintOperator =
        makeRoleId "BlueprintOperator" "437d2ced-4a38-4302-8479-ed2bcb43d090"

    /// Azure Sentinel Contributor
    let AzureSentinelContributor =
        makeRoleId "AzureSentinelContributor" "ab8e14d6-4a74-4a29-9ba8-549422addade"

    /// Azure Sentinel Responder
    let AzureSentinelResponder =
        makeRoleId "AzureSentinelResponder" "3e150937-b8fe-4cfb-8069-0eaf05ecd056"

    /// Azure Sentinel Reader
    let AzureSentinelReader =
        makeRoleId "AzureSentinelReader" "8d289c81-5878-46d4-8554-54e1e3d8b5cb"

    /// Can read workbooks.
    let WorkbookReader =
        makeRoleId "WorkbookReader" "b279062a-9be3-42a0-92ae-8b3cf002ec4d"

    /// Can save shared workbooks.
    let WorkbookContributor =
        makeRoleId "WorkbookContributor" "e8ddcd69-c73f-4f9f-9844-4100522f16ad"

    /// Allows read access to resource policies and write access to resource component policy events.
    let PolicyInsightsDataWriter =
        makeRoleId "PolicyInsightsDataWriter" "66bb4e9e-b016-4a94-8249-4c0511c2be84"

    /// Read SignalR Service Access Keys
    let SignalRAccessKeyReader =
        makeRoleId "SignalRAccessKeyReader" "04165923-9d83-45d5-8227-78b77b0a687e"

    /// Create, Read, Update, and Delete SignalR service resources
    let SignalRContributor =
        makeRoleId "SignalRContributor" "8cf5e20a-e4b2-4e9d-b3a1-5ceb692c2761"

    /// Can onboard Azure Connected Machines.
    let AzureConnectedMachineOnboarding =
        makeRoleId "AzureConnectedMachineOnboarding" "b64e21ea-ac4e-4cdf-9dc9-5b892992bee7"

    /// Can read, write, delete and re-onboard Azure Connected Machines.
    let AzureConnectedMachineResourceAdministrator =
        makeRoleId "AzureConnectedMachineResourceAdministrator" "cd570a14-e51a-42ad-bac8-bafd67325302"

    /// Managed Services Registration Assignment Delete Role allows the managing tenant users to delete the registration assignment assigned to their tenant.
    let ManagedServicesRegistrationAssignmentDeleteRole =
        makeRoleId "ManagedServicesRegistrationAssignmentDeleteRole" "91c1777a-f3dc-4fae-b103-61d183457e46"

    /// Allows full access to App Configuration data.
    let AppConfigurationDataOwner =
        makeRoleId "AppConfigurationDataOwner" "5ae67dd6-50cb-40e7-96ff-dc2bfa4b606b"

    /// Allows read access to App Configuration data.
    let AppConfigurationDataReader =
        makeRoleId "AppConfigurationDataReader" "516239f1-63e1-4d78-a4de-a74fb236a071"

    /// Role definition to authorize any user/service to create connectedClusters resource
    let KubernetesClusterAzureArcOnboarding =
        makeRoleId "KubernetesClusterAzureArcOnboarding" "34e09817-6cbe-4d01-b1a2-e0eac5743d41"

    /// Experimentation Contributor
    let ExperimentationContributor =
        makeRoleId "ExperimentationContributor" "7f646f1b-fa08-80eb-a22b-edd6ce5c915c"

    /// Let’s you read and test a KB only.
    let CognitiveServicesQnAMakerReader =
        makeRoleId "CognitiveServicesQnAMakerReader" "466ccd10-b268-4a11-b098-b4849f024126"

    /// Let’s you create, edit, import and export a KB. You cannot publish or delete a KB.
    let CognitiveServicesQnAMakerEditor =
        makeRoleId "CognitiveServicesQnAMakerEditor" "f4cc2bf9-21be-47a1-bdf1-5c5804381025"

    /// Experimentation Administrator
    let ExperimentationAdministrator =
        makeRoleId "ExperimentationAdministrator" "7f646f1b-fa08-80eb-a33b-edd6ce5c915c"

    /// Provides user with conversion, manage session, rendering and diagnostics capabilities for Azure Remote Rendering
    let RemoteRenderingAdministrator =
        makeRoleId "RemoteRenderingAdministrator" "3df8b902-2a6f-47c7-8cc5-360e9b272a7e"

    /// Provides user with manage session, rendering and diagnostics capabilities for Azure Remote Rendering.
    let RemoteRenderingClient =
        makeRoleId "RemoteRenderingClient" "d39065c4-c120-43c9-ab0a-63eed9795f0a"

    /// Allows for creating managed application resources.
    let ManagedApplicationContributorRole =
        makeRoleId "ManagedApplicationContributorRole" "641177b8-a67a-45b9-a033-47bc880bb21e"

    /// Lets you push assessments to Security Center
    let SecurityAssessmentContributor =
        makeRoleId "SecurityAssessmentContributor" "612c2aa1-cb24-443b-ac28-3ab7272de6f5"

    /// Lets you manage tags on entities, without providing access to the entities themselves.
    let TagContributor =
        makeRoleId "TagContributor" "4a9ae827-6dc8-4573-8ac7-8239d42aa03f"

    /// Allows developers to create and update workflows, integration accounts and API connections in integration service environments.
    let IntegrationServiceEnvironmentDeveloper =
        makeRoleId "IntegrationServiceEnvironmentDeveloper" "c7aa55d3-1abb-444a-a5ca-5e51e485d6ec"

    /// Lets you manage integration service environments, but not access to them.
    let IntegrationServiceEnvironmentContributor =
        makeRoleId "IntegrationServiceEnvironmentContributor" "a41e2c5b-bd99-4a07-88f4-9bf657a760b8"

    /// Administrator of marketplace resource provider
    let MarketplaceAdmin =
        makeRoleId "MarketplaceAdmin" "dd920d6d-f481-47f1-b461-f338c46b2d9f"

    /// Grants access to read and write Azure Kubernetes Service clusters
    let AzureKubernetesServiceContributorRole =
        makeRoleId "AzureKubernetesServiceContributorRole" "ed7f3fbd-7b88-4dd4-9017-9adb7ce333f8"

    /// Read-only role for Digital Twins data-plane properties
    let AzureDigitalTwinsReader =
        makeRoleId "AzureDigitalTwinsReader" "d57506d4-4c8d-48b1-8587-93c323f6a5a3"

    /// Full access role for Digital Twins data-plane
    let AzureDigitalTwinsOwner =
        makeRoleId "AzureDigitalTwinsOwner" "bcd981a7-7f74-457b-83e1-cceb9e632ffe"

    /// Allows users to edit and delete Hierarchy Settings
    let HierarchySettingsAdministrator =
        makeRoleId "HierarchySettingsAdministrator" "350f8d15-c687-4448-8ae1-157740a3936d"

    /// Role allows user or principal full access to FHIR Data
    let FHIRDataContributor =
        makeRoleId "FHIRDataContributor" "5a1fc7df-4bf1-4951-a576-89034ee01acd"

    /// Role allows user or principal to read and export FHIR Data
    let FHIRDataExporter =
        makeRoleId "FHIRDataExporter" "3db33094-8700-4567-8da5-1501d4e7e843"

    /// Role allows user or principal to read FHIR Data
    let FHIRDataReader =
        makeRoleId "FHIRDataReader" "4c8d0bbc-75d3-4935-991f-5f3c56d81508"

    /// Role allows user or principal to read and write FHIR Data
    let FHIRDataWriter =
        makeRoleId "FHIRDataWriter" "3f88fce4-5892-4214-ae73-ba5294559913"

    /// Experimentation Reader
    let ExperimentationReader =
        makeRoleId "ExperimentationReader" "49632ef5-d9ac-41f4-b8e7-bbe587fa74a1"

    /// Provides user with ingestion capabilities for Azure Object Understanding.
    let ObjectUnderstandingAccountOwner =
        makeRoleId "ObjectUnderstandingAccountOwner" "4dd61c23-6743-42fe-a388-d8bdd41cb745"

    /// Grants access to read, write, and delete access to map related data from an Azure maps account.
    let AzureMapsDataContributor =
        makeRoleId "AzureMapsDataContributor" "8f5e0ce6-4f7b-4dcf-bddf-e6f48634a204"

    /// Full access to the project, including the ability to view, create, edit, or delete projects.
    let CognitiveServicesCustomVisionContributor =
        makeRoleId "CognitiveServicesCustomVisionContributor" "c1ff6cc2-c111-46fe-8896-e0ef812ad9f3"

    /// Publish, unpublish or export models. Deployment can view the project but can’t update.
    let CognitiveServicesCustomVisionDeployment =
        makeRoleId "CognitiveServicesCustomVisionDeployment" "5c4089e1-6d96-4d2f-b296-c1bc7137275f"

    /// View, edit training images and create, add, remove, or delete the image tags. Labelers can view the project but can’t update anything other than training images and tags.
    let CognitiveServicesCustomVisionLabeler =
        makeRoleId "CognitiveServicesCustomVisionLabeler" "88424f51-ebe7-446f-bc41-7fa16989e96c"

    /// Read-only actions in the project. Readers can’t create or update the project.
    let CognitiveServicesCustomVisionReader =
        makeRoleId "CognitiveServicesCustomVisionReader" "93586559-c37d-4a6b-ba08-b9f0940c2d73"

    /// View, edit projects and train the models, including the ability to publish, unpublish, export the models. Trainers can’t create or delete the project.
    let CognitiveServicesCustomVisionTrainer =
        makeRoleId "CognitiveServicesCustomVisionTrainer" "0a5ae4ab-0d65-4eeb-be61-29fc9b54394b"

    /// Perform all data plane operations on a key vault and all objects in it, including certificates, keys, and secrets. Cannot manage key vault resources or manage role assignments. Only works for key vaults that use the 'Azure role-based access control' permission model.
    let KeyVaultAdministrator =
        makeRoleId "KeyVaultAdministrator" "00482a5a-887f-4fb3-b363-3b7fe8e74483"

    /// Perform any action on the keys of a key vault, except manage permissions. Only works for key vaults that use the 'Azure role-based access control' permission model.
    let KeyVaultCryptoOfficer =
        makeRoleId "KeyVaultCryptoOfficer" "14b46e9e-c2b7-41b4-b07b-48a6ebf60603"

    /// Perform cryptographic operations using keys. Only works for key vaults that use the 'Azure role-based access control' permission model.
    let KeyVaultCryptoUser =
        makeRoleId "KeyVaultCryptoUser" "12338af0-0e69-4776-bea7-57ae8d297424"

    /// Perform any action on the secrets of a key vault, except manage permissions. Only works for key vaults that use the 'Azure role-based access control' permission model.
    let KeyVaultSecretsOfficer =
        makeRoleId "KeyVaultSecretsOfficer" "b86a8fe4-44ce-4948-aee5-eccb2c155cd7"

    /// Read secret contents. Only works for key vaults that use the 'Azure role-based access control' permission model.
    let KeyVaultSecretsUser =
        makeRoleId "KeyVaultSecretsUser" "4633458b-17de-408a-b874-0445c86b69e6"

    /// Perform any action on the certificates of a key vault, except manage permissions. Only works for key vaults that use the 'Azure role-based access control' permission model.
    let KeyVaultCertificatesOfficer =
        makeRoleId "KeyVaultCertificatesOfficer" "a4417e6f-fecd-4de8-b567-7b0420556985"

    /// Read metadata of key vaults and its certificates, keys, and secrets. Cannot read sensitive values such as secret contents or key material. Only works for key vaults that use the 'Azure role-based access control' permission model.
    let KeyVaultReader =
        makeRoleId "KeyVaultReader" "21090545-7ca7-4776-b22c-e363652d74d2"

    /// Read metadata of keys and perform wrap/unwrap operations. Only works for key vaults that use the 'Azure role-based access control' permission model.
    let KeyVaultCryptoServiceEncryption =
        makeRoleId "KeyVaultCryptoServiceEncryption" "e147488a-f6f5-4113-8e2d-b22465e65bf6"

    /// Lets you view all resources in cluster/namespace, except secrets.
    let AzureArcKubernetesViewer =
        makeRoleId "AzureArcKubernetesViewer" "63f0a09d-1495-4db4-a681-037d84835eb4"

    /// Lets you update everything in cluster/namespace, except (cluster)roles and (cluster)role bindings.
    let AzureArcKubernetesWriter =
        makeRoleId "AzureArcKubernetesWriter" "5b999177-9696-4545-85c7-50de3797e5a1"

    /// Lets you manage all resources in the cluster.
    let AzureArcKubernetesClusterAdmin =
        makeRoleId "AzureArcKubernetesClusterAdmin" "8393591c-06b9-48a2-a542-1bd6b377f6a2"

    /// Lets you manage all resources under cluster/namespace, except update or delete resource quotas and namespaces.
    let AzureArcKubernetesAdmin =
        makeRoleId "AzureArcKubernetesAdmin" "dffb1e0c-446f-4dde-a09f-99eb5cc68b96"

    /// Lets you manage all resources in the cluster.
    let AzureKubernetesServiceRBACClusterAdmin =
        makeRoleId "AzureKubernetesServiceRBACClusterAdmin" "b1ff04bb-8a4e-4dc4-8eb5-8693973ce19b"

    /// Lets you manage all resources under cluster/namespace, except update or delete resource quotas and namespaces.
    let AzureKubernetesServiceRBACAdmin =
        makeRoleId "AzureKubernetesServiceRBACAdmin" "3498e952-d568-435e-9b2c-8d77e338d7f7"

    /// Lets you view all resources in cluster/namespace, except secrets.
    let AzureKubernetesServiceRBACReader =
        makeRoleId "AzureKubernetesServiceRBACReader" "7f6c6a51-bcf8-42ba-9220-52d62157d7db"

    /// Lets you update everything in cluster/namespace, except resource quotas, namespaces, pod security policies, certificate signing requests, (cluster)roles and (cluster)role bindings.
    let AzureKubernetesServiceRBACWriter =
        makeRoleId "AzureKubernetesServiceRBACWriter" "a7ffa36f-339b-4b5c-8bdf-e2c188b2c0eb"

    /// Services Hub Operator allows you to perform all read, write, and deletion operations related to Services Hub Connectors.
    let ServicesHubOperator =
        makeRoleId "ServicesHubOperator" "82200a5b-e217-47a5-b665-6d8765ee745b"

    /// Lets you read ingestion jobs for an object understanding account.
    let ObjectUnderstandingAccountReader =
        makeRoleId "ObjectUnderstandingAccountReader" "d18777c0-1514-4662-8490-608db7d334b6"

    /// List cluster user credentials action.
    let AzureArcEnabledKubernetesClusterUserRole =
        makeRoleId "AzureArcEnabledKubernetesClusterUserRole" "00493d72-78f6-4148-b6c5-d3ce8e4799dd"

    /// Lets your app server access SignalR Service with AAD Auth options.
    let SignalRAppServer =
        makeRoleId "SignalRAppServer" "420fcaa2-552c-430f-98ca-3264be4806c7"

    /// Lets your app access service in serverless mode.
    let SignalRServerlessContributor =
        makeRoleId "SignalRServerlessContributor" "fd53cd77-2268-407a-8f46-7e7863d0f521"

    /// Can manage data packages of a collaborative.
    let CollaborativeDataContributor =
        makeRoleId "CollaborativeDataContributor" "daa9e50b-21df-454c-94a6-a8050adab352"

    /// Gives you read access to management and content operations, but does not allow making changes
    let DeviceUpdateReader =
        makeRoleId "DeviceUpdateReader" "e9dba6fb-3d52-4cf0-bce3-f06ce71b9e0f"

    /// Gives you full access to management and content operations
    let DeviceUpdateAdministrator =
        makeRoleId "DeviceUpdateAdministrator" "02ca0879-e8e4-47a5-a61e-5c618b76e64a"

    /// Gives you full access to content operations
    let DeviceUpdateContentAdministrator =
        makeRoleId "DeviceUpdateContentAdministrator" "0378884a-3af5-44ab-8323-f5b22f9f3c98"

    /// Gives you full access to management operations
    let DeviceUpdateDeploymentsAdministrator =
        makeRoleId "DeviceUpdateDeploymentsAdministrator" "e4237640-0e3d-4a46-8fda-70bc94856432"

    /// Gives you read access to management operations, but does not allow making changes
    let DeviceUpdateDeploymentsReader =
        makeRoleId "DeviceUpdateDeploymentsReader" "49e2f5d2-7741-4835-8efa-19e1fe35e47f"

    /// Gives you read access to content operations, but does not allow making changes
    let DeviceUpdateContentReader =
        makeRoleId "DeviceUpdateContentReader" "d1ee9a80-8b14-47f0-bdc2-f4a351625a7b"

    /// Full access to the project, including the system level configuration.
    let CognitiveServicesMetricsAdvisorAdministrator =
        makeRoleId "CognitiveServicesMetricsAdvisorAdministrator" "cb43c632-a144-4ec5-977c-e80c4affc34a"

    /// Access to the project.
    let CognitiveServicesMetricsAdvisorUser =
        makeRoleId "CognitiveServicesMetricsAdvisorUser" "3b20f47b-3825-43cb-8114-4bd2201156a8"

    /// Read and list Schema Registry groups and schemas.
    let SchemaRegistryReader =
        makeRoleId "SchemaRegistryReader" "2c56ea50-c6b3-40a6-83c0-9d98858bc7d2"

    /// Read, write, and delete Schema Registry groups and schemas.
    let SchemaRegistryContributor =
        makeRoleId "SchemaRegistryContributor" "5dffeca3-4936-4216-b2bc-10343a5abb25"

    /// Provides read access to AgFood Platform Service
    let AgFoodPlatformServiceReader =
        makeRoleId "AgFoodPlatformServiceReader" "7ec7ccdc-f61e-41fe-9aaf-980df0a44eba"

    /// Provides contribute access to AgFood Platform Service
    let AgFoodPlatformServiceContributor =
        makeRoleId "AgFoodPlatformServiceContributor" "8508508a-4469-4e45-963b-2518ee0bb728"

    /// Provides admin access to AgFood Platform Service
    let AgFoodPlatformServiceAdmin =
        makeRoleId "AgFoodPlatformServiceAdmin" "f8da80de-1ff9-4747-ad80-a19b7f6079e3"

    /// Lets you manage managed HSM pools, but not access to them.
    let ManagedHSMcontributor =
        makeRoleId "ManagedHSMcontributor" "18500a29-7fe2-46b2-a342-b16a415e101d"

    /// Read-only access to Azure SignalR Service REST APIs
    let SignalRServiceReader =
        makeRoleId "SignalRServiceReader" "ddde6b66-c0df-4114-a159-3618637b3035"

    /// Full access to Azure SignalR Service REST APIs
    let SignalRServiceOwner =
        makeRoleId "SignalRServiceOwner" "7e4f1700-ea5a-4f59-8f37-079cfe29dce3"

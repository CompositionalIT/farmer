Release Notes
=============

## 1.6.5
* Azure Firewall: Bug fix for link_to_vhub and added depends_on to builder
* Functions: Add support for keyvault reference user identity
* VirtualHubs/hubRouteTables : Add support for labels
* Virtual Machine: Add option to static IP allocation
* Web App: Add support for keyvault reference user identity

## 1.6.4
* DNS Zone: Added SOA and SRV record support
* Azure Firewall: Added support for Azure Firewalls
* Service Bus: Support max queue and topic sizes.
* Service Bus: Set default message TTL for subscriptions.
* Virtual Hubs: Support for virtualHubs and hubRouteTables
* Virtual Machine: Added Identity support
* Virtual Machine: Added a PasswordParameterARM member

## 1.6.3
* Container Service (AKS): Support basic SKU for the cluster's load balancer (default is standard).
* Container Service (AKS): Support for private Kubernetes API access.
* Container Service (AKS): Support for restricting IP ranges for Kubernetes API access.
* Functions: Support publishing as a docker container
* Service bus: Add support for authorization rules.
* Virtual Machine: Added disable password authentication to Virtual Machine linux configuration
* Virtual Machine: Added sshKeys and paths to Virtual Machine linux configuration

## 1.6.2
* Functions: Support Elastic Premium SKUs for Functions service plans.
* SQL Azure: Support for minimum TLS version.
* Storage: Support for minimum TLS version.
* Virtual Machine: Provide control over the public IP
* Virtual Machine: Support for customData on osProfile Properties
* Virtual Network: Add support for vnet peering
* WebApp: Added support for PrivateEndpoints

## 1.6.1
* Web App: Workaround ARM regression when Identity is set to "None".

## 1.6.0
* Added support for nesting resource groups
* Storage: Support for firewall to restrict storage account network access to virtual network subnets, IP addresses, and CIDR prefixes.
* Virtual Network: Support for creating service endpoints on subnets.
* Virtual Network: Support for assigning existing service endpoint policies to subnets.

## 1.5.3
* CDN: Support for CDN rules
* Container Service (AKS): Support for using managed identity (msi) for the service principal.
* LoadBalancer: Adds support for public and internal load balancers.
* Traffic Manager: initial release.

## 1.5.2
* ServiceBus: TopicConfig implements IBuilder and supports link_to_unmanaged_namespace.
* ServiceBus: Support for forwarding messages delivered to a subscription.

## 1.5.1
* Communication Services: add builder.
* ExpressRoute: Adds ServiceKey property to generate an expression for the service key on a new circuit.
* Network Security Groups: Enable builder to create outbound rules.
* ServiceBus: Fix an issue with Premium Sku ARM Writer
* ServiceBus: Fix an issue with Rules depends on ARM Writer
* Storage Accounts: Support for CORS.
* Virtual WAN: add builder

##  1.5.0
* Container Groups: Support for init containers.
* Container Groups: Support for liveliness and readiness probes on containers.
* Container Groups: Connect network profile to an existing virtual network.
* Container Groups: Bugfix for outputs.
* CosmosDb: Add support for MongoDB as a database kind.
* Event Grid: Ensure destination Queues are created as a dependency
* Event Grid: Add ServiceBus Queue and Topic as supported destinations
* Functions: Support for 64 bits.
* Functions: Add option to use managed Key Vault
* Functions: Add support for dotnet-isolated runtime (NET5)
* KeyVault: Fix an issue with adding tags on main KeyVault builder.
* KeyVault: Support Azure RBAC for data plane access.
* ServiceBus: update namespace validation rules to follow [Microsoft documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/management/resource-name-rules#microsoftservicebus)
* Storage: Add support for tables
* Web App: Disables the automatic addition of the logging site extension when `docker_image` is used
* Web App: Add dotnet 5.0 runtime option

* Framework: Updated DeterministicGuid for RFC 4122 compatibility
* Framework: Add support for NET5, upgrade to F#5.
* Framework: Simplify Event Grid builder
* Framework: Use System.Text.Json instead of Newtonsoft.Json

## 1.4.0
* Bing Search: Support for Bing Search (migrated from Cognitive Services).
* Container Registry: Added ARM expressions for admin account credentials
* Databricks Workspace: Support for creating Databricks Workspaces
* Diagnostic Settings: Support for creating Diagnostic Settings on other resources.
* Event Hub: Update built-in expression paths for default key.
* Functions: Added some extra keywords which were already present on Web App.
* Storage: Support for setting default blob access tier at account level with "default_blob_access_tier"
* SQL Azure: Validation and fail fast on account names instead of silently fixing them (breaking run-time change).

* Web App: Improved KeyVault integration.
* Web App: Add PremiumV3 SKU.
* Web App: Automatically add Logging extension for ASP.NET Core apps (additive change to ARM).
* Web App: Added Instrumentation Key Setting for Linux WebApp.
* Web App: Automatically add Client Id setting for user assigned identities.
* Web App: Support for 64 bits.

* Azure CLI: Ensure JSON output.
* Framework: Extension methods for Taggable and Dependable to simplify boilerplate keywords.
* Framework: Common keywords between Functions and Web Apps factored out.

## 1.3.2
* Storage: Revert User Assigned Identity scope to ResourceGroup
* User Assigned Identity: Allow explicitly setting dependencies

## 1.3.1
* CosmosDB: Fix an issue whereby dependent resource paths were sometimes incorrectly generated.

## 1.3.0
* ARM generation: Smarter emitting of "raw" ARM expressions.

* CDN: Fix issues around custom domain host names.
* CDN: Improved integration with Storage Accounts.

* Container Instance: Support for secure parameters for environment variables and secret volumes.
* Container Instance: Support for command line arguments.

* Deployment Scripts: Support for secure parameters for environment variables (minor breaking change).
* Deployment Scripts: Specifies cleanup on expiration when retention interval is set, and enables cleanup on success only.
* Deployment Scripts: Support for running the script after other resources are deployed.
* Deployment Scripts: Run Azure CLI commands as part of an ARM deployment (PowerShell or AzCli).

* Functions: Support for external unmanaged storage accounts.
* Functions: Support for user-assigned managed identity.

* Key Vault: Support for setting tags on key vault secrets.

* Storage Account: Support for the full set of Storage Account Kind and SKUs (minor breaking change).
* Storage Account: Improved integration with CDN.

* Web App: Support for site extensions.
* Web App: Unmanaged Server Farm uses Resource Id for fully-qualified path.

## 1.2.0
* Log Analytics: Initial release.
* Static Web Apps: Initial release.
* Managed Identity: Initial release.

* SQL Azure: Connection string owner now has the correct path.
* SQL Azure: New PasswordParameter returns the name of the Password parameter.

* Functions: Ability to override the auto-generated storage account name.
* Functions: Ability to add multiple ARM Expressions as settings.
* Functions: Ability to add a Resource Name as a setting.

* Key Vault: Grant access to managed identities.

* Service Bus: Fix an issue whereby duplicate topics across different subscriptions were silently removed.
* Service Bus: Set message TTL

* Web App: Ability to add multiple ARM Expressions as settings.
* Web App: Full support for Managed Identity (minor breaking change).
* Web App: Easy Key Vault integration.

* Storage Account: WebsitePrimaryEndpoint is now a generated ARM expression.
* Storage Account: Upgrade API version to 2019-04-01 to support RA-GZRS.
* Storage Account: WebsitePrimaryEndpoint depends on storage account name instead of being hardcoded.
* Storage Account: Grant access to managed identities.

* Container Service (AKS): Support for Managed Identity.

* Container Groups: Support for creating group without public IP Address.
* Container Groups: Support for image registry credentials for private registries.
* Container Groups: Support for partial CPU cores.
* Container Groups: Support for Managed Identity.

* Event Hubs: Remove redundant kafka flag (minor breaking change).
* Gateway: Add VPN Client configuration
* Bastion Hosts: Create bastion hosts for accessing resources on a virtual network
* DNS Zones: Basic Azure DNS support

* Provide all Roles for managed identity purposes.
* Support for implicitly adding dependencies based on usage e.g. add settings, connection strings etc.

* Internal: ARM Expression refernces now add the Resource Id as the Owner.
* Internal: Changes to better capture full resource IDs.

## 1.1.1
* SQL Azure: Fix a bug whereby firewall rule IP addresses were inverted.

## 1.1.0
* AKS: Basic AKS support
* App Insights:
    * Create key expressions
    * Support for IP Masking and Sampling
* Container Instance: Change modelling from an anonymous type to a discriminated union (interop) (https://github.com/CompositionalIT/farmer/issues/372)
* Cognitive Services: Retrieve ARM expression to the Key of the Cognitive Services instance.
* CosmosDB: Create connection string and key expressions.
* Functions: Fix an issue with incorrect Service Plan linking
* Gateway: Add VPN Client configuration
* SQL Azure:
    * Small updates to type naming
    * Support for VCore model
    * Support for specifying disk size
* Storage:
    * Create connection string expressions.
    * Data Lake support is now optional and off by default
    * Support for lifecycle policies
* Web App / Functions: Allow CORS enable credentials (https://github.com/CompositionalIT/farmer/issues/265)
* Web App: Support for Connection Strings
* Azure CLI: Better error message when Azure CLI be found (https://github.com/CompositionalIT/farmer/pull/369)
* Fix a bug whereby optional Location, Tags and DependsOn were set to empty lists instead of null when not required.
* Internal updates to ARM resource construction
* Support for adding a list of dependencies to resources

## 1.0.0
* Formal release

## 1.0.0 RC2
* Fix an issue with CosmosDB tags being set twice
* ACI dropped support for assigning static private IP

## 1.0.0 RC
* Postgres API redesign
* Network Security Group API redesign
* Validation for all Storage Account resources
* Prevent supplying VM custom script files without a custom script
* Basic Validation helper functions

## 0.24.0
* More documentation
* Simplified Service Bus filtering
* Tagging support for most ARM resources
* Fix incorrect PostgresSQL template generation caused by a breaking change in F#
* Fix a bug in Redis SKU generation
* KeyVault now supports dependencies
* Eager Storage Account Name validation

## 0.23.0
* Fix documentation
* Volume Mount support for Container Groups
* Network Security Group (NSG) support
* Data Lake on Storage Account support
* Static Website on Storage Account enhancements
* Filter support on Service Bus Subscriptions
* Storage Account validation on account name enhancements
* Web Apps can now connect to externally-managed Server Farms
* Simplified Resource References and better distinguish resource relationships
* Improved test coverage

## 0.22.0
* Cosmos DB: More keys exposed as properties
* Deployment: Display currently selected subscription id when deploying
* Event Hub: Dependency support
* Event Grid: Initial support
* Functions: Zip deploy support
* Storage Account: Static website support
* Storage Account: File Share support
* Storage Account: Queue support
* Internal: More automated test coverage
* Internal: Refactoring of ARM resources to use strongly-typed resource path generation

## 0.21.0
* VNet Gateway support
* Event Hub Capture support
* Virtual Machine script support
* Fix a bug where some ARM Expressions were sometimes incorrectly formed

## 0.20.1
* Fix a bug in KeyVault where key validation was applied incorrectly
* Allow optionally setting AccessPolicy permissions using the create helper
* Set LIST and GET as the default AccessPolicy permission set using the create helper

## 0.20.0
* Support for safely building resourceId expressions
* Simplify construction of ARM resources
* Better error handling on JSON deserialization

## 0.19.0
* CDN support added
* Split Container Instance builder back to two
* Environment variable support for Container Instances
* Support for public and internal ports on Container Instances
* Improved subnet and address space support for virtual networks
* Add S0 SKU for Cognitive Services
* Automatically create the path if it does not exist when writing ARM templates

## 0.18.0
* Improved Subnet and VNet support
* Validation checks on CosmosDB
* Source Control support in Web Apps

## 0.17.3
* Ability to assign users and groups to KeyVault access policies
* Ability to add multiple KeyVault secrets

## 0.17.2
* Workaround for issue with FSharp.Core 4.7.2 and DU stringify

## 0.17.1
* Enhance access policy maintenance in KeyVault

## 0.17.0
* Data Lake support
* Managed Identity support for Web Apps and Functions
* CORS support for Web Apps and Functions
* Secret settings support for Web Apps and Functions
* Improved typing around Cosmos DB
* Simplified Key Vault support for adding secrets and simple policies
* Ability to inject raw JSON ARM resources into the Farmer pipeline
* Support for more locations
* Minimum Azure CLI version now 2.5.0
* Promote some types into the top-level Farmer namespace

## 0.16.0
* Extra settings for Functions and Web Apps
* Rationalise depends_on so any resource is dependable
* Redesign SQL Server builder
* Better support for SQL Server Elastic Pools
* Clean up documentation

## 0.15.0
* Improvements to PostgreSQL
* Unique Key support in CosmosDB
* Azure Maps support
* Service Bus topic support
* SignalR support
* Elastic Pool SQL Azure support
* More tests
* Better VNet and Subnet support in VMs
* Make any Builder a dependency
* Respect HTTPSOnly flag in Functions

## 0.14.0
* Support for extensible plugin-model
* Express Route support
* Service Bus queue support
* IOT Hub support
* PostreSQL support

## 0.13.0
* Support for executing a deployment with fast fail
* ARM template Validation support
* Service Bus Queues support
* Azure Container Registry support
* Refactor some values to improve type safety

## 0.12.3
* Improve support for Azure CLI on Linux

## 0.12.2
* More resilient version checking
* Better parameterisation for Docker credentials
* Avoid duplicate parameters

## 0.12.1
* Minimum version check on Azure CLI (2.3.1)
* List all subscriptions
* Set minimum subscription

## 0.12.0
* Rename "db_name" keywords to just "name" (consistency)
* Improve CLI access on Windows
* Better CLI error handling on Linux & Mac
* Azure Container Registry support for Web Apps
* Support for providing multiple settings at once on WebApp and Functions

## 0.11.1
* Fix a bug with deploy parameterisation

## 0.11.0
* Remove REST API support
* Enhance Azure CLI support
* Support for optional Azure CLI authentication

## 0.10.0
* Allow supplying an explicit related service plan
* Support for HTTPS-only on web app
* Block when deploying via Azure CLI
* Put all deploy transient files in a folder
* Server Farm builder
* Don't login on Azure CLI unless needed

## 0.9.1
* Fix a bug with WebApp builder causing a stack overflow

## 0.9.0
* Support for Cognitive Services
* Ensure Functions Runtime is correct set (lower-case)
* Support for Docker Hub on Web Apps

## 0.8.0
* Improved support for What-If API
* Post-deployment Web Deploy for App Service

## 0.7.0
* Minor bug fixes
* Simplify API for hierarchical resources e.g. Containers, Cosmos, SQL Azure, WebApps and Functions
* Support for Validation API before deploying
* Basic support for What-If API
* Error handling on deployment status updates

## 0.6.0
* Client Secret is now a string
* Sanitise storage accounts automatically
* Improvements to Redis and Event Hub
* Restrict adding resources to supported types

## 0.5.0
* Support for Redis Cache
* Support for Event Hub
* Fixes for Web Apps on Linux
* Remove unnecessary site extension for App Insights on Web Apps

## 0.4.0
* Upgrade to netcore3.1
* Support for REST API deployment using SPI credentials
* Refactor code to simplify and separate writing and deployment
* Fix a couple of small bugs with overloads of keywords in builders

## 0.3.0
* Quick deploy support for Linux and Mac
* Automatic password generation for quick deploy
* SQL Connection String property on database
* Re-introduced *limited* support for parameter expressions
* Support for configuration of Functions runtime

## 0.2.0
* KeyVault support
* Location type
* Fixed a bug regarding Worker Size
* Null elements are now omitted from generated templates

## 0.1.0
* Initial Release

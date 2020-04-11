Release Notes
=============
## 0.9.1
* Fix a bug with WebApp builder causing a stack overflow.
*
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
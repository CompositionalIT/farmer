---
title: "SQL Azure"
date: 2020-02-05T08:53:46+01:00
weight: 19
chapter: false
---

#### Overview
The SQL Azure builder is used to called SQL Azure servers and databases. It supports features such as encryption and firewalls. Every SQL Azure server you create will automatically create a SecureString parameter for the admin account password.
If you wish to create a SQL Database attached to an existing server, use the `link_to_server` keyword and supply the resource name of the existing server.

#### Builder Keywords
| Applies To | Keyword | Purpose |
|-|-|-|
| Database | name | Sets the name of the database. |
| Database | sku | Sets the sku of the database. |
| Database | collation | Sets the collation of the database. |
| Database | use_encryption | Enables transparent data encryption of the database. |
| Database | link_to_server | Links this database to an existing SQL Azure server instead of creating a new one. |
| Server | server_name | Sets the name of the SQL server. |
| Server | add_firewall_rule | Adds a custom firewall rule given a name, start and end IP address range. |
| Server | enable_azure_firewall | Adds a firewall rule that enables access to other Azure services. |
| Server | admin_username | Sets the admin username of the server. |

#### Configuration Members
| Member | Purpose |
|-|-|
| FullyQualifiedDomainName | Gets the ARM expression path to the FQDN of this SQL instance. |
| ConnectionString | Gets a literal .NET connection string using the administrator username / password. The password will be evaluated based on the contents of the password parameter supplied to the template at deploy time. |

#### Example
TBD
---
title: "SQL Azure"
date: 2020-02-05T08:53:46+01:00
weight: 4
chapter: false
---

#### Overview
The SQL Azure builder is used to called SQL Azure servers and databases. It supports features such as encryption and firewalls. Every SQL Azure instance you create will automatically create a SecureString parameter for the admin account password.

#### Builder Keywords
| Keyword | Purpose |
|-|-|
| server_name | Sets the name of the SQL server. |
| db_name | Sets the name of the database. |
| sku | Sets the sku of the database. |
| collation | Sets the collation of the database. |
| use_encryption | Enables encryption of the database. |
| add_firewall_rule | Adds a custom firewall rule given a name, start and end IP address range. |
| enable_azure_firewall | Adds a firewall rule that enables access to other Azure services. |
| admin_username | Sets the admin username of the server. |

#### Configuration Members
| Member | Purpose |
|-|-|
| FullyQualifiedDomainName | Gets the ARM expression path to the FQDN of this SQL instance. |
| ConnectionString | Gets a literal .NET connection string using the administrator username / password. The password will be evaluated based on the contents of the password parameter supplied to the template at deploy time. |

#### Example
TBD
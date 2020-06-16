---
title: "Farmer and ARM"
date: 2020-05-23T18:11:25+02:00
draft: false
---

| | Farmer | ARM Template |
|-|-:|:-|
| **Core ARM features** |
| Repeatable deployments? | **Yes**, Farmer runs on top of ARM | **Yes** |
| ARM deployment mechanisms? | **All**, plus easy-to-use F# deployment | **All** |
| Variables support? | **Yes**, native support in F# | **Yes** |
| Parameters support? | **Yes**, native support in F# or secure parameters | **Yes** |
| Supported resources? | **All**, including **custom builders for 30+ popular resources**  | **All** |
| Declarative model support? | **Yes** | **Yes** |
| Support for all ARM tools? | **Yes**, Farmer runs on top of ARM | **Yes** |
| Linked Template support? | **No** - generally not required. | **Yes** |
| **Authoring** |
| Easy to author? | **Yes** | **No** |
| Easy to read? | **Yes** | **No** |
| Documented? | **Yes**, website and discoverable intellisense | **Limited**, documented but often out-of-date |
| Editor support? | **Yes**, any F# editor including VS Code, VS and Rider | **Limited**, only VS Code has any support |
| **Safety** |
| Type-safe? | **Yes**, full support from the F# compiler and type system | **Limited** through VS Code extension and LSP |
| Validation support? | **Edit-time, run-time, deploy-time** | **Deploy-time and limited edit-time** |
| **Flexibility** |
| Link resources easily? | **Yes** | **Not easily** complex path expressions must be known |
| Compose resources together? | **Yes** | **Not easily** |
| Create multiple resources simultaneously? | **Yes** | **No**, each resource must be defined separately |
| Create resources in several ways? | **Yes**, builders, records, functions or classes | **No**, must use JSON |
| Full programming language? | **Yes**, F# is a simple yet powerful programming language | **No**, JSON with limited functions |
| Imperative model? | **Yes**, F# supports imperative programming | **No**, you must program in a declarative style |
| **Interop and extensibility** |
| Add your own ARM resources? | **Yes**, plug-in model to add new ARM resources | **N/A**
| Create your own combinations of resources? | **Yes** | **No**, each resource must be defined separately |
| Use external libraries? | **Yes**, use any NuGet packages during authoring and full .NET Core | **No**, fixed set of functions |
| Use in .NET applications? | **Yes**, Farmer is a .NET Core library and can be used in-proc | **No**, JSON files |
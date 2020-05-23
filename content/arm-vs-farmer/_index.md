---
title: "Farmer and ARM"
date: 2020-05-23T18:11:25+02:00
draft: false
---

| | Farmer | ARM Template |
|-|-:|:-|
| Repeatable deployments? | **Yes** Farmer runs on top of ARM | **Yes** |
| ARM deployment mechanisms? | **All** | **All** |
| Variables support? | **Yes** Built into F# | **Yes** |
| Parameters support? | **Yes** Built into F#, or secure parameters | **Yes** |
| Supported resources? | **30+ popular resources** with more added regularly | **All** |
| Declarative model support? | **Yes** | **Yes** |
|||
| Easy to author? | **Yes** | **No** |
| Easy to read? | **Yes** | **No** |
| Documented? | **Yes**, website and discoverable intellisense | **Limited** documented but often out-of-date |
| Succinct syntax? | **Yes** | **No** |
| Type-safe? | **Yes**, F# has a mature and powerful compiler | **Limited** to LSP - and only for VS Code |
| Validation support? | **Edit-time, template generation and run-time** | **Run-time and limited edit-time** |
| Link resources easily? | **Yes** | **Not easily** complex path expressions must be known |
| Compose resources together? | **Yes** | **Not easily** |
| Treat multiple resources as one? | **Yes** | **No**, each resource must be defined separately |
| Create resources in several ways? | **Yes**, builders, records, functions or classes | **No**, must use JSON |
| Add your own ARM resources | **Yes**, plug-in model allows supplying your own implementations | **N/A**
| Create your own combinations of resources? | **Yes** | **No**, each resource must be defined separately |
| Full programming language? | **Yes**, F# is a simple yet powerful programming language | **No**, JSON with limited functions |
| Imperative model? | **Yes**, F# supports imperative programming | **No**, you must program in a declarative style |
| Extensible? | **Yes**, use any NuGet packages during authoring and full .NET Core | **No**, fixed set of functions |
| Use in .NET applications? | **Yes** Farmer is a .NET Core library so can be used in-proc | **No**, JSON files |
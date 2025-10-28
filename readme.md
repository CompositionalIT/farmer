![](Logo.png)

## Farmer makes repeatable Azure deployments easy!

See the full docs [here](https://compositionalit.github.io/farmer).

Want to edit the docs? Check out the [docs folder](https://github.com/CompositionalIT/farmer/tree/master/docs).

[![Build Status](https://compositional-it.visualstudio.com/Farmer/_apis/build/status/CompositionalIT.farmer?branchName=master)](https://compositional-it.visualstudio.com/Farmer/_build/latest?definitionId=14&branchName=master)

[![Farmer on Nuget](https://img.shields.io/nuget/dt/Farmer?label=NuGet%20Downloads)](https://www.nuget.org/packages/farmer/)

## TLS Version Support

**Note:** As of version 1.9.24 (October 2025), Farmer has:
- Added support for TLS 1.3
- Deprecated TLS 1.0 and TLS 1.1 (marked as obsolete)

If you see compiler warnings about `Tls10` or `Tls11` being obsolete, these versions now automatically fall back to TLS 1.2 for security reasons. Update your code to use `Tls12` or `Tls13` explicitly to remove the warnings.

module Program

open Expecto
open System
open Farmer

[<Tests>]
let allTests =
    testSequencedGroup "" <|
        testList "All Tests" [
            testList "Builders" [
                LogAnalytics.tests
                AppInsights.tests
                Bastion.tests
                Cdn.tests
                CognitiveServices.tests
                ContainerGroup.tests
                ContainerService.tests
                DeploymentScript.tests
                Dns.tests
                EventHub.tests
                IotHub.tests
                Storage.tests
                ContainerRegistry.tests
                ExpressRoute.tests
                KeyVault.tests
                NetworkSecurityGroup.tests
                ServiceBus.tests
                VirtualMachine.tests
                PostgreSQL.tests
                Cosmos.tests
                Maps.tests
                SignalR.tests
                Sql.tests
                EventGrid.tests
                WebApp.tests
                Functions.tests
                StaticWebApp.tests
                VirtualNetworkGateway.tests
                Databricks.tests
                JsonRegression.tests
                AzCli.tests
            ]
            testList "Control" [
                Template.tests
                Identity.tests
                Common.tests
                if Environment.GetEnvironmentVariable "TF_BUILD" = "True" then AzCli.endToEndTests
            ]
        ]

[<EntryPoint>]
let main _ =
    printfn "Running tests!"
    runTests { defaultConfig with verbosity = Logging.Info } allTests
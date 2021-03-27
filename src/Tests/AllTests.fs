module Program

open Expecto
open System
open Farmer

let hasEnv a b = Environment.GetEnvironmentVariable a = b

[<Tests>]
let allTests =
    testSequencedGroup "" <|
        testList "All Tests" [
            testList "Builders" [
                DiagnosticSettings.tests
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
                Types.tests
                AzCli.tests
            ]
            testList "Control" [
                Template.tests
                Identity.tests
                Common.tests
                if hasEnv "TF_BUILD" "True" || hasEnv "FARMER_E2E" "True" then AzCli.endToEndTests
            ]
        ]

[<EntryPoint>]
let main _ =
    printfn "Running tests!"
    runTests { defaultConfig with verbosity = Logging.Info } allTests
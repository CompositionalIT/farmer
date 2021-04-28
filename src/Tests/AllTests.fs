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
                AppInsights.tests
                DiagnosticSettings.tests
                Functions.tests
                LogAnalytics.tests
                Bastion.tests
                CommunicationServices.tests
                BingSearch.tests
                Cdn.tests
                ContainerGroup.tests
                ContainerService.tests
                CognitiveServices.tests
                Dns.tests
                ExpressRoute.tests
                EventHub.tests
                NetworkSecurityGroup.tests
                Storage.tests
                ServiceBus.tests
                IotHub.tests
                AzCli.tests
                Cosmos.tests
                KeyVault.tests
                VirtualMachine.tests
                ContainerRegistry.tests
                PostgreSQL.tests
                Maps.tests
                Sql.tests
                SignalR.tests
                EventGrid.tests
                WebApp.tests
                StaticWebApp.tests
                VirtualNetworkGateway.tests
                DeploymentScript.tests
                Databricks.tests
                JsonRegression.tests
                Types.tests
            ]
            testList "Control" [
                Common.tests
                Template.tests
                Identity.tests
                if hasEnv "TF_BUILD" "True" || hasEnv "FARMER_E2E" "True" then AzCli.endToEndTests
            ]
        ]

[<EntryPoint>]
let main _ =
    printfn "Running tests!"
    runTests { defaultConfig with verbosity = Logging.Info } allTests
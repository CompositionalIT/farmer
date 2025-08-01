module Program

open Expecto
open System
open Farmer

module Build =
    let hasEnv a b =
        Environment.GetEnvironmentVariable a = b

    let notEnv a b =
        Environment.GetEnvironmentVariable a <> b

    let isPullRequest = hasEnv "BUILD_REASON" "PullRequest"
    let isCIBuild = hasEnv "TF_BUILD" "True"
    let isFarmerEndToEnd = hasEnv "FARMER_E2E" "True"
    let isCiMaster = not isPullRequest && isCIBuild

[<Tests>]
let allTests =
    testSequencedGroup ""
    <| testList "All Tests" [
        testList "Builders" [
            AppGateway.tests
            AppInsights.tests
            AppInsightsAvailability.tests
            if Build.isCiMaster then
                AzCli.tests
            AutoscaleSettings.tests
            AzureFirewall.tests
            B2cTenant.tests
            Bastion.tests
            BingSearch.tests
            Cdn.tests
            CognitiveServices.tests
            CommunicationServices.tests
            ContainerApps.tests
            ContainerGroup.tests
            ContainerRegistry.tests
            ContainerService.tests
            Cosmos.tests
            Databricks.tests
            DedicatedHosts.tests
            DeploymentScript.tests
            DiagnosticSettings.tests
            Disk.tests
            Dns.tests
            DnsResolver.tests
            EventGrid.tests
            EventHub.tests
            ExpressRoute.tests
            Functions.tests
            IotHub.tests
            Gallery.tests
            ImageTemplate.tests
            JsonRegression.tests
            KeyVault.tests
            Network.tests
            LoadBalancer.tests
            LogAnalytics.tests
            LogicApps.tests
            Maps.tests
            NetworkSecurityGroup.tests
            OperationsManagement.tests
            PostgreSQL.tests
            PrivateLink.tests
            ResourceGroup.tests
            RoleAssignment.tests
            ServiceBus.tests
            SignalR.tests
            Sql.tests
            StaticWebApp.tests
            Storage.tests
            TrafficManager.tests
            Types.tests
            VirtualHub.tests
            VirtualMachine.tests
            VmScaleSet.tests
            VirtualNetworkGateway.tests
            VirtualWan.tests
            WebApp.tests
            Dashboards.tests
            Alerts.tests
            ServicePlan.tests
            ActionGroup.tests
        ]
        testList "Control" [
            // Temporarily disabling end to end tests while transitioning to new subscription.
            //if Build.isCiMaster || Build.isFarmerEndToEnd then
            //    AzCli.endToEndTests
            Common.tests
            Identity.tests
            Template.tests
        ]
    ]

[<EntryPoint>]
let main _ =
    printfn "Running tests!"

    runTestsWithCLIArgs
        [
            Verbosity Logging.LogLevel.Info
            if Build.isCIBuild then
                Fail_On_Focused_Tests
        ]
        [||]
        allTests
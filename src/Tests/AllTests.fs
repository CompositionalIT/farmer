module Program

open Expecto
open System

[<Tests>]
let allTests =
    testSequencedGroup "" <|
        testList "All Tests" [
            testList "Builders" [
                ContainerGroup.tests
                Storage.tests
                ContainerRegistry.tests
                ExpressRoute.tests
                VirtualMachine.tests
            ]
            testList "Control" [
                Template.tests
                if Environment.GetEnvironmentVariable "TF_BUILD" = "True" then AzCli.tests
            ]
        ]

[<EntryPoint>]
let main _ =
    printfn "Running tests!"
    runTests { defaultConfig with verbosity = Logging.LogLevel.Debug } allTests
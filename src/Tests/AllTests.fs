module Program

open Expecto
open System

[<Tests>]
let allTests =
    testList "All Tests" [
        testList "Control" [
            Template.tests
            if Environment.GetEnvironmentVariable "TF_BUILD" = "True" then AzCli.tests
        ]
        testList "Builders" [
            ContainerGroup.tests
            Storage.tests
            ContainerRegistry.tests
        ]
    ]

[<EntryPoint>]
let main _ =
    printfn "Running tests!"
    runTests { defaultConfig with verbosity = Logging.LogLevel.Verbose } allTests
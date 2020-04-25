module Program

open Expecto
open System

[<Tests>]
let allTests =
    testList "All Tests" [
        Template.tests
        ContainerGroup.tests
        if Environment.GetEnvironmentVariable "TF_BUILD" = "True" then
            AzCli.tests
    ]
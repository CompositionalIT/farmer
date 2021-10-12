module AppGateway

open Expecto
open Farmer.Builders.ApplicationGateway

let tests = testList "Application Gateway Tests" [
    ftest "No AppGateway yet" {
        let ag =
            appGateway {
                name "appGateway"
            }
        ()
    }
]
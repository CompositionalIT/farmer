module AppGateway

open Expecto

module Builders =
    type AppGatewayConfig = {
        Name: string
    }

    type AppGatewayBuilder () =
        member _.Yield _ : AppGatewayConfig = {
            Name = ""
        }
        [<CustomOperation "name">]
        member _.Name (config:AppGatewayConfig, name:string) =
            { config with Name = name }

    let appGateway = AppGatewayBuilder ()

open Builders

let tests = testList "Application Gateway Tests" [
    ftest "No AppGateway yet" {
        let ag =
            appGateway {
                name "appGateway"
            }
        ()
    }
]
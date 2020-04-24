module AzCli

open Xunit
open Farmer

[<Fact>]
let ``Can connect to Az CLI``() =
    match Deploy.checkVersion Deploy.Az.MinimumVersion with
    | Ok _ -> ()
    | Error x -> failwithf "Version check failed: %s" x

[<Fact>]
let ``If parameters are missing, deployment is immediately rejected``() =
    let deployment = Template.TestHelpers.createSimpleDeployment [] [ "p1" ]
    let result = deployment |> Deploy.execute "sample-rg" [] |> sprintf "%A"
    Assert.Equal(result, Error "The following parameters are missing: p1." |> sprintf "%A")
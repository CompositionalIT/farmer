module Tests

open Farmer
open System
open System.IO
open Xunit
open Newtonsoft.Json

[<Fact>]
let ``Can create a basic template`` () =
    arm { location NorthEurope } |> Writer.quickWrite "basic-template"
    let template = File.ReadAllText "basic-template.json" |> JsonConvert.DeserializeObject<{| ``$schema`` : Uri; outputs : Map<string,string>; parameters : Map<string,string>; resources : obj array |}>
    Assert.Equal(template.``$schema``, Uri "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#")
    Assert.Empty template.outputs
    Assert.Empty template.parameters
    Assert.Empty template.resources

[<Fact>]
let ``Can connect to Az CLI``() =
    match Deploy.checkVersion Deploy.Az.MinimumVersion with
    | Ok _ -> ()
    | Error x -> failwithf "%s" x
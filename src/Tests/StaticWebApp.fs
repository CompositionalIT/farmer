module StaticWebApp

open Expecto
open Farmer
open Farmer.Builders
open Farmer.WebApp
open Farmer.Arm
open System

let tests = testList "Static Web App Tests" [
    test "Creates a basic static web app" {
        let swa = staticWebApp {
            name "foo"
            api_location "api"
            app_location "app"
            artifact_location "artifact"
            branch "feature"
            repository "https://compositional-it.com"
        }
        let swaArm = (swa :> IBuilder).BuildResources(Location.WestEurope).[0] :?> StaticSite
        Expect.equal swaArm.ApiLocation (Some "api") "Api"
        Expect.equal swaArm.Name (ResourceName "foo") "Name"
        Expect.equal swaArm.AppLocation "app" "AppLocation"
        Expect.equal swaArm.AppArtifactLocation (Some "artifact") "ArtifactLocation"
        Expect.equal swaArm.Branch "feature" "Branch"
        Expect.equal swaArm.Repository (Uri "https://compositional-it.com") "Repository"
    }
    test "Defaults to master branch" {
        let swa = staticWebApp {
            name "foo"
            repository "https://compositional-it.com"
        }
        let swaArm = (swa :> IBuilder).BuildResources(Location.WestEurope).[0] :?> StaticSite
        Expect.equal swaArm.Branch "master" "Branch"
    }
    test "Supports app settings" {
        let swa = staticWebApp {
            name "foo"
            repository "https://compositional-it.com"
            app_settings [
                "foo", "bar"
                "blip", "blop"
            ]

        }
        let swaArm = (swa :> IBuilder).BuildResources(Location.WestEurope).[1] :?> StaticSites.Config
        Expect.equal swaArm.Properties (Map [ "foo", "bar"; "blip", "blop"]) "App Settings not set"
    }
]
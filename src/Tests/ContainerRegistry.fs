module ContainerRegistry

open Expecto
open Farmer
open Farmer.ContainerRegistry
open Farmer.Builders
open System.Collections.Generic

type RegistryJson = {
    resources:
        {|
            name: string
            ``type``: string
            apiVersion: string
            sku: {| name: string |}
            location: string
            properties: IDictionary<string, System.Text.Json.JsonElement>
        |} array
}

let toTemplate loc (d: ContainerRegistryConfig) =
    let a = arm {
        location loc
        add_resource d
    }

    a.Template

let ofJson = Serialization.ofJson<RegistryJson>
let resource (r: RegistryJson) = r.resources.[0]

let whenWritten deploy =
    deploy |> toTemplate Location.NorthEurope |> Writer.toJson |> ofJson
// resource assertions
let shouldHaveName name (r: RegistryJson) =
    Expect.equal name (resource(r).name) "Resource names do not match"
    r

let shouldHaveSku (sku: Sku) (r: RegistryJson) =
    Expect.equal (sku.ToString()) (resource(r).sku.name) "SKUs do not match"
    r

let shouldHaveType t (r: RegistryJson) =
    Expect.equal t (resource(r).``type``) "Types do not match"
    r

let shouldHaveApiVersion v (r: RegistryJson) =
    Expect.equal v (resource(r).apiVersion) "API version is not expected version"
    r

let shouldHaveALocation (r: RegistryJson) =
    Expect.isNotEmpty (resource(r).location) "Location should be set"
    r
// property assertions
// admin user
let shouldHaveAdminUserEnabled (r: RegistryJson) =
    let b = resource(r).properties.["adminUserEnabled"].GetBoolean()
    Expect.isTrue b "adminUserEnabled was expected to be enabled"
    r

let shouldHaveAdminUserDisabled (r: RegistryJson) =
    let b = resource(r).properties.["adminUserEnabled"].GetBoolean()
    Expect.isFalse b "adminUserEnabled was expected to be disabled"
    r

let tests =
    testList "Container Registry" [
        test "Basic resource settings are written to template resource" {
            containerRegistry {
                name "validContainerRegistryName"
                sku Premium
            }
            |> whenWritten
            |> shouldHaveType "Microsoft.ContainerRegistry/registries"
            |> shouldHaveApiVersion "2019-05-01"
            |> shouldHaveName "validContainerRegistryName"
            |> shouldHaveSku Premium
            |> shouldHaveALocation
            |> shouldHaveAdminUserDisabled
            |> ignore
        }

        test "When enable_admin_user is set it is written to resource properties" {
            containerRegistry {
                name "validContainerRegistryName"
                enable_admin_user
            }
            |> whenWritten
            |> shouldHaveAdminUserEnabled
            |> ignore
        }

        testList "Container Registry Name Validation tests" [
            let invalidNameCases = [
                "Empty Account", "", "cannot be empty", "Name too short"
                "Min Length", "abc", "min length is 5, but here is 3. The invalid value is 'abc'", "Name too short"
                "Max Length",
                "abcdefghij1234567890abcde12345678901234567890abcdef",
                "max length is 50, but here is 51. The invalid value is 'abcdefghij1234567890abcde12345678901234567890abcdef'",
                "Name too long"
                "Non alphanumeric",
                "abcde!",
                "can only contain alphanumeric characters. The invalid value is 'abcde!'",
                "Value contains non-alphanumeric characters"
            ]

            for testName, containerRegisterName, error, why in invalidNameCases do
                test testName {
                    Expect.equal
                        (ContainerRegistryValidation.ContainerRegistryName.Create containerRegisterName)
                        (Error("Container Registry Name " + error))
                        why
                }

            let validNameCases = [
                "Valid Name 1", "abcde", "Should have created a valid Container Registry name"
                "Valid Name 2", "abc123", "Should have created a valid Container Registry name"
            ]

            for testName, containerRegisterName, why in validNameCases do
                test testName {
                    Expect.equal
                        (ContainerRegistryValidation.ContainerRegistryName
                            .Create(containerRegisterName)
                            .OkValue.ResourceName)
                        (ResourceName containerRegisterName)
                        why
                }
        ]
    ]
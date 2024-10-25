module Sql

open Expecto
open Farmer
open Farmer.Arm
open Farmer.Sql
open Farmer.Builders
open Microsoft.Azure.Management.Sql
open System
open Microsoft.Rest
open System.Text.Json
open System.Text.Json.Nodes

let client =
    new SqlManagementClient(Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let tests =
    testList "SQL Server" [
        test "Can create a basic server and DB" {
            let sql = sqlServer {
                name "server"
                admin_username "isaac"

                add_databases [
                    sqlDb {
                        name "db"
                        sku DtuSku.S0
                    }
                ]
            }

            let model: Models.Server = sql |> getResourceAtIndex client.SerializationSettings 0
            Expect.equal model.Name "server" "Incorrect Server name"
            Expect.equal model.AdministratorLogin "isaac" "Incorrect Administration Login"

            let model: Models.Database =
                sql |> getResourceAtIndex client.SerializationSettings 1

            Expect.equal model.Name "server/db" "Incorrect database name"
            Expect.equal model.Sku.Name "S0" "Incorrect SKU"
        }

        test "Transparent data encryption name" {
            let sql = sqlServer {
                name "server"
                admin_username "isaac"

                add_databases [
                    sqlDb {
                        name "db"
                        use_encryption
                        sku DtuSku.S0
                    }
                ]
            }

            let encryptionModel: Models.TransparentDataEncryption =
                sql |> getResourceAtIndex client.SerializationSettings 2

            Expect.equal encryptionModel.Name "server/db/current" "Should always equal to current"
        }

        test "Creates an elastic pool where needed" {
            let sql = sqlServer {
                name "server"
                admin_username "isaac"
                elastic_pool_sku PoolSku.Basic200
                add_databases [ sqlDb { name "db" } ]
            }

            let model: Models.Database =
                sql |> getResourceAtIndex client.SerializationSettings 1

            Expect.isNull model.Sku "Should not be a SKU on the DB"

            Expect.equal
                "[resourceId('Microsoft.Sql/servers/elasticPools', 'server', 'server-pool')]"
                model.ElasticPoolId
                "Incorrect pool reference"

            let model: Models.ElasticPool =
                sql |> getResourceAtIndex client.SerializationSettings 2

            Expect.equal model.Sku.Name "BasicPool" "Incorrect Elastic Pool SKU"
            Expect.equal model.Sku.Capacity (Nullable 200) "Incorrect Elastic Pool SKU size"
        }

        test "Works with VCore databases" {
            let sql = sqlServer {
                name "server"
                admin_username "isaac"

                add_databases [
                    sqlDb {
                        name "db"
                        sku M_18
                    }
                ]
            }

            let model: Models.Database =
                sql |> getResourceAtIndex client.SerializationSettings 1

            Expect.equal model.Sku.Name "BC_M_18" "Incorrect SKU"
            Expect.equal model.LicenseType "LicenseIncluded" "Incorrect License"
        }

        test "Cannot set hybrid if not VCore" {
            Expect.throws
                (fun () ->
                    sqlServer {
                        name "server"
                        admin_username "isaac"

                        add_databases [
                            sqlDb {
                                name "db"
                                hybrid_benefit
                            }
                        ]
                    }
                    |> ignore)
                "Shouldn't set hybrid on non-VCore"
        }

        test "Sets license and size correctly" {
            let sql = sqlServer {
                name "server"
                admin_username "isaac"

                add_databases [
                    sqlDb {
                        name "db"
                        sku (GeneralPurpose Gen5_12)
                        hybrid_benefit
                        db_size 2048<Mb>
                    }
                ]
            }

            let model: Models.Database =
                sql |> getResourceAtIndex client.SerializationSettings 1

            Expect.equal model.Sku.Name "GP_Gen5_12" "Incorrect SKU"
            Expect.equal model.MaxSizeBytes (Nullable 2147483648L) "Incorrect Size"
            Expect.equal model.LicenseType "BasePrice" "Incorrect SKU"
        }

        test "SQL Firewall is correctly configured" {
            let sql = sqlServer {
                name "server"
                admin_username "isaac"
                add_firewall_rule "Rule" "0.0.0.0" "255.255.255.255"
                add_databases [ sqlDb { name "db" } ]
            }

            let model: Models.FirewallRule =
                sql |> getResourceAtIndex client.SerializationSettings 2

            Expect.equal model.StartIpAddress "0.0.0.0" "Incorrect start IP"
            Expect.equal model.EndIpAddress "255.255.255.255" "Incorrect end IP"
        }

        test "SQL Firewall is correctly configured with list" {
            let sql = sqlServer {
                name "server"
                admin_username "isaac"
                add_firewall_rules [ "Rule", "0.0.0.0", "255.255.255.255" ]
                add_databases [ sqlDb { name "db" } ]
            }

            let model: Models.FirewallRule =
                sql |> getResourceAtIndex client.SerializationSettings 2

            Expect.equal model.StartIpAddress "0.0.0.0" "Incorrect start IP"
            Expect.equal model.EndIpAddress "255.255.255.255" "Incorrect end IP"
        }

        test "Validation occurs on account name" {
            let check (v: string) m =
                Expect.equal (SqlAccountName.Create v) (Error("SQL account names " + m))

            check "" "cannot be empty" "Name too short"
            let longName = Array.init 64 (fun _ -> 'a') |> String
            check longName $"max length is 63, but here is 64. The invalid value is '{longName}'" "Name too long"

            check
                "zzzT"
                "can only contain lowercase letters. The invalid value is 'zzzT'"
                "Upper case character allowed"

            check
                "zz!z"
                "can only contain alphanumeric characters or the dash (-). The invalid value is 'zz!z'"
                "Bad character allowed"

            check "-zz" "cannot start with a dash (-). The invalid value is '-zz'" "Start with dash"
            check "zz-" "cannot end with a dash (-). The invalid value is 'zz-'" "End with dash"
        }

        test "Sets Min TLS version correctly" {
            let sql = sqlServer {
                name "server"
                admin_username "isaac"

                add_databases [
                    sqlDb {
                        name "db"
                        sku DtuSku.S0
                    }
                ]

                min_tls_version Tls12
            }

            let model: Models.Server = sql |> getResourceAtIndex client.SerializationSettings 0
            Expect.equal model.Name "server" "Incorrect Server name"
            Expect.equal model.MinimalTlsVersion "1.2" "Min TLS version is wrong"
        }

        test "Test Geo-replication" {
            let sql = sqlServer {
                name "my36server"
                admin_username "isaac"

                add_databases [
                    sqlDb {
                        name "mydb21"
                        sku DtuSku.S0
                    }
                ]

                geo_replicate (
                    {
                        NameSuffix = "geo"
                        Location = Location.UKWest
                        DbSku = Some Farmer.Sql.DtuSku.S0
                    }
                )
            }

            let template = arm {
                location Location.UKSouth
                add_resources [ sql ]
            }

            let jsn = template.Template |> Writer.toJson
            let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

            let geoLocated =
                jobj.SelectToken("resources[?(@.name=='my36servergeo/mydb21geo')].location")

            Expect.equal (geoLocated.ToString()) "ukwest" "Geo-replication with location not found"
            ()
        }

        test "Serverless sql has min and max capacity" {
            let sql = sqlServer {
                name "my37server"
                admin_username "isaac"

                add_databases [
                    sqlDb {
                        name "mydb22"
                        sku (GeneralPurpose(S_Gen5(2, 4)))
                    }
                ]
            }

            let template = arm {
                location Location.UKSouth
                add_resources [ sql ]
            }

            let jsn = template.Template |> Writer.toJson
            let jobj = jsn |> Newtonsoft.Json.Linq.JObject.Parse

            Expect.equal
                (jobj
                    .SelectToken("resources[?(@.name=='my37server/mydb22')].sku.name")
                    .ToString())
                "GP_S_Gen5"
                "Not serverless name"

            Expect.equal
                (jobj
                    .SelectToken("resources[?(@.name=='my37server/mydb22')].sku.capacity")
                    .ToString())
                "4"
                "Incorrect max capacity"

            Expect.equal
                (jobj
                    .SelectToken("resources[?(@.name=='my37server/mydb22')].properties.minCapacity")
                    .ToString())
                "2"
                "Incorrect min capacity"

            Expect.equal
                (jobj
                    .SelectToken("resources[?(@.name=='my37server/mydb22')].properties.autoPauseDelay")
                    .ToString())
                "-1"
                "Incorrect autoPauseDelay"
        }

        test "Must set either SQL Server or AD authentication" {
            Expect.throws (fun () -> sqlServer { name "test" } |> ignore) "Should throw if no auth set"
        }

        test "Can use Entra ID auth" {
            let server = sqlServer {
                name "my-sql-server"

                entra_id_admin
                    "entra-user"
                    (ObjectId(Guid.Parse "f9d49c34-01ba-4897-b7e2-3694bf3de2cf"))
                    PrincipalType.User
            }

            let template = arm { add_resource server }

            let json = template.Template |> Writer.toJson |> JsonObject.Parse
            let adminToken = json.["resources"].[0].["properties"].["administrators"]

            Expect.equal (adminToken["administratorType"].GetValue()) "ActiveDirectory" "Incorrect administrator type"
            Expect.equal (adminToken["login"].GetValue()) "entra-user" "Incorrect AD login name"
            Expect.equal (adminToken["principalType"].GetValue()) "User" "Incorrect principal type"
            Expect.equal (adminToken["sid"].GetValue()) "f9d49c34-01ba-4897-b7e2-3694bf3de2cf" "Incorrect SID"
            Expect.isTrue (adminToken["azureADOnlyAuthentication"].GetValue()) "Should only have AD auth."
        }

        test "No Entra ARM when just using SQL" {
            let theServer = sqlServer {
                name "my-sql-server"
                admin_username "test"
            }

            let template = arm { add_resource theServer }
            let json = template.Template |> Writer.toJson |> JsonObject.Parse
            let properties = json.["resources"].[0].["properties"].AsObject()
            Expect.isFalse (properties.ContainsKey "administrators") "Should not have an AD admin"
        }

        test "Can set both SQL and Entra ID auth" {
            let theServer = sqlServer {
                name "my-sql-server"
                admin_username "test"
                entra_id_admin "" (ObjectId Guid.Empty) PrincipalType.User
            }

            let template = arm { add_resource theServer }
            let json = template.Template |> Writer.toJson |> JsonObject.Parse

            let azureAdOnlyAuth =
                json.["resources"].[0].["properties"].["administrators"].["azureADOnlyAuthentication"]

            Expect.isFalse (azureAdOnlyAuth.GetValue()) "Should not only have AD auth."
        }
    ]
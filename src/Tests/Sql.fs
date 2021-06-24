module Sql

open Expecto
open Farmer
open Farmer.Sql
open Farmer.Builders
open Microsoft.Azure.Management.Sql
open System
open Microsoft.Rest

let sql = sqlServer

let client = new SqlManagementClient (Uri "http://management.azure.com", TokenCredentials "NotNullOrWhiteSpace")

let tests = testList "SQL Server" [
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
        let model : Models.Server = sql |> getResourceAtIndex client.SerializationSettings 0
        Expect.equal model.Name "server" "Incorrect Server name"
        Expect.equal model.AdministratorLogin "isaac" "Incorrect Administration Login"

        let model : Models.Database = sql |> getResourceAtIndex client.SerializationSettings 1
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

        let encryptionModel : Models.TransparentDataEncryption = sql |> getResourceAtIndex client.SerializationSettings 2
        Expect.equal encryptionModel.Name "server/db/current" "Should always equal to current"
    }

    test "Creates an elastic pool where needed" {
        let sql = sqlServer {
            name "server"
            admin_username "isaac"
            elastic_pool_sku PoolSku.Basic200
            add_databases [ sqlDb { name "db" } ]
        }
        let model : Models.Database = sql |> getResourceAtIndex client.SerializationSettings 1
        Expect.isNull model.Sku "Should not be a SKU on the DB"
        Expect.equal "[resourceId('Microsoft.Sql/servers/elasticPools', 'server', 'server-pool')]" model.ElasticPoolId "Incorrect pool reference"

        let model : Models.ElasticPool = sql |> getResourceAtIndex client.SerializationSettings 2
        Expect.equal model.Sku.Name "BasicPool" "Incorrect Elastic Pool SKU"
        Expect.equal model.Sku.Size "200" "Incorrect Elastic Pool SKU size"
    }

    test "Works with VCore databases" {
        let sql = sqlServer {
            name "server"
            admin_username "isaac"
            add_databases [ sqlDb { name "db"; sku M_18 } ]
        }

        let model : Models.Database = sql |> getResourceAtIndex client.SerializationSettings 1
        Expect.equal model.Sku.Name "BC_M_18" "Incorrect SKU"
        Expect.equal model.LicenseType "LicenseIncluded" "Incorrect License"
    }

    test "Cannot set hybrid if not VCore" {
        Expect.throws(fun () ->
            sqlServer {
                name "server"
                admin_username "isaac"
                add_databases [ sqlDb { name "db"; hybrid_benefit } ]
            } |> ignore) "Shouldn't set hybrid on non-VCore"
    }

    test "Sets license and size correctly" {
        let sql =
            sqlServer {
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

        let model : Models.Database = sql |> getResourceAtIndex client.SerializationSettings 1
        Expect.equal model.Sku.Name "GP_Gen5_12" "Incorrect SKU"
        Expect.equal model.MaxSizeBytes (Nullable 2147483648L) "Incorrect Size"
        Expect.equal model.LicenseType "BasePrice" "Incorrect SKU"
    }

    test "SQL Firewall is correctly configured" {
        let sql =
            sqlServer {
                name "server"
                admin_username "isaac"
                add_firewall_rule "Rule" "0.0.0.0" "255.255.255.255"
                add_databases [ sqlDb { name "db" } ]
            }
        let model : Models.FirewallRule = sql |> getResourceAtIndex client.SerializationSettings 2
        Expect.equal model.StartIpAddress "0.0.0.0" "Incorrect start IP"
        Expect.equal model.EndIpAddress "255.255.255.255" "Incorrect end IP"
    }

    test "Validation occurs on account name" {
        let check (v:string) m = Expect.equal (SqlAccountName.Create v) (Error ("SQL account names " + m))

        check "" "cannot be empty" "Name too short"
        let longName = Array.init 64 (fun _ -> 'a') |> String
        check longName ("max length is 63, but here is 64 ('" + longName + "')") "Name too long"
        check "zzzT" "can only contain lowercase letters ('zzzT')" "Upper case character allowed"
        check "zz!z" "can only contain alphanumeric characters or the dash ('zz!z')" "Bad character allowed"
        check "-zz" "cannot start with a dash ('-zz')" "Start with dash"
        check "zz-" "cannot end with a dash ('zz-')" "End with dash"
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

        let model : Models.Server = sql |> getResourceAtIndex client.SerializationSettings 0
        Expect.equal model.Name "server" "Incorrect Server name"
        Expect.equal model.MinimalTlsVersion "1.2" "Min TLS version is wrong"
    }
]
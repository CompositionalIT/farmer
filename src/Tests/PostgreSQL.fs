module PostgreSQL

open System

open Expecto
open Farmer
open Farmer.PostgreSQL
open Farmer.Builders
open Newtonsoft.Json.Linq
open Farmer.Arm

type PostgresSku =
    { name : string
      family : string
      capacity : int
      tier : string
      size : int }


type StorageProfile =
    { backupRetentionDays : int
      geoRedundantBackup : string
      storageAutoGrow : string
      storageMB : int }

type Properties =
    { administratorLogin : string
      administratorLoginPassword : string
      version : string
      storageProfile : StorageProfile }


type PostgresTemplate =
    { name : string
      ``type`` : string
      apiVersion : string
      sku : PostgresSku
      location : string
      geoRedundantBackup : string
      resources : JToken list
      properties : Properties }

type Dependencies = string array

type DatabaseResource =
    { name : string
      ``type`` : string
      apiVersion : string
      properties : {| collation: string; charset: string |}
      dependsOn : string list }

type FirewallResource =
    {   name: string
        apiVersion: string
        ``type`` : string
        dependsOn : string list
        properties: {| endIpAddress: string; startIpAddress: string |}
        location: string }

let runBuilder<'T> = toTypedTemplate<'T> Location.NorthEurope

module Expect =
    let throwsNot f message =
        let thrown =
            try
                f ()
                None
            with e ->
                Some e.Message
        match thrown with
        | None -> ()
        | Some msg ->
            failtestf "%s. Expected f to not throw, but it did. Exception message: %s" message msg

let tests = testList "PostgreSQL Database Service" [
    test "Server settings are correct" {
        let actual = runBuilder<PostgresTemplate> <| postgreSQL {
            name "testdb"
            admin_username "myadminuser"
            server_version VS_10
            storage_size 50<Gb>
            backup_retention 17<Days>
            capacity 4<VCores>
            tier GeneralPurpose
            enable_geo_redundant_backup
            disable_storage_autogrow
        }

        Expect.equal actual.apiVersion "2017-12-01" "apiVersion"
        Expect.equal actual.``type`` "Microsoft.DBforPostgreSQL/servers" "type"
        Expect.equal actual.sku.name "GP_Gen5_4" "sku name"
        Expect.equal actual.sku.family "Gen5" "sku family"
        Expect.equal actual.sku.capacity 4 "sku capacity"
        Expect.equal actual.sku.tier "GeneralPurpose" "sku tier"
        Expect.equal actual.sku.size 51200 "sku size"
        Expect.equal actual.properties.administratorLogin "myadminuser" "Admin user prop"
        Expect.equal actual.properties.administratorLoginPassword "[parameters('password-for-testdb')]" "Admin password prop"
        Expect.equal actual.properties.version "10" "server version"
        Expect.equal actual.properties.storageProfile.geoRedundantBackup "Enabled" "geo backup"
        Expect.equal actual.properties.storageProfile.storageAutoGrow "Disabled" "storage autogrow"
        Expect.equal actual.properties.storageProfile.backupRetentionDays 17 "backup retention"
    }

    test "Database settings are correct" {
        let db = postgreSQLDb {
            name "my_db"
            collation "de_DE"
            charset "ASCII"
        }
        let actual = postgreSQL {
            name "testdb"
            admin_username "myadminuser"
            add_database db
        }
        let actual =
            actual
            |> toTemplate Location.NorthEurope
            |> Writer.toJson
            |> Microsoft.Rest.Serialization.SafeJsonConvert.DeserializeObject<TypedArmTemplate<DatabaseResource>>
            |> fun r -> r.Resources
            |> Seq.find(fun r -> r.name = "testdb/my_db")

        let expectedDbRes = {
            name = "testdb/my_db"
            apiVersion = "2017-12-01"
            ``type`` = "Microsoft.DBforPostgreSQL/servers/databases"
            properties = {| collation = "de_DE"; charset = "ASCII" |}
            dependsOn = [ "[string('testdb')]" ]
        }

        Expect.equal actual expectedDbRes "database resource"
    }

    test "Firewall rules are correctly set" {
        let actual = postgreSQL {
            name "testdb"
            admin_username "myadminuser"
            enable_azure_firewall
        }
        let actual =
            actual
            |> toTemplate Location.NorthEurope
            |> Writer.toJson
            |> Microsoft.Rest.Serialization.SafeJsonConvert.DeserializeObject<TypedArmTemplate<FirewallResource>>
            |> fun r -> r.Resources
            |> Seq.find(fun r -> r.name = "testdb/allow-azure-services")
        let expectedFwRuleRes =
            { name = "testdb/allow-azure-services"
              ``type`` = "Microsoft.DBforPostgreSQL/servers/firewallrules"
              apiVersion = "2017-12-01"
              dependsOn = [ "[string('testdb')]" ]
              location = "northeurope"
              properties = {| startIpAddress = "0.0.0.0"; endIpAddress = "0.0.0.0" |} }
        Expect.equal actual expectedFwRuleRes "Firewall is incorrect"
    }

    test "Server name must be given" {
        Expect.throws (fun () -> runBuilder <| postgreSQL { admin_username "adminuser" } |> ignore) "Missing server name"
    }

    test "Admin username must be given" {
        Expect.throws (fun () -> runBuilder <| postgreSQL { name "servername" } |> ignore) "Missing admin username"
    }

    test "server_name is validated when set" {
        Expect.throws (fun () -> postgreSQL { name "123bad" } |> ignore) "Bad server name"
    }

    test "admin_username is validated when set" {
        Expect.throws (fun () -> postgreSQL { admin_username "123bad" } |> ignore) "Bad admin username"
    }

    test "backup_retention is validated when set" {
        Expect.throws (fun () -> postgreSQL { backup_retention 2<Days> } |> ignore) "Bad backup retention"
    }

    test "storage_size is validated when set" {
        Expect.throws (fun () -> postgreSQL { storage_size 1<Gb> } |> ignore) "Bad backup retention"
    }

    test "capacity is validated when set" {
        Expect.throws (fun () -> postgreSQL { capacity 6<VCores> } |> ignore) "Bad capacity"
    }

    test "Username can be validated" {
        let validate c =
            fun () ->
                Validate.username "u" c
        let badNames = [
            (null, "Null username"); ("", "Empty username"); ("   \t ", "Blank username")
            (String('a', 64), "Username too long")
            ("Ædmin", "Bad chars in username")
            ("123abc", "Can not begin with number")
            ("admin_123", "More bad chars in username")
        ]
        for (candidate,label) in badNames do
            Expect.throws (validate candidate) label

        Validate.reservedUsernames |> List.iter (fun candidate ->
            Expect.throws (validate candidate) (sprintf "Reserved name '%s'" candidate)
        )
        let goodNames = [
            "a"; "abd23"; (String('a', 63))
        ]
        for candidate in goodNames do
            Expect.throwsNot (validate candidate) (sprintf "'%s' should work" candidate)
    }

    test "Servername can be validated" {
        let validate c =
            fun () ->
                Validate.servername c

        let badNames = [
            ("  \t ", "Blank servername"); (null, "Null servername"); ("", "Empty servername")
            (String('a', 64), "servername too long")
            ("ab", "servername too short")
            ("aBcd", "uppercase char in servername")
            ("-server", "Beginning hyphen")
            ("server-", "Ending hyphen")
            ("særver", "Bad chars in servername")
            ("123abc", "Can not begin with number")
        ]
        for candidate,label in badNames do
            Expect.throws (validate candidate) label

        let goodNames = [
            "abc"; "abd-23"; (String('a', 63))
        ]
        for candidate in goodNames do
            Expect.throwsNot (validate candidate) (sprintf "'%s' should work" candidate)
    }

    test "Database name can be validated" {
        let validate c =
            fun () ->
                Validate.dbname c
        let badNames = [
            (null, "Null dbname"); ("", "Empty dbname"); ("   \t ", "Blank dbname")
            (String('a', 64), "dbname too long")
            ("123abc", "Can not begin with number")
        ]
        for candidate,label in badNames do
            Expect.throws (validate candidate) label

        let goodNames = [
            "abc"; "abd-23"; (String('a', 63))
        ]
        for candidate in goodNames do
            Expect.throwsNot (validate candidate) (sprintf "'%s' should work" candidate)
    }

    test "Storage size can be validated" {
        Expect.throws (fun () -> Validate.storageSize 4<Gb>) "Storage size too small"
        Expect.throws (fun () -> Validate.storageSize 1025<Gb>) "Storage size too large"
        Expect.throwsNot (fun () -> Validate.storageSize 5<Gb>) "Storage size just right, min"
        Expect.throwsNot (fun () -> Validate.storageSize 50<Gb>) "Storage size just right"
        Expect.throwsNot (fun () -> Validate.storageSize 1024<Gb>) "Storage size just right, max"
    }

    test "Backup retention can be validated" {
        Expect.throws (fun () -> Validate.backupRetention 4<Days>) "Backup retention too small"
        Expect.throws (fun () -> Validate.backupRetention 1000<Days>) "Backup retention too large"
        Expect.throwsNot (fun () -> Validate.backupRetention 21<Days>) "Backup retention just right"
    }

    test "Capacity can be validated" {
        Expect.throws (fun () -> Validate.capacity 0<VCores>) "Capacity too small"
        Expect.throws (fun () -> Validate.capacity 128<VCores>) "Capacity too large"
        Expect.throws (fun () -> Validate.capacity 13<VCores>) "Capacity not a power of two"
        Expect.throwsNot (fun () -> Validate.capacity 16<VCores>) "Capacity just right"
    }

    test "Family name should not include type name" {
        Expect.equal PostgreSQLFamily.Gen5.AsArmValue "Gen5" "Wrong value for Gen5 family"
        Expect.equal (PostgreSQLFamily.Gen5.ToString()) "Gen5" "Wrong value for Gen5 family"
    }
]
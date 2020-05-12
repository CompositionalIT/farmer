module PostgreSQL

open Expecto
open Farmer
open Farmer.Builders

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
      properties : Properties }

let runBuilder builder = toTypedTemplate<PostgresTemplate> NorthEurope builder

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
    test "Basic resource settings come through" {
        let actual = runBuilder <| postgreSQL {
            server_name "testdb"
            admin_username "myadminuser"
            server_version Arm.DBforPostgreSQL.VS_10
            storage_size 50<GB>
            backup_retention 17<Days>
            capacity 4<VCores>
            tier Arm.DBforPostgreSQL.GeneralPurpose
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
        Expect.equal actual.properties.administratorLoginPassword "[parameters('administratorLoginPassword')]" "Admin password prop"
        Expect.equal actual.properties.version "10" "server version"
        Expect.equal actual.properties.storageProfile.geoRedundantBackup "Enabled" "geo backup"
        Expect.equal actual.properties.storageProfile.storageAutoGrow "Disabled" "storage autogrow"
        Expect.equal actual.properties.storageProfile.backupRetentionDays 17 "backup retention"
    }

    test "Server name must be given" {
        Expect.throws (fun () -> runBuilder <| postgreSQL { admin_username "adminuser" } |> ignore) "Missing server name"
    }

    test "Admin username must be given" {
        Expect.throws (fun () -> runBuilder <| postgreSQL { server_name "servername" } |> ignore) "Missing admin username"
    }

    test "server_name is validated when set" {
        Expect.throws (fun () -> postgreSQL { server_name "123bad" } |> ignore) "Bad server name"
    }

    test "admin_username is validated when set" {
        Expect.throws (fun () -> postgreSQL { admin_username "123bad" } |> ignore) "Bad admin username"
    }

    test "backup_retention is validated when set" {
        Expect.throws (fun () -> postgreSQL { backup_retention 2<Days> } |> ignore) "Bad backup retention"
    }

    test "storage_size is validated when set" {
        Expect.throws (fun () -> postgreSQL { storage_size 1<GB> } |> ignore) "Bad backup retention"
    }

    test "capacity is validated when set" {
        Expect.throws (fun () -> postgreSQL { capacity 6<VCores> } |> ignore) "Bad capacity"
    }

    test "Username can be validated" {
        let validate c =
            fun () ->
                Validate.username "u" c
        let badNames = [
            (null, "Null username"); ("", "Empty username"); ("   /t ", "Blank username")
            (System.String('a', 64), "Username too long")
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
            "a"; "abd23"; (System.String('a', 63))
        ]
        for candidate in goodNames do
            Expect.throwsNot (validate candidate) (sprintf "'%s' should work" candidate)
    }

    test "Servername can be validated" {
        let validate c =
            fun () ->
                Validate.servername c

        let badNames = [
            (null, "Null servername"); ("", "Empty servername"); ("   /t ", "Blank servername")
            (System.String('a', 64), "servername too long")
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
            "abc"; "abd-23"; (System.String('a', 63))
        ]
        for candidate in goodNames do
            Expect.throwsNot (validate candidate) (sprintf "'%s' should work" candidate)
    }

    test "Storage size can be validated" {
        Expect.throws (fun () -> Validate.storageSize 4<GB>) "Storage size too small"
        Expect.throws (fun () -> Validate.storageSize 1025<GB>) "Storage size too large"
        Expect.throwsNot (fun () -> Validate.storageSize 5<GB>) "Storage size just right, min"
        Expect.throwsNot (fun () -> Validate.storageSize 50<GB>) "Storage size just right"
        Expect.throwsNot (fun () -> Validate.storageSize 1024<GB>) "Storage size just right, max"
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
]
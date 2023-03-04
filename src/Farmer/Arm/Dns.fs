[<AutoOpen>]
module Farmer.Arm.Dns

open Farmer
open Farmer.Dns

let zones = ResourceType("Microsoft.Network/dnsZones", "2018-05-01")
let aRecord = ResourceType("Microsoft.Network/dnsZones/A", "2018-05-01")
let aaaaRecord = ResourceType("Microsoft.Network/dnsZones/AAAA", "2018-05-01")
let cnameRecord = ResourceType("Microsoft.Network/dnsZones/CNAME", "2018-05-01")
let txtRecord = ResourceType("Microsoft.Network/dnsZones/TXT", "2018-05-01")
let mxRecord = ResourceType("Microsoft.Network/dnsZones/MX", "2018-05-01")
let nsRecord = ResourceType("Microsoft.Network/dnsZones/NS", "2018-05-01")
let soaRecord = ResourceType("Microsoft.Network/dnsZones/SOA", "2018-05-01")
let srvRecord = ResourceType("Microsoft.Network/dnsZones/SRV", "2018-05-01")
let ptrRecord = ResourceType("Microsoft.Network/dnsZones/PTR", "2018-05-01")

let privateZones = ResourceType("Microsoft.Network/privateDnsZones", "2020-06-01")

let privateARecord =
    ResourceType("Microsoft.Network/privateDnsZones/A", "2020-06-01")

let privateAaaaRecord =
    ResourceType("Microsoft.Network/privateDnsZones/AAAA", "2020-06-01")

let privateCnameRecord =
    ResourceType("Microsoft.Network/privateDnsZones/CNAME", "2020-06-01")

let privateTxtRecord =
    ResourceType("Microsoft.Network/privateDnsZones/TXT", "2020-06-01")

let privateMxRecord =
    ResourceType("Microsoft.Network/privateDnsZones/MX", "2020-06-01")

let privateSoaRecord =
    ResourceType("Microsoft.Network/privateDnsZones/SOA", "2020-06-01")

let privateSrvRecord =
    ResourceType("Microsoft.Network/privateDnsZones/SRV", "2020-06-01")

let privatePtrRecord =
    ResourceType("Microsoft.Network/privateDnsZones/PTR", "2020-06-01")

type DnsRecordType with

    member private this.publicResourceType =
        match this with
        | CName _ -> cnameRecord
        | A _ -> aRecord
        | AAAA _ -> aaaaRecord
        | NS _ -> nsRecord
        | PTR _ -> ptrRecord
        | TXT _ -> txtRecord
        | MX _ -> mxRecord
        | SRV _ -> srvRecord
        | SOA _ -> soaRecord

    member private this.privateResourceType =
        match this with
        | CName _ -> privateCnameRecord
        | A _ -> privateARecord
        | AAAA _ -> privateAaaaRecord
        | NS _ -> raiseFarmer "Private DNS zones do not support NS records"
        | PTR _ -> privatePtrRecord
        | TXT _ -> privateTxtRecord
        | MX _ -> privateMxRecord
        | SRV _ -> privateSrvRecord
        | SOA _ -> privateSoaRecord

    member this.ResourceType zoneType =
        match zoneType with
        | Public -> this.publicResourceType
        | Private -> this.privateResourceType

type DnsZone =
    {
        Name: ResourceName
        Dependencies: Set<ResourceId>
        Properties: {| ZoneType: string |}
    }

    member private this.zoneResource =
        match this.Properties.ZoneType with
        | "Public" -> zones
        | "Private" -> privateZones
        | _ -> raiseFarmer "Invalid value for ZoneType"

    interface IArmResource with
        member this.ResourceId = this.zoneResource.resourceId this.Name

        member this.JsonModel =
            {| this.zoneResource.Create(this.Name, Location.Global, this.Dependencies) with
                properties = {|
                    zoneType = this.Properties.ZoneType
                |}
            |}

module DnsRecords =
    let private sourceZoneNSRecordReference (zoneResourceId: ResourceId) : ArmExpression =
        let sourceZoneResId =
            { zoneResourceId with
                Segments = [ ResourceName "@" ]
                Type = nsRecord
            }

        ArmExpression
            .reference(nsRecord, sourceZoneResId)
            .Map(fun r -> r + ".NSRecords")
            .WithOwner(sourceZoneResId)

    let private ttlKey zoneType =
        match zoneType with
        | Public -> "TTL"
        | Private -> "ttl"

    let private cnameRecordKey zoneType =
        match zoneType with
        | Public -> "CNAMERecord"
        | Private -> "cnameRecord"

    let private mxRecordKey zoneType =
        match zoneType with
        | Public -> "MXRecords"
        | Private -> "mxRecords"

    let private txtRecordKey zoneType =
        match zoneType with
        | Public -> "TXTRecords"
        | Private -> "txtRecords"

    let private ptrRecordKey zoneType =
        match zoneType with
        | Public -> "PTRRecords"
        | Private -> "ptrRecords"

    let private aRecordKey zoneType =
        match zoneType with
        | Public -> "ARecords"
        | Private -> "aRecords"

    let private aaaRecordKey zoneType =
        match zoneType with
        | Public -> "AAAARecords"
        | Private -> "aaaaRecords"

    let private srvRecordKey zoneType =
        match zoneType with
        | Public -> "SRVRecords"
        | Private -> "srvRecords"

    let private soaRecordKey zoneType =
        match zoneType with
        | Public -> "SOARecord"
        | Private -> "soaRecord"

    type DnsRecord =
        {
            Name: ResourceName
            Dependencies: Set<ResourceId>
            Zone: LinkedResource
            ZoneType: DnsZoneType
            Type: DnsRecordType
            TTL: int
        }

        /// Includes the DnsZone if deployed in the same template (Managed).
        member private this.dependsOn =
            match this.Zone with
            | Managed id -> this.Dependencies |> Set.add id
            | Unmanaged _ -> this.Dependencies

        member private this.RecordType = this.Type.ResourceType this.ZoneType

        interface IArmResource with
            member this.ResourceId = this.RecordType.resourceId (this.Zone.Name, this.Name)

            member this.JsonModel =
                {| this.RecordType.Create(this.Zone.Name / this.Name, dependsOn = this.dependsOn) with
                    properties =
                        [
                            (ttlKey this.ZoneType), box this.TTL

                            match this.Type with
                            | A(Some targetResource, _)
                            | CName(Some targetResource, _)
                            | AAAA(Some targetResource, _) ->
                                "targetResource",
                                box
                                    {|
                                        id = targetResource.ArmExpression.Eval()
                                    |}
                            | _ -> ()

                            match this.Type with
                            | CName(_, Some cnameRecord) ->
                                (cnameRecordKey this.ZoneType), box {| cname = cnameRecord |}
                            | MX records ->
                                (mxRecordKey this.ZoneType),
                                records
                                |> List.map (fun mx -> {|
                                    preference = mx.Preference
                                    exchange = mx.Exchange
                                |})
                                |> box
                            | NS(NsRecords.Records records) ->
                                "NSRecords", records |> List.map (fun ns -> {| nsdname = ns |}) |> box
                            | NS(NsRecords.SourceZone sourceZone) ->
                                "NSRecords", (sourceZoneNSRecordReference sourceZone).Eval() |> box
                            | TXT records ->
                                (txtRecordKey this.ZoneType),
                                records |> List.map (fun txt -> {| value = [ txt ] |}) |> box
                            | PTR records ->
                                (ptrRecordKey this.ZoneType),
                                records |> List.map (fun ptr -> {| ptrdname = ptr |}) |> box
                            | A(_, records) ->
                                (aRecordKey this.ZoneType), records |> List.map (fun a -> {| ipv4Address = a |}) |> box
                            | AAAA(_, records) ->
                                (aaaRecordKey this.ZoneType),
                                records |> List.map (fun aaaa -> {| ipv6Address = aaaa |}) |> box
                            | SRV records ->
                                let records =
                                    records
                                    |> List.map (fun srv -> {|
                                        priority = srv.Priority |> Option.toNullable
                                        weight = srv.Weight |> Option.toNullable
                                        port = srv.Port |> Option.toNullable
                                        target = Option.toObj srv.Target
                                    |})

                                (srvRecordKey this.ZoneType), box records
                            | SOA record ->
                                let record = {|
                                    host = Option.toObj record.Host
                                    email = Option.toObj record.Email
                                    serialNumber = record.SerialNumber |> Option.toNullable
                                    refreshTime = record.RefreshTime |> Option.toNullable
                                    retryTime = record.RetryTime |> Option.toNullable
                                    expireTime = record.ExpireTime |> Option.toNullable
                                    minimumTTL = record.MinimumTTL |> Option.toNullable
                                |}

                                (soaRecordKey this.ZoneType), box record
                            | CName(_, None) -> ()
                        ]
                        |> Map
                |}

[<AutoOpen>]
module Farmer.Arm.Dns

open Farmer
open Farmer.CoreTypes
open Farmer.Dns

let zones = ResourceType ("Microsoft.Network/dnsZones", "2018-05-01")
let aRecord = ResourceType ("Microsoft.Network/dnsZones/A", "2018-05-01")
let aaaaRecord = ResourceType ("Microsoft.Network/dnsZones/AAAA", "2018-05-01")
let cnameRecord = ResourceType ("Microsoft.Network/dnsZones/CNAME", "2018-05-01")
let txtRecord = ResourceType ("Microsoft.Network/dnsZones/TXT", "2018-05-01")
let mxRecord = ResourceType ("Microsoft.Network/dnsZones/MX", "2018-05-01")
let nsRecord = ResourceType ("Microsoft.Network/dnsZones/NS", "2018-05-01")
let soaRecord = ResourceType ("Microsoft.Network/dnszones/SOA", "2018-05-01")
let srvRecord = ResourceType ("Microsoft.Network/dnsZones/SRV", "2018-05-01")
let ptrRecord = ResourceType ("Microsoft.Network/dnsZones/PTR", "2018-05-01")

type DnsRecordType with
    member this.ResourceType =
        match this with
        | CName _ -> cnameRecord
        | A _ -> aRecord
        | AAAA _ -> aaaaRecord
        | NS _ -> nsRecord
        | PTR _ -> ptrRecord
        | TXT _ -> txtRecord
        | MX _ -> mxRecord

type DnsZone =
    { Name : ResourceName
      Properties : {| ZoneType : string |} }

    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| zones.Create(this.Name, Location.Global) with
                properties = {| zoneType = this.Properties.ZoneType |}
            |} :> _

module DnsRecords =
    type DnsRecord =
        { Name : ResourceName
          Zone : ResourceName
          Type : DnsRecordType
          TTL : int }
        interface IArmResource with
            member this.ResourceName = this.Name
            member this.JsonModel =
                {| this.Type.ResourceType.Create(this.Zone + this.Name, dependsOn = [ this.Zone ]) with
                    properties =
                        {| TTL = this.TTL
                           targetResource =
                            match this.Type with
                            | A (Some targetResource, _)
                            | CName (Some targetResource, _)
                            | AAAA (Some targetResource, _)  ->
                                box {| id = targetResource.Value |}
                            | _ ->
                                null
                           CNAMERecord =
                            match this.Type with
                            | CName (_, Some cnameRecord) -> box {| cname = cnameRecord |}
                            | _ -> null
                           MXRecords =
                            match this.Type with
                            | MX records -> records |> List.map (fun mx -> {| preference = mx.Preference; exchange = mx.Exchange |}) |> box
                            | _ -> null
                           NSRecords =
                            match this.Type with
                            | NS records -> records |> List.map (fun ns -> {| nsdname = ns |}) |> box
                            | _ -> null
                           TXTRecords =
                            match this.Type with
                            | TXT records -> records |> List.map (fun txt -> {| value = [ txt ] |}) |> box
                            | _ -> null
                           PTRRecords =
                            match this.Type with
                            | PTR records -> records |> List.map (fun ptr -> {| ptrdname = ptr |}) |> box
                            | _ -> null
                           ARecords =
                            match this.Type with
                            | A (_, records) -> records |> List.map (fun a -> {| ipv4Address = a |}) |> box
                            | _ -> null
                           AAAARecords =
                            match this.Type with
                            | AAAA (_, records) -> records |> List.map (fun aaaa -> {| ipv6Address = aaaa |}) |> box
                            | _ -> null
                        |}
                |} :> _
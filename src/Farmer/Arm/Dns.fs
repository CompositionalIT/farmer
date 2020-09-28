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
        | CName -> cnameRecord
        | A -> aRecord
        | AAAA -> aaaaRecord
        | NS -> nsRecord
        | PTR -> ptrRecord
        | TXT -> txtRecord
        | MX -> mxRecord

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
    type CNameDnsRecord =
        { Name : ResourceName
          Zone : ResourceName
          Type : DnsRecordType
          TTL : int
          TargetResource : ResourceName option
          CNameRecord : string option }
        interface IArmResource with
            member this.ResourceName = this.Name
            member this.JsonModel =
                {| this.Type.ResourceType.Create(this.Zone + this.Name, dependsOn = [ ResourceId.create this.Zone ]) with
                    properties =
                        {| TTL = this.TTL
                           targetResource =
                               match this.TargetResource with
                               | Some targetResource -> box {| id = targetResource.Value |}
                               | None -> null
                           CNAMERecord = {| cname = this.CNameRecord |> Option.toObj |} |}
                |} :> _

    type MxDnsRecord =
        { Name : ResourceName
          Zone : ResourceName
          Type : DnsRecordType
          TTL : int
          MxRecords : {| Preference : int; Exchange : string |} list }
        interface IArmResource with
            member this.ResourceName = this.Name
            member this.JsonModel =
                {| this.Type.ResourceType.Create(this.Zone + this.Name, dependsOn = [ ResourceId.create this.Zone ]) with
                    properties =
                        {| TTL = this.TTL;
                           MXRecords = this.MxRecords |> List.map (fun mx -> {| preference = mx.Preference; exchange = mx.Exchange |}) |}
                |} :> _

    type NsDnsRecord =
        { Name : ResourceName
          Zone : ResourceName
          Type : DnsRecordType
          TTL : int
          NsRecords : string list }
        interface IArmResource with
            member this.ResourceName = this.Name
            member this.JsonModel =
                {| this.Type.ResourceType.Create(this.Zone + this.Name, dependsOn = [ ResourceId.create this.Zone ]) with
                    properties =
                        {| TTL = this.TTL
                           NSRecords = this.NsRecords |> List.map (fun ns -> {| nsdname = ns |}) |}
                |} :> _

    type TxtDnsRecord =
        { Name : ResourceName
          Zone : ResourceName
          Type : DnsRecordType
          TTL : int
          TxtRecords : string list }
        interface IArmResource with
            member this.ResourceName = this.Name
            member this.JsonModel =
                {| this.Type.ResourceType.Create(this.Zone + this.Name, dependsOn = [ ResourceId.create this.Zone ]) with
                    properties =
                        {| TTL = this.TTL;
                           TXTRecords = this.TxtRecords |> List.map (fun txt -> {| value = [ txt ] |}) |}
                |} :> _

    type PtrDnsRecord =
        { Name : ResourceName
          Zone : ResourceName
          Type : DnsRecordType
          TTL : int
          PtrRecords : string list }
        interface IArmResource with
            member this.ResourceName = this.Name
            member this.JsonModel =
                {| this.Type.ResourceType.Create(this.Zone + this.Name, dependsOn = [ ResourceId.create this.Zone ]) with
                    properties =
                        {| TTL = this.TTL;
                           PTRRecords = this.PtrRecords |> List.map (fun ptr -> {| ptrdname = ptr |}) |}
                |} :> _

    type ADnsRecord =
        { Name : ResourceName
          Zone : ResourceName
          Type : DnsRecordType
          TTL : int
          TargetResource : ResourceName option
          ARecords : string list }
        interface IArmResource with
            member this.ResourceName = this.Name
            member this.JsonModel =
                {| this.Type.ResourceType.Create(this.Zone + this.Name, dependsOn = [ ResourceId.create this.Zone ]) with
                    properties =
                        {| TTL = this.TTL;
                           targetResource =
                               match this.TargetResource with
                               | Some targetResource -> box {| id = targetResource.Value |}
                               | None -> null
                           ARecords = this.ARecords |> List.map (fun a -> {| ipv4Address = a |}) |}
                |} :> _

    type AaaaDnsRecord =
        { Name : ResourceName
          Zone : ResourceName
          Type : DnsRecordType
          TTL : int
          TargetResource : ResourceName option
          AaaaRecords : string list }
        interface IArmResource with
            member this.ResourceName = this.Name
            member this.JsonModel =
                {| this.Type.ResourceType.Create(this.Zone + this.Name, dependsOn = [ ResourceId.create this.Zone ]) with
                    properties =
                        {| TTL = this.TTL;
                           targetResource =
                               match this.TargetResource with
                               | Some targetResource -> box {| id = targetResource.Value |}
                               | None -> null
                           AAAARecords = this.AaaaRecords |> List.map (fun aaaa -> {| ipv6Address = aaaa |}) |}
                |} :> _
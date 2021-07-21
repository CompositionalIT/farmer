[<AutoOpen>]
module Farmer.Builders.Dns

open System

open Farmer
open Farmer.Dns
open Farmer.Arm.Dns
open DnsRecords

type DnsZoneRecordConfig =
    { Name : ResourceName
      Type : DnsRecordType
      TTL : int
      Zone : LinkedResource option }
    static member Create(name, ttl, zone, recordType) =
        { Name =
              if name = ResourceName.Empty then failwith "You must set a DNS zone name"
              name
          TTL =
              match ttl with
              | Some ttl -> ttl
              | None -> failwith "You must set a TTL"
          Zone = zone
          Type = recordType }
    interface IBuilder with
        member this.ResourceId =
            match this.Zone with
            | Some zone ->
                this.Type.ResourceType.resourceId (zone.Name/this.Name)
            | None -> failwith "DNS record must be linked to a zone to properly assign the resourceId."
        member this.BuildResources _ =
            match this.Zone with
            | Some zone ->
                [
                    { DnsRecord.Name = this.Name
                      Zone = zone
                      TTL = this.TTL
                      Type = this.Type }
                ]
            | None -> failwith "DNS record must be linked to a zone."

type CNameRecordProperties =  { Name: ResourceName; CName : string option; TTL: int option; Zone: LinkedResource option; TargetResource: ResourceName option }
type ARecordProperties =  { Name: ResourceName; Ipv4Addresses : string list; TTL: int option; Zone: LinkedResource option; TargetResource: ResourceName option  }
type AaaaRecordProperties =  { Name: ResourceName; Ipv6Addresses : string list; TTL: int option; Zone: LinkedResource option; TargetResource: ResourceName option }
type NsRecordProperties =  { Name: ResourceName; NsdNames : string list; TTL: int option; Zone: LinkedResource option }
type PtrRecordProperties =  { Name: ResourceName; PtrdNames : string list; TTL: int option; Zone: LinkedResource option; }
type TxtRecordProperties =  { Name: ResourceName; TxtValues : string list; TTL: int option; Zone: LinkedResource option; }
type MxRecordProperties =  { Name: ResourceName; MxValues : {| Preference : int; Exchange : string |} list; TTL: int option; Zone: LinkedResource option; }
type SrvRecordProperties =  { Name: ResourceName; SrvValues : SrvRecord list; TTL: int option; Zone: LinkedResource option; }
type SoaRecordProperties =
    { Name: ResourceName
      Host : string option
      Email : string option
      SerialNumber : int64 option
      RefreshTime : int64 option
      RetryTime : int64 option
      ExpireTime : int64 option
      MinimumTTL : int64 option
      TTL: int option
      Zone: LinkedResource option }

type DnsCNameRecordBuilder() =
    member _.Yield _ = { CNameRecordProperties.CName = None; Name = ResourceName.Empty; TTL = None; Zone = None; TargetResource = None }
    member _.Run(state : CNameRecordProperties) = DnsZoneRecordConfig.Create(state.Name, state.TTL, state.Zone, CName(state.TargetResource, state.CName))

    /// Sets the name of the record set.
    [<CustomOperation "name">]
    member _.RecordName(state:CNameRecordProperties, name) = { state with Name = name }
    member this.RecordName(state:CNameRecordProperties, name:string) = this.RecordName(state, ResourceName name)

    /// Sets the canonical name for this CNAME record.
    [<CustomOperation "cname">]
    member _.RecordCName(state:CNameRecordProperties, cName) = { state with CName = Some cName }

    /// Sets the TTL of the record.
    [<CustomOperation "ttl">]
    member _.RecordTTL(state:CNameRecordProperties, ttl) = { state with TTL = Some ttl }

    /// Sets the target resource of the record.
    [<CustomOperation "target_resource">]
    member _.RecordTargetResource(state:CNameRecordProperties, targetResource) = { state with TargetResource = Some targetResource }

    /// Builds a record for an existing DNS zone.
    [<CustomOperation "link_to_unmanaged_dns_zone">]
    member _.LinkToDnsZone(state:CNameRecordProperties, zone:ResourceId) = { state with Zone = Some (Unmanaged zone) }

type DnsARecordBuilder() =
    member _.Yield _ = { ARecordProperties.Ipv4Addresses = []; Name = ResourceName "@"; TTL = None; Zone = None; TargetResource = None }
    member _.Run(state : ARecordProperties)  = DnsZoneRecordConfig.Create(state.Name, state.TTL, state.Zone, A(state.TargetResource, state.Ipv4Addresses))

    /// Sets the name of the record set.
    [<CustomOperation "name">]
    member _.RecordName(state:ARecordProperties, name) = { state with Name = name }
    member this.RecordName(state:ARecordProperties, name:string) = this.RecordName(state, ResourceName name)

    /// Sets the ipv4 address.
    [<CustomOperation "add_ipv4_addresses">]
    member _.RecordAddress(state:ARecordProperties, ipv4Addresses) = { state with Ipv4Addresses = state.Ipv4Addresses @ ipv4Addresses }

    /// Sets the TTL of the record.
    [<CustomOperation "ttl">]
    member _.RecordTTL(state:ARecordProperties, ttl) = { state with TTL = Some ttl }

    /// Sets the target resource of the record.
    [<CustomOperation "target_resource">]
    member _.RecordTargetResource(state:ARecordProperties, targetResource) = { state with TargetResource = Some targetResource }

    /// Builds a record for an existing DNS zone.
    [<CustomOperation "link_to_unmanaged_dns_zone">]
    member _.LinkToDnsZone(state:ARecordProperties, zone:ResourceId) = { state with Zone = Some (Unmanaged zone) }

type DnsAaaaRecordBuilder() =
    member _.Yield _ = { AaaaRecordProperties.Ipv6Addresses = []; Name = ResourceName "@"; TTL = None; Zone = None; TargetResource = None }
    member _.Run(state : AaaaRecordProperties) = DnsZoneRecordConfig.Create(state.Name, state.TTL, state.Zone, AAAA(state.TargetResource, state.Ipv6Addresses))

    /// Sets the name of the record set.
    [<CustomOperation "name">]
    member _.RecordName(state:AaaaRecordProperties, name) = { state with Name = name }
    member this.RecordName(state:AaaaRecordProperties, name:string) = this.RecordName(state, ResourceName name)

    /// Sets the ipv6 address.
    [<CustomOperation "add_ipv6_addresses">]
    member _.RecordAddress(state:AaaaRecordProperties, ipv6Addresses) = { state with Ipv6Addresses = state.Ipv6Addresses @ ipv6Addresses }

    /// Sets the TTL of the record.
    [<CustomOperation "ttl">]
    member _.RecordTTL(state:AaaaRecordProperties, ttl) = { state with TTL = Some ttl }

    /// Sets the target resource of the record.
    [<CustomOperation "target_resource">]
    member _.RecordTargetResource(state:AaaaRecordProperties, targetResource) = { state with TargetResource = Some targetResource }

    /// Builds a record for an existing DNS zone.
    [<CustomOperation "link_to_unmanaged_dns_zone">]
    member _.LinkToDnsZone(state:AaaaRecordProperties, zone:ResourceId) = { state with Zone = Some (Unmanaged zone) }

type DnsNsRecordBuilder() =
    member _.Yield _ = { NsRecordProperties.NsdNames = []; Name = ResourceName "@"; TTL = None; Zone = None }
    member _.Run(state : NsRecordProperties) = DnsZoneRecordConfig.Create(state.Name, state.TTL, state.Zone, NS state.NsdNames)

    /// Sets the name of the record set.
    [<CustomOperation "name">]
    member _.RecordName(state:NsRecordProperties, name) = { state with Name = name }
    member this.RecordName(state:NsRecordProperties, name:string) = this.RecordName(state, ResourceName name)

    /// Add NSD names
    [<CustomOperation "add_nsd_names">]
    member _.RecordNsdNames(state:NsRecordProperties, nsdNames) = { state with NsdNames = state.NsdNames @ nsdNames }

    /// Sets the TTL of the record.
    [<CustomOperation "ttl">]
    member _.RecordTTL(state:NsRecordProperties, ttl) = { state with TTL = Some ttl }

    /// Builds a record for an existing DNS zone.
    [<CustomOperation "link_to_unmanaged_dns_zone">]
    member _.LinkToDnsZone(state:NsRecordProperties, zone:ResourceId) = { state with Zone = Some (Unmanaged zone) }

type DnsPtrRecordBuilder() =
    member __.Yield _ = { PtrRecordProperties.PtrdNames = []; Name = ResourceName "@"; TTL = None; Zone = None }
    member __.Run(state : PtrRecordProperties) = DnsZoneRecordConfig.Create(state.Name, state.TTL, state.Zone, PTR state.PtrdNames)

    /// Sets the name of the record set.
    [<CustomOperation "name">]
    member _.RecordName(state:PtrRecordProperties, name) = { state with Name = name }
    member this.RecordName(state:PtrRecordProperties, name:string) = this.RecordName(state, ResourceName name)

    /// Add PTR names
    [<CustomOperation "add_ptrd_names">]
    member _.RecordPtrdNames(state:PtrRecordProperties, ptrdNames) = { state with PtrdNames = state.PtrdNames @ ptrdNames }

    /// Sets the TTL of the record.
    [<CustomOperation "ttl">]
    member _.RecordTTL(state:PtrRecordProperties, ttl) = { state with TTL = Some ttl }

    /// Builds a record for an existing DNS zone.
    [<CustomOperation "link_to_unmanaged_dns_zone">]
    member _.LinkToDnsZone(state:PtrRecordProperties, zone:ResourceId) = { state with Zone = Some (Unmanaged zone) }

type DnsTxtRecordBuilder() =
    member _.Yield _ = { TxtRecordProperties.Name = ResourceName "@"; TxtValues = []; TTL = None; Zone = None; }
    member _.Run(state : TxtRecordProperties) = DnsZoneRecordConfig.Create(state.Name, state.TTL, state.Zone, TXT state.TxtValues)

    /// Sets the name of the record set.
    [<CustomOperation "name">]
    member _.RecordName(state:TxtRecordProperties, name) = { state with Name = name }
    member this.RecordName(state:TxtRecordProperties, name:string) = this.RecordName(state, ResourceName name)

    /// Add TXT values
    [<CustomOperation "add_values">]
    member _.RecordValues(state:TxtRecordProperties, txtValues) = { state with TxtValues = state.TxtValues @ txtValues }

    /// Sets the TTL of the record.
    [<CustomOperation "ttl">]
    member _.RecordTTL(state:TxtRecordProperties, ttl) = { state with TTL = Some ttl }

    /// Builds a record for an existing DNS zone.
    [<CustomOperation "link_to_unmanaged_dns_zone">]
    member _.LinkToDnsZone(state:TxtRecordProperties, zone:ResourceId) = { state with Zone = Some (Unmanaged zone) }

type DnsMxRecordBuilder() =
    member _.Yield _ = { MxRecordProperties.Name = ResourceName "@"; MxValues = []; TTL = None; Zone = None }
    member _.Run(state : MxRecordProperties) = DnsZoneRecordConfig.Create(state.Name, state.TTL, state.Zone, MX state.MxValues)

    /// Sets the name of the record set.
    [<CustomOperation "name">]
    member _.RecordName(state:MxRecordProperties, name) = { state with Name = name }
    member this.RecordName(state:MxRecordProperties, name:string) = this.RecordName(state, ResourceName name)

    /// Add MX records.
    [<CustomOperation "add_values">]
    member _.RecordValue(state:MxRecordProperties, mxValues : (int * string) list) =
        { state
            with MxValues = state.MxValues @ (mxValues |> List.map(fun x -> {| Preference = fst x; Exchange = snd x; |})) }

    /// Sets the TTL of the record.
    [<CustomOperation "ttl">]
    member _.RecordTTL(state:MxRecordProperties, ttl) = { state with TTL = Some ttl }

    /// Builds a record for an existing DNS zone.
    [<CustomOperation "link_to_unmanaged_dns_zone">]
    member _.LinkToDnsZone(state:MxRecordProperties, zone:ResourceId) = { state with Zone = Some (Unmanaged zone) }

type DnsSrvRecordBuilder() =
    member _.Yield _ = { SrvRecordProperties.Name = ResourceName "@"; SrvValues = []; TTL = None; Zone = None; }
    member _.Run(state : SrvRecordProperties) = DnsZoneRecordConfig.Create(state.Name, state.TTL, state.Zone, SRV state.SrvValues)

    /// Sets the name of the record set.
    [<CustomOperation "name">]
    member _.RecordName(state:SrvRecordProperties, name) = { state with Name = name }
    member this.RecordName(state:SrvRecordProperties, name:string) = this.RecordName(state, ResourceName name)

    /// Add SRV records.
    [<CustomOperation "add_values">]
    member _.RecordValue(state:SrvRecordProperties, srvValues : SrvRecord list) =
        { state with SrvValues = state.SrvValues @ srvValues }

    /// Sets the TTL of the record.
    [<CustomOperation "ttl">]
    member _.RecordTTL(state:SrvRecordProperties, ttl) = { state with TTL = Some ttl }

    /// Builds a record for an existing DNS zone.
    [<CustomOperation "link_to_unmanaged_dns_zone">]
    member _.LinkToDnsZone(state:SrvRecordProperties, zone:ResourceId) = { state with Zone = Some (Unmanaged zone) }

type DnsSoaRecordBuilder() =
    member _.Yield _ =
        { SoaRecordProperties.Name = ResourceName "@"
          Host = None
          Email = None
          SerialNumber = None
          RefreshTime = None
          RetryTime = None
          ExpireTime = None
          MinimumTTL = None
          TTL = None
          Zone = None }

    member _.Run(state : SoaRecordProperties) =
        let value =
            { Host = state.Host
              Email = state.Email
              SerialNumber = state.SerialNumber
              RefreshTime = state.RefreshTime
              RetryTime = state.RetryTime
              ExpireTime = state.ExpireTime
              MinimumTTL = state.MinimumTTL }
        DnsZoneRecordConfig.Create(state.Name, state.TTL, state.Zone, SOA value)

    /// Sets the name of the record set.
    [<CustomOperation "name">]
    member _.RecordName(state:SoaRecordProperties, name) = { state with Name = name }
    member this.RecordName(state:SoaRecordProperties, name:string) = this.RecordName(state, ResourceName name)

    /// Sets the name of the record set.
    [<CustomOperation "host">]
    member _.RecordHost(state:SoaRecordProperties, host : string) = { state with Host = Some host }

    /// Sets the email for this SOA record (required).
    [<CustomOperation "email">]
    member _.RecordEmail(state:SoaRecordProperties, email : string) = { state with Email = Some email }

    /// Sets the expire time for this SOA record in seconds.
    /// Defaults to 2419200 (28 days).
    [<CustomOperation "expire_time">]
    member _.RecordExpireTime(state:SoaRecordProperties, expireTime : int64) = { state with ExpireTime = Some expireTime }

    /// Sets the minimum time to live for this SOA record in seconds.
    /// Defaults to 300.
    [<CustomOperation "minimum_ttl">]
    member _.RecordMinimumTTL(state:SoaRecordProperties, minTTL : int64) = { state with MinimumTTL = Some minTTL }

    /// Sets the refresh time for this SOA record in seconds.
    /// Defaults to 3600 (1 hour)
    [<CustomOperation "refresh_time">]
    member _.RecordRefreshTime(state:SoaRecordProperties, refreshTime : int64) = { state with RefreshTime = Some refreshTime }

    /// Sets the retry time for this SOA record in seconds.
    /// Defaults to 300 seconds.
    [<CustomOperation "retry_time">]
    member _.RetryTime(state:SoaRecordProperties, retryTime : int64) = { state with RetryTime = Some retryTime }

    /// Sets the serial number for this SOA record (required).
    [<CustomOperation "serial_number">]
    member _.RecordSerialNumber(state:SoaRecordProperties, serialNo : int64) = { state with SerialNumber = Some serialNo }

    /// Sets the TTL of the record.
    [<CustomOperation "ttl">]
    member _.RecordTTL(state:SoaRecordProperties, ttl) = { state with TTL = Some ttl }

    /// Builds a record for an existing DNS zone.
    [<CustomOperation "link_to_unmanaged_dns_zone">]
    member _.LinkToDnsZone(state:NsRecordProperties, zone:ResourceId) = { state with Zone = Some (Unmanaged zone) }

type DnsZoneConfig =
    { Name : ResourceName
      ZoneType : DnsZoneType
      Records : DnsZoneRecordConfig list }

    interface IBuilder with
        member this.ResourceId = zones.resourceId this.Name
        member this.BuildResources _ = [
            { DnsZone.Name = this.Name
              Properties = {| ZoneType = this.ZoneType |> string |} }

            for record in this.Records do
                { DnsRecord.Name = record.Name
                  Zone = Managed (zones.resourceId this.Name)
                  TTL = record.TTL
                  Type = record.Type }
        ]

type DnsZoneBuilder() =
    member __.Yield _ =
        { DnsZoneConfig.Name = ResourceName ""
          Records = []
          ZoneType = Public }
    member __.Run(state) : DnsZoneConfig =
        { state with
            Name =
                if state.Name = ResourceName.Empty then failwith "You must set a DNS zone name"
                else state.Name }
    /// Sets the name of the DNS Zone.
    [<CustomOperation "name">]
    member _.ServerName(state:DnsZoneConfig, serverName) = { state with Name = serverName }
    member this.ServerName(state:DnsZoneConfig, serverName:string) = this.ServerName(state, ResourceName serverName)

    /// Sets the type of the DNS Zone.
    [<CustomOperation "zone_type">]
    member _.RecordType(state:DnsZoneConfig, zoneType) = { state with ZoneType = zoneType }

    /// Add DNS records to the DNS Zone.
    [<CustomOperation "add_records">]
    member _.AddRecords(state:DnsZoneConfig, records) = { state with Records = state.Records @ records }

let dnsZone = DnsZoneBuilder()
let cnameRecord = DnsCNameRecordBuilder()
let aRecord = DnsARecordBuilder()
let aaaaRecord = DnsAaaaRecordBuilder()
let nsRecord = DnsNsRecordBuilder()
let ptrRecord = DnsPtrRecordBuilder()
let txtRecord = DnsTxtRecordBuilder()
let mxRecord = DnsMxRecordBuilder()
let srvRecord = DnsSrvRecordBuilder()
let soaRecord = DnsSoaRecordBuilder()
[<AutoOpen>]
module Farmer.Builders.Dns

open Farmer
open Farmer.Dns
open Farmer.Arm.Dns
open DnsRecords

type DnsZoneRecordConfig =
    { Name : ResourceName
      Type : DnsRecordType
      TTL : int }
    static member Create(name, ttl, recordType) =
        { Name =
              if name = ResourceName.Empty then failwith "You must set a DNS zone name"
              name
          TTL =
              match ttl with
              | Some ttl -> ttl
              | None -> failwith "You must set a TTL"
          Type = recordType }

type CNameRecordProperties =  { Name: ResourceName; CName : string option; TTL: int option; TargetResource: ResourceName option }
type ARecordProperties =  { Name: ResourceName; Ipv4Addresses : string list; TTL: int option; TargetResource: ResourceName option  }
type AaaaRecordProperties =  { Name: ResourceName; Ipv6Addresses : string list; TTL: int option; TargetResource: ResourceName option }
type NsRecordProperties =  { Name: ResourceName; NsdNames : string list; TTL: int option; }
type PtrRecordProperties =  { Name: ResourceName; PtrdNames : string list; TTL: int option; }
type TxtRecordProperties =  { Name: ResourceName; TxtValues : string list; TTL: int option; }
type MxRecordProperties =  { Name: ResourceName; MxValues : {| Preference : int; Exchange : string |} list; TTL: int option; }

type DnsCNameRecordBuilder() =
    member __.Yield _ = { CNameRecordProperties.CName = None; Name = ResourceName.Empty; TTL = None; TargetResource = None }
    member __.Run(state : CNameRecordProperties) = DnsZoneRecordConfig.Create(state.Name, state.TTL, CName(state.TargetResource, state.CName))

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

type DnsARecordBuilder() =
    member __.Yield _ = { ARecordProperties.Ipv4Addresses = []; Name = ResourceName "@"; TTL = None; TargetResource = None }
    member __.Run(state : ARecordProperties)  = DnsZoneRecordConfig.Create(state.Name, state.TTL, A(state.TargetResource, state.Ipv4Addresses))

    /// Sets the name of the record set.
    [<CustomOperation "name">]
    member _.RecordName(state:CNameRecordProperties, name) = { state with Name = name }
    member this.RecordName(state:CNameRecordProperties, name:string) = this.RecordName(state, ResourceName name)

    /// Sets the ipv4 address.
    [<CustomOperation "add_ipv4_addresses">]
    member _.RecordAddress(state:ARecordProperties, ipv4Addresses) = { state with Ipv4Addresses = state.Ipv4Addresses @ ipv4Addresses }

    /// Sets the TTL of the record.
    [<CustomOperation "ttl">]
    member _.RecordTTL(state:ARecordProperties, ttl) = { state with TTL = Some ttl }

    /// Sets the target resource of the record.
    [<CustomOperation "target_resource">]
    member _.RecordTargetResource(state:ARecordProperties, targetResource) = { state with TargetResource = Some targetResource }

type DnsAaaaRecordBuilder() =
    member __.Yield _ = { AaaaRecordProperties.Ipv6Addresses = []; Name = ResourceName "@"; TTL = None; TargetResource = None }
    member __.Run(state : AaaaRecordProperties) = DnsZoneRecordConfig.Create(state.Name, state.TTL, AAAA(state.TargetResource, state.Ipv6Addresses))

    /// Sets the name of the record set.
    [<CustomOperation "name">]
    member _.RecordName(state:CNameRecordProperties, name) = { state with Name = name }
    member this.RecordName(state:CNameRecordProperties, name:string) = this.RecordName(state, ResourceName name)

    /// Sets the ipv6 address.
    [<CustomOperation "add_ipv6_addresses">]
    member _.RecordAddress(state:AaaaRecordProperties, ipv6Addresses) = { state with Ipv6Addresses = state.Ipv6Addresses @ ipv6Addresses }

    /// Sets the TTL of the record.
    [<CustomOperation "ttl">]
    member _.RecordTTL(state:AaaaRecordProperties, ttl) = { state with TTL = Some ttl }

    /// Sets the target resource of the record.
    [<CustomOperation "target_resource">]
    member _.RecordTargetResource(state:AaaaRecordProperties, targetResource) = { state with TargetResource = Some targetResource }

type DnsNsRecordBuilder() =
    member __.Yield _ = { NsRecordProperties.NsdNames = []; Name = ResourceName "@"; TTL = None; }
    member __.Run(state : NsRecordProperties) = DnsZoneRecordConfig.Create(state.Name, state.TTL, NS state.NsdNames)

    /// Sets the name of the record set.
    [<CustomOperation "name">]
    member _.RecordName(state:CNameRecordProperties, name) = { state with Name = name }
    member this.RecordName(state:CNameRecordProperties, name:string) = this.RecordName(state, ResourceName name)

    /// Add NSD names
    [<CustomOperation "add_nsd_names">]
    member _.RecordNsdNames(state:NsRecordProperties, nsdNames) = { state with NsdNames = state.NsdNames @ nsdNames }

    /// Sets the TTL of the record.
    [<CustomOperation "ttl">]
    member _.RecordTTL(state:NsRecordProperties, ttl) = { state with TTL = Some ttl }

type DnsPtrRecordBuilder() =
    member __.Yield _ = { PtrRecordProperties.PtrdNames = []; Name = ResourceName "@"; TTL = None; }
    member __.Run(state : PtrRecordProperties) = DnsZoneRecordConfig.Create(state.Name, state.TTL, PTR state.PtrdNames)

    /// Sets the name of the record set.
    [<CustomOperation "name">]
    member _.RecordName(state:CNameRecordProperties, name) = { state with Name = name }
    member this.RecordName(state:CNameRecordProperties, name:string) = this.RecordName(state, ResourceName name)

    /// Add PTR names
    [<CustomOperation "add_ptrd_names">]
    member _.RecordPtrdNames(state:PtrRecordProperties, ptrdNames) = { state with PtrdNames = state.PtrdNames @ ptrdNames }

    /// Sets the TTL of the record.
    [<CustomOperation "ttl">]
    member _.RecordTTL(state:PtrRecordProperties, ttl) = { state with TTL = Some ttl }

type DnsTxtRecordBuilder() =
    member __.Yield _ = { TxtRecordProperties.Name = ResourceName "@"; TxtValues = []; TTL = None; }
    member __.Run(state : TxtRecordProperties) = DnsZoneRecordConfig.Create(state.Name, state.TTL, TXT state.TxtValues)

    /// Sets the name of the record set.
    [<CustomOperation "name">]
    member _.RecordName(state:CNameRecordProperties, name) = { state with Name = name }
    member this.RecordName(state:CNameRecordProperties, name:string) = this.RecordName(state, ResourceName name)

    /// Add TXT values
    [<CustomOperation "add_values">]
    member _.RecordValues(state:TxtRecordProperties, txtValues) = { state with TxtValues = state.TxtValues @ txtValues }

    /// Sets the TTL of the record.
    [<CustomOperation "ttl">]
    member _.RecordTTL(state:TxtRecordProperties, ttl) = { state with TTL = Some ttl }

type DnsMxRecordBuilder() =
    member __.Yield _ = { MxRecordProperties.Name = ResourceName "@"; MxValues = []; TTL = None; }
    member __.Run(state : MxRecordProperties) = DnsZoneRecordConfig.Create(state.Name, state.TTL, MX state.MxValues)

    /// Sets the name of the record set.
    [<CustomOperation "name">]
    member _.RecordName(state:CNameRecordProperties, name) = { state with Name = name }
    member this.RecordName(state:CNameRecordProperties, name:string) = this.RecordName(state, ResourceName name)

    /// Add MX records.
    [<CustomOperation "add_values">]
    member _.RecordValue(state:MxRecordProperties, mxValues : (int * string) list) =
        { state
            with MxValues = state.MxValues @ (mxValues |> List.map(fun x -> {| Preference = fst x; Exchange = snd x; |})) }

    /// Sets the TTL of the record.
    [<CustomOperation "ttl">]
    member _.RecordTTL(state:MxRecordProperties, ttl) = { state with TTL = Some ttl }

type DnsZoneConfig =
    { Name : ResourceName
      ZoneType : DnsZoneType
      Records : DnsZoneRecordConfig list }

    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources _ = [
            { DnsZone.Name = this.Name
              Properties = {| ZoneType = this.ZoneType |> string |} }

            for record in this.Records do
                { Name = record.Name
                  Zone = this.Name
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
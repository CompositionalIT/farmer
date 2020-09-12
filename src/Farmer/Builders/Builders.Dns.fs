[<AutoOpen>]
module Farmer.Builders.Dns

open Farmer
open Farmer.Dns
open Farmer.Arm.Dns
open DnsRecords

type DnsZoneType = Public | Private

type DnsZoneRecordConfig =
    { Name : ResourceName
      Type : DnsRecordType
      TTL : int
      TargetResource : ResourceName option
      CNameRecord : string option
      ARecords : string list
      AaaaRecords : string list
      NsRecords : string list
      PtrRecords : string list }

let emptyRecord =
    { DnsZoneRecordConfig.Name = ResourceName.Empty;
      Type = Unknown
      TTL = 0
      TargetResource = None
      CNameRecord = None
      ARecords = []
      AaaaRecords = []
      NsRecords = []
      PtrRecords = [] }

type CNameRecordProperties =  { Name: ResourceName; CName : string option; TTL: int option; TargetResource: ResourceName option }
type ARecordProperties =  { Name: ResourceName; Ipv4Addresses : string list; TTL: int option; TargetResource: ResourceName option  }
type AaaaRecordProperties =  { Name: ResourceName; Ipv6Addresses : string list; TTL: int option; TargetResource: ResourceName option }


type DnsCNameRecordBuilder() =
    member __.Yield _ = { CNameRecordProperties.CName = None; Name = ResourceName.Empty; TTL = None; TargetResource = None }
    member __.Run(state : CNameRecordProperties) : DnsZoneRecordConfig =
        { emptyRecord with
            Name =
                if state.Name = ResourceName.Empty then failwith "You must set a DNS zone name"
                else state.Name
            TTL =
                if state.TTL = None then failwith "You must set a TTL"
                else state.TTL.Value
            CNameRecord = state.CName
            Type = CName }

    /// Sets the name of the SQL server.
    [<CustomOperation "name">]
    member _.RecordName(state:CNameRecordProperties, name) = { state with Name = name }
    member this.RecordName(state:CNameRecordProperties, name:string) = this.RecordName(state, ResourceName name)

    /// Sets the name of the SQL server.
    [<CustomOperation "cname">]
    member _.RecordCName(state:CNameRecordProperties, cName) = { state with CName = Some cName }

    /// Sets the TTL of the record.
    [<CustomOperation "ttl">]
    member _.RecordTTL(state:CNameRecordProperties, ttl) = { state with TTL = Some ttl }

    /// Sets the target resource of the record.
    [<CustomOperation "target_resource">]
    member _.RecordTargetResource(state:CNameRecordProperties, targetResource) = { state with TargetResource = Some targetResource }

type DnsARecordBuilder() =
    member __.Yield _ = { ARecordProperties.Ipv4Addresses = []; Name = ResourceName.Empty; TTL = None; TargetResource = None }
    member __.Run(state : ARecordProperties) : DnsZoneRecordConfig =
        { emptyRecord with
            Name = ResourceName "@"
            TTL =
                if state.TTL = None then failwith "You must set a TTL"
                else state.TTL.Value
            ARecords = state.Ipv4Addresses
            Type = A }

    /// Sets the name of the SQL server.
    [<CustomOperation "add_ipv4_addresses">]
    member _.RecordCName(state:ARecordProperties, ipv4Addresses) = { state with Ipv4Addresses = state.Ipv4Addresses @ ipv4Addresses }

    /// Sets the TTL of the record.
    [<CustomOperation "ttl">]
    member _.RecordTTL(state:ARecordProperties, ttl) = { state with TTL = Some ttl }

    /// Sets the target resource of the record.
    [<CustomOperation "target_resource">]
    member _.RecordTargetResource(state:ARecordProperties, targetResource) = { state with TargetResource = Some targetResource }

type DnsAaaaRecordBuilder() =
    member __.Yield _ = { AaaaRecordProperties.Ipv6Addresses = []; Name = ResourceName.Empty; TTL = None; TargetResource = None }
    member __.Run(state : AaaaRecordProperties) : DnsZoneRecordConfig =
        { emptyRecord with
            Name = ResourceName "@"
            TTL =
                if state.TTL = None then failwith "You must set a TTL"
                else state.TTL.Value
            ARecords = state.Ipv6Addresses
            Type = AAAA }

    /// Sets the name of the SQL server.
    [<CustomOperation "add_ipv6_addresses">]
    member _.RecordCName(state:AaaaRecordProperties, ipv6Addresses) = { state with Ipv6Addresses = state.Ipv6Addresses @ ipv6Addresses }

    /// Sets the TTL of the record.
    [<CustomOperation "ttl">]
    member _.RecordTTL(state:AaaaRecordProperties, ttl) = { state with TTL = Some ttl }

    /// Sets the target resource of the record.
    [<CustomOperation "target_resource">]
    member _.RecordTargetResource(state:AaaaRecordProperties, targetResource) = { state with TargetResource = Some targetResource }

type DnsZoneConfig =
    { Name : ResourceName
      ZoneType : DnsZoneType
      Records : DnsZoneRecordConfig list  }

    interface IBuilder with
        member this.DependencyName = this.Name
        member this.BuildResources _ = [
            { DnsZone.Name = this.Name
              Properties = {| ZoneType = this.ZoneType |> string |} }

            for record in this.Records do
                { DnsRecord.Name = record.Name
                  Zone = this.Name
                  Type = record.Type
                  TTL = record.TTL
                  TargetResource = record.TargetResource
                  CNameRecord = record.CNameRecord
                  ARecords = record.ARecords
                  AaaaRecords = record.AaaaRecords
                  NsRecords = record.NsRecords
                  PtrRecords = record.PtrRecords }
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
    /// Sets the name of the SQL server.
    [<CustomOperation "name">]
    member _.ServerName(state:DnsZoneConfig, serverName) = { state with Name = serverName }
    member this.ServerName(state:DnsZoneConfig, serverName:string) = this.ServerName(state, ResourceName serverName)
    /// The per-database min and max DTUs to allocate.
    [<CustomOperation "add_records">]
    member _.AddRecords(state:DnsZoneConfig, records) = { state with Records = state.Records @ records }

let dnsZone = DnsZoneBuilder()
let cnameRecord = DnsCNameRecordBuilder()
let aRecord = DnsARecordBuilder()
let aaaaRecord = DnsAaaaRecordBuilder()


//type DnsRecordBuilder() =
//    member _.Yield _ =
//        { DnsZoneRecordConfig.Name = ResourceName ""
//          Type = DnsRecordType.Unknown
//          TTL = 0
//          TargetResource = None
//          CNameRecord = None
//          ARecords = []
//          AaaaRecords = []
//          NsRecords = []
//          PtrRecords = [] }

//    /// Sets the name of the record.
//    [<CustomOperation "name">]
//    member _.RecordName(state:DnsZoneRecordConfig, name) = { state with Name = name }
//    member this.RecordName(state:DnsZoneRecordConfig, name:string) = this.RecordName(state, ResourceName name)

//    /// Sets the type of the record.
//    [<CustomOperation "record_type">]
//    member _.RecordType(state:DnsZoneRecordConfig, _type) = { state with Type = _type }

//    /// Sets the TTL of the record.
//    [<CustomOperation "ttl">]
//    member _.RecordTTL(state:DnsZoneRecordConfig, ttl) = { state with TTL = ttl }

//    /// Sets the target resource of the record.
//    [<CustomOperation "target_resource">]
//    member _.RecordTargetResource(state:DnsZoneRecordConfig, targetResource) = { state with TargetResource = Some targetResource }

//    /// Sets the cname of the record.
//    [<CustomOperation "cname_record">]
//    member _.RecordCname(state:DnsZoneRecordConfig, cname) = { state with CNameRecord = Some cname; Type = CName }

//    /// Sets the IPv4 of the A record.
//    [<CustomOperation "a_record">]
//    member _.RecordA(state:DnsZoneRecordConfig, a) = { state with ARecord = Some a; Type = A; Name = ResourceName "@" }

//    /// Sets the IPv6 of the AAAA record.
//    [<CustomOperation "aaaa_record">]
//    member _.RecordAaaa(state:DnsZoneRecordConfig, a) = { state with AaaaRecord = Some a; Type = AAAA; Name = ResourceName "@" }

//    /// Sets the IPv6 of the AAAA record.
//    [<CustomOperation "ns_record">]
//    member _.RecordNs(state:DnsZoneRecordConfig, a) = { state with NsRecord = Some a; Type = NS; Name = ResourceName "@" }

//    /// Sets the IPv6 of the AAAA record.
//    [<CustomOperation "ptr_record">]
//    member _.RecordPtr(state:DnsZoneRecordConfig, a) = { state with PtrRecord = Some a; Type = PTR; Name = ResourceName "@" }


//    member _.Run (state:DnsZoneRecordConfig) =
//        if state.Name = ResourceName.Empty then failwith "You must set a record name."
//        if state.Type = Unknown then failwith "You must set a record type."
//        if state.TTL <= 0 then failwith "You must set a valid TTL."
//        state


//let dnsRecord = DnsRecordBuilder()
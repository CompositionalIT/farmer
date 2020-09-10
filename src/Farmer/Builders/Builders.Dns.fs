[<AutoOpen>]
module Farmer.Builders.Dns

open Farmer
open Farmer.CoreTypes
open Farmer.Dns
open Farmer.Arm.Dns
open DnsRecords

type DnsZoneType = Public | Private

type DnsZoneConfigRecordProperties =
    { TTL : int 
      TargetResource : ResourceName option }

type DnsZoneRecordConfig =
    { Name : ResourceName
      Type : DnsRecordType
      TTL : int
      TargetResource : ResourceName option
      CNameRecord : string option
      ARecord : string option
      AaaaRecord : string option
      NsRecord : string option
      PtrRecord : string option }

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
                  ARecord = record.ARecord
                  AaaaRecord = record.AaaaRecord
                  NsRecord = record.NsRecord
                  PtrRecord = record.PtrRecord }
        ]

type DnsRecordBuilder() =
    member _.Yield _ =
        { DnsZoneRecordConfig.Name = ResourceName ""
          Type = DnsRecordType.Unknown
          TTL = 0
          TargetResource = None
          CNameRecord = None
          ARecord = None
          AaaaRecord = None
          NsRecord = None
          PtrRecord = None }
    
    /// Sets the name of the record.
    [<CustomOperation "name">]
    member _.RecordName(state:DnsZoneRecordConfig, name) = { state with Name = name }
    member this.RecordName(state:DnsZoneRecordConfig, name:string) = this.RecordName(state, ResourceName name)
    
    /// Sets the type of the record.
    [<CustomOperation "record_type">]
    member _.RecordType(state:DnsZoneRecordConfig, _type) = { state with Type = _type }

    /// Sets the TTL of the record.
    [<CustomOperation "ttl">]
    member _.RecordTTL(state:DnsZoneRecordConfig, ttl) = { state with TTL = ttl }

    /// Sets the target resource of the record.
    [<CustomOperation "target_resource">]
    member _.RecordTargetResource(state:DnsZoneRecordConfig, targetResource) = { state with TargetResource = Some targetResource }

    /// Sets the cname of the record.
    [<CustomOperation "cname_record">]
    member _.RecordCname(state:DnsZoneRecordConfig, cname) = { state with CNameRecord = Some cname; Type = CName }

    /// Sets the IPv4 of the A record.
    [<CustomOperation "a_record">]
    member _.RecordA(state:DnsZoneRecordConfig, a) = { state with ARecord = Some a; Type = A; Name = ResourceName "@" }

    /// Sets the IPv6 of the AAAA record.
    [<CustomOperation "aaaa_record">]
    member _.RecordAaaa(state:DnsZoneRecordConfig, a) = { state with AaaaRecord = Some a; Type = AAAA; Name = ResourceName "@" }

    /// Sets the IPv6 of the AAAA record.
    [<CustomOperation "ns_record">]
    member _.RecordNs(state:DnsZoneRecordConfig, a) = { state with NsRecord = Some a; Type = NS; Name = ResourceName "@" }

    /// Sets the IPv6 of the AAAA record.
    [<CustomOperation "ptr_record">]
    member _.RecordPtr(state:DnsZoneRecordConfig, a) = { state with PtrRecord = Some a; Type = PTR; Name = ResourceName "@" }


    member _.Run (state:DnsZoneRecordConfig) =
        if state.Name = ResourceName.Empty then failwith "You must set a record name."
        if state.Type = Unknown then failwith "You must set a record type."
        if state.TTL <= 0 then failwith "You must set a valid TTL."
        state

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
let dnsRecord = DnsRecordBuilder()
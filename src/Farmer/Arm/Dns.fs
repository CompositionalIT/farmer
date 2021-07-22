[<AutoOpen>]
module Farmer.Arm.Dns

open Farmer
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
        | SRV _ -> srvRecord
        | SOA _ -> soaRecord

type DnsZone =
    { Name : ResourceName
      Dependencies : Set<ResourceId>
      Properties : {| ZoneType : string |} }

    interface IArmResource with
        member this.ResourceId = zones.resourceId this.Name
        member this.JsonModel =
            {| zones.Create(this.Name, Location.Global, this.Dependencies) with
                properties = {| zoneType = this.Properties.ZoneType |}
            |} :> _

module DnsRecords =
    type DnsRecord =
        { Name : ResourceName
          Dependencies : Set<ResourceId>
          Zone : LinkedResource
          Type : DnsRecordType
          TTL : int }
        /// Includes the DnsZone if deployed in the same template (Managed).
        member private this.dependsOn =
            match this.Zone with
            | Managed id -> this.Dependencies |> Set.add id
            | Unmanaged _ -> this.Dependencies
        interface IArmResource with
            member this.ResourceId = this.Type.ResourceType.resourceId (this.Zone.Name, this.Name)

            member this.JsonModel =
                {| this.Type.ResourceType.Create(this.Zone.Name/this.Name, dependsOn = this.dependsOn) with
                    properties = [
                        "TTL", box this.TTL

                        match this.Type with
                        | A (Some targetResource, _)
                        | CName (Some targetResource, _)
                        | AAAA (Some targetResource, _)  ->
                            "targetResource", box {| id = targetResource.Value |}
                        | _ ->
                            ()

                        match this.Type with
                        | CName (_, Some cnameRecord) -> "CNAMERecord", box {| cname = cnameRecord |}
                        | MX records -> "MXRecords", records |> List.map (fun mx -> {| preference = mx.Preference; exchange = mx.Exchange |}) |> box
                        | NS records -> "NSRecords", records |> List.map (fun ns -> {| nsdname = ns |}) |> box
                        | TXT records -> "TXTRecords", records |> List.map (fun txt -> {| value = [ txt ] |}) |> box
                        | PTR records -> "PTRRecords", records |> List.map (fun ptr -> {| ptrdname = ptr |}) |> box
                        | A (_, records) -> "ARecords", records |> List.map (fun a -> {| ipv4Address = a |}) |> box
                        | AAAA (_, records) -> "AAAARecords", records |> List.map (fun aaaa -> {| ipv6Address = aaaa |}) |> box
                        | SRV records -> 
                            let records = 
                                records 
                                |> List.map (fun srv ->
                                    {| priority = srv.Priority |> Option.toNullable
                                       weight = srv.Weight |> Option.toNullable
                                       port = srv.Port |> Option.toNullable
                                       target =  Option.toObj srv.Target |})
                            "SRVRecords", box records
                        | SOA record -> 
                            let record = 
                                {| host = Option.toObj record.Host
                                   email = Option.toObj record.Email
                                   serialNumber = record.SerialNumber |> Option.toNullable
                                   refreshTime = record.RefreshTime |> Option.toNullable
                                   retryTime = record.RetryTime |> Option.toNullable
                                   expireTime = record.ExpireTime |> Option.toNullable
                                   minimumTTL = record.MinimumTTL |> Option.toNullable |}
                            "SOARecord", box record
                        | CName (_, None) -> ()
                    ] |> Map
                |} :> _
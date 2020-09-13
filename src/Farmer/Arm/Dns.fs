[<AutoOpen>]
module Farmer.Arm.Dns

open Farmer
open Farmer.CoreTypes
open Farmer.Dns

let zones = ResourceType "Microsoft.Network/dnszones"
let aRecord = ResourceType "Microsoft.Network/dnszones/A"
let aaaaRecord = ResourceType "Microsoft.Network/dnszones/AAAA"
let cnameRecord = ResourceType "Microsoft.Network/dnszones/CNAME"
let txtRecord = ResourceType "Microsoft.Network/dnszones/TXT"
let mxRecord = ResourceType "Microsoft.Network/dnszones/MX"
let nsRecord = ResourceType "Microsoft.Network/dnszones/NS"
let soaRecord = ResourceType "Microsoft.Network/dnszones/SOA"
let srvRecord = ResourceType "Microsoft.Network/dnszones/SRV"
let ptrRecord = ResourceType "Microsoft.Network/dnszones/PTR"

type DnsZone =
    { Name : ResourceName
      Properties : {| ZoneType : string |} }

    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = zones.ArmValue
               name = this.Name.Value
               location = "global"
               apiVersion = "2018-05-01"
               properties =
                   {| zoneType = this.Properties.ZoneType |}
            |} :> _

module DnsRecords =

    let mapRecordType = function
        | Unknown -> failwith "Not Implemented"
        | CName -> cnameRecord
        | A -> aRecord
        | AAAA -> aaaaRecord
        | NS -> nsRecord
        | PTR -> ptrRecord
        | TXT -> txtRecord
        | MX -> mxRecord
        // | Soa -> soaRecord
        // | Srv -> srvRecord

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
                {| ``type`` = (mapRecordType this.Type).ArmValue
                   name = this.Zone.Value + "/" + this.Name.Value
                   apiVersion = "2018-05-01"
                   properties =
                        {| TTL = this.TTL;
                           targetResource = {| id = this.TargetResource |}
                           CNAMERecords = {| cname = this.CNameRecord |> Option.toObj |} |}
                   dependsOn =
                     [ this.Zone.Value ]
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
                {| ``type`` = (mapRecordType this.Type).ArmValue
                   name = this.Zone.Value + "/" + this.Name.Value
                   apiVersion = "2018-05-01"
                   properties =
                        {| TTL = this.TTL;
                           MXRecords = this.MxRecords |> List.map (fun x -> {| preference = x.Preference; exchange = x.Exchange |}) |}
                   dependsOn =
                     [ this.Zone.Value ]
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
                {| ``type`` = (mapRecordType this.Type).ArmValue
                   name = this.Zone.Value + "/" + this.Name.Value
                   apiVersion = "2018-05-01"
                   properties =
                        {| TTL = this.TTL;
                           targetResource = {| id = null |}
                           NSRecords = this.NsRecords |> List.map (fun x -> {| nsdname = x |}) |}
                   dependsOn =
                     [ this.Zone.Value ]
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
                {| ``type`` = (mapRecordType this.Type).ArmValue
                   name = this.Zone.Value + "/" + this.Name.Value
                   apiVersion = "2018-05-01"
                   properties =
                        {| TTL = this.TTL;
                           TXTRecords = this.TxtRecords |> List.map (fun x -> {| value = [ x ] |}) |}
                   dependsOn =
                     [ this.Zone.Value ]
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
                {| ``type`` = (mapRecordType this.Type).ArmValue
                   name = this.Zone.Value + "/" + this.Name.Value
                   apiVersion = "2018-05-01"
                   properties =
                        {| TTL = this.TTL;
                           PTRRecords = this.PtrRecords |> List.map (fun x -> {| ptrdname = x |}) |}
                   dependsOn =
                     [ this.Zone.Value ]
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
                {| ``type`` = (mapRecordType this.Type).ArmValue
                   name = this.Zone.Value + "/" + this.Name.Value
                   apiVersion = "2018-05-01"
                   properties =
                        {| TTL = this.TTL;
                           targetResource = {| id = this.TargetResource |}
                           ARecords = this.ARecords |> List.map (fun x -> {| ipv4Address = x |}) |}
                   dependsOn =
                     [ this.Zone.Value ]
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
                {| ``type`` = (mapRecordType this.Type).ArmValue
                   name = this.Zone.Value + "/" + this.Name.Value
                   apiVersion = "2018-05-01"
                   properties =
                        {| TTL = this.TTL;
                           targetResource = {| id = this.TargetResource |}
                           AAAARecords = this.AaaaRecords |> List.map (fun x -> {| ipv6Address = x |}) |}
                   dependsOn =
                     [ this.Zone.Value ]
                |} :> _

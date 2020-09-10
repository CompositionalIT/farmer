[<AutoOpen>]
module Farmer.Arm.Dns

open Farmer
open Farmer.CoreTypes
open Farmer.Dns

let zones = ResourceType "Microsoft.Network/dnszones"
let aRecord = ResourceType "Microsoft.Network/dnszones/A"
let cnameRecord = ResourceType "Microsoft.Network/dnszones/CNAME"
let txtRecord = ResourceType "Microsoft.Network/dnszones/TXT"
let mxRecord = ResourceType "Microsoft.Network/dnszones/MX"
let nsRecord = ResourceType "Microsoft.Network/dnszones/NS"
let soaRecord = ResourceType "Microsoft.Network/dnszones/SOA"
let srvRecord = ResourceType "Microsoft.Network/dnszones/SRV"

type DnsZone =
    { Name : ResourceName
      Properties : {| ZoneType : string |}
    }
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
        | Txt -> txtRecord
        | Mx -> mxRecord
        | Ns -> nsRecord
        | Soa -> soaRecord
        | Srv -> srvRecord

    type DnsRecord =
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
                           targetResource = {| id = this.TargetResource |};
                           CNAMERecord = {| cname = this.CNameRecord |> Option.toObj |}   |}
                   dependsOn =
                     [ this.Zone.Value ]
                |} :> _

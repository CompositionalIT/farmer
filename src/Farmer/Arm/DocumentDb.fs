[<AutoOpen>]
module Farmer.Arm.DocumentDb

open Farmer
open Farmer.CoreTypes
open Farmer.CosmosDb
open System

module DatabaseAccounts =
    module SqlDatabases =
        type Containers =
            { Name : ResourceName
              Account : ResourceName
              Database : ResourceName
              PartitionKey :
                {| Paths : string list; Kind : IndexKind |}
              UniqueKeyPolicy :
                {| UniqueKeys : {| Paths : string list |} Set |}
              IndexingPolicy :
                {| IncludedPaths :
                    {| Path : string
                       Indexes : (IndexDataType * IndexKind) list
                    |} list
                   ExcludedPaths : string list
                |}
            }
            interface IArmResource with
                member this.ResourceName = this.Name
                member this.JsonModel =
                    {| ``type`` = "Microsoft.DocumentDb/databaseAccounts/sqlDatabases/containers"
                       name = sprintf "%s/%s/%s" this.Account.Value this.Database.Value this.Name.Value
                       apiVersion = "2020-03-01"
                       dependsOn = [ this.Database.Value ]
                       properties =
                           {| resource =
                               {| id = this.Name.Value
                                  partitionKey =
                                   {| paths = this.PartitionKey.Paths
                                      kind = string this.PartitionKey.Kind |}
                                  uniqueKeyPolicy =
                                   {| uniqueKeys =
                                      this.UniqueKeyPolicy.UniqueKeys
                                      |> Set.map (fun k -> {| paths = k.Paths |}) |}
                                  indexingPolicy =
                                   {| indexingMode = "consistent"
                                      includedPaths =
                                          this.IndexingPolicy.IncludedPaths
                                          |> List.map(fun p ->
                                           {| path = p.Path
                                              indexes =
                                               p.Indexes
                                               |> List.map(fun (dataType, kind) ->
                                                   {| kind = string kind
                                                      dataType = dataType.ToString().ToLower()
                                                      precision = -1 |})
                                           |})
                                      excludedPaths =
                                       this.IndexingPolicy.ExcludedPaths
                                       |> List.map(fun p -> {| path = p |})
                                   |}
                               |}
                           |}
                    |} :> _

    type SqlDatabases =
        { Name : ResourceName
          Account : ResourceName
          Throughput : int<RU> }
        interface IArmResource with
            member this.ResourceName = this.Name
            member this.JsonModel =
                {| ``type`` = "Microsoft.DocumentDB/databaseAccounts/sqlDatabases"
                   name = sprintf "%s/%s" this.Account.Value this.Name.Value
                   apiVersion = "2020-03-01"
                   dependsOn = [ this.Account.Value ]
                   properties =
                       {| resource = {| id = this.Name.Value |}
                          options = {| throughput = string this.Throughput |} |}
                |} :> _

type DatabaseAccount =
    { Name : ResourceName
      Location : Location
      ConsistencyPolicy : ConsistencyPolicy
      FailoverPolicy : FailoverPolicy
      PublicNetworkAccess : FeatureFlag
      FreeTier : bool }
    member this.MaxStatelessPrefix =
        match this.ConsistencyPolicy with
        | BoundedStaleness (staleness, _) -> Some staleness
        | Session | Eventual | ConsistentPrefix | Strong -> None
    member this.MaxInterval =
        match this.ConsistencyPolicy with
        | BoundedStaleness (_, interval) -> Some interval
        | Session | Eventual | ConsistentPrefix | Strong -> None
    member this.EnableAutomaticFailover =
        match this.FailoverPolicy with
        | AutoFailover _ -> Some true
        | _ -> None
    member this.EnableMultipleWriteLocations =
        match this.FailoverPolicy with
        | MultiMaster _ -> Some true
        | _ -> None
    member this.FailoverLocations = [
        match this.FailoverPolicy with
        | AutoFailover secondary
        | MultiMaster secondary ->
            {| LocationName = this.Location.ArmValue; FailoverPriority = 0 |}
            {| LocationName = secondary.ArmValue; FailoverPriority = 1 |}
        | NoFailover ->
            ()
    ]

    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = "Microsoft.DocumentDB/databaseAccounts"
               name = this.Name.Value
               apiVersion = "2020-03-01"
               location = this.Location.ArmValue
               kind = "GlobalDocumentDB"
               tags =
                   {| defaultExperience = "Core (SQL)"
                      CosmosAccountType = "Non-Production" |}
               properties =
                   {| consistencyPolicy =
                        {| defaultConsistencyLevel =
                            match this.ConsistencyPolicy with
                            | BoundedStaleness _ -> "BoundedStaleness"
                            | Session | Eventual | ConsistentPrefix | Strong as policy -> string policy
                           maxStalenessPrefix = this.MaxStatelessPrefix |> Option.toNullable
                           maxIntervalInSeconds = this.MaxInterval |> Option.toNullable
                        |}
                      databaseAccountOfferType = "Standard"
                      enableAutomaticFailover = this.EnableAutomaticFailover |> Option.toNullable
                      autoenableMultipleWriteLocations = this.EnableMultipleWriteLocations |> Option.toNullable
                      locations =
                        match this.FailoverLocations with
                        | [] -> null
                        | locations -> box locations
                      publicNetworkAccess = string this.PublicNetworkAccess
                      enableFreeTier = this.FreeTier
                   |} |> box
            |} :> _
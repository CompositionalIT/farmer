[<AutoOpen>]
module Farmer.Arm.DocumentDb

open Farmer

module DatabaseAccounts =
    module SqlDatabases =
        type Containers =
            { Name : ResourceName
              Account : ResourceName
              Database : ResourceName
              PartitionKey :
                {| Paths : string list
                   Kind : string |}
              IndexingPolicy :
                {| IncludedPaths :
                    {| Path : string
                       Indexes :
                        {| Kind : string
                           DataType : string |} list
                    |} list
                   ExcludedPaths : string list
                |}
            }
            interface IArmResource with
                member this.ResourceName = this.Name
                member this.JsonValue =
                    {| ``type`` = "Microsoft.DocumentDb/databaseAccounts/sqlDatabases/containers"
                       name = sprintf "%s/%s/%s" this.Account.Value this.Database.Value this.Name.Value
                       apiVersion = "2020-03-01"
                       dependsOn = [ this.Database.Value ]
                       properties =
                           {| resource =
                               {| id = this.Name.Value
                                  partitionKey =
                                   {| paths = this.PartitionKey.Paths
                                      kind = this.PartitionKey.Kind |}
                                  indexingPolicy =
                                   {| indexingMode = "consistent"
                                      includedPaths =
                                          this.IndexingPolicy.IncludedPaths
                                          |> List.map(fun p ->
                                           {| path = p.Path
                                              indexes =
                                               p.Indexes
                                               |> List.map(fun i ->
                                                   {| kind = i.Kind
                                                      dataType = i.DataType
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
          Throughput : string }
        interface IArmResource with
            member this.ResourceName = this.Name
            member this.JsonValue =
                {| ``type`` = "Microsoft.DocumentDB/databaseAccounts/sqlDatabases"
                   name = sprintf "%s/%s" this.Account.Value this.Name.Value
                   apiVersion = "2020-03-01"
                   dependsOn = [ this.Account.Value ]
                   properties =
                       {| resource = {| id = this.Name.Value |}
                          options = {| throughput = this.Throughput |} |}
                |} :> _

type DatabaseAccount =
    { Name : ResourceName
      Location : Location
      ConsistencyPolicy : string
      MaxStaleness : int option
      MaxInterval : int option
      EnableAutomaticFailure : bool option
      EnableMultipleWriteLocations : bool option
      FailoverLocations : {| Location :  Location; Priority : int |} list
      PublicNetworkAccess : FeatureFlag
      FreeTier : bool }
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonValue =
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
                        {| defaultConsistencyLevel = this.ConsistencyPolicy
                           maxStalenessPrefix = this.MaxStaleness |> Option.toNullable
                           maxIntervalInSeconds = this.MaxInterval |> Option.toNullable
                        |}
                      databaseAccountOfferType = "Standard"
                      enableAutomaticFailure = this.EnableAutomaticFailure |> Option.toNullable
                      autoenableMultipleWriteLocations = this.EnableMultipleWriteLocations |> Option.toNullable
                      locations =
                            match this.FailoverLocations with
                            | [] ->
                                null
                            | locations ->
                                box [
                                    for location in locations do
                                        {| locationName = location.Location.ArmValue
                                           failoverPriority = location.Priority |}
                                ]
                      publicNetworkAccess = string this.PublicNetworkAccess
                      enableFreeTier = this.FreeTier
                   |} |> box
            |} :> _
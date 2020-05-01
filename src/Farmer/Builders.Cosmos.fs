[<AutoOpen>]
module Farmer.Resources.CosmosDb

open Farmer
open System

type CosmosDbContainer =
    { Name : ResourceName
      Account : ResourceName
      Database : ResourceName
      PartitionKey :
        {| Paths : string list
           Kind : CosmosDbIndexKind |}
      IndexingPolicy :
        {| IncludedPaths :
            {| Path : string
               Indexes :
                {| Kind : CosmosDbIndexKind
                   DataType : CosmosDbIndexDataType |} list
            |} list
           ExcludedPaths : string list
        |}
    }
    interface IResource with
        member this.ResourceName = this.Name
        member this.ToArmObject() =
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
                          indexingPolicy =
                           {| indexingMode = "consistent"
                              includedPaths =
                                  this.IndexingPolicy.IncludedPaths
                                  |> List.map(fun p ->
                                   {| path = p.Path
                                      indexes =
                                       p.Indexes
                                       |> List.map(fun i ->
                                           {| kind = string i.Kind
                                              dataType = (string i.DataType).ToLower()
                                              precision = -1 |})
                                   |})
                              excludedPaths =
                               this.IndexingPolicy.ExcludedPaths
                               |> List.map(fun p -> {| path = p |})
                           |}
                       |}
                   |}
            |} :> _

type CosmosDbSql =
    { Name : ResourceName
      Account : ResourceName
      Throughput : string }
    interface IResource with
        member this.ResourceName = this.Name
        member this.ToArmObject() =
            {| ``type`` = "Microsoft.DocumentDB/databaseAccounts/sqlDatabases"
               name = sprintf "%s/%s" this.Account.Value this.Name.Value
               apiVersion = "2020-03-01"
               dependsOn = [ this.Account.Value ]
               properties =
                   {| resource = {| id = this.Name.Value |}
                      options = {| throughput = this.Throughput |} |}
            |} :> _

type CosmosDbAccount =
    { Name : ResourceName
      Location : Location
      ConsistencyPolicy : ConsistencyPolicy
      WriteModel : FailoverPolicy
      PublicNetworkAccess : FeatureFlag
      FreeTier : bool }
    interface IResource with
        member this.ResourceName = this.Name
        member this.ToArmObject() =
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
                           match this.ConsistencyPolicy with
                           | BoundedStaleness(maxStaleness, maxInterval) ->
                               box {| defaultConsistencyLevel = "BoundedStaleness"
                                      maxStalenessPrefix = maxStaleness
                                      maxIntervalInSeconds = maxInterval |}
                           | Session
                           | Eventual
                           | ConsistentPrefix
                           | Strong ->
                               box {| defaultConsistencyLevel = string this.ConsistencyPolicy |}
                      databaseAccountOfferType = "Standard"
                      enableAutomaticFailure = match this.WriteModel with AutoFailover _ -> Nullable true | _ -> Nullable()
                      autoenableMultipleWriteLocations = match this.WriteModel with MultiMaster _ -> Nullable true | _ -> Nullable()
                      locations =
                       match this.WriteModel with
                       | AutoFailover secondary
                       | MultiMaster secondary ->
                           [ {| locationName = this.Location.ArmValue; failoverPriority = 0 |}
                             {| locationName = secondary.ArmValue; failoverPriority = 1 |} ] |> box
                       | NoFailover ->
                           Nullable() |> box
                      publicNetworkAccess = string this.PublicNetworkAccess
                      enableFreeTier = this.FreeTier
                   |} |> box
            |} :> _

type CosmosDbContainerConfig =
    { Name : ResourceName
      PartitionKey : string list * CosmosDbIndexKind
      Indexes : (string * (CosmosDbIndexDataType * CosmosDbIndexKind) list) list
      ExcludedPaths : string list }
type CosmosDbConfig =
    { ServerName : ResourceRef
      ServerConsistencyPolicy : ConsistencyPolicy
      ServerFailoverPolicy : FailoverPolicy
      Name : ResourceName
      DbThroughput : int
      Containers : CosmosDbContainerConfig list
      PublicNetworkAccess : FeatureFlag
      FreeTier : bool }
    interface IResourceBuilder with
        member this.BuildResources location _ = [
            match this.ServerName with
            | AutomaticallyCreated name ->
                NewResource
                    { Name = name
                      Location = location
                      ConsistencyPolicy = this.ServerConsistencyPolicy
                      WriteModel = this.ServerFailoverPolicy
                      PublicNetworkAccess = this.PublicNetworkAccess
                      FreeTier = this.FreeTier }
            | AutomaticPlaceholder ->
                failwith "No CosmosDB server was specified."
            | External _ ->
                ()
            let sqlDb =
                NewResource
                    { Name = this.Name
                      Account = this.ServerName.ResourceName
                      Throughput = string this.DbThroughput }
            for container in this.Containers do
                NewResource
                    { Name = container.Name
                      Account = this.ServerName.ResourceName
                      Database = this.Name
                      PartitionKey =
                        {| Paths = fst container.PartitionKey
                           Kind = snd container.PartitionKey |}
                      IndexingPolicy =
                        {| ExcludedPaths = container.ExcludedPaths
                           IncludedPaths = [
                                for (path, indexes) in container.Indexes do
                                    {| Path = path
                                       Indexes = [
                                           for (dataType, kind) in indexes do
                                            {| DataType = dataType
                                               Kind = kind |}
                                       ]
                                    |}
                           ]
                        |}
                    }
        ]


type CosmosDbContainerBuilder() =
    member __.Yield _ =
        { Name = ResourceName ""
          PartitionKey = [], Hash
          Indexes = []
          ExcludedPaths = [] }

    /// Sets the name of the container.
    [<CustomOperation "name">]
    member __.Name (state:CosmosDbContainerConfig, name) =
        { state with Name = ResourceName name }

    /// Sets the partition key of the container.
    [<CustomOperation "partition_key">]
    member __.PartitionKey (state:CosmosDbContainerConfig, partitions, indexKind) =
        { state with PartitionKey = partitions, indexKind }

    /// Adds an index to the container.
    [<CustomOperation "add_index">]
    member __.AddIndex (state:CosmosDbContainerConfig, path, indexes) =
        { state with Indexes = (path, indexes) :: state.Indexes }

    /// Excludes a path from the container index.
    [<CustomOperation "exclude_path">]
    member __.ExcludePath (state:CosmosDbContainerConfig, path) =
        { state with ExcludedPaths = path :: state.ExcludedPaths }
type CosmosDbBuilder() =
    member __.Yield _ =
        { Name = ResourceName.Empty
          ServerName = AutomaticPlaceholder
          ServerConsistencyPolicy = Eventual
          ServerFailoverPolicy = NoFailover
          DbThroughput = 400
          Containers = []
          PublicNetworkAccess = Enabled
          FreeTier = false }
    member __.Run state =
        match state.ServerName with
        | AutomaticallyCreated _
        | External _ ->
            state
        | AutomaticPlaceholder ->
            { state with ServerName = sprintf "%s-server" state.Name.Value |> ResourceName |> AutomaticallyCreated }
    /// Sets the name of the CosmosDB server.
    [<CustomOperation "server_name">]
    member __.ServerName(state:CosmosDbConfig, serverName) = { state with ServerName = AutomaticallyCreated serverName }
    member this.ServerName(state:CosmosDbConfig, serverName:string) = this.ServerName(state, ResourceName serverName)
    /// Links the database to an existing server
    [<CustomOperation "link_to_server">]
    member __.LinkToServer(state:CosmosDbConfig, server:CosmosDbConfig) = { state with ServerName = External server.ServerName.ResourceName }
    /// Sets the name of the database.
    [<CustomOperation "name">]
    member __.Name(state:CosmosDbConfig, name) = { state with Name = name }
    member this.Name(state:CosmosDbConfig, name:string) = this.Name(state, ResourceName name)
    /// Sets the consistency policy of the database.
    [<CustomOperation "consistency_policy">]
    member __.ConsistencyPolicy(state:CosmosDbConfig, consistency:ConsistencyPolicy) = { state with ServerConsistencyPolicy = consistency }
    /// Sets the failover policy of the database.
    [<CustomOperation "failover_policy">]
    member __.FailoverPolicy(state:CosmosDbConfig, failoverPolicy:FailoverPolicy) = { state with ServerFailoverPolicy = failoverPolicy }
    /// Sets the throughput of the server.
    [<CustomOperation "throughput">]
    member __.Throughput(state:CosmosDbConfig, throughput) = { state with DbThroughput = throughput }
    /// Adds a list of containers to the database.
    [<CustomOperation "add_containers">]
    member __.AddContainers(state:CosmosDbConfig, containers) = { state with Containers = state.Containers @ containers }
    /// Enables public network access
    [<CustomOperation "enable_public_network_access">]
    member __.PublicNetworkAccess(state:CosmosDbConfig) = { state with PublicNetworkAccess = Enabled }
    /// Disables public network access
    [<CustomOperation "disable_public_network_access">]
    member __.PrivateNetworkAccess(state:CosmosDbConfig) = { state with PublicNetworkAccess = Disabled }
    /// Enables the use of CosmosDB free tier (one per subscription).
    [<CustomOperation "free_tier">]
    member __.FreeTier(state:CosmosDbConfig) = { state with FreeTier = true }

open WebApp
type WebAppBuilder with
    member this.DependsOn(state:WebAppConfig, cosmosDbConfig:CosmosDbConfig) =
        this.DependsOn(state, cosmosDbConfig.Name)
type FunctionsBuilder with
    member this.DependsOn(state:FunctionsConfig, cosmosDbConfig:CosmosDbConfig) =
        this.DependsOn(state, cosmosDbConfig.Name)

let cosmosDb = CosmosDbBuilder()
let cosmosContainer = CosmosDbContainerBuilder()

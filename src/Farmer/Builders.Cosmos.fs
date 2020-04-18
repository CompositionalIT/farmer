[<AutoOpen>]
module Farmer.Resources.CosmosDb

open Farmer
open Farmer.Models

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

module Converters =
    let cosmosDb location (cosmos:CosmosDbConfig) =
        let account =
            match cosmos.ServerName with
            | AutomaticallyCreated name ->
                { Name = name
                  Location = location
                  ConsistencyPolicy = cosmos.ServerConsistencyPolicy
                  WriteModel = cosmos.ServerFailoverPolicy
                  PublicNetworkAccess = cosmos.PublicNetworkAccess
                  FreeTier = cosmos.FreeTier } |> Some
            | AutomaticPlaceholder ->
                failwith "No CosmosDB server was specified."
            | External _ ->
                None
        let sqlDb =
            { Name = cosmos.Name
              Account = cosmos.ServerName.ResourceName
              Throughput = string cosmos.DbThroughput }
        let containers = [
            for container in cosmos.Containers do
                { Name = container.Name
                  Account = cosmos.ServerName.ResourceName
                  Database = cosmos.Name
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
        {| Account = account
           SqlDb = sqlDb
           Containers = containers |}
    module Outputters =
        open System
        let cosmosDbContainer (container:CosmosDbContainer) = {|
            ``type`` = "Microsoft.DocumentDb/databaseAccounts/sqlDatabases/containers"
            name = sprintf "%s/%s/%s" container.Account.Value container.Database.Value container.Name.Value
            apiVersion = "2020-03-01"
            dependsOn = [ container.Database.Value ]
            properties =
                {| resource =
                    {| id = container.Name.Value
                       partitionKey =
                        {| paths = container.PartitionKey.Paths
                           kind = string container.PartitionKey.Kind |}
                       indexingPolicy =
                        {| indexingMode = "consistent"
                           includedPaths =
                               container.IndexingPolicy.IncludedPaths
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
                            container.IndexingPolicy.ExcludedPaths
                            |> List.map(fun p -> {| path = p |})
                        |}
                    |}
                |}
        |}
        let cosmosDbAccount (account:CosmosDbAccount) = {|
            ``type`` = "Microsoft.DocumentDB/databaseAccounts"
            name = account.Name.Value
            apiVersion = "2020-03-01"
            location = account.Location.Value
            kind = "GlobalDocumentDB"
            tags =
                {| defaultExperience = "Core (SQL)"
                   CosmosAccountType = "Non-Production" |}
            properties =
                {| consistencyPolicy =
                        match account.ConsistencyPolicy with
                        | BoundedStaleness(maxStaleness, maxInterval) ->
                            box {| defaultConsistencyLevel = "BoundedStaleness"
                                   maxStalenessPrefix = maxStaleness
                                   maxIntervalInSeconds = maxInterval |}
                        | Session
                        | Eventual
                        | ConsistentPrefix
                        | Strong ->
                            box {| defaultConsistencyLevel = string account.ConsistencyPolicy |}
                   databaseAccountOfferType = "Standard"
                   enableAutomaticFailure = match account.WriteModel with AutoFailover _ -> Nullable true | _ -> Nullable()
                   autoenableMultipleWriteLocations = match account.WriteModel with MultiMaster _ -> Nullable true | _ -> Nullable()
                   locations =
                    match account.WriteModel with
                    | AutoFailover secondary
                    | MultiMaster secondary ->
                        [ {| locationName = account.Location.Value; failoverPriority = 0 |}
                          {| locationName = secondary.Value; failoverPriority = 1 |} ] |> box
                    | NoFailover ->
                        Nullable() |> box
                   publicNetworkAccess = string account.PublicNetworkAccess
                   enableFreeTier = account.FreeTier
                |} |> box
        |}
        let cosmosDbSql (db:CosmosDbSql) = {|
            ``type`` = "Microsoft.DocumentDB/databaseAccounts/sqlDatabases"
            name = sprintf "%s/%s" db.Account.Value db.Name.Value
            apiVersion = "2020-03-01"
            dependsOn = [ db.Account.Value ]
            properties =
                {| resource = {| id = db.Name.Value |}
                   options = {| throughput = db.Throughput |} |}
        |}

open Farmer.Models
type ArmBuilder.ArmBuilder with
    member __.AddResource(state:ArmConfig, config:CosmosDbConfig) =
        let outputs =
            config
            |> Converters.cosmosDb state.Location
        let resources = [
            match outputs.Account with
            | Some account -> CosmosAccount account
            | None -> ()
            CosmosSqlDb outputs.SqlDb
            yield! outputs.Containers |> List.map CosmosContainer
        ]
        { state with Resources = state.Resources @ resources }
    member this.AddResources (state, configs) = addResources<CosmosDbConfig> this.AddResource state configs

let cosmosDb = CosmosDbBuilder()
let cosmosContainer = CosmosDbContainerBuilder()

[<AutoOpen>]
module Farmer.Resources.CosmosDb

open Farmer

type CosmosDbContainerConfig =
    { Name : ResourceName
      PartitionKey : string list * CosmosDbIndexKind
      Indexes : (string * (CosmosDbIndexDataType * CosmosDbIndexKind) list) list
      ExcludedPaths : string list }
type CosmosDbConfig =
    { ServerName : ResourceName
      ServerConsistencyPolicy : ConsistencyPolicy
      ServerFailoverPolicy : FailoverPolicy
      DbName : ResourceName
      DbThroughput : string
      Containers : CosmosDbContainerConfig list }

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
        { DbName = ResourceName "CosmosDatabase"
          ServerName = ResourceName "CosmosServer"
          ServerConsistencyPolicy = Eventual
          ServerFailoverPolicy = NoFailover
          DbThroughput = "400"
          Containers = [] }
    /// Sets the name of the CosmosDB server.
    [<CustomOperation "server_name">]
    member __.ServerName(state:CosmosDbConfig, serverName) = { state with ServerName = serverName }
    member this.ServerName(state:CosmosDbConfig, serverName:string) = this.ServerName(state, ResourceName serverName)
    /// Sets the name of the database.
    [<CustomOperation "name">]
    member __.Name(state:CosmosDbConfig, name) = { state with DbName = name }
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
    member this.Throughput(state:CosmosDbConfig, throughput:int) = this.Throughput(state, string throughput)
    /// Adds a list of containers to the database.
    [<CustomOperation "add_containers">]
    member __.AddContainers(state:CosmosDbConfig, containers) =
        { state with Containers = state.Containers @ containers }

open WebApp
type WebAppBuilder with
    member this.DependsOn(state:WebAppConfig, cosmosDbConfig:CosmosDbConfig) =
        this.DependsOn(state, cosmosDbConfig.DbName)
type FunctionsBuilder with
    member this.DependsOn(state:FunctionsConfig, cosmosDbConfig:CosmosDbConfig) =
        this.DependsOn(state, cosmosDbConfig.DbName)

module Converters =
    open Farmer.Models

    let cosmosDb location (cosmos:CosmosDbConfig) =
        let account =
            { Name = cosmos.ServerName
              Location = location
              ConsistencyPolicy = cosmos.ServerConsistencyPolicy
              WriteModel = cosmos.ServerFailoverPolicy }
        let sqlDb =
            { Name = cosmos.DbName
              Account = cosmos.ServerName
              Throughput = cosmos.DbThroughput }
        let containers =
            cosmos.Containers
            |> List.map(fun c ->
                { Name = c.Name
                  Account = cosmos.ServerName
                  Database = cosmos.DbName
                  PartitionKey =
                    {| Paths = fst c.PartitionKey
                       Kind = snd c.PartitionKey |}
                  IndexingPolicy =
                    {| ExcludedPaths = c.ExcludedPaths
                       IncludedPaths =
                           c.Indexes
                           |> List.map(fun index ->
                             {| Path = fst index
                                Indexes =
                                    index
                                    |> snd
                                    |> List.map(fun (dataType, kind) ->
                                        {| DataType = dataType
                                           Kind = kind |})
                             |})
                    |}
                })
        {| Account = account; SqlDb = sqlDb; Containers = containers |}

let cosmosDb = CosmosDbBuilder()
let container = CosmosDbContainerBuilder()

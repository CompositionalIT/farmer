[<AutoOpen>]
module Farmer.CosmosDb

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

type CosmosDbContainer() =
    member __.Yield _ =
        { Name = ResourceName ""
          PartitionKey = [], Hash
          Indexes = []
          ExcludedPaths = [] }

    [<CustomOperation "name">]
    /// Sets the name of the container.
    member __.Name (state:CosmosDbContainerConfig, name) =
        { state with Name = ResourceName name }

    [<CustomOperation "partition_key">]
    /// Sets the partition key of the container.
    member __.PartitionKey (state:CosmosDbContainerConfig, partitions, indexKind) =
        { state with PartitionKey = partitions, indexKind }

    [<CustomOperation "add_index">]
    /// Adds an index to the container.
    member __.AddIndex (state:CosmosDbContainerConfig, path, indexes) =
        { state with Indexes = (path, indexes) :: state.Indexes }

    [<CustomOperation "exclude_path">]
    /// Excludes a path from the container index.
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
    [<CustomOperation "server_name">]
    /// Sets the name of the CosmosDB server.
    member __.ServerName(state:CosmosDbConfig, serverName) = { state with ServerName = serverName }
    member this.ServerName(state:CosmosDbConfig, serverName:string) = this.ServerName(state, ResourceName serverName)
    [<CustomOperation "name">]
    /// Sets the name of the database.
    member __.Name(state:CosmosDbConfig, name) = { state with DbName = name }
    member this.Name(state:CosmosDbConfig, name:string) = this.Name(state, ResourceName name)
    [<CustomOperation "consistency_policy">]
    /// Sets the consistency policy of the database.
    member __.ConsistencyPolicy(state:CosmosDbConfig, consistency:ConsistencyPolicy) = { state with ServerConsistencyPolicy = consistency }
    [<CustomOperation "failover_policy">]
    /// Sets the failover policy of the database.
    member __.FailoverPolicy(state:CosmosDbConfig, failoverPolicy:FailoverPolicy) = { state with ServerFailoverPolicy = failoverPolicy }
    [<CustomOperation "throughput">]
    /// Sets the throughput of the server.
    member __.Throughput(state:CosmosDbConfig, throughput) = { state with DbThroughput = throughput }
    member this.Throughput(state:CosmosDbConfig, throughput:int) = this.Throughput(state, string throughput)
    [<CustomOperation "add_containers">]
    /// Adds a list of containers to the database.
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
    open Farmer.Internal
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
let container = CosmosDbContainer()

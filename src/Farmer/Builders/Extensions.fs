namespace Farmer.Builders

open Farmer
open System
open Farmer.Arm

type ITaggable<'TConfig> =
    abstract member Add : 'TConfig -> list<string * string> -> 'TConfig
type IDependable<'TConfig> =
    abstract member Add : 'TConfig -> ResourceId Set -> 'TConfig
type IPrivateEndpoints<'TConfig> =
    abstract member Add : 'TConfig -> (SubnetReference * String option * LinkedResource option) Set -> 'TConfig // todo: create privateDnsZoneReference

[<AutoOpen>]
module Extensions =
    module Map =
        let merge newValues map =
            (map, newValues)
            ||> List.fold (fun map (key, value) -> Map.add key value map)

    type ITaggable<'T> with
        /// Adds the provided set of tags to the builder.
        [<CustomOperation "add_tags">]
        member this.Tags(state:'T, pairs) =
            this.Add state pairs
        /// Adds the provided tag to the builder.
        [<CustomOperation "add_tag">]
        member this.Tag(state:'T, key, value) = this.Tags(state, [ key, value ])

    type IDependable<'TConfig> with
        [<CustomOperation "depends_on">]
        member this.DependsOn(state:'TConfig, builder:IBuilder) = this.DependsOn (state, builder.ResourceId)
        member this.DependsOn(state:'TConfig, builders:IBuilder list) = this.DependsOn (state, builders |> List.map (fun x -> x.ResourceId))
        member this.DependsOn(state:'TConfig, resource:IArmResource) = this.DependsOn (state, resource.ResourceId)
        member this.DependsOn(state:'TConfig, resources:IArmResource list) = this.DependsOn (state, resources |> List.map (fun x -> x.ResourceId))
        member this.DependsOn (state:'TConfig, resourceId:ResourceId) = this.DependsOn(state, [ resourceId ])
        member this.DependsOn (state:'TConfig, resourceIds:ResourceId list) = this.Add state (Set resourceIds)
        member this.DependsOn (state:'TConfig, resources:LinkedResource list) = this.Add state (Set (resources |> List.choose (function | Managed r -> Some r | _ -> None)))
        member this.DependsOn (state:'TConfig, resource:LinkedResource) = this.DependsOn (state , [ resource ])

    type IPrivateEndpoints<'TConfig> with
        [<CustomOperation "add_private_endpoint">]
        member this.AddPrivateEndpoint(state, (subnet, name) ) = this.Add state (Set.singleton (subnet, Some name, None))
        member this.AddPrivateEndpoint(state, subnet) = this.Add state (Set.singleton (subnet, None, None))
        member this.AddPrivateEndpoint(state, (subnet, name, privateDnsZone)) = this.Add state (Set.singleton (subnet, name, privateDnsZone))
        [<CustomOperation "add_private_endpoints">]
        member this.AddPrivateEndpoints(state, subnets) = this.Add state (subnets |> Set.map (fun (sn,ep) -> (sn, Some ep, None)))
        member this.AddPrivateEndpoints(state, subnets) = this.Add state (subnets |> Set.map (fun sn -> (sn, None, None)))
        member this.AddPrivateEndpoints(state, subnets) = this.Add state subnets

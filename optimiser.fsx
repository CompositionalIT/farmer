#r "nuget:FsCheck"

open FsCheck
open System

type [<Measure>] Gb
type [<Measure>] VCores

let resourceMinimums =
    [
        0.25<VCores>, 0.5<Gb>
        0.5<VCores>, 1.0<Gb>
        0.75<VCores>, 1.5<Gb>
        1.0<VCores>, 2.0<Gb>
        1.25<VCores>, 2.5<Gb>
        1.5<VCores>, 3.0<Gb>
        1.75<VCores>, 3.5<Gb>
        2.0<VCores>, 4.<Gb>
    ]


let optimise containers (cores:float<VCores>, memory:float<Gb>) =
    let containers = float containers
    let minCores = resourceMinimums |> List.tryFind (fun (cores, _) -> float cores > containers * 0.05) |> Option.map fst
    let minRam = resourceMinimums |> List.tryFind (fun (_, ram) -> float ram > containers * 0.01) |> Option.map snd
    match minCores, minRam with
    | Some minCores, Some minRam ->
        if minCores > cores then Error $"Insufficient cores (minimum is {minCores}VCores)."
        elif minRam > memory then Error $"Insufficient memory (minimum is {minRam}Gb)."
        else
            let cores = float cores
            let memory = float memory

            let vcoresPerContainer = Math.Round ((cores / containers) * 20., MidpointRounding.ToZero) / 20.
            let remainingCores = cores - (vcoresPerContainer * containers)

            let gbPerContainer = Math.Round ((memory / containers) * 100., MidpointRounding.ToZero) / 100.
            let remainingGb = memory - (gbPerContainer * containers)

            Ok
                [
                    for container in 1. .. containers do
                        if container = 1. then (vcoresPerContainer + remainingCores) * 1.<VCores>, (gbPerContainer + remainingGb) * 1.<Gb>
                        else vcoresPerContainer * 1.<VCores>, gbPerContainer * 1.<Gb>
                ]
    | None, _ ->
        Error "Insufficient cores"
    | _, None ->
        Error "Insufficient memory"

// Usage
optimise 4 (1.0<VCores>, 4.<Gb>)













type Inputs = PositiveInt * float<VCores> * float<Gb>
type ValidInput = ValidInput of Inputs
type InvalidInput = InvalidInput of Inputs

type Tests =
    static member totalsAlwaysEqualInput (ValidInput(PositiveInt containers, cores, memory)) =
        let split = optimise containers (cores, memory)
        match split with
        | Ok split ->
            let correctCores = split |> List.sumBy fst |> decimal = decimal cores
            let correctRam = split |> List.sumBy snd |> decimal = decimal memory
            correctCores && correctRam
        | Error msg ->
            failwith msg

    static member givesBackCorrectNumberOfConfigs (ValidInput(PositiveInt containers, cores, memory)) =
        let split = optimise containers (cores, memory)
        match split with
        | Ok split -> split.Length = containers
        | Error msg -> failwith msg

    static member neverReturnsLessThanMinimum (ValidInput(PositiveInt containers, cores, memory)) =
        let split = optimise containers (cores, memory)
        match split with
        | Ok split -> split |> List.forall(fun (c, m) -> c >= 0.05<VCores> && m >= 0.01<Gb>)
        | Error msg -> failwith msg

    static member failsIfInputsInvalid (InvalidInput(PositiveInt containers, cores, memory)) =
        let split = optimise containers (cores, memory)
        match split with
        | Ok _ -> failwith "Should have failed."
        | Error _ -> true

let basicGen = gen {
    let! cores, gb = Gen.elements resourceMinimums
    let! containers = Arb.Default.PositiveInt () |> Arb.filter(fun (PositiveInt s) -> s < 20) |> Arb.toGen
    return containers, cores, gb
}

let shrinker checker (con:PositiveInt, cor, mem) =
    [
        if con.Get > 1 then PositiveInt (con.Get - 1), cor, mem
        if cor > 0.25<VCores> then con, cor - 0.25<VCores>, mem - 0.5<Gb>
    ]
    |> List.filter checker

type ResourceArb =
    static member IsValid (PositiveInt con, cor, mem) =
        let cores = resourceMinimums |> Seq.find(fun (cores, _) -> float cores > float con * 0.05) |> fst <= cor
        let memory = resourceMinimums |> Seq.find(fun (_, mem) -> float mem > float con * 0.01) |> snd <= mem
        cores && memory

    static member ValidInputs () =
        { new Arbitrary<ValidInput> () with
            override _.Generator = basicGen |> Gen.filter ResourceArb.IsValid |> Gen.map ValidInput
            override _.Shrinker (ValidInput inputs) = inputs |> shrinker ResourceArb.IsValid |> Seq.map ValidInput
        }
    static member InvalidInputs () =
        { new Arbitrary<InvalidInput> () with
            override _.Generator = basicGen |> Gen.filter (ResourceArb.IsValid >> not) |> Gen.map InvalidInput
            override _.Shrinker (InvalidInput inputs) = inputs |> shrinker (ResourceArb.IsValid >> not) |> Seq.map InvalidInput
        }

let config = { Config.Default with Arbitrary = [ typeof<ResourceArb> ] }

Check.All<Tests>(config)
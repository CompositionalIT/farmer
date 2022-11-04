namespace Farmer

exception FarmerException of ErrorMessage: string with
    override this.Message = this.ErrorMessage

[<AutoOpen>]
module Exceptions =
    let raiseFarmer msg = msg |> FarmerException |> raise

namespace global

[<RequireQualifiedAccess>]
module Result =
    open Farmer
    open System

    let ofOption error =
        function
        | Some s -> Ok s
        | None -> Error error

    let toOption =
        function
        | Ok s -> Some s
        | Error _ -> None

    let ignore result = Result.map ignore result

    let sequence results =
        let successes = ResizeArray()
        let failures = ResizeArray()

        for result in results do
            match result with
            | Ok result -> successes.Add result
            | Error result -> failures.Add result

        if failures.Count > 0 then
            Error failures.[0]
        else
            Ok(successes.ToArray())

    let ofExn thunk arg =
        try
            Ok(thunk arg)
        with ex ->
            Error(string ex)
    // Unsafely unwraps a Result. If the Result is an Error, the Error is cascaded as an exception.
    let get =
        function
        | Ok value -> value
        | Error err -> raiseFarmer (err.ToString())

    let bindError onError =
        function
        | Error s -> onError s
        | s -> s

    type ResultBuilder() =
        member _.Return(x) = Ok x

        member _.ReturnFrom(m: Result<_, _>) = m

        member _.Bind(m, f) = Result.bind f m
        member _.Bind((m, error): (Option<'T> * 'E), f) = m |> ofOption error |> Result.bind f

        member _.Zero() = None

        member _.Combine(m, f) = Result.bind f m

        member _.Delay(f: unit -> _) = f

        member _.Run(f) = f ()

        member this.TryWith(m, h) =
            try
                this.ReturnFrom(m)
            with e ->
                h e

        member this.TryFinally(m, compensation) =
            try
                this.ReturnFrom(m)
            finally
                compensation ()

        member this.Using(res: #IDisposable, body) =
            this.TryFinally(
                body res,
                fun () ->
                    match res with
                    | null -> ()
                    | disp -> disp.Dispose()
            )

        member this.While(guard, f) =
            if not (guard ()) then
                Ok()
            else
                do f () |> Core.Operators.ignore
                this.While(guard, f)

        member this.For(sequence: seq<_>, body) =
            this.Using(
                sequence.GetEnumerator(),
                fun enum -> this.While(enum.MoveNext, this.Delay(fun () -> body enum.Current))
            )

[<RequireQualifiedAccess>]
module Option =
    /// Maps an optional value then pipes the result to Option.toList.
    let mapList mapper = Option.map mapper >> Option.toList

    /// Maps an optional value, boxing the result, and then pipes the result to Option.toObj.
    let mapBoxed mapper =
        Option.map (mapper >> box) >> Option.toObj

[<AutoOpen>]
module Builders =
    let result = Result.ResultBuilder()

    type Result<'TS, 'TE> with

        /// Unsafely unwraps the Success value out of the Result.
        member this.OkValue = Result.get this

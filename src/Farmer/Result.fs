module Result

open System

let ofOption error = function Some s -> Ok s | None -> Error error
let ignore result = Result.map ignore result

type ResultBuilder() =
    member _.Return(x) = Ok x

    member _.ReturnFrom(m: Result<_, _>) = m

    member _.Bind(m, f) = Result.bind f m
    member _.Bind((m, error): (Option<'T> * 'E), f) = m |> ofOption error |> Result.bind f

    member _.Zero() = None

    member _.Combine(m, f) = Result.bind f m

    member _.Delay(f: unit -> _) = f

    member _.Run(f) = f()

    member _.TryWith(m, h) =
        try __.ReturnFrom(m)
        with e -> h e

    member _.TryFinally(m, compensation) =
        try __.ReturnFrom(m)
        finally compensation()

    member _.Using(res:#IDisposable, body) =
        __.TryFinally(body res, fun () -> match res with null -> () | disp -> disp.Dispose())

    member _.While(guard, f) =
        if not (guard()) then Ok () else
        do f() |> ignore
        __.While(guard, f)

    member _.For(sequence:seq<_>, body) =
        __.Using(sequence.GetEnumerator(), fun enum -> __.While(enum.MoveNext, __.Delay(fun () -> body enum.Current)))

let result = ResultBuilder()
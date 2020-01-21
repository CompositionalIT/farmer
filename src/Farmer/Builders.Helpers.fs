module Farmer.Helpers

open Farmer
open System

let sanitise filters maxLength (resourceName:ResourceName) =
    resourceName.Value.ToLower()
    |> Seq.filter(fun c -> Seq.exists(fun filter -> filter c) filters)
    |> Seq.truncate maxLength
    |> Seq.toArray
    |> System.String
let sanitiseStorage = sanitise [ Char.IsLetterOrDigit ] 16
let sanitiseSearch = sanitise [ Char.IsLetterOrDigit; (=) '-' ] 60
let santitiseDb = sanitise [ Char.IsLetterOrDigit ] 100 >> fun r -> r.ToLower()
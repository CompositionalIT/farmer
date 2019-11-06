module Farmer.Helpers

open Farmer

let sanitise filters maxLength (resourceName:ResourceName) =
    resourceName.Value.ToLower()
    |> Seq.filter(fun c -> Seq.exists(fun filter -> filter c) filters)
    |> Seq.truncate maxLength
    |> Seq.toArray
    |> System.String
let sanitiseStorage = sanitise [ System.Char.IsLetterOrDigit ] 16
let sanitiseSearch = sanitise [ System.Char.IsLetterOrDigit; (=) '-' ] 60
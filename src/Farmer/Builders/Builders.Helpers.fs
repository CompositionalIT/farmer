module Farmer.Helpers

open System

let sanitise filters maxLength (resourceName:ResourceName) =
    resourceName.Value.ToLower()
    |> Seq.filter(fun c -> Seq.exists(fun filter -> filter c) filters)
    |> Seq.truncate maxLength
    |> Seq.toArray
    |> String
let sanitiseStorage = sanitise [ Char.IsLetterOrDigit ] 24
let sanitiseSearch = sanitise [ Char.IsLetterOrDigit; (=) '-' ] 60
let sanitiseDb = sanitise [ Char.IsLetterOrDigit ] 100 >> fun r -> r.ToLower()
let sanitiseMaps = sanitise [ Char.IsLetterOrDigit; (=) '-'; (=) '.'; (=) '_' ] 98
let sanitiseSignalR = sanitise [ Char.IsLetterOrDigit; (=) '-' ] 63
module Farmer.Helpers

open System

let sanitise filters maxLength (resourceName:_ ResourceName) =
    resourceName.Value.ToLower()
    |> Seq.filter(fun c -> Seq.exists(fun filter -> filter c) filters)
    |> Seq.truncate maxLength
    |> Seq.toArray
    |> String
let sanitiseStorage v = v |> sanitise [ Char.IsLetterOrDigit ] 24 |> Storage.createStorageAccountName |> Result.get
let sanitiseSearch v = v |> sanitise [ Char.IsLetterOrDigit; (=) '-' ] 60
let sanitiseDb v = v |> sanitise [ Char.IsLetterOrDigit ] 100 |> fun r -> r.ToLower()
let sanitiseMaps v = v |> sanitise [ Char.IsLetterOrDigit; (=) '-'; (=) '.'; (=) '_' ] 98
let sanitiseSignalR v = v |> sanitise [ Char.IsLetterOrDigit; (=) '-' ] 63

let singletonOrEmptyList = function
    | None -> []
    | Some v -> [v]
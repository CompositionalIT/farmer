#r "nuget:Farmer"

open Farmer
open Farmer.Builders

let myStorage = storageAccount {
    name "mystorage"
    sku Storage.Sku.Standard_LRS
    add_lifecycle_rule "cleanup" [ Storage.DeleteAfter 7<Days> ] Storage.NoRuleFilters
    add_lifecycle_rule "test" [ Storage.DeleteAfter 1<Days>; Storage.DeleteAfter 2<Days>; Storage.ArchiveAfter 1<Days>; ] [ "foo/bar" ]
}
let template = arm {
    add_resource myStorage
}

template |> Writer.quickWrite "template"
template |> Deploy.execute "functions-rg" []
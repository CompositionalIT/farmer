#r "nuget:Farmer"

open Farmer
open Farmer.Builders

let myStorage = storageAccount {
    name "myfarmerstorage"
    sku Storage.Sku.Standard_LRS
    add_queues [ "queue1"; "queue2" ]
    add_private_container "container1"
    add_table "table1"
    add_tables [ "table2"; "table3" ]
    add_lifecycle_rule "cleanup" [ Storage.DeleteAfter 7<Days> ] Storage.NoRuleFilters

    add_lifecycle_rule "test" [
        Storage.DeleteAfter 1<Days>
        Storage.DeleteAfter 2<Days>
        Storage.ArchiveAfter 1<Days>
    ] [ "foo/bar" ]

    disable_public_network_access
    disable_blob_public_access
    disable_shared_key_access
    default_to_oauth_authentication
    restrict_to_azure_services [ Farmer.Arm.Storage.NetworkRuleSetBypass.AzureServices ]
}

let template = arm { add_resource myStorage }

template |> Writer.quickWrite "template"
template |> Deploy.execute "farmer-test-rg" []

module Farmer.Arm.ResourceTypes.Storage

open Farmer

let storageAccounts =
    ResourceType("Microsoft.Storage/storageAccounts", "2022-05-01")

let blobServices =
    ResourceType("Microsoft.Storage/storageAccounts/blobServices", "2019-06-01")

let containers =
    ResourceType("Microsoft.Storage/storageAccounts/blobServices/containers", "2018-03-01-preview")

let fileServices =
    ResourceType("Microsoft.Storage/storageAccounts/fileServices", "2019-06-01")

let fileShares =
    ResourceType("Microsoft.Storage/storageAccounts/fileServices/shares", "2019-06-01")

let queueServices =
    ResourceType("Microsoft.Storage/storageAccounts/queueServices", "2019-06-01")

let queues =
    ResourceType("Microsoft.Storage/storageAccounts/queueServices/queues", "2019-06-01")

let tableServices =
    ResourceType("Microsoft.Storage/storageAccounts/tableServices", "2019-06-01")

let tables =
    ResourceType("Microsoft.Storage/storageAccounts/tableServices/tables", "2019-06-01")

let managementPolicies =
    ResourceType("Microsoft.Storage/storageAccounts/managementPolicies", "2019-06-01")

let roleAssignments =
    ResourceType("Microsoft.Storage/storageAccounts/providers/roleAssignments", "2018-09-01-preview")

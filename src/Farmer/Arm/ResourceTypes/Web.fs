module Farmer.Arm.ResourceTypes.Web

open Farmer

let serverFarms = ResourceType("Microsoft.Web/serverfarms", "2018-02-01")
let sites = ResourceType("Microsoft.Web/sites", "2021-03-01")
let config = ResourceType("Microsoft.Web/sites/config", "2016-08-01")

let sourceControls =
    ResourceType("Microsoft.Web/sites/sourcecontrols", "2019-08-01")

let staticSites = ResourceType("Microsoft.Web/staticSites", "2019-12-01-preview")

let staticSitesConfig =
    ResourceType("Microsoft.Web/staticSites/config", "2020-09-01")

let siteExtensions =
    ResourceType("Microsoft.Web/sites/siteextensions", "2020-06-01")

let slots = ResourceType("Microsoft.Web/sites/slots", "2020-09-01")
let certificates = ResourceType("Microsoft.Web/certificates", "2019-08-01")

let hostNameBindings =
    ResourceType("Microsoft.Web/sites/hostNameBindings", "2020-12-01")

let virtualNetworkConnections =
    ResourceType("Microsoft.Web/sites/virtualNetworkConnections", "2021-03-01")

let siteFunctions = ResourceType("Microsoft.Web/sites/functions", "2021-03-01")

let slotsVirtualNetworkConnections =
    ResourceType("Microsoft.Web/sites/slots/virtualNetworkConnections", "2021-03-01")

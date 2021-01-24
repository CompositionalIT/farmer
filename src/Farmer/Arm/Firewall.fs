[<AutoOpen>]
module Farmer.Arm.Firewall

open Farmer

// Further information on properties and examples: https://docs.microsoft.com/en-us/azure/templates/microsoft.network/azurefirewalls
let azureFirewalls = ResourceType ("Microsoft.Network/azureFirewalls", "2020-07-01")

// https://docs.microsoft.com/en-us/azure/templates/microsoft.network/firewallpolicies
let azureFirewallPolicies = ResourceType ("Microsoft.Network/firewallPolicies", "2020-07-01")

// https://docs.microsoft.com/en-us/azure/templates/microsoft.network/firewallpolicies/rulecollectiongroups
let azureFirewallPoliciesRuleCollectionGroups = ResourceType ("Microsoft.Network/firewallPolicies/ruleCollectionGroups", "2020-07-01")

[<RequireQualifiedAccess>]
type AzureFirewallSkuName =
    | AZFW_VNet
    | AZFW_Hub
    member this.ArmValue =
        match this with
        | AZFW_VNet -> "AZFW_VNet"
        | AZFW_Hub -> "AZFW_Hub"

[<RequireQualifiedAccess>]
type AzureFirewallSkuTier =
    | Standard
    | Premium
    member this.ArmValue =
        match this with
        | Standard -> "Standard"
        | Premium -> "Premium"

type AzureFirewallSku =
    {
        Name : AzureFirewallSkuName
        Tier : AzureFirewallSkuTier
    }

[<RequireQualifiedAccess>]
type ThreatIntelMode =
    | Alert
    | Deny
    | Off
    member this.ArmValue =
        match this with
        | Alert -> "Alert"
        | Deny -> "Deny"
        | Off -> "Off"

type FirewallPolicyThreatIntelWhitelist =
    {
        IPAddress : System.Net.IPAddress list
        FQDNs : string list
    }

[<RequireQualifiedAccess>]
type IPProtocol =
    | Any
    | ICMP
    | TCP
    | UDP
    member this.ArmValue =
        match this with
        | Any -> "Any"
        | ICMP -> "ICMP"
        | TCP -> "TCP"
        | UDP -> "UDP"

[<RequireQualifiedAccess>]
type FwPoliyRuleType =
    | NetworkRule
    | ApplicationRule
    member this.ArmValue =
        match this with
        | NetworkRule -> "NetworkRule"
        | ApplicationRule -> "ApplicationRule"

type DnsSettings =
    {
        Servers : string list
        EnableProxy : bool
        RequireProxyForNetworkRules : bool option
    }

type FirewallPolicyRuleCollectionGroupRules =
    {
        Name : string
        DestinationAddresses : string list
        DestinationIPGroups : string list
        DestinationFQDNs : string list
        DestinationPorts : string list
        IPProtocols : IPProtocol list
        RuleType : FwPoliyRuleType
        SourceAddresses : string list
        SourceIPGroups : string list
    }

[<RequireQualifiedAccessAttribute>]
type FirewallPolicyRuleCollectionGroupAction =
    | Allow
    | Deny
    member this.ArmValue =
        match this with
        | Allow -> "Allow"
        | Deny -> "Deny"

type FirewallPolicyRuleCollectionGroup =
    {
        Name : string
        Action : FirewallPolicyRuleCollectionGroupAction
        Priority : int
        RuleCollectionType : string
        Rules : FirewallPolicyRuleCollectionGroupRules list
    }

type Hole = Hole

type AzureFirewall =
    { /// It's recommended to use resource group + -azfw.
      /// e.g. "name": "[concat(resourceGroup().name,'-azfw')]"
      Name : ResourceName
      Zones : Hole  // array
      AdditionalProperties : Hole
      ApplicationRuleCollections : Hole
      NatRuleCollections : Hole
      NetworkRuleCollections : Hole
      IpConfigurations : Hole
      ManagementIpConfiguration : Hole
      ThreatIntelMode : ThreatIntelMode option
      FirewallPolicy : ResourceName
      HubIPAddressesCount : int
      AzureFirewallSku : AzureFirewallSku
      VHUB : ResourceName
    }
    interface IArmResource with
        member this.ResourceId = azureFirewalls.resourceId this.Name
        member this.JsonModel =
            let dependencies =
                [
                    virtualHubs.resourceId this.VHUB
                    azureFirewallPolicies.resourceId this.FirewallPolicy
                ]
            {| azureFirewalls.Create(this.Name, dependsOn = dependencies) with
                location = "[resourceGroup().location]"
                properties =
                  {| sku =
                      {| name = this.AzureFirewallSku.Name.ArmValue
                         tier = this.AzureFirewallSku.Tier.ArmValue
                      |}
                     firewallPolicy =
                      {| id = (azureFirewallPolicies.resourceId this.FirewallPolicy).ArmExpression.Eval() |}
                     hubIPAddresses =
                      {| publicIPs = {| count = 2 |}
                      |}
                     virtualHub =
                      {|
                        id = (virtualHubs.resourceId this.VHUB).ArmExpression.Eval()
                      |}
                |}
            |}:> _

type AzureFirewallPolicy =
    { /// It's recommended to use resource group + -fwpolicy.
      /// e.g. "name": "[concat(resourceGroup().name,'-fwpolicy')]"
      Name : ResourceName
      ThreatIntelMode : ThreatIntelMode
      ThreatIntelWhitelist : FirewallPolicyThreatIntelWhitelist
      DnsSettings : DnsSettings
      BasePolicy : ResourceId option
      IntrusionDetection : obj option   // Need to set up types for these objects
      TransportSecurity : obj option    // Need to set up types for these objects
      FirewallPolicySku : obj option    // Need to set up types for these objects
      VHUB : ResourceName
    }
    interface IArmResource with
        member this.ResourceId = azureFirewallPolicies.resourceId this.Name
        member this.JsonModel =
            let dependencies = [virtualHubs.resourceId this.VHUB]
            {| azureFirewallPolicies.Create(this.Name, dependsOn = dependencies) with
                location = "[resourceGroup().location]"
                properties =
                    {|
                       basePolicy = this.BasePolicy |> Option.defaultValue Unchecked.defaultof<ResourceId>
                       threatIntelMode = this.ThreatIntelMode.ArmValue
                       threatIntelWhitelist =
                         {| ipAddresses = this.ThreatIntelWhitelist.IPAddress
                            fqdns = this.ThreatIntelWhitelist.FQDNs
                         |}
                       dnsSettings =
                         {| servers = this.DnsSettings.Servers
                            enableProxy = this.DnsSettings.EnableProxy
                            requireProxyForNetworkRules = this.DnsSettings.RequireProxyForNetworkRules |> Option.map box |> Option.defaultValue null
                         |}
                       intrusionDetection = this.IntrusionDetection |> Option.defaultValue Unchecked.defaultof<obj>
                       transportSecurity = this.TransportSecurity |> Option.defaultValue Unchecked.defaultof<obj>
                       sku = this.FirewallPolicySku |> Option.defaultValue Unchecked.defaultof<obj>
                    |}
            |}:> _

type AzureFirewallPolicyRuleCollectionGroup =
    {
      FirewallPolicy : ResourceName
      RuleCollections : FirewallPolicyRuleCollectionGroup list
      Priority : int
      AZFW : ResourceName
    }
    interface IArmResource with
        member this.ResourceId = azureFirewallPoliciesRuleCollectionGroups.resourceId (ResourceName ($"{this.FirewallPolicy}/DefaultNetworkRuleCollectionGroup"))
        member this.JsonModel =
            let dependencies = [azureFirewalls.resourceId this.AZFW]
            {| azureFirewallPoliciesRuleCollectionGroups.Create ((ResourceName $"{this.FirewallPolicy.Value}/DefaultNetworkRuleCollectionGroup"), dependsOn = dependencies) with
                location = "[resourceGroup().location]"
                properties =
                    {|
                        priority = this.Priority
                        ruleCollections =
                            this.RuleCollections |> List.map (fun ruleCollection ->
                            {|
                                ruleCollectionType = ruleCollection.RuleCollectionType
                                action = {| ``type``= ruleCollection.Action.ArmValue |}
                                rules = ruleCollection.Rules |> List.map (fun rule ->
                                    {|
                                        ruleType = rule.RuleType.ArmValue
                                        name = rule.Name
                                        ipProtocols = rule.IPProtocols |> List.map (fun p -> p.ArmValue)
                                        sourceAddresses = rule.SourceAddresses
                                        sourceIpGroups = rule.SourceIPGroups
                                        destinationAddresses = rule.DestinationAddresses
                                        destinationIpGroups = rule.DestinationIPGroups
                                        destinationFqdns = rule.DestinationFQDNs
                                        destinationPorts = rule.DestinationPorts
                                    |})
                                name = ruleCollection.Name
                                priority = ruleCollection.Priority
                            |})
                    |}
            |}:> _

---
title: "App Insights - Availability Tests"
date: 2021-08-06T07:00:00+01:00
weight: 1
chapter: false
---

#### Overview
The App Insights - Availability Tests builder is used to create Application Insights Availability Tests. You will need an Application Insights instance to run the tests.
The tests can be just pinging the website and expecting response code of 200, or they can be recored Visual Studio WebTests as custom XML strings.

* Application Insights (`Microsoft.Insights/webtests`)

#### Builder Keywords

| Keyword | Purpose |
|-|-|
| name | Sets the name of this Webtest instance. |
| link_to_app_insights | Name or resource of the App Insight instance. |
| web_test | AvailabilityTest.WebsiteUrl Uri to website, or AvailabilityTest.CustomWebtestXml string |
| locations | List of locations where the site is pinged. These are not format of Farmer.Location but AvailabilityTest.TestSiteLocation.  |
| timeout | Timeout if the test is not responding. Default: 120 seconds. |
| frequency | Frequency how often the test is run. Default: 900 seconds. |

#### Example

```fsharp
open Farmer
open Farmer.Builders

let ai = appInsights { name "ai" }
let myAvailabilityTest =
    availabilityTest {
        name "avTest"
        link_to_app_insights ai
        timeout 60<Seconds>
        frequency 800<Seconds>
        locations [ 
            AvailabilityTest.TestSiteLocation.NorthEurope
            AvailabilityTest.TestSiteLocation.WestEurope
            AvailabilityTest.TestSiteLocation.CentralUS
            AvailabilityTest.TestSiteLocation.UKSouth
        ]
        web_test (
            "https://mywebsite.com" 
            |> System.Uri 
            |> AvailabilityTest.WebsiteUrl)
    }
```
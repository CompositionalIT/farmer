---
title: "Contributing"
date: 2020-06-15T03:57:42+02:00
draft: false
chapter: false
---

Thanks for thinking about contributing! Azure is a giant beast and help supporting more use-cases is always appreciated. To make it easier to contribute, we put together this little guide. Please take a few minutes to read through before starting work on a pull request (PR) to Farmer.

### The process (don't worry... this is not waterfall)
1. Open an issue, or comment on an existing open issue covering the resource you would like to work on. Basically, a PR from you should not come as a surprise.
1. Implement the 20% of features that cover 80% of the use cases.
1. PR against the `master` branch from your *fork*.
1. Add/update tests as required.
1. Create a new **.md* file with the name of your resource in the folder **/content/api-overview/resources/**. Eg. **container-registry.md**
1. Add a description, keywords, and an example to the docs page.
1. PRs need to pass build/test against both Linux & Windows build, and a review, before being merged in.

### TODO
There's still more to document!

* Validation
* Outputs
* Dependencies
* Secure parameters
* Multiple resource builders
* Linking resources (one-to-many relationships)
* Post-deploy tasks
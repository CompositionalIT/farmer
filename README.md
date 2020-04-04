# Farmer Docs

## How this works

Farmer's docs use [Hugo](https://gohugo.io/) and the [hugo-theme-learn theme](https://github.com/matcornic/hugo-theme-learn).

## To build these docs locally

* Install Go language support and hugo (if on Windows, we recommend using [Chocolatey](https://chocolatey.org/) and running `choco install golang hugo`)
* The theme is in a sub-module, so you'll also want to run `git submodule update --init`
* Install the theme: `cd themes/` and then `git clone https://github.com/matcornic/hugo-theme-learn.git`
* To build, run `hugo` from the root. To serve a local copy, run `hugo server`.
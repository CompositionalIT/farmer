# Farmer Docs

## About the Documentation Platform

Farmer's docs use [Hugo](https://gohugo.io/) and the [hugo-theme-learn theme](https://github.com/matcornic/hugo-theme-learn).

## To build these docs locally

* Install Go language support and hugo (if on Windows, we recommend using [Chocolatey](https://chocolatey.org/) and running `choco install golang hugo`)
* The theme is in a sub-module, so you'll also want to run `git submodule update --init`
* Install the theme: `cd themes/` and then `git clone https://github.com/matcornic/hugo-theme-learn.git`
* To build, run `hugo` from the root. To serve a local copy, run `hugo server`.

## Publishing these Docs

These docs use [GitHub Actions](https://github.com/features/actions) and the [Actions-hugo](https://github.com/peaceiris/actions-hugo) tooling to publish the contents to [GitHub pages](https://pages.github.com/)

How it works:

* A change is committed to the `docs` branch (say, when a PR is merged).
* The GitHub Actions workflow begins. 
* The action pulls the docs branch, runs hugo, and then publishes the "public" folder output to the `gh-pages` branch.
* The `gh-pages` branch is served by GitHub Pages at https://compositionalit.github.io/farmer.

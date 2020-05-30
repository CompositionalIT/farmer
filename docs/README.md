# Farmer Docs

## About the Documentation Platform

Farmer's docs use [Hugo](https://gohugo.io/) and the 
[hugo-theme-learn theme](https://github.com/compositionalit/hugo-theme-learn).

## To build these docs locally

* Install [Go language](https://golang.org/) support and 
[Hugo](https://gohugo.io/) (if on Windows, we recommend using 
[Chocolatey](https://chocolatey.org/) and running `choco install golang hugo`)
  
  *Note*: there is currently a problem with the newest version of Hugo and the 
  theme, so use version 0.68.3 of Hugo, otherwise you'll get a compilation error 
* The theme is in a sub-module, so you'll also want to run 
  `git submodule update --init` and then `cd docs/themes` followed by 
  `git clone https://github.com/compositionalit/hugo-theme-learn.git`
* To build, run `hugo --minify` from the `docs` folder. 
  To serve a local copy, run `hugo server`.

## Publishing these Docs

These docs use [GitHub Actions](https://github.com/features/actions) and the 
[Actions-hugo](https://github.com/peaceiris/actions-hugo) tooling to publish 
the contents to [GitHub pages](https://pages.github.com/)

How it works:

* A change is committed to the `master` branch (say, when a PR is merged).
* The GitHub Actions workflow begins.
* The action runs hugo against the `docs` folder, and then publishes the 
  `public` folder output to the `gh-pages` branch.
* The `gh-pages` branch is served by GitHub Pages at 
  https://compositionalit.github.io/farmer.

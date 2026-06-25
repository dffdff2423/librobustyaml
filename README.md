# Librobustyaml

Project for making tooling around RobustToolbox yaml files. Originally going to be part of my LSP server but was separated out
into a separate project so other people can use it. Right now still a work on progress.

Unlike other projects that deal with parsing Robust Yaml this directly loads and executes code from the RT assemblies.
This allows us to be compatible with as many forks as possible and in most contexts that you would want to parse yaml
you trust the codebase with ACE already or you can run it in a CI box or some other sandboxed environment.

This repo is also home to the yamldocs project which is a WIP website for hosting API documentation of RT
components, prototypes, etc.

This project follows the REUSE spec for licensing.

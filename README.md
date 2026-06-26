# Librobustyaml

Project for making tooling around RobustToolbox yaml files. Originally going to be part of my LSP server but was separated out
into a separate project so other people can use it. Right now still a work on progress.

Unlike other projects that deal with parsing Robust Yaml this directly loads and executes code from the RT assemblies.
This allows us to be compatible with as many forks as possible. I don't think this is a security flaw since in basically 
all cases you would want to parse RT yaml you already trust the codebase you are parsing from with access to your system.

For this library to be able to pull in prototype, component, datadef docs you need to build the targeted project in the following way:
```sh
dotnet build -p:GenerateDocumentationFile=true
```

## Yamldocs
This repo is also home to the yamldocs generator  which is a WIP static site generator for RT yaml API docs.

## Copyright
This project follows the REUSE spec for licensing. Most stuff is GPL-3.0-only but some code copied from RT is MIT. Refer 
to the copyright info in the file or REUSE.toml
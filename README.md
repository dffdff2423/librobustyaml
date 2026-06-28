# Librobustyaml

Project for making tooling around RobustToolbox yaml files. Originally going to be part of my LSP server but was separated out
into a separate project so other people can use it. Right now still a work on progress and is only capable of extracting
RT types but does not have tools for loading yaml files.

Unlike other projects that deal with parsing Robust Yaml this directly loads and executes code from the RT assemblies.
This allows us to be compatible with as many forks as possible. I don't think this is a security flaw since in basically
all cases you would want to parse RT yaml you already trust the codebase you are parsing from with access to your system.

For this library to be able to pull in docs you need to build the targeted project in the following way:
```sh
dotnet build -p:GenerateDocumentationFile=true
```
You may want to disable warnings with `-p:WarningLevel=0` since the above command produces tens of thousands of missing
documentation warnings.

## Yamldocs
This repo is also home to the yamldocs generator which is a WIP static site generator for RT yaml API docs. You can
view it at [yamldocs.notaslug.org](https://yamldocs.notaslug.org). If you would like your server added or want to report
an issue feel free to send me an email or open an issue here.

## Contributing
Patches are accepted via github PR and email.

This project follows a code style that is non-standard for c# projects but commonly used in embedded development.

You should try to avoid combining code and data and avoid "object oriented programming". Define types separately and use records when they make sense. Try to keep business logic inside of static methods and write in a procedural style. Prefer long functions to many short ones since it is easier to understand the logic of a single large function than trace the control flow of many short functions. Use comments or c# regions if you feel the need to spit stuff up. It is okay to write traditional classes if you have a good reason to do so but they should not be the norm. It is also encouraged to define multiple types inside of the same file. Prefer long files to short files up to a reasonable point since switching between files in an editor is more key presses than doing a ctrl+f style search.

Use linq and functional programming techniques when they make code more understandable. Optimize for readability over performance in non-critical paths.

If you are editing existing code match the style rather than creating needless refactoring noise.

I don't claim this is the best way to organize a codebase, but it is what I strongly prefer.

For syntax, see .editorconfig, if you use rider it should take care of that automatically.
## Copyright
This project follows the REUSE spec for licensing. Most stuff is GPL-3.0-only but some code copied from RT is MIT. Refer
to the copyright info in the file or REUSE.toml

If you are contributing a new file that you wrote you can choose between the two licenses.

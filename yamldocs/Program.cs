// SPDX-FileCopyrightText: (C) 2026 dffdff2423 <dffdff2423@gmail.com>
//
// SPDX-License-Identifier: GPL-3.0-only

using System.Text.Encodings.Web;
using System.Text.Json;

using YamlWarrior.Robust;
using YamlWarrior.Robust.TypeLoading;

namespace Yamldocs;

internal static class Program {
    private static void Main(string[] argv) {
        var flags = new[] {
            new Flag(Type: ArgumentType.Bool, Long: "dump", HelpText: "Dump info extracted from assemblies as JSON to the CLI then exit."),
            new Flag(Type: ArgumentType.String, Long: "assembly-dir", Short: 'a', HelpText: "Path to an ss14 build directory"),
            new Flag(Type: ArgumentType.String, Long: "output", Short: 'o', HelpText: "Path to output doc site (required if no --dump)"),
            new Flag(Type: ArgumentType.String, Long: "gh-slug", Short: 'g', HelpText: "GitHub slug for the fork (optional)"),
            new Flag(Type: ArgumentType.String, Long: "commit", Short: 'c', HelpText: "Commit the docs are generated for"),
        };
        var opts = ArgumentParser.ParseArguments(flags, "-a <assembly> [options...]", argv);

        if (!opts.ContainsKey("assembly-dir")) {
            Console.Error.WriteLine("Must specify a build directory with -a or --assembly-dir");
            Environment.Exit(1);
        }
        if (!opts.ContainsKey("output") && !opts.ContainsKey("dump")) {
            Console.Error.WriteLine("Must specify an output directory");
            Environment.Exit(1);
        }

        var build = (string)opts["assembly-dir"];

        var rtSharedPath = Path.Join(build, AssemblyNames.RobustSharedPath);
        var yamlCtx = new YamlProcessingContext(rtSharedPath);
        yamlCtx.LoadAllContent(build);

        if (opts.ContainsKey("dump")) {
            Console.WriteLine(JsonSerializer.Serialize(yamlCtx.RobustTypes, new JsonSerializerOptions {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            }));
            Environment.Exit(0);
        }

        var output = (string)opts["output"];
        SiteGenerator.Generate(new SiteGenerator.Parameters {
            GhSlug = opts.TryGetValue("gh-slug", out object? opt) ? (string)opt : null,
            GitCommit = opts.TryGetValue("commit", out object? opt2) ? (string)opt2 : null,
            OutputPath = output,
            Yaml = yamlCtx,
        });
    }
}

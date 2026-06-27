// SPDX-FileCopyrightText: (C) 2026 dffdff2423 <dffdff2423@gmail.com>
//
// SPDX-License-Identifier: GPL-3.0-only

using System.Text;
using System.Xml.Linq;

using Fluid;
using Fluid.Values;

using JetBrains.Annotations;

using Microsoft.Extensions.FileProviders;

using YamlWarrior.Robust;

namespace Yamldocs;

public static class SiteGenerator {
    public sealed record Parameters {
        public string? GhSlug { get; init; }

        public required string OutputPath { get; init; }

        public required YamlProcessingContext Types { get; init; }
    }

    private static readonly ManifestEmbeddedFileProvider TemplateProvider = new(typeof(SiteGenerator).Assembly, "templates");

    public static IFluidTemplate GetTemplate(FluidParser parser, string path) {
        using var reader = new StreamReader(TemplateProvider.GetFileInfo(path).CreateReadStream(), Encoding.UTF8);
        var readContents = reader.ReadToEnd();
        return parser.Parse(readContents);
    }

    public static void Generate(Parameters opts) {
        var parser = new FluidParser();
        var ctx = new TemplateContext {
            Options = {
                MemberAccessStrategy = new UnsafeMemberAccessStrategy(),
                FileProvider = TemplateProvider,
            },
        };
        ctx.Options.Filters.AddFilter("doc_summerize", DocSummerize);

        var generatorVersion = typeof(SiteGenerator).Assembly.GetName().Version;
        var rtYamlVersion = typeof(YamlProcessingContext).Assembly.GetName().Version;
        ctx.SetValue("generator_info", $"yamldocs v{generatorVersion}, librobustyaml v{rtYamlVersion}");
        ctx.SetValue("github_slug",  opts.GhSlug);

        var protos = opts.Types.RobustTypes.Prototypes.Keys.ToArray();
        protos.Sort();
        var dds = opts.Types.RobustTypes.DataDefinitions.Keys.ToArray();
        dds.Sort();
        var comps = opts.Types.RobustTypes.Components.Keys.ToArray();
        comps.Sort();

        ctx.SetValue("asm_types",
            new Dictionary<string, string[]> {
                ["Prototypes"] = protos,
                ["DataDefs"] = dds,
                ["Components"] = comps,
            });

        ctx.SetValue("type_info", opts.Types.RobustTypes);

        var index = GetTemplate(parser, "index.liquid");
        var indexTxt = index.Render(ctx);

        var output = opts.GhSlug != null ? Path.Combine(opts.OutputPath, opts.GhSlug) : opts.OutputPath;
        Directory.CreateDirectory(output);

        File.WriteAllText(Path.Combine(output, "index.html"), indexTxt);
    }

    private static ValueTask<FluidValue> DocSummerize(FluidValue input, FilterArguments filter, TemplateContext ctx) {
        if (input.IsNil())
            return EmptyValue.Instance;
        if (input.ToObjectValue() is not XElement el)
            throw new ArgumentException("Input must be documentation XElement", nameof(input));
        if (el.Name == "member") {
            return new StringValue(el.Element("summary")?.Value ?? "");
        }

        if (el.Name == "mergedDocs") {
            var outEl = new XElement("div");

            var client = el.Element("Client");
            if (client != null) {
                var sum = client.Element("summary");
                if (sum != null) {
                    outEl.Add(new XElement("h4", "Client"));
                    outEl.Add(sum.Value);
                }
            }

            var server = el.Element("Server");
            if (server != null) {
                var sum = server.Element("summary");
                if (sum != null) {
                    outEl.Add(new XElement("h4", "Server"));
                    outEl.Add(sum.Value);
                }
            }

            var shared =  el.Element("Shared");
            if (shared != null) {
                var sum = shared.Element("summary");
                if (sum != null) {
                    outEl.Add(new XElement("h4", "Shared"));
                    outEl.Add(shared.Value);
                }
            }

            if (outEl.Elements().Any()) {
                return new StringValue(outEl.ToString());
            }

            return EmptyValue.Instance;
        }

        throw new ArgumentException($"Input must be documentation XElement. Got {el}", nameof(input));
    }
}

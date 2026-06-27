// SPDX-FileCopyrightText: (C) 2026 dffdff2423 <dffdff2423@gmail.com>
//
// SPDX-License-Identifier: GPL-3.0-only

using System.Text;

using Fluid;

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
        var ctx = new TemplateContext();
        ctx.Options.MemberAccessStrategy = new UnsafeMemberAccessStrategy();
        ctx.Options.FileProvider = TemplateProvider;

        var generatorVersion = typeof(SiteGenerator).Assembly.GetName().Version;
        var rtYamlVersion = typeof(YamlProcessingContext).Assembly.GetName().Version;
        ctx.SetValue("generator_info", $"yamldocs v{generatorVersion}, librobustyaml v{rtYamlVersion}");
        ctx.SetValue("github_slug",  opts.GhSlug);

        var index = GetTemplate(parser, "index.liquid");
        var indexTxt = index.Render(ctx);

        var output = opts.GhSlug != null ? Path.Combine(opts.OutputPath, opts.GhSlug) : opts.OutputPath;
        Directory.CreateDirectory(output);

        File.WriteAllText(Path.Combine(output, "index.html"), indexTxt);
    }
}

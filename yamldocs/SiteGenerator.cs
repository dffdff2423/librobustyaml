// SPDX-FileCopyrightText: (C) 2026 dffdff2423 <dffdff2423@gmail.com>
//
// SPDX-License-Identifier: GPL-3.0-only

using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

using Fluid;
using Fluid.Values;

using JetBrains.Annotations;

using Microsoft.Extensions.FileProviders;

using YamlWarrior.Robust;
using YamlWarrior.Robust.Utilities;

namespace Yamldocs;

public static class SiteGenerator {
    public sealed record Parameters {
        public string? GhSlug { get; init; }

        public string? GitCommit { get; init; }

        public required string OutputPath { get; init; }

        public required YamlProcessingContext Yaml { get; init; }
    }

    private static readonly ManifestEmbeddedFileProvider ResourceProvider = new(typeof(SiteGenerator).Assembly, "res");

    public static IFluidTemplate GetTemplate(FluidParser parser, string path) {
        using var reader = new StreamReader(ResourceProvider.GetFileInfo(path).CreateReadStream(), Encoding.UTF8);
        var readContents = reader.ReadToEnd();
        return parser.Parse(readContents);
    }

    public static void Generate(Parameters opts) {
        var parser = new FluidParser(new FluidParserOptions {
            AllowFunctions = true,
        });
        var ctx = new TemplateContext {
            Options = {
                MemberAccessStrategy = new UnsafeMemberAccessStrategy(),
                FileProvider = ResourceProvider,
            },
        };
        ctx.Options.Filters.AddFilter("format_docs", FormatDocs);
        ctx.Options.Filters.AddFilter("format_type_name", FormatTypeName);

        var ver = typeof(SiteGenerator).Assembly.GetName().Version;
        if (ver != null) {
            var vertxt = $"v{ver.Major}.{ver.Minor}.{ver.Build}";
            ctx.SetValue("generator_info", $"yamldocs {vertxt}");
            ctx.SetValue("yamldocs_version", vertxt);
        }
        ctx.SetValue("github_slug",  opts.GhSlug);
        ctx.SetValue("git_commit", opts.GitCommit);

        var protos = opts.Yaml.RobustTypes.Prototypes.Keys.ToArray();
        protos.Sort();
        var dds = opts.Yaml.RobustTypes.DataDefinitions.Keys.ToArray();
        dds.Sort();
        var comps = opts.Yaml.RobustTypes.Components.Keys.ToArray();
        comps.Sort();
        var sers = opts.Yaml.RobustTypes.Serializables.Keys.ToArray();
        sers.Sort();

        ctx.SetValue("asm_types",
            new Dictionary<string, string[]> {
                ["Prototypes"] = protos,
                ["DataDefs"] = dds,
                ["Components"] = comps,
                ["Serializables"] = sers,
            });

        ctx.SetValue("type_info", opts.Yaml.RobustTypes);


        var output = opts.GhSlug != null ? Path.Combine(opts.OutputPath, opts.GhSlug) : opts.OutputPath;
        Directory.CreateDirectory(output);

        // Process single page templates
        var index = GetTemplate(parser, "index.liquid");
        var indexTxt = index.Render(ctx);
        File.WriteAllText(Path.Combine(output, "index.html"), indexTxt);

        var baseType = GetTemplate(parser, "basic-types.liquid");
        var baseTypeTxt = baseType.Render(ctx);
        File.WriteAllText(Path.Combine(output, "basic-types.html"), baseTypeTxt);

        var redirect =  GetTemplate(parser, "redirect.liquid");

        // Process type templates
        var proto = GetTemplate(parser, "prototype.liquid");
        foreach (var (kind, ty) in opts.Yaml.RobustTypes.Prototypes) {
            ctx.SetValue("curr_ty", ty);
            var txt = proto.Render(ctx);
            File.WriteAllText(Path.Combine(output, $"{kind}.html"), txt);

            ctx.SetValue("redirect_target", kind);
            var redirectTxt = redirect.Render(ctx);
            File.WriteAllText(Path.Combine(output, $"{ty.FullName}.html"), redirectTxt);
        }

        var comp = GetTemplate(parser, "component.liquid");
        foreach (var (yamlName, ty) in opts.Yaml.RobustTypes.Components) {
            if (ty.Unsaved)
                continue; // Yaml users should not care about unsaved comps

            ctx.SetValue("curr_ty", ty);
            var txt = comp.Render(ctx);
            File.WriteAllText(Path.Combine(output, $"{yamlName}.html"), txt);

            if (!ty.Predicted) {
                ctx.SetValue("redirect_target", yamlName);
                var redirectTxt = redirect.Render(ctx);
                File.WriteAllText(Path.Combine(output, $"{ty.FullName}.html"), redirectTxt);
            } else {
                if (ty.ClientFullName != null) {
                    ctx.SetValue("redirect_target", yamlName);
                    var redirectTxt = redirect.Render(ctx);
                    File.WriteAllText(Path.Combine(output, $"{ty.ClientFullName}.html"), redirectTxt);
                }

                if (ty.ServerFullName != null) {
                    ctx.SetValue("redirect_target", yamlName);
                    var redirectTxt = redirect.Render(ctx);
                    File.WriteAllText(Path.Combine(output, $"{ty.ServerFullName}.html"), redirectTxt);
                }

                if (ty.SharedFullName != null) {
                    ctx.SetValue("redirect_target", yamlName);
                    var redirectTxt = redirect.Render(ctx);
                    File.WriteAllText(Path.Combine(output, $"{ty.SharedFullName}.html"), redirectTxt);
                }
            }
        }

        var datadef = GetTemplate(parser, "datadef.liquid");
        foreach (var (path, ty) in opts.Yaml.RobustTypes.DataDefinitions) {
            ctx.SetValue("curr_ty", ty);
            var txt = datadef.Render(ctx);
            File.WriteAllText(Path.Combine(output, $"{path}.html"), txt);
        }

        var serializable = GetTemplate(parser, "serializable.liquid");
        foreach (var (path, ty) in opts.Yaml.RobustTypes.Serializables) {
            ctx.SetValue("curr_ty", ty);
            var txt = serializable.Render(ctx);
            File.WriteAllText(Path.Combine(output, $"{path}.html"), txt);
        }

        var jsonTxt = JsonSerializer.Serialize(opts.Yaml.RobustTypes, new JsonSerializerOptions {
                WriteIndented = true,
            });
        File.WriteAllText(Path.Combine(output, "types.json"), jsonTxt);

        // Copy non-templates to new site
        foreach (var f in ResourceProvider.GetDirectoryContents(".")) {
            if (Path.GetExtension(f.Name) == ".liquid") {
                continue;
            }
            using var os = File.Create(Path.Combine(output, f.Name));
            using var iss = f.CreateReadStream();
            iss.CopyTo(os);
        }
    }

    [Pure]
    private static ValueTask<FluidValue> FormatDocs(FluidValue input, FilterArguments filter, TemplateContext ctx) {
        if (input.IsNil())
            return EmptyValue.Instance;
        if (input.ToObjectValue() is not XElement el)
            throw new ArgumentException("Input must be XElement", nameof(input));
        if (el.Name == "member") {
            return  new StringValue(HtmlifyCsharpDocComment(el).ToString());
        }

        if (el.Name == "mergedDocs") {
            var outEl = new XElement("div");

            var client = el.Element("Client");
            if (client != null) {
                var sum = client.Element("summary");
                if (sum != null) {
                    outEl.Add(new XElement("h4", "Client"));
                    outEl.Add(HtmlifyCsharpDocComment(sum));
                }
            }

            var server = el.Element("Server");
            if (server != null) {
                var sum = server.Element("summary");
                if (sum != null) {
                    outEl.Add(new XElement("h4", "Server"));
                    outEl.Add(HtmlifyCsharpDocComment(sum));
                }
            }

            var shared =  el.Element("Shared");
            if (shared != null) {
                var sum = shared.Element("summary");
                if (sum != null) {
                    outEl.Add(new XElement("h4", "Shared"));
                    outEl.Add(HtmlifyCsharpDocComment(sum));
                }
            }

            if (outEl.Elements().Any()) {
                return new StringValue(outEl.ToString());
            }

            return EmptyValue.Instance;
        }

        throw new ArgumentException($"Input must be documentation XElement. Got {el}", nameof(input));
    }

    private static XElement HtmlifyCsharpDocComment(XElement el) {
        return new XElement("div",
            el
                .Nodes()
                .Select(node =>
                    node is XElement child ? HtmlifyElement(child) : node));
    }

    private static XNode HtmlifyElement(XElement el) {
        // All ye who enter here abandon hope. Nobody formats their documentation comments correctly so we just map them
        // to something that should look somewhat okay in HTML and call it good enough.
        switch (el.Name.LocalName) {
        case "b":
        case "i":
        case "u":
        case "br":
        case "pre": // I don't think this one actually is allowed but wizden uses it anyways
            return el;
        case "c":
            return new XElement("span", new XAttribute("class", "inlinecode"), HtmlifyNodes(el.Nodes()));
        case "para":
            return new XElement("p", HtmlifyNodes(el.Nodes()));
        case "summary":
            return new XElement("div", HtmlifyNodes(el.Nodes()));
        case "example":
            return new XElement("div",
                new XElement("h4", "Example"),
                HtmlifyNodes(el.Nodes()));
        case "remarks":
            return new XElement("div",
                new XElement("h4", "Remarks"),
                HtmlifyNodes(el.Nodes()));
        case "seealso":
            var tyalso = el.Attribute("cref");
            // if (tyalso == null) {
            //     return new XElement("em", "MISSING CREF");
            // }
            //return new XElement("div", "See Also: ", new XElement("a", new XAttribute("href", HtmlifySeeCref(tyalso!.Value)), tyalso.Value));
            return new XElement("div", "See Also: ", new XElement("span", new XAttribute("class", "inlinecode"), tyalso!.Value));
        case "see":
            var cref = el.Attribute("cref");
            if (cref != null) { // we don't support link
                return new XElement("span", new XAttribute("class", "inlinecode"), cref.Value);
            }
            var href = el.Attribute("href");
            if (href != null) { // we don't support link
                return new XElement("a", new XAttribute("href", href), href.Value);
            }
            var langword  = el.Attribute("langword");
            if (langword != null) {
                return new XElement("a", new XAttribute("langword", "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/"), langword.Value);
            }
            return new XElement("em", "MISSING CREF");
        case "typeparam":
            var name = el.Attribute("name");
            if (name != null) {
                return new XElement("div", "Type Parameter: ", new XElement("strong", name.Value), " - ", HtmlifyNodes(el.Nodes()));
            }

            return new XElement("div", "Type Parameter: ", HtmlifyNodes(el.Nodes()));
        case "returns":
            return new XElement("div", "Returns: ", HtmlifyNodes(el.Nodes()));
        case "value":
            return new XElement("div", "Value: ", HtmlifyNodes(el.Nodes()));
        case "param":
            var pname = el.Attribute("name");
            if (pname != null) {
                return new XElement("div", "Parameter: ", new XElement("strong", pname.Value), " - ", HtmlifyNodes(el.Nodes()));
            }

            return new XElement("div", "Parameter: ", HtmlifyNodes(el.Nodes()));
        case "list":
            return new XElement("ul", HtmlifyNodes(el.Nodes()));
        case "item":
            return new XElement("li", HtmlifyNodes(el.Nodes()));
        case "description":
            return new XText(el.Value);
        case "code":
            return new XElement("pre", new XAttribute("class", "boxed"), HtmlifyNodes(el.Nodes()));
        case "inheridoc": // This is something incorrect that wizden uses at least once. Probably should send a PR to fix it.
        case "inheritdoc":
            return new XElement("div", "Documentation inherited from superclass. The generator does not support this right now.");
        default:
            // I only bothered to implment the elements RT actually uses
            throw new NotSupportedException($"Input {el.Name} is not supported. This is probably an issue in yamldocs/librobustyaml");
        }
    }

    private static List<XNode> HtmlifyNodes(IEnumerable<XNode> nodes) {
        var result = new List<XNode>();
        foreach (var node in nodes) {
            if (node is XElement el) {
                result.Add(HtmlifyElement(el));
            } else {
                result.Add(node);
            }
        }
        return result;
    }

    [Pure]
    private static ValueTask<FluidValue> FormatTypeName(FluidValue input, FilterArguments filter, TemplateContext ctx) {
        var istr = input.ToStringValue();
        if (istr == null)
            throw new ArgumentException("Input must be a string", nameof(input));
        var ty = new CSharpTypeName(istr);

        return new StringValue(FormatTypeNameInternal(ty));
    }

    [Pure] private static string FormatTypeNameInternal(CSharpTypeName ty) {
        var tyPathUrl = $"./{WebUtility.UrlEncode(ty.TypePath)}.html";
        var tyPathHtml = WebUtility.HtmlEncode(ty.TypePath);

        // Special cases
        switch (ty.TypePath) {
        case "System.String":
            tyPathHtml = "string";
            tyPathUrl = "./basic-types.html#string";
            break;
        case "System.Int64":
        case "System.Int32":
        case "System.Int16":
            tyPathHtml = "integer" + ty.TypePath[^2..];
            tyPathUrl = "./basic-types.html#integer";
            break;
        case "System.UInt64":
        case "System.UInt32":
        case "System.UInt16":
            tyPathHtml = "uinteger" + ty.TypePath[^2..];
            tyPathUrl = "./basic-types.html#integer";
            break;
        case "System.SByte":
            tyPathHtml = "integer8";
            tyPathUrl = "./basic-types.html#integer";
            break;
        case "System.Byte":
            tyPathHtml = "uinteger8";
            tyPathUrl = "./basic-types.html#integer";
            break;
        case "System.Boolean":
            tyPathHtml = "bool";
            tyPathUrl = "./basic-types.html#bool";
            break;
        case "System.Single":
        case "System.Double":
            tyPathHtml = "real";
            tyPathUrl = "./basic-types.html#real";
            break;
        case "System.Numerics.Vector2":
        case "System.Numerics.Vector3":
        case "System.Numerics.Vector4":
            tyPathHtml = "Vector" + ty.TypePath[^1..];
            tyPathUrl = "./basic-types.html#vector";
            break;
        case "Content.Shared.FixedPoint.FixedPoint2":
        case "Content.Shared.FixedPoint.FixedPoint4":
            tyPathHtml = "FixedPoint" + ty.TypePath[^1..];
            tyPathUrl = "./basic-types.html#real";
            break;
        case "System.Collections.Generic.Dictionary":
            tyPathHtml = "Dictionary";
            tyPathUrl = "./basic-types.html#dict";
            break;
        case "System.Collections.Generic.HashSet":
            tyPathHtml = "Set";
            tyPathUrl = "./basic-types.html#array"; // Not sure if we should just show this as an array
            break;
        case "System.Collections.Generic.List":
        case "System.Collections.Generic.IReadOnlyCollection":
            // We do a bit of lying
            return FormatTypeNameInternal(ty.GenericParameters![0]) + "[]";
        case "System.Nullable":
            return FormatTypeNameInternal(ty.GenericParameters![0]) + "?";
        case "System.ValueTuple":
            var vtsb = new StringBuilder();
            vtsb.Append('(');
            var first = true;
            foreach (var gen in ty.GenericParameters!) {
                if (!first) {
                    vtsb.Append(", ");
                }
                vtsb.Append(FormatTypeNameInternal(gen));

                first = false;
            }

            vtsb.Append(')');
            return vtsb.ToString();
        case "System.TimeSpan":
            tyPathHtml = "TimeSpan";
            tyPathUrl = "./basic-types.html#string"; // TODO: Figure out what RT accepts for TimeSpans. Link to string for now
            break;
        case "Robust.Shared.Prototypes.ComponentRegistry":
            tyPathHtml = "IComponent[]";
            tyPathUrl = "./basic-types.html#component-registry";
            break;
        }

        var genNum = ty.NumGenerics != 0 ? $"`{ty.NumGenerics}" : string.Empty;

        var genVal = ty.GenericParameters
            ?.Select(FormatTypeNameInternal)
            .Aggregate(string.Empty, (s0, s1) =>  s1 + ", " + s0)
            ?? string.Empty;
        if (genVal != string.Empty) {
            genVal = $"&lt;{genVal[..^2]}&gt;";
            genNum = string.Empty;
        }

        if (genVal != string.Empty) {
            Debug.Write("Break me");
        }

        var array = ty.IsArray ? "[]" : string.Empty;

        return $"<a href=\"{tyPathUrl}\" >{tyPathHtml}</a>{genNum}{genVal}{array}";
    }
}

using System.Text.Json;

using YamlWarrior.Robust;
using YamlWarrior.Robust.Assemblies;

namespace Yamldocs;

internal static class Program {
    private static void Main(string[] argv) {
        var flags = new[] {
            new Flag(Type: ArgumentType.Bool, Long: "dump", HelpText: "Dump info extracted from assemblies as JSON then exit."),
            new Flag(Type: ArgumentType.String, Long: "assembly-dir", Short: 'a', HelpText: "Path to an ss14 build directory"),
        };
        var opts = ArgumentParser.ParseArguments(flags, "-a <assembly> [options...]", argv);

        if (!opts.ContainsKey("assembly-dir")) {
            Console.Error.WriteLine("Must specify a build directory with -a or --assembly-dir");
            Environment.Exit(1);
        }

        var build = (string)opts["assembly-dir"];

        var rtSharedPath = Path.Join(build, AssemblyNames.RobustSharedPath);
        var yamlCtx = new YamlProcessingContext(rtSharedPath);
        yamlCtx.LoadAllContent(build);

        if ((bool)opts["dump"]) {
            Console.WriteLine(JsonSerializer.Serialize(yamlCtx.RobustTypes, new JsonSerializerOptions { WriteIndented = true }));
            Environment.Exit(0);
        }
    }
}

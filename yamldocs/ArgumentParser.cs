// SPDX-FileCopyrightText: (C) 2026 dffdff2423 <dffdff2423@gmail.com>
//
// SPDX-License-Identifier: GPL-3.0-only

using System.Diagnostics;

using JetBrains.Annotations;

namespace Yamldocs;

/// <summary>
/// A "simple" argument parser library. Every single c# arg parser I found was incredibly bloated.
/// </summary>
[PublicAPI]
public static class ArgumentParser {
    /// <summary>
    /// Parse the provided command-line arguments. Automatically handles --help and -h
    /// </summary>
    /// <returns>Dictionary from Flag.Long to the parsed result. Missing flags are absent from the dict. Safe to cast to the expected value</returns>
    public static IReadOnlyDictionary<string, object> ParseArguments(Flag[] flags, string usage, string[] argv) {
        var output = new Dictionary<string, object>();

        for (int i = 0; i < argv.Length; ++i) {
            var arg = argv[i].ToCharArray();
            if (arg[0] != '-' || arg.Length < 2) {
                Console.Error.WriteLine($"Invalid argument: {argv[i]}");
                Environment.Exit(1);
            }

            // Short flags
            if (arg[1] != '-') {
                var selected = arg[1..];
                // Yes, I know this is O(n^2) but who cares
                foreach (char c in selected) {
                    if (c == 'h') {
                        PrintHelp(flags, usage);
                        continue;
                    }

                    foreach (var flag in flags) {
                        if (c != flag.Short)
                            continue;

                        if (flag.Type == ArgumentType.Bool) {
                            output[flag.Long] = true;
                            continue;
                        }

                        if (output.ContainsKey(flag.Long)) {
                            Console.Error.WriteLine($"Argument repeated: {flag.Long}");
                            Environment.Exit(1);
                        }
                        ArgumentAvailableOrDie(i + 1, argv);
                        i++;
                        output[flag.Long] = ParseArgument(flag, argv[i]);
                    }
                }
            } else { // long flags
                var selected = new string(arg[2..]);
                if (selected == "help") {
                    PrintHelp(flags, usage);
                    continue;
                }

                foreach (var flag in flags) {
                    if (selected != flag.Long)
                        continue;

                    if (flag.Type == ArgumentType.Bool) {
                        output[flag.Long] = true;
                        continue;
                    }

                    if (output.ContainsKey(flag.Long)) {
                        Console.Error.WriteLine($"Argument repeated: {flag.Long}");
                        Environment.Exit(1);
                    }
                    ArgumentAvailableOrDie(i + 1, argv);
                    i++;
                    output[flag.Long] = ParseArgument(flag, argv[i]);
                }
            }
        }

        return output;
    }


    private static void ArgumentAvailableOrDie(int i, string[] argv) {
        if (i >= argv.Length) {
            Console.Error.WriteLine("Expected argument after last flag.");
            Environment.Exit(1);
        }
    }

    [Pure]
    private static object ParseArgument(Flag f, string argument) {
        try {
            return f.Type switch {
                ArgumentType.Bool => throw new ArgumentOutOfRangeException(nameof(f.Type),
                    "Bool should already be handled before this is called"),
                ArgumentType.String => argument,
                ArgumentType.Int32 => int.Parse(argument),
                _ => throw new ArgumentOutOfRangeException(nameof(f.Type), f.Type, null),
            };
        } catch (FormatException) {
            Console.Error.WriteLine($"Flag `{f.Long}` expected argument which is a valid {f.Type.ToString()}");
            Environment.Exit(1);
        } catch (OverflowException) {
            Console.Error.WriteLine($"Flag `{f.Long}` expected argument which is a valid {f.Type.ToString()}");
            Environment.Exit(1);
        }

        throw new UnreachableException();
    }

    private static void PrintHelp(Flag[] flags, string usage) {
        // c# argv from main does not contain the program name. Grab it here. We still want to take argv so this can be
        // unit tested at some point)
        var argv0 = Environment.GetCommandLineArgs()[0];
        Console.WriteLine("USAGE:");
        Console.WriteLine($"\t{usage}");
        Console.WriteLine();
        Console.WriteLine("OPTIONS:");
        Console.WriteLine("\t-h, --help\t\tDisplay this information");

        foreach (var flag in flags.OrderBy(f => f.Long)) {
            Console.Write('\t');
            if (flag.Short != null)
                Console.Write($"-{flag.Short}, ");
            Console.WriteLine($"--{flag.Long,-16}\t{flag.HelpText}");
        }
        Environment.Exit(0);
    }
}

/// <summary>
/// Type of argument
/// </summary>
[PublicAPI]
public enum ArgumentType {
    /// <summary>
    /// Switches, eg. the `-a` in `ls -a`
    /// </summary>
    Bool,
    /// <summary>
    /// Takes a string argument
    /// </summary>
    String,
    /// <summary>
    /// Takes and integer argument
    /// </summary>
    Int32,
}

/// <summary>
/// Command-line argument.
/// </summary>
[PublicAPI]
public sealed record Flag(
    ArgumentType Type,
    string Long,
    string HelpText,
    char? Short = null
    );

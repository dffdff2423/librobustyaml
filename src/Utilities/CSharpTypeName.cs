// SPDX-FileCopyrightText: (C) 2026 dffdff2423 <dffdff2423@gmail.com>
//
// SPDX-License-Identifier: GPL-3.0-only

using System.Text.RegularExpressions;

using JetBrains.Annotations;

namespace YamlWarrior.Robust.Utilities;

/// <summary>
/// A class that can parse most types in the "FullName" field of Type Infos. Primerally intended for making nice looking user-visable output.
/// Probably not perfect but good enough for most purposes.
/// </summary>
[PublicAPI]
public sealed partial record CSharpTypeName {
    /// <summary>
    /// The path to the type
    /// </summary>
    public string TypePath { get; }

    /// <summary>
    /// The number of generics the type accepts. Zero if not generic
    /// </summary>
    public int NumGenerics { get; }

    /// <summary>
    /// If this is an array type
    /// </summary>
    public bool IsArray { get; }

    /// <summary>
    /// Non-null if this is a parameterized generic. Note for generics without filled parameters this value is null.
    /// </summary>
    public CSharpTypeName[]? GenericParameters { get; }

    [GeneratedRegex(@"\[([^,\]]+)")]
    private static partial Regex GenericParsingRegex { get; }

    public CSharpTypeName(string type) {
        var bracketSplit = type.Split('[', count: 2);
        TypePath = bracketSplit[0];

        // ugly hack to clean up normal types
        if (TypePath[^2] == '`') {
            TypePath = TypePath[..^2];
        }

        var genericSplit = type.Split('`', count: 2);

        if (type.EndsWith("[]")) {
            IsArray = true;
        }
        if (genericSplit.Length > 1) {
            NumGenerics = ParseLeadingNumber(genericSplit[1], out var rest);
            if (rest == null) return;

            rest = bracketSplit[1];
            if (!rest.StartsWith("[")) {
                throw new ArgumentException("Invalid type string",  nameof(type));
            }

            GenericParameters = GenericParsingRegex.Matches(rest)
                .Select(m => m.Groups[1].Value)
                .Select(m => new CSharpTypeName(m))
                .ToArray();
        }
    }

    [Pure]
    private static int ParseLeadingNumber(string input, out string? rest) {
        int i = 0;
        while (i < input.Length && char.IsDigit(input[i]))
            ++i;

        if (i == 0)
            throw new ArgumentException("Input does not start with a number", nameof(input));

        int number = int.Parse(input[..i]);
        rest = input[i..];
        if (rest.Length == 0) {
            rest = null;
        }

        return number;
    }
}

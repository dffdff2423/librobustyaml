// SPDX-FileCopyrightText: (C) 2026 dffdff2423 <dffdff2423@gmail.com>
//
// SPDX-License-Identifier: GPL-3.0-only

namespace YamlWarrior.Robust.TypeInfo;

public sealed record AssemblyTypes {
    /// <summary>
    /// Map of KindId to Prototype
    /// </summary>
    public Dictionary<string, PrototypeInfo> Prototypes { get; init; } = new();

    /// <summary>
    /// Mapping of type path to DataDefinitions or DataRecords. We treat those the same here.
    /// </summary>
    public Dictionary<string, DataDefinitionInfo> DataDefinitions { get; init; } = new();

    /// <summary>
    /// Mapping of YamlName to Components.
    /// </summary>
    public Dictionary<string, ComponentInfo> Components { get; init; } = new();

    public static AssemblyTypes Merge(AssemblyTypes lhs, AssemblyTypes rhs) {
        var joined = new AssemblyTypes {
            Prototypes = new Dictionary<string, PrototypeInfo>(lhs.Prototypes),
            DataDefinitions = new Dictionary<string, DataDefinitionInfo>(lhs.DataDefinitions),
            Components = new Dictionary<string, ComponentInfo>(lhs.Components),
        };

        foreach (var (kind, proto) in rhs.Prototypes) {
            if (!joined.Prototypes.TryAdd(kind, proto)) {
                throw new Exception("Duplicate kind: " + kind);
            }
        }

        foreach (var (kind, dd) in rhs.DataDefinitions) {
            if (!joined.DataDefinitions.TryAdd(kind, dd)) {
                throw new Exception("Duplicate kind: " + kind);
            }
        }

        foreach (var (kind, comp) in rhs.Components) {
            if (!joined.Components.TryAdd(kind, comp)) {
                // TODO: Support predicted components
                throw new Exception("Duplicate kind: " + kind);
            }
        }

        return joined;
    }
}

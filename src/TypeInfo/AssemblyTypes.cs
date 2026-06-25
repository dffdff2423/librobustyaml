// SPDX-FileCopyrightText: (C) 2026 dffdff2423 <dffdff2423@gmail.com>
//
// SPDX-License-Identifier: GPL-3.0-only

namespace YamlWarrior.Robust.TypeInfo;

public sealed record AssemblyTypes {
    /// <summary>
    /// Map of KindId to Prototype
    /// </summary>
    public Dictionary<string, PrototypeInfo> Prototypes { get; init; } = new();

    public static AssemblyTypes Merge(AssemblyTypes lhs, AssemblyTypes rhs) {
        var joined = new AssemblyTypes {
            Prototypes = new Dictionary<string, PrototypeInfo>(lhs.Prototypes)
        };

        foreach (var (kind, proto) in rhs.Prototypes) {
            if (!joined.Prototypes.TryAdd(kind, proto)) {
                throw new Exception("Duplicate kind: " + kind);
            }
        }

        return joined;
    }
}

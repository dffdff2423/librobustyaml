// SPDX-FileCopyrightText: (C) 2026 dffdff2423 <dffdff2423@gmail.com>
//
// SPDX-License-Identifier: GPL-3.0-only

using YamlWarrior.Robust.TypeLoading;

namespace YamlWarrior.Robust.TypeInfo;

public sealed record RobustAssemblyTypes {
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

    public static RobustAssemblyTypes Merge(RobustAssemblyTypes lhs, RobustAssemblyTypes rhs) {
        var joined = new RobustAssemblyTypes {
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

        foreach (var (kind, newComp) in rhs.Components) {
            if (joined.Components.TryGetValue(kind, out var oldComp)) {
                if (!newComp.Predicted && !oldComp.Predicted) {
                    // TODO: Sometimes the client and server have the exact same component with the same name. But they do not form a
                    //       predicted component hierarchy. For now, we will just store the server component since I still need to check
                    //       if client components are even relevant at all for yaml parsing. RadiationCollectorComponent is an example of this

                    // This code should be fine since it should be illegal RegisterComponent on shared components
                    if (newComp.FullName.Contains("Server")) {
                        joined.Components[kind] = newComp;
                    } else {
                        joined.Components[kind] = oldComp;
                    }
                } else {
                    joined.Components[kind] = ContentAssembly.MergePredictedComponents(oldComp, newComp);
                }
            } else {
                joined.Components.Add(kind, newComp);
            }
        }

        return joined;
    }
}

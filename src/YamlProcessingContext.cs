// SPDX-FileCopyrightText: (C) 2026 dffdff2423 <dffdff2423@gmail.com>
//
// SPDX-License-Identifier: GPL-3.0-only

using System.Xml.Linq;

using JetBrains.Annotations;

using YamlWarrior.Robust.TypeInfo;
using YamlWarrior.Robust.TypeLoading;

namespace YamlWarrior.Robust;

/// <summary>
/// Self-explanatory name. This class stores the information needed to process yaml.
/// </summary>
/// <remarks>
/// On startup it loads the required types from Robust.Shared.dll. To process DataDefinitions in an assembly you must
/// call <see cref="LoadContent"/>. If you are lazy, <see cref="LoadAllContent"/> processes all relevant ss14 assemblies.
/// Note that RT assemblies that define components also need to be processed as "content" for most applications.
/// </remarks>
[PublicAPI]
public sealed class YamlProcessingContext(string robustSharedPath) {
    /// <summary>
    /// Type information parsed from assemblies
    /// </summary>
    public AssemblyTypes RobustTypes { get; private set; } = new();

    private readonly EngineAssemblies _engine = new(robustSharedPath);

    /// <summary>
    /// Documentation XMLs for loaded assemblies
    /// </summary>
    public Dictionary<string, XElement> Documentation { get; } = new();

    /// <summary>
    /// Adds a content assembly to this context.
    /// </summary>
    public void LoadContent(string path) {
        var docs = XElement.Load(Path.ChangeExtension(path, ".xml"));
        var data = ContentAssembly.ExtractYamlTypes(_engine, path, docs);
        RobustTypes = AssemblyTypes.Merge(RobustTypes, data);
    }

    /// <summary>
    /// Load all relevant assemblies in the given build prefix
    /// </summary>
    public void LoadAllContent(string pfx) {
        foreach (var seg in AssemblyNames.DefaultContentAssemblyPathSegments) {
            LoadContent(Path.Join(pfx, seg));
        }
    }
}

// SPDX-FileCopyrightText: (C) 2026 dffdff2423 <dffdff2423@gmail.com>
//
// SPDX-License-Identifier: GPL-3.0-only

using System.Reflection;

namespace YamlWarrior.Robust.Assemblies;

public sealed class EngineAssemblies {
    public readonly Type PrototypeAttribute;
    public readonly PropertyInfo PrototypeAttributeTypeProperty;

    // ReSharper disable once InconsistentNaming
    public readonly Type IInheritingPrototype;

    public EngineAssemblies(string sharedPath) {
        var shared = Assembly.LoadFrom(sharedPath);

        var prototype = shared.GetType(RobustNames.PrototypeAttribute);
        PrototypeAttribute = prototype ?? throw new InvalidDataException(nameof(sharedPath));

        var typeField = prototype.GetProperty("Type");
        if (typeField == null) {
            throw new InvalidDataException(nameof(sharedPath));
        }
        PrototypeAttributeTypeProperty = typeField;

        var iInheriting = shared.GetType(RobustNames.IInheritingPrototype);
        IInheritingPrototype = iInheriting ?? throw new InvalidDataException(nameof(sharedPath));
    }
}

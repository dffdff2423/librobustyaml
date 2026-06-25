// SPDX-FileCopyrightText: (C) 2026 dffdff2423 <dffdff2423@gmail.com>
//
// SPDX-License-Identifier: GPL-3.0-only

using System.Diagnostics;
using System.Reflection;

using JetBrains.Annotations;

using YamlWarrior.Robust.TypeInfo;

namespace YamlWarrior.Robust.Assemblies;

/// <summary>
/// Operations on Content assemblies. Note that we consider any assembly which we parse DataDefinitions from a content
/// assembly. Even ones inside engine code.
/// </summary>
public static class ContentAssembly {
    /// <summary>
    /// Extract DataDefinitions et al. from an assembly.
    /// </summary>
    /// <param name="engine">Engine info</param>
    /// <param name="path">Assembly to extract</param>
    public static AssemblyTypes ExtractYamlTypes(EngineAssemblies engine, string path) {
        var infos = new AssemblyTypes();

        var asm = Assembly.LoadFrom(path);

        var types = asm.GetTypes();
        foreach (var ty in types) {
            var protoAttr = ty.GetCustomAttribute(engine.PrototypeAttribute);
            if (protoAttr != null) {
                var kindId = (string?)engine.PrototypeAttributeTypeProperty.GetValue(protoAttr);
                if (kindId == null)
                    kindId = ConvertTypeNameToPrototypeKindId(ty.Name);

                // Prototypes should not contain generics
                Debug.Assert(!ty.ContainsGenericParameters);
                Debug.Assert(ty.FullName != null);

                infos.Prototypes.Add(kindId, new PrototypeInfo {
                    KindId = kindId,
                    FullName = ty.FullName,
                    SupportsInheritance = ty.ImplementsInterface(engine.IInheritingPrototype),
                });
            }
        }

        return infos;
    }

    [Pure]
    private static string ConvertTypeNameToPrototypeKindId(string str) {
        const string prototypeNameEnding = "Prototype";

        // Taken directly from RT.
        // SPDX-SnippetBegin
        // SPDX-SnippetCopyrightText: Copyright (c) 2017-2026 Space Wizards Federation
        // SPDX-License-Identifier: MIT
        var name = str.AsSpan();
        if (!str.EndsWith(prototypeNameEnding))
            return $"{char.ToLowerInvariant(name[0])}{name.Slice(1).ToString()}";

        return $"{char.ToLowerInvariant(name[0])}{name.Slice(1, name.Length - prototypeNameEnding.Length - 1).ToString()}";
        // SPDX-SnippetEnd
    }

    extension(Type ty) {
        [Pure]
        private bool ImplementsInterface(Type other) => ty.GetInterfaces().Contains(other);
    }
}

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
            if (protoAttr == null)
                continue;

            var kindId = (string?)engine.PrototypeAttributeTypeProperty.GetValue(protoAttr);
            if (kindId == null)
                kindId = ConvertTypeNameToPrototypeKindId(ty.Name);

            // Prototypes should not contain generics
            Debug.Assert(!ty.ContainsGenericParameters);
            Debug.Assert(ty.FullName != null);

            var idField = ty.GetProperties() .SingleOrDefault(p => p.GetCustomAttribute(engine.IdDataFieldAttribute) != null);
            var parentField = ty.GetProperties().SingleOrDefault(p => p.GetCustomAttribute(engine.ParentDataFieldAttribute) != null);
            var abstractField = ty.GetProperties().SingleOrDefault(p => p.GetCustomAttribute(engine.AbstractDataFieldAttribute) != null);

            var fields = ty.GetProperties()
                .Where(prop => prop.GetCustomAttribute(engine.DataFieldAttribute) != null)
                .Select(prop => ExtractDataFieldInfo(engine, prop))
                .ToArray();

            infos.Prototypes.Add(kindId, new PrototypeInfo {
                KindId = kindId,
                FullName = ty.FullName,
                IdDataField = idField != null ? ExtractDataFieldInfo(engine, idField) : null,
                ParentDataField = parentField != null ? ExtractDataFieldInfo(engine, parentField) : null,
                AbstractDataField = abstractField != null ? ExtractDataFieldInfo(engine, abstractField) : null,
                DataFields = fields,
                SupportsInheritance = ty.ImplementsInterface(engine.IInheritingPrototype),
            });
        }

        foreach (var ty in types) {
            var ddAttr = ty.GetCustomAttribute(engine.DataDefinitionAttribute);
            if (ddAttr == null)
                continue;

            // DDs should not be generic
            Debug.Assert(!ty.ContainsGenericParameters);
            Debug.Assert(ty.FullName != null);

            var fields = ty.GetProperties()
                .Where(prop => prop.GetCustomAttribute(engine.DataFieldAttribute) != null)
                .Select(prop => ExtractDataFieldInfo(engine, prop))
                .ToArray();

            infos.DataDefinitions.Add(ty.FullName, new DataDefinitionInfo {
                FullName = ty.FullName,
                DataFields = fields,
            });
        }

        foreach (var ty in types) {
            var drAttr = ty.GetCustomAttribute(engine.DataRecordAttribute);
            if (drAttr == null)
                continue;

            // DDs should not be generic
            Debug.Assert(!ty.ContainsGenericParameters);
            Debug.Assert(ty.FullName != null);

            var fields = ty.GetProperties()
                .Select(prop => ExtractDataFieldInfo(engine, prop))
                .ToArray();

            infos.DataDefinitions.Add(ty.FullName, new DataDefinitionInfo {
                FullName = ty.FullName,
                DataFields = fields,
            });
        }
        return infos;
    }

    [Pure]
    private static DataFieldInfo ExtractDataFieldInfo(EngineAssemblies engine, PropertyInfo prop) {
        var attr = prop.GetCustomAttribute(engine.DataFieldAttribute);
        var tag = ConvertFieldNameToTag(prop.Name);
        var required = false;
        var priority = 0;
        Type? customType = null;

        if (attr != null) {
            tag = (string?)engine.DataFieldAttributeTagProperty.GetValue(attr) ?? tag;
            required = (bool?)engine.DataFieldAttributeRequiredProperty.GetValue(attr) ?? required;
            priority = (int?)engine.DataFieldBaseAttributePriorityProperty.GetValue(attr) ?? priority;
            customType = (Type?)engine.DataFieldBaseAttributeCustomTypeSerializerProperty.GetValue(attr) ?? customType;
        }

        Debug.Assert(prop.PropertyType.FullName != null);

        return new DataFieldInfo {
            TypeName = prop.PropertyType.FullName,
            Tag = tag,
            Required = required,
            Priority = priority,
            CustomTypeSerializer = customType,
            CustomTypeSerializerName = customType?.FullName,
        };
    }

    [Pure]
    private static string ConvertTypeNameToPrototypeKindId(string str) {
        const string prototypeNameEnding = "Prototype";

        // Taken directly from RT: PrototypeUtility.CalculatePrototypeName
        // SPDX-SnippetBegin
        // SPDX-SnippetCopyrightText: Copyright (c) 2017-2026 Space Wizards Federation
        // SPDX-License-Identifier: MIT
        var name = str.AsSpan();
        if (!str.EndsWith(prototypeNameEnding))
            return $"{char.ToLowerInvariant(name[0])}{name.Slice(1).ToString()}";

        return $"{char.ToLowerInvariant(name[0])}{name.Slice(1, name.Length - prototypeNameEnding.Length - 1).ToString()}";
        // SPDX-SnippetEnd
    }

    [Pure]
    private static string ConvertFieldNameToTag(string str) {
        // Taken directly from RT: DataDefinitionUtility.AutoGenerateTag
        // SPDX-SnippetBegin
        // SPDX-SnippetCopyrightText: Copyright (c) 2017-2026 Space Wizards Federation
        // SPDX-License-Identifier: MIT
        var span = str.AsSpan();
        return $"{char.ToLowerInvariant(span[0])}{span.Slice(1).ToString()}";
        // SPDX-SnippetEnd
    }

    extension(Type ty) {
        [Pure]
        private bool ImplementsInterface(Type other) => ty.GetInterfaces().Contains(other);
    }
}

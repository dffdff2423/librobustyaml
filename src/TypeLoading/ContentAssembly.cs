// SPDX-FileCopyrightText: (C) 2026 dffdff2423 <dffdff2423@gmail.com>
//
// SPDX-License-Identifier: GPL-3.0-only

using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;

using JetBrains.Annotations;

using YamlWarrior.Robust.TypeInfo;

namespace YamlWarrior.Robust.TypeLoading;

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
    /// <param name="docs">XML documentation file for the given assembly</param>
    public static AssemblyTypes ExtractYamlTypes(EngineAssemblies engine, string path, XElement? docs = null) {
        var infos = new AssemblyTypes();

        var asm = Assembly.LoadFrom(path);

        var types = asm.GetTypes();
        foreach (var ty in types) {
            var protoAttr = ty.GetCustomAttribute(engine.PrototypeAttribute);
            if (protoAttr == null)
                continue;

            var kindId = (string?)engine.PrototypeAttributeTypeProperty.GetValue(protoAttr)
                         ?? ConvertTypeNameToPrototypeKindId(ty.Name);

            // Prototypes should not contain generics
            Debug.Assert(!ty.ContainsGenericParameters);
            Debug.Assert(ty.FullName != null);

            var idField = ty.GetProperties() .SingleOrDefault(p => p.GetCustomAttribute(engine.IdDataFieldAttribute) != null);
            var parentField = ty.GetProperties().SingleOrDefault(p => p.GetCustomAttribute(engine.ParentDataFieldAttribute) != null);
            var abstractField = ty.GetProperties().SingleOrDefault(p => p.GetCustomAttribute(engine.AbstractDataFieldAttribute) != null);

            var fields = ExtractDataFields(engine, ty, docs, true);

            var docElem = docs != null ? GetTypeDocs(docs, ty.FullName) : null;

            infos.Prototypes.Add(kindId, new PrototypeInfo {
                KindId = kindId,
                FullName = ty.FullName,
                IdDataField = idField != null ? ExtractDataFieldInfo(engine, idField, docs) : null,
                ParentDataField = parentField != null ? ExtractDataFieldInfo(engine, parentField, docs) : null,
                AbstractDataField = abstractField != null ? ExtractDataFieldInfo(engine, abstractField, docs) : null,
                Docs = docElem,
                DocsString = docElem?.ToString(),
                DataFields = fields,
                SupportsInheritance = ty.ImplementsInterface(engine.IInheritingPrototype),
            });
        }

        // I know this is needlessly iterating types an unneeded number of times but this is fast enough for this and makes the code easier to follow
        foreach (var ty in types) {
            var ddAttr = ty.GetCustomAttribute(engine.DataDefinitionAttribute);
            if (ddAttr == null)
                continue;

            // DDs should not be generic
            Debug.Assert(!ty.ContainsGenericParameters);
            Debug.Assert(ty.FullName != null);

            var fields = ExtractDataFields(engine, ty, docs, true);

            var docElem = docs != null ? GetTypeDocs(docs, ty.FullName) : null;

            infos.DataDefinitions.Add(ty.FullName, new DataDefinitionInfo {
                FullName = ty.FullName,
                DataFields = fields,
                Docs = docElem,
                DocsString = docElem?.ToString(),
            });
        }

        foreach (var ty in types) {
            var drAttr = ty.GetCustomAttribute(engine.DataRecordAttribute);
            if (drAttr == null)
                continue;

            // DDs should not be generic
            Debug.Assert(!ty.ContainsGenericParameters);
            Debug.Assert(ty.FullName != null);

            var fields = ExtractDataFields(engine, ty, docs, false);

            var docElem = docs != null ? GetTypeDocs(docs, ty.FullName) : null;

            infos.DataDefinitions.Add(ty.FullName, new DataDefinitionInfo {
                FullName = ty.FullName,
                DataFields = fields,
                Docs = docElem,
                DocsString = docElem?.ToString(),
            });
        }

        foreach (var ty in types) {
            var rcAttr =  ty.GetCustomAttribute(engine.RegisterComponentAttribute);
            if (rcAttr == null)
                continue;
            var cpAttr = ty.GetCustomAttribute(engine.ComponentProtoNameAttribute);
            var unsavedAttr = ty.GetCustomAttribute(engine.UnsavedComponentAttribute);

            // DDs should not be generic
            Debug.Assert(!ty.ContainsGenericParameters);
            Debug.Assert(ty.FullName != null);

            var yamlName = ConvertComponentName(ty.Name);
            if (cpAttr != null) {
                yamlName = (string?)engine.ComponentProtoNameAttributePrototypeNameProperty.GetValue(cpAttr) ?? yamlName;
            }

            var fields = ExtractDataFields(engine, ty, docs, true);

            var docElem = docs != null ? GetTypeDocs(docs, ty.FullName) : null;

            infos.Components.Add(ty.FullName, new ComponentInfo {
                FullName = ty.FullName,
                DataFields = fields,
                Unsaved = unsavedAttr != null,
                YamlName = yamlName,
                Docs = docElem,
                DocsString = docElem?.ToString(),
            });
        }
        return infos;
    }

    [Pure]
    private static DataFieldInfo[] ExtractDataFields(EngineAssemblies engine, Type ty, XElement? docs, bool requireAttr) {
        var fields = ty.GetFields()
            .Where(prop => !requireAttr || prop.GetCustomAttribute(engine.DataFieldAttribute) != null)
            .Select(prop => ExtractDataFieldInfo(engine, prop, docs));
        var props = ty.GetProperties()
            .Where(prop => !requireAttr || prop.GetCustomAttribute(engine.DataFieldAttribute) != null)
            .Select(prop => ExtractDataFieldInfo(engine, prop, docs))
            .Concat(fields)
            .ToArray();
        return props;
    }

    [Pure]
    private static DataFieldInfo ExtractDataFieldInfo(EngineAssemblies engine, MemberInfo prop, XElement? docs) {
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

        var type = GetPropFieldType(prop);
        Debug.Assert(type.FullName != null);

        var owner = prop.DeclaringType?.FullName ?? throw new InvalidDataException("DataField property does not have owner");
        var docElem = docs != null ? GetPropertyDocs(docs, owner, prop.Name) : null;

        return new DataFieldInfo {
            TypeName = type.FullName,
            Tag = tag,
            Required = required,
            Priority = priority,
            CustomTypeSerializer = customType,
            CustomTypeSerializerName = customType?.FullName,
            Docs = docElem,
            DocsString = docElem?.ToString() ?? "",
        };
    }

    [Pure]
    private static Type GetPropFieldType(MemberInfo prop) {
        return prop.MemberType switch {
            MemberTypes.Field => ((FieldInfo)prop).FieldType,
            MemberTypes.Property => ((PropertyInfo)prop).PropertyType,
            _ => throw new ArgumentException($"{prop.MemberType} is not a property or a field")
        };
    }

    [Pure]
    private static XElement? GetTypeDocs(XElement docs, string typeName)
        => docs
            .Element("members")
            ?.Elements()
            .SingleOrDefault(el => el.Attribute("name")?.Value == "T:" + typeName);

    [Pure]
    private static XElement? GetPropertyDocs(XElement docs, string typeName, string propName)
        => docs
            .Element("members")
            ?.Elements()
            .SingleOrDefault(el => el.Attribute("name")?.Value == "F:" + typeName + propName);

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

    /// <summary>
    /// Based on RT ComponentFactory.CalculateComponentName
    /// </summary>
    private static string ConvertComponentName(string typeName)
    {
        // Taken from RT, slightly modified by aquif for librobustyaml
        // SPDX-SnippetBegin
        // SPDX-SnippetCopyrightText: Copyright (c) 2017-2026 Space Wizards Federation
        // SPDX-License-Identifier: MIT
        const string component = "Component";
        if (!typeName.EndsWith(component))
        {
            return typeName;

            // RT throws here, we don't just to be a bit graceful.
            // throw new InvalidDataException($"Component {typeName} must end with the word Component");
        }

        string name = typeName[..^component.Length];
        const string client = "Client";
        const string server = "Server";
        const string shared = "Shared";
        if (typeName.StartsWith(client, StringComparison.Ordinal))
        {
            name = typeName[client.Length..^component.Length];
        }
        else if (typeName.StartsWith(server, StringComparison.Ordinal))
        {
            name = typeName[server.Length..^component.Length];
        }
        else if (typeName.StartsWith(shared, StringComparison.Ordinal))
        {
            name = typeName[shared.Length..^component.Length];
        }
        Debug.Assert(name != String.Empty, $"Component {typeName} has invalid name");
        return name;
        // SPDX-SnippetEnd
    }

    extension(Type ty) {
        [Pure]
        private bool ImplementsInterface(Type other) => ty.GetInterfaces().Contains(other);
    }
}

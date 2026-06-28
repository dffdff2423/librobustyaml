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
/// assembly, including ones inside engine code.
/// </summary>
public static class ContentAssembly {
    /// <summary>
    /// Extract DataDefinitions et al. from an assembly.
    /// </summary>
    /// <param name="engine">Engine info</param>
    /// <param name="path">Assembly to extract</param>
    /// <param name="docs">XML documentation file for the given assembly</param>
    public static RobustAssemblyTypes ExtractYamlTypes(EngineAssemblies engine, string path, XElement? docs = null) {
        var infos = new RobustAssemblyTypes();

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

            var idField = ty.GetProperties().SingleOrDefault(p => p.GetCustomAttribute(engine.IdDataFieldAttribute) != null);
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
                SuperTypeFullName = ty.BaseType?.FullName,
            });
        }

        // I know this is needlessly iterating types an unneeded number of times but this is fast enough for this and makes the code easier to follow
        foreach (var ty in types) {
            var rcAttr =  ty.GetCustomAttribute(engine.RegisterComponentAttribute);
            if (rcAttr != null)
                continue; // some comps inherit from IDDI types so we need to skip those here

            var ddAttr = ty.GetCustomAttribute(engine.DataDefinitionAttribute);
            var superClass = ty.BaseType;
            var superDdiAttr = superClass?.GetCustomAttribute(engine.ImplicitDataDefinitionForInheritorsAttribute);
            // Treat DDIss as DDs to maintain class hierarchy
            var ddiAttr = ty.GetCustomAttribute(engine.ImplicitDataDefinitionForInheritorsAttribute);

            if (ddAttr == null && ddiAttr == null && superDdiAttr == null)
                continue;
            if (superClass == engine.Component) // We handle components below
                continue;

            // DDs can be generic but not contructed
            Debug.Assert(!ty.IsConstructedGenericType, $"Constructed Generic: {ty.FullName}");
            Debug.Assert(ty.FullName != null);

            var fields = ExtractDataFields(engine, ty, docs, true);

            var docElem = docs != null ? GetTypeDocs(docs, ty.FullName) : null;

            infos.DataDefinitions.Add(ty.FullName, new DataDefinitionInfo {
                FullName = ty.FullName,
                DataFields = fields,
                Docs = docElem,
                DocsString = docElem?.ToString(),
                SuperTypeFullName = ty.BaseType?.FullName,
            });
        }

        foreach (var ty in types) {
            var drAttr = ty.GetCustomAttribute(engine.DataRecordAttribute);
            if (drAttr == null)
                continue;

            // DRs should not be generic
            Debug.Assert(!ty.ContainsGenericParameters);
            Debug.Assert(ty.FullName != null);

            var fields = ExtractDataFields(engine, ty, docs, false);

            var docElem = docs != null ? GetTypeDocs(docs, ty.FullName) : null;

            infos.DataDefinitions.Add(ty.FullName, new DataDefinitionInfo {
                FullName = ty.FullName,
                DataFields = fields,
                Docs = docElem,
                DocsString = docElem?.ToString(),
                SuperTypeFullName = ty.BaseType?.FullName,
            });
        }

        foreach (var ty in types) {
            var rcAttr =  ty.GetCustomAttribute(engine.RegisterComponentAttribute);
            var superClass = ty.BaseType;
            if (rcAttr == null && superClass != engine.Component)
                continue;
            var cpAttr = ty.GetCustomAttribute(engine.ComponentProtoNameAttribute);
            var unsavedAttr = ty.GetCustomAttribute(engine.UnsavedComponentAttribute);

            // Comps should not be constructed generic
            Debug.Assert(!ty.IsConstructedGenericType, $"Constructed Generic: {ty.FullName}");
            Debug.Assert(ty.FullName != null);

            var yamlName = ConvertComponentName(ty.Name);
            if (cpAttr != null) {
                yamlName = (string?)engine.ComponentProtoNameAttributePrototypeNameProperty.GetValue(cpAttr) ?? yamlName;
            }

            var fields = ExtractDataFields(engine, ty, docs, true);

            var docElem = docs != null ? GetTypeDocs(docs, ty.FullName) : null;

            string? sharedName = null;
            string? clientName = null;
            string? serverName = null;
            var predicted = false;
            if (superClass != engine.Component || rcAttr == null) {
                predicted = true;
                switch (GuessComponentSide(ty.Name, asm.GetName().Name)) {
                case ComponentSide.Shared:
                    sharedName = ty.FullName;
                    break;
                case ComponentSide.Client:
                    clientName = ty.FullName;
                    break;
                case ComponentSide.Server:
                    serverName = ty.FullName;
                    break;
                default:
                    throw new InvalidDataException("Unable to guess predicted component side");
                }
            }

            var newComp = new ComponentInfo {
                FullName = ty.FullName,
                DataFields = fields,
                Unsaved = unsavedAttr != null,
                YamlName = yamlName,
                Docs = docElem,
                DocsString = docElem?.ToString(),
                SuperTypeFullName = ty.BaseType?.FullName,
                SharedFullName = sharedName,
                ClientFullName = clientName,
                ServerFullName = serverName,
                Predicted = predicted,
            };
            if (infos.Components.TryGetValue(yamlName, out var oldComp)) {
                infos.Components[yamlName] = MergePredictedComponents(oldComp, newComp);
            } else {
                infos.Components.Add(yamlName, newComp);
            }
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

    /// <summary>
    /// Merges two components that are sides or shared version of a predicted component and combines them into a single <see cref="ComponentInfo"/>
    /// </summary>
    /// <returns>The combined component</returns>
    public static ComponentInfo MergePredictedComponents(ComponentInfo lhs, ComponentInfo rhs) {
        // This merge logic supports predicted components which is why it is so ugly.
        // Basically we want to combine them into a single component.

        string? clientName = lhs.ClientFullName, serverName = lhs.ServerFullName, sharedName = lhs.SharedFullName;

        clientName ??= rhs.ClientFullName;
        serverName ??= rhs.ServerFullName;
        sharedName ??= rhs.SharedFullName;

        XElement? docs = null;
        XElement? opposingDocs = null;
        if (lhs.Docs?.Name == "mergedDocs") {
            docs = lhs.Docs;
            if (rhs.Docs != null) {
                opposingDocs = new XElement(GetComponentSideForMerging(rhs).ToString(), rhs.Docs.Elements());
            }
        } else if (rhs.Docs?.Name == "mergedDocs") {
            docs = rhs.Docs;
            if (lhs.Docs != null) {
                opposingDocs = new XElement(GetComponentSideForMerging(lhs).ToString(), lhs.Docs.Elements());
            }
        } else {
            XElement? lhsDocs = null;
            XElement? rhsDocs = null;
            if (lhs.Docs != null)
                lhsDocs = new XElement(GetComponentSideForMerging(lhs).ToString(), lhs.Docs.Elements());
            if (rhs.Docs != null)
                rhsDocs =  new XElement(GetComponentSideForMerging(rhs).ToString(), rhs.Docs.Elements());
            if (lhsDocs != null || rhsDocs != null) {
                docs = new XElement("mergedDocs", lhsDocs);
                opposingDocs = rhsDocs;
            }
        }
        docs = new XElement("mergedDocs", docs?.Elements(), opposingDocs);

        return new ComponentInfo {
            Predicted = true,
            FullName = "PREDICTED COMPONENT. DO NOT USE THE FullName FIELD. IF YOU SEE THIS AS A USER IT IS A BUG.",
            DataFields = lhs.DataFields.Concat(rhs.DataFields).ToArray(),
            YamlName = lhs.YamlName,
            Unsaved = lhs.Unsaved,
            ClientFullName = clientName,
            ServerFullName = serverName,
            SharedFullName = sharedName,
            Docs = docs,
            DocsString = docs?.ToString(),
        };
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
        var docElem = docs != null ? GetPropOrFieldDocs(docs, owner, prop) : null;

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

    private static XElement? GetPropOrFieldDocs(XElement docs, string parent, MemberInfo prop) {
        return prop.MemberType switch {
            MemberTypes.Field => GetFieldDocs(docs, parent, prop.Name),
            MemberTypes.Property => GetPropDocs(docs, parent, prop.Name),
            _ => throw new ArgumentException($"{prop.MemberType} is not a property or a field"),
        };
    }
    [Pure]
    private static XElement? GetFieldDocs(XElement docs, string typeName, string propName)
        => docs
            .Element("members")
            ?.Elements()
            .SingleOrDefault(el => el.Attribute("name")?.Value == "F:" + typeName + "." + propName);

    [Pure]
    private static XElement? GetPropDocs(XElement docs, string typeName, string propName)
        => docs
            .Element("members")
            ?.Elements()
            .SingleOrDefault(el => el.Attribute("name")?.Value == "P:" + typeName + "." + propName);

    [Pure]
    public static string ConvertTypeNameToPrototypeKindId(string str) {
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

    [Pure]
    private static ComponentSide GuessComponentSide(string typeName, string? asmName) {
        const string client = "Client";
        const string server = "Server";
        const string shared = "Shared";

        if (typeName.StartsWith(client, StringComparison.Ordinal)) {
            return ComponentSide.Client;
        }
        if (typeName.StartsWith(server, StringComparison.Ordinal)) {
            return ComponentSide.Server;
        }
        if (typeName.StartsWith(shared, StringComparison.Ordinal)) {
            return ComponentSide.Shared;
        }

        return asmName switch {
            "Content.Client" => ComponentSide.Client,
            "Content.Server" => ComponentSide.Server,
            "Content.Shared" => ComponentSide.Shared,
            "Robust.Client" => ComponentSide.Client,
            "Robust.Server" => ComponentSide.Server,
            "Robust.Shared" => ComponentSide.Shared,

            _ => ComponentSide.Unknown,
        };
    }

    [Pure]
    private static ComponentSide GetComponentSideForMerging(ComponentInfo info) {
        Debug.Assert(info.Predicted);

        if (info.ServerFullName != null) {
            Debug.Assert(info.ClientFullName == null);
            Debug.Assert(info.SharedFullName == null);
            return ComponentSide.Server;
        }

        if (info.ClientFullName != null) {
            Debug.Assert(info.SharedFullName == null);
            Debug.Assert(info.ServerFullName == null);
            return ComponentSide.Client;
        }

        if (info.SharedFullName != null) {
            Debug.Assert(info.ClientFullName == null);
            Debug.Assert(info.ServerFullName == null);
            return ComponentSide.Server;
        }

        return ComponentSide.Unknown;
    }

    private enum ComponentSide {
        Shared,
        Client,
        Server,
        Unknown,
    }

    /// <summary>
    /// Based on RT ComponentFactory.CalculateComponentName
    /// </summary>
    [Pure]
    public static string ConvertComponentName(string typeName)
    {
        // Taken from RT, slightly modified by aquif for librobustyaml
        // SPDX-SnippetBegin
        // SPDX-SnippetCopyrightText: Copyright (c) 2017-2026 Space Wizards Federation
        // SPDX-License-Identifier: MIT
        const string component = "Component";
        if (!typeName.EndsWith(component)) {
            return typeName;

            // RT throws here, we don't just to be a bit graceful.
            // throw new InvalidDataException($"Component {typeName} must end with the word Component");
        }

        string name = typeName[..^component.Length];
        const string client = "Client";
        const string server = "Server";
        const string shared = "Shared";
        if (typeName.StartsWith(client, StringComparison.Ordinal)) {
            name = typeName[client.Length..^component.Length];
        }
        else if (typeName.StartsWith(server, StringComparison.Ordinal)) {
            name = typeName[server.Length..^component.Length];
        }
        else if (typeName.StartsWith(shared, StringComparison.Ordinal)) {
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

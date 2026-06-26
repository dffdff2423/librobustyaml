// SPDX-FileCopyrightText: (C) 2026 dffdff2423 <dffdff2423@gmail.com>
//
// SPDX-License-Identifier: GPL-3.0-only

using System.Reflection;

using JetBrains.Annotations;

using YamlWarrior.Robust.TypeInfo;

namespace YamlWarrior.Robust.TypeLoading;

/// <summary>
/// Types extracted from RT for reflection use
/// </summary>
public sealed class EngineAssemblies {
    public readonly Type PrototypeAttribute;
    public readonly PropertyInfo PrototypeAttributeTypeProperty;

    // ReSharper disable once InconsistentNaming
    public readonly Type IInheritingPrototype;

    [PublicAPI]
    public readonly Type DataFieldBaseAttribute;
    public readonly FieldInfo DataFieldBaseAttributePriorityProperty;
    public readonly FieldInfo DataFieldBaseAttributeCustomTypeSerializerProperty;

    public readonly Type DataFieldAttribute;
    public readonly PropertyInfo DataFieldAttributeTagProperty;
    public readonly FieldInfo DataFieldAttributeRequiredProperty;

    public readonly Type IdDataFieldAttribute;
    public readonly Type ParentDataFieldAttribute;
    public readonly Type AbstractDataFieldAttribute;

    public readonly Type DataDefinitionAttribute;
    public readonly Type DataRecordAttribute;

    public readonly Type RegisterComponentAttribute;
    public readonly Type ComponentProtoNameAttribute;
    public readonly PropertyInfo ComponentProtoNameAttributePrototypeNameProperty;
    public readonly Type UnsavedComponentAttribute;

    public EngineAssemblies(string sharedPath) {
        var shared = Assembly.LoadFrom(sharedPath);

        PrototypeAttribute = shared.GetType(RobustNames.PrototypeAttribute) ??  throw new InvalidDataException(sharedPath);
        PrototypeAttributeTypeProperty = PrototypeAttribute.GetProperty("Type") ?? throw new InvalidDataException(sharedPath);

        IInheritingPrototype = shared.GetType(RobustNames.IInheritingPrototype) ?? throw new InvalidDataException(sharedPath);

        DataFieldBaseAttribute = shared.GetType(RobustNames.DataFieldBaseAttribute) ?? throw new InvalidDataException(sharedPath);
        DataFieldBaseAttributePriorityProperty = DataFieldBaseAttribute.GetField(nameof(DataFieldInfo.Priority)) ?? throw new InvalidDataException(sharedPath);
        DataFieldBaseAttributeCustomTypeSerializerProperty = DataFieldBaseAttribute.GetField(nameof(DataFieldInfo.CustomTypeSerializer)) ?? throw new InvalidDataException(sharedPath);

        DataFieldAttribute = shared.GetType(RobustNames.DataFieldAttribute) ?? throw new InvalidDataException(sharedPath);
        DataFieldAttributeTagProperty = DataFieldAttribute.GetProperty(nameof(DataFieldInfo.Tag)) ?? throw new InvalidDataException(sharedPath);
        DataFieldAttributeRequiredProperty = DataFieldAttribute.GetField(nameof(DataFieldInfo.Required)) ?? throw new InvalidDataException(sharedPath);

        IdDataFieldAttribute = shared.GetType(RobustNames.IdDataFieldAttribute) ?? throw new InvalidDataException(sharedPath);

        ParentDataFieldAttribute = shared.GetType(RobustNames.ParentDataFieldAttribute) ?? throw new InvalidDataException(sharedPath);

        AbstractDataFieldAttribute = shared.GetType(RobustNames.AbstractDataFieldAttribute) ?? throw new InvalidDataException(sharedPath);

        DataDefinitionAttribute = shared.GetType(RobustNames.DataDefinitionAttribute) ?? throw new InvalidDataException(sharedPath);
        DataRecordAttribute = shared.GetType(RobustNames.DataRecordAttribute) ?? throw new InvalidDataException(sharedPath);

        RegisterComponentAttribute = shared.GetType(RobustNames.RegisterComponentAttribute) ?? throw new InvalidDataException(sharedPath);
        ComponentProtoNameAttribute = shared.GetType(RobustNames.ComponentProtoNameAttribute) ?? throw new InvalidDataException(sharedPath);
        ComponentProtoNameAttributePrototypeNameProperty = ComponentProtoNameAttribute.GetProperty("PrototypeName") ?? throw new InvalidDataException(sharedPath);
        UnsavedComponentAttribute = shared.GetType(RobustNames.UnsavedComponentAttribute) ?? throw new InvalidDataException(sharedPath);
    }
}

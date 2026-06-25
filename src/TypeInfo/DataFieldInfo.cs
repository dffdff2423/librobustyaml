// SPDX-FileCopyrightText: (C) 2026 dffdff2423 <dffdff2423@gmail.com>
//
// SPDX-License-Identifier: GPL-3.0-only

using System.Text.Json.Serialization;

using JetBrains.Annotations;

namespace YamlWarrior.Robust.TypeInfo;

[PublicAPI]
public sealed record DataFieldInfo {
    /// <summary>
    /// The type of the data field
    /// </summary>
    public required string TypeName { get; init; }

    /// <summary>
    /// The yaml name of the data field
    /// </summary>
    public required string Tag { get; init; }

    /// <summary>
    /// If this data field is mandatory in yaml definitions
    /// </summary>
    public bool Required { get; init; }

    /// <summary>
    /// The priority of the data field for serialization. Fields with higher priority get serialized first This does not
    /// actually seem to be used at all, so I don't intend to support it in this library unless requested, or I find a need.
    /// </summary>
    public int Priority { get; init; }

    /// <summary>
    /// Custom Type Serializer.
    /// </summary>
    // TODO: Support custom serializers
    [JsonIgnore]
    public Type? CustomTypeSerializer { get; init; }

    /// <summary>
    ///  Printable name of the CustomTypeSerializer
    /// </summary>
    public string? CustomTypeSerializerName { get; init; }

    /// <summary>
    /// Documentation
    /// </summary>
    public string Docs { get; init; } = "";
}

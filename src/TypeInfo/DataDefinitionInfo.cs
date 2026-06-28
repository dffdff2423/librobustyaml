// SPDX-FileCopyrightText: (C) 2026 dffdff2423 <dffdff2423@gmail.com>
//
// SPDX-License-Identifier: GPL-3.0-only

using System.Text.Json.Serialization;
using System.Xml.Linq;

using JetBrains.Annotations;

namespace YamlWarrior.Robust.TypeInfo;

/// <summary>
/// RT Data definition
/// </summary>
/// <remarks>
/// All DataDefinition types support write-only serialization with <see cref="System.Text.Json"/>.
/// </remarks>
[PublicAPI]
public record DataDefinitionInfo {
    /// <summary>
    /// The full name of the type
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    /// Documentation in <see cref="XElement"/> form
    /// </summary>
    [JsonIgnore]
    public XElement? Docs { get; init; }

    /// <summary>
    /// If you want to parse XML some other way. If you are fine with <see cref="System.Xml.Linq.XElement"/> use <see cref="Docs"/>.
    /// </summary>
    public string? DocsString { get; init; }

    /// <summary>
    /// Member data fields
    /// </summary>
    public DataFieldInfo[] DataFields { get; init; } = [];

    /// <summary>
    /// The full name of this type's super class. Arbitrary for predicted components.
    /// </summary>
    public string? SuperTypeFullName { get; init; }
}

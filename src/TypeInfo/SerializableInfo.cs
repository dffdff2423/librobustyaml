// SPDX-FileCopyrightText: (C) 2026 dffdff2423 <dffdff2423@gmail.com>
//
// SPDX-License-Identifier: GPL-3.0-only

using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace YamlWarrior.Robust.TypeInfo;

/// <summary>
/// Represents a <see cref="System.SerializableAttribute"/> marked type. Note that not every one of these types can be
/// serialized by RT.
/// </summary>
public record SerializableInfo {
    // TODO: Possibly add special handling for enums? Looking through the codebase they do not seem to be very common in
    //       DDs so I am not sure it is needed.

    /// <summary>
    /// The FullName of the type
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    /// CSharp type so you can try and figure out how to serialize it.
    /// </summary>
    [JsonIgnore]
    public Type? AsmType { get; init; }

    /// <summary>
    /// Documentation in <see cref="XElement"/> form
    /// </summary>
    [JsonIgnore]
    public XElement? Docs { get; init; }

    /// <summary>
    /// If you want to parse XML some other way. If you are fine with <see cref="System.Xml.Linq.XElement"/> use <see cref="Docs"/>.
    /// </summary>
    public string? DocsString { get; init; }
}

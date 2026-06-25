// SPDX-FileCopyrightText: (C) 2026 dffdff2423 <dffdff2423@gmail.com>
//
// SPDX-License-Identifier: GPL-3.0-only

using JetBrains.Annotations;

namespace YamlWarrior.Robust.TypeInfo;

[PublicAPI]
public sealed record PrototypeInfo {
    public required string FullName { get; init; }

    /// <summary>
    /// `entity` in `-type: entity`
    /// </summary>
    public required string KindId { get; init; }

    /// <summary>
    /// Wheaten or not the prototype supports yaml inheritance
    /// </summary>
    public bool SupportsInheritance { get; init; }

    /// <summary>
    /// Documentation
    /// </summary>
    public string Docs { get; init; } = "";

    /// <summary>
    /// The ID data field for the prototype
    /// </summary>
    public required DataFieldInfo? IdDataField { get; init; }

    /// <summary>
    /// Nonnull if <see cref="SupportsInheritance"/>
    /// </summary>
    public DataFieldInfo? ParentDataField { get; init; }

    /// <summary>
    /// Nonnull if <see cref="SupportsInheritance"/>
    /// </summary>
    public DataFieldInfo? AbstractDataField { get; init; }

    /// <summary>
    /// Member data fields
    /// </summary>
    public DataFieldInfo[] DataFields { get; init; } = [];
}

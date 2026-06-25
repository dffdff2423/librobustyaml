// SPDX-FileCopyrightText: (C) 2026 dffdff2423 <dffdff2423@gmail.com>
//
// SPDX-License-Identifier: GPL-3.0-only

using JetBrains.Annotations;

namespace YamlWarrior.Robust.TypeInfo;

[PublicAPI]
public record DataDefinitionInfo {
    /// <summary>
    /// The full name of the type
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    /// Documentation
    /// </summary>
    public string Docs { get; init; } = "";

    /// <summary>
    /// Member data fields
    /// </summary>
    public DataFieldInfo[] DataFields { get; init; } = [];
}

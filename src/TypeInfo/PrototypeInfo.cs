// SPDX-FileCopyrightText: (C) 2026 dffdff2423 <dffdff2423@gmail.com>
//
// SPDX-License-Identifier: GPL-3.0-only

using JetBrains.Annotations;

namespace YamlWarrior.Robust.TypeInfo;

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
}

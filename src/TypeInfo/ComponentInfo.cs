// SPDX-FileCopyrightText: (C) 2026 dffdff2423 <dffdff2423@gmail.com>
//
// SPDX-License-Identifier: GPL-3.0-only

namespace YamlWarrior.Robust.TypeInfo;

public sealed record ComponentInfo : DataDefinitionInfo {
    /// <summary>
    /// Name of the component in yaml. In RT it is called PrototypeName, but I hate it, so I called it this instead.
    /// </summary>
    public required string YamlName { get; set; }

    /// <summary>
    /// Whether this component is serialized/de-serialized from entities. Probably show a warning to users if they try
    /// to add it in yaml.
    /// </summary>
    public bool Unsaved { get; set; }
}

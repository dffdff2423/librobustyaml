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

    /// <summary>
    /// If this component has a seperate client/server version. If true <see cref="DataDefinitionInfo.FullName"/> is
    /// arbitrary and <see cref="DataDefinitionInfo.Docs"/> is set to the shared docs until I come up with a better
    /// solution for merging them.
    /// </summary>
    public bool Predicted { get; set; }

    /// <summary>
    /// Full name for shared component. Only set if <see cref="Predicted"/> is true
    /// </summary>
    public string? SharedFullName { get; set; }

    /// <summary>
    /// Full name for server component. Only set if <see cref="Predicted"/> is true
    /// </summary>
    public string? ServerFullName { get; set; }

    /// <summary>
    /// Full name for client component. Only set if <see cref="Predicted"/> is true
    /// </summary>
    public string? ClientFullName { get; set; }
}

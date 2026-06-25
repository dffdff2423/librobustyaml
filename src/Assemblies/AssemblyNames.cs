// SPDX-FileCopyrightText: (C) 2026 dffdff2423 <dffdff2423@gmail.com>
//
// SPDX-License-Identifier: GPL-3.0-only

namespace YamlWarrior.Robust.Assemblies;

// TODO: this should be in some kind of config file
public static class AssemblyNames {
    public const string RobustSharedPath = "Content.Server/Robust.Shared.dll";

    public static readonly string[] DefaultContentAssemblyPathSegments = [
        "Content.Client/Content.Client.dll",
        "Content.Client/Robust.Client.dll",
        "Content.Server/Content.Server.dll",
        "Content.Server/Content.Shared.dll",
        "Content.Server/Robust.Server.dll",
        "Content.Server/Robust.Shared.Maths.dll",
        "Content.Server/Robust.Shared.dll",
    ];
}

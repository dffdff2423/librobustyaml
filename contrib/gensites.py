#!/usr/bin/env python3
# SPDX-FileCopyrightText: (C) 2025 dffdff2423 <dffdff2423@gmail.com>
#
# SPDX-License-Identifier: GPL-3.0-only

##### WARNING: THIS SCRIPT IS INTENDED TO BE RUN IN CI #####

import subprocess
from pathlib import Path

ROOT = Path.cwd();

projects = {
    "funky": "funky-station/forky-station",
    "starlight": "ss14Starlight/space-station-14",
    "cd": "cosmatic-drift-14/cosmatic-drift",
    "deltav": "DeltaV-Station/Delta-v",
    "wizden": "space-wizards/space-station-14",
};

yamldocs = ROOT / "bin" / "Yamldocs" / "net10.0" / "Yamldocs";

html_suffix = "<ul>";

for ident, github_repo in projects.items():
    repo_dir = ROOT / ident;

    # note that github actions is a pile of shit and displays subprocess output out of order
    print(f'::group::Clone {ident}');
    clone = ["git", "clone", "--recurse-submodules", "--depth", "1", f"https://github.com/{github_repo}.git", str(repo_dir) ];
    print(clone);
    subprocess.run(clone, check=True);

    print(f'::group::Build {ident}');
    # delete buildchecker since it causes issues with out github actions setup
    sed = ['sed', '-i', '/BuildChecker/,+2d', f'{repo_dir}/SpaceStation14.slnx'];
    print(sed);
    subprocess.run(sed, check=True);
    builddocs = ["dotnet", "build", "-p:GenerateDocumentationFile=true", "-p:WarningLevel=0"];
    print(builddocs);
    subprocess.run(builddocs, cwd=repo_dir, check=True);

    print(f'::group::Yamldocs {ident}');
    commit = subprocess.check_output( ["git", "rev-parse", "HEAD"], cwd=repo_dir, text=True).strip();

    runyamldocs = [str(yamldocs), "-a", str(repo_dir / "bin"), "-g", github_repo, "-c", commit, "-o", str(ROOT / "outsite" / ident)];
    print(runyamldocs)
    subprocess.run(runyamldocs, check=True);

    html_suffix = html_suffix + f"<li><a href=\"./{ident}/index.html\">{github_repo}</a>";

print(f'::group::Write index page');
with open("contrib/selectorpage-header.html", "r") as f:
    content = f.read();
    with open("outsite/index.html", "w") as o:
        o.write(content + html_suffix + "</body></html>");

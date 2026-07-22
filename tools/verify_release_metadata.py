#!/usr/bin/env python3
"""Validate release metadata and pinned build inputs without third-party packages."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET


SEMVER = re.compile(r"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$")
PINNED_ACTION = re.compile(r"^\s*-\s+uses:\s+[^\s@]+@([0-9a-f]{40})(?:\s+#\s+\S.*)?$")
EXPECTED_SDK = "10.0.302"
EXPECTED_RUNTIME = "10.0.10"
EXPECTED_LICENSE_HASHES = {
    "licenses/dotnet/10.0.10/LICENSE.txt": "cbff440b0d8166de75b5dae0e18498c869b3521974b37df7c7093a349e560b3a",
    "licenses/dotnet/10.0.10/THIRD-PARTY-NOTICES.txt": "a57da3e7e708320c506c955fd265d045be3e195e1ca6e78d9e5e52d873a22d28",
    "licenses/wpf/10.0.10/LICENSE.txt": "cfc21f5e8bd655ae997eec916138b707b1d290b83272c02a95c9f821b8c87310",
    "licenses/wpf/10.0.10/THIRD-PARTY-NOTICES.txt": "0c7ccb28a96f8708de372ca37b883443abbda7db88697fb45fa5f31dae80a404",
}


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def property_value(root: ET.Element, name: str) -> str:
    values = [node.text.strip() for node in root.findall(f".//{name}") if node.text]
    require(len(values) == 1, f"Directory.Build.props must define {name} exactly once")
    return values[0]


def verify(root: Path, expected_tag: str | None) -> str:
    properties = ET.parse(root / "Directory.Build.props").getroot()
    version = property_value(properties, "Version")
    require(SEMVER.fullmatch(version) is not None, f"invalid central version: {version}")
    numeric = version.split("-", 1)[0].split("+", 1)[0]
    require(property_value(properties, "AssemblyVersion") == f"{numeric}.0", "AssemblyVersion is out of sync")
    require(property_value(properties, "FileVersion") == f"{numeric}.0", "FileVersion is out of sync")
    require(property_value(properties, "InformationalVersion") == version, "InformationalVersion is out of sync")
    require(property_value(properties, "IncludeSourceRevisionInInformationalVersion") == "false",
            "InformationalVersion must remain identical to the release version")
    require(property_value(properties, "RuntimeFrameworkVersion") == EXPECTED_RUNTIME, "runtime version is not pinned")
    if expected_tag is not None:
        require(expected_tag == f"v{version}", f"tag {expected_tag} does not match source version {version}")

    global_json = json.loads(read(root / "global.json"))
    require(global_json["sdk"]["version"] == EXPECTED_SDK, "global.json SDK is not pinned")
    require(global_json["sdk"].get("rollForward") == "disable", "global.json must disable SDK roll-forward")

    project = ET.parse(root / "src/NaiWaPet/NaiWaPet.csproj").getroot()
    for property_name in ("Version", "AssemblyVersion", "FileVersion", "InformationalVersion"):
        require(project.find(f".//{property_name}") is None, f"{property_name} must not be duplicated in NaiWaPet.csproj")
    manifest = ET.parse(root / "src/NaiWaPet/app.manifest").getroot()
    identity = manifest.find("{urn:schemas-microsoft-com:asm.v1}assemblyIdentity")
    require(identity is not None and identity.get("version") == f"{numeric}.0", "Windows manifest version is out of sync")

    readme = read(root / "README.md")
    expected_names = (
        f"NaiWaPet-{version}-win-x64.exe",
        f"NaiWaPet-{version}-win-x64.exe.sha256",
        f"NaiWaPet-{version}-win-x64-portable.zip",
        f"NaiWaPet-{version}-win-x64-portable.zip.sha256",
    )
    require(f"releases/tag/v{version}" in readme, "README release link is out of sync")
    require(f"当前应用版本为 **{version}**" in readme, "README current version is out of sync")
    for name in expected_names:
        require(name in readme, f"README does not document release file {name}")

    changelog = read(root / "CHANGELOG.md")
    require(re.search(rf"^## \[{re.escape(version)}\] - \d{{4}}-\d{{2}}-\d{{2}}$", changelog, re.MULTILINE) is not None,
            "CHANGELOG does not contain the current release and date")
    notes = root / "docs/releases" / f"{version}.md"
    require(notes.is_file() and notes.stat().st_size > 0, f"missing release notes: {notes.relative_to(root)}")

    package_script = read(root / "package.ps1")
    require('Directory.Build.props' in package_script, "package.ps1 must read the central version")
    require('[string]$Version = ' not in package_script, "package.ps1 must not define a default version")

    workflows = sorted((root / ".github/workflows").glob("*.yml"))
    require(workflows, "no GitHub Actions workflows found")
    action_count = 0
    for workflow in workflows:
        for line_number, line in enumerate(read(workflow).splitlines(), start=1):
            if re.match(r"^\s*-\s+uses:", line):
                action_count += 1
                require(PINNED_ACTION.fullmatch(line) is not None,
                        f"action is not pinned to a full SHA: {workflow.relative_to(root)}:{line_number}")
    require(action_count > 0, "no GitHub Actions dependencies found")

    for relative, expected_hash in EXPECTED_LICENSE_HASHES.items():
        path = root / relative
        require(path.is_file(), f"missing official license file: {relative}")
        require(sha256(path) == expected_hash, f"official license file changed unexpectedly: {relative}")

    return version


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--tag", help="expected release tag, for example v1.0.1")
    arguments = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    try:
        version = verify(root, arguments.tag)
    except (ET.ParseError, OSError, KeyError, TypeError, ValueError, json.JSONDecodeError) as error:
        print(f"release metadata verification failed: {error}", file=sys.stderr)
        return 1
    print(f"verified release metadata for v{version}, .NET SDK {EXPECTED_SDK}, runtime {EXPECTED_RUNTIME}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

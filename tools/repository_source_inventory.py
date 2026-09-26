"""Classify tracked repository paths from Git metadata, without reading file payloads.

This is a repository-plane inventory, not proof of per-symbol business ownership,
reference distribution rights, or permission to remove tracked files.
"""

from __future__ import annotations

import hashlib
import json
from pathlib import Path
import subprocess
import sys


ROOT = Path(__file__).resolve().parents[1]
PLANES = frozenset({"source", "content", "tests", "tools", "scripts", "docs", "references", "design"})
SOURCE_ROOTS = frozenset({
    "AnimusForge.Bootstrap", "AnimusForge.SiegeAftermathIntervention", "PolicySystem",
    "Properties", "Refactor", "UI", "WarStats", "WorldEvents", "extensions", "src",
})
CACHE_ROOTS = frozenset({".codex_tmp", ".dotnet", ".dotnet_cli", ".tmp", "tmp"})
REFERENCE_ROOTS = frozenset({"原版游戏本体代码1.3.x", "原版游戏本体代码1.4.5"})
SAFE_UNTRACKED_GROUPS = frozenset({"bin", "local", "tools", "artifacts", "obj", ".tmp"})
LOCAL_ONLY_HANDOFF = "docs/handoffs/2026-09-11-native-history-snapshot-team-handoff.md"


def classify_path(path: str) -> str | None:
    """Return one plane or a named HOLD; unknown paths have no default plane."""
    parts = path.split("/")
    top = parts[0]
    name = parts[-1]
    suffix = Path(name).suffix.lower()
    lower_name = name.lower()

    # Sensitive and generated entries must win over extension-based source rules.
    if lower_name == "local.settings.json" or path == ".claude/settings.local.json":
        return "HOLD:local-settings"
    if any(part.lower() in {"browser", "browsers", "browser-data", "browser-profile",
                            "chrome", "firefox", "edge", "user data", "userdata"}
           for part in parts[:-1]):
        return "HOLD:user-data"
    if path == LOCAL_ONLY_HANDOFF:
        return "HOLD:local-only-handoff"
    if top in REFERENCE_ROOTS:
        return "HOLD:reference-provenance"
    if path.startswith("AnimusForge/PlayerExports/"):
        return "HOLD:user-data"
    if path.startswith("AnimusForge/ONNX/"):
        return "HOLD:model-provenance"
    if top == "_deps_auto":
        return "HOLD:dependency-provenance"
    if top in CACHE_ROOTS:
        return "HOLD:tracked-cache"
    if top == "Phase0_Local_Archive":
        return "HOLD:archive"
    if top == "_DeveloperPatch":
        return "HOLD:local-patch"
    if top == "animusforge-policy-effect-module-skill-draft":
        return "HOLD:skill-draft"
    if path.startswith("AnimusForge/AssetPackages/"):
        return "HOLD:asset-package-provenance"
    if top == "extensions" and "AssetPackages" in parts:
        return "HOLD:asset-package-provenance"
    if top == "tools" and path.startswith("tools/PlayerExportsEditor/dist/"):
        return "HOLD:tool-distribution"
    if top == "tools" and any(part.lower() in {"dist", "runs", "bin", "obj", ".generated"} for part in parts[2:-1]):
        return "HOLD:tool-output"
    if (len(parts) == 1 or top == "tools") and suffix in {".log", ".jsonl", ".trn"}:
        return "HOLD:run-log"
    if top in SOURCE_ROOTS and any(part.lower() in {"bin", "obj"} for part in parts[1:-1]):
        return "HOLD:build-output"

    if top == "AnimusForge":
        if len(parts) < 2:
            return None
        if parts[1] == "AssetSources":
            return "design"
        if parts[1] in {"CustomPrompts", "GUI", "ModuleData"}:
            return "content"
        if len(parts) == 2 and name in {"SubModule.xml", "VoiceMapping.json"}:
            return "content"
        return None
    if top == "tests":
        return "tests"
    if top == "tools":
        if len(parts) == 2:
            if name.endswith("Tests.py") or (name.startswith("test_") and suffix == ".py"):
                return "tests"
            return "scripts" if suffix in {".py", ".ps1"} else None
        if parts[1].endswith(("Tests", "SmokeTests")) or name.startswith("test_"):
            return "tests"
        return "tools"
    if top == "docs":
        return "tests" if len(parts) > 2 and parts[1] == "fixtures" else "docs"
    if top in {".agents", ".claude"}:
        return "docs"
    if top == "一键编译覆盖推送":
        return "scripts"
    if top == "PNG":
        return "design"
    if top in SOURCE_ROOTS:
        if suffix in {".md", ".txt"}:
            return "docs"
        if suffix in {".json", ".xml", ".png", ".mbproj"}:
            return "content"
        if suffix in {".cs", ".csproj", ".sln", ".slnx", ".props", ".targets"}:
            return "source"
        if suffix in {".py", ".ps1", ".cmd", ".bat"}:
            return "scripts"
        return None
    if len(parts) != 1:
        return None
    if suffix == ".cs":
        return "source"
    if suffix in {".csproj", ".sln"} or name in {".editorconfig", ".gitignore"}:
        return "source"
    if suffix == ".md":
        return "docs"
    if suffix in {".py", ".ps1", ".cmd", ".bat"}:
        return "scripts"
    if suffix == ".png":
        return "design"
    if suffix in {".zip", ".rar", ".7z"}:
        return "HOLD:archive"
    if suffix == ".html":
        return "HOLD:diagnostic"
    if suffix == ".lnk":
        return "HOLD:external-link"
    if suffix == ".lscache":
        return "HOLD:tracked-cache"
    if ".broken-backup-" in name:
        return "HOLD:backup"
    return None


def git_output(root: Path, *arguments: str, input_text: str | None = None) -> bytes | str:
    command = ["git", *arguments]
    result = subprocess.run(
        command, cwd=root, input=input_text, capture_output=True, check=True,
        text=input_text is not None, encoding="ascii" if input_text is not None else None,
    )
    return result.stdout


def index_entries(root: Path) -> list[tuple[str, str]]:
    raw = git_output(root, "ls-files", "--stage", "-z")
    entries: list[tuple[str, str]] = []
    seen: set[str] = set()
    for row in raw.split(b"\0"):
        if not row:
            continue
        metadata, filename = row.split(b"\t", 1)
        mode, oid, stage = metadata.split()
        path = filename.decode("utf-8", "surrogateescape")
        if stage != b"0" or path in seen or mode == b"160000":
            raise ValueError("Unmerged, duplicate, or submodule index entry")
        seen.add(path)
        entries.append((path, oid.decode("ascii")))
    return entries


def blob_sizes(root: Path, object_ids: list[str]) -> dict[str, int]:
    unique_ids = list(dict.fromkeys(object_ids))
    if not unique_ids:
        return {}
    raw = git_output(root, "cat-file", "--batch-check=%(objectname) %(objecttype) %(objectsize)",
                     input_text="\n".join(unique_ids) + "\n")
    sizes: dict[str, int] = {}
    for line in raw.splitlines():
        oid, object_type, size = line.split()
        if object_type != "blob":
            raise ValueError("Tracked entry is not a blob")
        sizes[oid] = int(size)
    if set(sizes) != set(unique_ids):
        raise ValueError("Incomplete Git object-size response")
    return sizes


def count_paths(root: Path, *arguments: str) -> int:
    raw = git_output(root, "ls-files", *arguments, "-z")
    return sum(bool(item) for item in raw.split(b"\0"))


def path_group_counts(root: Path, *arguments: str) -> dict[str, int]:
    raw = git_output(root, "ls-files", *arguments, "-z")
    counts: dict[str, int] = {}
    for item in raw.split(b"\0"):
        if not item:
            continue
        top = item.split(b"/", 1)[0].decode("utf-8", "surrogateescape")
        group = top if top in SAFE_UNTRACKED_GROUPS else "other"
        counts[group] = counts.get(group, 0) + 1
    return dict(sorted(counts.items()))


def build_report(root: Path = ROOT) -> dict:
    entries = index_entries(root)
    sizes = blob_sizes(root, [oid for _, oid in entries])
    categories: dict[str, int] = {}
    category_bytes: dict[str, int] = {}
    unknown_count = 0
    digest = hashlib.sha256()
    for path, oid in sorted(entries):
        category = classify_path(path)
        if category is None:
            unknown_count += 1
            category = "UNKNOWN"
        if category not in PLANES and not category.startswith("HOLD:") and category != "UNKNOWN":
            raise ValueError("Invalid inventory category")
        size = sizes[oid]
        categories[category] = categories.get(category, 0) + 1
        category_bytes[category] = category_bytes.get(category, 0) + size
        digest.update(path.encode("utf-8", "surrogateescape") + b"\0" + category.encode("ascii") + b"\0" + str(size).encode("ascii") + b"\n")
    untracked_groups = path_group_counts(root, "--others", "--exclude-standard")
    ignored_groups = path_group_counts(root, "--others", "--ignored", "--exclude-standard")
    return {
        "tracked_count": len(entries),
        "categories": dict(sorted(categories.items())),
        "bytes": dict(sorted(category_bytes.items())),
        "unknown_count": unknown_count,
        "untracked_count": sum(untracked_groups.values()),
        "untracked_groups": untracked_groups,
        "ignored_untracked_count": sum(ignored_groups.values()),
        "ignored_untracked_groups": ignored_groups,
        "tracked_ignored_count": count_paths(root, "--cached", "--ignored", "--exclude-standard"),
        "inventory_sha256": digest.hexdigest(),
        "business_owner_state": "UNVERIFIED; source plane includes transitional owners",
    }


def main(arguments: list[str] | None = None) -> int:
    if arguments:
        raise SystemExit("No arguments are supported")
    try:
        report = build_report()
    except (OSError, subprocess.CalledProcessError, ValueError) as exc:
        print(json.dumps({"status": "FAIL", "reason": type(exc).__name__}, sort_keys=True))
        return 1
    print(json.dumps({"status": "PASS" if report["unknown_count"] == 0 else "FAIL", **report},
                     ensure_ascii=False, sort_keys=True))
    return 0 if report["unknown_count"] == 0 else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))

"""Explicit, byte-preserving AF2 user-data migration; never deletes legacy data.

Private manifests (including filenames) stay under AFDataRoot/Recovery. Console output
contains counts only. This tool is not a startup hook and does not touch game saves.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import uuid

if os.name == "nt":
    import msvcrt
else:
    import fcntl


CHUNK = 1024 * 1024
REPO = Path(__file__).resolve().parents[1]


def _hash(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(CHUNK), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _reparse(path: Path) -> bool:
    if not path.exists() and not path.is_symlink():
        return False
    stat = path.lstat()
    return path.is_symlink() or bool(getattr(stat, "st_file_attributes", 0) & 0x400)


def _check_ancestors(path: Path) -> None:
    for part in (path, *path.parents):
        if _reparse(part):
            raise RuntimeError("A migration path crosses a reparse point")


def _safe_root(root: Path, allow_test_root: bool) -> Path:
    if not root.is_absolute():
        raise ValueError("AF data root must be absolute")
    root = Path(os.path.abspath(root))
    _check_ancestors(root)
    if root == Path(root.anchor) or any(part.lower() == "single_module_stage" for part in root.parts):
        raise ValueError("Unsafe AF data root")
    if not allow_test_root:
        for parent in (root, *root.parents):
            if (parent / "AnimusForge.csproj").is_file() or (parent / "SubModule.xml").is_file():
                raise ValueError("AF data root cannot be inside source or a module")
        local = os.environ.get("LOCALAPPDATA")
        if not local or root != Path(os.path.abspath(Path(local) / "AnimusForge")):
            raise ValueError("This migration CLI only activates the default LocalAppData/AnimusForge root")
    return root


def _snapshot(sources: list[tuple[str, Path]], root: Path) -> list[dict]:
    entries: list[dict] = []
    seen_ids: set[str] = set()
    for source_id, module in sources:
        if not source_id or any(c not in "abcdefghijklmnopqrstuvwxyz0123456789-_" for c in source_id):
            raise ValueError("Source identity must be a simple non-private label")
        if source_id in seen_ids:
            raise ValueError("Duplicate source identity")
        seen_ids.add(source_id)
        module = Path(os.path.abspath(module))
        _check_ancestors(module)
        if root == module or root in module.parents or module in root.parents:
            raise ValueError("Migration source and data root must be separate")
        base = module / "PlayerExports"
        if not base.is_dir():
            continue
        _check_ancestors(base)
        casefolded: set[str] = set()
        for directory, dirs, files in os.walk(base, followlinks=False):
            directory_path = Path(directory)
            _check_ancestors(directory_path)
            dirs.sort()
            files.sort()
            if directory_path == base and any(name.startswith(".af-export-") for name in dirs):
                raise RuntimeError("Unresolved export candidate or retired package requires recovery before migration")
            for name in dirs + files:
                if ":" in name or name in (".", ".."):
                    raise ValueError("Invalid source entry")
                if _reparse(directory_path / name):
                    raise RuntimeError("Source contains a reparse point")
            for name in sorted(files):
                path = directory_path / name
                if not path.is_file():
                    raise RuntimeError("Source contains a non-file entry")
                relative = path.relative_to(base).as_posix()
                folded = relative.casefold()
                if folded in casefolded:
                    raise RuntimeError("Case-insensitive source path collision")
                casefolded.add(folded)
                stat = path.stat()
                package = base / relative.split("/", 1)[0]
                entries.append({"source": source_id, "relative": relative,
                                "sha256": _hash(path), "size": stat.st_size,
                                "mtimeNs": stat.st_mtime_ns,
                                "packageMtimeNs": package.stat().st_mtime_ns})
    return entries


def _json_bytes(value: object) -> bytes:
    return json.dumps(value, ensure_ascii=False, sort_keys=True, separators=(",", ":")).encode("utf-8")


def _write_new_verified(source: Path, destination: Path, expected: dict) -> None:
    _check_ancestors(destination)
    if destination.exists():
        if destination.stat().st_size != expected["size"] or _hash(destination) != expected["sha256"]:
            raise RuntimeError("Existing private backup/candidate differs; manual recovery required")
        return
    destination.parent.mkdir(parents=True, exist_ok=True)
    _check_ancestors(destination.parent)
    # Keep the same-volume candidate name short: appending a GUID to a long
    # player filename can exceed Windows MAX_PATH even when the final path fits.
    temporary = destination.parent / (".afp-" + uuid.uuid4().hex)
    try:
        with source.open("rb") as read, temporary.open("xb") as write:
            shutil.copyfileobj(read, write, CHUNK)
            write.flush()
            os.fsync(write.fileno())
        if temporary.stat().st_size != expected["size"] or _hash(temporary) != expected["sha256"]:
            raise RuntimeError("Private copy hash/size verification failed")
        os.utime(temporary, ns=(expected["mtimeNs"], expected["mtimeNs"]))
        os.rename(temporary, destination)
    finally:
        # Failed partial copies are deliberately retained in private Recovery.
        pass


def _write_record(path: Path, value: dict) -> None:
    _check_ancestors(path)
    raw = _json_bytes(value)
    temporary = path.with_name(path.name + ".pending." + uuid.uuid4().hex)
    with temporary.open("xb") as stream:
        stream.write(raw)
        stream.flush()
        os.fsync(stream.fileno())
    os.replace(temporary, path)


def _source_key(module: Path) -> str:
    normalized = str(Path(os.path.abspath(module))).rstrip("\\/").upper()
    return hashlib.sha256(normalized.encode("utf-8")).hexdigest()


def _mark_sources_ready(root: Path, sources: list[tuple[str, Path]], manifest_hash: str) -> None:
    if not sources:
        return
    marker = root / "UserData" / ".player-exports-ready.json"
    _check_ancestors(marker)
    marker.parent.mkdir(parents=True, exist_ok=True)
    if marker.exists():
        record = json.loads(marker.read_text(encoding="utf-8"))
        if record.get("schema") != 1 or not isinstance(record.get("sources"), dict):
            raise RuntimeError("Invalid private migration readiness record")
    else:
        record = {"schema": 1, "sources": {}}
    for _, path in sources:
        record["sources"][_source_key(path)] = manifest_hash
    _write_record(marker, record)


def _lock_stream(stream) -> None:
    stream.seek(0)
    if os.name == "nt":
        msvcrt.locking(stream.fileno(), msvcrt.LK_NBLCK, 1)
    else:
        fcntl.flock(stream.fileno(), fcntl.LOCK_EX | fcntl.LOCK_NB)


def _unlock_stream(stream) -> None:
    stream.seek(0)
    if os.name == "nt":
        msvcrt.locking(stream.fileno(), msvcrt.LK_UNLCK, 1)
    else:
        fcntl.flock(stream.fileno(), fcntl.LOCK_UN)


def migrate(sources: list[tuple[str, Path]], root: Path, *, allow_test_root: bool = False,
            hook=None) -> dict:
    """Backup then activate absent files; existing active files always win.

    Source order sets priority. No source, target, or backup file is removed.
    A failed/interrupted run has no completion marker and is safe to resume.
    """
    root = _safe_root(Path(root), allow_test_root)
    normalized = [(label, Path(os.path.abspath(path))) for label, path in sources]
    entries = _snapshot(normalized, root)
    if not entries:
        return {"backed_up": 0, "activated": 0, "conflicts": 0, "already_complete": False}
    existing = root
    while not existing.exists():
        existing = existing.parent
    required_bytes = sum(entry["size"] for entry in entries) * 2 + 64 * 1024 * 1024
    if shutil.disk_usage(existing).free < required_bytes:
        raise RuntimeError("Insufficient free space for verified backup and candidate")
    manifest = {"schema": 1, "sources": [{"id": label, "path": str(path)} for label, path in normalized],
                "files": entries}
    manifest_hash = hashlib.sha256(_json_bytes(manifest)).hexdigest()
    recovery = root / "Recovery" / ("player-exports-" + manifest_hash[:24])
    _check_ancestors(recovery)
    root.mkdir(parents=True, exist_ok=True)
    (root / "Recovery").mkdir(exist_ok=True)
    recovery.mkdir(exist_ok=True)
    lock = root / "Recovery" / ".player-exports-migration.lock"
    lock_stream = lock.open("a+b")
    acquired_lock = False
    try:
        _lock_stream(lock_stream)
        acquired_lock = True
        result = {"backed_up": len(entries), "activated": 0, "conflicts": 0,
                  "already_complete": False, "recovery": str(recovery), "manifest_sha256": manifest_hash}
        source_by_id = dict(normalized)
        for entry in entries:
            source = source_by_id[entry["source"]] / "PlayerExports" / entry["relative"]
            backup = recovery / "sources" / entry["source"] / "PlayerExports" / entry["relative"]
            _write_new_verified(source, backup, entry)
        manifest_file = recovery / "manifest.json"
        if manifest_file.exists():
            if _hash(manifest_file) != manifest_hash:
                raise RuntimeError("Private manifest differs; manual recovery required")
        else:
            _write_record(manifest_file, manifest)
        if hook:
            hook("backed_up", len(entries))
        if _snapshot(normalized, root) != entries:
            raise RuntimeError("Source changed during migration; no activation performed")
        selected: dict[str, dict] = {}
        for entry in entries:
            folded = entry["relative"].casefold()
            if folded in selected:
                if selected[folded]["sha256"] != entry["sha256"]:
                    result["conflicts"] += 1
            else:
                selected[folded] = entry
        active = root / "UserData" / "PlayerExports"
        completed = recovery / "completed.json"
        if completed.exists():
            record = json.loads(completed.read_text(encoding="utf-8"))
            if record.get("manifestSha256") != manifest_hash:
                raise RuntimeError("Completion record differs; manual recovery required")
            result["already_complete"] = True
            result["conflicts"] = record["conflicts"]
            _mark_sources_ready(root, normalized, manifest_hash)
            return result
        activation_plan = recovery / "activation-plan.json"
        if activation_plan.exists():
            plan = json.loads(activation_plan.read_text(encoding="utf-8"))
            if plan.get("manifestSha256") != manifest_hash:
                raise RuntimeError("Activation plan differs; manual recovery required")
        else:
            new_packages = {}
            existing_packages = {}
            for entry in selected.values():
                package_name = entry["relative"].split("/", 1)[0]
                package_path = active / package_name
                if package_path.is_dir():
                    existing_packages[package_name] = package_path.stat().st_mtime_ns
                elif not package_path.exists():
                    new_packages[package_name] = entry["packageMtimeNs"]
                else:
                    raise RuntimeError("An active package path is not a directory")
            plan = {"schema": 1, "manifestSha256": manifest_hash,
                    "newPackages": new_packages, "existingPackages": existing_packages}
            _write_record(activation_plan, plan)
        for entry in selected.values():
            backup = recovery / "sources" / entry["source"] / "PlayerExports" / entry["relative"]
            candidate = recovery / "candidate" / "PlayerExports" / entry["relative"]
            _write_new_verified(backup, candidate, entry)
        for entry in selected.values():
            target = active / entry["relative"]
            _check_ancestors(target)
            if target.exists():
                if not target.is_file() or _hash(target) != entry["sha256"]:
                    result["conflicts"] += 1
                continue
            target.parent.mkdir(parents=True, exist_ok=True)
            _check_ancestors(target.parent)
            candidate = recovery / "candidate" / "PlayerExports" / entry["relative"]
            os.rename(candidate, target)  # Windows rename refuses an existing target.
            result["activated"] += 1
            if hook:
                hook("activated", result["activated"])
        for package_name, mtime_ns in {**plan["newPackages"], **plan["existingPackages"]}.items():
            package_path = active / package_name
            if package_path.is_dir():
                os.utime(package_path, ns=(mtime_ns, mtime_ns))
        _write_record(completed, {"schema": 1, "manifestSha256": manifest_hash,
                                  "activated": result["activated"], "conflicts": result["conflicts"]})
        _mark_sources_ready(root, normalized, manifest_hash)
        return result
    finally:
        if acquired_lock:
            _unlock_stream(lock_stream)
        lock_stream.close()


def _prompt_baselines() -> dict[str, str]:
    mapping = json.loads((REPO / "content" / "content-map.json").read_text(encoding="utf-8"))
    baselines: dict[str, str] = {}
    for entry in mapping["entries"]:
        target = entry["target"]
        if not target.startswith("CustomPrompts/"):
            continue
        relative = target[len("CustomPrompts/"):].casefold()
        source = REPO / entry["source"]
        _check_ancestors(source)
        if relative in baselines or not source.is_file():
            raise RuntimeError("Prompt baseline mapping is missing or ambiguous")
        expected = entry.get("sha256", "").lower()
        if len(expected) != 64 or any(c not in "0123456789abcdef" for c in expected) or _hash(source) != expected:
            raise RuntimeError("Prompt baseline hash lock differs from the content source")
        baselines[relative] = expected
    return baselines


def _snapshot_prompts(sources: list[tuple[str, Path]], root: Path,
                      baselines: dict[str, str]) -> list[dict]:
    entries: list[dict] = []
    seen_ids: set[str] = set()
    for source_id, module in sources:
        if not source_id or any(c not in "abcdefghijklmnopqrstuvwxyz0123456789-_" for c in source_id):
            raise ValueError("Source identity must be a simple non-private label")
        if source_id in seen_ids:
            raise ValueError("Duplicate source identity")
        seen_ids.add(source_id)
        module = Path(os.path.abspath(module))
        _check_ancestors(module)
        if root == module or root in module.parents or module in root.parents:
            raise ValueError("Migration source and data root must be separate")
        base = module / "CustomPrompts"
        if not base.is_dir():
            continue
        _check_ancestors(base)
        casefolded: set[str] = set()
        for directory, dirs, files in os.walk(base, followlinks=False):
            directory_path = Path(directory)
            _check_ancestors(directory_path)
            dirs.sort()
            files.sort()
            for name in dirs + files:
                if ":" in name or name in (".", ".."):
                    raise ValueError("Invalid prompt source entry")
                if _reparse(directory_path / name):
                    raise RuntimeError("Prompt source contains a reparse point")
            for name in files:
                path = directory_path / name
                if not path.is_file():
                    raise RuntimeError("Prompt source contains a non-file entry")
                relative = path.relative_to(base).as_posix()
                folded = relative.casefold()
                if folded in casefolded:
                    raise RuntimeError("Case-insensitive prompt source path collision")
                casefolded.add(folded)
                stat = path.stat()
                digest = _hash(path)
                entries.append({"source": source_id, "relative": relative,
                                "sha256": digest, "size": stat.st_size,
                                "mtimeNs": stat.st_mtime_ns,
                                "isDefault": digest == baselines.get(folded)})
    return entries


def _mark_prompts_ready(root: Path, sources: list[tuple[str, Path]], manifest_hash: str) -> None:
    marker = root / "UserData" / ".prompt-overrides-ready.json"
    _check_ancestors(marker)
    marker.parent.mkdir(parents=True, exist_ok=True)
    if marker.exists():
        record = json.loads(marker.read_text(encoding="utf-8"))
        if record.get("schema") != 1 or not isinstance(record.get("sources"), dict):
            raise RuntimeError("Invalid private prompt migration readiness record")
    else:
        record = {"schema": 1, "sources": {}}
    for _, path in sources:
        record["sources"][_source_key(path)] = manifest_hash
    _write_record(marker, record)


def migrate_prompts(sources: list[tuple[str, Path]], root: Path, *,
                    baselines: dict[str, str] | None = None,
                    allow_test_root: bool = False, hook=None) -> dict:
    """Back up all legacy Prompt files; activate only non-baseline JSON without overwrite."""
    root = _safe_root(Path(root), allow_test_root)
    normalized = [(label, Path(os.path.abspath(path))) for label, path in sources]
    known = _prompt_baselines() if baselines is None else baselines
    entries = _snapshot_prompts(normalized, root, known)
    if not entries:
        return {"backed_up": 0, "activated": 0, "conflicts": 0, "already_complete": False}
    existing = root
    while not existing.exists():
        existing = existing.parent
    if shutil.disk_usage(existing).free < sum(entry["size"] for entry in entries) * 2 + 64 * 1024 * 1024:
        raise RuntimeError("Insufficient free space for verified backup and candidate")
    manifest = {"schema": 1, "kind": "prompt-overrides",
                "sources": [{"id": label, "path": str(path)} for label, path in normalized],
                "files": entries}
    manifest_hash = hashlib.sha256(_json_bytes(manifest)).hexdigest()
    recovery = root / "Recovery" / ("prompt-overrides-" + manifest_hash[:24])
    _check_ancestors(recovery)
    root.mkdir(parents=True, exist_ok=True)
    (root / "Recovery").mkdir(exist_ok=True)
    recovery.mkdir(exist_ok=True)
    lock = root / "Recovery" / ".prompt-overrides-migration.lock"
    lock_stream = lock.open("a+b")
    acquired_lock = False
    try:
        _lock_stream(lock_stream)
        acquired_lock = True
        result = {"backed_up": len(entries), "activated": 0, "conflicts": 0,
                  "already_complete": False, "recovery": str(recovery),
                  "manifest_sha256": manifest_hash}
        source_by_id = dict(normalized)
        for entry in entries:
            source = source_by_id[entry["source"]] / "CustomPrompts" / entry["relative"]
            backup = recovery / "sources" / entry["source"] / "CustomPrompts" / entry["relative"]
            _write_new_verified(source, backup, entry)
        manifest_file = recovery / "manifest.json"
        if manifest_file.exists():
            if _hash(manifest_file) != manifest_hash:
                raise RuntimeError("Private manifest differs; manual recovery required")
        else:
            _write_record(manifest_file, manifest)
        if hook:
            hook("backed_up", len(entries))
        if _snapshot_prompts(normalized, root, known) != entries:
            raise RuntimeError("Prompt source changed during migration; no activation performed")
        selected: dict[str, dict] = {}
        for entry in entries:
            if entry["isDefault"] or not entry["relative"].lower().endswith(".json"):
                continue
            folded = entry["relative"].casefold()
            if folded in selected:
                if selected[folded]["sha256"] != entry["sha256"]:
                    result["conflicts"] += 1
            else:
                selected[folded] = entry
        completed = recovery / "completed.json"
        if completed.exists():
            record = json.loads(completed.read_text(encoding="utf-8"))
            if record.get("manifestSha256") != manifest_hash:
                raise RuntimeError("Completion record differs; manual recovery required")
            result["already_complete"] = True
            result["conflicts"] = record["conflicts"]
            _mark_prompts_ready(root, normalized, manifest_hash)
            return result
        for entry in selected.values():
            backup = recovery / "sources" / entry["source"] / "CustomPrompts" / entry["relative"]
            candidate = recovery / "candidate" / "CustomPrompts" / entry["relative"]
            _write_new_verified(backup, candidate, entry)
        active = root / "UserData" / "Overrides" / "CustomPrompts"
        for entry in selected.values():
            target = active / entry["relative"]
            _check_ancestors(target)
            if target.exists():
                if not target.is_file() or _hash(target) != entry["sha256"]:
                    result["conflicts"] += 1
                continue
            target.parent.mkdir(parents=True, exist_ok=True)
            _check_ancestors(target.parent)
            candidate = recovery / "candidate" / "CustomPrompts" / entry["relative"]
            os.rename(candidate, target)
            result["activated"] += 1
            if hook:
                hook("activated", result["activated"])
        _write_record(completed, {"schema": 1, "manifestSha256": manifest_hash,
                                  "activated": result["activated"], "conflicts": result["conflicts"]})
        _mark_prompts_ready(root, normalized, manifest_hash)
        return result
    finally:
        if acquired_lock:
            _unlock_stream(lock_stream)
        lock_stream.close()


def _snapshot_terminal_settings(sources: list[tuple[str, Path]], root: Path) -> list[dict]:
    entries: list[dict] = []
    seen_ids: set[str] = set()
    for source_id, module in sources:
        if not source_id or any(c not in "abcdefghijklmnopqrstuvwxyz0123456789-_" for c in source_id):
            raise ValueError("Source identity must be a simple non-private label")
        if source_id in seen_ids:
            raise ValueError("Duplicate source identity")
        seen_ids.add(source_id)
        module = Path(os.path.abspath(module))
        _check_ancestors(module)
        if root == module or root in module.parents or module in root.parents:
            raise ValueError("Migration source and data root must be separate")
        path = module / "ModuleData" / "TerminalSettings.json"
        _check_ancestors(path)
        if not path.exists():
            continue
        if not path.is_file():
            raise RuntimeError("Legacy TerminalSettings is not a regular file")
        stat = path.stat()
        try:
            value = json.loads(path.read_text(encoding="utf-8-sig"))
            valid = isinstance(value, dict) and all(
                key not in value or isinstance(value[key], bool)
                for key in ("IsHotkeyEnabled", "IsMapIconEnabled"))
        except (UnicodeError, ValueError):
            valid = False
        entries.append({"source": source_id, "relative": "TerminalSettings.json",
                        "sha256": _hash(path), "size": stat.st_size,
                        "mtimeNs": stat.st_mtime_ns, "valid": valid})
    return entries


def migrate_terminal_settings(sources: list[tuple[str, Path]], root: Path, *,
                              allow_test_root: bool = False, hook=None) -> dict:
    """Explicitly back up legacy TerminalSettings and activate only valid non-conflicting bytes."""
    root = _safe_root(Path(root), allow_test_root)
    normalized = [(label, Path(os.path.abspath(path))) for label, path in sources]
    entries = _snapshot_terminal_settings(normalized, root)
    if not entries:
        return {"backed_up": 0, "activated": 0, "conflicts": 0, "already_complete": False}
    existing = root
    while not existing.exists():
        existing = existing.parent
    if shutil.disk_usage(existing).free < sum(entry["size"] for entry in entries) * 2 + 64 * 1024 * 1024:
        raise RuntimeError("Insufficient free space for verified backup and candidate")
    manifest = {"schema": 1, "kind": "terminal-settings",
                "sources": [{"id": label, "path": str(path)} for label, path in normalized],
                "files": entries}
    manifest_hash = hashlib.sha256(_json_bytes(manifest)).hexdigest()
    recovery = root / "Recovery" / ("terminal-settings-" + manifest_hash[:24])
    _check_ancestors(recovery)
    root.mkdir(parents=True, exist_ok=True)
    (root / "Recovery").mkdir(exist_ok=True)
    recovery.mkdir(exist_ok=True)
    lock_stream = (root / "Recovery" / ".terminal-settings-migration.lock").open("a+b")
    acquired_lock = False
    try:
        _lock_stream(lock_stream)
        acquired_lock = True
        result = {"backed_up": len(entries), "activated": 0, "conflicts": 0,
                  "already_complete": False, "recovery": str(recovery),
                  "manifest_sha256": manifest_hash}
        source_by_id = dict(normalized)
        for entry in entries:
            source = source_by_id[entry["source"]] / "ModuleData" / "TerminalSettings.json"
            backup = recovery / "sources" / entry["source"] / "TerminalSettings.json"
            _write_new_verified(source, backup, entry)
        manifest_file = recovery / "manifest.json"
        if manifest_file.exists():
            if _hash(manifest_file) != manifest_hash:
                raise RuntimeError("Private manifest differs; manual recovery required")
        else:
            _write_record(manifest_file, manifest)
        if hook:
            hook("backed_up", len(entries))
        if _snapshot_terminal_settings(normalized, root) != entries:
            raise RuntimeError("TerminalSettings source changed during migration; no activation performed")
        completed = recovery / "completed.json"
        if completed.exists():
            record = json.loads(completed.read_text(encoding="utf-8"))
            if record.get("manifestSha256") != manifest_hash:
                raise RuntimeError("Completion record differs; manual recovery required")
            result["already_complete"] = True
            result["conflicts"] = record["conflicts"]
            return result
        selected = next((entry for entry in entries if entry["valid"]), None)
        if selected is not None:
            backup = recovery / "sources" / selected["source"] / "TerminalSettings.json"
            candidate = recovery / "candidate" / "TerminalSettings.json"
            _write_new_verified(backup, candidate, selected)
            target = root / "UserData" / "Settings" / "TerminalSettings.json"
            _check_ancestors(target)
            if target.exists():
                if not target.is_file() or _hash(target) != selected["sha256"]:
                    result["conflicts"] += 1
            else:
                target.parent.mkdir(parents=True, exist_ok=True)
                _check_ancestors(target.parent)
                os.rename(candidate, target)
                result["activated"] = 1
                if hook:
                    hook("activated", 1)
            result["conflicts"] += sum(
                entry["valid"] and entry["sha256"] != selected["sha256"]
                for entry in entries if entry is not selected)
        _write_record(completed, {"schema": 1, "manifestSha256": manifest_hash,
                                  "activated": result["activated"], "conflicts": result["conflicts"]})
        return result
    finally:
        if acquired_lock:
            _unlock_stream(lock_stream)
        lock_stream.close()


def _model_lock() -> dict:
    path = REPO / "content" / "models.lock.json"
    lock = json.loads(path.read_text(encoding="utf-8"))
    if lock.get("schemaVersion") != 1 or set(lock.get("groups", {})) != {"embedding", "reranker"}:
        raise RuntimeError("Model dependency lock is incomplete")
    for group in lock["groups"].values():
        variants = group.get("variants")
        if not isinstance(variants, dict) or not variants:
            raise RuntimeError("Model dependency lock has no variants")
        for files in variants.values():
            if not isinstance(files, dict) or not files:
                raise RuntimeError("Model dependency lock has an empty file group")
            for name, expected in files.items():
                if (not name or name in (".", "..") or "/" in name or "\\" in name or ":" in name
                        or not isinstance(expected, dict) or not isinstance(expected.get("size"), int)
                        or expected["size"] < 0 or not isinstance(expected.get("sha256"), str)
                        or len(expected["sha256"]) != 64
                        or any(c not in "0123456789abcdef" for c in expected["sha256"])):
                    raise RuntimeError("Model dependency lock contains an invalid file")
    return lock


def _model_group_path(module: Path, group: str) -> Path:
    base = module / "ONNX"
    return base if group == "embedding" else base / "reranker"


def _snapshot_models(sources: list[tuple[str, Path]], root: Path, lock: dict) -> list[dict]:
    entries: list[dict] = []
    seen_ids: set[str] = set()
    for source_id, module in sources:
        if not source_id or any(c not in "abcdefghijklmnopqrstuvwxyz0123456789-_" for c in source_id):
            raise ValueError("Source identity must be a simple non-private label")
        if source_id in seen_ids:
            raise ValueError("Duplicate source identity")
        seen_ids.add(source_id)
        module = Path(os.path.abspath(module))
        _check_ancestors(module)
        if root == module or root in module.parents or module in root.parents:
            raise ValueError("Migration source and data root must be separate")
        base = module / "ONNX"
        if not base.exists():
            continue
        _check_ancestors(base)
        if not base.is_dir():
            raise RuntimeError("Legacy model root is not a directory")
        expected_root = set(lock["groups"]["embedding"]["variants"].get(source_id, {}))
        if source_id in lock["groups"]["reranker"]["variants"]:
            expected_root.add("reranker")
        if set(os.listdir(base)) != expected_root:
            raise RuntimeError("Legacy model root differs from the locked complete group")
        for group_name, group in lock["groups"].items():
            files = group["variants"].get(source_id)
            if files is None:
                continue
            folder = _model_group_path(module, group_name)
            _check_ancestors(folder)
            if not folder.is_dir() or (group_name != "embedding" and set(os.listdir(folder)) != set(files)):
                raise RuntimeError("Legacy model group differs from the lock")
            for name, expected in sorted(files.items()):
                path = folder / name
                _check_ancestors(path)
                if not path.is_file():
                    raise RuntimeError("Legacy model group has a missing or non-file member")
                stat = path.stat()
                digest = _hash(path)
                if stat.st_size != expected["size"] or digest != expected["sha256"]:
                    raise RuntimeError("Legacy model bytes differ from the dependency lock")
                entries.append({"source": source_id, "group": group_name, "relative": name,
                                "sha256": digest, "size": stat.st_size, "mtimeNs": stat.st_mtime_ns})
    return entries


def _snapshot_existing_model_group(folder: Path) -> list[dict]:
    _check_ancestors(folder)
    if not folder.is_dir():
        raise RuntimeError("Active model group is not a directory")
    entries: list[dict] = []
    for directory, dirs, files in os.walk(folder, followlinks=False):
        parent = Path(directory)
        _check_ancestors(parent)
        for name in dirs + files:
            _check_ancestors(parent / name)
        for name in files:
            path = parent / name
            if not path.is_file():
                raise RuntimeError("Active model group contains a non-file entry")
            stat = path.stat()
            entries.append({"relative": path.relative_to(folder).as_posix(), "sha256": _hash(path),
                            "size": stat.st_size, "mtimeNs": stat.st_mtime_ns})
    return sorted(entries, key=lambda item: item["relative"])


def _mark_models_ready(root: Path, locked: dict, lock_hash: str, manifest_hash: str) -> None:
    """Publish only groups whose complete active bytes still match one locked variant."""
    models = root / "Models"
    marker = models / ".af-models-ready.json"
    _check_ancestors(marker)
    if marker.exists():
        prior = json.loads(marker.read_text(encoding="utf-8"))
        if prior.get("schema") != 1 or not isinstance(prior.get("groups"), dict):
            raise RuntimeError("Invalid private model readiness record")
    groups: dict[str, dict] = {}
    for group_name, group in locked["groups"].items():
        folder = models / group_name
        if not folder.exists():
            continue
        current = _snapshot_existing_model_group(folder)
        current_files = {item["relative"]: (item["size"], item["sha256"]) for item in current}
        for variant_name, expected in group["variants"].items():
            if current_files == {name: (value["size"], value["sha256"]) for name, value in expected.items()}:
                groups[group_name] = {"variant": variant_name,
                                      "files": {item["relative"]: {
                                          "size": item["size"], "sha256": item["sha256"],
                                          "mtimeUtcTicks": item["mtimeNs"] // 100 + 621355968000000000
                                      } for item in current}}
                break
    _write_record(marker, {"schema": 1, "lockSha256": lock_hash,
                           "completionManifestSha256": manifest_hash, "groups": groups})


def migrate_models(sources: list[tuple[str, Path]], root: Path, *,
                   allow_test_root: bool = False, lock: dict | None = None, hook=None) -> dict:
    """Verify complete locked groups, retain source and active conflicts, then rename whole candidates."""
    root = _safe_root(Path(root), allow_test_root)
    locked = _model_lock() if lock is None else lock
    normalized = [(label, Path(os.path.abspath(path))) for label, path in sources]
    entries = _snapshot_models(normalized, root, locked)
    if not entries:
        return {"backed_up": 0, "activated": 0, "conflicts": 0, "already_complete": False}
    active = root / "Models"
    existing_groups = {group: _snapshot_existing_model_group(active / group)
                       for group in {item["group"] for item in entries} if (active / group).exists()}
    existing = root
    while not existing.exists():
        existing = existing.parent
    needed = sum(item["size"] for item in entries) * 2 + sum(
        item["size"] for group in existing_groups.values() for item in group) + 64 * 1024 * 1024
    if shutil.disk_usage(existing).free < needed:
        raise RuntimeError("Insufficient free space for verified model backup and candidate")
    lock_hash = (_hash(REPO / "content" / "models.lock.json") if lock is None
                 else hashlib.sha256(_json_bytes(locked)).hexdigest())
    manifest = {"schema": 1, "kind": "models", "lockSha256": lock_hash,
                "sources": [{"id": label, "path": str(path)} for label, path in normalized], "files": entries}
    manifest_hash = hashlib.sha256(_json_bytes(manifest)).hexdigest()
    recovery = root / "Recovery" / ("models-" + manifest_hash[:24])
    _check_ancestors(recovery)
    root.mkdir(parents=True, exist_ok=True)
    (root / "Recovery").mkdir(exist_ok=True)
    recovery.mkdir(exist_ok=True)
    lock_stream = (root / "Recovery" / ".models-migration.lock").open("a+b")
    acquired_lock = False
    try:
        _lock_stream(lock_stream)
        acquired_lock = True
        result = {"backed_up": len(entries), "activated": 0, "conflicts": 0,
                  "already_complete": False, "recovery": str(recovery), "manifest_sha256": manifest_hash}
        source_by_id = dict(normalized)
        for item in entries:
            source = _model_group_path(source_by_id[item["source"]], item["group"]) / item["relative"]
            backup = recovery / "sources" / item["source"] / item["group"] / item["relative"]
            _write_new_verified(source, backup, item)
        manifest_file = recovery / "manifest.json"
        if manifest_file.exists():
            if _hash(manifest_file) != manifest_hash:
                raise RuntimeError("Private model manifest differs; manual recovery required")
        else:
            _write_record(manifest_file, manifest)
        if hook:
            hook("backed_up", len(entries))
        if _snapshot_models(normalized, root, locked) != entries:
            raise RuntimeError("Model source changed during migration; no activation performed")
        selected: dict[str, list[dict]] = {}
        for item in entries:
            selected.setdefault(item["group"], [])
            if not selected[item["group"]] or selected[item["group"]][0]["source"] == item["source"]:
                selected[item["group"]].append(item)
        for group, chosen in selected.items():
            chosen_hashes = {item["relative"]: item["sha256"] for item in chosen}
            if any(item["sha256"] != chosen_hashes.get(item["relative"])
                   for item in entries if item["group"] == group and item["source"] != chosen[0]["source"]):
                result["conflicts"] += 1
        completed = recovery / "completed.json"
        if completed.exists():
            record = json.loads(completed.read_text(encoding="utf-8"))
            if record.get("manifestSha256") != manifest_hash:
                raise RuntimeError("Model completion record differs; manual recovery required")
            result["already_complete"] = True
            result["conflicts"] = record["conflicts"]
            _mark_models_ready(root, locked, lock_hash, manifest_hash)
            return result
        _check_ancestors(active)
        for group, selected_files in selected.items():
            candidate = recovery / "candidate" / group
            for item in selected_files:
                backup = recovery / "sources" / item["source"] / group / item["relative"]
                _write_new_verified(backup, candidate / item["relative"], item)
            actual_candidate = _snapshot_existing_model_group(candidate)
            if {item["relative"]: (item["size"], item["sha256"]) for item in actual_candidate} != {
                    item["relative"]: (item["size"], item["sha256"]) for item in selected_files}:
                raise RuntimeError("Private model candidate is incomplete")
            target = active / group
            if target.exists():
                current = _snapshot_existing_model_group(target)
                if current != existing_groups.get(group):
                    raise RuntimeError("Active model group changed during migration")
                for item in current:
                    _write_new_verified(target / item["relative"],
                                        recovery / "active-existing" / group / item["relative"], item)
                if {item["relative"]: (item["size"], item["sha256"]) for item in current} != {
                        item["relative"]: (item["size"], item["sha256"]) for item in selected_files}:
                    result["conflicts"] += 1
            else:
                active.mkdir(parents=True, exist_ok=True)
                _check_ancestors(active)
                if hook:
                    hook("before_activation", group)
                if target.exists():
                    raise RuntimeError("Active model group appeared during activation")
                os.rename(candidate, target)
                result["activated"] += len(selected_files)
                if hook:
                    hook("activated", group)
        _write_record(completed, {"schema": 1, "manifestSha256": manifest_hash,
                                  "activated": result["activated"], "conflicts": result["conflicts"]})
        _mark_models_ready(root, locked, lock_hash, manifest_hash)
        return result
    finally:
        if acquired_lock:
            _unlock_stream(lock_stream)
        lock_stream.close()


def main() -> int:
    parser = argparse.ArgumentParser(description="Explicit AF2 user-data migration; no source deletion")
    parser.add_argument("--data-kind", choices=("player-exports", "prompts", "terminal-settings", "models"), default="player-exports")
    parser.add_argument("--source-kind", choices=("installed", "repo"), default="installed",
                        help="one source identity per operation; repository data is never auto-imported")
    parser.add_argument("--installed-module", type=Path)
    parser.add_argument("--apply", action="store_true", help="copy/verify/activate into LocalAppData")
    args = parser.parse_args()
    root_text = os.environ.get("LOCALAPPDATA")
    if not root_text:
        raise RuntimeError("LOCALAPPDATA is unavailable")
    root = _safe_root(Path(root_text) / "AnimusForge", False)
    if args.source_kind == "installed":
        if args.installed_module is None or not args.installed_module.is_dir():
            raise ValueError("--installed-module must name the existing installed AnimusForge module")
        sources = [("installed", args.installed_module)]
    else:
        sources = [("repo", REPO / "AnimusForge")]
    if not args.apply:
        if args.data_kind == "player-exports":
            entries = _snapshot(sources, root)
        elif args.data_kind == "prompts":
            entries = _snapshot_prompts(sources, root, _prompt_baselines())
        elif args.data_kind == "models":
            entries = _snapshot_models(sources, root, _model_lock())
        else:
            entries = _snapshot_terminal_settings(sources, root)
        print(f"read-only inventory: files={len(entries)} bytes={sum(e['size'] for e in entries)}")
        return 0
    if os.name != "nt":
        raise RuntimeError("Activation requires Windows non-overwriting rename semantics")
    processes = subprocess.run(["tasklist", "/FO", "CSV", "/NH"], capture_output=True, text=True, check=True)
    for row in csv.reader(processes.stdout.splitlines()):
        if row and ("bannerlord" in row[0].lower() or "playerexportseditor" in row[0].lower()):
            raise RuntimeError("Close Bannerlord and the PlayerExports editor before migration")
    if args.data_kind == "player-exports":
        result = migrate(sources, root)
    elif args.data_kind == "prompts":
        result = migrate_prompts(sources, root)
    elif args.data_kind == "models":
        result = migrate_models(sources, root)
    else:
        result = migrate_terminal_settings(sources, root)
    print(f"migration: backed_up={result['backed_up']} activated={result['activated']} "
          f"conflicts={result['conflicts']} already_complete={result['already_complete']}")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as exc:
        message = str(exc) if isinstance(exc, (ValueError, RuntimeError)) else "see private Recovery; no success was recorded"
        print(f"migration failed: {type(exc).__name__}: {message}", file=sys.stderr)
        sys.exit(1)

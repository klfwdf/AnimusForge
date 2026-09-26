"""Explicit, byte-preserving AF2 PlayerExports migration; never deletes legacy data.

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
    temporary = destination.with_name(destination.name + ".partial." + uuid.uuid4().hex)
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
        return result
    finally:
        if acquired_lock:
            _unlock_stream(lock_stream)
        lock_stream.close()


def main() -> int:
    parser = argparse.ArgumentParser(description="Explicit AF2 PlayerExports migration; no source deletion")
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
        entries = _snapshot(sources, root)
        print(f"read-only inventory: files={len(entries)} bytes={sum(e['size'] for e in entries)}")
        return 0
    if os.name != "nt":
        raise RuntimeError("Activation requires Windows non-overwriting rename semantics")
    processes = subprocess.run(["tasklist", "/FO", "CSV", "/NH"], capture_output=True, text=True, check=True)
    for row in csv.reader(processes.stdout.splitlines()):
        if row and ("bannerlord" in row[0].lower() or "playerexportseditor" in row[0].lower()):
            raise RuntimeError("Close Bannerlord and the PlayerExports editor before migration")
    result = migrate(sources, root)
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

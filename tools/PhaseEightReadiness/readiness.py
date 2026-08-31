#!/usr/bin/env python3
"""Read bounded, hash-bound evidence; never execute evidence or authorize actions."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import subprocess
import sys
from dataclasses import dataclass
from datetime import datetime, timedelta, timezone
from pathlib import Path, PurePosixPath
from typing import Any

CATALOG = "docs/fixtures/phase3-module-catalog/module-catalog.json"
COMPOSITION = "docs/fixtures/phase3-composition-matrix/composition-matrix.json"
BRIDGES = {
    "af.bridge.conversation-siege": "docs/fixtures/phase2-settlement-policy-bridges/settlement-siege-composition.json",
    "af.bridge.policy-diplomacy": "docs/fixtures/phase2-settlement-policy-bridges/policy-diplomacy-composition.json",
}
POLICY_FILES = (CATALOG, COMPOSITION, *BRIDGES.values())
LAYERS = ("OFFLINE", "LIVE", "SAVE", "RELEASE")
KINDS = {
    "OFFLINE": {"contract", "production-replay", "metadata", "stage", "readiness", "cleanup-inventory"},
    "LIVE": {"game-scenario"},
    "SAVE": {"save-roundtrip"},
    "RELEASE": {"package-validation", "rollback-drill"},
}
ARTIFACT_IDS = {"bootstrap", "implementation-1.3", "implementation-1.4", "module-manifest", "package"}
BRIDGE_EXTRA_CASES = {
    "incompatible-contract-version", "bridge-runtime-failure",
    "bridge-disabled-data-preserved", "safe-mode",
}
HEX40 = re.compile(r"^[0-9a-f]{40}$")
HEX64 = re.compile(r"^[0-9a-f]{64}$")
RELEASE_VERSION = re.compile(r"^v?\d+\.\d+\.\d+(?:\.\d+)?(?:-[a-zA-Z0-9][a-zA-Z0-9.-]*)?$")
GAME_BUILD = re.compile(r"^v1\.(3|4)\.\d+\.\d+$")
MAX_JSON_BYTES = 2 * 1024 * 1024
MAX_FILE_BYTES = 512 * 1024 * 1024
MAX_TOTAL_BYTES = 2 * 1024 * 1024 * 1024
MAX_ITEMS = 256
MAX_AGE = timedelta(days=14)


class InvalidEvidence(ValueError):
    pass


def require(condition: bool, message: str) -> None:
    if not condition:
        raise InvalidEvidence(message)


def nonempty(value: Any) -> bool:
    return isinstance(value, str) and bool(value.strip())


def string_list(value: Any) -> bool:
    return isinstance(value, list) and len(value) <= MAX_ITEMS and all(nonempty(item) for item in value)


def unique_pairs(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    result: dict[str, Any] = {}
    for key, value in pairs:
        require(key not in result, f"duplicate JSON key: {key}")
        result[key] = value
    return result


def decode_json(data: bytes) -> dict[str, Any]:
    require(len(data) <= MAX_JSON_BYTES, "JSON exceeds size limit")
    try:
        result = json.loads(data.decode("utf-8-sig"), object_pairs_hook=unique_pairs)
    except (UnicodeError, json.JSONDecodeError, RecursionError) as exc:
        raise InvalidEvidence(f"invalid JSON: {exc}") from exc
    require(isinstance(result, dict), "JSON root must be an object")
    return result


def timestamp(value: Any) -> datetime:
    require(isinstance(value, str), "timestamp must be an ISO-8601 string")
    try:
        parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
    except ValueError as exc:
        raise InvalidEvidence("invalid ISO-8601 timestamp") from exc
    require(parsed.tzinfo is not None, "timestamp requires a timezone")
    return parsed.astimezone(timezone.utc)


class EvidenceFiles:
    """Resolve allowlisted roots before opening, with bounded streaming hashes."""

    def __init__(self, project: Path, artifact_root: Path | None):
        self.roots = {"project": project.resolve(strict=True)}
        if artifact_root is not None:
            self.roots["artifact"] = artifact_root.resolve(strict=True)
        require(all(path.is_dir() for path in self.roots.values()), "roots must be directories")
        self.bytes_read = 0
        self.references = 0

    def resolve(self, root: Any, relative: Any) -> Path:
        require(isinstance(root, str) and root in self.roots, "unknown root; artifact root must be supplied on the CLI")
        require(nonempty(relative) and not any(c in relative for c in ("\\", ":", "\x00")), "path must be a relative POSIX file path")
        parts = relative.split("/")
        require(not PurePosixPath(relative).is_absolute() and all(part not in ("", ".", "..") for part in parts), "absolute/traversal paths are forbidden")
        path = (self.roots[root] / relative).resolve(strict=True)
        require(path.is_relative_to(self.roots[root]), "resolved path escapes its declared root")
        require(path.is_file(), "evidence must be a regular file")
        return path

    def bounded_read(self, path: Path, expected_hash: str | None, parse: bool) -> bytes:
        before = path.stat()
        limit = MAX_JSON_BYTES if parse else MAX_FILE_BYTES
        require(before.st_size <= limit, "file exceeds size limit")
        self.references += 1
        require(self.references <= 2048, "too many file references")
        digest = hashlib.sha256()
        collected = bytearray()
        with path.open("rb") as stream:
            read_count = 0
            while chunk := stream.read(1024 * 1024):
                read_count += len(chunk)
                self.bytes_read += len(chunk)
                require(read_count <= limit and self.bytes_read <= MAX_TOTAL_BYTES, "read budget exceeded")
                digest.update(chunk)
                if parse:
                    collected.extend(chunk)
        after = path.stat()
        require((before.st_size, before.st_mtime_ns) == (after.st_size, after.st_mtime_ns), "file changed during verification")
        if expected_hash is not None:
            require(digest.hexdigest() == expected_hash, "SHA-256 mismatch")
        return bytes(collected)

    def reference(self, reference: Any, parse: bool = False) -> dict[str, Any] | None:
        require(isinstance(reference, dict), "file reference must be an object")
        require(set(reference) == {"root", "path", "sha256"}, "file reference requires only root/path/sha256")
        checksum = reference["sha256"]
        require(isinstance(checksum, str) and HEX64.fullmatch(checksum) is not None, "SHA-256 must be 64 lowercase hex characters")
        path = self.resolve(reference["root"], reference["path"])
        data = self.bounded_read(path, checksum, parse)
        return decode_json(data) if parse else None


@dataclass(frozen=True)
class SourceState:
    commit: str
    clean: bool
    rollback_is_ancestor: bool


def git_state(project: Path, rollback: Any) -> SourceState:
    # Only fixed read-only Git commands; no command text from evidence is run.
    def git(*args: str) -> subprocess.CompletedProcess[str]:
        return subprocess.run(["git", "--no-optional-locks", "-c", "core.fsmonitor=false", "-C", str(project), *args],
                              capture_output=True, text=True, timeout=30, check=False)

    head = git("rev-parse", "--verify", "HEAD")
    status = git("status", "--porcelain=v1", "--untracked-files=all")
    require(head.returncode == 0 and status.returncode == 0, "cannot inspect current Git state")
    ancestor = False
    if isinstance(rollback, str) and HEX40.fullmatch(rollback):
        ancestor = git("merge-base", "--is-ancestor", rollback, "HEAD").returncode == 0
    return SourceState(head.stdout.strip(), not status.stdout.strip(), ancestor)


def load_policy(files: EvidenceFiles) -> tuple[dict[str, dict[str, Any]], dict[str, set[str]], set[str]]:
    documents = {path: decode_json(files.bounded_read(files.resolve("project", path), None, True)) for path in POLICY_FILES}
    entries = documents[CATALOG].get("modules", [])
    require(isinstance(entries, list) and 0 < len(entries) <= MAX_ITEMS, "module catalog is empty or invalid")
    require(all(isinstance(item, dict) and nonempty(item.get("id")) for item in entries), "module catalog IDs must be nonempty strings")
    modules = {item["id"]: item for item in entries}
    require(len(modules) == len(entries), "duplicate module IDs in existing catalog")
    for item in modules.values():
        require(nonempty(item.get("owner")) and string_list(item.get("maintainers")) and bool(item["maintainers"]), "existing catalog has no owner/maintainers")
    def case_ids(document: dict[str, Any]) -> set[str]:
        cases = document.get("cases")
        require(isinstance(cases, list) and 0 < len(cases) <= MAX_ITEMS, "contract cases must be a nonempty bounded array")
        require(all(isinstance(case, dict) and nonempty(case.get("id")) for case in cases), "contract case IDs must be strings")
        result = {case["id"] for case in cases}
        require(len(result) == len(cases), "duplicate contract case ID")
        return result

    composition_ids = case_ids(documents[COMPOSITION])
    require(BRIDGE_EXTRA_CASES <= composition_ids, "existing composition contract changed; review policy mapping")
    bridges = {module_id: case_ids(documents[path]) | BRIDGE_EXTRA_CASES for module_id, path in BRIDGES.items()}
    require(set(bridges) <= set(modules), "bridge IDs missing from existing catalog")
    return modules, bridges, composition_ids


def review(record: dict[str, Any], maintainers: list[str], now: datetime, recorded: datetime) -> None:
    approval = record.get("ownerReview", {})
    require(isinstance(approval, dict) and approval.get("decision") == "ACCEPTED", "owner review missing")
    require(string_list(approval.get("reviewers")) and set(maintainers) <= set(approval["reviewers"]), "required owner/co-owner review missing")
    reviewed = timestamp(approval.get("reviewedAt"))
    require(recorded <= reviewed <= now, "review predates evidence or is in the future")
    require(nonempty(approval.get("note")), "owner review note missing")


def validate_record(record: dict[str, Any], files: EvidenceFiles, modules: dict[str, Any],
                    artifacts: dict[str, Any], commit: str, release_version: str, mode: str, now: datetime) -> None:
    require(record.get("schemaVersion") == 1 and record.get("mode") == mode, "evidence schema/mode mismatch")
    require(nonempty(record.get("id")) and nonempty(record.get("moduleId")) and record["moduleId"] in modules, "unknown evidence/module ID")
    require(record.get("layer") in LAYERS, "unknown evidence layer")
    layer = record["layer"]
    require(record.get("kind") in KINDS[layer], "evidence kind cannot satisfy this layer")
    require(record.get("result") == "PASS", "evidence result is not PASS")
    require(record.get("sourceCommit") == commit, "evidence is from a different source commit")
    require(record.get("releaseVersion") == release_version, "evidence release version mismatch")
    recorded = timestamp(record.get("recordedAt"))
    require(now - MAX_AGE <= recorded <= now, "evidence expired or has a future timestamp")
    require(string_list(record.get("steps")) and bool(record["steps"]), "replay steps missing")
    require(nonempty(record.get("expected")) and nonempty(record.get("observed")), "expected/observed result missing")
    require(string_list(record.get("caseIds")), "caseIds must be an explicit array")
    review(record, modules[record["moduleId"]]["maintainers"], now, recorded)
    require(isinstance(record.get("artifactHashes"), dict), "artifactHashes missing")
    for artifact_id, checksum in record["artifactHashes"].items():
        require(artifact_id in artifacts and checksum == artifacts[artifact_id]["file"]["sha256"], "artifact identity/hash mismatch")
    attachments = record.get("attachments")
    require(isinstance(attachments, list) and 0 < len(attachments) <= MAX_ITEMS, "hashed observation/log attachments missing")
    for attachment in attachments:
        files.reference(attachment)
    if layer in ("LIVE", "SAVE"):
        api = record.get("apiLine")
        require(api in ("1.3", "1.4"), "real host evidence requires an explicit API line")
        require({"bootstrap", "implementation-" + api} <= set(record["artifactHashes"]), "running implementation/bootstrap hashes missing")
        environment = record.get("environment", {})
        require(isinstance(environment, dict) and environment.get("hostInitialized") is True, "initialized live host not demonstrated")
        require(environment.get("hostContext") in ("Campaign", "Mission"), "main-menu/process/stage is not a live host")
        build = environment.get("gameBuildInfo")
        require(isinstance(build, str) and GAME_BUILD.fullmatch(build) is not None and build.startswith("v" + api + "."), "game BuildInfo must be an exact supported numeric build matching the API line")
        require(nonempty(environment.get("saveIdentity")), "save/new-campaign identity missing")
        if layer == "SAVE":
            require(environment.get("oldSaveLoaded") is True and environment.get("roundTripVerified") is True, "old-save load and save/reload evidence missing")
    else:
        require(record.get("apiLine") == "agnostic", "offline/release index uses apiLine=agnostic")
    if layer == "RELEASE":
        require(ARTIFACT_IDS <= set(record["artifactHashes"]), "release record must bind complete artifact set")


def evaluate(project: Path, manifest: Path, artifact_root: Path | None = None,
             *, now: datetime | None = None, source_state: SourceState | None = None) -> dict[str, Any]:
    """source_state/now are deterministic test seams, never accepted from the CLI."""
    now = now or datetime.now(timezone.utc)
    issues: list[dict[str, str]] = []

    def check(code: str, action: Any) -> Any:
        try:
            return action()
        except (InvalidEvidence, OSError, KeyError, TypeError, ValueError, RuntimeError, subprocess.SubprocessError) as exc:
            issues.append({"code": code, "detail": str(exc)[:240]})
            return None

    report: dict[str, Any] = {
        "schemaVersion": 1, "status": "BLOCKED", "preparationReport": True,
        "scope": "existing-phase3-catalog-only", "fullProjectReleaseReady": False,
        "authorization": {name: False for name in ("delete", "defaultSwitch", "deploy", "push", "publish")},
        "checkedAt": now.isoformat(), "modules": [], "blockingIssues": issues,
        "limitations": ["Catalog scope is not the complete 20-domain program.",
                        "Hashes detect changed files; they do not prove human evidence authenticity.",
                        "No command from evidence is executed; readiness is not action authorization."],
    }
    files = check("ROOTS", lambda: EvidenceFiles(project, artifact_root))
    if files is None:
        return report

    def load_manifest() -> dict[str, Any]:
        path = manifest.resolve(strict=True)
        require(any(path.is_relative_to(root) for root in files.roots.values()), "manifest is outside allowlisted roots")
        document = decode_json(files.bounded_read(path, None, True))
        require(document.get("schemaVersion") == 1, "unsupported manifest schema")
        require(document.get("mode") in ("real", "fixture"), "manifest mode must be real or fixture")
        return document

    document = check("MANIFEST", load_manifest)
    policy = check("EXISTING_CONTRACTS", lambda: load_policy(files))
    if document is None or policy is None:
        return report
    modules, bridges, composition_cases = policy
    report["mode"] = document["mode"]
    report["requiredModuleIds"] = sorted(modules)
    report["requiredCompositionCaseIds"] = sorted(composition_cases)
    source = document.get("source", {})
    rollback = document.get("rollback", {})
    if not isinstance(source, dict) or not isinstance(rollback, dict):
        issues.append({"code": "MANIFEST", "detail": "source and rollback must be objects"})
        return report
    state = check("GIT", lambda: source_state or git_state(project, rollback.get("commit")))
    commit = state.commit if state else ""
    report["currentSourceCommit"] = commit
    check("SOURCE_COMMIT", lambda: require(bool(HEX40.fullmatch(commit)) and source.get("commit") == commit, "manifest must match current complete Git commit"))
    check("SOURCE_DIRTY", lambda: require(state is not None and state.clean, "working tree must be clean before evidence acceptance"))

    def source_bindings() -> None:
        bindings = source.get("files")
        require(isinstance(bindings, list) and 0 < len(bindings) <= MAX_ITEMS, "hash-bound source/contract files missing")
        bound_paths = set()
        for binding in bindings:
            files.reference(binding)
            require(binding["root"] == "project", "source bindings must use project root")
            bound_paths.add(binding["path"])
        require(set(POLICY_FILES) <= bound_paths, "existing catalog/bridge/composition hashes missing")

    check("SOURCE_BINDINGS", source_bindings)
    artifacts: dict[str, Any] = {}
    release_version = document.get("releaseVersion")
    check("RELEASE_VERSION", lambda: require(isinstance(release_version, str) and RELEASE_VERSION.fullmatch(release_version) is not None, "numeric releaseVersion missing or invalid"))

    def artifact_bindings() -> None:
        entries = document.get("artifacts")
        require(isinstance(entries, list) and len(entries) <= MAX_ITEMS, "artifacts must be a bounded array")
        for entry in entries:
            require(isinstance(entry, dict) and nonempty(entry.get("id")), "invalid artifact entry")
            artifact_id = entry["id"]
            require(artifact_id not in artifacts, "duplicate artifact ID")
            require(entry.get("sourceCommit") == commit, "artifact was built from a different source commit")
            require(entry.get("version") == release_version and isinstance(release_version, str) and RELEASE_VERSION.fullmatch(release_version) is not None, "artifact release version mismatch")
            expected_api = artifact_id.removeprefix("implementation-") if artifact_id in ("implementation-1.3", "implementation-1.4") else "agnostic"
            require(entry.get("apiLine") == expected_api, "artifact API line does not match its ID")
            files.reference(entry.get("file"))
            artifacts[artifact_id] = entry
        require(ARTIFACT_IDS <= set(artifacts), "Bootstrap, both implementations, module manifest and package are required")

    check("ARTIFACTS", artifact_bindings)
    records: dict[str, dict[str, Any]] = {}
    references = document.get("evidence", [])
    if not isinstance(references, list) or len(references) > MAX_ITEMS:
        issues.append({"code": "EVIDENCE_INDEX", "detail": "evidence must be a bounded array"})
        references = []
    for index, reference in enumerate(references):
        def accept_record(reference: Any = reference) -> None:
            record = files.reference(reference, parse=True)
            validate_record(record, files, modules, artifacts, commit, release_version, document["mode"], now)
            require(record["id"] not in records, "duplicate evidence ID")
            records[record["id"]] = record
        check(f"EVIDENCE_{index}", accept_record)

    for module_id, module in modules.items():
        accepted = [record for record in records.values() if record["moduleId"] == module_id]
        coverage: dict[str, str] = {}
        for layer in LAYERS:
            lines = ("1.3", "1.4") if layer in ("LIVE", "SAVE") else ("agnostic",)
            covered = all(any(record["layer"] == layer and record["apiLine"] == api
                              and (layer != "RELEASE" or record["kind"] == "package-validation")
                              for record in accepted) for api in lines)
            coverage[layer] = "EVIDENCE-ACCEPTED" if covered else "BLOCKED"
            if not covered:
                issues.append({"code": "MISSING_" + layer, "detail": module_id})
        report["modules"].append({"id": module_id, "owner": module["owner"], "layers": coverage})
    for module_id, cases in bridges.items():
        for layer, api in (("OFFLINE", "agnostic"), ("LIVE", "1.3"), ("LIVE", "1.4")):
            covered_cases = {case for record in records.values()
                             if (record["moduleId"], record["layer"], record["apiLine"]) == (module_id, layer, api)
                             for case in record["caseIds"]}
            if not cases <= covered_cases:
                issues.append({"code": "BRIDGE_MATRIX", "detail": f"{module_id}/{layer}/{api}: {','.join(sorted(cases - covered_cases))}"})
    for layer, api in (("OFFLINE", "agnostic"), ("LIVE", "1.3"), ("LIVE", "1.4")):
        covered = {case for record in records.values()
                   if (record["moduleId"], record["layer"], record["apiLine"]) == ("af.foundation.runtime", layer, api)
                   for case in record["caseIds"]}
        if not composition_cases <= covered:
            issues.append({"code": "COMPOSITION_MATRIX", "detail": f"{layer}/{api}: {','.join(sorted(composition_cases - covered))}"})

    def evidence_for(evidence_id: Any, module_id: str, layer: str, kind: str) -> dict[str, Any]:
        require(nonempty(evidence_id) and evidence_id in records, "review evidence is missing or invalid")
        record = records[evidence_id]
        require((record["moduleId"], record["layer"], record["kind"]) == (module_id, layer, kind), "review evidence has wrong owner/layer/kind")
        return record

    def cleanup_review() -> None:
        cleanup = document.get("cleanup", {})
        require(isinstance(cleanup, dict), "cleanup must be an object")
        evidence_for(cleanup.get("auditEvidenceId"), "af.foundation.runtime", "OFFLINE", "cleanup-inventory")
        candidates = cleanup.get("candidates")
        require(isinstance(candidates, list) and len(candidates) <= MAX_ITEMS, "cleanup candidates require an explicit bounded array")
        paths = set()
        for candidate in candidates:
            require(isinstance(candidate, dict), "cleanup candidate must be an object")
            files.reference(candidate.get("file"))
            require(candidate["file"]["root"] == "project", "cleanup candidate must be project-local")
            path = candidate["file"]["path"]
            require(path not in paths, "duplicate cleanup candidate")
            paths.add(path)
            module_id = candidate.get("moduleId")
            evidence_for(candidate.get("auditEvidenceId"), module_id, "OFFLINE", "cleanup-inventory")
            require(nonempty(candidate.get("rationale")), "cleanup rationale missing")
            require(string_list(candidate.get("activeCallers")) and string_list(candidate.get("dynamicEntryPoints")), "caller/reflection/registration inventory is required")
            require(type(candidate.get("saveIdentityRequired")) is bool, "save identity responsibility must be explicit")
            disposition = candidate.get("disposition")
            require(disposition in ("KEEP", "REVIEW_REMOVAL"), "cleanup disposition is not authorization")
            if disposition == "REVIEW_REMOVAL":
                require(not candidate["activeCallers"] and not candidate["dynamicEntryPoints"] and not candidate["saveIdentityRequired"], "active caller/dynamic entry/save responsibility prevents removal review")
                replacement = candidate.get("replacementEvidenceIds")
                require(string_list(replacement) and bool(replacement), "replacement evidence missing")
                require(all(item in records and records[item]["moduleId"] == module_id for item in replacement), "replacement evidence owner mismatch")
                required = {(layer, api) for layer in LAYERS for api in (("1.3", "1.4") if layer in ("LIVE", "SAVE") else ("agnostic",))}
                actual = {(records[item]["layer"], records[item]["apiLine"]) for item in replacement
                          if records[item]["layer"] != "RELEASE" or records[item]["kind"] == "package-validation"}
                require(required <= actual, "replacement lacks offline/live/save/release evidence")

    check("CLEANUP_REVIEW", cleanup_review)

    def rollback_review() -> None:
        require(isinstance(rollback.get("commit"), str) and HEX40.fullmatch(rollback["commit"]) is not None, "full rollback commit missing")
        require(state is not None and state.rollback_is_ancestor, "rollback commit must exist in current Git ancestry")
        record = evidence_for(rollback.get("evidenceId"), "af.foundation.runtime", "RELEASE", "rollback-drill")
        require(record.get("rollbackTargetCommit") == rollback["commit"], "rollback drill does not bind the declared target commit")
        require(rollback.get("saveSideEffectsNotUndone") is True, "source rollback does not undo save side effects")

    check("ROLLBACK", rollback_review)
    report["acceptedEvidenceCount"] = len(records)
    report["verifiedFileReferences"] = files.references
    if not issues:
        report["status"] = "FIXTURE-VALID" if document["mode"] == "fixture" else "READY-FOR-OWNER-REVIEW"
    return report


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--artifact-root", type=Path, help="Explicit extra read-only root; never read from the manifest")
    args = parser.parse_args()
    result = evaluate(args.project_root, args.manifest, args.artifact_root)
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 2 if result["status"] == "BLOCKED" else 0


if __name__ == "__main__":
    sys.exit(main())

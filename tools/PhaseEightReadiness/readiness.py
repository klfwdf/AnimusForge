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
DOMAIN_CATALOG = "docs/phase8/full-domain-readiness-catalog.json"
CLEANUP_CATALOG = "docs/phase8/cleanup-candidates.json"
POLICY_FILES = (CATALOG, COMPOSITION, *BRIDGES.values(), DOMAIN_CATALOG, CLEANUP_CATALOG)
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
SLUG = re.compile(r"^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$")
OWNER_ID = re.compile(r"^[A-Za-z][A-Za-z0-9]*(?:[._-][A-Za-z0-9]+)*$")
IDENTIFIER = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*$")
RELEASE_VERSION = re.compile(r"^v?\d+\.\d+\.\d+(?:\.\d+)?(?:-[a-zA-Z0-9][a-zA-Z0-9.-]*)?$")
GAME_BUILD = re.compile(r"^v1\.(3|4)\.\d+\.\d+$")
MAX_JSON_BYTES = 2 * 1024 * 1024
MAX_SOURCE_TEXT_BYTES = 8 * 1024 * 1024
MAX_FILE_BYTES = 512 * 1024 * 1024
MAX_TOTAL_BYTES = 2 * 1024 * 1024 * 1024
MAX_ITEMS = 256
MAX_AGE = timedelta(days=14)
DOMAIN_EVIDENCE_KEYS = {"offline", "compiled", "live", "save", "release"}
DOMAIN_EVIDENCE_STATES = {"LOCAL_PASS", "VERIFY", "NOT_RUN", "BLOCKED"}
PROMPT_ACTION_STATES = {"APPLICABLE", "NOT_APPLICABLE", "MIXED"}
DEFAULT_STATES = {"LEGACY_DEFAULT", "MIXED_DEFAULT", "OPT_IN", "ACTIVE", "TOOL_ONLY"}
BRIDGE_STATES = {"ACTIVE_BOUNDARY", "OPT_IN", "DESIGN_ONLY", "DESIGN_INVENTORY", "BLOCKED_LIVE"}
BRIDGE_TOPOLOGIES = {"PAIR", "CROSS_CUT"}
ENTRY_COVERAGE_STATES = {"REPRESENTATIVE", "COMPLETE"}
OWNER_ASSIGNMENT_STATES = {"ASSIGNED", "ROLE_PLACEHOLDER"}
STATIC_CLEANUP_DISPOSITIONS = {"KEEP", "HOLD", "REVIEW_REMOVAL"}
CANONICAL_DOMAIN_IDS = {
    "bootstrap-build", "host-composition", "runtime-diagnostics", "game-adapter-compatibility",
    "persistence-config", "conversation-encounter", "gateway-prompt-protocol", "action-commit",
    "memory-afef", "economy-reward-debt", "policy-political", "world-simulation-worldmap",
    "settlement-siege-gccz-sets", "scene-mission-combat", "duel", "courier-proactive-issue",
    "social-progression-reports", "knowledge-persona-profile", "ui-tts-external-integration",
    "tools-content-package",
}
CANONICAL_FULL_BRIDGE_IDS = {
    "bootstrap-host", "host-runtime", "runtime-game-adapter", "persistence-domain-owners",
    "conversation-gateway", "conversation-action", "action-memory", "action-economy",
    "policy-world-diplomacy", "conversation-siege", "scene-duel", "conversation-courier",
    "memory-social-reports", "gateway-knowledge-profile", "ui-runtime-integration",
    "tools-content-release",
}


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

    def bounded_text(self, path: Path) -> str:
        before = path.stat()
        require(before.st_size <= MAX_SOURCE_TEXT_BYTES, "source text exceeds audit size limit")
        self.references += 1
        require(self.references <= 2048, "too many file references")
        collected = bytearray()
        with path.open("rb") as stream:
            while chunk := stream.read(1024 * 1024):
                self.bytes_read += len(chunk)
                require(len(collected) + len(chunk) <= MAX_SOURCE_TEXT_BYTES
                        and self.bytes_read <= MAX_TOTAL_BYTES, "read budget exceeded")
                collected.extend(chunk)
        after = path.stat()
        require((before.st_size, before.st_mtime_ns) == (after.st_size, after.st_mtime_ns),
                "file changed during verification")
        try:
            return collected.decode("utf-8-sig")
        except UnicodeError as exc:
            raise InvalidEvidence("source text is not UTF-8") from exc


@dataclass(frozen=True)
class SourceState:
    commit: str
    clean: bool
    rollback_is_ancestor: bool
    rollback_is_head: bool = False


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
    commit = head.stdout.strip()
    return SourceState(commit, not status.stdout.strip(), ancestor, rollback == commit)


def exact_keys(value: Any, keys: set[str], label: str) -> dict[str, Any]:
    require(isinstance(value, dict) and set(value) == keys, f"{label} has missing or unknown fields")
    return value


def source_paths(files: EvidenceFiles, paths: Any, label: str) -> list[str]:
    require(string_list(paths) and bool(paths), f"{label} requires bounded project file paths")
    for path in paths:
        files.resolve("project", path)
    return paths


def load_domains(document: dict[str, Any], files: EvidenceFiles) -> tuple[dict[str, dict[str, Any]], dict[str, dict[str, Any]]]:
    exact_keys(document, {"schemaVersion", "catalogId", "programSource", "domains", "bridges"}, "full-domain catalog")
    require(document.get("schemaVersion") == 1, "unsupported full-domain catalog schema")
    require(document["catalogId"] == "af.phase8.full-domain-readiness", "unexpected full-domain catalog ID")
    files.resolve("project", document["programSource"])
    entries = document.get("domains")
    require(isinstance(entries, list) and len(entries) == 20, "full-domain catalog must contain exactly 20 domains")
    domains: dict[str, dict[str, Any]] = {}
    numbers: set[int] = set()
    for entry in entries:
        exact_keys(entry, {"number", "id", "title", "owner", "maintainers", "ownerAssignmentState",
                           "entryPaths", "entryCoverage", "promptAction",
                           "persistence", "failureFallback", "defaultState", "currentEvidence", "blockingGates",
                           "bridgeIds"}, "domain")
        domain_id = entry["id"]
        require(isinstance(domain_id, str) and SLUG.fullmatch(domain_id) is not None, "domain ID must be an English slug")
        require(domain_id not in domains, "duplicate domain ID")
        require(type(entry["number"]) is int and 1 <= entry["number"] <= 20 and entry["number"] not in numbers,
                "domain number must be unique in 1..20")
        require(nonempty(entry["title"]), "domain title missing")
        require(isinstance(entry["owner"], str) and OWNER_ID.fullmatch(entry["owner"]) is not None, "domain owner ID is invalid")
        require(string_list(entry["maintainers"]) and bool(entry["maintainers"]), "domain maintainers missing")
        require(all(OWNER_ID.fullmatch(item) is not None for item in entry["maintainers"]), "domain maintainer ID is invalid")
        require(entry["ownerAssignmentState"] in OWNER_ASSIGNMENT_STATES, "domain owner assignment state is invalid")
        source_paths(files, entry["entryPaths"], "domain entry")
        require(entry["entryCoverage"] in ENTRY_COVERAGE_STATES, "domain entry coverage state is invalid")
        prompt_action = exact_keys(entry["promptAction"], {"prompt", "actionPlan"}, "prompt/action applicability")
        require(set(prompt_action.values()) <= PROMPT_ACTION_STATES, "invalid prompt/action applicability")
        persistence = exact_keys(entry["persistence"], {"responsibility", "keys", "types"}, "persistence responsibility")
        require(nonempty(persistence["responsibility"]) and string_list(persistence["keys"]) and string_list(persistence["types"]),
                "persistence responsibility is incomplete")
        require(nonempty(entry["failureFallback"]), "failure fallback missing")
        require(entry["defaultState"] in DEFAULT_STATES, "invalid default state")
        evidence = exact_keys(entry["currentEvidence"], DOMAIN_EVIDENCE_KEYS, "current evidence")
        require(set(evidence.values()) <= DOMAIN_EVIDENCE_STATES, "invalid current evidence state")
        require(string_list(entry["blockingGates"]) and bool(entry["blockingGates"]), "blocking gates missing")
        require(string_list(entry["bridgeIds"]) and bool(entry["bridgeIds"]), "domain bridge inventory missing")
        domains[domain_id] = entry
        numbers.add(entry["number"])
    require(numbers == set(range(1, 21)) and set(domains) == CANONICAL_DOMAIN_IDS,
            "domain IDs/numbers must match the canonical 20-domain program")

    bridge_entries = document.get("bridges")
    require(isinstance(bridge_entries, list) and 0 < len(bridge_entries) <= MAX_ITEMS, "full-domain bridge matrix is missing")
    full_bridges: dict[str, dict[str, Any]] = {}
    for bridge in bridge_entries:
        exact_keys(bridge, {"id", "domains", "owner", "implementationState", "topology", "entryPaths", "requiredCases",
                            "blockingGates"}, "full-domain bridge")
        bridge_id = bridge["id"]
        require(isinstance(bridge_id, str) and SLUG.fullmatch(bridge_id) is not None and bridge_id not in full_bridges,
                "full-domain bridge ID is invalid or duplicated")
        require(string_list(bridge["domains"]) and len(set(bridge["domains"])) >= 2
                and set(bridge["domains"]) <= set(domains), "bridge endpoints are invalid")
        require(isinstance(bridge["owner"], str) and OWNER_ID.fullmatch(bridge["owner"]) is not None, "bridge owner ID is invalid")
        require(bridge["implementationState"] in BRIDGE_STATES, "invalid bridge implementation state")
        require(bridge["topology"] in BRIDGE_TOPOLOGIES, "invalid bridge topology")
        if bridge["topology"] == "PAIR":
            require(len(set(bridge["domains"])) == 2
                    and {"A_ONLY", "B_ONLY", "A_PLUS_B_NO_BRIDGE", "A_PLUS_B_WITH_BRIDGE"}
                    <= set(bridge["requiredCases"]), "pair bridge requires explicit A/B cases")
        else:
            require(len(set(bridge["domains"])) >= 3
                    and {"EACH_OWNER_ALONE", "ALL_WITHOUT_COORDINATOR", "ALL_WITH_COORDINATOR"}
                    <= set(bridge["requiredCases"]), "cross-cut bridge requires multi-owner cases")
        source_paths(files, bridge["entryPaths"], "bridge entry")
        require(string_list(bridge["requiredCases"]) and bool(bridge["requiredCases"]), "bridge cases missing")
        require(string_list(bridge["blockingGates"]) and bool(bridge["blockingGates"]), "bridge blocking gates missing")
        full_bridges[bridge_id] = bridge
    require(set(full_bridges) == CANONICAL_FULL_BRIDGE_IDS,
            "full-domain bridge IDs must match the canonical phase-eight matrix")
    for domain_id, domain in domains.items():
        require(set(domain["bridgeIds"]) <= set(full_bridges), "domain references an unknown bridge")
        require(all(domain_id in full_bridges[item]["domains"] for item in domain["bridgeIds"]),
                "domain/bridge endpoint mapping is inconsistent")
    require(all(bridge_id in domains[domain_id]["bridgeIds"]
                for bridge_id, bridge in full_bridges.items() for domain_id in bridge["domains"]),
            "bridge/domain reverse mapping is inconsistent")
    return domains, full_bridges


def load_cleanup_inventory(document: dict[str, Any], files: EvidenceFiles,
                           domains: dict[str, dict[str, Any]]) -> dict[str, dict[str, Any]]:
    exact_keys(document, {"schemaVersion", "catalogId", "baselineCommit", "candidates"}, "cleanup inventory")
    require(document.get("schemaVersion") == 1, "unsupported cleanup inventory schema")
    require(document["catalogId"] == "af.phase8.cleanup-candidates", "unexpected cleanup inventory ID")
    baseline = document.get("baselineCommit")
    require(isinstance(baseline, str) and HEX40.fullmatch(baseline) is not None, "cleanup inventory baseline must be a full commit")
    entries = document.get("candidates")
    require(isinstance(entries, list) and 0 < len(entries) <= MAX_ITEMS, "cleanup inventory must be explicit and nonempty")
    candidates: dict[str, dict[str, Any]] = {}
    source_cache: dict[str, str] = {}
    for entry in entries:
        exact_keys(entry, {"id", "path", "symbols", "ownerDomainId", "disposition", "rationale", "activeCallers",
                           "dynamicEntryPoints", "compatibilityResponsibilities", "replacement", "deletePreconditions",
                           "rollback", "risk"}, "cleanup inventory candidate")
        candidate_id = entry["id"]
        require(isinstance(candidate_id, str) and SLUG.fullmatch(candidate_id) is not None and candidate_id not in candidates,
                "cleanup candidate ID is invalid or duplicated")
        path = entry["path"]
        require(nonempty(path), "cleanup candidate path is missing")
        source_path = files.resolve("project", path)
        require(string_list(entry["symbols"]) and bool(entry["symbols"]), "cleanup symbols missing")
        if path not in source_cache:
            source_cache[path] = files.bounded_text(source_path)
        for symbol in entry["symbols"]:
            token = symbol.rsplit(".", 1)[-1]
            require(IDENTIFIER.fullmatch(token) is not None
                    and re.search(rf"\b{re.escape(token)}\b", source_cache[path]) is not None,
                    "cleanup symbol is not an identifier present in its source file")
        require(entry["ownerDomainId"] in domains, "cleanup candidate owner domain is unknown")
        require(entry["disposition"] in STATIC_CLEANUP_DISPOSITIONS, "invalid cleanup disposition")
        require(nonempty(entry["rationale"]) and nonempty(entry["risk"]), "cleanup rationale/risk missing")
        for name in ("activeCallers", "dynamicEntryPoints", "compatibilityResponsibilities", "replacement", "deletePreconditions"):
            require(string_list(entry[name]), f"cleanup {name} must be an explicit string array")
        rollback = exact_keys(entry["rollback"], {"strategy", "checkpoint"}, "cleanup rollback")
        require(rollback["strategy"] == "git-revert-after-reviewed-removal"
                and isinstance(rollback["checkpoint"], str) and HEX40.fullmatch(rollback["checkpoint"]) is not None,
                "cleanup rollback must use a full reviewed checkpoint")
        if entry["disposition"] == "REVIEW_REMOVAL":
            require(not entry["activeCallers"] and not entry["dynamicEntryPoints"]
                    and not entry["compatibilityResponsibilities"], "active responsibility prevents removal review")
            require(entry["replacement"] and entry["deletePreconditions"], "removal review lacks replacement/gates")
        candidates[candidate_id] = entry
    return candidates


def load_policy(files: EvidenceFiles) -> tuple[dict[str, dict[str, Any]], dict[str, set[str]], set[str],
                                               dict[str, dict[str, Any]], dict[str, dict[str, Any]],
                                               dict[str, dict[str, Any]]]:
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
    domains, full_bridges = load_domains(documents[DOMAIN_CATALOG], files)
    cleanup_candidates = load_cleanup_inventory(documents[CLEANUP_CATALOG], files, domains)
    return modules, bridges, composition_ids, domains, full_bridges, cleanup_candidates


def review(record: dict[str, Any], maintainers: list[str], now: datetime, recorded: datetime) -> None:
    approval = record.get("ownerReview", {})
    require(isinstance(approval, dict) and approval.get("decision") == "ACCEPTED", "owner review missing")
    require(string_list(approval.get("reviewers")) and set(maintainers) <= set(approval["reviewers"]), "required owner/co-owner review missing")
    reviewed = timestamp(approval.get("reviewedAt"))
    require(recorded <= reviewed <= now, "review predates evidence or is in the future")
    require(nonempty(approval.get("note")), "owner review note missing")


def validate_record(record: dict[str, Any], files: EvidenceFiles, modules: dict[str, Any],
                    domains: dict[str, dict[str, Any]], full_bridges: dict[str, dict[str, Any]],
                    cleanup_inventory: dict[str, dict[str, Any]], artifacts: dict[str, Any], commit: str,
                    release_version: str, mode: str, now: datetime) -> None:
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
    domain_ids = record.get("domainIds")
    require(string_list(domain_ids) and bool(domain_ids) and len(domain_ids) == len(set(domain_ids))
            and set(domain_ids) <= set(domains), "evidence must declare known unique domain IDs")
    required_reviewers = set(modules[record["moduleId"]]["maintainers"])
    for domain_id in domain_ids:
        required_reviewers.update(domains[domain_id]["maintainers"])
    review(record, sorted(required_reviewers), now, recorded)
    cleanup_candidate_ids = record.get("cleanupCandidateIds", [])
    require(string_list(cleanup_candidate_ids) and len(cleanup_candidate_ids) == len(set(cleanup_candidate_ids))
            and set(cleanup_candidate_ids) <= set(cleanup_inventory), "evidence cleanup candidate IDs are invalid")
    bridge_ids = record.get("bridgeIds")
    require(string_list(bridge_ids) and len(bridge_ids) == len(set(bridge_ids))
            and set(bridge_ids) <= set(full_bridges),
            "evidence bridge IDs must be an explicit unique array")
    require(all(set(full_bridges[item]["domains"]) <= set(domain_ids) for item in bridge_ids),
            "evidence bridge endpoints must be included in domainIds")
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
        "scope": "full-20-domain-program-with-existing-8-id-design-catalog", "fullProjectReleaseReady": False,
        "authorization": {name: False for name in ("delete", "defaultSwitch", "deploy", "push", "publish")},
        "checkedAt": now.isoformat(), "modules": [], "domains": [], "blockingIssues": issues,
        "limitations": ["The 20 domains are acceptance responsibility buckets, not 20 deployed physical modules.",
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
    modules, bridges, composition_cases, domains, full_bridges, cleanup_inventory = policy
    report["mode"] = document["mode"]
    report["requiredModuleIds"] = sorted(modules)
    report["requiredDomainIds"] = [item["id"] for item in sorted(domains.values(), key=lambda item: item["number"])]
    report["requiredCompositionCaseIds"] = sorted(composition_cases)
    report["fullDomainBridgeIds"] = sorted(full_bridges)
    report["cleanupInventory"] = {
        "candidateCount": len(cleanup_inventory),
        "dispositions": {state: sum(1 for item in cleanup_inventory.values() if item["disposition"] == state)
                         for state in sorted(STATIC_CLEANUP_DISPOSITIONS)},
    }
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
            validate_record(record, files, modules, domains, full_bridges, cleanup_inventory, artifacts, commit,
                            release_version, document["mode"], now)
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
    for domain in sorted(domains.values(), key=lambda item: item["number"]):
        accepted = [record for record in records.values() if domain["id"] in record["domainIds"]]
        coverage: dict[str, str] = {}
        for layer in LAYERS:
            lines = ("1.3", "1.4") if layer in ("LIVE", "SAVE") else ("agnostic",)
            covered = all(any(record["layer"] == layer and record["apiLine"] == api
                              and (layer != "RELEASE" or record["kind"] == "package-validation")
                              for record in accepted) for api in lines)
            coverage[layer] = "EVIDENCE-ACCEPTED" if covered else "BLOCKED"
            if not covered:
                issues.append({"code": "MISSING_DOMAIN_" + layer, "detail": domain["id"]})
        report["domains"].append({
            "number": domain["number"], "id": domain["id"], "title": domain["title"],
            "owner": domain["owner"], "ownerAssignmentState": domain["ownerAssignmentState"],
            "entryCoverage": domain["entryCoverage"], "defaultState": domain["defaultState"],
            "declaredEvidence": domain["currentEvidence"], "layers": coverage,
            "blockingGates": domain["blockingGates"],
        })
        if document["mode"] == "real" and domain["ownerAssignmentState"] != "ASSIGNED":
            issues.append({"code": "UNASSIGNED_DOMAIN_OWNER", "detail": domain["id"]})
        if document["mode"] == "real" and domain["entryCoverage"] != "COMPLETE":
            issues.append({"code": "INCOMPLETE_DOMAIN_ENTRY_INVENTORY", "detail": domain["id"]})
    for module_id, cases in bridges.items():
        for layer, api in (("OFFLINE", "agnostic"), ("LIVE", "1.3"), ("LIVE", "1.4"),
                           ("SAVE", "1.3"), ("SAVE", "1.4")):
            covered_cases = {case for record in records.values()
                             if (record["moduleId"], record["layer"], record["apiLine"]) == (module_id, layer, api)
                             for case in record["caseIds"]}
            if not cases <= covered_cases:
                issues.append({"code": "BRIDGE_MATRIX", "detail": f"{module_id}/{layer}/{api}: {','.join(sorted(cases - covered_cases))}"})
    for layer, api in (("OFFLINE", "agnostic"), ("LIVE", "1.3"), ("LIVE", "1.4"),
                       ("SAVE", "1.3"), ("SAVE", "1.4")):
        covered = {case for record in records.values()
                   if (record["moduleId"], record["layer"], record["apiLine"]) == ("af.foundation.runtime", layer, api)
                   for case in record["caseIds"]}
        if not composition_cases <= covered:
            issues.append({"code": "COMPOSITION_MATRIX", "detail": f"{layer}/{api}: {','.join(sorted(composition_cases - covered))}"})
    for bridge_id, bridge in full_bridges.items():
        endpoints = set(bridge["domains"])
        required_cases = set(bridge["requiredCases"])
        for layer, api in (("OFFLINE", "agnostic"), ("LIVE", "1.3"), ("LIVE", "1.4"),
                           ("SAVE", "1.3"), ("SAVE", "1.4")):
            covered = {case for record in records.values()
                       if record["layer"] == layer and record["apiLine"] == api
                       and bridge_id in record["bridgeIds"] and endpoints <= set(record["domainIds"])
                       for case in record["caseIds"]}
            if not required_cases <= covered:
                issues.append({"code": "FULL_DOMAIN_BRIDGE_MATRIX",
                               "detail": f"{bridge_id}/{layer}/{api}: {','.join(sorted(required_cases - covered))}"})

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
        inventory_ids = set()
        for candidate in candidates:
            require(isinstance(candidate, dict), "cleanup candidate must be an object")
            inventory_id = candidate.get("inventoryCandidateId")
            require(nonempty(inventory_id) and inventory_id in cleanup_inventory, "cleanup candidate is not in the reviewed inventory")
            require(inventory_id not in inventory_ids, "duplicate cleanup inventory candidate")
            inventory_ids.add(inventory_id)
            inventory = cleanup_inventory[inventory_id]
            files.reference(candidate.get("file"))
            require(candidate["file"]["root"] == "project", "cleanup candidate must be project-local")
            path = candidate["file"]["path"]
            require(path == inventory["path"], "cleanup candidate path does not match its inventory entry")
            module_id = candidate.get("moduleId")
            audit_record = evidence_for(candidate.get("auditEvidenceId"), module_id, "OFFLINE", "cleanup-inventory")
            require(inventory_id in audit_record.get("cleanupCandidateIds", [])
                    and inventory["ownerDomainId"] in audit_record["domainIds"],
                    "cleanup audit evidence is not bound to this candidate and owner domain")
            require(nonempty(candidate.get("rationale")), "cleanup rationale missing")
            require(string_list(candidate.get("activeCallers")) and string_list(candidate.get("dynamicEntryPoints")), "caller/reflection/registration inventory is required")
            require(type(candidate.get("saveIdentityRequired")) is bool, "save identity responsibility must be explicit")
            disposition = candidate.get("disposition")
            require(disposition in ("KEEP", "REVIEW_REMOVAL"), "cleanup disposition is not authorization")
            if disposition == "REVIEW_REMOVAL":
                require(inventory["disposition"] == "REVIEW_REMOVAL", "inventory has not admitted this removal review")
                require(not candidate["activeCallers"] and not candidate["dynamicEntryPoints"] and not candidate["saveIdentityRequired"], "active caller/dynamic entry/save responsibility prevents removal review")
                replacement = candidate.get("replacementEvidenceIds")
                require(string_list(replacement) and bool(replacement), "replacement evidence missing")
                require(all(item in records and records[item]["moduleId"] == module_id for item in replacement), "replacement evidence owner mismatch")
                require(all(inventory_id in records[item].get("cleanupCandidateIds", []) for item in replacement),
                        "replacement evidence is not bound to this cleanup candidate")
                require(all(inventory["ownerDomainId"] in records[item]["domainIds"] for item in replacement),
                        "replacement evidence does not cover the candidate owner domain")
                required = {(layer, api) for layer in LAYERS for api in (("1.3", "1.4") if layer in ("LIVE", "SAVE") else ("agnostic",))}
                actual = {(records[item]["layer"], records[item]["apiLine"]) for item in replacement
                          if records[item]["layer"] != "RELEASE" or records[item]["kind"] == "package-validation"}
                require(required <= actual, "replacement lacks offline/live/save/release evidence")
                candidate_rollback = exact_keys(candidate.get("rollback"),
                                                {"commit", "evidenceId", "saveSideEffectsNotUndone"},
                                                "candidate rollback")
                require(isinstance(candidate_rollback["commit"], str)
                        and HEX40.fullmatch(candidate_rollback["commit"]) is not None,
                        "candidate rollback commit missing")
                require(candidate_rollback["commit"] == rollback.get("commit")
                        and candidate_rollback["commit"] == inventory["rollback"]["checkpoint"],
                        "candidate rollback must use the manifest/inventory pre-cleanup checkpoint")
                rollback_record = evidence_for(candidate_rollback["evidenceId"], "af.foundation.runtime",
                                               "RELEASE", "rollback-drill")
                require(rollback_record.get("rollbackTargetCommit") == candidate_rollback["commit"],
                        "candidate rollback drill targets a different commit")
                require(inventory_id in rollback_record.get("cleanupCandidateIds", []),
                        "candidate rollback drill is not bound to this cleanup candidate")
                require(candidate_rollback["saveSideEffectsNotUndone"] is True,
                        "candidate source rollback does not undo save side effects")

    check("CLEANUP_REVIEW", cleanup_review)

    def rollback_review() -> None:
        require(isinstance(rollback.get("commit"), str) and HEX40.fullmatch(rollback["commit"]) is not None, "full rollback commit missing")
        require(state is not None and state.rollback_is_ancestor, "rollback commit must exist in current Git ancestry")
        require(not state.rollback_is_head, "rollback commit must be a strict ancestor, not current HEAD")
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

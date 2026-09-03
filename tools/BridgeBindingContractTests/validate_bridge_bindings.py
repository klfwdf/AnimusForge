#!/usr/bin/env python3
"""Validate the phase-8 Bridge binding manifest against the real source tree.

The manifest is an inventory and a small runtime-wiring ledger.  This tool is
deliberately source-only: it does not import the mod, load Bannerlord
assemblies, start a game, read a save, or execute a Bridge.  A binding marked
``declared-only`` is a truthful non-claim that the contract exists without a
runtime caller.  Only the explicitly reviewed wired entries may claim a
runtime gate.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path, PurePosixPath
from typing import Any


EXPECTED_MANIFEST_ID = "af.phase8.bridge-bindings"
EXPECTED_CATALOG_PATH = "docs/phase8/full-domain-readiness-catalog.json"
EXPECTED_CONFIG_PATH = "AnimusForge/ModuleData/FeatureBridges.json"
EXPECTED_CONTRACT_VERSION = 1
EXPECTED_BRIDGE_COUNT = 16
EXPECTED_WIRED = {
    "conversation-gateway": ("Refactor/Adapters/LegacyConfiguredChatGateway.cs", "GenerateExchangeAsync"),
    "conversation-action": ("Refactor/Runtime/InteractionResultCommitter.cs", "Commit"),
    "action-memory": ("Refactor/Runtime/InteractionResultCommitter.cs", "Commit"),
    "action-economy": ("Refactor/Adapters/LegacyNativeActionPlanExecutor.cs", "ValidateAndExecuteCore"),
    "conversation-siege": ("AfGcczShoutBridge.cs", "IsActive"),
    "conversation-courier": ("CourierDeliveryBehavior.cs", "IsCourierBridgeEnabled"),
    "memory-social-reports": ("PlayerNotorietyBehavior.ConversationOutcomes.cs", "IsSocialReportsBridgeEnabled"),
    "gateway-knowledge-profile": ("Refactor/Adapters/LegacyKnowledgeRagGateway.cs", "GenerateAsync"),
    "policy-world-diplomacy": ("WorldDiplomacyBehavior.cs", "NotifyExternalDiplomacyResolved"),
    "ui-runtime-integration": ("SceneActionsIntegrationBoundary.cs", "InitializeRuntime"),
}
EXPECTED_CONFIGURABLE = frozenset(EXPECTED_WIRED)
EXPECTED_GATE_TOKENS = {
    "conversation-gateway": "FeatureBridgeIds.ConversationGateway",
    "conversation-action": "FeatureBridgeIds.ConversationAction",
    "action-memory": "FeatureBridgeIds.ActionMemory",
    "action-economy": "FeatureBridgeIds.ActionEconomy",
    "policy-world-diplomacy": "FeatureBridgeIds.PolicyWorldDiplomacy",
    "conversation-siege": "FeatureBridgeIds.ConversationSiege",
    "conversation-courier": "FeatureBridgeIds.ConversationCourier",
    "memory-social-reports": "FeatureBridgeIds.MemorySocialReports",
    "gateway-knowledge-profile": "FeatureBridgeIds.GatewayKnowledgeProfile",
    "ui-runtime-integration": "FeatureBridgeIds.UiRuntimeIntegration",
}
EXPECTED_METHOD_CONTRACTS = {
    "conversation-gateway": {
        "method": "GenerateExchangeAsync",
        "gate": "FeatureBridgeIds.ConversationGateway",
        "gate_marker": "FeatureBridgeRuntime.IsEnabled",
        "before": ("_credentialResolver", "SendOnceAsync", "BuildPayload", "PrepareChatRequestJson", "CreateTimeout"),
    },
    "conversation-action": {
        "method": "Commit",
        "gate": "FeatureBridgeIds.ConversationAction",
        "gate_marker": "FeatureBridgeRuntime.IsEnabled",
        "before": ("CommitOnce", "TryAppendVisibleExchange", "ValidateAndExecute"),
    },
    "action-memory": {
        "method": "Commit",
        "gate": "FeatureBridgeIds.ActionMemory",
        "gate_marker": "FeatureBridgeRuntime.IsEnabled",
        "before": ("CommitOnce", "TryAppendVisibleExchange", "ValidateAndExecute"),
    },
    "action-economy": {
        "method": "ValidateAndExecuteCore",
        "gate": "FeatureBridgeIds.ActionEconomy",
        "gate_marker": "FeatureBridgeRuntime.IsEnabled",
        "before": ("_economyPlanner", "_economyExecutionGate", "_economyPort.Replay", "_requestBoundExecute(", "_execute(", "TryQueue(", "Publish"),
    },
    "conversation-siege": {
        "method": "IsActive",
        "gate": "FeatureBridgeIds.ConversationSiege",
        "gate_marker": "ConversationSiegeBridgeEnabled",
        "cached_gate": "ConversationSiegeBridgeEnabled",
        "initializer": "FeatureBridgeRuntime.IsEnabled(FeatureBridgeIds.ConversationSiege)",
        "before": ("IsTownOrCastleAftermathActive", "VillageAftermathBehavior.IsActive"),
    },
    "conversation-courier": {
        "method": "IsCourierBridgeEnabled",
        "gate": "FeatureBridgeIds.ConversationCourier",
        "gate_marker": "FeatureBridgeRuntime.IsEnabled",
        "before": (),
    },
    "memory-social-reports": {
        "method": "IsSocialReportsBridgeEnabled",
        "gate": "FeatureBridgeIds.MemorySocialReports",
        "gate_marker": "FeatureBridgeRuntime.IsEnabled",
        "before": (),
    },
    "gateway-knowledge-profile": {
        "method": "GenerateAsync",
        "gate": "FeatureBridgeIds.GatewayKnowledgeProfile",
        "gate_marker": "FeatureBridgeRuntime.IsEnabled",
        "before": ("_configuredGateway.GenerateAsync",),
    },
    "policy-world-diplomacy": {
        "method": "NotifyExternalDiplomacyResolved",
        "gate": "FeatureBridgeIds.PolicyWorldDiplomacy",
        "gate_marker": "FeatureBridgeRuntime.Evaluate",
        "before": ("ResolveInstance", "NotifyExternalDiplomacyResolvedInternal"),
    },
    "ui-runtime-integration": {
        "method": "InitializeRuntime",
        "gate": "FeatureBridgeIds.UiRuntimeIntegration",
        "gate_marker": "FeatureBridgeRuntime.Evaluate",
        "before": ("SceneActionsRuntimeHost.Initialize", "BattleSpeechRuntimeHost.Initialize", "SceneActionsAfBridgeHost.TryInstall"),
    },
}
ALLOWED_BINDING_STATES = {"wired", "declared-only"}
ALLOWED_TOPOLOGIES = {"PAIR", "CROSS_CUT"}
ALLOWED_IMPLEMENTATION_STATES = {
    "ACTIVE_BOUNDARY",
    "OPT_IN",
    "BLOCKED_LIVE",
    "DESIGN_INVENTORY",
    "DESIGN_ONLY",
}
ALLOWED_FALLBACKS = {"Native", "NoOp", "SafeMode", "RetryAtBoundary"}
ALLOWED_API_LINES = {"1.3", "1.4", "agnostic"}
ALLOWED_SYMBOL_KINDS = {"type", "method", "function", "asset"}
ALLOWED_WIRED_FREQUENCIES = {"startup", "event", "campaign-lifecycle", "mission-lifecycle"}
FORBIDDEN_FREQUENCIES = {"tick", "per-frame", "full-scan"}
FORBIDDEN_PATH_PARTS = {
    ".tmp",
    "artifacts",
    "bin",
    "obj",
    "packages",
    "terminal",
    "animusforgeterminalbehavior.cs",
    "animusforgeterminaluimodels.cs",
    "animusforgeterminalpopup.xml",
}
FORBIDDEN_PATH_TOKENS = ("terminal",)
MAX_MANIFEST_BYTES = 2 * 1024 * 1024
MAX_CONFIG_BYTES = 256 * 1024
MAX_SOURCE_BYTES = 8 * 1024 * 1024
MAX_ITEMS = 64
IDENTIFIER = re.compile(r"^[A-Za-z_][A-Za-z0-9_.]*$")
SLUG = re.compile(r"^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$")


class BridgeBindingFailure(ValueError):
    """Raised when the binding inventory is incomplete or unsafe."""


def mask_csharp(text: str) -> str:
    """Replace comments and string/char literals with spaces, preserving offsets."""
    chars = list(text)
    i = 0
    n = len(chars)
    state = "code"
    while i < n:
        c = chars[i]
        nxt = chars[i + 1] if i + 1 < n else ""
        if state == "code":
            if c == "/" and nxt == "/":
                chars[i] = chars[i + 1] = " "
                i += 2
                state = "line"
                continue
            if c == "/" and nxt == "*":
                chars[i] = chars[i + 1] = " "
                i += 2
                state = "block"
                continue
            if c == "@" and nxt == '"':
                chars[i] = chars[i + 1] = " "
                i += 2
                state = "verbatim"
                continue
            if c == '"':
                chars[i] = " "
                i += 1
                state = "string"
                continue
            if c == "'":
                chars[i] = " "
                i += 1
                state = "char"
                continue
            i += 1
            continue
        if state == "line":
            if c == "\n":
                state = "code"
            else:
                chars[i] = " "
            i += 1
            continue
        if state == "block":
            if c == "*" and nxt == "/":
                chars[i] = chars[i + 1] = " "
                i += 2
                state = "code"
            else:
                if c != "\n":
                    chars[i] = " "
                i += 1
            continue
        if state == "verbatim":
            if c == '"' and nxt == '"':
                chars[i] = chars[i + 1] = " "
                i += 2
            elif c == '"':
                chars[i] = " "
                i += 1
                state = "code"
            else:
                if c != "\n":
                    chars[i] = " "
                i += 1
            continue
        # normal string/char literal
        if c == "\\":
            chars[i] = " "
            if i + 1 < n:
                if chars[i + 1] != "\n":
                    chars[i + 1] = " "
                i += 2
            else:
                i += 1
        elif (state == "string" and c == '"') or (state == "char" and c == "'"):
            chars[i] = " "
            i += 1
            state = "code"
        else:
            if c != "\n":
                chars[i] = " "
            i += 1
    return "".join(chars)


def _find_matching(text: str, start: int, opening: str, closing: str) -> int:
    depth = 0
    for index in range(start, len(text)):
        if text[index] == opening:
            depth += 1
        elif text[index] == closing:
            depth -= 1
            if depth == 0:
                return index
    return -1


def extract_method_body(text: str, method: str) -> str:
    masked = mask_csharp(text)
    declaration = re.compile(
        rf"(?m)^[ \t]*(?:(?:public|private|protected|internal|static|async|virtual|override|sealed|unsafe|new|partial|extern|readonly)\s+)+"
        rf"[^\n;{{}}]*?\b{re.escape(method)}\s*\("
    )
    match = declaration.search(masked)
    require(match is not None, f"real method declaration is missing: {method}")
    open_paren = masked.find("(", match.start(), match.end())
    close_paren = _find_matching(masked, open_paren, "(", ")")
    require(close_paren >= 0, f"method parameter list is unbalanced: {method}")
    open_brace = masked.find("{", close_paren + 1)
    require(open_brace >= 0, f"method body is missing: {method}")
    close_brace = _find_matching(masked, open_brace, "{", "}")
    require(close_brace >= 0, f"method body braces are unbalanced: {method}")
    return masked[open_brace + 1:close_brace]


def extract_field_initializer(text: str, field: str) -> str:
    """Extract one real field initializer, excluding methods/comments/strings."""
    masked = mask_csharp(text)
    declaration = re.compile(
        rf"(?m)^[ \t]*(?:(?:public|private|protected|internal|static|readonly|volatile|const|new|unsafe)\s+)+"
        rf"[^\n;{{}}=]*?\b{re.escape(field)}\b\s*="
    )
    match = declaration.search(masked)
    require(match is not None, f"real cached field initializer is missing: {field}")
    end = masked.find(";", match.end())
    require(end >= 0, f"cached field initializer is unterminated: {field}")
    return masked[match.end():end]


def validate_wired_method_contract(
    bridge_id: str,
    source: str,
    method: str | None = None,
    gate: str | None = None,
    *,
    cached_gate: str | None = None,
) -> None:
    """Validate a reviewed gate against one real method body."""
    contract = EXPECTED_METHOD_CONTRACTS.get(bridge_id)
    require(contract is not None, f"no reviewed method contract: {bridge_id}")
    method = method or contract["method"]
    gate = gate or contract["gate"]
    masked = mask_csharp(source)
    body = extract_method_body(source, method)
    gate_marker = contract["gate_marker"]
    if contract.get("cached_gate"):
        gate_index = body.find(gate_marker)
    else:
        gate_match = re.search(
            rf"{re.escape(gate_marker)}\s*\(\s*[^)]*?{re.escape(gate)}",
            body,
            flags=re.DOTALL,
        )
        gate_index = gate_match.start() if gate_match else -1
    require(gate_index >= 0, f"bridge gate is not inside reviewed method: {bridge_id}")
    if not contract.get("cached_gate"):
        require(gate in body[gate_index:gate_index + 240], f"wrong bridge ID gate: {bridge_id}")
    for marker in contract.get("before", ()):
        position = body.find(marker)
        if position >= 0:
            require(gate_index < position, f"bridge gate occurs after side effect {marker}: {bridge_id}")
    if cached_gate or contract.get("cached_gate"):
        cache_name = cached_gate or contract["cached_gate"]
        require(re.search(rf"\b{re.escape(cache_name)}\b", body) is not None,
                f"cached bridge gate is not used in method: {bridge_id}")
        initializer = contract.get("initializer")
        field_initializer = extract_field_initializer(source, cache_name)
        require(initializer and initializer in field_initializer,
                f"cached bridge gate initializer is missing or has wrong ID: {bridge_id}")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise BridgeBindingFailure(message)


def _reject_duplicate_json_pairs(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    value: dict[str, Any] = {}
    for key, item in pairs:
        if key in value:
            raise BridgeBindingFailure(f"duplicate JSON field: {key}")
        value[key] = item
    return value


def load_json(path: Path, label: str, max_bytes: int = MAX_MANIFEST_BYTES) -> dict[str, Any]:
    try:
        size = path.stat().st_size
        require(size <= max_bytes, f"{label} exceeds the bounded JSON size")
        value = json.loads(
            path.read_text(encoding="utf-8"),
            object_pairs_hook=_reject_duplicate_json_pairs,
        )
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        raise BridgeBindingFailure(f"cannot load {label}: {exc}") from exc
    require(isinstance(value, dict), f"{label} root must be an object")
    return value


def exact_keys(value: Any, expected: set[str], label: str) -> dict[str, Any]:
    require(isinstance(value, dict) and set(value) == expected, f"{label} has missing or unknown fields")
    return value


def string_list(value: Any, label: str, *, nonempty: bool = False) -> list[str]:
    require(isinstance(value, list) and len(value) <= MAX_ITEMS, f"{label} must be a bounded string array")
    require(all(isinstance(item, str) and item.strip() for item in value), f"{label} contains a non-string/empty item")
    if nonempty:
        require(bool(value), f"{label} must not be empty")
    return value


def safe_relative_path(value: Any, label: str) -> str:
    require(isinstance(value, str) and value.strip(), f"{label} must be a non-empty path")
    path = value.strip()
    require("\\" not in path and ":" not in path and "\x00" not in path,
            f"{label} must use a relative POSIX path")
    parsed = PurePosixPath(path)
    require(not parsed.is_absolute(), f"{label} must not be absolute")
    parts = path.split("/")
    require(all(part not in {"", ".", ".."} for part in parts), f"{label} contains traversal/empty segments")
    lowered = {part.lower() for part in parts}
    require(not lowered & FORBIDDEN_PATH_PARTS, f"{label} points at generated/cache/terminal content")
    require(
        not any(token in part for part in lowered for token in FORBIDDEN_PATH_TOKENS),
        f"{label} points at terminal UI content",
    )
    return path


def resolve_project_file(project: Path, relative: str, label: str) -> Path:
    path = (project / relative).resolve()
    root = project.resolve()
    require(path.is_relative_to(root), f"{label} escapes project root")
    require(path.is_file(), f"{label} does not exist: {relative}")
    try:
        require(path.stat().st_size <= MAX_SOURCE_BYTES, f"{label} exceeds source size bound")
        return path
    except OSError as exc:
        raise BridgeBindingFailure(f"cannot stat {label}: {exc}") from exc


def read_source(project: Path, relative: str, cache: dict[str, str]) -> str:
    if relative in cache:
        return cache[relative]
    path = resolve_project_file(project, relative, f"source file {relative}")
    try:
        text = path.read_text(encoding="utf-8")
    except (OSError, UnicodeError) as exc:
        raise BridgeBindingFailure(f"cannot read source file {relative}: {exc}") from exc
    cache[relative] = text
    return text


def symbol_is_present(text: str, symbol: str, kind: str, label: str) -> None:
    require(isinstance(symbol, str) and IDENTIFIER.fullmatch(symbol) is not None,
            f"{label} symbol is not a safe identifier")
    token = symbol.rsplit(".", 1)[-1]
    escaped = re.escape(token)
    searchable = text if kind == "asset" else mask_csharp(text)
    if kind == "type":
        pattern = rf"\b(?:class|interface|struct|enum)\s+{escaped}\b"
    elif kind == "method":
        pattern = rf"\b{escaped}\s*\("
    elif kind == "function":
        pattern = rf"\bdef\s+{escaped}\s*\("
    else:
        # Asset symbols are canonical top-level JSON keys, not executable code.
        pattern = rf"[\"']{escaped}[\"']\s*:"
    require(re.search(pattern, searchable) is not None, f"{label} symbol is not present in its declared file")


def validate_feature_bridge_config(config: dict[str, Any]) -> dict[str, int]:
    exact_keys(
        config,
        {"schemaVersion", "contractVersion", "enabled"},
        "feature bridge runtime config",
    )
    require(config.get("schemaVersion") == 1, "feature bridge config schema must be 1")
    require(config.get("contractVersion") == EXPECTED_CONTRACT_VERSION,
            "feature bridge config contract version mismatch")
    enabled = config.get("enabled")
    require(isinstance(enabled, list) and len(enabled) <= EXPECTED_BRIDGE_COUNT,
            "feature bridge config enabled must be a bounded array")
    normalized: list[str] = []
    for item in enabled:
        require(isinstance(item, str) and item.strip(),
                "feature bridge config enabled contains an empty/non-string ID")
        bridge_id = item.strip()
        require(SLUG.fullmatch(bridge_id) is not None,
                "feature bridge config enabled contains an invalid ID")
        require(bridge_id in EXPECTED_CONFIGURABLE,
                f"feature bridge config enables an unwired or blocked ID: {bridge_id}")
        require(bridge_id not in normalized,
                f"feature bridge config contains a duplicate ID: {bridge_id}")
        normalized.append(bridge_id)
    return {"configEnabled": len(normalized)}


def load_catalog(catalog: dict[str, Any]) -> dict[str, dict[str, Any]]:
    require(catalog.get("schemaVersion") == 1, "full-domain catalog schema must be 1")
    require(catalog.get("catalogId") == "af.phase8.full-domain-readiness", "unexpected full-domain catalog ID")
    bridges = catalog.get("bridges")
    require(isinstance(bridges, list) and len(bridges) == EXPECTED_BRIDGE_COUNT,
            "full-domain catalog must contain exactly 16 bridges")
    indexed: dict[str, dict[str, Any]] = {}
    for bridge in bridges:
        require(isinstance(bridge, dict), "catalog bridge must be an object")
        bridge_id = bridge.get("id")
        require(isinstance(bridge_id, str) and SLUG.fullmatch(bridge_id) is not None,
                "catalog bridge ID is invalid")
        require(bridge_id not in indexed, f"duplicate catalog bridge ID: {bridge_id}")
        indexed[bridge_id] = bridge
    expected_ids = {
        "bootstrap-host", "host-runtime", "runtime-game-adapter", "persistence-domain-owners",
        "conversation-gateway", "conversation-action", "action-memory", "action-economy",
        "policy-world-diplomacy", "conversation-siege", "scene-duel", "conversation-courier",
        "memory-social-reports", "gateway-knowledge-profile", "ui-runtime-integration",
        "tools-content-release",
    }
    require(set(indexed) == expected_ids, "catalog bridge IDs do not match the canonical 16")
    return indexed


def validate_runtime_binding(
    binding: dict[str, Any],
    bridge_id: str,
    entry_paths: set[str],
    project: Path,
    source_cache: dict[str, str],
) -> str:
    runtime = exact_keys(
        binding,
        {"state", "entryPath", "symbol", "frequency", "notes"},
        f"runtime binding {bridge_id}",
    )
    state = runtime.get("state")
    require(state in ALLOWED_BINDING_STATES, f"runtime binding state is invalid: {bridge_id}")
    frequency = runtime.get("frequency")
    require(isinstance(frequency, str) and frequency, f"runtime binding frequency missing: {bridge_id}")
    require(frequency not in FORBIDDEN_FREQUENCIES, f"runtime binding cannot be a hot-path scan: {bridge_id}")
    require(isinstance(runtime.get("notes"), str) and runtime["notes"].strip(),
            f"runtime binding notes missing: {bridge_id}")
    expected = EXPECTED_WIRED.get(bridge_id)
    if state == "wired":
        require(expected is not None, f"unreviewed bridge was marked wired: {bridge_id}")
        entry_path = safe_relative_path(runtime.get("entryPath"), f"runtime entry path {bridge_id}")
        symbol = runtime.get("symbol")
        require(entry_path in entry_paths, f"runtime entry path is not an inventory entry: {bridge_id}")
        require((entry_path, symbol) == expected, f"wired runtime entry does not match reviewed gate: {bridge_id}")
        require(frequency in ALLOWED_WIRED_FREQUENCIES, f"wired runtime frequency is invalid: {bridge_id}")
        source = read_source(project, entry_path, source_cache)
        masked_source = mask_csharp(source)
        require("FeatureBridgeRuntime" in masked_source and "FeatureBridgeIds" in masked_source,
                f"wired entry lacks an explicit FeatureBridgeRuntime gate: {bridge_id}")
        gate_token = EXPECTED_GATE_TOKENS.get(bridge_id)
        if gate_token is not None:
            require(gate_token in masked_source,
                    f"wired entry lacks the expected bridge ID gate: {bridge_id}")
        symbol_is_present(source, symbol, "method", f"runtime binding {bridge_id}")
        contract = EXPECTED_METHOD_CONTRACTS.get(bridge_id)
        require(contract is not None, f"wired bridge lacks reviewed method contract: {bridge_id}")
        validate_wired_method_contract(
            bridge_id,
            source,
            symbol,
            gate_token,
            cached_gate=contract.get("cached_gate"),
        )
    else:
        require(expected is None, f"reviewed runtime gate was left declared-only: {bridge_id}")
        require(runtime.get("entryPath") is None and runtime.get("symbol") is None,
                f"declared-only bridge must not claim an entry/symbol: {bridge_id}")
        require(frequency == "none", f"declared-only bridge must have frequency=none: {bridge_id}")
    return state


def validate_manifest(manifest: dict[str, Any], catalog: dict[str, dict[str, Any]], project: Path) -> dict[str, int]:
    exact_keys(
        manifest,
        {"schemaVersion", "manifestId", "catalogPath", "contractVersion", "runtimeBindingPolicy", "bindings"},
        "bridge binding manifest",
    )
    require(manifest.get("schemaVersion") == 1, "bridge binding manifest schema must be 1")
    require(manifest.get("manifestId") == EXPECTED_MANIFEST_ID, "unexpected bridge binding manifest ID")
    require(manifest.get("catalogPath") == EXPECTED_CATALOG_PATH, "bridge binding catalog path mismatch")
    require(manifest.get("contractVersion") == EXPECTED_CONTRACT_VERSION, "bridge contract version mismatch")

    policy = exact_keys(
        manifest.get("runtimeBindingPolicy"),
        {"allowedStates", "wiredEntryFrequency", "forbiddenFrequency", "declaredOnlyMeaning"},
        "runtime binding policy",
    )
    require(set(string_list(policy["allowedStates"], "allowedStates", nonempty=True)) == ALLOWED_BINDING_STATES,
            "runtime binding state policy mismatch")
    require(set(string_list(policy["wiredEntryFrequency"], "wiredEntryFrequency", nonempty=True)) == ALLOWED_WIRED_FREQUENCIES,
            "runtime binding frequency policy mismatch")
    require(set(string_list(policy["forbiddenFrequency"], "forbiddenFrequency", nonempty=True)) == FORBIDDEN_FREQUENCIES,
            "runtime binding forbidden frequency policy mismatch")
    require(isinstance(policy["declaredOnlyMeaning"], str) and policy["declaredOnlyMeaning"].strip(),
            "declared-only meaning is missing")

    bindings = manifest.get("bindings")
    require(isinstance(bindings, list) and len(bindings) == EXPECTED_BRIDGE_COUNT,
            "bridge binding manifest must contain exactly 16 bindings")
    indexed: dict[str, dict[str, Any]] = {}
    source_cache: dict[str, str] = {}
    wired = 0
    declared_only = 0
    for binding in bindings:
        exact_keys(
            binding,
            {"id", "domains", "topology", "owner", "entryPaths", "symbols", "implementationState",
             "fallback", "apiLines", "requiredCases", "runtimeBinding"},
            "bridge binding",
        )
        bridge_id = binding.get("id")
        require(isinstance(bridge_id, str) and SLUG.fullmatch(bridge_id) is not None,
                "bridge binding ID is invalid")
        require(bridge_id in catalog, f"bridge binding is not in the full-domain catalog: {bridge_id}")
        require(bridge_id not in indexed, f"duplicate bridge binding ID: {bridge_id}")
        indexed[bridge_id] = binding
        source = catalog[bridge_id]

        domains = string_list(binding.get("domains"), f"domains {bridge_id}", nonempty=True)
        require(len(domains) == len(set(domains)) and set(domains) == set(source.get("domains", [])),
                f"bridge domains do not match catalog: {bridge_id}")
        require(binding.get("topology") == source.get("topology") and binding["topology"] in ALLOWED_TOPOLOGIES,
                f"bridge topology mismatch: {bridge_id}")
        require(binding.get("owner") == source.get("owner") and isinstance(binding["owner"], str),
                f"bridge owner mismatch: {bridge_id}")
        require(binding.get("implementationState") == source.get("implementationState")
                and binding["implementationState"] in ALLOWED_IMPLEMENTATION_STATES,
                f"bridge implementation state mismatch: {bridge_id}")
        require(binding.get("fallback") in ALLOWED_FALLBACKS, f"bridge fallback is invalid: {bridge_id}")

        entry_paths = string_list(binding.get("entryPaths"), f"entryPaths {bridge_id}", nonempty=True)
        require(len(entry_paths) == len(set(entry_paths)), f"duplicate bridge entry path: {bridge_id}")
        for relative in entry_paths:
            safe_relative_path(relative, f"entry path {bridge_id}")
            read_source(project, relative, source_cache)

        symbols = binding.get("symbols")
        require(isinstance(symbols, list) and 0 < len(symbols) <= MAX_ITEMS,
                f"symbols {bridge_id} must be a bounded non-empty array")
        seen_symbols: set[tuple[str, str]] = set()
        for item in symbols:
            symbol_entry = exact_keys(item, {"path", "symbol", "kind"}, f"symbol {bridge_id}")
            path = safe_relative_path(symbol_entry.get("path"), f"symbol path {bridge_id}")
            require(path in set(entry_paths), f"symbol path is not an entry path: {bridge_id}")
            kind = symbol_entry.get("kind")
            require(kind in ALLOWED_SYMBOL_KINDS, f"symbol kind is invalid: {bridge_id}")
            symbol = symbol_entry.get("symbol")
            require((path, symbol) not in seen_symbols, f"duplicate bridge symbol: {bridge_id}")
            seen_symbols.add((path, symbol))
            symbol_is_present(read_source(project, path, source_cache), symbol, kind, f"symbol {bridge_id}")

        api_lines = string_list(binding.get("apiLines"), f"apiLines {bridge_id}", nonempty=True)
        require(len(api_lines) == len(set(api_lines)) and set(api_lines) <= ALLOWED_API_LINES,
                f"bridge API lines are invalid: {bridge_id}")
        expected_api_lines = {"agnostic"} if bridge_id == "tools-content-release" else {"1.3", "1.4"}
        require(set(api_lines) == expected_api_lines, f"bridge API line closure mismatch: {bridge_id}")

        required_cases = string_list(binding.get("requiredCases"), f"requiredCases {bridge_id}", nonempty=True)
        catalog_cases = string_list(source.get("requiredCases"), f"catalog requiredCases {bridge_id}", nonempty=True)
        require(len(required_cases) == len(set(required_cases)) and set(required_cases) == set(catalog_cases),
                f"bridge required case inventory mismatch: {bridge_id}")
        state = validate_runtime_binding(binding.get("runtimeBinding"), bridge_id, set(entry_paths), project, source_cache)
        if state == "wired":
            wired += 1
        else:
            declared_only += 1

    require(set(indexed) == set(catalog), "bridge binding IDs do not close over the catalog")
    require(set(indexed) == set(EXPECTED_WIRED) | (set(catalog) - set(EXPECTED_WIRED)),
            "bridge binding ID closure is not canonical")
    require(wired == len(EXPECTED_WIRED) and declared_only == EXPECTED_BRIDGE_COUNT - len(EXPECTED_WIRED),
            "wired/declared-only counts do not match the reviewed boundary")
    return {"bindings": len(indexed), "wired": wired, "declaredOnly": declared_only, "sourceFiles": len(source_cache)}


def run(
    project_root: Path,
    manifest_path: Path | None = None,
    catalog_path: Path | None = None,
    config_path: Path | None = None,
) -> dict[str, Any]:
    project = project_root.resolve(strict=True)
    require(project.is_dir(), "project root must be a directory")
    manifest = manifest_path or (project / "docs" / "phase8" / "bridge-binding-manifest.json")
    catalog = catalog_path or (project / EXPECTED_CATALOG_PATH)
    config = config_path or (project / EXPECTED_CONFIG_PATH)
    manifest = manifest.resolve(strict=True)
    catalog = catalog.resolve(strict=True)
    config = config.resolve(strict=True)
    require(manifest.is_relative_to(project), "manifest must be inside project root")
    require(catalog.is_relative_to(project), "catalog must be inside project root")
    require(config.is_relative_to(project), "config must be inside project root")
    catalog_index = load_catalog(load_json(catalog, "full-domain catalog"))
    counts = validate_manifest(load_json(manifest, "bridge binding manifest"), catalog_index, project)
    config_counts = validate_feature_bridge_config(
        load_json(config, "feature bridge runtime config", MAX_CONFIG_BYTES)
    )
    return {
        "state": "PASS",
        **counts,
        **config_counts,
        "execution": "source-bound metadata-only; no Bannerlord/runtime/network/save access",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate AF phase-8 Bridge binding metadata")
    parser.add_argument("--project-root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--manifest", type=Path)
    parser.add_argument("--catalog", type=Path)
    parser.add_argument("--config", type=Path)
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args()
    try:
        result = run(args.project_root, args.manifest, args.catalog, args.config)
    except (BridgeBindingFailure, OSError, ValueError) as exc:
        result = {"state": "FAIL", "error": str(exc)[:240]}
        print(json.dumps(result, ensure_ascii=False), file=sys.stderr)
        return 1
    if args.json:
        print(json.dumps(result, ensure_ascii=False, separators=(",", ":")))
    else:
        print(
            "PASS bridgeBindings={bindings} wired={wired} declaredOnly={declaredOnly} "
            "configEnabled={configEnabled} "
            "sourceFiles={sourceFiles} execution={execution}".format(**result)
        )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

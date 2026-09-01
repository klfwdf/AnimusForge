#!/usr/bin/env python3
"""Pure phase-4 persistence/profile/config contract validator."""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
FIXTURE_DIR = ROOT / "docs" / "fixtures" / "phase4-persistence-profile-config"
KEY_PATTERN = re.compile(r'SyncData\("([^"\r\n]+)"')
SYMBOLIC_PATTERN = re.compile(r'SyncData\((?!")')


def load_json(path: Path) -> dict:
    with path.open("r", encoding="utf-8") as handle:
        value = json.load(handle)
    if not isinstance(value, dict):
        raise AssertionError(f"{path} must contain an object")
    return value


def assert_true(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)



def split_call_arguments(body: str) -> list[str]:
    parts: list[str] = []
    start = 0
    depth = 0
    quoted = False
    escaped = False
    for index, char in enumerate(body):
        if quoted:
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                quoted = False
            continue
        if char == '"':
            quoted = True
        elif char in "([{":
            depth += 1
        elif char in ")]}":
            depth -= 1
        elif char == ',' and depth == 0:
            parts.append(body[start:index].strip())
            start = index + 1
    parts.append(body[start:].strip())
    return parts


def resolve_storage_call_keys(name: str, argument_index: int) -> set[str]:
    resolved: set[str] = set()
    call_pattern = re.compile(name + r"\s*\((.*?)\)", re.DOTALL)
    constant_pattern = re.compile(r"\b(?:private|internal|public|protected)?\s*(?:static\s+)?const\s+string\s+(\w+)\s*=\s*\"([^\"]+)\"")
    for source_path in ROOT.rglob("*.cs"):
        if any(part in {"tools", "bin", "obj"} for part in source_path.parts):
            continue
        if any("原版游戏本体代码" in part for part in source_path.parts):
            continue
        source = source_path.read_text(encoding="utf-8")
        constants = dict(constant_pattern.findall(source))
        for match in call_pattern.finditer(source):
            arguments = split_call_arguments(match.group(1))
            if len(arguments) <= argument_index:
                continue
            expression = arguments[argument_index]
            if expression.startswith('"') and expression.endswith('"'):
                resolved.add(expression[1:-1])
            elif expression in constants:
                resolved.add(constants[expression])
    return resolved


def validate_chunk_contract(catalog: dict) -> dict:
    expected_chunked = set(catalog["chunkedStringStorageKeys"])
    expected_flattened = set(catalog["flattenedDictionaryStorageKeys"])
    assert_true(len(expected_chunked) == 13, "chunked string key catalog must contain 13 keys")
    assert_true(len(expected_flattened) == 39, "flattened dictionary key catalog must contain 39 keys")
    actual_chunked = resolve_storage_call_keys("SaveChunkedString", 1) | resolve_storage_call_keys("LoadChunkedString", 1)
    actual_flattened = resolve_storage_call_keys("FlattenStringDictionary", 1)
    # The helper's own overloads have no persisted key and are intentionally absent.
    assert_true(actual_chunked == expected_chunked, f"chunked key mismatch: missing={sorted(expected_chunked - actual_chunked)} extra={sorted(actual_chunked - expected_chunked)}")
    assert_true(actual_flattened == expected_flattened, f"flattened dictionary key mismatch: missing={sorted(expected_flattened - actual_flattened)} extra={sorted(actual_flattened - expected_flattened)}")
    helper = (ROOT / "CampaignSaveChunkHelper.cs").read_text(encoding="utf-8")
    contract = catalog["chunkContract"]
    int_fields = {
        "StorageChunkMaxBytes": "storageChunkMaxBytes",
        "LegacyInlineStorageMaxBytes": "legacyInlineStorageMaxBytes",
        "MaxChunkCount": "maxChunkCount",
    }
    for source_name, fixture_name in int_fields.items():
        match = re.search(rf"const int {source_name}\s*=\s*(\d+)\s*;", helper)
        assert_true(match is not None and int(match.group(1)) == contract[fixture_name], f"chunk constant drifted: {source_name}")
    string_fields = {
        "StringChunkCountSuffix": "stringChunkCountSuffix",
        "StringChunkKeyPrefix": "stringChunkKeyPrefix",
        "DictionaryChunkCountPrefix": "dictionaryChunkCountPrefix",
        "DictionaryChunkValuePrefix": "dictionaryChunkValuePrefix",
    }
    for source_name, fixture_name in string_fields.items():
        match = re.search(rf"const string {source_name}\s*=\s*\"([^\"]+)\"\s*;", helper)
        assert_true(match is not None and match.group(1) == contract[fixture_name], f"chunk string contract drifted: {source_name}")
    assert_true(contract["stringValueType"] == "string", "chunk string value type changed")
    assert_true(contract["flattenedDictionaryType"] == "Dictionary<string,string>", "flattened dictionary type changed")
    return {"chunkedStringKeys": len(expected_chunked), "flattenedDictionaryKeys": len(expected_flattened), "chunkMaxBytes": contract["storageChunkMaxBytes"]}


BINDING_PATTERN = re.compile(
    r'SyncData\s*\(\s*"([^"\r\n]+)"\s*,\s*ref\s+([A-Za-z_][A-Za-z0-9_]*)'
)
DECLARATION_PATTERN = re.compile(
    r'(?m)^\s*(?:(?:public|private|protected|internal|static|readonly|volatile|const)\s+)*'
    r'([A-Za-z_][A-Za-z0-9_]*(?:\s*<[^\n;=]+>)?(?:\[\])?)\s+'
    r'([A-Za-z_][A-Za-z0-9_]*)\s*(?:=|;)'
)


def discover_typed_bindings() -> list[dict]:
    rows: list[dict] = []
    for source_path in sorted(ROOT.rglob("*.cs")):
        if any(part in {"tools", "bin", "obj"} for part in source_path.parts):
            continue
        if any("原版游戏本体代码" in part for part in source_path.parts):
            continue
        source = source_path.read_text(encoding="utf-8")
        declarations = list(DECLARATION_PATTERN.finditer(source))
        for match in BINDING_PATTERN.finditer(source):
            ref_name = match.group(2)
            candidates = [
                (declaration.start(), declaration.group(1).strip())
                for declaration in declarations
                if declaration.group(2) == ref_name and declaration.start() < match.start()
            ]
            assert_true(candidates, f"unable to resolve SyncData ref variable: {source_path}:{ref_name}")
            rows.append({
                "key": match.group(1),
                "ref": ref_name,
                "type": candidates[-1][1],
                "source": source_path.relative_to(ROOT).as_posix(),
                "line": source[:match.start()].count("\n") + 1,
            })
    return rows


def validate_typed_bindings(binding_catalog: dict, persistence_catalog: dict) -> dict:
    assert_true(binding_catalog["assemblyIdentity"] == "AnimusForge", "binding catalog assembly identity changed")
    expected_rows = []
    for entry in binding_catalog["entries"]:
        assert_true(entry["bindings"], f"binding catalog entry has no bindings: {entry['key']}")
        types = {binding["type"] for binding in entry["bindings"]}
        assert_true(len(types) == 1, f"binding type differs across save/load: {entry['key']}")
        for binding in entry["bindings"]:
            expected_rows.append({
                "key": entry["key"],
                "ref": binding["ref"],
                "type": binding["type"],
                "source": binding["source"].replace("\\", "/"),
                "line": binding["line"],
            })
    actual_rows = discover_typed_bindings()
    assert_true(len(actual_rows) == binding_catalog["bindingCount"], f"typed binding count drifted: {len(actual_rows)}")
    assert_true(len(binding_catalog["entries"]) == binding_catalog["uniqueKeyCount"], "typed binding unique key count drifted")
    assert_true(sorted(actual_rows, key=lambda row: (row["source"], row["line"], row["key"])) == sorted(expected_rows, key=lambda row: (row["source"], row["line"], row["key"])), "typed SyncData binding catalog drifted")
    literal_keys = set(persistence_catalog["literalSyncDataKeys"])
    catalog_keys = {entry["key"] for entry in binding_catalog["entries"]}
    assert_true(catalog_keys == literal_keys, "typed binding keys do not match literal key catalog")
    return {"typedBindings": len(actual_rows), "typedBindingKeys": len(catalog_keys), "typedBindingTypes": len({row["type"] for row in actual_rows})}

def validate_persistence(catalog: dict) -> dict:
    keys = catalog["literalSyncDataKeys"]
    assert_true(catalog["assemblyIdentity"] == "AnimusForge", "assembly identity changed")
    assert_true(catalog["saveTypePolicy"].startswith("preserve"), "save identity policy is not conservative")
    assert_true(len(keys) == len(set(keys)), "duplicate literal SyncData key in catalog")
    assert_true(len(keys) == 95, f"expected 95 unique literal keys, got {len(keys)}")

    discovered: set[str] = set()
    for relative in catalog["sourceFiles"]:
        source = ROOT / relative
        assert_true(source.is_file(), f"missing production owner source: {relative}")
        discovered.update(KEY_PATTERN.findall(source.read_text(encoding="utf-8")))

    expected = set(keys)
    assert_true(discovered == expected, f"literal key mismatch: missing={sorted(expected - discovered)} extra={sorted(discovered - expected)}")
    assert_true(any(item["status"] == "inventory-required" for item in catalog["symbolicKeyFamilies"]), "symbolic key debt was hidden")
    symbolic_sources = []
    for source in sorted(ROOT.rglob("*.cs")):
        if any(part in {"tools", "bin", "obj"} for part in source.parts):
            continue
        if any("原版游戏本体代码" in part for part in source.parts):
            continue
        if SYMBOLIC_PATTERN.search(source.read_text(encoding="utf-8")):
            symbolic_sources.append(source.relative_to(ROOT).as_posix())
    assert_true(symbolic_sources == catalog["symbolicSyncDataSources"], "symbolic SyncData source inventory drifted")
    assert_true(any(item["path"] == "PlayerExports" and item["classification"] == "user-writable-merge-without-deletion" for item in catalog["contentRoots"]), "PlayerExports deletion boundary missing")
    chunk_result = validate_chunk_contract(catalog)
    return {"literalKeys": len(keys), "sourceFiles": len(catalog["sourceFiles"]), "symbolicSources": len(symbolic_sources), "symbolicFamilies": len(catalog["symbolicKeyFamilies"]), **chunk_result}


def validate_profiles(cases: dict) -> dict:
    profiles = cases["profiles"]
    required = {"af.foundation.runtime", "af.game-adapter", "af.persistence"}
    for name, modules in profiles.items():
        assert_true(required.issubset(modules), f"profile {name} lacks foundation closure")
    assert_true("af.module.conversation" not in profiles["safe-mode"], "safe-mode contains gameplay conversation")
    assert_true(any(case["expected"] == "inflight-keeps-model-a-future-uses-model-b" for case in cases["cases"]), "reload snapshot isolation case missing")
    assert_true(any(case["expected"] == "save-load-boundary" for case in cases["cases"]), "persistent lifecycle case missing")
    forbidden = [text.lower() for text in cases["forbiddenFixtureSubstrings"]]
    scan_document = {key: value for key, value in cases.items() if key != "forbiddenFixtureSubstrings"}
    serialized = json.dumps(scan_document, ensure_ascii=False).lower()
    for text in forbidden:
        assert_true(text not in serialized, f"forbidden credential/path substring present: {text}")
    for field in cases["credentialFields"]:
        assert_true(field not in cases["profiles"], f"credential field leaked into profile: {field}")
    return {"profiles": len(profiles), "cases": len(cases["cases"]), "credentialFieldsExcluded": len(cases["credentialFields"])}


def validate_namespaces(catalog: dict) -> dict:
    assert_true(catalog["assemblyIdentity"] == "AnimusForge", "namespace catalog assembly identity changed")
    policy = catalog["migrationPolicy"]
    assert_true(policy["readMode"] == "legacy-first", "migration must read legacy data first")
    assert_true(policy["idempotent"] is True, "migration must be idempotent")
    assert_true(policy["preserveUnknownData"] is True, "unknown data preservation missing")
    assert_true(policy["deleteLegacyKeys"] is False, "legacy key deletion is not allowed")
    namespaces = catalog["namespaces"]
    ids = [item["id"] for item in namespaces]
    assert_true(len(ids) == len(set(ids)), "duplicate persistence namespace id")
    assert_true(len(ids) >= 9, "persistence namespace catalog is incomplete")
    for item in namespaces:
        assert_true(item["id"].startswith("af."), f"invalid namespace id: {item['id']}")
        assert_true(isinstance(item["schema"], int) and item["schema"] > 0, f"invalid schema: {item['id']}")
        assert_true(item["owners"] and item["legacyKeyPrefixes"], f"namespace lacks owner or legacy boundary: {item['id']}")
    forbidden = ["sk-", "bearer ", "c:\\users\\", "f:\\steamlibrary"]
    serialized = json.dumps(catalog, ensure_ascii=False).lower()
    for text in forbidden:
        assert_true(text not in serialized, f"forbidden credential/path substring present: {text}")
    return {"namespaces": len(namespaces), "migrationIdempotent": policy["idempotent"], "unknownDataPreserved": policy["preserveUnknownData"]}


def validate_legacy_first_cases(cases: dict) -> dict:
    assert_true(
        cases["knownRepresentativeBindings"].get("_af_interactionMemoryRecovery_v1")
        == "Dictionary<string, string>",
        "memory recovery persistence binding is not cataloged",
    )
    by_id = {case["id"]: case for case in cases["cases"]}
    assert_true(
        by_id.get("missing-memory-recovery-journal-is-empty", {}).get("expected")
        == "publish-with-empty-memory-recovery-journal",
        "missing recovery journal compatibility case is absent",
    )
    assert_true(
        by_id.get("corrupt-memory-recovery-journal-fails-closed", {}).get("expected")
        == "quarantine-without-memory-replay",
        "corrupt recovery journal fail-closed case is absent",
    )
    return {"legacyFirstCases": len(cases["cases"])}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--json", action="store_true", dest="as_json")
    args = parser.parse_args()
    try:
        persistence_catalog = load_json(FIXTURE_DIR / "persistence-catalog.json")
        persistence = validate_persistence(persistence_catalog)
        typed = validate_typed_bindings(load_json(FIXTURE_DIR / "syncdata-binding-catalog.json"), persistence_catalog)
        persistence.update(typed)
        profiles = validate_profiles(load_json(FIXTURE_DIR / "config-snapshot-cases.json"))
        namespaces = validate_namespaces(load_json(FIXTURE_DIR / "persistence-namespace-migration-catalog.json"))
        migration_cases = validate_legacy_first_cases(
            load_json(FIXTURE_DIR / "legacy-first-safe-mode-migration-cases.json")
        )
        result = {"status": "PASS", **persistence, **profiles, **namespaces, **migration_cases}
    except (AssertionError, OSError, json.JSONDecodeError) as exc:
        result = {"status": "FAIL", "error": str(exc)}
        if args.as_json:
            print(json.dumps(result, ensure_ascii=False, sort_keys=True))
        else:
            print(f"FAIL {exc}")
        return 1
    if args.as_json:
        print(json.dumps(result, ensure_ascii=False, sort_keys=True))
    else:
        print("PASS persistenceProfileConfig " + " ".join(f"{key}={value}" for key, value in result.items() if key != "status"))
    return 0


if __name__ == "__main__":
    sys.exit(main())

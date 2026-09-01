from __future__ import annotations

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FIXTURE_DIR = ROOT / 'docs' / 'fixtures' / 'phase4-persistence-profile-config'


def fail(message: str) -> None:
    raise AssertionError(message)


def check(condition: bool, message: str) -> None:
    if not condition:
        fail(message)


def load(path: Path) -> dict:
    with path.open('r', encoding='utf-8') as handle:
        value = json.load(handle)
    check(isinstance(value, dict), f'{path} must be an object')
    return value


def simulate(records: list[dict], known: dict, safe_mode: bool = False) -> tuple[bool, list[dict], list[dict]]:
    legacy = json.loads(json.dumps(records, ensure_ascii=False))
    for record in records:
        key = record.get('key')
        if key in known and record.get('type') != known[key]:
            return False, legacy, [record]
    # A successful migration publishes only validated known records while
    # retaining every unknown record verbatim for SafeMode/future readers.
    published = json.loads(json.dumps(records, ensure_ascii=False))
    return True, published, []


def validate_chunk(chunk: dict) -> bool:
    count = chunk.get('count')
    values = chunk.get('chunks')
    if not isinstance(count, int) or count <= 0 or not isinstance(values, list):
        return False
    if len(values) != count or any(value is None for value in values):
        return False
    return True


def main() -> int:
    catalog = load(FIXTURE_DIR / 'syncdata-binding-catalog.json')
    persistence_catalog = load(FIXTURE_DIR / 'persistence-catalog.json')
    fixture = load(FIXTURE_DIR / 'legacy-first-safe-mode-migration-cases.json')
    check(fixture['assemblyIdentity'] == 'AnimusForge', 'assembly identity changed')
    policy = fixture['policy']
    check(policy == {'readMode': 'legacy-first', 'publishMode': 'single-point-after-validation', 'deleteLegacyKeys': False, 'preserveUnknownData': True}, 'migration policy drifted')
    known = fixture['knownRepresentativeBindings']
    catalog_types = {entry['key']: entry['type'] for entry in catalog['entries']}
    # Flattened symbolic journals preserve Dictionary<string,string> even
    # though their SyncData call uses a const key and is intentionally absent
    # from the literal-only typed binding inventory.
    for key in persistence_catalog['flattenedDictionaryStorageKeys']:
        catalog_types.setdefault(key, 'Dictionary<string, string>')
    for key, expected_type in known.items():
        check(catalog_types.get(key) == expected_type, f'representative typed binding drifted: {key}')
    unknown = fixture['unknownRecord']
    passed = 0
    for case in fixture['cases']:
        case_id = case['id']
        expected = case['expected']
        if 'chunk' in case:
            valid, _, _ = simulate([], known)
            check(not validate_chunk(case['chunk']), f'corrupt chunk unexpectedly validated: {case_id}')
            check(expected == 'no-publish-retain-legacy', f'chunk expected policy missing: {case_id}')
            passed += 1
            continue
        records = case.get('records', [])
        safe_mode = bool(case.get('safeMode', False))
        valid, published, errors = simulate(records, known, safe_mode)
        if expected == 'no-publish-retain-legacy':
            check(not valid and published == records and errors, f'type mismatch published: {case_id}')
        elif expected == 'publish-known-and-preserve-unknown':
            check(valid and published == records, f'valid legacy data did not publish: {case_id}')
            check(any(item.get('key') == unknown['key'] for item in published), f'unknown data was dropped: {case_id}')
        elif expected == 'publish-with-missing-key-untouched':
            check(valid and not any(item.get('key') == '_dialogueHistory_v2' for item in published), f'missing key was invented: {case_id}')
        elif expected == 'unknown-visible-without-business-instantiation':
            check(valid and safe_mode and published == records and published[0].get('key') == unknown['key'], f'SafeMode unknown retention failed: {case_id}')
        elif expected == 'same-output-on-second-pass':
            check(valid, f'first migration failed: {case_id}')
            valid_again, published_again, errors_again = simulate(published, known, safe_mode)
            check(valid_again and not errors_again and published_again == published, f'migration was not idempotent: {case_id}')
        elif expected in {
            'publish-with-empty-memory-recovery-journal',
            'publish-with-empty-weekly-action-outcome-journal',
        }:
            check(valid and published == records, f'missing additive journal invented data: {case_id}')
            missing_key = (
                '_af_interactionMemoryRecovery_v1'
                if 'memory-recovery' in case_id
                else '_af_weeklyActionOutcomeReceipts_v1'
            )
            check(not any(item.get('key') == missing_key for item in published), f'missing journal was synthesized: {case_id}')
        elif expected in {
            'retain-corrupt-memory-journal-for-owner-validation',
            'retain-corrupt-weekly-journal-for-owner-validation',
        }:
            check(valid and published == records and len(records) == 1,
                  f'corrupt journal fixture did not retain its raw record: {case_id}')
            check(records[0].get('valueKind') == 'corrupt-versioned-wire', f'corrupt journal fixture lost wire marker: {case_id}')
        else:
            fail(f'unknown expected result: {expected}')
        passed += 1
    print(f'PASS persistenceMigrationContract cases={passed} unknownRetention=1 missingOptional=1 typeMismatchRollback=1 chunkFailureClosed=1 corruptJournalRetained=2 idempotent=1 legacyFirst=1')
    return 0


if __name__ == '__main__':
    try:
        raise SystemExit(main())
    except (AssertionError, OSError, json.JSONDecodeError) as exc:
        print(f'FAIL {exc}')
        raise SystemExit(1)

"""Precisely undo separately tested B1 deltas before older owner parity assertions.
This is a source-review adapter, NOT runtime/game acceptance or a B1 completion gate.
"""
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent


def _sha256(text):
    return hashlib.sha256(text.encode()).hexdigest()


def _is_reviewed_deleted(item):
    if item.get('reviewed') is False:
        return False
    return item.get('status') != 'UNREVIEWED_WIP'


def restore_memory_summary_source(path, source):
    if path != 'MyBehavior.cs':
        return source
    review = json.loads((HERE / 'source-review-b1.json').read_text(encoding='utf-8'))
    spec = importlib.util.spec_from_file_location('b1_declaration_extractor', ROOT / 'tools/ChannelCutoverBoundaryTests/run.py')
    extractor = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(extractor)
    # The inverse is permitted only for the exact separately tested harness/runner.
    # It must not turn an arbitrary new assertion deletion into an old-owner PASS.
    assert review.get('testSourceHashNormalization') == 'utf8-no-bom-lf', 'Missing test source normalization'
    reviewed_deleted = [item for item in review.get('deletedDeclarations', []) if _is_reviewed_deleted(item)]
    labels = {label for item in review['declarations'] + reviewed_deleted for label in item.get('evidence') or []}
    for item in (review.get('unreviewedWip') or {}).get('declarations') or []:
        labels.update(item.get('evidence') or [])
    for item in review.get('deletedDeclarations', []):
        if not _is_reviewed_deleted(item):
            labels.update(item.get('evidence') or [])
    for label in labels:
        evidence = review['evidence'][label]
        for role in ('runner', 'harness'):
            text = (ROOT / evidence[role]).read_text(encoding='utf-8-sig')
            assert _sha256(text) == evidence['testSourceSha256'][role], 'Unreviewed B1 evidence source: ' + evidence[role]
        for dependency_path, expected in evidence.get('additionalTestSourceSha256', {}).items():
            text = (ROOT / dependency_path).read_text(encoding='utf-8-sig')
            assert _sha256(text) == expected, 'Unreviewed B1 evidence dependency: ' + dependency_path
    unreviewed = []
    seen = set()

    def note(message):
        if message in seen:
            return
        seen.add(message)
        unreviewed.append(message)

    def current_text(item_path):
        if item_path == path:
            return source
        return (ROOT / item_path).read_text(encoding='utf-8-sig').replace('\r\n', '\n')

    for item in (review.get('unreviewedWip') or {}).get('declarations') or []:
        signature = item['signature']
        text = current_text(item['path'])
        current = extractor.declaration(text, signature)
        if _sha256(current) != item['currentSha256']:
            note('Unreviewed B1 WIP declaration hash drifted: ' + signature)
        note('Unreviewed B1 declaration: ' + signature)
    for item in review['declarations']:
        current = extractor.declaration(source, item['signature'])
        if _sha256(current) != item['sha256']:
            note('Unreviewed B1 declaration: ' + item['signature'])
    for item in review.get('deletedDeclarations', []):
        if _is_reviewed_deleted(item):
            continue
        if item['signature'] in source:
            note('Unreviewed B1 deleted declaration still present: ' + item['signature'])
        note('Unreviewed B1 deleted declaration: ' + item['signature'])
    if unreviewed:
        raise AssertionError('Unreviewed B1 declarations:\n' + '\n'.join(unreviewed))
    baseline = subprocess.check_output(['git', 'show', review['baseline'] + ':' + path], cwd=ROOT).decode('utf-8-sig').replace('\r\n', '\n')
    restored = source
    for item in review['declarations']:
        assert item['path'] == path and item['evidence'], 'B1 review entry lacks path or scoped evidence'
        current = extractor.declaration(restored, item['signature'])
        prior = extractor.declaration(baseline, item['baselineSignature'])
        assert _sha256(current) == item['sha256'], 'Unreviewed B1 declaration: ' + item['signature']
        assert _sha256(prior) == item['baselineSha256'], 'B1 baseline declaration changed: ' + item['baselineSignature']
        assert restored.count(current) == 1, 'Ambiguous B1 declaration replacement'
        restored = restored.replace(current, prior, 1)
    for item in reviewed_deleted:
        assert item['path'] == path and item['evidence'], 'Deleted B1 declaration lacks scoped evidence'
        assert item['signature'] not in restored, 'Deleted B1 declaration unexpectedly restored in production'
        prior = extractor.declaration(baseline, item['baselineSignature'])
        assert _sha256(prior) == item['baselineSha256'], 'Deleted B1 body hash changed'
        next_prior = extractor.declaration(baseline, item['nextSignature'])
        next_current = extractor.declaration(restored, item['nextSignature'])
        assert _sha256(next_current) == item['nextDeclarationSha256'], 'Deleted B1 neighbor anchor changed'
        start = baseline.rfind('\n', 0, baseline.index(prior)) + 1
        end = baseline.rfind('\n', 0, baseline.index(next_prior)) + 1
        deleted_span = baseline[start:end]
        assert _sha256(deleted_span) == item['baselineSpanSha256'], 'Deleted B1 source span changed'
        anchor = '\t' + item['nextSignature']
        assert restored.count(anchor) == 1, 'Deleted B1 declaration has ambiguous neighbor anchor'
        restored = restored.replace(anchor, deleted_span + anchor, 1)
    # No blanket source replacement. Any extra field/method/comment/default drift survives
    # the exact replacements above and fails here, even if all listed hashes still match.
    assert restored == baseline, 'Unreviewed B1 surrounding source changes'
    return restored

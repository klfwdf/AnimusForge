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

def restore_memory_summary_source(path, source):
    if path != 'MyBehavior.cs':
        return source
    review = json.loads((HERE / 'source-review-b1.json').read_text(encoding='utf-8'))
    spec = importlib.util.spec_from_file_location('b1_declaration_extractor', ROOT / 'tools/ChannelCutoverBoundaryTests/run.py')
    extractor = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(extractor)
    baseline = subprocess.check_output(['git', 'show', review['baseline'] + ':' + path], cwd=ROOT).decode('utf-8-sig').replace('\r\n', '\n')
    # The inverse is permitted only for the exact separately tested harness/runner.
    # It must not turn an arbitrary new assertion deletion into an old-owner PASS.
    assert review.get('testSourceHashNormalization') == 'utf8-no-bom-lf', 'Missing test source normalization'
    labels = {label for item in review['declarations'] + review.get('deletedDeclarations', []) for label in item['evidence']}
    for label in labels:
        evidence = review['evidence'][label]
        for role in ('runner', 'harness'):
            text = (ROOT / evidence[role]).read_text(encoding='utf-8-sig')
            assert hashlib.sha256(text.encode()).hexdigest() == evidence['testSourceSha256'][role], 'Unreviewed B1 evidence source: ' + evidence[role]
        for dependency_path, expected in evidence.get('additionalTestSourceSha256', {}).items():
            text = (ROOT / dependency_path).read_text(encoding='utf-8-sig')
            assert hashlib.sha256(text.encode()).hexdigest() == expected, 'Unreviewed B1 evidence dependency: ' + dependency_path
    restored = source
    for item in review['declarations']:
        assert item['path'] == path and item['evidence'], 'B1 review entry lacks path or scoped evidence'
        current = extractor.declaration(restored, item['signature'])
        prior = extractor.declaration(baseline, item['baselineSignature'])
        assert hashlib.sha256(current.encode()).hexdigest() == item['sha256'], 'Unreviewed B1 declaration: ' + item['signature']
        assert hashlib.sha256(prior.encode()).hexdigest() == item['baselineSha256'], 'B1 baseline declaration changed: ' + item['baselineSignature']
        assert restored.count(current) == 1, 'Ambiguous B1 declaration replacement'
        restored = restored.replace(current, prior, 1)
    for item in review.get('deletedDeclarations', []):
        assert item['path'] == path and item['evidence'], 'Deleted B1 declaration lacks scoped evidence'
        assert item['signature'] not in restored, 'Deleted B1 declaration unexpectedly restored in production'
        prior = extractor.declaration(baseline, item['baselineSignature'])
        assert hashlib.sha256(prior.encode()).hexdigest() == item['baselineSha256'], 'Deleted B1 body hash changed'
        next_prior = extractor.declaration(baseline, item['nextSignature'])
        next_current = extractor.declaration(restored, item['nextSignature'])
        assert hashlib.sha256(next_current.encode()).hexdigest() == item['nextDeclarationSha256'], 'Deleted B1 neighbor anchor changed'
        start = baseline.rfind('\n', 0, baseline.index(prior)) + 1
        end = baseline.rfind('\n', 0, baseline.index(next_prior)) + 1
        deleted_span = baseline[start:end]
        assert hashlib.sha256(deleted_span.encode()).hexdigest() == item['baselineSpanSha256'], 'Deleted B1 source span changed'
        anchor = '\t' + item['nextSignature']
        assert restored.count(anchor) == 1, 'Deleted B1 declaration has ambiguous neighbor anchor'
        restored = restored.replace(anchor, deleted_span + anchor, 1)
    # No blanket source replacement. Any extra field/method/comment/default drift survives
    # the exact replacements above and fails here, even if all listed hashes still match.
    assert restored == baseline, 'Unreviewed B1 surrounding source changes'
    return restored

"""Restore only the reviewed input-boundary declarations for older whole-owner proofs."""
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[2]

def restore_input_source(path, source):
    if path != 'MyBehavior.cs':
        return source
    spec = importlib.util.spec_from_file_location('input_extractor', ROOT / 'tools/ChannelCutoverBoundaryTests/run.py')
    extract = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(extract)
    review = json.loads((Path(__file__).parent / 'source-review.json').read_text(encoding='utf-8'))
    prior = subprocess.check_output(['git', 'show', review['baseline'] + ':' + path], cwd=ROOT).decode('utf-8-sig')
    for item in review['declarations']:
        current = extract.declaration(source, item['signature'])
        assert hashlib.sha256(current.encode()).hexdigest() == item['sha256'], 'Unreviewed memory input declaration: ' + item['signature']
        assert 'TeamModuleServices.' not in current
        source = source.replace(current, extract.declaration(prior, item['signature']), 1)
    assert source == prior, 'Changes outside reviewed memory input declarations'
    return source

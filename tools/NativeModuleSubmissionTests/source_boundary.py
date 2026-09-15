"""Exact reviewed additive Native API changes; reject any unrelated change before restoring older proof."""
from pathlib import Path
import hashlib,json,subprocess
ROOT=Path(__file__).resolve().parents[2]
REVIEW=json.loads((Path(__file__).parent/'source-review.json').read_text(encoding='utf-8'))
def restore(path,current):
    path=str(path).replace('\\','/')
    for dependency,expected_hash in REVIEW.get('dependencies',{}).items():
        assert hashlib.sha256((ROOT/dependency).read_text(encoding='utf-8-sig').encode()).hexdigest()==expected_hash, 'Unreviewed Native API dependency: '+dependency
    evidence=REVIEW['files'].get(path)
    if evidence is None: return current
    old=subprocess.check_output(['git','show',REVIEW['baseline']+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
    assert hashlib.sha256(old.encode()).hexdigest()==evidence['beforeSha256'], 'Native API baseline mismatch: '+path
    expected=old.splitlines(keepends=True)
    for h in reversed(evidence['hunks']):
        assert ''.join(expected[h['oldStart']:h['oldEnd']])==h['before'], 'Native API reviewed hunk mismatch: '+path
        expected[h['oldStart']:h['oldEnd']]=h['after'].splitlines(keepends=True)
    expected=''.join(expected)
    assert hashlib.sha256(expected.encode()).hexdigest()==evidence['afterSha256'], 'Native API candidate hash mismatch: '+path
    assert (ROOT/path).read_text(encoding='utf-8-sig')==expected, 'Unreviewed Native API live source: '+path
    assert current in (old,expected), 'Unexpected Native API proof input: '+path
    return old
if __name__=='__main__':
    for path in REVIEW['files']:restore(path,(ROOT/path).read_text(encoding='utf-8-sig'))
    print('PASS exact Native API inverse: '+str(len(REVIEW['files']))+' reviewed production files')

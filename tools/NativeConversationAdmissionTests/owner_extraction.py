"""Strict J07b admission ownership inverse; current behavior is tested with the real owner."""
from pathlib import Path
import hashlib,json,subprocess
ROOT=Path(__file__).resolve().parents[2]
REVIEW=json.loads((ROOT/'tests/modules/AF.Module.Conversation/NativeTicket/source-review.json').read_text(encoding='utf-8'))
def restore(path, source):
    evidence=REVIEW['files'].get(path)
    if evidence is None: return source
    original=subprocess.check_output(['git','show',REVIEW['baseline']+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
    assert hashlib.sha256(original.encode()).hexdigest()==evidence['beforeSha256'], 'J07b original digest: '+path
    if source == original: return source
    expected=original
    for edit in evidence['edits']:
        assert expected.count(edit['before'])==edit['count'], 'J07b exact replacement count: '+path
        expected=expected.replace(edit['before'],edit['after'])
    assert hashlib.sha256(expected.encode()).hexdigest()==evidence['afterSha256'], 'J07b candidate digest: '+path
    assert source==expected, 'Unreviewed J07b source drift: '+path
    return original
if __name__=='__main__':
    for path in REVIEW['files']:
        restore(path,(ROOT/path).read_text(encoding='utf-8-sig'))
        print('PASS exact admission-owner inverse '+path)

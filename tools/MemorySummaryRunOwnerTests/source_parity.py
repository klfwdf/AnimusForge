from pathlib import Path
import subprocess,json,hashlib
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent

def restore(path,source):
    path=str(path).replace(chr(92),"/")
    review=json.loads((HERE/'source-review.json').read_text(encoding='utf-8'))
    if path not in review['paths']: return source
    for file,h in review.get('dependencies',{}).items():
        assert hashlib.sha256((ROOT/file).read_text(encoding='utf-8-sig').encode()).hexdigest()==h,'Unreviewed memory-run dependency: '+file
    old=subprocess.check_output(['git','show',review['baseline']+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n');lines=old.splitlines(keepends=True)
    for delta in reversed(review['paths'][path]):
        a,b=delta['start'],delta['end'];assert ''.join(lines[a:b])==delta['before'];lines[a:b]=[delta['after']]
    expected=''.join(lines)
    assert (ROOT/path).read_text(encoding='utf-8-sig')==expected,'Unreviewed live memory-run source: '+path
    assert source in (expected,old),'Unreviewed memory-run source changes: '+path
    return old
if __name__=='__main__':
    for path in json.loads((HERE/'source-review.json').read_text(encoding='utf-8'))['paths']:
        restore(path,(ROOT/path).read_text(encoding='utf-8-sig'));print('PASS exact memory-run inverse '+path)

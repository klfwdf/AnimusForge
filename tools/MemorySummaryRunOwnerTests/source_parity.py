from pathlib import Path
import subprocess,json,hashlib
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent

def _restore_j02_guard_path(path,source,require_new=False):
    if path in {
        'tools/MemorySummaryMainThreadBoundaryTests/run.py',
        'tools/MemorySummaryMainThreadBoundaryTests/run_business.py',
        'tools/MemorySummaryMainThreadBoundaryTests/run_captured.py',
        'tools/MemorySummaryMainThreadBoundaryTests/run_planning.py',
        'tools/MemorySummaryMainThreadBoundaryTests/run_sealing.py',
        'tools/MemorySummaryMainThreadBoundaryTests/run_terminal.py',
        'tools/MemorySummaryMainThreadBoundaryTests/run_writers.py',
    }:
        new='src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs'
        if require_new:
            assert source.count(new)==1,'Memory-run live runner guard path drift: '+path
        if new in source:
            assert source.count(new)==1,'Memory-run runner guard path drift: '+path
            return source.replace(new,'SaveRuntimeGuard.cs',1)
    return source

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
    live=_restore_j02_guard_path(path,(ROOT/path).read_text(encoding='utf-8-sig'),require_new=True)
    assert live==expected,'Unreviewed live memory-run source: '+path
    source=_restore_j02_guard_path(path,source)
    assert source in (expected,old),'Unreviewed memory-run source changes: '+path
    return old
if __name__=='__main__':
    for path in json.loads((HERE/'source-review.json').read_text(encoding='utf-8'))['paths']:
        restore(path,(ROOT/path).read_text(encoding='utf-8-sig'));print('PASS exact memory-run inverse '+path)

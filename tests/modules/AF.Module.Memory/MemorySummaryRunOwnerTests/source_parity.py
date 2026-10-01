from pathlib import Path
import subprocess,json,hashlib
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent

def _restore_j02_guard_path(path,source,require_new=False):
    if path in {
        'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run.py',
        'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_business.py',
        'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_captured.py',
        'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_planning.py',
        'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_sealing.py',
        'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_terminal.py',
        'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_writers.py',
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
        dependency=(ROOT/file).read_text(encoding='utf-8-sig')
        if file.endswith('/run.py'):
            for delta in reversed(review['runnerSafetyChanges']):
                assert dependency.count(delta['after'])==1,'Unreviewed memory-run runner safety'
                dependency=dependency.replace(delta['after'],delta['before'],1)
        if file.endswith(('/fixture_support.py','/run.py')):
            # J16a moved these two runners exactly two directory levels deeper.
            assert dependency.count('ROOT=Path(__file__).resolve().parents[4]')==1
            dependency=dependency.replace('ROOT=Path(__file__).resolve().parents[4]', 'ROOT=Path(__file__).resolve().parents[2]',1)
            dependency=dependency.replace('src/modules/AF.Module.Memory/Summary/MemorySummaryRunOwner.cs','Refactor/Runtime/MemorySummaryRunOwner.cs')
            dependency=dependency.replace('tests/AF.Contracts/ModuleFrameworkApiTests/run.py','tools/ModuleFrameworkApiTests/run.py')
        assert hashlib.sha256(dependency.encode()).hexdigest()==h,'Unreviewed memory-run dependency: '+file
    old=subprocess.check_output(['git','show',review['baseline']+':'+('tools/'+path.split('/')[-2]+'/'+path.split('/')[-1] if path.startswith('tests/modules/') else path)],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n');lines=old.splitlines(keepends=True)
    for delta in reversed(review['paths'][path]):
        a,b=delta['start'],delta['end'];assert ''.join(lines[a:b])==delta['before'];lines[a:b]=[delta['after']]
    expected=''.join(lines)
    live=_restore_j02_guard_path(path,(ROOT/path).read_text(encoding='utf-8-sig'),require_new=True)
    assert live==expected,'Unreviewed live memory-run source: '+path
    source=_restore_j02_guard_path(path,source)
    assert source in (expected,old),'Unreviewed memory-run source changes: '+path
    return old
def verify_current():
    """J17 consumer scope; old whole-owner inverse remains a historical proof."""
    import importlib.util
    spec=importlib.util.spec_from_file_location('run_scope_extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
    ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
    review=json.loads((HERE/'source-review.json').read_text(encoding='utf-8'))
    for file,h in review['dependencies'].items():
        if file.startswith('tests/'):continue
        assert hashlib.sha256((ROOT/file).read_text(encoding='utf-8-sig').encode()).hexdigest()==h,'Unreviewed memory-run dependency: '+file
    live=(ROOT/'MyBehavior.cs').read_text(encoding='utf-8-sig')
    accepted=subprocess.check_output(['git','show','f6e2ead7:MyBehavior.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
    signature='private async Task ProcessMemorySummaryQueueAsync('
    assert ex.declaration(live,signature)==ex.declaration(accepted,signature),'Unreviewed memory-run consumer'
    assert 'run.Dispose();' in ex.declaration(live,signature)
    assert '_memorySummaryProcessing' not in live,'Retired memory run flag returned'
    print('PASS scoped current run owner/adapter and actual Process consumer; historical inverse retained')

if __name__=='__main__':verify_current()

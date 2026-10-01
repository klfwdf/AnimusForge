"""Require real compiled assertion rejection, not a source or compiler failure."""
import argparse, subprocess, sys
from pathlib import Path
ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT/'tests'))
from output_isolation import new_run_root
p = argparse.ArgumentParser(); p.add_argument('--run-root', required=True, type=Path); args = p.parse_args()
out = new_run_root(ROOT, 'native-effects-mutations', args.run_root)
for name, assertion in [('exit-once','accepted-once-and-lease-release'), ('history-acceptance','expected-dispatch-failure'), ('late-exit','late-exit-context-revalidated'), ('capture-identity','exact-original-order')]:
    child = out/name
    result = subprocess.run([sys.executable,'-B',str(HERE/'run.py'),'--mutation',name,'--run-root',str(child)], cwd=ROOT, capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=120)
    (out/(name+'.log')).write_text(result.stdout+result.stderr,encoding='utf-8')
    assert (child/'run.log').is_file(), 'mutation did not generate/compile '+name
    log = (child/'run.log').read_text(encoding='utf-8')
    assert result.returncode != 0 and 'error CS' not in log and 'ASSERTION FAILURE: System.Exception: ASSERT '+assertion in log, 'mutation survived or failed before assertion '+name
    print('PASS compiled mutation rejected '+name+' '+assertion)

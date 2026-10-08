"""Require real compiled assertion rejection, not a source or compiler failure."""
import argparse, subprocess, sys
from pathlib import Path
ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT/'tests'))
from output_isolation import new_run_root
p = argparse.ArgumentParser(); p.add_argument('--run-root', required=True, type=Path); args = p.parse_args()
out = new_run_root(ROOT, 'game-thread-dispatch-mutations', args.run_root)
for name, assertion in [('claim','claim-once-lease-release'),('started-deadline','started-real-result'),('format','format-error-once')]:
    for api in ['1.3', '1.4']:
        child = out/(name+'-'+api)
        result = subprocess.run([sys.executable,'-B',str(HERE/'run.py'),'--mutation',name,'--api',api,'--run-root',str(child)], cwd=ROOT, capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=120)
        (out/(name+'-'+api+'.log')).write_text(result.stdout+result.stderr,encoding='utf-8')
        assert (child/('run-'+api+'.log')).is_file(), 'mutation did not generate/compile '+name+' '+api
        log = (child/('run-'+api+'.log')).read_text(encoding='utf-8')
        assert result.returncode != 0 and 'error CS' not in log and 'ASSERTION FAILURE: System.Exception: ASSERT '+assertion in log, 'mutation survived or failed before assertion '+name+' '+api
        print('PASS compiled mutation rejected '+name+' '+assertion+' '+api)

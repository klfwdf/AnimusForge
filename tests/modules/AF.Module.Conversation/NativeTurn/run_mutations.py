"""A mutation passes rejection only after compilation and its intended assertion."""
from pathlib import Path
import os, subprocess, sys
HERE=Path(__file__).resolve().parent
cases={
 'skip-stage-stop':'ASSERT sequencer-stop-0',
 'duplicate-commit':'ASSERT sequencer-order--1',
 'skip-capture-guard':'ASSERT stale-capture-rejected',
 'capture-on-worker':'ASSERT capture-on-game-thread',
 'normalize-on-worker':'ASSERT normalize-on-game-thread',
 'swallow-capture-failure':'ASSERT capture-exception-not-fallback',
}
env=dict(os.environ,PYTHONUTF8='1',PYTHONDONTWRITEBYTECODE='1')
for name,expected in cases.items():
    p=subprocess.run([sys.executable,'-B',str(HERE/'run.py'),'--mutate',name],env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=100)
    log=p.stdout+p.stderr
    assert p.returncode!=0 and expected in log and 'error CS' not in log,(name,log)
    print('PASS compiled rejection '+name+' -> '+expected)

"""A mutation passes rejection only after compilation and its intended assertion."""
from pathlib import Path
import os, subprocess, sys, argparse
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[3]
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
parser=argparse.ArgumentParser();parser.add_argument('--run-root',type=Path);args=parser.parse_args()
out=new_run_root(ROOT,'native-turn-mutations',args.run_root)
cases={
 'skip-stage-stop':'ASSERT sequencer-stop-0',
 'duplicate-commit':'ASSERT sequencer-order--1',
 'skip-capture-guard':'ASSERT stale-capture-rejected',
 'capture-on-worker':'ASSERT capture-on-game-thread',
 'normalize-on-worker':'ASSERT normalize-on-game-thread',
 'swallow-capture-failure':'ASSERT capture-exception-not-fallback',
}
env=minimal_test_environment(resolve_dotnet(ROOT),out)
env.update(AF_DOTNET=str(resolve_dotnet(ROOT)),PYTHONUTF8='1',PYTHONDONTWRITEBYTECODE='1')
for name,expected in cases.items():
    p=subprocess.run([sys.executable,'-B',str(HERE/'run.py'),'--mutate',name,'--run-root',str(out/name)],env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=100)
    log=p.stdout+p.stderr
    (out/(name+'.log')).write_text(log,encoding='utf-8')
    assert p.returncode!=0 and expected in log and 'error CS' not in log,(name,log)
    print('PASS compiled rejection '+name+' -> '+expected)

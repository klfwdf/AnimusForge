"""Mutation runs must compile, execute a named scenario and fail its expected assertion."""
from pathlib import Path
import json,subprocess,sys
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[3]
import argparse
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root
parser=argparse.ArgumentParser();parser.add_argument('--run-root',type=Path);args=parser.parse_args()
out=new_run_root(ROOT,'NativeMainReply-mutations',args.run_root)
expected={
 'skip-generation':'terminal-parity:load',
 'skip-target':'terminal-parity:target',
 'skip-rollback':'stage-order-parity:target',
 'skip-normalization':'terminal-parity:unicode-json',
 'skip-provider-error':'stage-order-parity:error',
 'wrong-pending-key':'stage-order-parity:target',
 'wrong-target-port':'terminal-parity:target',
 'wrong-failure-text':'effect-parity:error',
 'empty-before-validation':'stage-order-parity:empty',
 'skip-consumer-stop':'stage-order-parity:empty'}
results=[]
for mutation,assertion in expected.items():
 r=subprocess.run([sys.executable,'-B',str(HERE/'run.py'),'--mutate',mutation,'--run-root',str(out/mutation)],cwd=ROOT,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180)
 log=r.stdout+r.stderr
 assert r.returncode!=0 and 'FAIL '+assertion in log and 'error CS' not in log and 'Build FAILED' not in log,(mutation,log)
 results.append({'mutation':mutation,'exitCode':r.returncode,'expectedAssertion':assertion})
 print('PASS behavioral mutation rejected: '+mutation+' / '+assertion,flush=True)
(out/'mutations.json').write_text(json.dumps(results,indent=2)+'\n')

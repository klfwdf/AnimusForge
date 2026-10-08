"""Every mutant must compile and fail its intended named runtime assertion."""
from pathlib import Path
import argparse,json,subprocess,sys
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[3]
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root
p=argparse.ArgumentParser();p.add_argument('--run-root',type=Path);args=p.parse_args()
out=new_run_root(ROOT,'native-dispatch-claim-mutations',args.run_root)
expected={
    'start-without-claim':'duplicate_callback_cannot_start',
    'expire-started':'started_operation_cannot_expire',
    'expire-without-claim':'duplicate_expiry_cannot_settle_again',
    'allow-expired-start':'expired_callback_cannot_run_late'}

results=[]
for mutation,assertion in expected.items():
 r=subprocess.run([sys.executable,'-B',str(HERE/'run.py'),'--mutate',mutation,'--run-root',str(out/mutation)],cwd=ROOT,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180)
 log=r.stdout+r.stderr
 assert r.returncode!=0 and 'FAIL '+assertion in log and 'error CS' not in log and 'Build FAILED' not in log,(mutation,log)
 results.append({'mutation':mutation,'exitCode':r.returncode,'expectedAssertion':assertion})
 print('PASS runtime mutation rejected: '+mutation+' / '+assertion,flush=True)
(out/'mutations.json').write_text(json.dumps(results,indent=2)+'\n')

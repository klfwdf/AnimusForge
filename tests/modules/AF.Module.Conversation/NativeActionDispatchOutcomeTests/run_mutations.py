import subprocess,sys
from pathlib import Path
here=Path(__file__).parent;root=here.resolve().parents[3]
import argparse
sys.path.insert(0,str(root/'tests'))
from output_isolation import new_run_root
parser=argparse.ArgumentParser();parser.add_argument('--run-root',type=Path);args=parser.parse_args()
out=new_run_root(root,'NativeActionDispatchOutcomeTests-mutations',args.run_root)
for mutation in ['lose-start-boundary','return-null','swallow-owner-failure','allow-diagnostic-failure','drop-queue-claim','keep-failed-queue-live','skip-dispatch-timeout','leave-expired-callback-live','expire-started-dispatch']:
 r=subprocess.run([sys.executable,'-B',str(here/'run.py'),'--mutate',mutation,'--run-root',str(out/mutation)],cwd=root,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180)
 if r.returncode==0 or 'FAIL ' not in r.stdout+r.stderr or 'error CS' in r.stdout+r.stderr:
  print(r.stdout+r.stderr);raise SystemExit('Mutation not behaviorally rejected: '+mutation)
 print('PASS dispatch mutation rejected: '+mutation,flush=True)

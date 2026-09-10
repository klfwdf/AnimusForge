import subprocess,sys
from pathlib import Path
root=Path(__file__).resolve().parents[2];here=Path(__file__).parent
for mutation in ['drop-busy','release-new-slot','skip-timeout-cas','skip-queued-action-guard','skip-generation','old-overlay-finalizer','skip-queued-epoch']:
 r=subprocess.run([sys.executable,'-B',str(here/'run.py'),'--mutate',mutation],cwd=root,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180)
 if r.returncode==0 or 'FAIL ' not in r.stdout+r.stderr or 'error CS' in r.stdout+r.stderr:
  print(r.stdout+r.stderr);raise SystemExit('Mutation not rejected by behavior: '+mutation)
 print('PASS behavioral mutation rejected: '+mutation,flush=True)

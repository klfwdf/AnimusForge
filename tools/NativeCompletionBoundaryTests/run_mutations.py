import subprocess,sys
from pathlib import Path
here=Path(__file__).parent;root=here.resolve().parents[1]
for mutation in ['drop-generation','current-scene','late-nonhero','drop-context','drop-close-guard','backend-close','drop-completion','capture-after-start','drop-discard-context']:
 r=subprocess.run([sys.executable,'-B',str(here/'run.py'),'--mutate',mutation],cwd=root,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180)
 if r.returncode==0 or 'FAIL ' not in r.stdout+r.stderr or 'error CS' in r.stdout+r.stderr:
  print(r.stdout+r.stderr);raise SystemExit('Mutation not behaviorally rejected: '+mutation)
 print('PASS completion mutation rejected: '+mutation,flush=True)

import subprocess,sys
from pathlib import Path
here=Path(__file__).parent;root=here.resolve().parents[1]
for mutation in ['drop-callback-guard','drop-revision','use-backend-slot','drop-stamp-retirement','allow-stale-finish','allow-stale-notice']:
 r=subprocess.run([sys.executable,'-B',str(here/'run_presentation.py'),'--mutate',mutation],cwd=root,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180)
 if r.returncode==0 or 'FAIL ' not in r.stdout+r.stderr or 'error CS' in r.stdout+r.stderr:
  print(r.stdout+r.stderr);raise SystemExit('Presentation mutation not behaviorally rejected: '+mutation)
 print('PASS presentation behavioral mutation rejected: '+mutation,flush=True)

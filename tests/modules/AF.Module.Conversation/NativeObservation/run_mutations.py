"""Compiled mutations must fail named real phase scenarios, not compilation or path lookup."""
from pathlib import Path
import subprocess,sys,json
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[3]
expected={'move-back-to-worker':'healthy_observation_on_owner_thread_once','duplicate-observation':'healthy_observation_on_owner_thread_once','skip-target':'stale_target_never_observed','tts-back-to-worker':'tableau_early_tts_on_owner_thread_once'}
results=[]
for mutation,signal in expected.items():
 r=subprocess.run([sys.executable,'-B',str(HERE/'run.py'),'--mutate',mutation],cwd=ROOT,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180);log=r.stdout+r.stderr
 assert r.returncode!=0 and 'FAIL '+signal in log and 'error CS' not in log and 'Build FAILED' not in log,(mutation,log)
 results.append({'mutation':mutation,'exitCode':r.returncode,'assertion':signal});print('PASS behavioral rejection '+mutation+' / '+signal,flush=True)
(HERE/'.generated/mutations.json').write_text(json.dumps(results,indent=2)+'\n')

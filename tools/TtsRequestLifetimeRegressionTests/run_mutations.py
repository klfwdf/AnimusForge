from pathlib import Path
import argparse,subprocess,sys
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
p=argparse.ArgumentParser();p.add_argument('--consumer-source',type=Path,default=ROOT/'ShoutBehavior.cs');a=p.parse_args()
mutations=['accept-after-publication','accept-under-lock','revive-dequeued','network-token-none','duplicate-terminal','late-legacy-event','match-agent-only','ignore-scene-epoch']
failed=0
for m in mutations:
 r=subprocess.run([sys.executable,'-B',str(HERE/'run.py'),'--consumer-source',str(a.consumer_source),'--mutation',m,'--output-name','mutant-'+m],cwd=ROOT,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=170)
 detected=r.returncode!=0 and 'FAIL ' in r.stdout and 'RESULT ' in r.stdout
 print(('PASS mutation rejected: ' if detected else 'FAIL mutation survived or harness failed: ')+m)
 if not detected:print(r.stdout+r.stderr);failed+=1
print(f'RESULT {len(mutations)-failed} rejected / {failed} invalid');raise SystemExit(bool(failed))

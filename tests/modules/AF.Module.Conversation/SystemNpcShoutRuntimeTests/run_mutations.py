"""Every mutation must compile and hit the intended current-owner assertion."""
import argparse,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root
p=argparse.ArgumentParser();p.add_argument('--run-root',type=Path,required=True);a=p.parse_args()
out=new_run_root(ROOT,'system-npc-shout-mutations',a.run_root)
cases={'force-queue':'must preserve deferred queue','registry-claim':'duplicate must not repeat effect/history/TTS','history-order':'original body output order'}
cases.update({name:'stale target cannot have effects' for name in ['mission','generation','epoch','session','owner','agent-reference']})
for mutation,assertion in cases.items():
 child=out/mutation
 result=subprocess.run([sys.executable,'-B',str(HERE/'run.py'),'--mutation',mutation,'--run-root',str(child)],cwd=ROOT,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=100)
 log=result.stdout+result.stderr;(out/(mutation+'.log')).write_text(log,encoding='utf-8')
 assert (child/'run.log').is_file(),'mutation never compiled '+mutation
 assert result.returncode!=0 and 'error CS' not in log and 'System.Exception: ASSERT '+assertion in log,'mutation did not hit its actual assertion '+mutation+'\n'+log
 print('PASS compiled current-owner assertion rejection '+mutation)

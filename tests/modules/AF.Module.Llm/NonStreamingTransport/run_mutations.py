from pathlib import Path
import os,subprocess,sys
HERE=Path(__file__).resolve().parent
expected={'leak-response':'ASSERT primary-response-disposed-success','skip-accept':'ASSERT primary-text-stale-header','drop-caller-token':'ASSERT caller-cancellation-reaches-send','thinking-still-enabled':'ASSERT primary-payload-thinking','lose-retry-after':'ASSERT retry-after-preserved'}
for name,signal in expected.items():
 p=subprocess.run([sys.executable,'-B',str(HERE/'run.py'),'--mutate',name],env=dict(os.environ,PYTHONUTF8='1'),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=100)
 log=p.stdout+p.stderr
 assert p.returncode!=0 and signal in log and 'error CS' not in log,(name,log)
 print('PASS compiled mutation '+name+' -> '+signal,flush=True)

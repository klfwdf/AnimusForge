from pathlib import Path
import os,subprocess,sys
HERE=Path(__file__).resolve().parent
cases={'leak-response':'ASSERT success-response-disposed','skip-line-accept':'ASSERT line-rejection-before-delta','duplicate-content':'ASSERT unicode-and-reasoning','ignore-cancel':'ASSERT caller-cancellation-and-dispose','unbounded-raw':'ASSERT raw-bound-does-not-truncate-content'}
for name,signal in cases.items():
 p=subprocess.run([sys.executable,'-B',str(HERE/'run.py'),'--mutate',name],env=dict(os.environ,PYTHONUTF8='1'),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=100);log=p.stdout+p.stderr
 assert p.returncode!=0 and signal in log and 'error CS' not in log,(name,log)
 print('PASS compiled mutation '+name+' -> '+signal)

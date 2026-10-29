"""Invoke the candidate display helper against each actual Bannerlord CampaignTime implementation."""
from pathlib import Path
import argparse,subprocess,sys
ROOT=Path(__file__).resolve().parents[4]
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet
p=argparse.ArgumentParser();p.add_argument('--run-root',type=Path);a=p.parse_args()
out=new_run_root(ROOT,'civil-war-native-calendar',a.run_root)
dotnet=resolve_dotnet(ROOT);sdk=next((dotnet.parent/'sdk').glob('*/Roslyn/bincore/csc.dll'))
f=Path('C:/Windows/Microsoft.NET/Framework64/v4.0.30319')
exe=out/'Probe.exe'
subprocess.run([str(dotnet),str(sdk),'/nologo','/noconfig','/nostdlib+','/platform:x64','/out:'+str(exe),str(Path(__file__).with_name('Program.cs'))]+['/reference:'+str(f/(n+'.dll')) for n in ('mscorlib','System','System.Core')],check=True)
for api in ('1.3','1.4'):
 refs=ROOT/'.tmp/build_check'/api
 candidate=ROOT/'bin/Release/single_module_artifacts/versions'/api/'AnimusForge.dll'
 r=subprocess.run([str(exe),str(refs),str(candidate)],capture_output=True)
 (out/('run-'+api+'.log')).write_bytes(r.stdout+r.stderr)
 print(api,(r.stdout+r.stderr).decode('utf8',errors='replace'))
 if r.returncode:raise SystemExit(r.returncode)

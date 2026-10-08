"""Actual shared dispatcher and sole PendingOperationRegistry; deterministic game-thread post."""
import argparse,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
p=argparse.ArgumentParser();p.add_argument('--run-root',required=True,type=Path);p.add_argument('--mutation',choices=['claim','started-deadline','format']);p.add_argument('--api',choices=['1.3','1.4'],help='Run one existing API variant; default runs both.');a=p.parse_args()
out=new_run_root(ROOT,'game-thread-dispatch',a.run_root)
runtime=(ROOT/'src/modules/AF.Module.Conversation/Channels/Native/ConversationGameThreadDispatcher.cs').read_text(encoding='utf-8-sig')
mutations={
'claim': ('if (Interlocked.CompareExchange(ref state, 1, 0) != 0) return;', 'if (false) return;'),
'started-deadline': ('if (Interlocked.CompareExchange(ref state, 2, 0) != 0) return false;', 'if (Interlocked.Exchange(ref state, 2) == 2) return false;'),
'format': ('catch (PreprocessFormatException ex)', 'catch (NotSupportedException ex)')}
if a.mutation:
    old,new=mutations[a.mutation];assert runtime.count(old)==1;runtime=runtime.replace(old,new,1)
(out/'Runtime.cs').write_text(runtime,encoding='utf-8')
(out/'Registry.cs').write_bytes((ROOT/'src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs').read_bytes())
(out/'Drain.cs').write_bytes((ROOT/'src/modules/AF.Module.Conversation/Channels/Native/ConversationMainThreadActionDrain.cs').read_bytes())
(out/'Program.cs').write_bytes((HERE/'Harness.cs.txt').read_bytes())
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup></Project>')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet=resolve_dotnet(ROOT)
for api,defines in [('1.3',''),('1.4','BANNERLORD_1_4_OR_GREATER')]:
    if a.api and a.api != api: continue
    r=subprocess.run([str(dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release','-p:DefineConstants='+defines],cwd=ROOT,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90)
    log=r.stdout+r.stderr;(out/('run-'+api+'.log')).write_text(log,encoding='utf-8');print(api,log)
    if r.returncode: raise SystemExit(r.returncode)

"""Actual Scene synchronization context plus the one production pending-operation registry."""
import argparse,importlib.util,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
p=argparse.ArgumentParser();p.add_argument('--run-root',required=True,type=Path);p.add_argument('--mutation',choices=['lease','claim','retired-guard']);a=p.parse_args()
out=new_run_root(ROOT,'scene-continuation-lifecycle',a.run_root)
s=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ModuleSceneSubmission.cs').read_text(encoding='utf-8-sig')
context=ex.declaration(s,'private sealed class SceneMainThreadSynchronizationContext')
if a.mutation:
    pairs={'lease':('registration = _pending.Register(_pending.Version, () => Resume(true));','registration = new EmptyLease();'),'claim':('if (Interlocked.CompareExchange(ref claimed, 1, 0) != 0) return;','if (false) return;'),'retired-guard':('if (retired) Interlocked.Exchange(ref _retired, 1);','if (false) Interlocked.Exchange(ref _retired, 1);')}
    old,new=pairs[a.mutation];assert context.count(old)==1;context=context.replace(old,new,1)
(out/'Runtime.cs').write_text('using System;using System.Threading;using AnimusForge.Refactor.Runtime;namespace AnimusForge;internal sealed partial class SceneConversationSessionRuntime {\n'+context+'\n}',encoding='utf-8')
(out/'Registry.cs').write_bytes((ROOT/'src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs').read_bytes())
(out/'Program.cs').write_bytes((HERE/'Harness.cs.txt').read_bytes())
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup></Project>')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet=resolve_dotnet(ROOT);r=subprocess.run([str(dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90)
log=r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)

from pathlib import Path
import sys,argparse,importlib.util,subprocess,json,hashlib,re
R=Path(__file__).resolve().parents[4];H=Path(__file__).parent
sys.path.insert(0,str(R/'tests'))
from output_isolation import minimal_test_environment,new_run_root
s=importlib.util.spec_from_file_location('ex',R/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(s);s.loader.exec_module(ex)
p=argparse.ArgumentParser();p.add_argument('--run-root',type=Path,required=True);p.add_argument('--mutate',choices=['ignore_source','remove_spacing']);a=p.parse_args();out=new_run_root(R,'MemoryQueueRun',a.run_root)
main=(R/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs').read_text(encoding='utf-8-sig')
forward=re.search(r'private Task ProcessMemorySummaryQueueAsync\([^\n]*;',[main][0]).group(0)
assert '=> CreateMemorySummaryQueueRunRuntime().RunAsync(forceOverviewCandidateScan);' in forward
batch=re.search(r'private Task RunDailySummaryQueueItemsAsync\([^\n]*;',main).group(0)
assert '=> CreateMemorySummaryQueueRunRuntime().RunQueueItemsAsync(' in batch
factory=(R/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemorySummaryQueueRun.cs').read_text(encoding='utf-8-sig')
factory=factory[factory.index('{')+1:factory.rfind('}')]
assert factory.count('milliseconds => Task.Delay(milliseconds)')==1
factory=factory.replace('milliseconds => Task.Delay(milliseconds)','milliseconds => DelayAsync(milliseconds)')
plan=ex.declaration((R/'src/modules/AF.Module.Memory/Summary/MemorySummaryPlanningOwner.cs').read_text(encoding='utf-8-sig'),'internal sealed class MemorySummaryPlan\n')
golden=(H/'LegacyQueueRun.cs.txt').read_text(encoding='utf-8').replace('Task.Delay(60000)','DelayAsync(60000)')
program=(H/'QueueRunHarness.cs.txt').read_text(encoding='utf-8').replace('@@FACTORY@@',factory).replace('@@FORWARD@@',forward).replace('@@PLAN@@',plan).replace('@@LEGACY@@',golden)
(out/'Program.cs').write_text(program,encoding='utf-8')
paths=['src/modules/AF.Module.Memory/Summary/MemorySummaryQueueRunRuntime.cs','src/modules/AF.Module.Memory/Summary/MemorySummaryRunOwner.cs','src/modules/AF.Module.Memory/Summary/MemorySummaryAttemptRunner.cs','src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs']
compile_paths=[R/x for x in paths]
if a.mutate:
    source=compile_paths[0].read_text(encoding='utf-8-sig')
    before,after={'ignore_source':('() => IsSourceCurrent(result)','() => true'),'remove_spacing':('_ports.DelayAsync(60000)','_ports.DelayAsync(0)')}[a.mutate]
    assert source.count(before)==(1 if a.mutate=='ignore_source' else 2)
    (out/'Runtime.cs').write_text(source.replace(before,after),encoding='utf-8');compile_paths=compile_paths[1:]
(out/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+str(x)+'" />' for x in compile_paths)+'</ItemGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
d=R/'local/dotnet/8.0.425/dotnet.exe';r=subprocess.run([str(d),'run','--project',str(out/'Tests.csproj'),'-c','Release'],cwd=out,env=minimal_test_environment(d,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
(out/'run.log').write_text(r.stdout+r.stderr,encoding='utf-8');print(r.stdout+r.stderr,end='')
(out/'receipt.json').write_text(json.dumps({'exit_code':r.returncode,'sources':{x:hashlib.sha256((R/x).read_bytes()).hexdigest() for x in paths},'golden_sha256':hashlib.sha256((H/'LegacyQueueRun.cs.txt').read_bytes()).hexdigest(),'main_consumers':'2 actual facade methods; complete actual factory extracted; only60s scheduler leaf injected','live':'NOT_RUN'},indent=2),encoding='utf-8');sys.exit(r.returncode)

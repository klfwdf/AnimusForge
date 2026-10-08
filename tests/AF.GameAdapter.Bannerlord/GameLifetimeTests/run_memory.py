from pathlib import Path
import sys as _relocation_sys
_relocation_sys.path.insert(0, str(Path(__file__).resolve().parents[3] / "tests"))
from output_isolation import current_source_path, new_run_root, resolve_dotnet, minimal_test_environment
import argparse,importlib.util,os,subprocess
ROOT=Path(__file__).resolve().parents[3];HERE=Path(__file__).parent
spec=importlib.util.spec_from_file_location('util',ROOT/'tests/AF.Contracts/ModuleFrameworkApiTests/run.py');util=importlib.util.module_from_spec(spec);spec.loader.exec_module(util)
p=argparse.ArgumentParser();p.add_argument('--mutate',action='store_true');p.add_argument('--run-root',type=Path);a=p.parse_args()
run_root=new_run_root(ROOT,'game-lifetime-memory',a.run_root)
out=new_run_root(ROOT,'game-lifetime-memory',run_root/('memory-missing-retirement' if a.mutate else 'memory-current'))
source=(current_source_path(ROOT, 'MyBehavior.MemorySummaryMainThread.cs')).read_text(encoding='utf-8-sig')
if a.mutate:source=source.replace('Volatile.Read(ref _owner._campaignRuntimeRetired) == 0','true')
(out/'Boundary.cs').write_text(source,encoding='utf-8');(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
project=util.project(out,'MemoryRetirement',[out/'Boundary.cs',HERE/'MemoryRetirement.cs.txt',current_source_path(ROOT, 'MyBehavior.CampaignLifetime.cs'),ROOT/'src/modules/AF.Module.Memory/Summary/MemorySummaryDispatcher.cs',ROOT/'src/modules/AF.Module.Memory/Summary/IMemorySummaryDispatchHost.cs'],executable=True)
dotnet=resolve_dotnet(ROOT)
result=subprocess.run([str(dotnet),'run','--project',str(project),'-c','Release'],cwd=out,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180)
status,log=result.returncode,result.stdout+result.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log,end='');raise SystemExit(status)

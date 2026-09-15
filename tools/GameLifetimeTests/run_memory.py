from pathlib import Path
import argparse,importlib.util,os
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent
spec=importlib.util.spec_from_file_location('util',ROOT/'tools/ModuleFrameworkApiTests/run.py');util=importlib.util.module_from_spec(spec);spec.loader.exec_module(util)
p=argparse.ArgumentParser();p.add_argument('--mutate',action='store_true');a=p.parse_args()
out=HERE/'.generated'/('memory-missing-retirement' if a.mutate else 'memory-current');out.mkdir(parents=True,exist_ok=True)
source=(ROOT/'MyBehavior.MemorySummaryMainThread.cs').read_text(encoding='utf-8-sig')
if a.mutate:source=source.replace('Volatile.Read(ref _owner._campaignRuntimeRetired) == 0','true')
(out/'Boundary.cs').write_text(source,encoding='utf-8');(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
project=util.project(out,'MemoryRetirement',[out/'Boundary.cs',HERE/'MemoryRetirement.cs.txt',ROOT/'MyBehavior.CampaignLifetime.cs',ROOT/'Refactor/Runtime/MemorySummaryDispatcher.cs',ROOT/'Refactor/Contracts/IMemorySummaryDispatchHost.cs'],executable=True)
status,log=util.run_dotnet(os.environ.get('DOTNET_EXE',r'G:\AFMOD\.dotnet-sdk\dotnet.exe'),['run','--project',str(project),'-c','Release'],out);(out/'run.log').write_text(log,encoding='utf-8');print(log,end='');raise SystemExit(status)

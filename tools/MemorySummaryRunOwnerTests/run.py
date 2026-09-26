from pathlib import Path
import importlib.util,os,argparse
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent
spec=importlib.util.spec_from_file_location('util',ROOT/'tools/ModuleFrameworkApiTests/run.py');util=importlib.util.module_from_spec(spec);spec.loader.exec_module(util)
p=argparse.ArgumentParser();p.add_argument('--mutate',choices=['release_replacement','ignore_current','allow_overlap']);a=p.parse_args()
source=(ROOT/'src/modules/AF.Module.Memory/Summary/MemorySummaryRunOwner.cs').read_text(encoding='utf-8-sig')
mutations={'release_replacement':('Interlocked.CompareExchange(ref _owner._current, null, this)','Interlocked.Exchange(ref _owner._current, null)'), 'ignore_current':('ReferenceEquals(Volatile.Read(ref _owner._current), this)','true'),'allow_overlap':('Interlocked.CompareExchange(ref _current, candidate, null) == null ? candidate : null','Interlocked.Exchange(ref _current, candidate) == null ? candidate : candidate')}
if a.mutate:
 before,after=mutations[a.mutate];assert source.count(before)==1;source=source.replace(before,after,1)
out=HERE/'.generated'/(a.mutate or 'current');out.mkdir(parents=True,exist_ok=True);(out/'Owner.cs').write_text(source,encoding='utf-8');(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
project=util.project(out,'MemoryRunChecks',[out/'Owner.cs',HERE/'OwnerChecks.cs.txt'],executable=True);status,log=util.run_dotnet(os.environ.get('DOTNET_EXE',r'G:\AFMOD\.dotnet-sdk\dotnet.exe'),['run','--project',str(project),'-c','Release'],out);(out/'run.log').write_text(log,encoding='utf-8');print(log,end='');raise SystemExit(status)

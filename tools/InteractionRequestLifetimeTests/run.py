from pathlib import Path
import argparse,importlib.util,subprocess
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent
spec=importlib.util.spec_from_file_location('build',ROOT/'tools/ModuleFrameworkApiTests/run.py');util=importlib.util.module_from_spec(spec);spec.loader.exec_module(util)
SOURCES=['Refactor/Contracts/InteractionContracts.cs','Refactor/Contracts/LlmContracts.cs','Refactor/Contracts/ProfileConfigContracts.cs','src/modules/AF.Module.Conversation/Internal/InteractionRequestCoordinator.cs']
MAIN='437925b856fae76b4e9ee207e96ba048f35d5a67'
p=argparse.ArgumentParser();p.add_argument('--main',action='store_true');p.add_argument('--case',default='all',choices=['all','common','supersede','dispose','token','race','precancel','reentrant','surface']);p.add_argument('--mutate',choices=['dispose_early','propagate_callback','ignore_active_cancel']);args=p.parse_args()
if args.main and args.mutate:p.error("--main and --mutate are mutually exclusive")
out=HERE/'.generated'/('main' if args.main else (args.mutate or 'current'));out.mkdir(parents=True,exist_ok=True)
(out/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
sources=[]
for path in SOURCES:
 if args.main:
  file=out/Path(path).name;file.write_bytes(subprocess.check_output(['git','show',MAIN+':'+('Refactor/Runtime/InteractionRequestCoordinator.cs' if path=='src/modules/AF.Module.Conversation/Internal/InteractionRequestCoordinator.cs' else path)],cwd=ROOT));sources.append(file)
 else:sources.append(ROOT/path)
lease=ROOT/'src/modules/AF.Module.Conversation/Internal/InteractionRequestLease.cs'
if not args.main and lease.exists():
 if args.mutate:
  faults={
   'dispose_early':('            source.Cancel();','            source.Cancel();\n            source.Dispose();'),
   'propagate_callback':('        catch (AggregateException error)\n        {','        catch (AggregateException error)\n        {\n            throw;'),
   'ignore_active_cancel':('if (!_completed || _activeCancellations != 0)', 'if (!_completed)')}
  before,after=faults[args.mutate];text=lease.read_text(encoding='utf-8-sig');assert text.count(before)==1
  file=out/lease.name;file.write_text(text.replace(before,after),encoding='utf-8');sources.append(file)
 else:sources.append(lease)
project=util.project(out,'InteractionRequestLifetimeChecks',sources+[HERE/'Program.cs'],executable=True)
code,log=util.run_dotnet(r'G:\AFMOD\.dotnet-sdk\dotnet.exe',['run','--project',str(project),'-c','Release','--',args.case],out)
print(log,end='');(out/(args.case+'.log')).write_text(log,encoding='utf-8')
raise SystemExit(code)

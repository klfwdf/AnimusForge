from pathlib import Path
import argparse,importlib.util,subprocess
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent
p=argparse.ArgumentParser();p.add_argument('--original',action='store_true');p.add_argument('--mutate',choices=['cancel_claimed','timeout_claimed','drop_claim']);a=p.parse_args()
spec=importlib.util.spec_from_file_location('ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
spec=importlib.util.spec_from_file_location('util',ROOT/'tools/ModuleFrameworkApiTests/run.py');util=importlib.util.module_from_spec(spec);spec.loader.exec_module(util)
path='CourierDeliveryBehavior.DetachedPostprocess.cs'
s=subprocess.check_output(['git','show','4140bd04:'+path],cwd=ROOT).decode('utf-8-sig') if a.original else (ROOT/path).read_text(encoding='utf-8-sig')
method=ex.declaration(s,'private async Task<T> RunCourierOwnerPhaseAsync<T>(').replace('Task.Delay(30000)','Task.Delay(80)')
if a.mutate=='cancel_claimed':method=method.replace('if (Interlocked.CompareExchange(ref state, 2, 0) == 0)\n                completion.TrySetCanceled();','Interlocked.CompareExchange(ref state, 2, 0);\n            completion.TrySetCanceled();',1)
if a.mutate=='timeout_claimed':method=method.replace('if (winner != completion.Task && Interlocked.CompareExchange(ref state, 2, 0) == 0)','if (winner != completion.Task)',1)
if a.mutate=='drop_claim':method=method.replace('if (Interlocked.CompareExchange(ref state, 1, 0) != 0) return;','',1)
code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig').replace('@@METHOD@@',method)
out=HERE/'.generated'/('original' if a.original else a.mutate or 'current');out.mkdir(parents=True,exist_ok=True);(out/'Program.cs').write_text('using TaleWorlds.Library;\n'+code,encoding='utf-8');(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
project=util.project(out,'OwnerPhase', [out/'Program.cs',ROOT/'Refactor/Runtime/PendingOperationRegistry.cs'],executable=True)
code,log=util.run_dotnet(r'G:\AFMOD\.dotnet-sdk\dotnet.exe',['run','--project',str(project),'-c','Release'],out);print(log,end='');(out/'run.log').write_text(log,encoding='utf-8');raise SystemExit(code)

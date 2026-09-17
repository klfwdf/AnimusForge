from pathlib import Path
import argparse,importlib.util,os,subprocess
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent
spec=importlib.util.spec_from_file_location('util',ROOT/'tools/ModuleFrameworkApiTests/run.py');util=importlib.util.module_from_spec(spec);spec.loader.exec_module(util)
spec=importlib.util.spec_from_file_location('ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
p=argparse.ArgumentParser();p.add_argument('--original',action='store_true');p.add_argument('--mutate',choices=['drop_claim','expire_claimed','skip_retirement']);a=p.parse_args()
out=HERE/'.generated'/('commit-original' if a.original else 'commit-'+(a.mutate or 'current'));out.mkdir(parents=True,exist_ok=True)
source=(ROOT/'CourierDeliveryBehavior.CommitDispatch.cs').read_text(encoding='utf-8-sig')
if a.original:
 old=subprocess.check_output(['git','show','807bc5b9:CourierDeliveryBehavior.cs'],cwd=ROOT).decode('utf-8-sig')
 for sig in ['private Task<InteractionCommitResult> DispatchCourierRefactorCommitAsync(', 'private InteractionCommitResult InvokeCourierRefactorCommit(', 'private static async Task<InteractionCommitResult> AwaitCourierRefactorCommitAsync(']:source=source.replace(ex.declaration(source,sig),ex.declaration(old,sig),1)
if a.mutate=='drop_claim':source=source.replace('Interlocked.CompareExchange(ref state, 1, 0) != 0','false',1)
if a.mutate=='expire_claimed':source=source.replace('Interlocked.CompareExchange(ref state, 2, 0) != 0','Interlocked.Exchange(ref state, 2) == 2',1)
if a.mutate=='skip_retirement':source=source.replace('() => Retire("courier_owner_retired")','() => { }',1)
source=source.replace('Task.Delay(30000','Task.Delay(120')
(out/'Dispatch.cs').write_text(source,encoding='utf-8');(out/'Program.cs').write_text((HERE/'CourierCommit.cs.txt').read_text(encoding='utf-8-sig'),encoding='utf-8')
contracts=(ROOT/'Refactor/Contracts/InteractionContracts.cs').read_text(encoding='utf-8-sig')
(out/'Enums.cs').write_text('namespace AnimusForge.Refactor.Contracts;\n'+'\n'.join(ex.declaration(contracts,s) for s in ['public enum InteractionStatus','public enum ActionExecutionEffectState']),encoding='utf-8')
(out/'Receipt.cs').write_text('using AnimusForge.Refactor.Contracts;\nnamespace AnimusForge.Refactor.Runtime;\n'+ex.declaration((ROOT/'Refactor/Runtime/InteractionResultCommitter.cs').read_text(encoding='utf-8-sig'),'public sealed class InteractionCommitResult'),encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
project=util.project(out,'CourierCommit',[out/f for f in ['Dispatch.cs','Program.cs','Enums.cs','Receipt.cs']]+[ROOT/'src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs'],executable=True)
status,log=util.run_dotnet(os.environ.get('DOTNET_EXE',r'G:\AFMOD\.dotnet-sdk\dotnet.exe'),['run','--project',str(project),'-c','Release'],out);(out/'run.log').write_text(log,encoding='utf-8');print(log,end='');raise SystemExit(status)

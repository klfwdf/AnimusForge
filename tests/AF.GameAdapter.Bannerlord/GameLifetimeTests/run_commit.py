from pathlib import Path
import argparse,importlib.util,os,subprocess,sys
ROOT=Path(__file__).resolve().parents[3];HERE=Path(__file__).parent
spec=importlib.util.spec_from_file_location('util',ROOT/'tests/AF.Contracts/ModuleFrameworkApiTests/run.py');util=importlib.util.module_from_spec(spec);spec.loader.exec_module(util)
spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root, resolve_dotnet
p=argparse.ArgumentParser();p.add_argument('--run-root',type=Path);p.add_argument('--original',action='store_true');p.add_argument('--mutate',choices=['drop_claim','expire_claimed','skip_retirement']);a=p.parse_args()
out=new_run_root(ROOT,'game-lifetime-commit',a.run_root)
source=(ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.CommitDispatch.cs').read_text(encoding='utf-8-sig')
if a.original:
 old=subprocess.check_output(['git','show','807bc5b9:CourierDeliveryBehavior.cs'],cwd=ROOT).decode('utf-8-sig')
 for sig in ['private Task<InteractionCommitResult> DispatchCourierRefactorCommitAsync(', 'private InteractionCommitResult InvokeCourierRefactorCommit(', 'private static async Task<InteractionCommitResult> AwaitCourierRefactorCommitAsync(']:source=source.replace(ex.declaration(source,sig),ex.declaration(old,sig),1)
signatures=['private Task<InteractionCommitResult> DispatchCourierRefactorCommitAsync(', 'private InteractionCommitResult InvokeCourierRefactorCommit(', 'private static async Task<InteractionCommitResult> AwaitCourierRefactorCommitAsync(']
if not a.original:signatures.insert(0,'private static InteractionCommitResult CreateUnconfirmedCourierCommit(')
source='using System;\nusing System.Threading;\nusing System.Threading.Tasks;\nusing AnimusForge.Refactor.Contracts;\nusing AnimusForge.Refactor.Runtime;\nusing TaleWorlds.Library;\nnamespace AnimusForge;\npublic partial class CourierDeliveryBehavior\n{\n'+'\n\n'.join(ex.declaration(source,sig) for sig in signatures)+'\n}\n'
if a.mutate=='drop_claim':source=source.replace('Interlocked.CompareExchange(ref state, 1, 0) != 0','false',1)
if a.mutate=='expire_claimed':source=source.replace('Interlocked.CompareExchange(ref state, 2, 0) != 0','Interlocked.Exchange(ref state, 2) == 2',1)
if a.mutate=='skip_retirement':source=source.replace('() => Retire("courier_owner_retired")','() => { }',1)
source=source.replace('Task.Delay(30000','Task.Delay(120')
(out/'Dispatch.cs').write_text(source,encoding='utf-8');(out/'Program.cs').write_text((HERE/'CourierCommit.cs.txt').read_text(encoding='utf-8-sig'),encoding='utf-8')
contracts=(ROOT/'src/AF.Contracts/Internal/InteractionContracts.cs').read_text(encoding='utf-8-sig')
(out/'Enums.cs').write_text('namespace AnimusForge.Refactor.Contracts;\n'+'\n'.join(ex.declaration(contracts,s) for s in ['public enum InteractionStatus','public enum ActionExecutionEffectState']),encoding='utf-8')
(out/'Receipt.cs').write_text('using AnimusForge.Refactor.Contracts;\nnamespace AnimusForge.Refactor.Runtime;\n'+ex.declaration((ROOT/'src/modules/AF.Module.Actions/Receipts/InteractionResultCommitter.cs').read_text(encoding='utf-8-sig'),'public sealed class InteractionCommitResult'),encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
project=util.project(out,'CourierCommit',[out/f for f in ['Dispatch.cs','Program.cs','Enums.cs','Receipt.cs']]+[ROOT/'src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs'],executable=True)
status,log=util.run_dotnet(str(resolve_dotnet(ROOT)),['run','--project',str(project),'-c','Release'],out);(out/'run.log').write_text(log,encoding='utf-8');print(log,end='');raise SystemExit(status)

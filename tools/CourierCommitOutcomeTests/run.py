from pathlib import Path
import argparse,importlib.util,os,subprocess,json
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent
spec=importlib.util.spec_from_file_location('util',ROOT/'tools/ModuleFrameworkApiTests/run.py');util=importlib.util.module_from_spec(spec);spec.loader.exec_module(util)
spec=importlib.util.spec_from_file_location('ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
p=argparse.ArgumentParser();p.add_argument('--original',action='store_true');p.add_argument('--mutate',choices=['false_no_effect','retryable_failure','lose_inbound_effect','unguarded_diagnostic']);a=p.parse_args()
name='original' if a.original else a.mutate or 'current';out=HERE/'.generated'/name;out.mkdir(parents=True,exist_ok=True)
source=subprocess.check_output(['git','show','29448d1b:CourierDeliveryBehavior.CommitDispatch.cs'],cwd=ROOT).decode('utf-8-sig') if a.original else (ROOT/'CourierDeliveryBehavior.CommitDispatch.cs').read_text(encoding='utf-8-sig')
if a.mutate=='false_no_effect':source=source.replace('errorCode, ActionExecutionEffectState.UnknownAfterStart','errorCode, ActionExecutionEffectState.NoConfirmedEffect',1)
if a.mutate=='retryable_failure':source=source.replace('new InteractionCommitResult(InteractionStatus.NonRetryableFailure, false, false,','new InteractionCommitResult(InteractionStatus.RejectedByValidation, false, false,',1)
if a.mutate=='lose_inbound_effect':source=source.replace('result.EffectState);','ActionExecutionEffectState.NoConfirmedEffect);',1)
if a.mutate=='unguarded_diagnostic':
 edits=json.loads((HERE/'source-review.json').read_text(encoding='utf-8'))['edits'];before,after=next(x for x in edits if 'Diagnostics do not own' in x[1]);source=source.replace(after,before,1)
source=source.replace('Task.Delay(30000','Task.Delay(120')
harness=(ROOT/'tools/GameLifetimeTests/CourierCommit.cs.txt').read_text(encoding='utf-8-sig')
harness=harness.replace('internal static bool FailLog;','internal static bool FailLog,InboundForTests;internal static int AbortsForTests;',1).replace('GetSessionById(string id)=>null;','GetSessionById(string id)=>InboundForTests?new CourierSession():null;',1).replace('IsInboundToPlayer(CourierSession s)=>false;','IsInboundToPlayer(CourierSession s)=>InboundForTests;',1).replace('AbortCourierInboundCompletion(CourierSession s,string reason){}','AbortCourierInboundCompletion(CourierSession s,string reason){AbortsForTests++;}',1).replace('DispatchCourierRefactorCommitAsync(f,"target","session")','DispatchCourierRefactorCommitAsync(f,"target","session",InboundForTests)',1)
needle='  Console.WriteLine($"CourierCommitLifetime';assert needle in harness;harness=harness.replace(needle,(HERE/'AdditionalChecks.cs.txt').read_text(encoding='utf-8-sig')+needle,1).replace('CourierCommitLifetime checks=','CourierCommitOutcome checks=')
(out/'Dispatch.cs').write_text(source,encoding='utf-8');(out/'Program.cs').write_text(harness,encoding='utf-8');(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
contracts=(ROOT/'Refactor/Contracts/InteractionContracts.cs').read_text(encoding='utf-8-sig');(out/'Enums.cs').write_text('namespace AnimusForge.Refactor.Contracts;\n'+'\n'.join(ex.declaration(contracts,x) for x in ['public enum InteractionStatus','public enum ActionExecutionEffectState']),encoding='utf-8')
(out/'Receipt.cs').write_text('using AnimusForge.Refactor.Contracts;\nnamespace AnimusForge.Refactor.Runtime;\n'+ex.declaration((ROOT/'Refactor/Runtime/InteractionResultCommitter.cs').read_text(encoding='utf-8-sig'),'public sealed class InteractionCommitResult'),encoding='utf-8')
project=util.project(out,'CourierCommitOutcome',[out/x for x in ['Dispatch.cs','Program.cs','Enums.cs','Receipt.cs']]+[ROOT/'Refactor/Runtime/PendingOperationRegistry.cs'],executable=True)
status,log=util.run_dotnet(os.environ.get('DOTNET_EXE',r'G:\AFMOD\.dotnet-sdk\dotnet.exe'),['run','--project',str(project),'-c','Release'],out);(out/'run.log').write_text(log,encoding='utf-8');print(log,end='');raise SystemExit(status)

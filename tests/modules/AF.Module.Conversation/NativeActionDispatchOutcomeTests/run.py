import argparse,importlib.util,subprocess,os,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
p=argparse.ArgumentParser();p.add_argument('--original',action='store_true');p.add_argument('--timeout-baseline',action='store_true');p.add_argument('--retirement-baseline',action='store_true');p.add_argument('--mutate',choices=['lose-start-boundary','return-null','swallow-owner-failure','allow-diagnostic-failure','drop-queue-claim','keep-failed-queue-live','skip-dispatch-timeout','leave-expired-callback-live','expire-started-dispatch']);p.add_argument('--run-root',type=Path);args=p.parse_args();assert not (args.original and args.timeout_baseline)
import sys
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root, minimal_test_environment
out=new_run_root(ROOT,'native-action-dispatch-outcome',args.run_root)
spec=importlib.util.spec_from_file_location('extractor',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
baseline='646dd987' if args.original else '8da4fbd7' if args.timeout_baseline else '807bc5b9' if args.retirement_baseline else None
import sys
sys.path.insert(0,str(ROOT/'tests'))
from af2_terminal_migration_review import historical_source
s=subprocess.check_output(['git','show',baseline+':ShoutBehavior.cs'],cwd=ROOT).decode('utf-8-sig') if baseline else historical_source('ShoutBehavior.cs')
assert 'private const int NativeConversationMainThreadPreprocessTimeoutMs = 30000;' in s
code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig').replace('@@RESULT@@',ex.declaration(s,'private sealed class NativeConversationGameActionResult')).replace('@@QUEUE@@',ex.declaration(s,'private Task<NativeConversationGameActionResult> ApplyNativeConversationGameActionsOnMainThreadAsync('))
# The unchanged boundary is source-projected from verified current phases; NativeTurn executes the new schedule.
import sys
sys.path.insert(0,str(ROOT/'tests/modules/AF.Module.Conversation/NativeConversationAdmissionTests'))
from turn_extraction import projected_source, NEW_SIGNATURE
if NEW_SIGNATURE in s: s=projected_source(s)
body=ex.declaration(s,'private async Task<string> SubmitNativeConversationTextInternalAsync(')
consumer_end='\t\tnativeActionSw.Stop();' if '\t\tnativeActionSw.Stop();' in body else '\t\tnativeTurnSw.Stop();'
consumer=body[body.index('\t\tif (nativeActionResult?.ResponseDiscarded == true)'):body.index(consumer_end)]
code=code.replace('@@CONSUMER_GATE@@',consumer)
if not args.original:
 overlay=(ROOT / 'src/AF.GameAdapter.Bannerlord/UI/Conversation/AnimusForgeNativeConversationOverlay.cs').read_text(encoding='utf-8-sig');reports=[]
 for signature,name in [('private async Task SubmitAsync(string text)','Normal'),('private async Task SubmitNpcInitiatedOpeningAsync(','Opening')]:
  method=ex.declaration(overlay,signature);handler=ex.declaration(method,'catch (ShoutBehavior.NativeConversationActionDispatchException ex)')
  assert 'suppressReadyNotice = true' in handler and 'RunNativePresentationCallback(generation,' in handler and 'PromptRetry' not in handler
  reports.append('private bool '+name+'(ShoutBehavior.NativeConversationActionDispatchException failure) { bool suppressReadyNotice=false;int generation=1;try { throw failure; } '+handler+' return suppressReadyNotice; }')
 code=code.replace('@@UI_FAILURE@@','\n'.join(reports)+'\ninternal bool Report(ShoutBehavior.NativeConversationActionDispatchException ex,bool opening)=>opening?Opening(ex):Normal(ex);')
else:code=code.replace('@@UI_FAILURE@@','')
if args.mutate=='drop-queue-claim':code=code.replace('if (!dispatchClaim.TryStart())', 'if (false)', 1)
if args.mutate=='keep-failed-queue-live':code=code.replace('if (dispatchClaim.TryExpireBeforeStart())\n\t\t\t\ttcs.TrySetException(new NativeConversationActionDispatchException(false, ex));', 'if (true)\n\t\t\t\ttcs.TrySetException(new NativeConversationActionDispatchException(false, ex));', 1)
if args.mutate=='skip-dispatch-timeout':code=code.replace('PendingOperationRegistry.AwaitRelease(AwaitDispatch(), registration)','PendingOperationRegistry.AwaitRelease(tcs.Task, registration)',1)
if args.mutate=='leave-expired-callback-live':code=code.replace('winner != tcs.Task && dispatchClaim.TryExpireBeforeStart()','winner != tcs.Task && new NativeConversationDispatchClaim().TryExpireBeforeStart()',1)
if args.mutate=='expire-started-dispatch':code=code.replace('winner != tcs.Task && dispatchClaim.TryExpireBeforeStart()','winner != tcs.Task && true',1)
# The per-run root is allocated before source extraction; never reuse generated output.
(out/'Program.cs').write_text(code,encoding='utf-8');enum=ex.declaration((ROOT/'src/AF.Contracts/Internal/InteractionContracts.cs').read_text(encoding='utf-8-sig'),'public enum ActionExecutionEffectState');(out/'Effect.cs').write_text('namespace AnimusForge.Refactor.Contracts;\n'+enum,encoding='utf-8')
if not args.original:
 boundary=subprocess.check_output(['git','show',baseline+':ShoutBehavior.NativeActionDispatch.cs'],cwd=ROOT).decode('utf-8-sig') if args.timeout_baseline else (ROOT / 'src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeActionDispatch.cs').read_text(encoding='utf-8-sig')
 if args.mutate=='lose-start-boundary':boundary=boundary.replace('ownerStarted = true;','ownerStarted = false;',1)
 if args.mutate=='return-null':boundary=boundary.replace('throw new InvalidOperationException("native.action_result_missing");','return null;',1)
 if args.mutate=='swallow-owner-failure':boundary=boundary.replace('throw new NativeConversationActionDispatchException(ownerStarted, ex);','return new NativeConversationGameActionResult { Content = "fallback" };',1)
 if args.mutate=='allow-diagnostic-failure':boundary=boundary.replace('catch (Exception)\n        {\n            // Observability must never change whether actions run or how their Task completes.\n            return;\n        }','catch (Exception) { throw; }',1)
 (out/'Boundary.cs').write_text(boundary,encoding='utf-8')
(out/'NativeConversationDispatchClaim.cs').write_text((ROOT/'src/modules/AF.Module.Conversation/Channels/Native/NativeConversationDispatchClaim.cs').read_text(encoding='utf-8-sig'),encoding='utf-8')
(out/'CompletionStubs.cs').write_text((ROOT/'tests/modules/AF.Module.Conversation/NativeCompletionBoundaryTests/NoCompletionStubs.cs.txt').read_text(encoding='utf-8-sig'),encoding='utf-8')
if not args.original and not args.timeout_baseline:
 result_decl='private sealed class NativeConversationGameActionResult'
 admission_decl='private class NativeConversationAdmission'
 assert code.count(result_decl)==1 and code.count(admission_decl)==1
 code=code.replace(result_decl,'internal sealed class NativeConversationGameActionResult',1).replace(admission_decl,'internal class NativeConversationAdmission',1)
 (out/'Program.cs').write_text(code,encoding='utf-8')
 (out/'DispatchBridge.cs').write_text((ROOT/'tests/modules/AF.Module.Conversation/NativeConversationAdmissionTests/DispatchBridge.cs.txt').read_text(encoding='utf-8-sig'),encoding='utf-8')
 completion=(out/'CompletionStubs.cs').read_text(encoding='utf-8-sig')
 exception_decl='NativeConversationHistoryCommitException : System.InvalidOperationException'
 assert completion.count(exception_decl)==1
 (out/'CompletionStubs.cs').write_text(completion.replace(exception_decl,'NativeConversationHistoryCommitException : NativeConversationGameEffectsRuntime.NativeConversationHistoryCommitException',1),encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion>'+('<DefineConstants>ORIGINAL</DefineConstants>' if args.original else '<DefineConstants>TIMEOUT_BASELINE</DefineConstants>' if args.timeout_baseline else '')+'</PropertyGroup></Project>')
(out/'PendingOperationRegistry.cs').write_text((ROOT/'src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs').read_text(encoding='utf-8-sig'),encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
import sys;sys.path.insert(0,str(ROOT/'tests'));from output_isolation import resolve_dotnet;_dotnet=resolve_dotnet(ROOT)
env=minimal_test_environment(_dotnet,out)
r=subprocess.run([str(_dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=150)
log='original='+str(args.original)+' baseline='+str(baseline)+' clockBudgetFixture=40ms/500ms sourceSha256='+hashlib.sha256(s.encode()).hexdigest()+' mutation='+str(args.mutate)+'\n'+r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)

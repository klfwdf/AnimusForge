import argparse,importlib.util,subprocess,os,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent
p=argparse.ArgumentParser();p.add_argument('--original',action='store_true');p.add_argument('--mutate',choices=['lose-start-boundary','return-null','swallow-owner-failure','allow-diagnostic-failure','drop-queue-claim','keep-failed-queue-live']);args=p.parse_args()
spec=importlib.util.spec_from_file_location('extractor',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
s=subprocess.check_output(['git','show','646dd987:ShoutBehavior.cs'],cwd=ROOT).decode('utf-8-sig') if args.original else (ROOT/'ShoutBehavior.cs').read_text(encoding='utf-8-sig')
code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig').replace('@@RESULT@@',ex.declaration(s,'private sealed class NativeConversationGameActionResult')).replace('@@QUEUE@@',ex.declaration(s,'private Task<NativeConversationGameActionResult> ApplyNativeConversationGameActionsOnMainThreadAsync('))
body=ex.declaration(s,'private async Task<string> SubmitNativeConversationTextInternalAsync(')
consumer=body[body.index('\t\tif (nativeActionResult?.ResponseDiscarded == true)'):body.index('\t\tnativeActionSw.Stop();')]
code=code.replace('@@CONSUMER_GATE@@',consumer)
if not args.original:
 overlay=(ROOT/'AnimusForgeNativeConversationOverlay.cs').read_text(encoding='utf-8-sig');reports=[]
 for signature,name in [('private async Task SubmitAsync(string text)','Normal'),('private async Task SubmitNpcInitiatedOpeningAsync(','Opening')]:
  method=ex.declaration(overlay,signature);handler=ex.declaration(method,'catch (ShoutBehavior.NativeConversationActionDispatchException ex)')
  assert 'suppressReadyNotice = true' in handler and 'RunNativePresentationCallback(generation,' in handler and 'PromptRetry' not in handler
  reports.append('private bool '+name+'(ShoutBehavior.NativeConversationActionDispatchException failure) { bool suppressReadyNotice=false;int generation=1;try { throw failure; } '+handler+' return suppressReadyNotice; }')
 code=code.replace('@@UI_FAILURE@@','\n'.join(reports)+'\ninternal bool Report(ShoutBehavior.NativeConversationActionDispatchException ex,bool opening)=>opening?Opening(ex):Normal(ex);')
else:code=code.replace('@@UI_FAILURE@@','')
if args.mutate=='drop-queue-claim':code=code.replace('if (Interlocked.CompareExchange(ref dispatchState, 1, 0) != 0)', 'if (false)', 1)
if args.mutate=='keep-failed-queue-live':code=code.replace('if (Interlocked.CompareExchange(ref dispatchState, 2, 0) == 0)', 'if (true)', 1)
out=HERE/'.generated'/('original' if args.original else args.mutate or 'current');out.mkdir(parents=True,exist_ok=True)
(out/'Program.cs').write_text(code,encoding='utf-8');enum=ex.declaration((ROOT/'Refactor/Contracts/InteractionContracts.cs').read_text(encoding='utf-8-sig'),'public enum ActionExecutionEffectState');(out/'Effect.cs').write_text('namespace AnimusForge.Refactor.Contracts;\n'+enum,encoding='utf-8')
if not args.original:
 boundary=(ROOT/'ShoutBehavior.NativeActionDispatch.cs').read_text(encoding='utf-8-sig')
 if args.mutate=='lose-start-boundary':boundary=boundary.replace('ownerStarted = true;','ownerStarted = false;',1)
 if args.mutate=='return-null':boundary=boundary.replace('throw new InvalidOperationException("native.action_result_missing");','return null;',1)
 if args.mutate=='swallow-owner-failure':boundary=boundary.replace('throw new NativeConversationActionDispatchException(ownerStarted, ex);','return new NativeConversationGameActionResult { Content = "fallback" };',1)
 if args.mutate=='allow-diagnostic-failure':boundary=boundary.replace('catch (Exception)\n        {\n            // Observability must never change whether actions run or how their Task completes.\n            return;\n        }','catch (Exception) { throw; }',1)
 (out/'Boundary.cs').write_text(boundary,encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion>'+('<DefineConstants>ORIGINAL</DefineConstants>' if args.original else '')+'</PropertyGroup></Project>')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
env=os.environ.copy();env.update(DOTNET_ROOT=r'G:\AFMOD\.dotnet-sdk',DOTNET_CLI_HOME=str(ROOT/'.tmp/dotnet-cli'),NUGET_PACKAGES=str(ROOT/'.tmp/nuget-packages'),DOTNET_GENERATE_ASPNET_CERTIFICATE='false',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
r=subprocess.run([r'G:\AFMOD\.dotnet-sdk\dotnet.exe','run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=150)
log='original='+str(args.original)+' sourceSha256='+hashlib.sha256(s.encode()).hexdigest()+' mutation='+str(args.mutate)+'\n'+r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)

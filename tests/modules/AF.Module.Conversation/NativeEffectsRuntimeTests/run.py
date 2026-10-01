"""Real dispatch/claim/completion bodies + real registry; game/action-family ports are substitutes."""
import argparse, importlib.util, subprocess, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]; HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
p=argparse.ArgumentParser(); p.add_argument('--run-root',type=Path,required=True); p.add_argument('--mutation', choices=['exit-once','history-acceptance','late-exit','capture-identity']); args=p.parse_args()
spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py'); ex=importlib.util.module_from_spec(spec); spec.loader.exec_module(ex)
def read(file): return (ROOT/'src/modules/AF.Module.Conversation/Channels/Native'/file).read_text(encoding='utf-8-sig')
effects=read('NativeConversationGameEffectsRuntime.cs'); completion=read('ShoutBehavior.NativeCompletion.cs'); dispatch=read('ShoutBehavior.NativeActionDispatch.cs')
types=['internal sealed class NativeConversationHistoryCommitException','private sealed class NativeConversationCompletionScope']
methods=['private NativeConversationCompletionScope CaptureNativeConversationCompletionOnMainThread(', 'private static bool IsNativeConversationCompletionCampaignCurrent(', 'private bool IsNativeConversationCompletionContextCurrent(', 'private string CompleteNativeConversationReplyOnMainThread(', 'private static void RunNativeAcceptedReplySideEffects(', 'private void QueueNativeConversationCompletionExit(']
body='\n'.join(ex.declaration(completion,x) for x in types+methods)+'\n'+ex.declaration(dispatch,'private NativeConversationGameActionResult ExecuteNativeConversationActionDispatch(')+'\n'+ex.declaration(dispatch,'internal static void ObserveNativeActionDispatch(')+'\n'+ex.declaration(effects,'internal Task<NativeConversationGameActionResult> ApplyNativeConversationGameActionsOnMainThreadAsync(')
code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8').replace('@@BODY@@',body).replace('@@REQUEST@@',ex.declaration(completion,'internal sealed class NativeConversationCompletionRequest')).replace('@@EXCEPTION@@',ex.declaration(dispatch,'internal sealed class NativeConversationActionDispatchException'))
mutations={
'exit-once': ('if (Interlocked.Exchange(ref scope.ExitClaimed, 1) != 0)', 'if (false)'),
'history-acceptance': ('if (memory?.HistoryWritten != true)', 'if (false)'),
'late-exit': ('if (IsNativeConversationCompletionContextCurrent(scope))\n                    CloseNativeConversationForSceneMechanism', 'if (true)\n                    CloseNativeConversationForSceneMechanism'),
'capture-identity': ('scope.SceneSessionId);', 'TryGetCurrentSceneHistorySessionIdForHistoryPersistence());')}
if args.mutation:
    old,new=mutations[args.mutation]; assert code.count(old)==1,(args.mutation,code.count(old)); code=code.replace(old,new,1)
assert '@@' not in code
out=new_run_root(ROOT,'native-effects-runtime',args.run_root)
(out/'Program.cs').write_text(code,encoding='utf-8')
(out/'Ports.cs').write_text(read('NativeConversationGameEffectPorts.cs'),encoding='utf-8')
(out/'Claim.cs').write_text(read('NativeConversationDispatchClaim.cs'),encoding='utf-8')
(out/'Registry.cs').write_text((ROOT/'src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs').read_text(encoding='utf-8-sig'),encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649;CS0169;CS0414</NoWarn></PropertyGroup></Project>')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet=resolve_dotnet(ROOT)
r=subprocess.run([str(dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90)
log=r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)

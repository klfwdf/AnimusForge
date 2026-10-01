"""Real deferred deadline/identity/cleanup and actual blocking request seam; game commit is a sentinel."""
from pathlib import Path
import sys as _legacy_sys
from pathlib import Path as _LegacyPath
_legacy_sys.path.insert(0, str(_LegacyPath(__file__).resolve().parents[4] / "tests"))
from af2_terminal_migration_review import historical_source
# Explicit legacy oracle; current owner build/replay inputs are not projected.
AF2_FIXTURE_METADATA = {'sourceClass': 'legacy-oracle-extraction', 'terminalBindingAndExactInverseRequired': True, 'currentOwnerReplayProjected': False}
import argparse
import hashlib
import importlib.util
import json
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
spec = importlib.util.spec_from_file_location('extract', ROOT / 'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--run-root', type=Path)
parser.add_argument('--mutate', choices=['deadline-does-not-cancel', 'scope-does-not-bind', 'identity-ignores-cancel'])
args = parser.parse_args()
out = new_run_root(ROOT, 'scene-deferred-cancellation', args.run_root)
source_path = 'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ScenePostprocess.cs'
request_path = 'src/modules/AF.Module.Conversation/Internal/Postprocess/ShoutBehavior.UnifiedActionPostprocess.cs'
source = historical_source(source_path)
deadline = extract.declaration(source, 'async Task EnforceRequestDeadlineAsync(')
current = extract.declaration(source, 'bool IsRequestCurrent()')
request = extract.declaration(historical_source(request_path),
                              'private static bool TryRequestSceneUnifiedActionPostprocess(')
request_owner = extract.declaration((ROOT/'src/modules/AF.Module.Conversation/Internal/Postprocess/ConversationActionPostprocessOwner.cs').read_text(encoding='utf-8-sig'), 'internal static bool TryRequestSceneUnifiedActionPostprocess(')
start = source.index('\t\t\tfinally\n\t\t\t{', source.index('Task deadlineTask'))
cleanup = source[start:source.index('\n\t\t});', start)]
if args.mutate == 'deadline-does-not-cancel':
    assert deadline.count('networkCancellation.Cancel();') == 1, 'Deadline cancellation seam changed'
    deadline = deadline.replace('networkCancellation.Cancel();', '/* mutation */;')
elif args.mutate == 'identity-ignores-cancel':
    assert '!networkCancellation.IsCancellationRequested && ' in current and 'Volatile.Read(ref requestRetired) == 0' in current, 'Request cancellation identity seam changed'
    current = current.replace('!networkCancellation.IsCancellationRequested && ', '').replace('Volatile.Read(ref requestRetired) == 0', 'true')
scope_lines = [line.strip() for line in source.splitlines() if 'using IDisposable cancellationScope =' in line]
assert len(scope_lines) == 1, 'Deferred transport scope seam changed'
scope = scope_lines[0]
if args.mutate == 'scope-does-not-bind':
    assert scope.count('networkCancellation.Token') == 1, 'Deferred transport token binding changed'
    scope = scope.replace('networkCancellation.Token', 'CancellationToken.None')
code = r'''
using System;using System.Net;using System.Net.Http;using System.Threading;using System.Threading.Tasks;using AnimusForge.Refactor.Runtime;
namespace AnimusForge;
static class Logger{internal static void Log(string a,string b){}}
static class SaveRuntimeGuard{internal static bool IsCurrentGeneration(long g)=>g==1;}
static class LlmApiCompat{internal static void ApplyAuthenticationHeaders(HttpRequestMessage r,string e,string k){}}
static class AIConfigHandler{
 internal static bool TryCallAuxiliaryActionPostprocess(string s,string u,int m,float t,out string content,out string error){
  try{content=LlmNonStreamingTransport.SendAsync("https://fixture.invalid","fixture","{}",Program.Send,default).GetAwaiter().GetResult().Body;error="";return true;}
  catch(OperationCanceledException){content="";error="cancelled";return false;}
 }
}
static class Program{
 const int ScenePostprocessGateWaitTimeoutMilliseconds=40;
 enum ScenePostprocessStatus{Completed,NoAction,Stale,TargetUnavailable,TimedOut,Failed}
 static int _sceneHistorySessionId=1,_sceneConversationEpoch=1;
 internal static Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> Send;
 @@REQUEST@@
 static async Task Main(){
  foreach(bool timeout in new[]{true,false}){
   var requestLifetime=new ConversationRequestLifetime();
   CancellationTokenSource networkCancellation=CancellationTokenSource.CreateLinkedTokenSource(requestLifetime.Token);
   ExecutionContext requestExecutionContext=ExecutionContext.Capture();object runtimeScopeLock=new();int requestRetired=0;
   long queuedRuntimeGeneration=1;int queuedSceneSessionId=1,capturedConversationEpoch=1;string targetLog="fixture";
   var result=new TaskCompletionSource<ScenePostprocessStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
   bool Complete(ScenePostprocessStatus s,int relayTargetAgentIndex=-1)=>result.TrySetResult(s);
   @@CURRENT@@
   @@DEADLINE@@
   CancellationTokenSource deadlineCancellation=new();Task deadlineTask=EnforceRequestDeadlineAsync(deadlineCancellation.Token);
   var entered=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);CancellationToken actual=default;int commits=0;
   Send=async(_,token)=>{actual=token;entered.TrySetResult(true);await Task.Delay(Timeout.Infinite,token);return null;};
   Task worker=Task.Run(async()=>{
    try{
     using IDisposable requestWorker=requestLifetime.Enter();
     @@SCOPE@@
     TryRequestSceneUnifiedActionPostprocess("fixture","fixture",out _,out _);
     if(IsRequestCurrent()) commits++;
    }@@CLEANUP@@
   });
   await entered.Task;
   if(!timeout)requestLifetime.Retire();
   Task winner=await Task.WhenAny(worker,Task.Delay(1500));
   if(winner!=worker||!actual.IsCancellationRequested)throw new Exception("ASSERT scene-deferred-"+(timeout?"deadline":"owner-retirement")+"-aborts-actual-request");
   await worker;
   if(commits!=0)throw new Exception("ASSERT scene-deferred-canceled-result-no-commit");
   if(!deadlineTask.IsCompleted)throw new Exception("ASSERT deadline-joined-before-source-disposal");
   if(timeout&&await result.Task!=ScenePostprocessStatus.TimedOut)throw new Exception("ASSERT deadline-status-preserved");
  }
  Console.WriteLine("PASS SceneDeferredCancellation cases=2 deadline/ownerAbort=2 noCommit=2 actualDeadlineIdentityCleanup=true actualBlockingSeam=true downstreamGameCommit=SENTINEL game=NOT-RUN");
 }
}
'''
code += '\ninternal static class ConversationActionPostprocessOwner { '+request_owner+' }'

for key, value in [('REQUEST', request), ('CURRENT', current), ('DEADLINE', deadline), ('CLEANUP', cleanup), ('SCOPE', scope)]:
    code = code.replace('@@' + key + '@@', value)
(out / 'Program.cs').write_text(code, encoding='utf-8')
linked_paths = ['src/modules/AF.Module.Llm/Transport/LlmNonStreamingTransport.cs',
             'src/modules/AF.Module.Conversation/Internal/ConversationRequestLifetime.cs',
             'src/modules/AF.Module.Conversation/Internal/InteractionRequestLease.cs']
for path in linked_paths:
    (out / Path(path).name).write_text((ROOT / path).read_text(encoding='utf-8-sig'), encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
(out / 'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup></Project>', encoding='utf-8')
dotnet = resolve_dotnet(ROOT)
result = subprocess.run([str(dotnet), 'run', '--project', str(out / 'Proof.csproj'), '-c', 'Release'], cwd=out,
    env=minimal_test_environment(dotnet, out), capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=90)
log = result.stdout + result.stderr
(out / 'run.log').write_text(log, encoding='utf-8')
(out / 'result.json').write_text(json.dumps({
    'mutation': args.mutate, 'exit_code': result.returncode,
    'compiled': (out / 'bin/Release/net8.0/Proof.dll').is_file(),
    'source_sha256': {path: hashlib.sha256((ROOT / path).read_bytes()).hexdigest()
                      for path in [source_path, request_path, *linked_paths]},
    'harness_sha256': hashlib.sha256(code.encode('utf-8')).hexdigest(),
    'downstream_game_commit': 'SENTINEL', 'game': 'NOT-RUN',
}, indent=2), encoding='utf-8')
print('output=' + str(out))
print(log)
raise SystemExit(result.returncode)

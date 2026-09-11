import argparse, importlib.util, subprocess, os
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent
p=argparse.ArgumentParser();p.add_argument('--original',action='store_true');p.add_argument('--memory-baseline',action='store_true');p.add_argument('--mutate');a=p.parse_args()
spec=importlib.util.spec_from_file_location('ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
baseline='d9288faa' if a.original else '29ca75c9' if a.memory_baseline else None
def read(name):return subprocess.check_output(['git','show',baseline+':'+name],cwd=ROOT).decode('utf-8-sig') if baseline else (ROOT/name).read_text(encoding='utf-8-sig')
s=read('ShoutBehavior.cs');ad=read('ShoutBehavior.NativeAdmission.cs');body=ex.declaration(s,'private async Task<string> SubmitNativeConversationTextInternalAsync(')
start=body.index('\t\tNativeConversationGameActionResult nativeActionResult = await ApplyNativeConversationGameActionsOnMainThreadAsync(')
values={'RESULT':ex.declaration(s,'private sealed class NativeConversationGameActionResult'),'DISPATCH':ex.declaration(s,'private Task<NativeConversationGameActionResult> ApplyNativeConversationGameActionsOnMainThreadAsync('),'TAIL':body[start:body.rfind('\n\t}')],'ADMISSION':ex.declaration(ad,'internal sealed class NativeConversationAdmission'),'CHECKS':'\n'.join(ex.declaration(ad,x) for x in ['private bool IsNativeConversationAdmissionCurrent(','private bool IsNativeConversationContextStampCurrent(','private bool IsNativeConversationContextCurrent(']),'NO_SPEECH':ex.declaration(s,'private static bool IsNativeConversationNoSpeechPlaceholder(')}
memory=read('MyBehavior.cs')
assert 'private const string NonHeroMemoryIdPrefix = "af_nonhero:";' in memory
values['MEMORY_METHODS']='\n'.join(ex.declaration(memory,x) for x in ['public static void AppendExternalDialogueHistory(','public static void AppendExternalSceneDialogueHistory(','public static void AppendExternalNonHeroDialogueHistory(','public static void AppendExternalNonHeroSceneDialogueHistory(','public static MemoryCommitResult CommitExternalDialogueHistory(','private static string NormalizeMemoryHeroId(','private static bool IsNonHeroMemoryId(','private static bool IsHeroNpcEligibleForCompressedMemory('])
overlay=read('AnimusForgeNativeConversationOverlay.cs');handlers=[]
for signature,name in [('private async Task SubmitAsync(string text)','Normal'),('private async Task SubmitNpcInitiatedOpeningAsync(','Opening')]:
 method=ex.declaration(overlay,signature);handler=ex.declaration(method,'catch (ShoutBehavior.NativeConversationActionDispatchException ex)')
 handlers.append('private bool '+name+'(ShoutBehavior.NativeConversationActionDispatchException failure) { bool suppressReadyNotice=false;int generation=1;try { throw failure; } '+handler+' return suppressReadyNotice; }')
values['UI_HANDLERS']='\n'.join(handlers)+'\ninternal bool Report(ShoutBehavior.NativeConversationActionDispatchException e,bool opening)=>opening?Opening(e):Normal(e);'
code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig')
for k,v in values.items():code=code.replace('@@'+k+'@@',v)
assert '@@' not in code
out=HERE/'.generated'/('original' if a.original else 'memory-baseline' if a.memory_baseline else a.mutate or 'current');out.mkdir(parents=True,exist_ok=True)
(out/'Program.cs').write_text(code,encoding='utf-8');(out/'Dispatch.cs').write_text(read('ShoutBehavior.NativeActionDispatch.cs'),encoding='utf-8')
contracts=(ROOT/'Refactor/Contracts/InteractionContracts.cs').read_text(encoding='utf-8-sig');types='\n'.join(ex.declaration(contracts,x) for x in ['public enum ActionExecutionEffectState','public enum MemoryCommitStatus','public sealed class MemoryCommitResult']);(out/'Effect.cs').write_text('namespace AnimusForge.Refactor.Contracts;\n'+types,encoding='utf-8')
if not baseline:
 memory_owner=read('MyBehavior.DialogueHistoryCommit.cs')
 canonical=ex.declaration(memory_owner,'internal static MemoryCommitResult CommitDialogueHistoryWithScene(')
 prior=ex.declaration(subprocess.check_output(['git','show','29ca75c9:MyBehavior.cs'],cwd=ROOT).decode('utf-8-sig'),'public static MemoryCommitResult CommitExternalDialogueHistory(')
 inverse=canonical.replace(canonical.splitlines()[0],prior.splitlines()[0],1).replace('owner.AppendDialogueHistoryById(normalizedMemoryId, npcName, playerText, aiText, extraFact, sceneSessionId)','owner.AppendDialogueHistoryById(normalizedMemoryId, npcName, playerText, aiText, extraFact)',1).replace('owner.AppendDialogueHistory(hero, playerText, aiText, extraFact, sceneSessionId)','owner.AppendDialogueHistory(hero, playerText, aiText, extraFact)',1)
 assert inverse==prior, 'Strict owner logic changed beyond the explicit scene argument'
 if a.mutate=='lose-owner-scene':memory_owner=memory_owner.replace('extraFact, sceneSessionId)','extraFact, -1)')
 if a.mutate=='accept-owner-false':memory_owner=memory_owner.replace('new MemoryCommitResult(MemoryCommitStatus.Failed, "memory_owner_write_unconfirmed")','new MemoryCommitResult(MemoryCommitStatus.Applied)',1)
 if a.mutate=='drop-memory-thread':memory_owner=memory_owner.replace('if (!TWParallel.IsMainThread())','if (false)',1)
 if a.mutate=='public-scene-owner':memory_owner=memory_owner.replace('internal static MemoryCommitResult CommitDialogueHistoryWithScene','public static MemoryCommitResult CommitDialogueHistoryWithScene',1)
 (out/'MemoryOwner.cs').write_text(memory_owner,encoding='utf-8')
if not a.original:
 completion=read('ShoutBehavior.NativeCompletion.cs')
 if a.mutate=='drop-memory-acceptance':completion=completion.replace('if (memory?.HistoryWritten != true)','if (false)',1)
 if a.mutate=='lose-failed-exit':completion=completion.replace('QueueNativeConversationCompletionExit(scope, result);',';',1)
 if a.mutate=='drop-discard-context':
  old='''if (eventSequence <= 0 || !IsNativeConversationContextStampCurrent(admission)
            || admission.PresentationRevision != Interlocked.Read(ref _nativeConversationPresentationRevision)
            || !TryResolveNativeConversationTarget(out Hero hero, out var character, out _)
            || !ReferenceEquals(hero, admission.Hero) || !ReferenceEquals(character, admission.Character))'''
  assert old in completion;completion=completion.replace(old,'if (eventSequence <= 0)',1)
 if a.mutate=='drop-generation':completion=completion.replace('SaveRuntimeGuard.IsCurrentGeneration(scope.Admission.Generation)','true',1)
 if a.mutate=='current-scene':
  cut=completion.index('private string CompleteNativeConversationReplyOnMainThread(')
  completion=completion[:cut]+completion[cut:].replace('scope.SceneSessionId','TryGetCurrentSceneHistorySessionIdForHistoryPersistence()')
 if a.mutate=='late-nonhero':completion=completion.replace('Hero hero = scope.Admission.Hero;','Hero hero = scope.Admission.Hero; if (hero == null) scope.HasNonHeroMemory = TryResolveWildernessNonHeroMemory(scope.Npc, hero, scope.Admission.Character, scope.AgentIndex, out scope.NonHeroMemoryId, out scope.NonHeroMemoryName);',1)
 if a.mutate=='drop-context':completion=completion.replace('return scope != null && scope.Admission.PresentationRevision == Interlocked.Read(ref _nativeConversationPresentationRevision)\n            && IsNativeConversationContextCurrent(scope.Admission, out _);','return true;',1)
 if a.mutate=='drop-close-guard':completion=completion.replace('if (IsNativeConversationCompletionContextCurrent(scope))\n                    CloseNativeConversationForSceneMechanism','if (true)\n                    CloseNativeConversationForSceneMechanism',1)
 if a.mutate=='backend-close':completion=completion.replace('return scope != null && scope.Admission.PresentationRevision', 'return scope != null && ReferenceEquals(_nativeConversationAdmission, scope.Admission) && scope.Admission.PresentationRevision',1)
 if a.mutate=='drop-completion':
  code=code.replace('result.FinalVisible = CompleteNativeConversationReplyOnMainThread(completionScope, result);','result.FinalVisible = result.Content;',1)
  (out/'Program.cs').write_text(code,encoding='utf-8')
 if a.mutate=='capture-after-start':
  dispatch=read('ShoutBehavior.NativeActionDispatch.cs').replace('beforeOwner?.Invoke();\n            ownerStarted = true;','ownerStarted = true;\n            beforeOwner?.Invoke();',1)
  (out/'Dispatch.cs').write_text(dispatch,encoding='utf-8')
 (out/'Completion.cs').write_text(completion,encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion>'+('<DefineConstants>ORIGINAL</DefineConstants>' if a.original else '<DefineConstants>MEMORY_BASELINE</DefineConstants>' if a.memory_baseline else '')+'</PropertyGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
env=os.environ.copy();env.update(DOTNET_ROOT=r'G:\AFMOD\.dotnet-sdk',DOTNET_CLI_HOME=str(ROOT/'.tmp/dotnet-cli'),NUGET_PACKAGES=str(ROOT/'.tmp/nuget-packages'),DOTNET_GENERATE_ASPNET_CERTIFICATE='false',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
r=subprocess.run([r'G:\AFMOD\.dotnet-sdk\dotnet.exe','run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=150)
log=r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)

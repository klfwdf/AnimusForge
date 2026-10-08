import argparse, importlib.util, subprocess, os
from pathlib import Path
import sys
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment,current_source_path
from af2_terminal_migration_review import historical_source
AF2_FIXTURE_METADATA = {'sourceClass': 'legacy-completion-protocol-oracle/current-shared-memory-owner', 'terminalBindingAndExactInverseRequired': True, 'currentOwnerCompileLinksProjected': False, 'currentOwnerEvidence': ['NativeEffectsRuntimeTests/run.py', 'NativeTurn/run.py'], 'wholeCurrentProtocolExecution': 'NOT_RUN'}
p=argparse.ArgumentParser();p.add_argument('--original',action='store_true');p.add_argument('--memory-baseline',action='store_true');p.add_argument('--mutate');p.add_argument('--run-root',type=Path);a=p.parse_args()
spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
baseline='d9288faa' if a.original else '29ca75c9' if a.memory_baseline else None
def read(name):return subprocess.check_output(['git','show',baseline+':'+name],cwd=ROOT).decode('utf-8-sig') if baseline else (historical_source(name) if name in {'ShoutBehavior.NativeActionDispatch.cs', 'ShoutBehavior.NativeCompletion.cs'} else current_source_path(ROOT,name).read_text(encoding='utf-8-sig'))
s=read('ShoutBehavior.cs');ad=read('ShoutBehavior.NativeAdmission.cs');
# The unchanged boundary is source-projected from verified current phases; NativeTurn executes the new schedule.
import sys
sys.path.insert(0,str(ROOT/'tests/modules/AF.Module.Conversation/NativeConversationAdmissionTests'))
from turn_extraction import projected_source, NEW_SIGNATURE
if NEW_SIGNATURE in s: s=projected_source(s)
body=ex.declaration(s,'private async Task<string> SubmitNativeConversationTextInternalAsync(')
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
values['ROLLBACK']='' if baseline else ex.declaration(s,'private static void RollbackNativeConversationPendingPlayerHistory(')
if not baseline:
 # The accepted-reply gate lives in the CURRENT dispatch lambda (the projection returns the
 # pre-extraction file). Use the live declaration, and require it to differ from the projected
 # one only by that gate so no other dispatch drift slips in unreviewed.
 live=historical_source('ShoutBehavior.cs').replace('\r\n','\n') # Fixed pre-terminal accepted-reply gate oracle.
 dispatch=ex.declaration(live,'private Task<NativeConversationGameActionResult> ApplyNativeConversationGameActionsOnMainThreadAsync(')
 gate_old='\t\t\t\tif (completionScope != null && result != null && !result.ResponseDiscarded)\n\t\t\t\t\tresult.FinalVisible = CompleteNativeConversationReplyOnMainThread(completionScope, result);\n'
 gate_new='\t\t\t\tif (completionScope != null && result != null && !result.ResponseDiscarded)\n\t\t\t\t{\n\t\t\t\t\tRunNativeAcceptedReplySideEffects(completion);\n\t\t\t\t\tresult.FinalVisible = CompleteNativeConversationReplyOnMainThread(completionScope, result);\n\t\t\t\t}\n'
 assert dispatch.count(gate_new)==1 and dispatch.replace(gate_new,gate_old)==values['DISPATCH'].replace('\r\n','\n'), 'Unreviewed Native dispatch drift'
 if a.mutate=='accepted-effects-on-discard':dispatch=dispatch.replace(gate_new,'\t\t\t\tRunNativeAcceptedReplySideEffects(completion);\n'+gate_old,1)
 values['DISPATCH']=dispatch
 # Production NativeTurnCommit supplies AcceptedReplySideEffects; the projected tail predates it,
 # so the fixture attaches a counting delegate to observe the real dispatch/completion gate.
 hook='PendingPlayerHistoryKey = nativePendingAfefKey\n'
 assert values['TAIL'].count(hook)==1,'completion request shape changed'
 values['TAIL']=values['TAIL'].replace(hook,'PendingPlayerHistoryKey = nativePendingAfefKey,\n\t\t\t\tAcceptedReplySideEffects = () => { Host.Game(); Host.Accepted++; }\n',1)
code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig')
for k,v in values.items():code=code.replace('@@'+k+'@@',v)
assert '@@' not in code
out=new_run_root(ROOT,'NativeCompletionBoundaryTests',a.run_root)
(out/'AnimusForgeDialogueHistoryEntry.cs').write_text(read('AnimusForgeDialogueHistoryEntry.cs'),encoding='utf-8')
(out/'Program.cs').write_text(code,encoding='utf-8');(out/'Dispatch.cs').write_text(read('ShoutBehavior.NativeActionDispatch.cs'),encoding='utf-8')
contracts=(ROOT/'src/AF.Contracts/Internal/InteractionContracts.cs').read_text(encoding='utf-8-sig');types='\n'.join(ex.declaration(contracts,x) for x in ['public enum ActionExecutionEffectState','public enum MemoryCommitStatus','public sealed class MemoryCommitResult']);(out/'Effect.cs').write_text('namespace AnimusForge.Refactor.Contracts;\n'+types,encoding='utf-8')
if not baseline:
 memory_owner=read('MyBehavior.DialogueHistoryCommit.cs')
 wrapper=ex.declaration(memory_owner,'internal static MemoryCommitResult CommitDialogueHistoryWithScene(string memoryId, bool isNonHero, string npcName, string playerText, string aiText, string extraFact, int sceneSessionId)')
 assert 'return CommitDialogueHistoryWithScene(memoryId, isNonHero, npcName, playerText, aiText, extraFact, sceneSessionId, -1, null);' in wrapper, 'seven-argument Native memory ABI changed'
 canonical=ex.declaration(memory_owner,'internal static MemoryCommitResult CommitDialogueHistoryWithScene(string memoryId, bool isNonHero, string npcName, string playerText, string aiText, string extraFact, int sceneSessionId, int playerTargetAgentIndex')
 prior=ex.declaration(subprocess.check_output(['git','show','29ca75c9:MyBehavior.cs'],cwd=ROOT).decode('utf-8-sig'),'public static MemoryCommitResult CommitExternalDialogueHistory(')
 application_source=read('src/AF.GameAdapter.Bannerlord/Memory/MemoryHistoryCommitBannerlordAdapter.cs')
 application=ex.declaration(application_source,'internal MemoryCommitResult CommitDialogueHistoryWithScene(')
 route='return owner._memoryHistoryCommit.CommitDialogueHistoryWithScene(memoryId,isNonHero,npcName,playerText,aiText,extraFact,sceneSessionId,playerTargetAgentIndex,playerTargetName);'
 assert canonical.count(route)==1, 'strict scene ABI must call the sole history commit owner'
 core=application[application.index('\t\t\tstring normalizedMemoryId'):application.index('\n  }\n  catch')]
 inverse_core=core.replace('MemoryEntityIdentityBannerlordAdapter.FindHeroById(', 'FindHeroById(').replace('MemoryEntityIdentityBannerlordAdapter.IsHeroNpcEligibleForCompressedMemory(', 'IsHeroNpcEligibleForCompressedMemory(')
 for name,args in [('AppendDialogueHistoryById','normalizedMemoryId, npcName, playerText, aiText, extraFact'),('AppendDialogueHistory','hero, playerText, aiText, extraFact')]:
  current=name+'('+args+', sceneSessionId, playerTargetAgentIndex, playerTargetName)'
  assert inverse_core.count(current)==1
  inverse_core=inverse_core.replace(current,'owner.'+name+'('+args+')',1)
 inverse=canonical.replace(canonical.splitlines()[0],prior.splitlines()[0],1).replace('\t\t\t'+route,inverse_core,1)
 assert inverse==prior, 'Strict owner logic changed beyond the explicit scene argument'
 if a.mutate=='lose-owner-scene':
  before='extraFact, sceneSessionId, -1, null)';assert memory_owner.count(before)==1;memory_owner=memory_owner.replace(before,'extraFact, -1, -1, null)',1)
 if a.mutate=='accept-owner-false':
  before='new MemoryCommitResult(MemoryCommitStatus.Failed, "memory_owner_write_unconfirmed")';assert application.count(before)==1;application=application.replace(before,'new MemoryCommitResult(MemoryCommitStatus.Applied)',1)
 # Full current commit method, with only the original storage/game leaves controlled.
 storage_leaves=r"""
 private readonly MyBehavior _owner;
 internal MemoryHistoryCommitBannerlordAdapter(MyBehavior owner){_owner=owner;}
 private static string NormalizeMemoryHeroId(string id)=>MemoryRecordRules.NormalizeMemoryHeroId(id);
 private static bool IsNonHeroMemoryId(string id)=>MemoryBusinessStateOwner.IsNonHeroMemoryId(id);
 private bool AppendDialogueHistory(Hero hero,string p,string a,string f,int session,int target,string targetName)=>_owner.AppendDialogueHistory(hero,p,a,f,session,target,targetName);
 private bool AppendDialogueHistoryById(string id,string name,string p,string a,string f,int session,int target,string targetName)=>_owner.AppendDialogueHistoryById(id,name,p,a,f,session,target,targetName);
 """
 (out/'MemoryApplication.cs').write_text('using System; using AnimusForge.Refactor.Contracts; using TaleWorlds.CampaignSystem; namespace AnimusForge { public partial class MyBehavior { internal sealed class MemoryHistoryCommitBannerlordAdapter {'+storage_leaves+application+'} } }',encoding='utf-8')
 host_anchor='public partial class MyBehavior {';assert code.count(host_anchor)==1
 code=code.replace(host_anchor,host_anchor+'\n private readonly MemoryHistoryCommitBannerlordAdapter _memoryHistoryCommit; public MyBehavior(){_memoryHistoryCommit=new MemoryHistoryCommitBannerlordAdapter(this);}\n',1)
 (out/'Program.cs').write_text(code,encoding='utf-8')
 identity=read('src/AF.GameAdapter.Bannerlord/Memory/MemoryEntityIdentityBannerlordAdapter.cs')
 business=read('src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.cs')
 identity_body=ex.declaration(identity,'internal static bool IsHeroNpcEligibleForCompressedMemory(')
 nonhero_body=ex.declaration(business,'internal static bool IsNonHeroMemoryId(')
 (out/'MemoryIdentity.cs').write_text('using System; using TaleWorlds.CampaignSystem; namespace AnimusForge { internal static class MemoryEntityIdentityBannerlordAdapter { internal static Hero FindHeroById(string id)=>Hero.Find(id); '+identity_body+' } internal static class MemoryBusinessStateOwner { private const string NonHeroMemoryIdPrefix="af_nonhero:"; '+nonhero_body+' } }',encoding='utf-8')
 if a.mutate=='drop-memory-thread':memory_owner=memory_owner.replace('if (!TWParallel.IsMainThread())','if (false)',1)
 if a.mutate=='public-scene-owner':memory_owner=memory_owner.replace('internal static MemoryCommitResult CommitDialogueHistoryWithScene','public static MemoryCommitResult CommitDialogueHistoryWithScene',1)
 (out/'MemoryOwner.cs').write_text(memory_owner,encoding='utf-8')
if not a.original:
 completion=read('ShoutBehavior.NativeCompletion.cs')
 if a.mutate=='drop-memory-acceptance':completion=completion.replace('if (memory?.HistoryWritten != true)','if (false)',1)
 if a.mutate=='lose-failed-exit':completion=completion.replace('QueueNativeConversationCompletionExit(scope, result);',';',1)
 if a.mutate=='drop-discard-context':
  code=code.replace('|| !owner.IsNativeConversationContextStampCurrent(admission)\n            || !owner._nativeAdmissionOwner.IsPresentationCurrent(admission.PresentationRevision)','|| false',1)
  (out/'Program.cs').write_text(code,encoding='utf-8')
 if a.mutate=='drop-generation':completion=completion.replace('SaveRuntimeGuard.IsCurrentGeneration(scope.Admission.Generation)','true',1)
 if a.mutate=='current-scene':
  cut=completion.index('private string CompleteNativeConversationReplyOnMainThread(')
  completion=completion[:cut]+completion[cut:].replace('scope.SceneSessionId','TryGetCurrentSceneHistorySessionIdForHistoryPersistence()')
 if a.mutate=='late-nonhero':completion=completion.replace('Hero hero = scope.Admission.Hero;','Hero hero = scope.Admission.Hero; if (hero == null) scope.HasNonHeroMemory = TryResolveWildernessNonHeroMemory(scope.Npc, hero, scope.Admission.Character, scope.AgentIndex, out scope.NonHeroMemoryId, out scope.NonHeroMemoryName);',1)
 if a.mutate=='drop-context':completion=completion.replace('return scope != null && _nativeAdmissionOwner.IsPresentationCurrent(scope.Admission.PresentationRevision)\n            && IsNativeConversationContextCurrent(scope.Admission, out _);','return true;',1)
 if a.mutate=='drop-close-guard':completion=completion.replace('if (IsNativeConversationCompletionContextCurrent(scope))\n                    CloseNativeConversationForSceneMechanism','if (true)\n                    CloseNativeConversationForSceneMechanism',1)
 if a.mutate=='backend-close':completion=completion.replace('return scope != null && _nativeAdmissionOwner.IsPresentationCurrent', 'return scope != null && _nativeAdmissionOwner.Owns(scope.Admission) && _nativeAdmissionOwner.IsPresentationCurrent',1)
 if a.mutate=='drop-completion':
  code=code.replace('result.FinalVisible = CompleteNativeConversationReplyOnMainThread(completionScope, result);','result.FinalVisible = result.Content;',1)
  (out/'Program.cs').write_text(code,encoding='utf-8')
 if a.mutate=='capture-after-start':
  dispatch=read('ShoutBehavior.NativeActionDispatch.cs').replace('beforeOwner?.Invoke();\n            ownerStarted = true;','ownerStarted = true;\n            beforeOwner?.Invoke();',1)
  (out/'Dispatch.cs').write_text(dispatch,encoding='utf-8')
 (out/'Completion.cs').write_text(completion,encoding='utf-8')
spec_core=importlib.util.spec_from_file_location('native_core_fixture',ROOT/'tests/modules/AF.Module.Conversation/NativeModuleSubmissionTests/fixture_support.py');core_fixture=importlib.util.module_from_spec(spec_core);spec_core.loader.exec_module(core_fixture);core_fixture.include_operation_sources(out)
if not baseline:
 core_fixture.include_admission_owner(out);code=core_fixture.migrate_admission_fixture(code)
 # Execute current admission checks through the same sole adapter/state owner.
 # Generation/opening capture are outside this completion suite, not fake admission algorithms.
 dto=ex.declaration(ad,'internal sealed class NativeConversationAdmissionException')
 anchor='public partial class ShoutBehavior {';assert code.count(anchor)==1
 code=code.replace(anchor,anchor+'\n'+dto+'\nprivate readonly NativeAdmissionApplicationAdapter NativeAdmissions;\n',1)
 ctor='internal ShoutBehavior(){CurrentInstance=this;';assert code.count(ctor)==1
 binding='NativeAdmissions=new NativeAdmissionApplicationAdapter(_nativeAdmissionOwner,IsBannerlordMainThreadForNativeActions,()=>ReferenceEquals(CurrentInstance,this),()=>true,_mainThreadActions.Enqueue,NativeConversationMainThreadPreprocessTimeoutMs,TryResolveNativeConversationTarget,TryResolveNativeConversationAgentIndex,IsNativeConversationResponseTargetAvailableForActionDispatch,(a,t,s,d,p,r,o)=>Task.FromException<string>(new NotSupportedException("Generation is not a completion fixture capability")));'
 code=code.replace(ctor,ctor+binding,1)
 guard='static class SaveRuntimeGuard {internal static long Generation;';assert code.count(guard)==1
 code=code.replace(guard,guard+'internal static long CaptureGeneration()=>Generation;',1)
 (out/'NativeAdmissionApplicationAdapter.cs').write_text((ROOT/'src/modules/AF.Module.Conversation/Channels/Native/NativeAdmissionApplicationAdapter.cs').read_text(encoding='utf-8-sig'),encoding='utf-8')
 (out/'AdmissionOpeningLeaf.cs').write_text('using System;using TaleWorlds.CampaignSystem;namespace AnimusForge { internal static class NpcInitiatedOpeningRouter { internal static bool TryConsumePendingNativeOpening(Hero h,out string fact,out string prompt,out string source)=>throw new NotSupportedException("Opening capture is not requested by completion fixture"); } }',encoding='utf-8')
 (out/'Program.cs').write_text(code,encoding='utf-8')
if not baseline:
 memory_rules=(ROOT/'src/modules/AF.Module.Memory/Records/MemoryPersistenceModels.cs').read_text(encoding='utf-8-sig')
 (out/'MemoryRecordRules.cs').write_text('namespace AnimusForge { internal static class MemoryRecordRules { '+ex.declaration(memory_rules,'internal static string NormalizeMemoryHeroId(')+' }}',encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion>'+('<DefineConstants>ORIGINAL</DefineConstants>' if a.original else '<DefineConstants>MEMORY_BASELINE</DefineConstants>' if a.memory_baseline else '')+'</PropertyGroup></Project>',encoding='utf-8')
(out/'PendingOperationRegistry.cs').write_text((ROOT/'src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs').read_text(encoding='utf-8-sig'),encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
dotnet=resolve_dotnet(ROOT);env=minimal_test_environment(dotnet,out)
r=subprocess.run([str(dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=150)
log=r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)

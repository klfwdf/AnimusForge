"""Exact game lifecycle/retirement changes before previous whole-file proofs."""
from pathlib import Path
import hashlib,importlib.util,json,re,subprocess
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent;BASELINE='807bc5b9'
spec=importlib.util.spec_from_file_location('life_decl',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');e=importlib.util.module_from_spec(spec);spec.loader.exec_module(e)
PATHS=('ShoutBehavior.cs','CourierDeliveryBehavior.cs','src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DetachedPostprocess.cs','SubModule.cs','MyBehavior.MemorySummaryMainThread.cs')
MOVED_DEPENDENCIES={
 'Refactor/Runtime/PendingOperationRegistry.cs':'src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs',
 'Refactor/Runtime/GameLifetimeCoordinator.cs':'src/AF.Foundation.Runtime/Lifecycle/GameLifetimeCoordinator.cs',
 'AfCampaignRuntimeLifecycle.cs':'src/AF.GameAdapter.Bannerlord/Composition/AfCampaignRuntimeLifecycle.cs',
}
RUNNER_PATH_EDITS={
 'Refactor/Runtime/MemorySummaryDispatcher.cs':'src/modules/AF.Module.Memory/Summary/MemorySummaryDispatcher.cs',
 'Refactor/Contracts/IMemorySummaryDispatchHost.cs':'src/modules/AF.Module.Memory/Summary/IMemorySummaryDispatchHost.cs',
 'Refactor/Runtime/InteractionResultCommitter.cs':'src/modules/AF.Module.Actions/Receipts/InteractionResultCommitter.cs',
 'Refactor/Runtime/PendingOperationRegistry.cs':'src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs',
 'Refactor/Runtime/GameLifetimeCoordinator.cs':'src/AF.Foundation.Runtime/Lifecycle/GameLifetimeCoordinator.cs',
 'AfCampaignRuntimeLifecycle.cs':'src/AF.GameAdapter.Bannerlord/Composition/AfCampaignRuntimeLifecycle.cs',
}
def restore_commit(source):
 spec=importlib.util.spec_from_file_location('courier_outcome_inverse',ROOT/'tools/CourierCommitOutcomeTests/source_parity.py');module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
 return module.restore(source)

def old(path):return subprocess.check_output(['git','show',BASELINE+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
def expected(path):
 s=old(path)
 if path=='ShoutBehavior.cs':
  sig='private Task<T> RunNativeConversationMainThreadFuncAsync<T>(';before=e.declaration(s,sig);after=before.replace('if (func == null) return Task.FromResult(fallback);','long retirementVersion = _pendingMainThreadFunctions.Version;\n        if (func == null || !_pendingMainThreadFunctions.Accepting) return Task.FromResult(fallback);',1)
  after=after.replace('''        int state = 0;
        try''','''        int state = 0;
        bool Retire()
        {
            if (Interlocked.CompareExchange(ref state, 2, 0) != 0) return false;
            tcs.TrySetResult(fallback);
            return true;
        }
        IDisposable registration = _pendingMainThreadFunctions.Register(retirementVersion, () => Retire());
        if (registration == null) return tcs.Task;
        try''',1)
  after=after.replace('''        return AwaitNativeConversationMainThreadFuncAsync(tcs.Task, op, target, targetAgentIndex, () =>
        {
            if (Interlocked.CompareExchange(ref state, 2, 0) != 0) return false;
            tcs.TrySetResult(fallback);
            return true;
        });''','''        return AnimusForge.Refactor.Runtime.PendingOperationRegistry.AwaitRelease(
            AwaitNativeConversationMainThreadFuncAsync(tcs.Task, op, target, targetAgentIndex, Retire), registration);''',1)
  s=s.replace(before,after,1)
  for before,after in json.loads((HERE/'source-review.json').read_text(encoding='utf-8'))['nativeActionExactEdits']:
   assert s.count(before)==1;s=s.replace(before,after,1)
  pattern=r'(?m)^(\t+)(?:Action result;\n\1)?while \(_mainThreadActions.TryDequeue\(out (?:var _|result)\)\)\n\1\{\n\1\}'
  assert len(list(re.finditer(pattern,s)))==4;s=re.sub(pattern,lambda m:m.group(1)+'ResetPendingMainThreadFunctions();',s)
 elif path=='CourierDeliveryBehavior.cs':
  before='''\t\t\twhile (MainThreadActions.TryDequeue(out var _))
\t\t\t{
\t\t\t}''';assert s.count(before)==1;s=s.replace(before,'''\t\t\tif (Instance != null) Instance.ResetPendingOwnerPhases();
\t\t\telse while (MainThreadActions.TryDequeue(out var _)) { }''',1)
  packet=json.loads((HERE/'source-review.json').read_text(encoding='utf-8'))['courierCommitExtraction']
  moved=[]
  for sig,(before,after) in zip(packet['signatures'],packet['edits']):
   assert e.declaration(s,sig)==before;s=s.replace('\t'+before+'\n\n','',1);moved.append('\t'+after)
  assert restore_commit((ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.CommitDispatch.cs').read_text(encoding='utf-8-sig'))==packet['header']+'\n\n'.join(moved)+'\n}\n','Unreviewed Courier commit extraction'
 elif path=='src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DetachedPostprocess.cs':
  sig='private async Task<T> RunCourierOwnerPhaseAsync<T>(';before=e.declaration(s,sig);after=before.replace('cancellationToken.ThrowIfCancellationRequested();','long retirementVersion = _pendingOwnerPhases.Version;\n        cancellationToken.ThrowIfCancellationRequested();\n        if (!_pendingOwnerPhases.Accepting) throw new OperationCanceledException("Courier owner retired.");',1)
  after=after.replace('        bool mainThread = false;','''        using (IDisposable registration = _pendingOwnerPhases.Register(retirementVersion, () =>
        {
            if (Interlocked.CompareExchange(ref state, 2, 0) == 0) completion.TrySetCanceled();
        }))
        {
        bool mainThread = false;''',1);after=after[:-1]+'    }\n    }';s=s.replace(before,after,1)
 elif path=='SubModule.cs':
  s=s.replace('\t\tRemoveMapButtonLayer();\n\t\tbase.OnGameEnd(game);','\t\tRemoveMapButtonLayer();\n\t\tAfCampaignRuntimeLifecycle.End(game);\n\t\tbase.OnGameEnd(game);',1).replace('\t\tModuleFrameworkRuntime.Shutdown();','\t\tAfCampaignRuntimeLifecycle.Stop();\n\t\tModuleFrameworkRuntime.Shutdown();',1)
  sig='protected override void InitializeGameStarter(';before=e.declaration(s,sig)
  after='''protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
\t{
\t\tCampaignGameStarter campaignStarter = starterObject as CampaignGameStarter;
\t\tif (campaignStarter != null) AfCampaignRuntimeLifecycle.Begin(game);
\t\ttry
\t\t{
\t\t\tModuleFrameworkRuntime.RegisterCampaign(starterObject);
\t\t\tif (campaignStarter != null) AfCampaignRuntimeLifecycle.CaptureOwners(game, campaignStarter);
\t\t}
\t\tcatch
\t\t{
\t\t\tif (campaignStarter != null)
\t\t\t{
\t\t\t\ttry { AfCampaignRuntimeLifecycle.CaptureOwners(game, campaignStarter); }
\t\t\t\tcatch (Exception) { } // Preserve the original registration failure.
\t\t\t\ttry { AfCampaignRuntimeLifecycle.End(game); }
\t\t\t\tcatch (Exception) { }
\t\t\t}
\t\t\tthrow;
\t\t}
\t}''';s=s.replace(before,after,1)
 elif path=='MyBehavior.MemorySummaryMainThread.cs':
  s=s.replace('    private MemorySummaryDispatcher _memorySummaryDispatcher;', '    private MemorySummaryDispatcher _memorySummaryDispatcher;\n    private int _campaignRuntimeRetired;',1).replace('            ReferenceEquals(Instance, _owner) && SaveRuntimeGuard.IsCurrentGeneration(generation);','            Volatile.Read(ref _owner._campaignRuntimeRetired) == 0\n            && ReferenceEquals(Instance, _owner) && SaveRuntimeGuard.IsCurrentGeneration(generation);',1)
 return s

def check_dependencies():
 data=json.loads((HERE/'source-review.json').read_text(encoding='utf-8'))
 for p,h in data['dependencies'].items():
  source=(ROOT/MOVED_DEPENDENCIES.get(p,p)).read_text(encoding='utf-8-sig')
  if p=='src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.CommitDispatch.cs':source=restore_commit(source)
  if p in ('tools/GameLifetimeTests/run.py','tools/GameLifetimeTests/run_bindings.py','tools/GameLifetimeTests/run_commit.py','tools/GameLifetimeTests/run_memory.py'):
   for historical,current in RUNNER_PATH_EDITS.items():
    source=source.replace(current,historical)
  assert hashlib.sha256(source.encode()).hexdigest()==h,'Unreviewed game lifetime dependency: '+p

spec_owner=importlib.util.spec_from_file_location('j07b_admission_inverse',ROOT/'tools/NativeConversationAdmissionTests/owner_extraction.py');owner_inverse=importlib.util.module_from_spec(spec_owner);spec_owner.loader.exec_module(owner_inverse)

def restore(path,source):
 source=owner_inverse.restore(path,source)
 if path=='ShoutBehavior.cs':
  # J03 exact inverse: d11eb572 added five request scopes; 2aa4edb7 moved
  # mission warmup seed capture to this call site. No other source drift is allowed.
  warmup_new='''\t\tPromptSemanticWarmupSeedBatch semanticWarmupSeeds = AIConfigHandler.CaptureGuardrailSemanticWarmupSeeds();
\t\tRagWarmupCoordinator.TryStartBackgroundWarmup("mission_start", semanticWarmupSeeds);
\t\tAIConfigHandler.TryStartBackgroundSemanticWarmup("mission_start", semanticWarmupSeeds);'''
  warmup_old='''\t\tRagWarmupCoordinator.TryStartBackgroundWarmup("mission_start");
\t\tAIConfigHandler.TryStartBackgroundSemanticWarmup("mission_start");'''
  assert source.count(warmup_new)==1,'Unreviewed J03 warmup source';source=source.replace(warmup_new,warmup_old,1)
  source,count=re.subn(r'(?m)^\t+using IDisposable guardrailScopeJ03 = AIConfigHandler\.BeginGuardrailRuntimeScope\(\);\n','',source)
  assert count==5,'Unreviewed J03 Shout scope count'
  # J04 exact inverse: 2a191526 collapsed one six-setter publish and four six-setter clears
  # into PromptRuntimeTargetBinding apply/clear. Only these exact blocks may differ.
  j04_apply_new='\t\t\tAIConfigHandler.ApplyGuardrailRuntimeTarget(MyBehavior.CreatePromptRuntimeTargetBinding(targetKingdomId, targetHero, targetCharacter, targetAgentIndex));\n'
  # bd2582aa introduced this exact capture/publication pair. Qualification behavior
  # is covered by CaptureEligibility; this inverse permits no other call-site drift.
  j06_apply_new=('\t\t\tPromptRuntimeTargetBinding runtimeTargetBinding = MyBehavior.CreatePromptRuntimeTargetBinding(targetKingdomId, targetHero, targetCharacter, targetAgentIndex);\n'
   '\t\t\tAIConfigHandler.ApplyGuardrailRuntimeTarget(runtimeTargetBinding, MyBehavior.CapturePromptRuleEligibility(targetHero, targetCharacter, runtimeTargetBinding));\n')
  assert source.count(j06_apply_new)==1,'Unreviewed J06 Shout eligibility capture'
  source=source.replace(j06_apply_new,j04_apply_new,1)
  j04_apply_old=('\t\t\tAIConfigHandler.SetGuardrailRuntimeTargetKingdom(targetKingdomId);\n'
   '\t\t\tAIConfigHandler.SetGuardrailRuntimeTargetHero(targetHeroId);\n'
   '\t\t\tAIConfigHandler.SetGuardrailRuntimeTargetCharacter(targetCharacterId);\n'
   '\t\t\tAIConfigHandler.SetGuardrailRuntimeTargetTroop(targetCharacterId);\n'
   '\t\t\tAIConfigHandler.SetGuardrailRuntimeTargetUnnamedRank((targetHero == null && targetCharacter != null) ? (targetCharacter.IsSoldier ? "soldier" : "commoner") : "");\n'
   '\t\t\tAIConfigHandler.SetGuardrailRuntimeTargetAgentIndex(targetAgentIndex);\n')
  assert source.count(j04_apply_new)==1,'Unreviewed J04 Shout target apply';source=source.replace(j04_apply_new,j04_apply_old,1)
  def j04_clear(m):
   i=m.group(1);return ''.join(i+'AIConfigHandler.SetGuardrailRuntimeTarget'+x+'\n' for x in ('Kingdom("");','Hero("");','Character("");','Troop("");','UnnamedRank("");','AgentIndex(-1);'))
  source,count=re.subn(r'(?m)^(\t+)AIConfigHandler\.ClearGuardrailRuntimeTarget\(\);\n',j04_clear,source)
  assert count==4,'Unreviewed J04 Shout target clear count'
  j04_const_new='	public const string PersistentAdpDebtPostprocessRuleId = PromptPreprocessRuleIdAssembler.PersistentAdpDebtRuleId;'
  j04_const_old='	public const string PersistentAdpDebtPostprocessRuleId = "persistent_adp_debt";'
  assert source.count(j04_const_new)==1,'Unreviewed J04 Shout debt rule id';source=source.replace(j04_const_new,j04_const_old,1)
  # J04f exact inverse: the Native call site schedules the shared prompt build as three steps.
  j04f_new='\t\tMyBehavior.ShoutPromptContext ctx = await BuildNativePromptContextScheduledAsync(admission, nativeTargetLog, nativeTargetAgentIndex, runtimeGeneration, targetHero, targetCharacter, routingInput, extraFact, cultureId, npc.IsHero, preprocessExcludedRuleIds, weeklyPromptSnapshot).ConfigureAwait(false);\n'
  j04f_old=('\t\tTask<MyBehavior.ShoutPromptContext> nativePreprocessTask = RunNativeConversationBackgroundPreprocessAsync(\n'
   '\t\t\tnativeTargetLog,\n'
   '\t\t\tnativeTargetAgentIndex,\n'
   '\t\t\truntimeGeneration,\n'
   '\t\t\t() => MyBehavior.BuildShoutPromptContextForExternal(targetHero, routingInput, extraFact, cultureId, hasAnyHero: npc.IsHero, targetCharacter: targetCharacter, kingdomIdOverride: null, targetAgentIndex: nativeTargetAgentIndex, preprocessExcludedRuleIds: preprocessExcludedRuleIds, weeklyPromptSnapshot: weeklyPromptSnapshot));\n'
   '\t\tMyBehavior.ShoutPromptContext ctx = await AwaitNativeConversationBackgroundPreprocessAsync(nativePreprocessTask, nativeTargetLog, nativeTargetAgentIndex, runtimeGeneration).ConfigureAwait(false);\n')
  assert source.count(j04f_new)==1,'Unreviewed J04f Native prompt build call';source=source.replace(j04f_new,j04f_old,1)
 if path=='CourierDeliveryBehavior.cs':
  prompt_spec=importlib.util.spec_from_file_location('courier_prompt_inverse',ROOT/'tools/CourierPromptPreparationTests/source_review.py');prompt=importlib.util.module_from_spec(prompt_spec);prompt_spec.loader.exec_module(prompt)
  source=prompt.restore(source)
 if path=='SubModule.cs':
  spec=importlib.util.spec_from_file_location('j02_host_inverse',ROOT/'tools/HostCompositionTests/source_inverse.py');host=importlib.util.module_from_spec(spec);spec.loader.exec_module(host)
  source=host.restore_submodule(source)
 if path not in PATHS:return source
 check_dependencies();assert source==expected(path),'Unreviewed game lifetime source change: '+path
 return old(path)

def restore_method(path,sig,method):
 actual=(ROOT/path).read_text(encoding='utf-8-sig');restore(path,actual)
 assert method==e.declaration(actual,sig),'Unreviewed game lifetime method'
 return e.declaration(old(path),sig)
if __name__=='__main__':
 for p in PATHS:restore(p,(ROOT/p).read_text(encoding='utf-8-sig'));print('PASS lifecycle exact inverse '+p)

"""Reviewed exact inverse of the complete deferred postprocess owner, fixed Git source."""
import subprocess,importlib.util,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
PATH='src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ScenePostprocess.cs'
before=subprocess.check_output(['git','show','84f428cd:'+PATH],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
after=(ROOT/PATH).read_text(encoding='utf-8-sig').replace('\r\n','\n')
after=after.replace('using static AnimusForge.ShoutBehavior;\n','').replace('internal sealed partial class SceneConversationSessionRuntime','public partial class ShoutBehavior')
after=after.replace('_ports.SceneSessionId()','Volatile.Read(ref _sceneHistorySessionId)').replace('_dispatcher.RunAsync(','RunNativeConversationMainThreadFuncAsync(').replace('_ports.','')
for text in ['enum ScenePostprocessStatus','sealed class ScenePostprocessOutcome','Task<ScenePostprocessOutcome> QueueDeferredScenePostprocessActions','bool CommitDeferredSceneActionPlan']:
    assert after.count('internal '+text)==1
    after=after.replace('internal '+text,'private '+text,1)
# Exact reviewed bridge relocation; the original body oracle remains immutable.
bridge=(ROOT/'src/bridges/Diplomacy/DiplomacyConversationBridge.cs').read_text(encoding='utf-8-sig')
assert bridge.count('internal static bool CanUseIndependentClanPeaceForExternal(')==1
assert 'DiplomacyModuleServices.Conversation.CanUseIndependentClanPeace(' in bridge
assert after.count('DiplomacyConversationBridge.CanUseIndependentClanPeaceForExternal(')==1
after=after.replace('DiplomacyConversationBridge.CanUseIndependentClanPeaceForExternal(', 'DiplomacyBehavior.CanUseIndependentClanPeaceForExternal(',1)
assert after==before,'Unreviewed complete Scene deferred postprocess body migration'
print('PASS full deferred Scene postprocess byte inverse against fixed Git 84f428cd (all 633 lines; routing/state only)')
old_path='src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneActionDirective.cs'
old=subprocess.check_output(['git','show','84f428cd:'+old_path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
new=(ROOT/'src/AF.GameAdapter.Bannerlord/SceneActions/SceneActionDirectiveController.cs').read_text(encoding='utf-8-sig').replace('internal static class SceneActionDirectiveController','public partial class ShoutBehavior')
reset='\n    internal static void Reset() => SceneActionReplyCaptures.Clear();\n'
assert new.count(reset)==1
new=new.replace(reset,'',1)
assert new==old,'Unreviewed scene directive rule/codec/ledger migration'
print('PASS full SceneActionDirective byte inverse against fixed Git 84f428cd')
spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
main=subprocess.check_output(['git','show','84f428cd:src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
gate=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ScenePostprocessGate.cs').read_text(encoding='utf-8-sig')
gate=gate.replace('IsProcessingShout','_isProcessingShout').replace('_ports.SceneSessionId()','Volatile.Read(ref _sceneHistorySessionId)').replace('_ports.ProcessingSequence()','Interlocked.Read(ref _sceneShoutProcessingSequence)').replace('_ports.PostMainThread(','_mainThreadActions.Enqueue(')
for sig in ['void RegisterScenePostprocessGateTask(','Task GetScenePostprocessGateTask(','void ForceClearScenePostprocessGate(','async Task WaitForScenePostprocessGateAsync(']:
    old=ex.declaration(main,'private '+sig)
    new=ex.declaration(gate,'internal '+sig).replace('internal '+sig,'private '+sig,1)
    assert new==old,'Unreviewed gate method '+sig
print('PASS complete four Scene postprocess gate methods byte inverse against fixed Git 84f428cd')
context_path='src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ModuleSceneContext.cs'
old=subprocess.check_output(['git','show','84f428cd:'+context_path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
new=(ROOT/context_path).read_text(encoding='utf-8-sig')
projected=old.replace('public partial class ShoutBehavior','internal sealed partial class SceneConversationSessionRuntime')
projected=projected.replace('internal static string IssueModuleSceneTicket','internal string IssueModuleSceneTicket').replace('internal static bool TryClaimModuleSceneTicket','internal bool TryClaimModuleSceneTicket').replace('internal static void RevokeModuleSceneTickets','internal void RevokeModuleSceneTickets')
projected=projected.replace('        ShoutBehavior owner = CurrentInstance;\n','').replace('owner == null','!_ports.IsOwnerCurrent()').replace('owner != null','_ports.IsOwnerCurrent()').replace('owner.','').replace('CurrentInstance?._scenePlayerShoutRequestOwner','_scenePlayerShoutRequestOwner')
projected=projected.replace('ReferenceEquals(CurrentInstance, this)','_ports.IsOwnerCurrent()').replace('_isProcessingShout','IsProcessingShout').replace('_activeShoutTargetingContext','_ports.ActiveTargetingContext()').replace('Volatile.Read(ref _sceneHistorySessionId)','_ports.SceneSessionId()').replace('GetAgentsForShoutTargetingContext(','_ports.GetAgentsForShoutTargetingContext(')
projected=projected.replace('private bool IsModuleSceneTargetingSourceCurrent','internal bool IsModuleSceneTargetingSourceCurrent').replace('private static ShoutTargetingContext CloneModuleSceneTargetingContext','internal static ShoutTargetingContext CloneModuleSceneTargetingContext')
new=new.replace('using static AnimusForge.ShoutBehavior;\n','')
assert new==projected,'Unreviewed entire module scene context/ticket/source claim migration'
print('PASS full module Scene capture/ticket/claim source inverse, same authoritative runtime identity')
stages=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneExecutionStages.cs').read_text(encoding='utf-8-sig')
old=ex.declaration(main,'private static string TryRunCompactTownOrdinaryAmbientPostprocess(')
prefix=old[:old.index('\t\tif (!AIConfigHandler.TryCallAuxiliaryActionPostprocess(')]
prefix=prefix.replace('private static string TryRunCompactTownOrdinaryAmbientPostprocess(', 'private static SceneActionPostprocessWorkItem PrepareCompactTownOrdinaryAmbientPostprocess(').replace('return EnsureScenePostprocessFallbackMood(dialogue);','return new SceneActionPostprocessWorkItem(EnsureScenePostprocessFallbackMood(dialogue));')
normalize=old[old.index('\t\tstring normalized ='):old.rfind('\n\t}')]
expected=prefix+'\t\treturn new SceneActionPostprocessWorkItem(systemPrompt, userPrompt, dialogue, rawTags =>\n\t\t{\n'+normalize+'\n\t\t});\n\t}'
actual=ex.declaration(stages,'private static SceneActionPostprocessWorkItem PrepareCompactTownOrdinaryAmbientPostprocess(')
assert actual==expected,'Unreviewed compact ambient prompt/rule/normalization body'
chains=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneConversationChains.cs').read_text(encoding='utf-8-sig')
publisher=ex.declaration(chains,'private async Task<bool> TryPublishImmediateSceneReactionAsync(')
assert 'AIConfigHandler.TryCallAuxiliaryActionPostprocess(systemPrompt, userPrompt, 256, 0f,' in publisher
assert 'using IDisposable lease = lifetime.Enter();' in publisher and 'using IDisposable cancellation = LlmNonStreamingTransport.PushOwnerCancellation(lifetime.Token);' in publisher
assert publisher.index('CanPublishImmediateSceneReactionRequest(request)',publisher.index('var network = await')) < publisher.index('CompleteCompactTownOrdinaryAmbientPostprocess(work,') < publisher.index('TeamModuleServices.Siege.TryProcessActionTags(')
print('PASS complete compact ambient prompt/rule/normalization inverse; original 256/0f request and guarded game completion order')
chain_path='src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneConversationChains.cs'
old_chains=subprocess.check_output(['git','show','84f428cd:'+chain_path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
before=ex.declaration(old_chains,'private async Task HandleGroupResponsePerHeroIndependent(')
after=ex.declaration(chains,'internal async Task HandleGroupResponsePerHeroIndependent(').replace('internal async','private async',1)
after=after.replace('_ports.SceneSessionId()','Volatile.Read(ref _sceneHistorySessionId)').replace('_ports.PostMainThread(','_mainThreadActions.Enqueue(').replace('_ports.','').replace('_dispatcher.RunAsync(','RunNativeConversationMainThreadFuncAsync(')
# Ordinary and module groups now share the already-existing five-phase module prompt path;
# no rule/relay/bystander/effect/history business body is substituted here.
start=before.index('\t\t\t\t\tMyBehavior.ShoutPromptContext ctx = receipt == null')
end=before.index('\t\t\t\t\tpreprocessSw.Stop();',start)
block=before[start:end]
lines=block.splitlines(True)
replacement='\t\t\t\t\tMyBehavior.ShoutPromptContext ctx = '+lines[2].lstrip().replace(': ','',1)+''.join(lines[3:])
replacement=replacement.replace('if (receipt != null && ctx == null)','if (ctx == null)').replace('receipt.Fail("scene.prompt_unconfirmed")','receipt?.Fail("scene.prompt_unconfirmed")')
projected=before[:start]+replacement+before[end:]
start=projected.index('\t\t\t\t\tList<string> historyLines = null;')
end=projected.index('\t\t\t\t\tscenePublicHistorySection =',start)
projected=projected[:start]+'\t\t\t\t\tList<string> historyLines = CaptureVisibleSceneHistoryLines(currentSpeaker.AgentIndex, GetSceneNpcHistoryNameForPrompt(currentSpeaker), multiNpcScene);\n\n\n'+projected[end:]
for edge in ['CanUseIndependentClanPeaceForExternal','CanUseDiplomacyActionPostprocessForExternal']:
    assert bridge.count('internal static bool '+edge+'(')==1
    assert after.count('DiplomacyConversationBridge.'+edge+'(')==1
    after=after.replace('DiplomacyConversationBridge.'+edge+'(', 'DiplomacyBehavior.'+edge+'(',1)
assert after==projected,'Unreviewed entire primary-first/relay/bystander/group effect/history execution body'
print('PASS full group execution source inverse ('+str(len(before.splitlines()))+' lines), only shared five-phase scheduling + scalar history capture changed')
def tokens(value):
    return re.findall(r'"(?:[^"\\]|\\.)*"|[A-Za-z_]\w*|[0-9]+|[^\s]',value)
for name in ['GenerateGroupConversationTurnLine','GetPassiveNpcResponse']:
    oldsig='private async Task<string> '+name+'Async(' if name.startswith('Generate') else 'private async Task<string> '+name+'('
    newsig='private async Task<string> '+name+'CoreAsync('
    expected=ex.declaration(old_chains,oldsig).replace(oldsig,newsig,1)
    actual=ex.declaration(chains,newsig).replace('_ports.SceneSessionId()','Volatile.Read(ref _sceneHistorySessionId)').replace('_ports.PostMainThread(','_mainThreadActions.Enqueue(').replace('_ports.','')
    capture='\n        long promptGeneration = SaveRuntimeGuard.CaptureGeneration();\n        int promptSession = Volatile.Read(ref _sceneHistorySessionId);\n        int promptEpoch = Volatile.Read(ref _sceneConversationEpoch);'
    expected=expected.replace('\n\t{','\n\t{'+capture,1)
    line=next(x for x in expected.splitlines() if 'MyBehavior.ShoutPromptContext ctx = MyBehavior.BuildShoutPromptContextForExternal(' in x)
    if name.startswith('Generate'):
        assert expected.count('if (isCurrent != null && !isCurrent()) return "";')==3
        expected=expected.replace('if (isCurrent != null && !isCurrent()) return "";','if (!IsSceneRequestSourceCurrent(promptGeneration, promptSession, promptEpoch) || (isCurrent != null && !isCurrent())) return "";')
        call='MyBehavior.ShoutPromptContext ctx = await BuildModuleScenePromptContextAsync(hero, characterObject, playerText, fullExtra, speakerNpc.CultureId ?? "neutral", kingdomIdOverride, speakerNpc.AgentIndex, speakerNpc.IsHero, hasPrecomputed && precomputed != null && precomputed.HasLoreContext, precomputed?.LoreContext, preprocessExcludedRuleIds, promptGeneration, promptSession, promptEpoch);'
    else:
        call='MyBehavior.ShoutPromptContext ctx = await BuildModuleScenePromptContextAsync(hero, passiveCharacter, inputActionText, fullExtra, cultureId, passiveKingdomIdOverride, data.AgentIndex, data.IsHero, !string.IsNullOrWhiteSpace(loreContext), loreContext, preprocessExcludedRuleIds, promptGeneration, promptSession, promptEpoch);'
        start=expected.index('List<string> historyLines = null;')
        end=expected.index('if (historyDump != null)',start)
        block=expected[start:end]
        branch=block[block.index('if (historyLines != null && historyLines.Count > 0)'):]
        # Only the two old container-lock/public-count braces are replaced; the actual dump loop remains.
        close='\t\t\t\t\t}\n\t\t\t\t}\n\t\t\t\t'
        assert branch.endswith(close)
        branch=branch[:-len(close)]
        expected=expected[:start]+'List<string> historyLines = CaptureVisibleSceneHistoryLines(data.AgentIndex, npcName, passiveMultiNpcScene);\n'+branch+'\n'+expected[end:]
        marker='string output = await LegacyShoutNetworkGateway.SendLegacyMessagesAsync(messages, 5000, promptRetryOnError: true);'
        assert expected.count(marker)==1
        expected=expected.replace(marker,marker+'\nif (!IsSceneRequestSourceCurrent(promptGeneration, promptSession, promptEpoch)) return "";',1)
    expected=expected.replace(line,call+'\nif (!IsSceneRequestSourceCurrent(promptGeneration, promptSession, promptEpoch) || ctx == null) return "";',1)
    assert tokens(expected)==tokens(actual),'Unreviewed complete Scene '+name+' rule/history/prompt body'
    print('PASS complete '+name+' source inverse: same business tokens, explicit shared scheduling/scope guards/scalar capture only')
entry=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ScenePlayerExecution.cs').read_text(encoding='utf-8-sig')
canonical=entry.replace('_ports.SceneSessionId()','Volatile.Read(ref _sceneHistorySessionId)').replace('_ports.IsOwnerCurrent()','ReferenceEquals(CurrentInstance, this)').replace('_ports.ActiveTargetingContext()','_activeShoutTargetingContext').replace('_ports.PostMainThread(','_mainThreadActions.Enqueue(').replace('_ports.','').replace('IsProcessingShout','_isProcessingShout').replace('_dispatcher.RunAsync(','RunNativeConversationMainThreadFuncAsync(')
for sig in ['ScenePlayerShoutRequest CaptureScenePlayerShoutRequest(', 'bool IsScenePlayerShoutRequestCurrent(']:
    old=ex.declaration(main,'private '+sig)
    new=ex.declaration(canonical,'internal '+sig).replace('internal '+sig,'private '+sig,1)
    assert new==old,'Unreviewed player snapshot identity/frozen range/current claim '+sig
print('PASS both complete Scene player request capture/current methods byte inverse')
sig='async Task ProcessCapturedScenePlayerShoutAsync('
expected=ex.declaration(main,'private '+sig)
old='NotePresentationRoundGroup(groupTask);'
assert expected.count(old)==1
expected=expected.replace(old,'await RunNativeConversationMainThreadFuncAsync("scene_group_note", "player", request.TargetingContext.PrimaryAgentIndex, () => { NotePresentationRoundGroup(groupTask); return true; }, false);',1)
actual=ex.declaration(canonical,'internal '+sig).replace('internal '+sig,'private '+sig,1)
assert actual==expected,'Unreviewed complete captured player gate/dispatch/group awaiting method'
sig='Task ProcessCurrentScenePlayerShout('
expected=ex.declaration(main,'private '+sig)
composition=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneSessionComposition.cs').read_text(encoding='utf-8-sig')
start=expected.index('\t\t_stareTimer = 0f;');end=expected.index('\t\tShoutTargetingContext targetingContext',start)
old=expected[start:end]
atom=next(line for line in composition.splitlines() if 'PreparePlayerRound = text =>' in line).split('=> {',1)[1].rsplit('},',1)[0]
assert tokens(re.sub(r'\btext\b','shoutText',atom))==tokens(old),'PreparePlayerRound changed original stare/literal qualification atom'
expected=expected[:start]+'PreparePlayerRound(shoutText);\n'+expected[end:]
start=expected.index('\t\t_pendingHeroHistoryExtraFactAfterSceneReply =');end=expected.index('\t\tif (!string.IsNullOrWhiteSpace(sharedExtraFact))',start)
old=expected[start:end]
atom=next(line for line in composition.splitlines() if 'ClearPendingHeroFacts = () =>' in line).split('=> {',1)[1].rsplit('},',1)[0]
assert tokens(atom)==tokens(old),'ClearPendingHeroFacts changed original three state assignments'
expected=expected[:start]+'ClearPendingHeroFacts();\n'+expected[end:]
start=expected.index('\t\tif (_floatingTextView != null && Agent.Main != null)');end=expected.index('\t\tList<NpcDataPacket> capturedNpcData',start)
old=expected[start:end]
atom=next(line for line in composition.splitlines() if 'ShowPlayerSpeech = text =>' in line)
assert 'if (_floatingTextView != null && Agent.Main != null) _floatingTextView.AddOrUpdateText(Agent.Main, text);' in atom
assert tokens(old)==tokens('if (_floatingTextView != null && Agent.Main != null) { _floatingTextView.AddOrUpdateText(Agent.Main, shoutText); }')
expected=expected[:start]+'ShowPlayerSpeech(shoutText);\n'+expected[end:]
old='return receipt == null ? Task.Run(RunGroupAsync) : RunSceneGroupOnMainThreadAsync(RunGroupAsync);'
assert expected.count(old)==1
expected=expected.replace(old,'return RunSceneGroupOnMainThreadAsync(RunGroupAsync);',1)
actual=ex.declaration(canonical,'internal '+sig).replace('internal '+sig,'private '+sig,1)
assert tokens(actual)==tokens(expected),'Unreviewed complete player group entry/frame/audience/fact/input business body'
print('PASS complete two Scene player execution entry bodies and every moved UI atom inverse; one game-thread group entry')
background=ex.declaration((ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneConversationChains.cs').read_text(encoding='utf-8-sig'),'internal void StartImmediateSceneReactionBackgroundRequest(')
queue_failure=background.split('catch (Exception ex2)',1)[1].split('\n\t\t\t});',1)[0]
assert 'FinishImmediateSceneReactionGeneration(' not in queue_failure
assert 'SettleImmediateSceneReaction(' not in queue_failure
assert 'Task.Run(' not in queue_failure
assert '_ports.PostMainThread(delegate' in background
print('PASS completion queue admission failure retains sole pending request for main-thread retirement; no worker effect/callback or retry')

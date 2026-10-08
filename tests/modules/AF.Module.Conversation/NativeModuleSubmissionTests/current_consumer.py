"""Current production Native request/turn consumer; no historical_source projection.
This runner is intentionally separate from the legacy inverse oracle.
"""
import importlib.util,json,subprocess,sys,hashlib,os
from pathlib import Path
from xml.sax.saxutils import escape
ROOT=Path(__file__).resolve().parents[4]; HERE=Path(__file__).parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment,current_source_path

def verify(run_root, dotnet=None, api_line=None):
    if api_line is None:
        output=new_run_root(ROOT,"native-module-current-both",run_root)
        results=[verify(output/("api-"+api),dotnet,api) for api in ("1.3","1.4")]
        (output/"result.json").write_text(json.dumps({"sourceClass":"current-whole-Native-runtime","apis":dict(zip(("1.3","1.4"),results))},indent=2),encoding="utf-8")
        return max(results)
    out=new_run_root(ROOT,'native-module-current',run_root)
    dotnet=Path(dotnet) if dotnet else resolve_dotnet(ROOT)
    spec=importlib.util.spec_from_file_location('current_extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
    ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
    def resolve(name):
        path=current_source_path(ROOT,name)
        if path.is_file(): return path
        matches=list((ROOT/'src/modules/AF.Module.Conversation').rglob(Path(name).name))
        assert len(matches)==1,(name,matches)
        return matches[0]
    source_texts={};extractions=[]
    def read(name):
        path=resolve(name);value=path.read_text(encoding='utf-8-sig')
        source_texts[hashlib.sha256(value.encode('utf-8')).hexdigest()]={'path':path.relative_to(ROOT).as_posix(),'rawSha256':hashlib.sha256(path.read_bytes()).hexdigest()}
        return value
    def declaration(text, signature):
        start=text.index(signature); brace=text.find('{',start); arrow=text.find('=>',start)
        if arrow>=0 and (brace<0 or arrow<brace):
            body=text[start:text.index(';',arrow)+1]
        else: body=ex.declaration(text,signature)
        provenance=source_texts.get(hashlib.sha256(text.encode('utf-8')).hexdigest())
        if provenance: extractions.append(dict(provenance,symbolSignature=signature,bodySha256=hashlib.sha256(body.encode('utf-8')).hexdigest(),sourceClass='current-production-exact-declaration'))
        return body
    def declarations(name, signatures): return '\n'.join(declaration(read(name),signature) for signature in signatures)
    s=read('ShoutBehavior.cs')
    host=(HERE/'CurrentConsumer.cs.txt').read_text(encoding='utf-8')
    host=host.replace(declaration(host,'internal sealed class NpcDataPacket'),'')
    host=host.replace('@@CURRENT_TURN_ENTRY@@',declaration(s,'private Task<string> SubmitNativeConversationTextInternalAsync('))
    import re
    assignment=re.search(r'NativeAdmissions = new NativeAdmissionApplicationAdapter\([^;]+;',s).group(0)
    host=host.replace('@@CURRENT_COMPOSITION@@','public ShoutBehavior() {'+assignment+'MainThreadActionDrain=new ConversationMainThreadActionDrain(_mainThreadActions);NativeDetachedPostprocesses=CreateFixtureDetached();}\nprivate ConversationGameThreadDispatcher _conversationGameThreadDispatcher => _fixtureDispatcher ??= new ConversationGameThreadDispatcher(_pendingMainThreadFunctions,_mainThreadActions.Enqueue,IsBannerlordMainThreadForNativeActions);\nprivate ConversationGameThreadDispatcher _fixtureDispatcher;')

    host=host.replace('@@CURRENT_DISPATCH@@',declarations('ShoutBehavior.cs',['private Task<T> RunNativeConversationMainThreadFuncAsync<T>(']))
    # Scene/Courier are unsupported fixture leaves in this Native-only consumer.
    host=host.replace('@@CURRENT_SCENE@@',"private sealed class SceneGroupReceipt { internal void Bind(int value) {} internal void AddTurn(int index,string name,string text,Task<bool> complete) {} }\nprivate void RetireModuleSceneGroup(string reason) {}\ninternal static void SubmitModuleSceneDialogue(CoreDialogueOperation operation) => operation.Finish(\"scene.owner_unavailable\");")
    dto_specs=[('ShoutBehavior.NativePreparation.cs','internal sealed class NativeConversationPreparationSnapshot'),('ShoutBehavior.cs','internal sealed class NativeConversationGameActionResult')]
    dtos='\n'.join(declarations(name,[sig]).replace('private sealed class','internal sealed class') for name,sig in dto_specs)
    host=host.replace('@@CURRENT_DATA@@',dtos)
    host=host.replace('@@CURRENT_PREPARATION_ENTRY@@',declarations('ShoutBehavior.NativePreparation.cs',['private NativeConversationPreparationSnapshot CaptureNativeConversationPreparation(']))
    my=read('src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs')
    for name in ['BeginSharedPromptBuild','CompleteSharedPromptBuild']:
        marker='@@CURRENT_SHARED_'+('BEGIN' if name=='BeginSharedPromptBuild' else 'COMPLETE')+'@@'
        match=re.search(r'internal [^;\n{}]+ '+name+r'\(',my);assert match,name
        host=host.replace(marker,declaration(my,match.group(0)))
    host=host.replace('@@CURRENT_SHARED_ROUTING@@',declaration(read('src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.SharedPromptRoutingCapture.cs'),'internal SharedPromptRoutingWork CaptureSharedPromptRoutingWork('))
    host+='\nnamespace AnimusForge { internal sealed partial class MyBehavior { private readonly BuiltInRuleStickyCarry _builtInRuleStickyCarry=new(); '+declaration(my,'private PromptRoutingPorts CreatePromptRoutingPorts(')+declaration(my,'private static PromptTopicSemanticEvaluator CreateBuiltInTopicSemanticEvaluator(')+' } }'
    agent=read('src/AF.GameAdapter.Bannerlord/Prompt/SceneAgentIdentityPromptCaptureAdapter.cs')
    for sig in ['internal NpcDataPacket BuildNativeConversationNpcData(','internal static string GetSceneNpcHistoryNameForPrompt(']:
        marker='@@CURRENT_BUILD_NATIVE_NPC@@' if sig.startswith('internal NpcDataPacket') else '@@CURRENT_NATIVE_HISTORY_NAME@@'
        host=host.replace(marker,declaration(agent,sig))
    host+='\nnamespace AnimusForge.Refactor.Adapters { internal sealed partial class SceneAgentIdentityPromptCaptureAdapter { '+declaration(agent,'internal static float ResolveNativeConversationNonHeroAge(')+' } }'
    host+='\nnamespace AnimusForge { internal sealed partial class ConversationActionBoundaryBannerlordAdapter { '+declarations('src/AF.GameAdapter.Bannerlord/Conversation/ConversationActionBoundaryBannerlordAdapter.cs',['internal static bool TryResolveNativeConversationMeetingTauntParty('])+' } }'
    identity=read('src/AF.GameAdapter.Bannerlord/Prompt/PersonaIdentityPromptCaptureAdapter.cs')
    old=declaration(host,'internal static int ResolvePlayerClanTierForPrompt(')
    host=host.replace(old,declaration(identity,'internal static int ResolvePlayerClanTierForPrompt('))
    old=declaration(host,'internal static partial class MemoryRecallInputCaptureAdapter')
    recall=read('src/AF.GameAdapter.Bannerlord/Prompt/MemoryRecallInputCaptureAdapter.cs')
    host=host.replace(old,'internal static partial class MemoryRecallInputCaptureAdapter { '+ '\n'.join(declaration(recall,sig) for sig in ['internal static bool IsActiveSceneSessionHistoryLine(','internal static bool IsLoreInjectionHistoryLine(','internal static bool IsPlayerTurnStartLine(','internal static bool TryStripPlayerSpeechPrefix('])+' }')
    host+='\nnamespace AnimusForge { internal static class MemoryBusinessStateOwner { '+declarations('src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.cs',['internal static bool IsLoreInjectionHistoryLine(','internal static bool IsPlayerTurnStartLine('])+' } }'
    memory=read('src/AF.GameAdapter.Bannerlord/Memory/MemoryHistoryCommitBannerlordAdapter.cs')
    names=['LoadDialogueHistory','LoadDialogueHistoryById','SaveDialogueHistoryById','RemoveExpiredSingleUseNpcFactLines','GetLatestNpcDialogueUtterance','GetLatestSceneNpcDialogueUtteranceFallback','IsActiveSceneSessionHistoryLine','IsSingleUseNpcFactLine','IsFirstMeetingNpcFactBody','CountDialogueHistoryLines','CountNonHeroDialogueHistoryOwners','CountNonHeroDailyDraftOwners','BuildNonHeroMemorySampleIds','StripSpeakerPrefixForRecall']
    read_methods=[]
    for name in names:
        match=re.search(r'internal (?:static )?[^;\n{}]+ '+name+r'\(',memory);assert match,name
        read_methods.append(declaration(memory,match.group(0)))
    for sig in ['internal static void LogNonHeroMemoryTrace(string','internal static void LogNonHeroMemoryTrace(Func<string>']:
        read_methods.append(declaration(memory,sig))
    history_property=declaration(memory,'private Dictionary<string,List<DialogueDay>> _dialogueHistory')
    host+='\nnamespace AnimusForge { internal sealed class MemoryHistoryCommitBannerlordAdapter { private readonly MemoryBusinessStateOwner _memory; private Dictionary<string,List<DailyMemoryDraft>> _dailyMemoryDrafts=>_memory.Drafts; private static string NormalizeMemoryHeroId(string id)=>MemoryRecordRules.NormalizeMemoryHeroId(id); internal MemoryHistoryCommitBannerlordAdapter(MemoryBusinessStateOwner memory){_memory=memory;} '+ history_property+'\n'+ '\n'.join(read_methods).replace('DialogueDay','MyBehavior.DialogueDay')+' } }'
    host=host.replace(history_property,history_property.replace('DialogueDay','MyBehavior.DialogueDay'))
    state=read('src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.cs')
    sigs=['internal static bool IsNonHeroMemoryId(','internal static bool IsSceneShoutObserverHistoryLine(','internal static bool IsMeaningfulDirectConversationLine(','internal static bool IsMeaningfulConversationLine(']
    methods='\n'.join(declaration(state,sig) for sig in sigs)
    host=host.replace('internal static class MemoryBusinessStateOwner {','internal sealed partial class MemoryBusinessStateOwner {',1)
    fields=[]
    for path in ['src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.IdentityStores.cs','src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.cs']:
        fields += [line for line in read(path).splitlines() if re.match(r'^\s*internal Dictionary<.*> (?:History|Drafts) =',line)]
    host+='\nnamespace AnimusForge { internal sealed partial class MemoryBusinessStateOwner { internal const string NonHeroMemoryIdPrefix="af_nonhero:"; '+ '\n'.join(fields)+methods+' } internal static class HistoryArchiveRecallOwner { '+declarations('src/modules/AF.Module.Memory/Recall/HistoryArchiveRecallOwner.cs',['internal static bool IsSystemFactLine('])+' } }'
    recovery=read('src/modules/AF.Module.Memory/Summary/MemoryRecoveryStateOwner.cs')
    host+='\nnamespace AnimusForge { internal static class MemoryRecoveryStateOwner { '+next(line for line in recovery.splitlines() if 'const int MaximumPersistedMemoryCommitMarkers=' in line)+'\n'+ '\n'.join(declaration(recovery,sig) for sig in ['internal static void CopyMemoryCommitMarkers(','internal static Dictionary<string, string> SanitizeMemoryCommitMarkers(','internal static bool TryParseMemoryCommitMarkerKey(','internal static bool IsValidMemoryCommitMarker(','internal static string BuildMemoryCommitMarkerKey(','internal static bool IsMemoryRecoveryHexDigest('])+' } }'
    host+='\nnamespace AnimusForge {public partial class ShoutBehavior { '+declaration(s,'public static string GetLatestNativeConversationNpcUtteranceForExternal(')+declaration(s,'public static string GetLatestSceneNpcUtteranceForExternal(')+declaration(s,'private string GetLatestSceneNpcUtterance(')+' } }'
    config=read('src/modules/AF.Module.Prompt/Configuration/AIConfigHandler.cs')
    host+='\nnamespace AnimusForge { internal static partial class AIConfigHandler { '+declaration(config,'internal static PromptRuleEligibility CapturePromptRuleEligibility(')+declaration(config,'internal static string FormatMatchedExtraRuleInstructions(')+declaration(config,'public static string BuildMatchedExtraRuleInstructions(string input, string secondaryInput, int maxRules, bool hasAnyHero, IEnumerable<string> excludedRuleIds)')+' } }'

    host+='\nnamespace AnimusForge { internal sealed partial class MyBehavior { '+declaration(read('src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs'),'internal class DialogueDay')+' } }'

    pure_helpers=['BuildSceneCompositeUserBlock','SplitPersistedHeroHistorySections','StripNpcNamePrefixSafely','StripLeakedPromptContentForShout','StripScenePersonaBlocks','ExtractTrustPromptBlock','SplitSceneExtraSections','BuildSceneSystemRuleBlock','TrimPrivateRecentWindowForActionPostprocess','FilterHistorySectionAgainstScenePublicHistory','HasPreprocessRuleHit','HasInjectedRuleBlockForPostprocess','HasPartyTransferRuleContext','AppendPostprocessContextBlockForScene','BuildSceneSingleNpcTaskSystemBlock','BuildScenePublicHistorySection','RemoveNativeMessagesAlreadyInPersistentMemory','BuildNativeConversationPostprocessChainName','ResolveNativeConversationPostprocessChainName','StripStageDirectionsForPassiveShout','IsSceneWeeklyFullReportHeader','FormatSceneRuleSection','FormatSceneKnowledgeSection','InjectSceneMechanismPromptSection','IsActionPostprocessPlayerTurnLine','IsPrivateRecentWindowHeader','BuildSingleNpcSceneReplyInstruction','BuildReplyLengthInstruction','IsLeakedPromptLineForShout','KeepAfefFactsAndRecentHistoryLines','ContainsVassalageActionTagForLog','ContainsKingdomAnnexActionTagForLog','MayContainGeneratedRpItemReward','BuildNpcInitiatedOpeningUserText','BuildNpcInitiatedOpeningPersistentFactText','BuildFallbackSceneTauntSpeech','ResolveCourierRuntimeTargetKingdomId','StripAfefPrefixForPromptSection','GetSceneReplyLengthLimits','CreateEmptyNativeConversationPromptContext','BuildNativeConversationPreprocessUnavailableText','CaptureNativeConversationPersistedHistoryWork','BuildNativeConversationNpcListBlockForPrompt','BuildSceneMechanismPromptSection','AppendPlayerCustomPromptRuleToSystemPrompt','StripAfefPromptScopeLabel','FilterScenePresentNpcsForPrompt','BuildSceneNpcListLineForPrompt','BuildSceneNonHeroNamingNoteForPrompt','AppendSceneUnifiedTargetPromptSection','JoinPromptSections','BuildPlayerCustomPromptRuleBlock','GetSceneNpcIdentityNameForPrompt','GetSceneNpcGivenNameForPrompt','GetSceneNpcListIdentityForPrompt','IsSameSceneNpcForPrompt','NormalizeSceneNpcMatchValue']
    helpers=[]
    import re
    for name in pure_helpers:
        match=re.search(r'(?:private|internal|public) static [^;\n{}]+? '+re.escape(name)+r'\(',s)
        if match:
            body=declaration(s,match.group(0))
            helpers.append(body.replace('private static','internal static',1))
    helpers.append(declarations('ShoutBehavior.PersonaPreparation.cs',['private Task<bool> EnsureNativeConversationPersonaReadyAsync(']))
    helpers.append(declarations('ShoutBehavior.SceneHistoryMessages.cs',['private List<object> BuildStrictSceneMessagesForNpc(','private static List<string> KeepAfefFactsAndRecentHistoryLines(']).replace('private static','internal static'))
    host=host.replace('@@CURRENT_PURE_HELPERS@@','\n'.join(helpers))

    utils=read('src/modules/AF.Module.Conversation/Channels/Scene/ShoutUtils.cs')
    host+='\nnamespace AnimusForge { internal static partial class ShoutUtils { internal static NpcDataPacket ExtractNpcData(Agent a)=>a==null?null:new(){AgentIndex=a.Index,Name=a.Name};internal static void EnsurePromptNameFields(NpcDataPacket n) {} '+ '\n'.join(declaration(utils,sig) for sig in ['public static bool TrySplitNamePrefixedLineSafely(', 'public static string StripNamePrefixedLineSafely(', 'public static string StripConversationMetadataPrefix(','public static string GetPromptIdentityName(','public static string GetPromptListName(','public static string GetPromptHistoryName('])+' } }'
    snapshots=(ROOT/'src/AF.GameAdapter.Bannerlord/Composition/LegacyInteractionSnapshotAdapters.cs').read_text(encoding='utf-8-sig')
    snapshot_methods=['private static InteractionEnvelope CreateEnvelope(','internal static InteractionEnvelope CaptureActionCommit(','private static IReadOnlyList<PromptMessage> CopyHistory(','private static IReadOnlyList<string> ExtractVisibleHeroIds(','private static string FirstNonEmpty(']
    host+='\nnamespace AnimusForge.Refactor.Adapters { internal static class LegacyInteractionSnapshotAdapters { private static long _sessionSequence; private static readonly string ProcessTraceNonce=Guid.NewGuid().ToString("N"); private static int CurrentDay()=>1; private static string CurrentLocationId()=>"test-town"; '+ '\n'.join(declaration(snapshots,sig) for sig in snapshot_methods)+' } }'
    mentions=declaration((ROOT/'src/modules/AF.Module.Knowledge/Entities/WorldEntityRetrievalService.cs').read_text(encoding='utf-8-sig'),'public sealed class MentionedWorldEntities')
    host+='\nnamespace AnimusForge { '+mentions+' }\n'
    # Reuse existing deterministic domain/config leaf models; do not reuse its mocked AF helpers.
    parity=(ROOT/'tests/modules/AF.Module.Conversation/ScenePostprocessParityTests/Harness.cs.txt').read_text(encoding='utf-8')
    leaf_names=['Fixture','F','VassalageDiagnosticLog','TownAfRuleRoutingPolicy','AIConfigHandler','AfGcczShoutBridge','KingdomAgendaCustomPolicyBehavior','DiplomacyBehavior','VassalageBehavior','LordEncounterBehavior','VanillaIssueOfferBridge','NoblePrisonerExecutionOrderBehavior','VoteDealBehavior','WorldMapPartyCommandBehavior','NobleGatheringBehavior','SexualConceptionBehavior','RomanceSystemBehavior','RewardSystemBehavior','MyBehavior','PromptListRetrievalService','NpcRulerPolicyBehavior']
    leafs=[]
    for name in leaf_names:
        match=re.search(r'public (?:static |sealed )?class '+name+r'\b',parity);assert match,name
        block=declaration(parity,match.group(0))
        block=block.replace('public static class','internal static partial class',1).replace('public sealed class','internal sealed partial class',1)
        if name=='MyBehavior':block=block.replace('internal static partial class','internal sealed partial class',1)
        if name=='RewardSystemBehavior':
            block=re.sub(r'    public sealed class DuelStakeOption[^\n]+\n','',block)
            block=re.sub(r'    public static RewardSystemBehavior Instance[^\n]+\n','',block)
        if name=='RomanceSystemBehavior':block=re.sub(r'    public static RomanceSystemBehavior Instance[^\n]+\n','',block)
        if name=='WorldMapPartyCommandBehavior':block=re.sub(r'    public static string BuildCurrentNpcCommandTasksPromptForExternal[^\n]+\n','',block)
        if name=='AIConfigHandler':
            block=re.sub(r'    public static (?:bool ShouldExcludeSceneMoveRuleForCurrentMission|MentionedWorldEntities GetLatestAuxiliaryMentionedEntitiesForExternal)[^\n]+\n','',block)
        leafs.append(block)
    host+='\nnamespace AnimusForge { '+'\n'.join(leafs)+' }\n'
    for name in ['PartyTransferPromptEntry','SettlementTransferPromptEntry']:
        actual=declaration((ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs').read_text(encoding='utf-8-sig'),'public sealed class '+name)
        actual=actual.replace('{','{public string Id;',1)
        old=declaration(host,'public class '+name)
        host=host.replace(old,actual)
    for name in ['RewardItemInfo','DuelStakeOption']:
        actual=declaration((ROOT/'src/modules/AF.Module.Economy/Host/RewardSystemBehavior.cs').read_text(encoding='utf-8-sig'),'public class '+name)
        actual=actual.replace('{','{public string Id;',1)
        match=re.search(r'(?:internal|public) (?:sealed )?class '+name+r'\b',host)
        host=host.replace(declaration(host,match.group(0)),actual)
    host+='\nnamespace AnimusForge { sealed partial class MyBehavior {public enum SettlementTransferAssetKind {Settlement,Workshop,Caravan}} }'
    host=host.replace('@@CURRENT_FILL_IDENTITY@@',declarations('src/AF.GameAdapter.Bannerlord/Prompt/SceneAgentIdentityPromptCaptureAdapter.cs',['internal static void FillSceneMessageHeroIdentity(']))
    persona=read('src/AF.GameAdapter.Bannerlord/Prompt/ScenePersonaPreparationAdapter.cs')
    persona_fields=persona[persona.index('    private readonly ConversationGameThreadDispatcher'):persona.index('    internal ScenePersonaPreparationAdapter(')]
    host+='\nnamespace AnimusForge.Refactor.Adapters { internal sealed class ScenePersonaPreparationAdapter { '+persona_fields+declaration(persona,'internal ScenePersonaPreparationAdapter(')+declaration(persona,'internal async Task<bool> EnsureNativeConversationPersonaReadyAsync(')+' } }'
    capture_signatures=['internal SceneHistoryPromptCaptureAdapter(', 'internal static ConversationMessage StampConversationMessageWithCurrentMemoryContext(', 'internal static string CaptureNativeConversationNonHeroUnnamedKey(', 'internal static string CaptureNativeConversationHistoryKey(', 'internal void RemoveNativeConversationSessionHistoryEventFromSceneHistoryCaptured(', 'internal void AppendNativeConversationSessionLineToSceneHistoryCaptured(', 'internal static void AppendNativeConversationSessionHistoryCaptured(', 'internal static List<AnimusForgeDialogueHistoryEntry> GetNativeConversationSessionHistorySnapshot(', 'internal static List<ConversationMessage> BuildNativeConversationSessionHistoryMessages(', 'internal List<ConversationMessage> ConsumePendingCurrentNativeAfefFactMessagesForPrompt(','internal sealed class PersistedHistoryCapturePorts','internal sealed class NativePreparationPorts','internal static NativeConversationPreparationSnapshot CaptureNativeConversationPreparation(','internal static bool HasNativeConversationSessionHistory(']
    capture=declarations('src/AF.GameAdapter.Bannerlord/Prompt/SceneHistoryPromptCaptureAdapter.cs',capture_signatures)
    host+='\nnamespace AnimusForge.Refactor.Adapters { internal sealed class SceneHistoryPromptCaptureAdapter { private readonly PersistedHistoryCapturePorts _persisted; private readonly Func<SceneConversationHistoryOwner> _history; private readonly ScenePendingAfefFactsOwner _pendingFacts;private readonly NativeConversationSessionOwner _nativeSessions; private readonly Action<List<NpcDataPacket>> _firstMeeting,_revisit; private readonly Func<bool,bool> _flushDeferred; private readonly Func<SceneHistoryPromptCaptureAdapter> _ownCapture; '+capture+' } }'
    host+='\nnamespace AnimusForge {internal sealed partial class MyBehavior { '+declaration(read('src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs'),'internal sealed class CourierPreprocessRequest')+' } }'
    host+='\nnamespace AnimusForge { public partial class ShoutBehavior { '+ '\n'.join(declaration(s,x) for x in ['private ConversationActionBoundaryBannerlordAdapter ActionBoundary =>','private ConversationActionExecutorComposition ActionExecutors =>','public static Task<LegacyNativeConversationOptInResult> SubmitNativeConversationRefactorOptInForExternalAsync(','private Task<LegacyNativeConversationOptInResult> SubmitNativeConversationRefactorOptInCoreAsync(','private static Task<LegacyNativeConversationOptInResult> CompleteNativeConversationOptInFallbackAsync(','public static Task<DetachedInteractionHostResult> SubmitSceneShoutRefactorOptInForExternalAsync(','private Task<DetachedInteractionHostResult> SubmitSceneShoutRefactorOptInCoreAsync(','public static LegacyNativeActionPlanExecutor CreateNativeConversationActionPlanExecutorForExternal(','public static LegacyNativeActionPlanExecutor CreateSceneShoutActionPlanExecutorForExternal('])+' } }'
    host=host.replace('@@CURRENT_FACTORY_NPC@@',declaration(s,'internal static NpcDataPacket BuildNativeConversationNpcData('))
    identity=read('src/AF.GameAdapter.Bannerlord/Memory/MemoryEntityIdentityBannerlordAdapter.cs')
    identity_signatures=['internal static bool IsMemoryEntityEligibleForCompressedMemory(','internal static bool IsHeroNpcEligibleForCompressedMemory(','internal static Hero FindHeroById(','internal static int GetCurrentGameDayIndexSafe(','internal static string GetHeroId(','internal static string ResolveDisplayNameBySettlementEntry(','internal static string ResolveHeroName(','internal static string ResolveClanName(','internal static string ResolveKingdomName(','internal static string BuildNpcMajorActionsRuntimeInstruction(']
    host+='\nnamespace AnimusForge {internal static class MemoryEntityIdentityBannerlordAdapter { '+ '\n'.join(declaration(identity,x) for x in identity_signatures)+' } internal static class CampaignCharacterRecordCaptureAdapter { '+declarations('src/AF.GameAdapter.Bannerlord/Records/CampaignCharacterRecordCaptureAdapter.cs',['internal static string GetMemoryHeroId(','internal static string BuildNpcActionSummary(','internal static string BuildNpcActionMetadataNarrativeSuffix('])+' } internal static class CampaignBattleRecordCaptureAdapter { '+declarations('src/AF.GameAdapter.Bannerlord/Records/CampaignBattleRecordCaptureAdapter.cs',['internal static string StripBattlePlayerMarker('])+' } internal static class WeeklyAggregateEventLineOwner { '+declarations('src/modules/AF.Module.Weekly/Materials/WeeklyAggregateEventLineOwner.cs',['internal static string TranslateNpcActionKindForPrompt('])+' } }'
    queues=read('src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.Queues.cs')
    state_fields=[]
    for path in ['src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.IdentityStores.cs','src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.Queues.cs','src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.cs']:
        state_fields += [line for line in read(path).splitlines() if re.match(r'^\s*internal Dictionary<.*> (?:RecentActions|MajorActions|MajorSummaries) =',line)]
    host+='\nnamespace AnimusForge { internal sealed partial class MemoryBusinessStateOwner { '+ '\n'.join(state_fields)+ '\n'+ '\n'.join(declaration(queues,x) for x in ['internal MajorActionSummaryState GetMajorActionSummaryState(','internal static bool IsNpcActionAfterSummaryCursor('])+' } }'
    host=host.replace('@@CURRENT_MAJOR_INSTRUCTION@@',declaration(read('src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs'),'private string BuildNpcMajorActionsRuntimeInstruction('))
    host+='\nnamespace AnimusForge {internal sealed partial class MyBehavior { '+declarations('src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.HistoryPromptSnapshot.cs',['internal sealed class HistoryPromptSnapshot'])+' } }'
    host+='\nnamespace AnimusForge { internal sealed partial class MemoryBusinessStateOwner { '+declarations('src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.cs',['internal static int GetMemoryFinalInjectCountFromSettings(','internal static int GetMemoryCandidateLimitFromSettings(','internal static int GetMemoryPreprocessModeFromSettings('])+' } internal static class MemoryRecallContextOwner { '+declarations('src/modules/AF.Module.Memory/Recall/MemoryRecallContextOwner.cs',['internal static string BuildMemoryRecallQueryText('])+' } }'
    host+='\nnamespace AnimusForge.Refactor.Adapters {internal static partial class MemoryRecallInputCaptureAdapter { '+declarations('src/AF.GameAdapter.Bannerlord/Prompt/MemoryRecallInputCaptureAdapter.cs',['internal static string BuildMemoryRecallQueryText('])+' } }'
    history_root=read('src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.HistoryPromptSnapshot.cs')
    for sig in ['internal static Func<string> CaptureHistoryContextWorkForHero(', 'internal static Func<string> CaptureHistoryContextWorkById(']:
        host=host.replace(declaration(host,sig),declaration(history_root,sig))
    histmethods=['private SharedPromptCaptureBannerlordAdapter.HistoryWorkCapturePorts HistoryWorkCapturePorts =>','internal static SharedPromptCaptureBannerlordAdapter.HistoryWorkCapturePorts ResolveHistoryWorkCapturePorts(']
    recall_root=read('src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemoryRecall.cs')
    init=my[my.index('        _memoryHistoryContext = new MemoryHistoryContextReadCapabilities'):my.index('        _campaignSaveExit =')]
    host+='\nnamespace AnimusForge {internal sealed partial class MyBehavior {private SharedPromptCaptureBannerlordAdapter.HistoryWorkCapturePorts _historyWorkCapturePorts;private MemoryRecallInputCaptureAdapter.CapturePorts _memoryRecallCapturePorts; private readonly MemoryHistoryContextReadCapabilities _memoryHistoryContext;public MyBehavior(){'+init+'}'+ '\n'.join(declaration(history_root,x) for x in histmethods)+declaration(recall_root,'private MemoryRecallInputCaptureAdapter.CapturePorts MemoryRecallCapturePorts =>')+declaration(my,'private MemoryBusinessStateOwner MemoryQueueState {')+'private static bool IsMemoryEntityEligibleForCompressedMemory(string id)=>MemoryEntityIdentityBannerlordAdapter.IsMemoryEntityEligibleForCompressedMemory(id);private static int GetMemoryOverviewStartBlockCountFromSettings()=>LlmRequestConfigurationCaptureAdapter.GetMemoryOverviewStartBlockCountFromSettings(); } }'
    host+='\nnamespace AnimusForge {internal sealed partial class MemoryBusinessStateOwner { '+ '\n'.join(line for line in state.splitlines() if re.match(r'^\s*internal Dictionary<.*> (?:Blocks|Overviews) =',line))+' internal MemoryQueuePort QueuePort;'+ '\n'.join(declaration(state,x) for x in ['internal List<DailyMemoryDraft> LoadDrafts(','internal List<CompressedMemoryBlock> LoadBlocks(','internal string BuildHistoryContextById('])+ '\n'.join(declaration(queues,x) for x in ['internal MemoryOverviewState GetMemoryOverviewState(','internal bool HasMemoryOverviewPendingBlocks('])+' } '+declaration(state,'internal sealed class MemoryHistoryContextReadCapabilities')+declaration(queues,'internal sealed class MemoryQueuePort')+' internal static class MemorySummaryApplicationAdapter { '+declarations('src/AF.GameAdapter.Bannerlord/Memory/MemorySummaryApplicationAdapter.cs',['internal static string BuildMemoryOverviewContextById('])+' } }'
    recall=read('src/modules/AF.Module.Memory/Recall/MemoryRecallContextOwner.cs')
    host=host.replace('internal static class MemoryRecallContextOwner {','internal static partial class MemoryRecallContextOwner {')
    host+='\nnamespace AnimusForge {internal static partial class MemoryRecallContextOwner { '+ '\n'.join(declaration(recall,x) for x in ['internal static string BuildCompressedMemoryContextById(','internal static void AssignMemoryCandidateDisplayIds(','internal static string FormatPastAfefLineForPrompt(','internal static string FormatMemoryHourRange(','internal static string FormatCompressedMemoryAgeSuffix(','internal static string StripMemoryTitleDateTime('])+' } '+declaration(recall,'internal sealed class MemoryRecallRequest')+' }'
    host+='\nnamespace AnimusForge.Refactor.Adapters {using HistoryPromptSnapshot=AnimusForge.MyBehavior.HistoryPromptSnapshot;internal static partial class MemoryRecallInputCaptureAdapter { '+declarations('src/AF.GameAdapter.Bannerlord/Prompt/MemoryRecallInputCaptureAdapter.cs',['internal sealed class CapturePorts','internal static string BuildCompressedMemoryContextById(','internal static MemoryRecallRequest CaptureMemoryRecallRequest(','internal static void PublishMemoryRecallFailure('])+' } internal static partial class LlmRequestConfigurationCaptureAdapter { '+declarations('src/AF.GameAdapter.Bannerlord/Configuration/LlmRequestConfigurationCaptureAdapter.cs',['internal static int GetMemoryOverviewStartBlockCountFromSettings('])+' } }'
    host+='\nnamespace AnimusForge {internal static partial class LlmRetryPrompt { '+declarations('src/AF.GameAdapter.Bannerlord/UI/Errors/LlmRetryPrompt.cs',['public static string BuildFailureDetail(','private static string NormalizeFullText('])+' } }'
    weekly=read('src/AF.GameAdapter.Bannerlord/Prompt/WeeklyPromptCaptureAdapter.cs')
    weekly_sigs=['internal sealed class CapturePorts','internal static WeeklyPromptSnapshot CaptureWeeklyPromptSnapshot(','internal static WeeklyPromptSnapshot CaptureWeeklyPromptSnapshotForExternal(','internal static string BuildWeeklyShortReportsPromptBlock(','internal static string BuildTriggeredWeeklyFullReportsPromptBlock(','internal static string BuildSingleWeeklyFullReportPromptBlock(','internal static Dictionary<string, WeeklyPromptReportSnapshot> CaptureLatestWeeklyPromptReportSnapshots(','internal static WeeklyPromptReportSnapshot CreateWeeklyPromptReportSnapshot(','internal static bool IsNewerWeeklyPromptReportSnapshot(','internal static string BuildWeeklyShortReportsPromptBlockFromSnapshot(','internal static string BuildSingleWeeklyFullReportPromptBlockFromSnapshot(','internal static string BuildTriggeredWeeklyFullReportsPromptBlockFromSnapshot(']
    host+='\nnamespace AnimusForge.Refactor.Adapters {using WeeklyPromptSnapshot=AnimusForge.MyBehavior.WeeklyPromptSnapshot;using WeeklyPromptReportSnapshot=AnimusForge.MyBehavior.WeeklyPromptReportSnapshot;using EventRecordEntry=AnimusForge.MyBehavior.EventRecordEntry;internal static class WeeklyPromptCaptureAdapter { '+ '\n'.join(declaration(weekly,x) for x in weekly_sigs)+' } }'
    host=host.replace(declaration(host,'internal sealed class WeeklyPromptSnapshot'),declaration(my,'public sealed class WeeklyPromptSnapshot'))
    host=host.replace(declaration(host,'internal static WeeklyPromptSnapshot CaptureWeeklyPromptSnapshotForExternal('),declaration(my,'public static WeeklyPromptSnapshot CaptureWeeklyPromptSnapshotForExternal('))
    host+='\nnamespace AnimusForge {internal sealed partial class MyBehavior { '+declaration(my,'internal sealed class WeeklyPromptReportSnapshot')+declaration(my,'internal sealed class EventRecordEntry')+declarations('src/modules/AF.Module.Weekly/Models/WeeklyLegacyDtos.cs',['internal sealed class EventMaterialReference'])+declaration(my,'private string BuildWeeklyShortReportsPromptBlock(')+declaration(my,'private string BuildTriggeredWeeklyFullReportsPromptBlock(')+' } internal static class WeeklyGenerationRules { '+declarations('src/modules/AF.Module.Weekly/Generation/WeeklyGenerationRules.cs',['internal static string BuildFallbackWeeklyReportShortSummary(','internal static string NeutralizeWeeklyReportScenarioName('])+' } internal static class WeeklyEventRecordStateOwner { '+declarations('src/modules/AF.Module.Weekly/Records/WeeklyEventRecordStateOwner.cs',['internal static List<string> SelectWeeklyShortReportKingdomIdsFromSnapshot('])+' } '+declarations('src/modules/AF.Module.Weekly/Bulletin/WorldBulletinPolicy.cs',['internal sealed class WorldBulletinEvent','internal sealed class WorldBulletinParticipant'])+' }'
    host+='\nnamespace AnimusForge {internal static partial class MemoryEntityIdentityBannerlordAdapter { '+declaration(identity,'internal static string ResolveKingdomDisplay(')+' } }'
    host=host.replace('internal static class MemoryEntityIdentityBannerlordAdapter {','internal static partial class MemoryEntityIdentityBannerlordAdapter {')
    host+='\nnamespace AnimusForge {internal sealed partial class MyBehavior { '+declarations('src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.CampaignMaterialRecords.cs',['internal sealed class EventSourceMaterialEntry'])+' } }'
    host+='\nnamespace AnimusForge.Refactor.Adapters {internal static partial class PersonaIdentityPromptCaptureAdapter { '+declarations('src/AF.GameAdapter.Bannerlord/Prompt/PersonaIdentityPromptCaptureAdapter.cs',['internal sealed class HeroIdentityInfoSnapshot'])+' } }'
    host+='\nnamespace AnimusForge {internal sealed partial class MemoryBusinessStateOwner { '+declaration(state,'internal static PromptTopicSemanticEvaluator CreateBuiltInTopicSemanticEvaluator(')+' } }'
    history_helpers=['internal static string BuildScenePublicHistorySection(','internal static Func<string> CaptureNativeConversationPersistedHistoryWork(','internal static string GetLatestNativeConversationNpcUtteranceForExternal(','internal static bool IsDetailedSceneSpeechPromptEnabled(','internal static bool ShouldPreserveSceneAsteriskActions(']
    host+='\nnamespace AnimusForge.Refactor.Adapters {internal sealed partial class SceneHistoryPromptCaptureAdapter { '+declarations('src/AF.GameAdapter.Bannerlord/Prompt/SceneHistoryPromptCaptureAdapter.cs',history_helpers)+' } }'
    host=host.replace('internal sealed class SceneHistoryPromptCaptureAdapter {','internal sealed partial class SceneHistoryPromptCaptureAdapter {')
    agent_helpers=['internal static void GetSceneReplyLengthLimits(','internal static string BuildNativeConversationNpcListBlockForPrompt(','internal static List<NpcDataPacket> FilterScenePresentNpcsForPrompt(','internal static string BuildSceneNpcListLineForPrompt(','internal static string BuildSceneNonHeroNamingNoteForPrompt(','internal static string JoinPromptSections(','internal static string BuildPlayerCustomPromptRuleBlock(','internal static bool IsSameSceneNpcForPrompt(','internal static string NormalizeSceneNpcMatchValue(']
    host+='\nnamespace AnimusForge.Refactor.Adapters {internal sealed partial class SceneAgentIdentityPromptCaptureAdapter { '+ '\n'.join(declaration(agent,x) for x in agent_helpers)+'internal static void LogNonHeroMemoryTrace(string value){} } internal static partial class PersonaIdentityPromptCaptureAdapter {'+next(line for line in read('src/AF.GameAdapter.Bannerlord/Prompt/PersonaIdentityPromptCaptureAdapter.cs').splitlines() if 'internal const int MarriageCandidateMinAgeForPrompt =' in line)+declarations('src/AF.GameAdapter.Bannerlord/Prompt/PersonaIdentityPromptCaptureAdapter.cs',['internal static string BuildFactionLineForPrompt('])+' } }'
    host+='\nnamespace AnimusForge {internal static partial class AIConfigHandler {public static bool TryCallAuxiliaryRuleCodesForExternal(string p,string n,string context,int count,out List<string> ids,out string error,IEnumerable<string> excluded=null){ids=new();error="";return true;} } }'
    host=host.replace('public class Hero {','public class Hero {public bool IsPrisoner;public PartyBase PartyBelongedToAsPrisoner;')
    host=host.replace('public class Settlement {','public class Settlement {public bool IsCastle,IsTown,IsVillage;public Clan OwnerClan,MapFaction;')
    host=host.replace('public class PartyBase {','public class PartyBase {public bool IsSettlement,IsMobile;public Settlement Settlement;public MobileParty MobileParty;public Hero LeaderHero;public Clan MapFaction;')
    host=host.replace('public class MobileParty {','public class MobileParty {public string Name,StringId;public Clan ActualClan;')
    host+='\nnamespace AnimusForge {internal sealed partial class DuelSettings {public bool UseDetailedSceneSpeechPrompt=true,PreserveSceneAsteriskActions=true;} }'
    host+='\nnamespace AnimusForge {internal static class PartyAssetTransferBannerlordAdapter { '+declarations('src/AF.GameAdapter.Bannerlord/Composition/PartyAssetTransferBannerlordAdapter.cs',['internal static int TransferItemsFromRosterByStringId('])+' } internal sealed partial class MyBehavior { '+declaration(my,'public static int TransferItemsFromRosterByStringId(')+declaration(my,'public static int RemoveItemsFromRosterByStringId(')+' } }'
    host=host.replace('public struct EquipmentElement {}','public struct EquipmentElement {public ItemObject Item;public string Modifier;public EquipmentElement(ItemObject item,string modifier){Item=item;Modifier=modifier;}}')
    host='global using TaleWorlds.CampaignSystem.Roster;\n'+host
    host=host.replace('try{Run();RunOptIn();','try{RosterTransferCurrentChecks.Verify();Run();RunOptIn();')
    host+=r"""
namespace TaleWorlds.CampaignSystem.Roster {
public struct ItemRosterElement {public EquipmentElement EquipmentElement;public int Amount;}
public sealed class ItemRoster {
 public readonly List<ItemRosterElement> Entries=new();public bool FailOnAdd;public int Reads;public int Count=>Entries.Count;
 public ItemRosterElement GetElementCopyAtIndex(int index){Reads++;return Entries[index];}
 public void AddToCounts(EquipmentElement equipment,int count){if(FailOnAdd)throw new InvalidOperationException("synthetic target failure");int i=Entries.FindIndex(x=>ReferenceEquals(x.EquipmentElement.Item,equipment.Item)&&x.EquipmentElement.Modifier==equipment.Modifier);if(i<0){Entries.Add(new(){EquipmentElement=equipment,Amount=count});return;}var entry=Entries[i];entry.Amount+=count;if(entry.Amount==0)Entries.RemoveAt(i);else Entries[i]=entry;}
}
}
namespace AnimusForge {
internal static class RosterTransferCurrentChecks {
 static void Check(bool ok,string name){if(!ok)throw new InvalidOperationException("ROSTER ASSERT "+name);Console.WriteLine("PASS roster "+name);}
 static ItemRoster Roster(params (ItemObject item,string modifier,int count)[] values){var result=new ItemRoster();foreach(var x in values)result.AddToCounts(new EquipmentElement(x.item,x.modifier),x.count);return result;}
 internal static void Verify(){var sword=new ItemObject{StringId="sword"};var sameId=new ItemObject{StringId="SWORD"};var other=new ItemObject{StringId="other"};ItemObject first;
 Check(MyBehavior.TransferItemsFromRosterByStringId(null,null,"sword",2,out first)==0&&first==null,"null source preserves zero/out null");
 var source=Roster((sword,"fine",2));Check(MyBehavior.TransferItemsFromRosterByStringId(source,null," ",2,out first)==0&&first==null&&source.Count==1&&MyBehavior.TransferItemsFromRosterByStringId(source,null,"sword",0,out first)==0&&first==null,"blank ID/nonpositive amount do not touch source");
 source=Roster((other,"plain",1),(sword,"fine",2),(sameId,"rusty",3));var target=Roster();var moved=MyBehavior.TransferItemsFromRosterByStringId(source,target," sWoRd ",4,out first);Check(moved==4&&ReferenceEquals(first,sword)&&source.Entries.Single(x=>ReferenceEquals(x.EquipmentElement.Item,sameId)).Amount==1&&source.Entries[0].EquipmentElement.Item==other,"trim case/min count/re-read changing roster/out first item");
 Check(target.Entries.Count==2&&target.Entries[0].EquipmentElement.Modifier=="fine"&&target.Entries[0].Amount==2&&target.Entries[1].EquipmentElement.Modifier=="rusty"&&target.Entries[1].Amount==2,"EquipmentElement modifiers/order survive source-to-target game leaf");
 moved=MyBehavior.TransferItemsFromRosterByStringId(source,target,"sword",99,out first);Check(moved==1&&ReferenceEquals(first,sameId)&&source.Count==1,"partial shortage returns actual delivered not requested");
 source=Roster((sword,"fine",3));Check(MyBehavior.RemoveItemsFromRosterByStringId(source,"SWORD",2,out first)==2&&ReferenceEquals(first,sword)&&source.Entries[0].Amount==1,"public Remove ABI uses same transfer leaf with null target");
 var failedTarget=new ItemRoster{FailOnAdd=true};bool threw=false;try{MyBehavior.TransferItemsFromRosterByStringId(source,failedTarget,"sword",1,out first);}catch(InvalidOperationException){threw=true;}Check(threw&&source.Count==0&&ReferenceEquals(first,sword),"original target failure propagates after source decrement without rollback");
 source=Roster((other,"",1));Check(MyBehavior.TransferItemsFromRosterByStringId(source,null,"missing",1,out first)==0&&first==null&&source.Count==1,"missing item exits without falsely delivering");
 Console.WriteLine("PASS 8 actual roster transfer/Transfer Remove ABI assertions");
 }
}
}
"""
    # Opt-in uses the actual host, pipeline, request-bound executor and dispatcher, not a factory-only test.
    action=read('src/modules/AF.Module.Conversation/Actions/ConversationActionExecutorComposition.cs')
    action_signatures=['internal ConversationActionExecutorComposition(', 'internal LegacyNativeActionPlanExecutor CreateNativeConversationActionPlanExecutorForExternal(', 'internal LegacyNativeActionPlanExecutor CreateSceneShoutActionPlanExecutorForExternal(', 'internal async Task<DetachedInteractionHostResult> SubmitSceneShoutRefactorOptInCoreAsync(', 'internal static IInteractionMemory CreateSceneShoutMemoryFacadeForExternal(', 'internal static async Task<DetachedInteractionHostResult> RunDetachedRefactorFallbackAsync(', 'internal Task<InteractionCommitResult> DispatchSceneShoutRefactorCommitAsync(', 'internal static LegacyInteractionPipelinePorts CreateSceneShoutDetachedPortsForExternal(', 'internal async Task<LegacyNativeConversationOptInResult> SubmitNativeConversationRefactorOptInCoreAsync(', 'internal static IInteractionMemory CreateNativeConversationMemoryFacadeForExternal(', 'internal static async Task<LegacyNativeConversationOptInResult> CompleteNativeConversationOptInFallbackAsync(', 'internal Task<InteractionCommitResult> DispatchNativeConversationOptInCommitAsync(']
    host+='\nnamespace AnimusForge { internal sealed class ConversationActionExecutorComposition { private readonly Func<bool> _isCurrentOwner;private readonly SceneMovementController _sceneMovement;private readonly NativeConversationGameEffectsRuntime _nativeGameEffects;private readonly ConversationActionBoundaryBannerlordAdapter _boundary;private readonly ConversationGameThreadDispatcher _dispatcher;'+ '\n'.join(declaration(action,x) for x in action_signatures)+' } }'
    host+='\nnamespace AnimusForge { internal static class ConversationActionContextBannerlordAdapter { '+declarations('src/AF.GameAdapter.Bannerlord/Conversation/ConversationActionContextBannerlordAdapter.cs',['internal static IEconomyRewardDebtMainThreadPort CreateEconomyReplayPortForExternal(','internal static string ResolveDetachedInteractionSubjectId('])+' } }'
    dispatch=read('src/modules/AF.Module.Duel/DuelBehavior.DispatchOwner.cs')
    outcomes=read('src/modules/AF.Module.Duel/Host/DuelBehavior.Outcomes.cs')
    fields=outcomes[outcomes.index('private static readonly object _duelOutcomeOwnerSync'):outcomes.index('private DuelOutcomeStartIdentity _activeDuelOutcomeStart')]
    duel=dispatch[dispatch.index('private static readonly IDetachedDuelDispatchOwner'):dispatch.index('internal static IDetachedDuelDispatchOwner')]
    host+='\nnamespace AnimusForge { internal static partial class DuelBehavior { '+fields+duel+'\n'+ '\n'.join(declaration(dispatch,x) for x in ['internal static IDetachedDuelDispatchOwner CreateDetachedDuelDispatchOwnerForExternal(','private sealed class DuelBehaviorDetachedDispatchOwner','private static bool ValidateDetachedDuelDispatchContext(','private static string NormalizeDuelOutcomeReason('])+ '\n'+ '\n'.join(declaration(outcomes,x) for x in ['private static DuelOutcomeOperationStatus RejectDuelOutcomeRequest(','private static void IndexDuelOutcome(','private static void IndexDuelOutcomeRequest(','private static void ClearDuelOutcomeSubjectIndex(','private static string NormalizeDuelOutcomeSubject('])+' } }'
    host+='\nnamespace AnimusForge.Refactor.Adapters { '+declaration(read('src/AF.GameAdapter.Bannerlord/Composition/LegacyInteractionSnapshotAdapters.cs'),'public sealed class MyBehaviorMemoryFacade')+' }'
    host+='\nnamespace AnimusForge.Refactor.Adapters { '+declaration(read('src/modules/AF.Module.Conversation/Channels/Native/LegacyNativeConversationOptInRunner.cs'),'public sealed class LegacyNativeConversationOptInResult')+' }'
    boundary=read('src/AF.GameAdapter.Bannerlord/Conversation/ConversationActionBoundaryBannerlordAdapter.cs')
    host+='\nnamespace AnimusForge { internal delegate bool ConversationOpenHallAction(Hero h,CharacterObject c,int index,ref string tags);internal sealed partial class ConversationActionBoundaryBannerlordAdapter { '+boundary[boundary.index('private readonly NativeConversationGameEffectsRuntime _nativeGameEffects'):boundary.index('internal ConversationActionBoundaryBannerlordAdapter(')]+declaration(boundary,'internal ConversationActionBoundaryBannerlordAdapter(')+''+ '\n'.join(declaration(boundary,x) for x in ['internal bool TryApplyDeferredSceneMoodTag(','internal bool TryApplyDeferredScenePostprocessActionTagsDirectly('])+' } public partial class ShoutBehavior { '+ '\n'.join(declaration(s,x) for x in ['internal static string StripDeferredSceneMoodTags(','internal static string ExtractDeferredSceneActionTags(','internal static bool HasNonMoodDeferredSceneActionTag('])+' } }'
    host+='\nnamespace AnimusForge.Refactor.Adapters { internal sealed partial class SceneAgentIdentityPromptCaptureAdapter { '+declaration(agent,'internal static Hero ResolveHeroFromAgentIndex(')+' } }'
    host+='\nnamespace AnimusForge {internal static class SceneShoutInputController { '+declarations('src/AF.GameAdapter.Bannerlord/Scene/SceneShoutInputController.cs',['internal static bool CanAgentParticipateInSceneSpeech('])+' } }'
    movement=read('src/AF.GameAdapter.Bannerlord/SceneActions/SceneMovementController.cs')
    regex_fields='\n'.join(line for line in movement.splitlines() if 'private static readonly Regex SceneFollowStartTagRegex =' in line or 'private static readonly Regex SceneFollowStopTagRegex =' in line)
    host+='\nnamespace AnimusForge {internal sealed partial class SceneMovementController { '+regex_fields+declaration(movement,'internal bool TryExecuteDeferredSceneFollowTagsDirectly(')+' } }'
    host=host.replace('internal static partial class SceneMovementController','internal sealed partial class SceneMovementController')
    mechanism=read('src/AF.GameAdapter.Bannerlord/Prompt/SceneMechanismPromptCaptureAdapter.cs')
    mech_sigs=['internal SceneMechanismPromptCaptureAdapter(','internal List<PostprocessRuleEntry> BuildRuntimeSceneMechanismPostprocessRulesForScene(','internal static void AddSceneMechanismPostprocessRule(','internal static void AppendSceneUnifiedTargetPromptSection(','internal static string BuildSceneMechanismPromptSection(','internal static bool ShouldExcludeWorldMapCommandTopicForPreprocess(','internal bool CanInjectSceneMechanismTopicIntoPreprocess(','internal bool CanInjectRuleTopicIntoPreprocessForCurrentInteraction(','internal List<string> BuildPreprocessExcludedRuleIdsForCurrentInteraction(','internal static bool CanInjectDuelPostprocessRule(']
    host+='\nnamespace AnimusForge.Refactor.Adapters {using SceneSummonPromptTarget=AnimusForge.ShoutBehavior.SceneSummonPromptTarget;using SceneGuidePromptTarget=AnimusForge.ShoutBehavior.SceneGuidePromptTarget;internal sealed class SceneMechanismPromptCaptureAdapter { '+mechanism[mechanism.index('    private readonly Func<int,bool>'):mechanism.index('    internal SceneMechanismPromptCaptureAdapter(')]+ '\n'.join(declaration(mechanism,x) for x in mech_sigs)+' } }'
    (out/'Host.cs').write_text(host,encoding='utf-8')
    metadata=(ROOT/'tests/AF.Contracts/ModuleFrameworkApiTests/HostStubs.cs').read_text(encoding='utf-8')
    metadata=metadata.replace('internal static object Policy','internal static AnimusForge.FixtureTeamLeaves Policy').replace('internal static object Gathering','internal static AnimusForge.FixtureTeamLeaves Gathering').replace('internal static object Siege','internal static AnimusForge.FixtureTeamLeaves Siege').replace('= new object();','= new();')
    metadata=metadata.replace('internal static FeatureBridgeDecision Evaluate','internal static bool IsEnabled(string id,int version=1)=>Evaluate(id,version).Status==FeatureBridgeDecisionStatus.Allowed;\n        internal static FeatureBridgeDecision Evaluate')
    (out/'MetadataLeaves.cs').write_text(metadata,encoding='utf-8')
    playback_stubs=(ROOT/'tests/modules/AF.Module.Conversation/NativeWaitAudioBoundaryTests/Stubs.cs').read_text(encoding='utf-8')
    leaf_types=[('TaleWorlds.Library','public struct Vec3 '),('TaleWorlds.Engine','public sealed class Scene '),('TaleWorlds.Engine','public sealed class SoundEvent '),('TaleWorlds.MountAndBlade','public sealed class AgentVisuals '),('AnimusForge','public static partial class ConversationHelper '),('AnimusForge','public sealed class TtsEngine '),('AnimusForge','public static class BannerlordExceptionSentinel '),('AnimusForge','public static class EncyclopediaEntityLinkFormatter '),('TaleWorlds.CampaignSystem','public interface ICampaignMission '),('TaleWorlds.CampaignSystem','public static class CampaignMission ')]
    playback='\n'.join('namespace '+ns+' { '+declaration(playback_stubs,sig)+' }' for ns,sig in leaf_types)+'\nnamespace SandBox {}'
    (out/'PlaybackLeaves.cs').write_text(playback,encoding='utf-8')
    sources=[out/'Host.cs',out/'MetadataLeaves.cs',out/'PlaybackLeaves.cs']
    paths=['src/modules/AF.Module.Prompt/Composition/PersonaIntroTextRules.cs','src/modules/AF.Module.Memory/Records/MemoryRecallCandidate.cs','src/modules/AF.Module.Memory/Records/NpcActionRecordOwner.cs','src/modules/AF.Module.Memory/Records/NpcActionLedger.cs','src/modules/AF.Module.Conversation/Channels/Native/ConversationMainThreadActionDrain.cs','src/AF.Contracts/Internal/ProfileConfigContracts.cs','src/modules/AF.Module.Conversation/Internal/DetachedInteractionHost.cs','src/modules/AF.Module.Conversation/Internal/Pipeline/LegacyInteractionPipelineComposition.cs','src/modules/AF.Module.Conversation/Internal/Pipeline/LegacyChannelInteractionFacade.cs','src/modules/AF.Module.Conversation/Channels/Native/LegacyNativeConversationFacade.cs','src/modules/AF.Module.Conversation/Internal/InteractionRequestCoordinator.cs','src/modules/AF.Module.Conversation/Internal/Pipeline/InteractionPipeline.cs','src/modules/AF.Module.Conversation/Internal/Pipeline/FullInteractionPipeline.cs','src/modules/AF.Module.Actions/Execute/LegacyNativeActionPlanExecutor.cs','src/modules/AF.Module.Economy/Planning/LegacyEconomyRewardDebtAdapter.cs','src/modules/AF.Module.Memory/Recovery/MemoryCommitReceiptCache.cs','src/AF.GameAdapter.Bannerlord/Prompt/PromptRuleCaptureBannerlordAdapter.cs','src/modules/AF.Module.Prompt/Composition/ExtraRuleInstructionComposer.cs','src/modules/AF.Module.Memory/Records/NpcActionEntry.cs','src/modules/AF.Module.Memory/Records/MemoryPersistenceModels.cs','src/modules/AF.Module.Memory/Recovery/InteractionMemoryRecoveryLedger.cs','src/modules/AF.Module.Conversation/Channels/Scene/NpcDataPacket.cs','src/modules/AF.Module.Memory/Records/DialogueHistoryLedger.cs','src/AF.GameAdapter.Bannerlord/Composition/PromptContextCaptureBannerlordAdapter.cs','src/modules/AF.Module.Prompt/Composition/PromptContextDecisions.cs','src/modules/AF.Module.Prompt/Composition/PromptAssemblyStage.cs','src/modules/AF.Module.Prompt/Composition/PromptRuleInstructionComposer.cs','src/modules/AF.Module.Prompt/Composition/PromptPreprocessRuleIdAssembler.cs','src/modules/AF.Module.Prompt/Composition/PromptExtrasComposer.cs','src/modules/AF.Module.Prompt/Composition/PromptRuleBlockText.cs','src/modules/AF.Module.Conversation/Internal/History/NativeHistoryIdentityProjectionOwner.cs','src/modules/AF.Module.Conversation/Internal/History/SceneConversationHistoryOwner.cs','src/modules/AF.Module.Conversation/Channels/Scene/ScenePendingAfefFactsOwner.cs','src/modules/AF.Module.Conversation/Channels/Native/NativeConversationSessionOwner.cs','src/modules/AF.Module.Memory/Records/AnimusForgeDialogueHistoryEntry.cs','src/modules/AF.Module.Conversation/Channels/Native/NativeDetachedPostprocessApplicationAdapter.cs','ShoutBehavior.NativeDetachedPostprocess.cs','src/modules/AF.Module.Prompt/Configuration/LegacyDetachedRuleSelector.cs','src/AF.GameAdapter.Bannerlord/Prompt/SharedPromptCaptureBannerlordAdapter.cs','src/AF.GameAdapter.Bannerlord/Scene/NativeConversationSpeechAdapter.cs','src/AF.GameAdapter.Bannerlord/Scene/NativeConversationPlaybackWaitAdapter.cs','src/AF.GameAdapter.Bannerlord/Scene/SceneAudioLipSyncController.cs','src/AF.Contracts/Internal/InteractionContracts.cs','src/AF.Contracts/Internal/LlmContracts.cs',
      'src/modules/AF.Module.Conversation/Channels/Native/NativeAdmissionApplicationAdapter.cs',
      'src/modules/AF.Module.Conversation/Channels/Native/ConversationGameThreadDispatcher.cs',
      'src/modules/AF.Module.Conversation/Channels/Native/NativeConversationDispatchClaim.cs',
      'src/modules/AF.Module.Conversation/Channels/Native/NativeConversationAdmissionOwner.cs',
      'ShoutBehavior.NativeAdmission.cs','ShoutBehavior.ModuleNativeSubmission.cs',
      'src/modules/AF.Module.Conversation/Channels/Native/NativeConversationGameEffectsRuntime.cs',
      'ShoutBehavior.NativeActionCommit.cs','ShoutBehavior.NativeActionDispatch.cs','ShoutBehavior.NativeCompletion.cs',
      'src/modules/AF.Module.Conversation/Channels/Native/NativeConversationGameEffectPorts.cs',
      'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.NativeGameEffects.cs',
      'src/modules/AF.Module.Actions/Tags/LegacyActionTagParser.cs','src/modules/AF.Module.Actions/Tags/LegacyActionTagCatalog.cs',
      'src/modules/AF.Module.Actions/Execute/LegacyChannelActionPlanExecutor.cs','src/modules/AF.Module.Actions/Execute/LegacyChannelActionCommitter.cs',
      'src/modules/AF.Module.Conversation/Internal/History/ConversationMessage.cs',
      'src/modules/AF.Module.Conversation/Internal/History/SceneHistoryProjectionOwner.cs',
      'src/modules/AF.Module.Prompt/Composition/ConversationRoleClassificationOwner.cs',
      'src/modules/AF.Module.Actions/Receipts/InteractionResultCommitter.cs',
      'src/modules/AF.Module.Actions/Plan/ActionPlanIntegrityPolicy.cs',
      'src/modules/AF.Module.Actions/Receipts/ActionExecutionCommitter.cs','src/modules/AF.Module.Duel/DuelOutcomeReceipt.cs',
      'src/modules/AF.Module.Weekly/Receipts/WeeklyMemoryMaterialOutcomeReceipt.cs',
      'src/AF.Contracts/Compatibility/Economy/EconomyRewardDebtContracts.cs',

      'src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs',
      'src/modules/AF.Module.Conversation/Channels/Native/NativeConversationTurnCoordinator.cs',
      'src/modules/AF.Module.Conversation/Channels/Native/NativeConversationTurnPorts.cs',
      'ShoutBehavior.NativeTurn.cs','ShoutBehavior.NativeTurnPrompt.cs','ShoutBehavior.NativeTurnPresentation.cs','ShoutBehavior.NativeTurnCommit.cs',
      'ShoutBehavior.NativeMainReply.cs','ShoutBehavior.NativePromptBuild.cs',
      'ShoutBehavior.NativePendingHistory.cs','src/AF.GameAdapter.Bannerlord/Prompt/NativePendingHistoryApplicationAdapter.cs','src/modules/AF.Module.Persona/Generation/NpcPersonaReadinessSnapshot.cs','src/modules/AF.Module.Conversation/Internal/PersonaGenerationWaiter.cs',
      'src/modules/AF.Module.Conversation/Channels/Native/NativePromptWorkScheduler.cs',
      'src/modules/AF.Module.Conversation/Channels/Native/NativeConversationMainReplyContracts.cs',
      'src/modules/AF.Module.Conversation/Channels/Native/NativeConversationMainReplyStage.cs',
      'src/modules/AF.Module.Prompt/Composition/SharedPromptRoutingRuntime.cs',
      'src/modules/AF.Module.Prompt/Composition/PromptTopicRoutingStage.cs',
      'src/modules/AF.Module.Prompt/Composition/PromptBuiltInTopicRouter.cs',
      'src/modules/AF.Module.Prompt/Composition/BuiltInRuleStickyCarry.cs',
      'src/modules/AF.Module.Prompt/Composition/PromptRuleIdPolicy.cs',
      'src/modules/AF.Module.Prompt/Composition/PromptBuildRequest.cs',
      'src/modules/AF.Module.Prompt/Composition/PromptRuntimeTargetBinding.cs',
      'src/modules/AF.Module.Prompt/Composition/PromptRuleEligibility.cs',
      'src/modules/AF.Module.Prompt/Composition/PromptRetrievalCapture.cs',
      'src/modules/AF.Module.Prompt/Retrieval/GuardrailRuleHit.cs',
      'src/modules/AF.Module.Actions/Receipts/InteractionCommitReceiptCache.cs',
      'src/modules/AF.Module.Conversation/Internal/Postprocess/ConversationActionPostprocessOwner.cs',
      'src/modules/AF.Module.Conversation/Internal/Postprocess/ShoutBehavior.UnifiedActionPostprocess.cs',
      'src/modules/AF.Module.Prompt/Configuration/PostprocessRuleEntry.cs',
      'src/modules/AF.Module.Economy/Host/TransferQuantitySpec.cs',
      'src/modules/AF.Module.Prompt/Composition/LegacyNativePromptParity.cs',
      'src/modules/AF.Module.Prompt/Composition/LegacyPromptPackageAdapter.cs',
      'src/modules/AF.Module.Prompt/Composition/LegacyDetachedPostprocessPromptComposer.cs',
      'src/modules/AF.Module.Prompt/Composition/LegacyDetachedPromptComposer.cs',
      'src/bridges/Vengeance/Host/PublicExecutionOrderPolicy.cs',
      'src/modules/AF.Module.Economy/Host/GiveAssetTagCodec.cs',
      'src/modules/AF.Module.Prompt/Composition/HistorySectionProjectionOwner.cs',
      'src/modules/AF.Module.Prompt/Composition/MainPromptMessageAssemblyOwner.cs',
      'src/modules/AF.Module.Prompt/Composition/SceneHistoryMessageAssemblyOwner.cs',
      'src/modules/AF.Module.Prompt/Composition/ScenePromptMessageProjectionComposer.cs',
      'src/modules/AF.Module.Conversation/Internal/History/ConversationSpeechTextRules.cs',
      'src/modules/AF.Module.Llm/Protocol/LlmVisibleReplyNormalizer.cs',

      'src/modules/AF.Module.PublicApi/V1/AfApi.cs','src/AF.Contracts/PublicApi/V1/AfApiContracts.cs',
      'src/modules/AF.Module.PublicApi/V1/AfDialogueClient.cs','src/modules/AF.Module.PublicApi/Internal/AfV1DialogueProjection.cs',
      'src/modules/AF.Module.PublicApi/Internal/AfV1SnapshotProjection.cs',
      'src/modules/AF.Module.Conversation/Internal/CoreDialogueContracts.cs','src/modules/AF.Module.Conversation/Internal/CoreDialogueOperation.cs',
      'src/modules/AF.Module.Conversation/Internal/CoreDialogueClient.cs','src/modules/AF.Module.Conversation/Internal/CoreDialogueServices.cs',
      'src/AF.Foundation.Runtime/ModuleDirectory/HostedExtensionCatalog.cs',
      'src/AF.Foundation.Runtime/ModuleDirectory/ModuleFrameworkSnapshot.cs','src/AF.Foundation.Runtime/ModuleDirectory/InternalModuleDirectory.cs',
      'src/AF.Foundation.Runtime/ModuleDirectory/ModuleDirectoryLifecycleOwner.cs',
      'src/AF.GameAdapter.Bannerlord/Composition/ModuleFrameworkRuntime.cs','src/AF.GameAdapter.Bannerlord/Composition/TeamModuleRegistration.cs',
      'src/AF.Contracts/Internal/FeatureBridgeContracts.cs']
    paths += ['src/modules/AF.Module.Diplomacy/Adapters/DiplomacyDialogueSourceScope.cs', 'src/modules/AF.Module.Diplomacy/Domain/Dialogue/DialogueInteractionOrigin.cs']
    manifest=[]
    for path in paths:
        source_path=resolve(path);text=source_path.read_text(encoding='utf-8-sig')
        target=out/source_path.name;target.write_text(text,encoding='utf-8');sources.append(target)
        manifest.append({'path':source_path.relative_to(ROOT).as_posix(),'sourceClass':'current-production','rawSha256':__import__('hashlib').sha256(source_path.read_bytes()).hexdigest()})
    spec=importlib.util.spec_from_file_location('lifetime',HERE/'fixture_support.py');fixture=importlib.util.module_from_spec(spec);spec.loader.exec_module(fixture);fixture.include_request_lifetime(out)
    sources += [out/name for name in ['ConversationRequestLifetime.cs','InteractionRequestLease.cs','CancellationScope.cs']]
    # Native execution, prompt Begin/Complete/CaptureSections/Preparation, and receipt phases are actual production sources. Pending leaf callbacks are explicitly separated below.
    (out/'sources.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    (out/'extractions.json').write_text(json.dumps(extractions,indent=2),encoding='utf-8')
    (out/'verification-scope.json').write_text(json.dumps({
        'verified': ['Current public/internal ModuleNative request/admission-stamp/NativeTurn/real prompt preparation/Begin/Knowledge/Complete/pending-history/coordinator/effects/receipt/cancel',
                     'Actual B speech/playback/audio: strict fallback releases typewriter but does not complete wait; real TTS finish/duplicate-old-callback successor guards',
                     'Current public Native and Scene opt-in entry -> real full facade/pipeline/detached host -> request-bound actual factory/context/executor/committer -> owning dispatcher; normal/cancel/target/session/generation/memory-failure/null protocols',
                     'Actual major action input uses same A state, NpcActionRecordOwner/Ledger/summary/metadata without whole-owner substitute',
                     'Actual My/C HistoryWork captures detached block/AFEF, A same-state overview/context and actual compressed renderer; generation and retired-owner guards',
                     'Actual Weekly capture/selection/ranking/snapshot rendering and Short/Full callbacks (legacy report snapshot branch)'],
        'unverifiedCapturedInputs': [],
        'controlledLeafCapabilities': ['Native target/Agent identity, Persona and necessary movement-target game capture',
              'Weekly CapturePorts game identity/eligible/proximity/record-list/opening leaves are controlled; actual SelectSnapshot and C capture/render algorithms are production declarations',
              'AIConfigHandler guardrail runtime-scope state and independent game gates',
              'PlayerNotoriety external API boundary; persistence batch backend and Reward/Economy port null protocol'],
        'syntheticLeaves': ['Bannerlord live objects and game effects', 'LLM/lore/entity/rule retrieval', 'persistent memory commit backend', 'TTS engine/render/typewriter/timer', 'diagnostic sinks'],
        'notRun': ['live game', 'old saves', 'real provider frames', 'whole TeamModuleServices',
                   'Scene/Courier public ticket consumers (owned by B/C, not this D opt-in consumer)',
                   'Economy/Duel/Follow world execution; Mood-consumed empty Follow branch is actual current body',
                   'Recall embedding/preprocess branches (>FinalCount) and bulletin-enabled weekly branch (unvisited capabilities explicitly NotSupported)',
                   'Weekly request-generation/material policies; no claim beyond original Native prompt-input slice',
                   'Unused daily-append constructor/save/lifetime beyond source-linked read projection']},indent=2),encoding='utf-8')
    newtonsoft=Path(os.environ.get('AF_NEWTONSOFT') or ROOT/'local/bannerlord-refs/1.4.7.117484/Newtonsoft.Json.dll')
    assert newtonsoft.is_file(),newtonsoft
    project=out/'CurrentConsumer.csproj'
    project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><DefineConstants>' + ('BANNERLORD_1_4_OR_GREATER' if api_line=='1.4' else '') + '</DefineConstants><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+escape(str(x.resolve()))+'" />' for x in sources)+'<Reference Include="Newtonsoft.Json"><HintPath>'+escape(str(newtonsoft))+'</HintPath></Reference></ItemGroup></Project>',encoding='utf-8')
    (out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
    result=subprocess.run([str(dotnet),'build',str(project),'-c','Release'],cwd=ROOT,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=150)
    log=result.stdout+result.stderr;(out/'run.log').write_text(log,encoding='utf-8');print('Current consumer compilation:',result.returncode,'log:',out/'run.log');print('\n'.join(dict.fromkeys(re.findall(r'error ((?:CS|NU|MSB)\d+:.*?) \[',log))))
    if result.returncode!=0:return result.returncode
    execution=subprocess.run([str(dotnet),str(out/'bin/Release/net8.0/CurrentConsumer.dll')],cwd=ROOT,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90)
    (out/'execution.log').write_text(execution.stdout+execution.stderr,encoding='utf-8');print(execution.stdout+execution.stderr)
    return execution.returncode

if __name__=='__main__':
    import argparse
    p=argparse.ArgumentParser();p.add_argument('--run-root',required=True,type=Path);a=p.parse_args();raise SystemExit(verify(a.run_root))

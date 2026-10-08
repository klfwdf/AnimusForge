from pathlib import Path
import argparse,re,subprocess,sys
ROOT=Path(__file__).resolve().parents[3];HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import minimal_test_environment
parser=argparse.ArgumentParser();parser.add_argument('--run-root',required=True);parser.add_argument('--api',choices=['1.3','1.4'],default='1.3');args=parser.parse_args()
out=Path(args.run_root).resolve();out.relative_to(ROOT);out.mkdir(parents=True,exist_ok=True)
import importlib.util
spec=importlib.util.spec_from_file_location('extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');extract=importlib.util.module_from_spec(spec);spec.loader.exec_module(extract)
fields=[]
for p in (ROOT/'src/modules/AF.Module.Memory/Summary').glob('MemoryBusinessStateOwner*.cs'):
 for line in p.read_text(encoding='utf-8-sig').splitlines():
  if re.match(r'^\s*internal (?:Dictionary<.*>|List<.*>) \w+\s*=',line):fields.append(line)
identity_state=(ROOT/'src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.IdentityStores.cs').read_text(encoding='utf-8-sig')
identity_primary=extract.declaration(identity_state,'internal sealed partial class MemoryBusinessStateOwner')
for name in ['GetOrStartActiveNativeConversationMemorySessionId','GetCurrentNativeConversationMemorySessionIdForSuppression','BuildCurrentMemorySessionKey']:assert ' '+name+'(' in identity_primary,name
fields += [line for line in identity_state.splitlines() if re.match(r'^\s*internal (?:int|string) _(?:nativeConversationMemorySessionCounter|activeNativeConversationMemorySessionId|memoryRuntimeSessionKey)\b',line)]
source=(ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs').read_text(encoding='utf-8-sig')
a=source.index('\tinternal class DialogueDay');z=source.index('\n\t}',a)+4
daydto=source[a:z]
dto_extra={}
for name,marker in [('HeroShownRecord','@@SHOWN_RECORD@@'),('NpcPersonaProfile','@@PERSONA_PROFILE@@'),('EventRecordEntry','@@EVENT_RECORD@@')]:
 prefix='\tinternal sealed class ' if name in ['EventRecordEntry','EventMaterialReference'] else '\tinternal class '
 a=source.index(prefix+name);z=source.index('\n\t}',a)+4;dto_extra[marker]=source[a:z]
scene_source=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs').read_text(encoding='utf-8-sig')
a=scene_source.index('sealed class ScenePrepaidTransferRecord');a=scene_source.rfind('\n',0,a)+1;z=scene_source.index('\n\t}',a)+4
prepaid_dto=scene_source[a:z].replace('private sealed class','internal sealed class')
harness=(HERE/'Harness.cs.txt').read_text(encoding='utf-8').replace('@@STATE_FIELDS@@','\n'.join(fields)).replace('@@DIALOGUE_DAY@@',daydto)
for marker,value in dto_extra.items():harness=harness.replace(marker,value)
harness=harness.replace('@@SCENE_PREPAID_RECORD@@',prepaid_dto)
harness=harness.replace('@@ACTUAL_NOTICE_MAP_WRAPPER@@',next(line.strip() for line in source.splitlines() if line.strip().startswith('internal bool OpenWeeklyReportNoticeFromMap(')).replace('_weeklyNoticeGameAdapter','NoticeForFixture'))
patience_state=next(line for line in source.splitlines() if 'class PatienceState : PatienceRecord' in line).replace('private class','internal class')
a=source.index('class PatienceStateSaveModel');a=source.rfind('\n',0,a)+1;z=source.index('\n\t}',a)+4
harness=harness.replace('@@PATIENCE_DTOS@@',patience_state+'\n'+source[a:z].replace('private class','internal class'))
summary_source=(ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemorySummaryInput.cs').read_text(encoding='utf-8-sig')
summary_dtos=[]
for name in ['PartyTransferPromptEntry','SettlementTransferPromptEntry','WorldWeeklyReportHistoryEntry']:summary_dtos.append(extract.declaration(source,'public sealed class '+name))
for name in ['PartyTransferEntrySection','SettlementTransferEntrySection','SettlementTransferAssetKind']:summary_dtos.append(extract.declaration(source,'public enum '+name))
for name in ['MemorySummaryInput','CapturedMemorySummaryResult','MemorySummaryContextDependencies','MemorySummarySceneDependency']:
 a=summary_source.index('sealed class '+name);a=summary_source.rfind('\n',0,a)+1;z=summary_source.index('\n    }',a)+6
 summary_dtos.append(summary_source[a:z].replace('private sealed','internal sealed'))
a=source.index('internal sealed class ApiCallResult');a=source.rfind('\n',0,a)+1;z=source.index('\n\t}',a)+4
for name in ['PendingAutomaticKingdomRebellionContext','KingdomRebellionResolutionResult','RebelKingdomNamingResult','ClanVisualSnapshot','RebelFactionColorChoice','KingdomRebellionCandidateInfo','KingdomRebellionFollowerInfo','WeeklyReportRetryContext','WeeklyReportGenerationResult','DailySummaryQueueResult','MemorySummaryExecutionResult','MajorActionSummaryExecutionResult','MemoryOverviewExecutionResult']:
 summary_dtos.append(extract.declaration(source,'private sealed class '+name).replace('private sealed','internal sealed',1) if 'private sealed class '+name in source else extract.declaration(source,'internal sealed class '+name))
history_snapshot_source=(ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.HistoryPromptSnapshot.cs').read_text(encoding='utf-8-sig')
summary_dtos.append(extract.declaration(history_snapshot_source,'internal sealed class HistoryPromptSnapshot'))
for name,kind in [('DailyMaintenanceJob','sealed class'),('DailyMaintenanceTaskKind','enum')]:
 prefix='internal '+kind+' '+name if 'internal '+kind+' '+name in source else 'private '+kind+' '+name
 summary_dtos.append(extract.declaration(source,prefix).replace('private '+kind,'internal '+kind,1))
bulletin_host_source=(ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.WorldBulletin.cs').read_text(encoding='utf-8-sig')
summary_dtos.append(extract.declaration(bulletin_host_source,'private sealed class WorldBulletinDeathSnapshot').replace('private sealed','internal sealed',1) if 'private sealed class WorldBulletinDeathSnapshot' in bulletin_host_source else extract.declaration(bulletin_host_source,'internal sealed class WorldBulletinDeathSnapshot'))
harness=harness.replace('@@SUMMARY_DTOS@@','\n'.join(summary_dtos)+'\n'+source[a:z])
a=source.index('sealed class NpcActionFacts');a=source.rfind('\n',0,a)+1;z=source.index('\n\t}',a)+4
harness=harness.replace('@@ACTION_FACTS_DTO@@',source[a:z].replace('private sealed','internal sealed'))
state_source=(ROOT/'src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.cs').read_text(encoding='utf-8-sig')
actual_state_class=extract.declaration(state_source,'internal sealed partial class MemoryBusinessStateOwner')
for required in ['TryParseBestSummaryJsonObject','BuildSummaryJsonParseFailureMessage','CleanAIResponse','ContainsAnyIgnoreCase','HasCompressedMemoryBlock','IsNonHeroMemoryId','IsMemoryBlockIncludedInOverview','ResolveMaintenanceBudget','IsSceneShoutObserverHistoryLine','IsLoreInjectionHistoryLine','IsPlayerTurnStartLine','IsMeaningfulDirectConversationLine','IsMeaningfulConversationLine','IsDailyMemoryLinePublished']:
 assert ' '+required+'(' in actual_state_class, 'Actual primary class lacks method: '+required
parser_names=['IsMapEventNpcAction','IsPrisonerTakenAction','IsPrisonerReleasedAction','GetCapturedHeroId','GetReleasedHeroId','IsArmyCommanderDailyBehavior','IsDailyBehaviorRaid','IsDailyBehaviorDefend','TryParseBestSummaryJsonObject','TryParseTaggedSummaryObject','AddTaggedSummaryProperty','TryExtractTaggedBlock','TryParseLooseSummaryJsonObject','AddLooseJsonStringProperties','BuildRequiredJsonFieldDescription','BuildRequiredJsonFieldGroupDescription','HasAnyNonWhiteSpaceJsonProperty','BuildSummaryJsonParseFailureMessage']
methods=[]
pure_read_source=(ROOT/'src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.cs').read_text(encoding='utf-8-sig')
for sig in ['internal static void AppendPlayerExtraFactLine(', 'internal static List<string> BuildRenderedHistoryLines(', 'internal static bool IsCurrentInputPlayerLine(', 'internal static string EncounterDiagEscape(', 'internal static int ClampRecentDialogueTurns(', 'internal static int GetRecentDialogueTurnsFromSettings(']:methods.append(extract.declaration(pure_read_source,sig))
methods.append(extract.declaration(state_source,'internal static PromptTopicSemanticEvaluator CreateBuiltInTopicSemanticEvaluator('))
topic_router=(ROOT/'src/modules/AF.Module.Prompt/Composition/PromptBuiltInTopicRouter.cs').read_text(encoding='utf-8-sig')
harness+='\nnamespace AnimusForge {'+next(line for line in topic_router.splitlines() if 'internal delegate bool PromptTopicSemanticEvaluator(' in line)+'}\n'
for signature in ['internal int CountNonHeroDailyDraftLines(', 'internal static string StripActionTags(', 'internal static bool ContainsIgnoreCase(', 'internal static bool IsNpcAskingForConfirmation(']:methods.append(extract.declaration(state_source,signature))
party_codec_source=(ROOT/'src/modules/AF.Module.Economy/Execution/Party/PartyTransferExecutionContext.cs').read_text(encoding='utf-8-sig')
harness+='\nnamespace AnimusForge {'+extract.declaration(party_codec_source,'internal static class PartyTransferTagCodec')+'}\n'
for commit_name in ['ApplyDaily','ApplyOverview','ApplyMajor']:
 methods.append(extract.declaration(state_source,'internal bool '+commit_name+'('))
methods.append(extract.declaration(state_source,'private static bool Same('))

for name in parser_names:
 signature=next(line.strip() for line in state_source.splitlines() if ' '+name+'(' in line and 'internal static' in line)
 methods.append(extract.declaration(state_source,signature))
queue_source=(ROOT/'src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.Queues.cs').read_text(encoding='utf-8-sig')
for name in ['MarkMemoryOverviewDirty','EnqueueMemoryOverviewCandidateScanId','QueueDirtyMemoryOverviewCandidatesForDeferredScan','QueueAllMemoryOverviewCandidatesForDeferredScan','ProcessMemoryOverviewCandidateScanBudget','ShouldScanMemoryOverviewCandidates']:
 assert ' '+name+'(' in actual_state_class,name
 signature=next(line.strip() for line in state_source.splitlines() if line.strip().startswith('internal ') and ' '+name+'(' in line)
 methods.append(extract.declaration(state_source,signature))
methods += [line for line in state_source.splitlines() if line.strip().startswith('internal readonly ') and any(name in line for name in ['DirtyOverviewIds','OverviewCandidateIds','OverviewCandidateIdSet'])]
methods.append('internal long LastMemoryOverviewCandidateScanUtcTicks;')
methods.append('internal MemoryQueuePort QueuePort;')
methods += ['private MemorySealingOwner _sealing;','internal MemorySealingOwner Sealing => _sealing ??= new MemorySealingOwner(this);']
for sig in ['internal void TryEnqueueMemoryOverviewForAllCandidates(', 'internal bool TrySealPastDailyMemoryDrafts(']:methods.append(extract.declaration(state_source,sig))
methods.append(extract.declaration(state_source,'internal static string ConvertWeekNumberToChineseOrdinal('))
methods.append(extract.declaration(state_source,'internal string BuildHistoryContextById('))
methods.append(extract.declaration(state_source,'internal List<CompressedMemoryBlock> LoadBlocks('))
for sig in ['internal void TryEnqueueMemoryOverviewForMemoryId(', 'internal bool HasMemoryOverviewPendingBlocks(', 'internal MemoryOverviewState GetMemoryOverviewState(']:methods.append(extract.declaration(queue_source,sig))
methods.append(extract.declaration(queue_source,'internal bool CancelUnavailableHeroCompressionWorkById('))
methods.append(extract.declaration(queue_source,'internal static void GetMajorActionMaxCursor('))
for signature in ['internal MajorActionSummaryState GetMajorActionSummaryState(','internal static bool IsNpcActionAfterSummaryCursor(']:methods.append(extract.declaration(queue_source,signature))
for name in ['GetMemoryCandidateLimitFromSettings','GetMemoryFinalInjectCountFromSettings','GetMemoryPreprocessModeFromSettings','GetMemorySummaryRequestsPerMinuteFromSettings']:
 methods.append(extract.declaration(state_source,'internal static int '+name+'('))
methods.append(extract.declaration(state_source,'internal static bool IsNonHeroMemoryId('))
methods.append(extract.declaration(state_source,'internal bool HasCompressedMemoryBlock('))
methods.append(extract.declaration(state_source,'internal static bool IsMemoryBlockIncludedInOverview('))
methods.append(extract.declaration(state_source,'internal void ResolveMaintenanceBudget('))
methods.append(extract.declaration(state_source,'internal static string NormalizeWeeklyPromptKeyPart('))
methods.append(extract.declaration(state_source,'internal static bool HasMeaningfulConversationHistoryIncludingActiveScene('))
for name in ['IsSceneShoutObserverHistoryLine','IsLoreInjectionHistoryLine','IsPlayerTurnStartLine','IsMeaningfulDirectConversationLine','IsMeaningfulConversationLine']:
 signature=next(line.strip() for line in state_source.splitlines() if ' '+name+'(' in line and 'internal static' in line)
 methods.append(extract.declaration(state_source,signature))
identity_store_source=(ROOT/'src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.IdentityStores.cs').read_text(encoding='utf-8-sig')
methods += [line for line in identity_store_source.splitlines() if line.startswith('internal int ActionGlobalOrderCounter;') or line.startswith('internal int NextActionSequence()')]
methods.append('internal MemoryIdentityPort IdentityPort;')
methods.append(extract.declaration(identity_store_source,'internal void RemoveMemoryEntityDataById('))
for sig in ['internal void MergeMemoryEntityDataById(', 'internal static List<MyBehavior.DialogueDay> MergeDialogueDayLists(', 'internal void MergeNpcActionStorageById(']:methods.append(extract.declaration(identity_store_source,sig))
merge_identity_source=(ROOT/'src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.Identity.cs').read_text(encoding='utf-8-sig')
for sig in [l.strip() for l in merge_identity_source.splitlines() if l.strip().startswith('internal ') and '(' in l]:methods.append(extract.declaration(merge_identity_source,sig))
methods.append(extract.declaration(state_source,'internal void SaveBlocks('))
harness += '\nnamespace AnimusForge { '+extract.declaration(identity_store_source,'internal sealed class MemoryIdentityPort')+' }\n'
methods += ['internal bool MaintenanceCycleActive;','internal MemoryMaintenanceWorkBudget MaintenanceBudget;']
methods.append(next(line for line in state_source.splitlines() if 'const string NonHeroMemoryIdPrefix' in line))
begin=queue_source.index('internal bool HasBlock(')
methods.append(queue_source[begin:queue_source.index(';',begin)+1])
harness=harness.replace('@@SUMMARY_PARSER_METHODS@@','\n'.join(methods))
harness += '\nnamespace AnimusForge { '+extract.declaration(queue_source,'internal sealed class MemoryQueuePort')+' }\n'
political_source=(ROOT/'src/AF.GameAdapter.Bannerlord/Records/PoliticalDecisionRecordCaptureAdapter.cs').read_text(encoding='utf-8-sig')
harness += '\nnamespace AnimusForge {internal static class PoliticalDecisionRecordCaptureAdapter { '+extract.declaration(political_source,'internal static string LimitCustomPolicyWeeklyMaterialText(')+' }}\n'
harness += '\nnamespace AnimusForge { '+extract.declaration(state_source,'internal sealed class MemoryHistoryContextReadCapabilities')+' }\n'
army_narrative_source=(ROOT/'src/AF.GameAdapter.Bannerlord/Prompt/PersonaIdentityPromptCaptureAdapter.cs').read_text(encoding='utf-8-sig')
harness=harness.replace('@@C_SIEGE_START@@',extract.declaration(army_narrative_source,'internal static string BuildSiegeStartNarrative('))
notice_format_source=(ROOT/'src/AF.GameAdapter.Bannerlord/UI/Editors/WeeklyEditorProjection.cs').read_text(encoding='utf-8-sig')
harness += '\nnamespace AnimusForge {using static AnimusForge.MyBehavior; '+extract.declaration(notice_format_source,'internal class WeeklyEditorDisplayPort')+' internal static class WeeklyEditorProjection { '+extract.declaration(notice_format_source,'internal static string BuildWeeklyReportBrowserDefaultTitle(')+' } }\n'
notice_dto_source=(ROOT/'src/AF.GameAdapter.Bannerlord/UI/Weekly/AnimusForgeWeeklyReportMapNotification.cs').read_text(encoding='utf-8-sig')
harness += '\nnamespace AnimusForge {using TaleWorlds.Core;using TaleWorlds.Localization; '+extract.declaration(notice_dto_source,'internal sealed class AnimusForgeWeeklyReportMapNotification : InformationData')+' '+extract.declaration(notice_dto_source,'internal sealed class AnimusForgeWeeklyReportMapNotificationItemVM : MapNotificationItemBaseVM')+' }\n'
weekly_material_order=(ROOT/'src/modules/AF.Module.Weekly/Materials/WeeklyPromptMaterialOwner.MaterialCopies.cs').read_text(encoding='utf-8-sig')
social_status_source=(ROOT/'src/modules/AF.Module.Social/Notoriety/NotorietyConversationOutcomeReceipt.cs').read_text(encoding='utf-8-sig')
harness += '\nnamespace AnimusForge.Refactor.Runtime { '+extract.declaration(social_status_source,'internal enum NotorietyConversationOutcomeOperationStatus')+' }\n'
bulletin_presentation_source=(ROOT/'src/modules/AF.Module.Weekly/Generation/WorldBulletinStateOwner.Presentation.cs').read_text(encoding='utf-8-sig')
harness += '\nnamespace AnimusForge { '+extract.declaration(bulletin_presentation_source,'internal sealed class WorldBulletinLayout')+' }\n'
bulletin_owner_source=(ROOT/'src/modules/AF.Module.Weekly/Generation/WorldBulletinStateOwner.cs').read_text(encoding='utf-8-sig')
bulletin_capture_methods=[extract.declaration(bulletin_owner_source,sig) for sig in ['internal WorldBulletinSaveState EnsureWorldBulletinState(', 'internal bool CaptureWorldBulletinEvent(string kind, string key, int score, string sentence, bool involvesPlayer, string group, string detail, params', 'internal bool CaptureWorldBulletinEvent(string kind, string key, int score, string sentence, bool involvesPlayer, string group, string detail, WorldBulletinParticipant[]', 'internal void ApplyStability(', 'internal static bool IsWorldBulletinEnabled(', 'internal static bool IsWorldBulletinPublishingEnabled(']]
bulletin_capture_methods.append(extract.declaration(bulletin_owner_source,'internal static bool IsWorldBulletinEventId('))
bulletin_capture_fields='\n'.join(bulletin_owner_source.splitlines()[3:7])
harness += '\nnamespace AnimusForge { using static AnimusForge.MyBehavior; internal sealed class WorldBulletinStateOwner { '+bulletin_capture_fields+'\n'+'\n'.join(bulletin_capture_methods)+' } '+extract.declaration(bulletin_owner_source,'internal sealed class WorldBulletinPort')+' }\n'
harness += '\nnamespace AnimusForge { '+extract.declaration(bulletin_owner_source,'internal sealed class WorldBulletinPromptFacts')+' }\n'
planning_source=(ROOT/'src/modules/AF.Module.Memory/Summary/MemorySummaryPlanningOwner.cs').read_text(encoding='utf-8-sig')
harness += '\nnamespace AnimusForge { '+extract.declaration(planning_source,'internal sealed class MemorySummaryPlanEntry')+' }\n'
harness += '\nnamespace AnimusForge { '+extract.declaration(planning_source,'internal static class MemorySummaryPlanningRules')+' }\n'
weekly_line_source=(ROOT/'src/modules/AF.Module.Weekly/Materials/WeeklyAggregateEventLineOwner.cs').read_text(encoding='utf-8-sig')
harness=harness.replace('@@ACTION_KIND_TRANSLATOR@@',extract.declaration(weekly_line_source,'internal static string TranslateNpcActionKindForPrompt('))
editor_query_source=(ROOT/'src/AF.GameAdapter.Bannerlord/UI/Editors/EventEditorProjection.cs').read_text(encoding='utf-8-sig')
harness=harness.replace('@@EDITABLE_KINGDOM_QUERY@@',extract.declaration(editor_query_source,'internal static List<Kingdom> GetDevEditableKingdoms(')+'\n'+extract.declaration(editor_query_source,'internal static string BuildDevSummaryPreview('))
foothold_source=(ROOT/'src/AF.GameAdapter.Bannerlord/Composition/WeeklyMaterialValueBannerlordAdapter.cs').read_text(encoding='utf-8-sig')
reward_source=(ROOT/'src/modules/AF.Module.Economy/Host/RewardSystemBehavior.cs').read_text(encoding='utf-8-sig')
harness=harness.replace('@@REWARD_ITEM_INFO@@',extract.declaration(reward_source,'public class RewardItemInfo'))
capture_start=foothold_source.index('internal static WeeklyMaterialValuePort Capture(');capture_end=foothold_source.index('};',capture_start)+2
harness=harness.replace('@@WEEKLY_VALUE_CAPTURE@@',foothold_source[capture_start:capture_end])
harness=harness.replace('@@PLAYER_FOOTHOLD_QUERY@@',extract.declaration(foothold_source,'internal static bool TryResolvePlayerFoothold('))

vengeance_source=(ROOT/'src/bridges/Vengeance/Host/VengeanceRuntimeBridge.cs').read_text(encoding='utf-8-sig')
vengeance_dto=extract.declaration(vengeance_source,'internal sealed class VengeanceExecutionFacts')
vengeance_read_dto=extract.declaration(vengeance_dto,'private VengeanceExecutionFacts(')+'\n'+'\n'.join(line for line in vengeance_dto.splitlines() if line.strip().startswith('internal ') and '{ get; }' in line)
harness += '\nnamespace AnimusForge { internal sealed class VengeanceExecutionFacts { '+vengeance_read_dto+' } }\n'
retry_source=(ROOT/'src/AF.GameAdapter.Bannerlord/UI/Errors/LlmRetryPrompt.cs').read_text(encoding='utf-8-sig')
prefix_source=(ROOT/'src/AF.GameAdapter.Bannerlord/Prompt/MemoryRecallInputCaptureAdapter.cs').read_text(encoding='utf-8-sig')
harness=harness.replace('@@C_PLAYER_PREFIX@@',extract.declaration(prefix_source,'internal static bool TryStripPlayerSpeechPrefix('))
archive_source=(ROOT/'src/modules/AF.Module.Memory/Recall/HistoryArchiveRecallOwner.cs').read_text(encoding='utf-8-sig')
harness+='\nnamespace AnimusForge {'+extract.declaration(archive_source,'internal sealed class HistoryLineEntry')+'}\n'
identity_prompt_source=(ROOT/'src/AF.GameAdapter.Bannerlord/Prompt/PersonaIdentityPromptCaptureAdapter.cs').read_text(encoding='utf-8-sig')
harness=harness.replace('internal static class PersonaIdentityPromptCaptureAdapter {','internal static class PersonaIdentityPromptCaptureAdapter {'+extract.declaration(identity_prompt_source,'internal static string NormalizePlayerHistoryLineForPrompt('))
edit_source=(ROOT/'src/modules/AF.Module.Memory/ImportExport/MemoryDeveloperEditOwner.Daily.cs').read_text(encoding='utf-8-sig')
harness+='\nnamespace AnimusForge {internal static class MemoryDeveloperEditOwner {'+extract.declaration(edit_source,'internal static string NormalizeDialogueHistoryLineForDailyMemorySync(')+'}}\n'
harness=harness.replace('@@SYSTEM_FACT_PREDICATE@@',extract.declaration(archive_source,'internal static bool IsSystemFactLine('))
recall_source=(ROOT/'src/modules/AF.Module.Memory/Recall/MemoryRecallContextOwner.cs').read_text(encoding='utf-8-sig')
harness=harness.replace('@@RECALL_HOUR_FORMAT@@',extract.declaration(recall_source,'internal static string FormatMemoryHourRange('))
harness=harness.replace('@@LLM_FAILURE_TEXT@@',extract.declaration(retry_source,'public static string BuildFailureDetail(')+'\n'+extract.declaration(retry_source,'private static string NormalizeFullText('))

lifecycle=(ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.CampaignLifetime.cs').read_text(encoding='utf-8-sig')
a=lifecycle.index('    internal static void RetireConversationRequestsForDeveloperClear()');z=lifecycle.index('\n    }',a)+6
harness=harness.replace('@@CLEAR_RETIRE@@',lifecycle[a:z])

(out/'Program.cs').write_text(harness,encoding='utf-8')
paths=['src/modules/AF.Module.Weekly/Bulletin/WorldBulletinPolicy.cs','src/modules/AF.Module.Weekly/Materials/WorldBulletinCampaignMaterialPolicy.cs','src/modules/AF.Module.Kingdom/Scheduling/AutomaticKingdomRebellionOwner.cs','src/modules/AF.Module.Memory/Summary/MemoryRecoveryStateOwner.cs','src/modules/AF.Module.Memory/Summary/CooperativeMemoryQueueSort.cs','src/modules/AF.Module.Memory/Summary/MemorySealingOwner.cs','src/modules/AF.Module.Memory/Recovery/InteractionMemoryAuxiliaryCompletionCoordinator.cs','src/AF.GameAdapter.Bannerlord/Diagnostics/PerfProbe.cs','src/AF.Foundation.Runtime/Diagnostics/PerformanceWindow.cs','src/AF.GameAdapter.Bannerlord/Persistence/CampaignMemoryPersistenceAdapter.cs','src/AF.Persistence/CampaignSaveChunkHelper.cs','src/AF.Persistence/OwnerJsonStorageCodec.cs','src/modules/AF.Module.Memory/Records/MemoryPersistenceModels.cs','src/modules/AF.Module.Memory/Records/NpcActionEntry.cs']
paths += ['src/AF.GameAdapter.Bannerlord/Persistence/'+name+'.cs' for name in ['CampaignShownRecordPersistenceAdapter','CampaignPersonaPersistenceAdapter','CampaignNpcActionPersistenceAdapter','CampaignWeeklyRecordPersistenceAdapter','CampaignMaterialPersistenceAdapter']]
paths += ['src/modules/AF.Module.Persona/Profiles/PersonaProfileStateOwner.cs','src/modules/AF.Module.Economy/Presentation/ShownResourceRecordOwner.cs']
paths.append('src/modules/AF.Module.Economy/Negotiation/ScenePrepaidTransferRecordOwner.cs')
paths.append('src/modules/AF.Module.Weekly/Records/WeeklyEventRecordStateOwner.cs')
paths.append('src/AF.GameAdapter.Bannerlord/Memory/SceneDeferredHistoryFactOwner.cs')
paths += ['src/modules/AF.Module.Memory/History/SceneRevisitStateOwner.cs','src/AF.GameAdapter.Bannerlord/Persistence/CampaignSceneRevisitPersistenceAdapter.cs']
paths.append('src/AF.GameAdapter.Bannerlord/Memory/SceneRevisitRecordGameAdapter.cs')
paths += ['src/AF.GameAdapter.Bannerlord/Memory/MemoryEntityIdentityBannerlordAdapter.cs','src/AF.GameAdapter.Bannerlord/Records/CampaignCharacterRecordCaptureAdapter.cs']
harness += '\nnamespace AnimusForge { '+extract.declaration(state_source,'internal sealed class MemoryDailyCommitEffects')+' }\n'
paths += ['src/AF.GameAdapter.Bannerlord/Records/WeeklyTownStatCaptureAdapter.cs','src/modules/AF.Module.Weekly/Materials/WeeklyPoliticalMaterialPolicy.cs']
paths += ['src/modules/AF.Module.Weekly/Materials/WeeklyMemoryMaterialPolicy.cs']
paths += ['src/modules/AF.Module.Economy/Host/GiveAssetTagCodec.cs','AnimusForge.SiegeAftermathIntervention/SiegeActionTagCatalog.cs','AnimusForge.SiegeAftermathIntervention/SiegeInterventionActionKind.cs','AnimusForge.SiegeAftermathIntervention/LegacyTownTagAdapter.cs']
harness = harness.replace('@@TOWN_STAT_SNAPSHOT@@', extract.declaration(source,'private sealed class TownStatSnapshot').replace('private sealed','internal sealed',1) if 'private sealed class TownStatSnapshot' in source else extract.declaration(source,'internal sealed class TownStatSnapshot'))
paths += ['src/modules/AF.Module.Memory/Records/NpcActionRecordOwner.cs','src/AF.GameAdapter.Bannerlord/Records/CampaignBattleRecordCaptureAdapter.cs']
paths.append('src/modules/AF.Module.Persona/Generation/NpcPersonaTextRules.cs')
paths.append('src/modules/AF.Module.Prompt/Composition/PersonaIntroTextRules.cs')
paths += ['src/AF.GameAdapter.Bannerlord/Memory/MemorySummaryApplicationAdapter.cs','src/modules/AF.Module.Memory/Summary/MemorySummaryRunOwner.cs','src/modules/AF.Module.Memory/Summary/MemorySummaryAttemptRunner.cs','src/modules/AF.Module.Memory/Summary/MemorySummaryRules.cs','src/modules/AF.Module.Llm/Protocol/JsonResponseTextCodec.cs']
paths += ['src/AF.GameAdapter.Bannerlord/Persistence/CampaignMemoryRecoveryPersistenceAdapter.cs','src/modules/AF.Module.Memory/Recovery/InteractionMemoryRecoveryLedger.cs']
paths += ['src/AF.GameAdapter.Bannerlord/Persistence/CampaignExecutionTranscriptPersistenceAdapter.cs','src/modules/AF.Module.Memory/Records/ExecutionTranscriptStore.cs']
paths += ['src/AF.GameAdapter.Bannerlord/Persistence/CampaignWeeklyActionOutcomePersistenceAdapter.cs','src/modules/AF.Module.Weekly/Publication/WeeklyActionOutcomePublicationOwner.cs','src/modules/AF.Module.Weekly/Receipts/WeeklyMemoryMaterialOutcomeReceipt.cs','src/AF.Contracts/Internal/InteractionContracts.cs','src/AF.Contracts/Internal/LlmContracts.cs','src/AF.Contracts/Compatibility/Economy/EconomyRewardDebtContracts.cs']
paths += ['src/AF.GameAdapter.Bannerlord/Persistence/CampaignPatiencePersistenceAdapter.cs','src/modules/AF.Module.Social/Patience/PatienceOwner.cs','src/modules/AF.Module.Social/Patience/PatienceRules.cs']
paths += ['src/modules/AF.Module.Memory/Records/NpcActionLedger.cs','src/modules/AF.Module.Weekly/Scheduling/WeeklyAutoScheduleOwner.cs','src/modules/AF.Module.Weekly/Scheduling/WeeklyReportSchedulePolicy.cs']
paths.append('src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs')
paths += ['src/AF.GameAdapter.Bannerlord/Persistence/CampaignWeeklyNoticePersistenceAdapter.cs','src/modules/AF.Module.Weekly/Generation/WeeklyNoticeStateOwner.cs']
paths.append('src/AF.GameAdapter.Bannerlord/Persistence/CampaignVoicePersonaPersistenceAdapter.cs')
paths += ['src/AF.GameAdapter.Bannerlord/Persistence/CampaignKingdomPersistenceAdapter.cs','src/modules/AF.Module.Kingdom/Stability/KingdomStabilityPolicy.cs','src/modules/AF.Module.Kingdom/Rebellion/RebellionRules.cs']
paths.append('src/modules/AF.Module.Weekly/Models/WeeklyLegacyDtos.cs')
paths += ['src/modules/AF.Module.Memory/Records/CampaignMaterialRecordOwner.cs','src/modules/AF.Module.Memory/Records/EventSourceMaterialIndex.cs']
material_source=(ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.CampaignMaterialRecords.cs').read_text(encoding='utf-8-sig')
a=material_source.index('\tinternal sealed class EventSourceMaterialEntry');z=material_source.index('\n\t}',a)+4
harness=harness.replace('@@SOURCE_MATERIAL@@',material_source[a:z])
paths += ['src/AF.GameAdapter.Bannerlord/Weekly/WeekZeroOpeningSummaryGenerationController.cs','src/AF.GameAdapter.Bannerlord/Weekly/WorldBulletinEventCaptureAdapter.cs']
paths.append('src/AF.GameAdapter.Bannerlord/UI/CampaignSaveExitController.cs')
paths += ['src/AF.GameAdapter.Bannerlord/Weekly/WeeklyNoticeGameAdapter.cs','src/modules/AF.Module.Weekly/Materials/WeeklyReportTextHelper.cs']
paths.append('src/modules/AF.Module.Memory/Records/AnimusForgeDialogueHistoryEntry.cs')
paths.append('src/modules/AF.Module.Prompt/Composition/PromptRuleBlockText.cs')
paths.append('src/modules/AF.Module.Memory/Recovery/MemoryRecoverySeedRules.cs')
paths += ['src/AF.GameAdapter.Bannerlord/Memory/MemoryHistoryCommitBannerlordAdapter.cs','src/modules/AF.Module.Memory/Records/DialogueHistoryLedger.cs']
paths += ['src/modules/AF.Module.Prompt/Composition/UncompressedMemoryMessageAssemblyOwner.cs','src/modules/AF.Module.Conversation/Internal/History/ConversationMessage.cs']
paths += ['src/AF.GameAdapter.Bannerlord/Weekly/CampaignDailyMaintenanceController.cs','src/modules/AF.Module.Memory/Summary/MemoryMaintenanceWorkBudget.cs']
paths += ['src/modules/AF.Module.Weekly/Materials/WeeklyMaterialAggregationOwner.cs','src/modules/AF.Module.Weekly/Materials/WeeklyMaterialStageCursor.cs','src/modules/AF.Module.Weekly/Materials/WeeklyActionMaterialCursor.cs','src/modules/AF.Module.Weekly/Materials/WeeklyMaterialBatchPlanner.cs','src/modules/AF.Module.Weekly/Materials/WeeklyPromptMaterialOwner.cs','src/modules/AF.Module.Weekly/Materials/WeeklyPromptMaterialOwner.MaterialCopies.cs','src/modules/AF.Module.Weekly/Generation/WeeklyReportMaterialRevisionOwner.cs']
paths += ['src/AF.GameAdapter.Bannerlord/Kingdom/PlayerKingdomRebellionImmunity.cs','src/modules/AF.Module.Kingdom/Scheduling/KingdomMaintenanceOwner.cs']
paths.append('src/AF.GameAdapter.Bannerlord/Kingdom/KingdomRebellionGameAdapter.cs')
paths.append('src/modules/AF.Module.Kingdom/Rebellion/RebellionNamingRules.cs')
paths += ['src/modules/AF.Module.Kingdom/Rebellion/RebellionNamingOwner.cs','src/AF.GameAdapter.Bannerlord/Kingdom/KingdomRebellionRuntimeController.cs']
summary_actual=(ROOT/'src/AF.GameAdapter.Bannerlord/Prompt/NpcPersonaGenerationApplicationAdapter.cs').read_text(encoding='utf-8-sig')
(out/'RebelSummaryActual.cs').write_text('using System;using System.Collections.Generic;using System.Linq;using TaleWorlds.CampaignSystem;using TaleWorlds.CampaignSystem.Settlements;namespace AnimusForge.Refactor.Adapters;\n'+extract.declaration(summary_actual,'internal static class RebelNamingPromptSummaryCaptureAdapter'),encoding='utf-8')
paths += ['src/AF.GameAdapter.Bannerlord/Kingdom/KingdomStabilityGameAdapter.cs','src/modules/AF.Module.Kingdom/Stability/KingdomStabilityOwner.cs','src/AF.GameAdapter.Bannerlord/Persistence/CampaignCivilWarPersistenceAdapter.cs']
request_signature=next(line.strip() for line in source.splitlines() if 'sealed class WeekZeroShortSummaryRequest' in line)
request=extract.declaration(source,request_signature).replace('private sealed','internal sealed')
harness=harness.replace('@@WEEKZERO_REQUEST@@',request)
rules=(ROOT/'src/modules/AF.Module.Weekly/Generation/WeeklyGenerationRules.cs').read_text(encoding='utf-8-sig')
role=(ROOT/'src/modules/AF.Module.Prompt/Composition/ConversationRoleClassificationOwner.cs').read_text(encoding='utf-8-sig')
harness=harness.replace('@@ROLE_HISTORY_LEAVES@@','\n'.join(extract.declaration(role,sig) for sig in ['internal static int FindDialogueHistorySpeakerDelimiter(', 'internal static bool IsLikelyPlayerHistorySpeaker(']))
harness=harness.replace('@@MEMORY_SESSION_RULES@@','\n'.join(extract.declaration(identity_state,'internal '+sig) for sig in ['int GetOrStartActiveNativeConversationMemorySessionId(', 'int GetCurrentNativeConversationMemorySessionIdForSuppression(', 'string BuildCurrentMemorySessionKey('])+ '\n'+extract.declaration(state_source,'internal bool IsDailyMemoryLinePublished('))
weekly_trigger=(ROOT/'src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.WeeklyTriggers.cs').read_text(encoding='utf-8-sig')
harness=harness.replace('@@DAILY_APPEND_OWNER@@','\n'.join(extract.declaration(state_source,sig) for sig in ['internal bool AppendDailyMemoryLineById(', 'internal static int CountDailyMemoryDraftLines(', 'internal List<DailyMemoryDraft> LoadDrafts(', 'internal void SaveDrafts('])+ '\n'+ '\n'.join(extract.declaration(weekly_trigger,sig) for sig in ['internal bool AttachConfirmedWeeklyOutcome(', 'private static bool HasExactWeeklyActionOutcomeTrigger(', 'internal void StageWeeklyTrigger(', 'internal bool StageOrAttachWeeklyTrigger(', 'internal void AddWeeklyTrigger(', 'internal void AttachPendingWeeklyTriggers(', 'internal void PrunePendingWeeklyTriggers(']))
harness=harness.replace('@@DAILY_APPEND_CAPABILITIES@@',extract.declaration(state_source,'internal sealed class MemoryDailyAppendCapabilities'))
persona=(ROOT/'src/AF.GameAdapter.Bannerlord/Prompt/PersonaIdentityPromptCaptureAdapter.cs').read_text(encoding='utf-8-sig')
harness=harness.replace('@@C_ADDRESSED_INPUT@@',extract.declaration(persona,'internal static string BuildPlayerAddressedInputForName('))
weekly_import=(ROOT/'src/modules/AF.Module.Weekly/ImportExport/WeeklyEventDataImportOwner.cs').read_text(encoding='utf-8-sig')
harness=harness.replace('@@WEEKLY_IMPORT_RULES@@','\n'.join(extract.declaration(weekly_import,sig) for sig in ['internal static List<EventRecordEntry> SanitizeEventRecordEntries(', 'internal static string GetKingdomOpeningSummary(']))
archive=(ROOT/'src/modules/AF.Module.Weekly/Panel/WeeklyReportArchivePolicy.cs').read_text(encoding='utf-8-sig')
archive_start=archive.index('internal static List<string> NormalizeKingdomIds(')
harness=harness.replace('@@WEEKLY_KINGDOM_IDS@@',archive[archive_start:archive.index(';',archive_start)+1])
weekly_runtime=(ROOT/'src/modules/AF.Module.Weekly/Generation/WeeklyReportRuntimeOwner.cs').read_text(encoding='utf-8-sig')
harness=harness.replace('@@A8_PREVIEW_READ@@','\n'.join(extract.declaration(weekly_runtime,sig) for sig in ['internal DevWeeklyReportBatchPreviewEntry FindLatestWeeklyReportBatchDevPreview(', 'internal static string BuildWeeklyReportBatchPreviewKey(']))
harness+='\nnamespace AnimusForge { public partial class MyBehavior {'+extract.declaration(source,'internal sealed class DevWeeklyReportBatchPreviewEntry')+'}}\n'
harness=harness.replace('@@WEEKLY_QUEUE_PUMPS@@','\n'.join(extract.declaration(weekly_runtime,sig) for sig in ['internal static List<WeeklyEventMaterialPreviewGroup> BuildRemainingWeeklyReportGroupsForRetry(', 'internal async Task<ApiCallResult> CallWeeklyReportBatchApiAttemptAsync(', 'internal Task<List<Task<WeeklyReportBatchExecutionResult>>> EnqueueWeeklyWaveLaunchAsync(', 'internal bool ProcessPendingWeeklyWaveLaunches(', 'internal bool ProcessPendingWeeklyBatchApiAttempts(', 'internal bool ProcessPendingWeeklyPromptPreparations(']))
harness=harness.replace('@@WEEKLY_GROUP_REPORT_ID@@',next(line for line in weekly_runtime.splitlines() if 'internal static string BuildWeeklyReportGroupReportId(' in line))
harness += '\nnamespace AnimusForge {public partial class MyBehavior {'+'\n'.join(extract.declaration(source,('internal sealed class ' if 'internal sealed class '+n in source else 'private sealed class ')+n).replace('private sealed','internal sealed',1) for n in ['PendingWeeklyWaveLaunchContext','PendingWeeklyBatchApiAttemptContext','PendingWeeklyPromptPreparationContext','WeeklyReportBatchExecutionResult'])+extract.declaration(source,'internal enum WeeklyPromptPreparationResult')+'}}\n'
paths.append('src/modules/AF.Module.Weekly/Generation/WeeklyReportCommitQueueOwner.cs')
paths.append('src/modules/AF.Module.Weekly/Generation/WeeklyGenerationModels.cs')
paths.append('src/modules/AF.Module.Weekly/Generation/WeeklyFullReportCompletionOwner.cs')
paths.append('src/AF.Persistence/NpcDataFileName.cs')
paths.append('src/modules/AF.Module.Economy/Host/TransferQuantitySpec.cs')
paths.append('src/modules/AF.Module.Weekly/Materials/WeeklyMemoryMaterialValuePolicy.cs')
weekly_fact_source=(ROOT/'src/modules/AF.Module.Weekly/Materials/WeeklyAggregateEventLineOwner.cs').read_text(encoding='utf-8-sig')
harness=harness.replace('@@A8_NAMES@@',extract.declaration(weekly_fact_source,'internal static List<string> ResolveNames('))
weekly_ui_source=(ROOT/'src/AF.GameAdapter.Bannerlord/UI/Editors/WeeklyReportEditorController.cs').read_text(encoding='utf-8-sig')
harness+='\nnamespace AnimusForge {using static AnimusForge.MyBehavior; internal static class WeeklyReportEditorController {'+'\n'.join(extract.declaration(weekly_ui_source,sig) for sig in ['internal static void ShowWeeklyFullOnDemandProgressPopup(', 'internal static void ShowWeeklyFullOnDemandFailurePopup('])+'}}\n'
harness+='\nnamespace AnimusForge {'+extract.declaration(weekly_fact_source,'internal sealed class WeeklyHeroFact')+'}\n'
harness=harness.replace('@@WEEKLY_FULL_ACTUAL@@','\n'.join(extract.declaration(weekly_runtime,sig) for sig in ['internal static List<EventMaterialReference> CloneWeeklyReportMaterials(', 'internal static WeeklyEventMaterialPreviewGroup BuildWeeklyReportRecordPreviewGroup(', 'internal static string BuildWeeklyFullReportSourceState(', 'internal async Task<bool> GenerateWeeklyReportFullByEventIdAsync(', 'internal EventRecordEntry FindWeeklyReportRecordById(']))
harness=harness.replace('@@WEEKLY_FULL_ACTUAL_PARSER@@',extract.declaration(rules,'internal bool TryParseWeeklyFullOnDemandReportResponse(').replace('BuildFallbackWeeklyReportShortSummary(', 'WeeklyGenerationRules.BuildFallbackWeeklyReportShortSummary(').replace('NeutralizeWeeklyReportScenarioName(', 'WeeklyGenerationRules.NeutralizeWeeklyReportScenarioName('))
harness=harness.replace('@@DAILY_ACTUAL_BUDGET@@',extract.declaration(weekly_runtime,'internal static bool IsDailyMaintenanceBudgetExceeded('))
harness=harness.replace('@@PENDING_WEEKLY_ACTUAL_STAGE@@','\n'.join(extract.declaration(weekly_runtime,sig) for sig in ['internal void TryInitializePendingAutoWeeklyReportBuild(', 'internal void ProcessPendingAutoWeeklyReportBuildBudget(', 'internal void FinalizePendingAutoWeeklyReportBuild(', 'internal void StartAutoWeeklyReportsForWeek(', 'internal void TryStartDeferredAutoWeeklyReports(', 'internal async Task GenerateAutoWeeklyReportsAsync(', 'internal bool ProcessPendingAutoWeeklyReportPreviewGroupsBudget(', 'internal void ProcessPendingAutoWeeklyReportSourceMaterial(', 'internal bool ProcessPendingAutoWeeklyReportActionSlice(', 'internal void ProcessPendingAutoWeeklyReportNpcAction(', 'internal void ProcessPendingWeeklyReportAggregationBudget(', 'internal void ProcessPendingWeeklyReportPromptMaterialsBudget(', 'internal void ProcessPendingWeeklyReportBatchPromptsBudget(', 'internal void PrepareWeeklyReportBatchPrompt(', 'internal static bool IsWeeklyReportGroupEligible(', 'internal static List<WeeklyReportBatchRequest> BuildWeeklyReportBatchRequests(', 'internal static string BuildWeeklyReportBatchDisplayLabel('])+'\ninternal static string BuildWeeklyReportGroupDisplayLabel(MyBehavior.WeeklyEventMaterialPreviewGroup group) => group?.KingdomId ?? \"\";')
harness=harness.replace('@@PENDING_WEEKLY_DTO@@',extract.declaration(source, 'private sealed class PendingAutoWeeklyReportBuild' if 'private sealed class PendingAutoWeeklyReportBuild' in source else 'internal sealed class PendingAutoWeeklyReportBuild').replace('private sealed','internal sealed'))
harness=harness.replace('@@WEEKLY_ACTUAL_LOOKUP@@',extract.declaration(weekly_runtime,'internal EventRecordEntry FindWeeklyReportRecordById('))
weekly_prompt=(ROOT/'src/AF.GameAdapter.Bannerlord/Prompt/WeeklyPromptCaptureAdapter.cs').read_text(encoding='utf-8-sig')
harness=harness.replace('@@C_WEEKLY_SNAPSHOT_RULES@@','\n'.join(extract.declaration(weekly_prompt,sig) for sig in ['internal static Dictionary<string, WeeklyPromptReportSnapshot> CaptureLatestWeeklyPromptReportSnapshots(', 'internal static WeeklyPromptReportSnapshot CreateWeeklyPromptReportSnapshot(', 'internal static bool IsNewerWeeklyPromptReportSnapshot(']))
harness=harness.replace('@@WEEKLY_REPORT_SNAPSHOT@@',extract.declaration(source,'internal sealed class WeeklyPromptReportSnapshot'))
harness=harness.replace('@@WEEKLY_PROMPT_SNAPSHOT@@',extract.declaration(source,'public sealed class WeeklyPromptSnapshot'))
harness=harness.replace('@@SAVE_EXIT_ENUMS@@','\n'.join(extract.declaration(source,'internal enum '+n) if 'internal enum '+n in source else extract.declaration(source,'private enum '+n).replace('private enum','internal enum') for n in ['SaveAndExitStage','SaveAndExitReason']))
harness=harness.replace('@@WEEKZERO_RULES@@','\n'.join(extract.declaration(rules,signature) for signature in ['internal static List<string> BuildWeeklyBatchExpectedReportIds(', 'internal static bool IsWeeklyReportBatchPromptPrepared(', 'internal static string BuildWeeklyReportGroupReportId(', 'internal static int GetWeeklyReportStabilityDeltaForTag(', 'internal static int ExtractWeeklyReportStabilityDelta(', 'internal static string BuildFallbackWeeklyReportShortSummary(', 'internal static string NeutralizeWeeklyReportScenarioName(', 'internal static string NormalizeWeeklyReportTagText(']))
import json
legacy=json.loads((ROOT/'artifacts/j17-host-implementation-20261004/a/weekzero-original-methods.json').read_text(encoding='utf-8'))
harness=harness.replace('@@LEGACY_WEEKZERO@@','\n'.join(legacy[n].replace('private Task<bool>','internal Task<bool>') for n in ['ComputeWeekZeroShortSummarySourceHash','BuildWeekZeroPromptText','ApplyWeekZeroShortSummaryOnMainThreadAsync']))
(out/'Program.cs').write_text(harness,encoding='utf-8')
reference=ROOT/'local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll'
(out/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><NoWarn>CS0649</NoWarn><DefineConstants>'+('BANNERLORD_1_4_OR_GREATER' if args.api=='1.4' else '')+'</DefineConstants></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+str(ROOT/p)+'"/>' for p in paths)+'<Reference Include="Newtonsoft.Json"><HintPath>'+str(reference)+'</HintPath></Reference></ItemGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet=ROOT/'local/dotnet/8.0.425/dotnet.exe';result=subprocess.run([str(dotnet),'run','--project',str(out/'Tests.csproj'),'-c','Release'],cwd=out,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90)
(out/'run.log').write_text(result.stdout+result.stderr,encoding='utf-8');print(result.stdout+result.stderr,end='');raise SystemExit(result.returncode)

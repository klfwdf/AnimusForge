from pathlib import Path
import sys as _legacy_sys
from pathlib import Path as _LegacyPath
_legacy_sys.path.insert(0, str(_LegacyPath(__file__).resolve().parents[4] / "tests"))
from af2_terminal_migration_review import historical_source
# Explicit legacy oracle; current owner build/replay inputs are not projected.
AF2_FIXTURE_METADATA = {'sourceClass': 'legacy-oracle-extraction', 'terminalBindingAndExactInverseRequired': True, 'currentOwnerReplayProjected': False}
import argparse, importlib.util, os, re
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
import sys
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import current_source_path
import sys
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root
p=argparse.ArgumentParser();p.add_argument('--current-consumer',action='store_true');p.add_argument('--api-line',choices=['1.3','1.4'],default='1.3');p.add_argument('--old',action='store_true');p.add_argument('--mutate',choices=['drop-failure','ignore-run','old-fallback','keep-stale-tags']);p.add_argument('--output-name');p.add_argument('--run-root',type=Path);a=p.parse_args()
if a.output_name is not None and not re.fullmatch(r'[A-Za-z0-9_-]+',a.output_name):p.error('Invalid output name')
def load(n,p):
 sp=importlib.util.spec_from_file_location(n,p);m=importlib.util.module_from_spec(sp);sp.loader.exec_module(m);return m
ex=load('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');util=load('util',ROOT/'tests/AF.Contracts/ModuleFrameworkApiTests/run.py')
inverse=load('liveness_inverse',HERE/'liveness_review.py')
def source(path):return inverse.old_source(path) if a.old else historical_source(path)

if a.current_consumer:
 # Explicit current mode reuses the existing Native/game-leaf closure, not its unsupported Courier stub.
 # Historical oracle sources, shortened timers and truncated commit bodies are never inputs here.
 import json,hashlib,subprocess,xml.etree.ElementTree as ET
 from output_isolation import resolve_dotnet,minimal_test_environment
 test_paths=[HERE/'run_liveness.py',HERE/'LivenessHooks.cs.txt',HERE/'LivenessCases.cs.txt']
 test_inputs=[{'path':path.relative_to(ROOT).as_posix(),'rawSha256':hashlib.sha256(path.read_bytes()).hexdigest()} for path in test_paths]
 if a.old or a.mutate or a.output_name: p.error('current consumer requires --run-root and forbids legacy inverse/mutants')
 if not a.run_root: p.error('current consumer requires a new explicit --run-root')
 d=load('native_current_leaf_closure',ROOT/'tests/modules/AF.Module.Conversation/NativeModuleSubmissionTests/current_consumer.py')
 # The current My capability was made internal for the typed C capture consumer.
 # Adapt only the seed runner's extraction selector in memory; source bodies and D files stay unchanged.
 seed_driver=ROOT/'tests/modules/AF.Module.Conversation/NativeModuleSubmissionTests/current_consumer.py'
 seed_script=seed_driver.read_text(encoding='utf-8').replace("'private static SharedPromptCaptureBannerlordAdapter.HistoryWorkCapturePorts ResolveHistoryWorkCapturePorts('","'internal static SharedPromptCaptureBannerlordAdapter.HistoryWorkCapturePorts ResolveHistoryWorkCapturePorts('")
 exec(compile(seed_script,str(seed_driver),'exec'),d.__dict__)

 seed=a.run_root.resolve()/'native-game-leaf-closure'
 import contextlib,io
 native_output=io.StringIO()
 with contextlib.redirect_stdout(native_output): baseline=d.verify(seed,api_line=a.api_line)
 (seed/'native-baseline-console.log').write_text(native_output.getvalue(),encoding='utf-8')
 print('Current Native leaf-closure exit='+str(baseline)+' output='+str(seed))
 # A changed C game-capture body may require additional engine scalar leaves in the combined Courier fixture.
 # Preserve the baseline failure; final Courier compilation/execution is the proof boundary.
 records=[]
 seed_source_text='\n'.join(path.read_text(encoding='utf-8-sig') for path in sorted(seed.glob('*.cs')))
 reused_seed_declarations=[]
 def missing_seed_declaration(body):
  if body in seed_source_text:
   reused_seed_declarations.append({'bodySha256':hashlib.sha256(body.encode('utf-8')).hexdigest(),'signature':body.splitlines()[0],'reason':'identical actual current declaration already supplied by Native seed'})
   return ''
  return body
 def current_text(name):
  path=ROOT/'src/modules/AF.Module.Conversation/Channels/Courier'/('CourierDeliveryBehavior'+name+'.cs')
  text=path.read_text(encoding='utf-8-sig')
  if name=='.SessionTransport': assert hashlib.sha256(path.read_bytes()).hexdigest()=='a393a98b42c1f2d37150578d5d5ede170540be8eb4874751fbc2821eb9cfe23e','protected dirty transport changed'
  records.append({'path':path.relative_to(ROOT).as_posix(),'rawSha256':hashlib.sha256(path.read_bytes()).hexdigest()})
  return text
 def exact(text,sig):
  start=text.index(sig);brace=text.find('{',start);arrow=text.find('=>',start)
  return text[start:text.index(';',arrow)+1] if arrow>=0 and (brace<0 or arrow<brace) else ex.declaration(text,sig)
 main=current_text('')
 dtos='\n'.join(exact(main,'private '+typ+' '+name) for typ,name in [('enum','CourierStage'),('enum','CourierPayloadMode'),('sealed class','CourierSession'),('sealed class','CourierCargoEntry'),('sealed class','CourierReplyGenerationRequest'),('sealed class','InboundLetterGenerationRequest'),('sealed class','CourierRoutePlan'),('sealed class','CourierLetterInventoryRecord'),('sealed class','PendingCourierFlow'),('sealed class','CourierTradeOption')])
 consts='\n'.join(line for line in main.splitlines() if line.strip().startswith('private const '))
 registry=current_text('.SessionRegistry')
 registry_method=exact(registry,'private CourierSession GetSessionById(')
 life=current_text('.CampaignLifetime')
 life_field=life[life.index('    private readonly Dictionary<CourierSession, ConversationRequestLifetime>'):life.index('    private ConversationRequestLifetime BeginCourierRequestLifetime(')]
 life_methods='\n'.join(exact(life,sig) for sig in ['private ConversationRequestLifetime BeginCourierRequestLifetime(','private void RetireCourierRequestLifetime(','private void RetireCourierRequestLifetimes('])
 phase=exact(current_text('.DetachedPostprocess'),'private async Task<T> RunCourierOwnerPhaseAsync<T>(')
 runtime=exact(current_text('.RuntimeTick'),'private static void EnqueueMainThreadActionForGeneration(')
 extra='\n'.join(exact(registry,sig) for sig in ['private static CourierStage ParseStage(','private static CourierPayloadMode ParsePayloadMode(','private static bool IsTerminalStage(','private static void NormalizeSession(','private static bool IsInboundToPlayer(','private static string NormalizeCourierDirection(','private static string SafeHeroId('])
 extra+='\n'+'\n'.join(exact(main,sig) for sig in ['private static int ClampInt(','private static string PrepareNpcReplyForActionPostprocess(','private static string CleanNpcReply(','private static string NormalizeInboundLetterText(','private static bool LooksLikeApiError(','private static string StripCourierActionTags(','private static List<string> MergeCourierSelectedRuleIds(','private static List<string> ExcludeCourierSelectedRuleIds('])
 # Immutable exclusions and original dictionary/gate identity are necessary runtime inputs.
 extra+='\n'+next(line for line in main.splitlines() if 'private static readonly string[] CourierExcludedRuleIds' in line)
 state_fields='\n'.join(line for line in main.splitlines() if line.strip().startswith('private readonly ') and (' _sessions ' in line or ' _sessionLock ' in line))
 extra+='\n'+state_fields+'\n'+'\n'.join(line for line in main.splitlines() if line.strip().startswith('private ') and '_courierReplyWait' in line and ';' in line)
 # Preserve complete private receipt bookkeeping and the original same-session/generation checks.
 module=current_text('.ModuleSubmission')
 module_fields=module[module.index('    private readonly object _moduleCourierGate'):module.index('    // Still an internal entry:')]
 extra+='\n'+exact(module,'private sealed class ModuleCourierRequest')+module_fields
 extra+='\n'+'\n'.join(exact(module,sig) for sig in ['private ModuleCourierRequest FindModuleCourierRequest(','private void RecordModuleCourierStep(','private void FailModuleCourierSession(','private void CompleteModuleCourierTransport(','private void CheckModuleCourierSessionIdentity(','private bool ConfirmModuleCourierPayload('])
 extra+='\n'+exact(current_text('.DraftAdmission'),'private static Tuple<string, string, string> CourierCargoIdentity(')
 extra+='\n'+'\n'.join(exact(registry,sig) for sig in ['private Hero ResolveRecipient(','private Hero ResolveSender(','private static Hero ResolveHeroByIdForCourier(','private MobileParty ResolveCourierParty('])
 extra+='\n'+next(line for line in main.splitlines() if 'private readonly Dictionary<string, MobileParty> _courierPartyCache' in line)
 extra+='\n'+exact(registry,'private void RemoveCourierRuntimeIndex(')
 extra+='\n'+next(line for line in main.splitlines() if 'private Dictionary<string, string> _npcLetterLastDeliveredEventKeyBySender' in line)
 extra+='\n'+exact(main,'private static int EstimateCourierItemUnitValue(')
 # Close remaining actual Courier helpers indicated by the previous compiler, never replace AF algorithms with leaves.
 missing=set()
 linked_suffixes={'.PromptPreparation','.PromptSchedule','.HistoryPreparation','.PreparationAdmission','.GenerationLifecycle','.SessionTransport','.CommitDispatch','.DomainCommit','.PromptMessages','.ReplyWait','.DeliveryLifetime','.RouteTransport','.InboundCompletion','.DeliveredMemory','.DetachedPostprocess'}
 for suffix in linked_suffixes:
  linked_text=(ROOT/'src/modules/AF.Module.Conversation/Channels/Courier'/('CourierDeliveryBehavior'+suffix+'.cs')).read_text(encoding='utf-8-sig')
  missing.update(re.findall(r'\b([A-Za-z_][A-Za-z0-9_]*)\b',linked_text))
 # Close the transitive real-method graph once, using current source declarations only.
 helper_methods=[]
 for helper_path in sorted((ROOT/'src/modules/AF.Module.Conversation/Channels/Courier').glob('CourierDeliveryBehavior*.cs')):
  suffix=helper_path.stem.removeprefix('CourierDeliveryBehavior')
  if suffix in linked_suffixes: continue
  helper_text=helper_path.read_text(encoding='utf-8-sig')
  for match in re.finditer(r'(?m)^\s*(?:private|internal|public) (?:static )?(?:async )?[^\n{};=]+ ([A-Za-z_][A-Za-z0-9_]*)\(',helper_text):
   name=match.group(1)
   helper_methods.append((name,exact(helper_text[match.start():],match.group(0).strip()),helper_path))
 selected=[]
 changed=True
 while changed:
  changed=False
  for name,body,path in helper_methods:
   if name not in missing or body in extra or body in selected or body in registry_method or body in life_methods or body in phase or body in runtime: continue
   selected.append(body);changed=True
   missing.update(re.findall(r'\b([A-Za-z_][A-Za-z0-9_]*)\b',body))
   records.append({'path':path.relative_to(ROOT).as_posix(),'rawSha256':hashlib.sha256(path.read_bytes()).hexdigest(),'symbol':name,'layer':'current-exact-Courier-helper-declarations'})
 extra+='\n'+'\n'.join(selected)
 # Preserve exact current runtime field identities needed by transport/acceptance.
 for field in ['_courierReplyRegisteredMapNotificationView','LastCourierLogicPulseTicks','_hasPotentialActiveCourierPartiesForAi','_activeCourierPartyIdsSnapshot','_courierRuntimeIndexesReady','_courierInboundCompletionScanCursor','_courierLetterInventoryRecords','_courierLetterInventoryStorage','LastTrackerEventPulseTicks','CourierMapEventMarkers','_pendingFlow','_letterInputOpen']:
  extra+='\n'+next(line for line in main.splitlines() if re.search(r'\b'+re.escape(field)+r'\b',line) and line.strip().startswith('private '))

 draft=current_text('.DraftAdmission')
 extra+='\n'+exact(main,'private sealed class CourierMapEventMarker')+'\n'+exact(draft,'private sealed class CourierDraftTicket')
 extra+='\n'+draft[draft.index('    private readonly object _courierDraftTicketGate'):draft.index('    // Capture is main-thread only')]
 extra+='\n'+'\n'.join(body for body in (exact(main,sig) for sig in ['private static void Log(','private static void LogVerbose(','private static bool ContainsVassalageActionTag(','private static bool ContainsKingdomAnnexActionTag(','private static void LogCourierContextAlignment(','private static RewardSystemBehavior.RpItemIntroductionContext CreateCourierRpItemIntroductionContext(','private static string AppendCourierPlayerRecentActions(','private static bool IsCourierBridgeEnabled(']) if body not in extra)
 my=(ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs').read_text(encoding='utf-8-sig')
 records.append({'path':'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs','rawSha256':hashlib.sha256((ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs').read_bytes()).hexdigest(),'layer':'current-exact-Courier/My-facades'})
 # The current NpcAction owner references the real nested CampaignMaterial DTO.
 # Preserve that identity rather than flattening it into a synthetic top-level type.
 material_path=ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.CampaignMaterialRecords.cs'
 material_source=material_path.read_text(encoding='utf-8-sig')
 material_dto=exact(material_source,'internal sealed class EventSourceMaterialEntry')
 (seed/'CourierCampaignMaterialIdentity.cs').write_text('namespace AnimusForge;internal sealed partial class MyBehavior {'+missing_seed_declaration(material_dto)+'}',encoding='utf-8')
 records.append({'path':material_path.relative_to(ROOT).as_posix(),'rawSha256':hashlib.sha256(material_path.read_bytes()).hexdigest(),'symbol':'MyBehavior.EventSourceMaterialEntry','bodySha256':hashlib.sha256(material_dto.encode('utf-8')).hexdigest(),'layer':'actual complete unchanged nested runtime DTO identity'})
 my_wrappers='\n'.join(exact(my,sig) for sig in ['internal CourierPreprocessRequest BeginCourierRulePreprocess(','internal List<string> RunCourierRulePreprocessRetrieval(','internal static ShoutPromptContext CreateEmptyShoutPromptContext('])
 my_more=['public static string ResolveCurrentMemorySceneLabelForExternal(','public static string BuildPlayerPublicDisplayNameForExternal(', 'public static string BuildHistoryContextForExternal(','private string BuildHistoryContext(','public static string BuildRecentNpcFactContextForExternal(','private string BuildRecentNpcFactContext(','public static string BuildCurrentDateFactForExternal(','public static string AppendPlayerCustomPromptRuleToSystemPromptForExternal(','public static ShoutPromptContext BuildShoutPromptContextForExternal(','private ShoutPromptContext BuildShoutPromptContextForExternalInternal(','private List<string> RunCourierRulePreprocessInternal(','private static string GetMemoryHeroId(','private string BuildHistoryContextById(','private static string ResolveCurrentMemorySceneLabel(','private string BuildCurrentDateFactForPrompt(','public static int TransferItemsFromRosterByStringId(','public static void RecordShownResourcesForExternal(','public static int GetRemainingShowableGoldForExternal(','public static int GetRemainingShowableItemCountForExternal(','private void RecordShownResources(','private int GetRemainingShowableGold(','private int GetRemainingShowableItemCount(','private static string ResolveShownRecordKey(','private static string NormalizeShownRecordKey(']
 my_wrappers+='\n'+'\n'.join(missing_seed_declaration(exact(my,sig)) for sig in my_more)
 external_root_path=ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.PromptContextCapture.cs'
 external_root=external_root_path.read_text(encoding='utf-8-sig')
 start=external_root.index('    private SharedPromptCaptureBannerlordAdapter.ExternalPromptBuildPorts _externalPromptBuildCapture;')
 end=external_root.index('    private PromptContextCaptureBannerlordPorts CreatePromptContextCapturePorts(',start)
 my_wrappers+='\n'+external_root[start:end]
 records.append({'path':external_root_path.relative_to(ROOT).as_posix(),'rawSha256':hashlib.sha256(external_root_path.read_bytes()).hexdigest(),'layer':'current-exact-narrow ExternalPromptBuild cached ports factory; no new facade stub'})

 my_wrappers+='\n'+exact(my,'public static string BuildPlayerPublicDisplayNameForExternal(Hero observer)')+exact(my,'public static string BuildNpcPlayerKinshipPromptLineForExternal(')+'\n'+''.join(exact(my[m.start():],m.group()) for m in re.finditer(r'public static string BuildPlayerCourier(?:Sender|Recipient)IdentityForExternal\(',my))
 my_wrappers+='private readonly ShownResourceRecordOwner _shownResourceRecords=new();'+exact(my,'internal class HeroShownRecord')
 party_facade_path=ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.PartyAssetTransfers.cs';party_facade=party_facade_path.read_text(encoding='utf-8-sig')
 my_wrappers+='\n'+exact(party_facade,'public static bool IsPartyTransferLordEligibleForExternal(')+exact(party_facade,'public static bool IsSettlementTransferLeaderEligibleForExternal(')
 records.append({'path':party_facade_path.relative_to(ROOT).as_posix(),'rawSha256':hashlib.sha256(party_facade_path.read_bytes()).hexdigest(),'layer':'actual-forwarding-external-gameplay-module-boundaries'})
 my_wrappers+='\n'+exact(my,'public static List<ConversationMessage> BuildUncompressedMemoryRoleMessagesForExternal(')
 uncompressed_root=(ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.UncompressedMemoryPrompt.cs').read_text(encoding='utf-8-sig')
 my_wrappers+='\n'+exact(uncompressed_root,'private List<ConversationMessage> CaptureAndBuildUncompressedMemoryRoleMessages(')+exact(my,'private List<ConversationMessage> BuildUncompressedMemoryRoleMessages(')
 my_wrappers+='\n'+exact(my,'public static string BuildRuleTargetKeyForExternal(')+exact(my,'private static string ResolveRuleTargetKey(')
 for name in ['RunSharedPromptRouting','RunSharedKnowledgeRetrieval']:
  for match in re.finditer(r'(?:private|internal) (?:static )?void '+name+r'\(PromptBuildPhases',my):my_wrappers+='\n'+exact(my[match.start():],match.group())
 # Both overloaded Courier routing boundaries are compiled with their current complete bodies.
 for match in re.finditer(r'public static List<string> RunCourierRulePreprocessForExternal\(',my):my_wrappers+='\n'+exact(my[match.start():],match.group())

 scene_current=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs').read_text(encoding='utf-8-sig')
 retry_current=(ROOT/'src/AF.GameAdapter.Bannerlord/UI/Errors/LlmRetryPrompt.cs').read_text(encoding='utf-8-sig')
 facade_helpers='public partial class ShoutBehavior {'+'\n'.join(exact(scene_current,sig) for sig in ['public static bool HasInjectedRuleBlockForExternal(','internal static bool HasPreprocessRuleHitForExternal('])+'}internal static partial class LlmRetryPrompt {'+'\n'.join(exact(retry_current,sig) for sig in ['public static bool IsRetryableLlmError(','public static string BuildRetryDescription('])+'}'
 (seed/'CourierCurrentPolicyFacades.cs').write_text('using System;using System.Collections.Generic;using System.Linq;namespace AnimusForge;'+facade_helpers,encoding='utf-8')
 (seed/'CourierMyCurrentPrompt.cs').write_text('using System;using System.Collections.Generic;using System.Linq;using TaleWorlds.CampaignSystem;using TaleWorlds.CampaignSystem.Roster;using TaleWorlds.Library;using AnimusForge.Refactor.Contracts;using AnimusForge.Refactor.Runtime;using AnimusForge.Refactor.Adapters;namespace AnimusForge;internal sealed partial class MyBehavior {'+my_wrappers+'}',encoding='utf-8')
 bootstrap='using System;using System.Collections.Generic;using System.Collections.Concurrent;using System.Linq;using System.Text;using System.Text.RegularExpressions;using System.Threading;using System.Threading.Tasks;using System.Runtime.CompilerServices;using TaleWorlds.CampaignSystem;using TaleWorlds.CampaignSystem.Party;using TaleWorlds.CampaignSystem.Roster;using TaleWorlds.Library;using TaleWorlds.Core;using AnimusForge.Refactor.Adapters;using AnimusForge.Refactor.Contracts;using AnimusForge.Refactor.Modules;using AnimusForge.Refactor.Runtime;namespace AnimusForge;public sealed partial class CourierDeliveryBehavior {'+consts+dtos+registry_method+life_field+life_methods+phase+runtime+extra+'}'
 (seed/'CourierRuntimeIdentity.cs').write_text(bootstrap,encoding='utf-8')
 # Link complete current scheduling, producer, completion, transport and delivery-acceptance algorithms.
 for suffix in ['.PromptPreparation','.PromptSchedule','.HistoryPreparation','.PreparationAdmission','.GenerationLifecycle','.SessionTransport','.CommitDispatch','.DomainCommit','.PromptMessages','.ReplyWait','.DeliveryLifetime','.RouteTransport','.InboundCompletion','.DeliveredMemory','.DetachedPostprocess']:
  text=current_text(suffix)
  if suffix=='.DetachedPostprocess': text=text.replace(phase,'') # Same full phase body is already compiled in RuntimeIdentity.
  if suffix=='.PromptMessages': text=text.replace(exact(text,'private static string BuildPendingPayloadSummary('),'') # UI pending-menu rendering is not reachable by the current session consumer.
  # The relevant original imports are supplied by the shared bootstrap/game leaf closure.
  imports='using System;using System.Collections.Generic;using System.Collections.Concurrent;using System.Diagnostics;using System.Linq;using System.Text;using System.Text.RegularExpressions;using System.Threading;using System.Threading.Tasks;using System.Runtime.CompilerServices;using TaleWorlds.CampaignSystem;using TaleWorlds.CampaignSystem.Party;using TaleWorlds.CampaignSystem.Roster;using TaleWorlds.Library;using TaleWorlds.Core;using TaleWorlds.ModuleManager;using AnimusForge.Refactor.Adapters;using AnimusForge.Refactor.Contracts;using AnimusForge.Refactor.Modules;using AnimusForge.Refactor.Runtime;\n'
  (seed/('Courier'+suffix+'.cs')).write_text(imports+text[text.index('namespace AnimusForge;'):],encoding='utf-8')
 metadata=seed/'MetadataLeaves.cs';text=metadata.read_text(encoding='utf-8');text=text.replace(exact(text,'internal static class CourierDeliveryBehavior'),'');metadata.write_text(text,encoding='utf-8')
 host=seed/'Host.cs';text=host.read_text(encoding='utf-8');text=text.replace('public static int Main()','public static int NativeMain()',1)
 for definition in ['public class Hero','public class CharacterObject','public class MobileParty','public class PartyBase','public class Settlement','public struct CampaignVec2','public class Campaign','public static class InformationManager','public struct EquipmentElement','public class ItemObject','public class Clan','public class Kingdom','public struct CampaignTime']: text=text.replace(definition,definition.replace('class ','partial class ').replace('struct ','partial struct '))
 text=text.replace('internal sealed class SceneHistoryPromptCaptureAdapter','internal sealed partial class SceneHistoryPromptCaptureAdapter')
 text=text.replace('public partial class CharacterObject : BasicCharacterObject {','public partial class CharacterObject : BasicCharacterObject {public bool IsPlayerCharacter;')
 text=text.replace('internal static class PlayerNotorietyBehavior {','internal static partial class PlayerNotorietyBehavior {')
 text=text.replace('static class Logger {','static class Logger {internal static bool IsModLogicEnabled=>false;')
 text=text.replace('internal static void BuildCurrentSettlementHeroNpcLineForPrompt(){', 'internal static string BuildCurrentSettlementHeroNpcLineForPrompt(){return "";').replace('internal static void BuildCurrentSettlementHeroNpcLineForPrompt()=>', 'internal static string BuildCurrentSettlementHeroNpcLineForPrompt()=>')
 exception_path=ROOT/'src/modules/AF.Module.Prompt/Composition/PreprocessFormatException.cs'
 exception_source=exception_path.read_text(encoding='utf-8-sig')
 text=text.replace('class PreprocessFormatException : Exception { }',exact(exception_source,'public sealed class PreprocessFormatException'))
 records.append({'path':exception_path.relative_to(ROOT).as_posix(),'rawSha256':hashlib.sha256(exception_path.read_bytes()).hexdigest(),'layer':'actual complete public exception identity/constructor'})
 text=text.replace('internal static class AIConfigHandler {','internal static partial class AIConfigHandler {')
 text=text.replace('internal static void ClearGuardrailRuntimeTarget() {}','internal static void ClearGuardrailRuntimeTarget() {CourierCurrentContextProbe.Clears++;}')
 text=text.replace('internal static class PartyAssetTransferBannerlordAdapter {','internal static partial class PartyAssetTransferBannerlordAdapter {')
 text=text.replace('internal static class LegacyInteractionSnapshotAdapters {','internal static partial class LegacyInteractionSnapshotAdapters {')
 text=text.replace(exact(text,'internal static class MemoryRecoveryStateOwner'),'') # Replace the Native marker-only projection by the full current recovery authority.
 text=text.replace('internal sealed class MemoryHistoryCommitBannerlordAdapter {','internal sealed partial class MemoryHistoryCommitBannerlordAdapter {')
 text=text.replace(exact(text,'internal static MemoryCommitResult CommitExternalDialogueHistoryRecoverable('),'') # No fabricated memory receipt in Courier.
 text=text.replace('internal static class MemoryEntityIdentityBannerlordAdapter {','internal static partial class MemoryEntityIdentityBannerlordAdapter {')
 text=text.replace(exact(text,'private static string BuildRuleTargetKeyForExternal('),'')
 text=text.replace('static class PlayerEncounterCompat {','static partial class PlayerEncounterCompat {')
 text=text.replace('internal static bool IsAgentFollowingPlayerBySceneCommand(Agent a)=>','internal bool IsAgentFollowingPlayerBySceneCommand(Agent a)=>').replace('public static void BuildCurrentSettlementHeroNpcLineForPrompt()', 'public static string BuildCurrentSettlementHeroNpcLineForPrompt()')
 text=text.replace('public Clan OwnerClan,MapFaction;','public Clan OwnerClan;public IFaction MapFaction;')
 text=text.replace('public Clan MapFaction;','public IFaction MapFaction;').replace('public sealed class FixtureCulture {','public sealed class FixtureCulture {public string Name="Culture";').replace('public class Agent {','public partial class Agent {')
 text=text.replace(exact(text,'internal static MemoryCommitResult CommitDialogueHistoryWithScene('),'')
 text=text.replace(exact(text,'internal static IEnumerable<ConversationMessage> BuildUncompressedMemoryRoleMessagesForExternal('),'')
 text=text.replace('string text,int i,NpcDataPacket n){NativeHost.Game();NativeHost.Echo++;','string text,int i=-1,NpcDataPacket n=null){NativeHost.Game();NativeHost.Echo++;')
 text=text.replace(exact(text,'public enum PartyTransferEntrySection'),exact((ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs').read_text(encoding='utf-8-sig'),'public enum PartyTransferEntrySection'))
 text=text.replace(exact(text,'public enum SettlementTransferEntrySection'),exact((ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs').read_text(encoding='utf-8-sig'),'public enum SettlementTransferEntrySection'))
 text=text.replace('Latest=(kind,id)=>throw new NotSupportedException("live report backend outside Native detached snapshot slice")','Latest=(kind,id)=>CourierWeeklyLatestQuery.Find(WeeklyFixtureRecords,kind,id)')
 text=text.replace('internal static void Log(string a,string b){','internal static void Log(string a,string b){if(a=="CourierDelivery"&&b.StartsWith("llm main start session=",StringComparison.Ordinal))CourierNetworkLeaf.PromptStarted=true;if(a=="Logic"&&b.Contains("prompt_context_stage stage=complete",StringComparison.Ordinal))System.Threading.Interlocked.Exchange(ref CourierNetworkLeaf.OnPromptComplete,null)?.Invoke();if(a=="MemoryRecovery"&&b.StartsWith("auxiliary_outcome",StringComparison.Ordinal))System.Threading.Interlocked.Exchange(ref CourierNetworkLeaf.OnAcceptedMemory,null)?.Invoke();')
 text=text.replace('JsonSerializerOptions{IncludeFields=true}','JsonSerializerOptions{IncludeFields=true,ReferenceHandler=System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles}').replace('public static Hero Find(string id)=>AnimusForge.NativeHost.Hero;','public static Hero Find(string id)=>string.Equals(id,MainHero.StringId,StringComparison.Ordinal)?MainHero:string.Equals(id,AnimusForge.NativeHost.Hero.StringId,StringComparison.Ordinal)?AnimusForge.NativeHost.Hero:null;')
 text=text.replace('public enum Occupation { None,','public enum Occupation { Wanderer,None,').replace('internal sealed class ScenePersonaPreparationAdapter {','internal sealed partial class ScenePersonaPreparationAdapter {')
 host.write_text(text,encoding='utf-8')
 playback=seed/'PlaybackLeaves.cs';playback.write_text(playback.read_text(encoding='utf-8').replace('public struct Vec3 {','public struct Vec3 {public float LengthSquared=>X*X;'),encoding='utf-8')
 agent_path=ROOT/'src/AF.GameAdapter.Bannerlord/Prompt/SceneAgentIdentityPromptCaptureAdapter.cs'
 agent_text=agent_path.read_text(encoding='utf-8-sig')
 history_path=ROOT/'src/AF.GameAdapter.Bannerlord/Prompt/SceneHistoryPromptCaptureAdapter.cs'
 history_text=history_path.read_text(encoding='utf-8-sig')
 history_ctor_ports=missing_seed_declaration(exact(history_text,'internal sealed class PersistedHistoryCapturePorts'))
 history_additions='\n'.join(missing_seed_declaration(exact(history_text,sig)) for sig in ['internal static string BuildScenePublicHistorySection(', 'internal static string GetLatestNativeConversationNpcUtteranceForExternal(', 'internal static Func<string> CaptureNativeConversationPersistedHistoryWork('])
 (seed/'CourierHistoryCurrentPorts.cs').write_text('using System;using System.Linq;using System.Collections.Generic;using TaleWorlds.CampaignSystem;using TaleWorlds.MountAndBlade;using AnimusForge.Refactor.Modules;using AnimusForge;namespace AnimusForge.Refactor.Adapters;internal sealed partial class SceneHistoryPromptCaptureAdapter {'+missing_seed_declaration('private readonly PersistedHistoryCapturePorts _persisted;')+history_ctor_ports+history_additions+'}',encoding='utf-8')
 (seed/'CourierAgentPromptLeaves.cs').write_text('using System;using System.Collections.Generic;using System.Linq;using System.Text;using TaleWorlds.CampaignSystem;using TaleWorlds.MountAndBlade;using AnimusForge.Refactor.Modules;using RichExecutions.Core;namespace AnimusForge.Refactor.Adapters;internal sealed partial class SceneAgentIdentityPromptCaptureAdapter {'+'\n'.join(missing_seed_declaration(exact(agent_text,sig)) for sig in ['internal static void LogNonHeroMemoryTrace(', 'internal static void GetSceneReplyLengthLimits(', 'internal static string BuildNativeConversationNpcListBlockForPrompt(', 'internal static List<NpcDataPacket> FilterScenePresentNpcsForPrompt(', 'internal static string BuildSceneNpcListLineForPrompt(', 'internal static string BuildSceneNonHeroNamingNoteForPrompt(', 'internal static bool IsInspectionPrisonerNpcForPrompt(', 'internal static bool IsInspectionPrisonerAgentForPrompt(', 'internal static string JoinPromptSections(','internal static string BuildPlayerCustomPromptRuleBlock(','internal static bool IsSameSceneNpcForPrompt(','internal static string NormalizeSceneNpcMatchValue('])+'}',encoding='utf-8')
 persona_path=ROOT/'src/AF.GameAdapter.Bannerlord/Prompt/PersonaIdentityPromptCaptureAdapter.cs'
 persona=persona_path.read_text(encoding='utf-8-sig')
 pure_path=ROOT/'src/modules/AF.Module.Prompt/Composition/PersonaIntroTextRules.cs';pure=pure_path.read_text(encoding='utf-8-sig')
 for path in [persona_path,pure_path]: records.append({'path':path.relative_to(ROOT).as_posix(),'rawSha256':hashlib.sha256(path.read_bytes()).hexdigest(),'layer':'current-exact-detached-identity-schema-and-pure-rules'})
 (seed/'CourierIdentitySchema.cs').write_text('using System;using System.Text;using TaleWorlds.CampaignSystem;namespace AnimusForge.Refactor.Adapters {internal static partial class PersonaIdentityPromptCaptureAdapter {'+missing_seed_declaration(exact(persona,'internal sealed class HeroIdentityInfoSnapshot'))+missing_seed_declaration(exact(persona,'internal static string BuildFactionLineForPrompt('))+exact(persona,'internal static string BuildPlayerAddressedInputForName(')+exact(persona,'internal static int GetCurrentHourOfDaySafeForPrompt(')+exact(persona,'internal static string BuildCurrentDateFactForPrompt(')+''.join(exact(persona,'internal static '+kind+' '+name+'(') for kind,name in [('int','GetDaysInSeasonSafeForPrompt'),('int','GetDaysInYearSafeForPrompt'),('string','GetSeasonTextZhForPrompt'),('string','GetTimeOfDayTextZhForPrompt'),('string','GetTimeOfDayDetailTextZhForPrompt'),('bool','IsDayTimeForPrompt')])+missing_seed_declaration(next(line for line in persona.splitlines() if 'const int MarriageCandidateMinAgeForPrompt' in line))+'}}namespace AnimusForge {internal static class PersonaIntroTextRules {'+exact(pure,'internal static string GetClanTierReputationLabel(')+exact(pure,'internal static string BuildAgeBracketLabel(')+'}}',encoding='utf-8')
 kinship_names=['BuildNpcPlayerKinshipPromptLine','BuildNpcPlayerKinshipPromptLineForExternal','ResolveNpcPlayerKinshipText','GetPlayerDisplayNameForRelationshipPrompt','ContainsHero','ShareKnownParent','BuildPlayerSiblingLabelForPrompt']
 kinship_bodies=[]
 for name in kinship_names:
  match=re.search(r'internal static [^\n{}=]+ '+name+r'\(',persona);assert match,name;kinship_bodies.append(exact(persona,match.group()))
 kinship_bodies.append(exact(persona,'internal static string BuildPlayerPublicDisplayNameForPrompt()'))
 kinship_bodies.append(exact(persona,'internal static string BuildPlayerPublicDisplayNameForExternal(Hero observer)'))
 kinship_bodies.append(exact(persona,'internal static Hero ResolveCurrentPlayerIdentityObserverForPrompt('))
 (seed/'CourierKinshipCurrent.cs').write_text('using System;using System.Collections.Generic;using TaleWorlds.CampaignSystem;using TaleWorlds.CampaignSystem.Party;namespace AnimusForge.Refactor.Adapters;internal static partial class PersonaIdentityPromptCaptureAdapter {'+'\n'.join(kinship_bodies)+'}',encoding='utf-8')
 army_path=ROOT/'src/AF.GameAdapter.Bannerlord/Prompt/ArmyIdentityPromptCaptureAdapter.cs';army=army_path.read_text(encoding='utf-8-sig')
 army_bodies=[exact(army[m.start():],m.group()) for m in re.finditer(r'internal static [^\n{}=]+ (?:BuildPlayerCourier\w+|BuildPlayerVassalageRelationPromptLineForExternal|Resolve\w+VassalagePrompt)\(',army)]
 (seed/'CourierArmyIdentityCurrent.cs').write_text('using System;using System.Collections.Generic;using System.Linq;using System.Text;using TaleWorlds.CampaignSystem;using TaleWorlds.CampaignSystem.Party;using TaleWorlds.CampaignSystem.Settlements;using TaleWorlds.MountAndBlade;namespace AnimusForge.Refactor.Adapters;internal static class ArmyIdentityPromptCaptureAdapter {'+'\n'.join(army_bodies)+'}',encoding='utf-8')
 (seed/'CourierFactionIdentityCurrent.cs').write_text('using System;using TaleWorlds.CampaignSystem;namespace AnimusForge.Refactor.Adapters;internal static partial class PersonaIdentityPromptCaptureAdapter {'+''.join(exact(persona,sig) for sig in ['internal static void GetHeroFactionAndLiegeForPrompt(','internal static string GetHeroCultureNameForPrompt(','internal static bool TryResolveActiveKingdomRuledByHeroForPrompt('])+'}',encoding='utf-8')
 records.append({'path':army_path.relative_to(ROOT).as_posix(),'rawSha256':hashlib.sha256(army_path.read_bytes()).hexdigest(),'layer':'current-exact-Courier-identity-and-vassalage-capture-algorithms'})
 snapshot_path=ROOT/'src/AF.GameAdapter.Bannerlord/Composition/LegacyInteractionSnapshotAdapters.cs';snapshot=snapshot_path.read_text(encoding='utf-8-sig')
 snapshot_symbols=['private static string CurrentLocationId(', 'public static LegacyChannelInteractionFacade CreateCourierInteractionFacade(','public static InteractionEnvelope CaptureCourierFromPromptPackage(','public static RuntimeConfigSnapshot CaptureNativeConversationRuntimeConfiguration(','private static RuntimeConfigSnapshotStore GetRuntimeConfigurationStore(','private static RuntimeConfigSnapshot BuildLegacyRuntimeConfigurationSnapshot(']
 snapshot_fields='\n'.join(line for line in snapshot.splitlines() if any(x in line for x in ['private static long _configurationSequence;','private static readonly object RuntimeConfigurationStoreLock','private static RuntimeConfigSnapshotStore _runtimeConfigurationStore;','public const string NativeConversationModuleId','public const string LegacyShoutNetworkProviderId']))
 (seed/'CourierSnapshotCapture.cs').write_text('using System;using System.Collections.Generic;using System.Linq;using System.Threading;using AnimusForge.Refactor.Contracts;using AnimusForge.Refactor.Runtime;using TaleWorlds.CampaignSystem;using TaleWorlds.MountAndBlade;using TaleWorlds.CampaignSystem.Settlements;using TaleWorlds.CampaignSystem.Party;namespace AnimusForge.Refactor.Adapters;internal static partial class LegacyInteractionSnapshotAdapters {'+snapshot_fields+'\n'+'\n'.join(exact(snapshot,sig) for sig in snapshot_symbols)+'}',encoding='utf-8')
 records.append({'path':snapshot_path.relative_to(ROOT).as_posix(),'rawSha256':hashlib.sha256(snapshot_path.read_bytes()).hexdigest(),'layer':'current-exact-Courier-detached-envelope-and-configuration-capture'})
 state_path=ROOT/'src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.cs';state=state_path.read_text(encoding='utf-8-sig')
 queues_path=ROOT/'src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.Queues.cs';queues=queues_path.read_text(encoding='utf-8-sig')
 (seed/'CourierMemoryStatePublication.cs').write_text('using System;using System.Collections.Generic;using System.Linq;namespace AnimusForge;internal sealed partial class MemoryBusinessStateOwner {'+exact(state,'internal void SaveDrafts(')+exact(state,'internal bool HasCompressedMemoryBlock(')+exact(state,'internal static int CountDailyMemoryDraftLines(')+exact(queues,'internal bool HasBlock(')+'}',encoding='utf-8')
 for path in [state_path,queues_path]:records.append({'path':path.relative_to(ROOT).as_posix(),'rawSha256':hashlib.sha256(path.read_bytes()).hexdigest(),'layer':'current-exact-same-state-publication/query'})
 memory_path=ROOT/'src/AF.GameAdapter.Bannerlord/Memory/MemoryHistoryCommitBannerlordAdapter.cs';memory=memory_path.read_text(encoding='utf-8-sig')
 recovery_facade_path=ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemoryRecovery.cs';recovery_facade=recovery_facade_path.read_text(encoding='utf-8-sig')
 memory_names=['BuildUncompressedMemoryRoleMessages','BuildUncompressedMemoryRoleMessagesById','GetCurrentSceneSessionIdForDailyMemorySuppression','CommitDialogueHistoryWithScene','AppendDailyMemoryLineById','AppendDialogueHistory','AppendDialogueHistoryById','IsDialogueHistoryPublished','CommitPreparedDialogueHistoryRecovery','TryPrepareExternalDialogueHistoryRecovery','BuildInteractionMemoryRecoverySeed','BuildInteractionMemoryRecoverySessionKey','ResolveInteractionMemoryOriginGameDate','GetExternalDialogueHistoryRecoveryStatus','HasPublishedDailyInteractionMemoryComponent','CompleteInitialInteractionMemoryNotorietyOutcome','NotifyInitialMemoryNotorietyComponent']
 memory_bodies=[]
 for name in memory_names:
  match=re.search(r'internal (?:static )?[^\n{}=]+ '+name+r'\(',memory);assert match,name;memory_bodies.append(exact(memory,match.group()))
 strict_path=ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.DialogueHistoryCommit.cs';strict=strict_path.read_text(encoding='utf-8-sig')
 strict_wrappers=exact(my,'public static MemoryCommitResult CommitExternalDialogueHistory(')+''.join(exact(strict[m.start():],m.group()) for m in re.finditer(r'internal static MemoryCommitResult CommitDialogueHistoryWithScene\(',strict))
 records.append({'path':strict_path.relative_to(ROOT).as_posix(),'rawSha256':hashlib.sha256(strict_path.read_bytes()).hexdigest(),'layer':'current-exact-strict-history-main-thread-acceptance-boundary'})
 recovery_wrappers='\n'.join(exact(recovery_facade,sig) for sig in ['internal static MemoryCommitResult CommitExternalDialogueHistoryRecoverable(','internal static bool TryPrepareExternalDialogueHistoryRecoveryIdentity(','internal static InteractionMemoryRecoveryLookupStatus GetExternalDialogueHistoryRecoveryStatus(','private static bool TryPrepareExternalDialogueHistoryRecovery('])
 # The same fixture memory state is bound to the full production recovery ledger and actual history publication capabilities.
 root_cap='private MemoryRecoveryStateOwner _courierRecovery;private MemoryHistoryCommitBannerlordAdapter _memoryHistoryCommit {get {var history=MemoryRead; _courierRecovery ??=new MemoryRecoveryStateOwner(MemoryReadState){LoadedGeneration=SaveRuntimeGuard.CurrentGeneration,LoadConfirmed=1};_courierRecovery.Bind(new MemoryRecoveryPort {IsEntityEligible=MemoryEntityIdentityBannerlordAdapter.IsMemoryEntityEligibleForCompressedMemory,CurrentDay=()=>3,CurrentDate=()=>"Day 3",History=()=>MemoryReadState.History,LoadHistory=history.LoadDialogueHistoryById,SaveHistory=history.SaveDialogueHistoryById,RemoveExpiredFacts=MemoryHistoryCommitBannerlordAdapter.RemoveExpiredSingleUseNpcFactLines,Log=Logger.Log,TickScope=()=>null});history.BindCourierRecovery(()=>_courierRecovery);history.BindCourierDailyAppend(new MemoryDailyAppendCapabilities(MemoryEntityIdentityBannerlordAdapter.IsMemoryEntityEligibleForCompressedMemory,()=>3,()=>"Day 3",PersonaIdentityPromptCaptureAdapter.GetCurrentHourOfDaySafeForPrompt,()=>MemoryReadState.GetOrStartActiveNativeConversationMemorySessionId(MemoryEntityIdentityBannerlordAdapter.IsNonSceneNativeConversationActiveForMemory),SceneLocationPromptCaptureAdapter.ResolveCurrentMemorySceneLabel,()=>3,PlayerNotorietyBehavior.NoteConversationLineForExternal,MemoryHistoryCommitBannerlordAdapter.LogNonHeroMemoryTrace));return history;}}'
 (seed/'CourierCurrentMemoryAcceptance.cs').write_text('using System;using System.Diagnostics;using System.Collections.Generic;using System.Linq;using System.Globalization;using System.Threading;using TaleWorlds.CampaignSystem;using TaleWorlds.MountAndBlade;using TaleWorlds.Library;using AnimusForge.Refactor.Contracts;using AnimusForge.Refactor.Runtime;using AnimusForge.Refactor.Adapters;using static AnimusForge.MemoryBusinessStateOwner;using DialogueDay=AnimusForge.MyBehavior.DialogueDay;namespace AnimusForge;internal sealed partial class MemoryHistoryCommitBannerlordAdapter {private MemoryDailyAppendCapabilities _dailyAppend;internal void BindCourierDailyAppend(MemoryDailyAppendCapabilities capture){_dailyAppend=capture;}private Func<MemoryRecoveryStateOwner> _recovery;internal void BindCourierRecovery(Func<MemoryRecoveryStateOwner> recovery){_recovery=recovery;}'+ '\n'.join(memory_bodies)+'}internal sealed partial class MyBehavior {'+root_cap+recovery_wrappers+strict_wrappers+'}',encoding='utf-8')
 for path in [memory_path,recovery_facade_path]:records.append({'path':path.relative_to(ROOT).as_posix(),'rawSha256':hashlib.sha256(path.read_bytes()).hexdigest(),'layer':'current-exact-recoverable-memory-facades/identity/acceptance'})
 weekly_append_path=ROOT/'src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.WeeklyTriggers.cs';weekly_append=weekly_append_path.read_text(encoding='utf-8-sig')
 daily_bodies=''.join(exact(state,sig) for sig in ['internal bool IsDailyMemoryLinePublished(','internal bool AppendDailyMemoryLineById('])
 daily_bodies+=''.join(exact(weekly_append,sig) for sig in ['internal void AddWeeklyTrigger(','internal void AttachPendingWeeklyTriggers(','internal void PrunePendingWeeklyTriggers('])
 daily_bodies+=next(line for line in state.splitlines() if 'internal List<WeeklyMemoryMaterialTrigger> PendingWeeklyTriggers =' in line)
 (seed/'CourierDailyMemoryPublication.cs').write_text('using System;using System.Collections.Generic;using System.Linq;using AnimusForge.Refactor.Runtime;namespace AnimusForge;internal sealed partial class MemoryBusinessStateOwner {'+daily_bodies+'}'+exact(state,'internal sealed class MemoryDailyAppendCapabilities'),encoding='utf-8')
 entity_path=ROOT/'src/AF.GameAdapter.Bannerlord/Memory/MemoryEntityIdentityBannerlordAdapter.cs';entity=entity_path.read_text(encoding='utf-8-sig')
 (seed/'CourierNativeSessionGameCapture.cs').write_text('using TaleWorlds.CampaignSystem;using TaleWorlds.MountAndBlade;namespace AnimusForge;internal static partial class MemoryEntityIdentityBannerlordAdapter {'+exact(entity,'internal static bool IsNonSceneNativeConversationActiveForMemory(')+'}',encoding='utf-8')
 for path in [weekly_append_path,entity_path]: records.append({'path':path.relative_to(ROOT).as_posix(),'rawSha256':hashlib.sha256(path.read_bytes()).hexdigest(),'layer':'current-exact-daily-memory-publication-and-native-session-capture'})
 identity_store_path=ROOT/'src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.IdentityStores.cs';identity_store=identity_store_path.read_text(encoding='utf-8-sig')
 store_fields='\n'.join(line for line in identity_store.splitlines() if any(x in line for x in ['internal int _nativeConversationMemorySessionCounter;','internal int _activeNativeConversationMemorySessionId =','internal string _memoryRuntimeSessionKey =']))
 (seed/'CourierMemorySessionIdentity.cs').write_text('using System;namespace AnimusForge;internal sealed partial class MemoryBusinessStateOwner {'+store_fields+exact(identity_store,'internal int GetOrStartActiveNativeConversationMemorySessionId(')+exact(identity_store,'internal string BuildCurrentMemorySessionKey(')+exact(identity_store,'internal int GetCurrentNativeConversationMemorySessionIdForSuppression(')+'}',encoding='utf-8')
 social_path=ROOT/'src/modules/AF.Module.Social/Notoriety/NotorietyConversationOutcomeReceipt.cs';social=social_path.read_text(encoding='utf-8-sig')
 (seed/'CourierSocialOutcomeIdentity.cs').write_text('namespace AnimusForge;'+exact(social,'internal enum NotorietyConversationOutcomeOperationStatus'),encoding='utf-8')
 for path in [identity_store_path,social_path]:records.append({'path':path.relative_to(ROOT).as_posix(),'rawSha256':hashlib.sha256(path.read_bytes()).hexdigest(),'layer':'current-exact-session-state/receipt-identity'})
 shown_persistence_path=ROOT/'src/AF.GameAdapter.Bannerlord/Persistence/CampaignShownRecordPersistenceAdapter.cs'
 (seed/'CourierShownKeyPolicy.cs').write_text('namespace AnimusForge;internal static class CampaignShownRecordPersistenceAdapter {'+exact(shown_persistence_path.read_text(encoding='utf-8-sig'),'internal static string NormalizeKey(')+'}',encoding='utf-8')
 records.append({'path':shown_persistence_path.relative_to(ROOT).as_posix(),'rawSha256':hashlib.sha256(shown_persistence_path.read_bytes()).hexdigest(),'layer':'current-exact-normalization-policy'})
 # Current role capture/composition is real; only typed world-observation leaves are controlled.
 intro_path=ROOT/'src/AF.GameAdapter.Bannerlord/Composition/PersonaIntroLivePorts.cs';intro=intro_path.read_text(encoding='utf-8-sig')
 assignments={}
 for cls in ['MyPersonaIntroLivePort','ScenePersonaIntroLivePort']:
  declarations=exact(intro,'internal sealed class '+cls)
  fields=[]
  for match in re.finditer(r'internal delegate ([^\n]+?) (\w+)Query\(([^\n]*)\);',declarations):
   ret,name,params=match.groups();params=re.sub(r' = [^,]+','',params)
   if ret=='void':
    outs=re.findall(r'out string (\w+)',params);body='{NativeHost.Game();'+''.join(x+'="";' for x in outs)+'}'
   elif ret=='string':body='{NativeHost.Game();return "";}'
   elif ret=='bool':body='{NativeHost.Game();return false;}'
   elif ret in ['PartyBase','IFaction']:body='{NativeHost.Game();return null;}'
   else:raise AssertionError(ret)
   fields.append(name+'=('+params+')=>'+body)
  assignments[cls]=','.join(fields)
 factory_fields='private static readonly PersonaEquipmentPromptCaptureAdapter CourierPersonaFactory=new(new MyPersonaIntroLivePort {'+assignments['MyPersonaIntroLivePort']+'},new ScenePersonaIntroLivePort {'+assignments['ScenePersonaIntroLivePort']+'});private static SceneAgentIdentityPromptCaptureAdapter.RoleIntroCapturePorts SceneRoleIntroCapture=>new(new SceneAgentIdentityPromptCaptureAdapter(),()=>CourierPersonaFactory,()=>new SceneAgentIdentityPromptCaptureAdapter.ActionReadPorts(null,null,null,null));'
 role_wrapper=exact(scene_current,'public static string BuildHeroStableRoleContextForExternal(')
 prep_path=ROOT/'src/AF.GameAdapter.Bannerlord/Prompt/ScenePersonaPreparationAdapter.cs';prep=prep_path.read_text(encoding='utf-8-sig')
 (seed/'CourierRoleCurrentFallback.cs').write_text('using System;using System.Text;using TaleWorlds.CampaignSystem;namespace AnimusForge.Refactor.Adapters;internal sealed partial class ScenePersonaPreparationAdapter {'+exact(prep,'internal static void BuildHeroPersonaFallback(')+'}',encoding='utf-8')
 records.append({'path':prep_path.relative_to(ROOT).as_posix(),'rawSha256':hashlib.sha256(prep_path.read_bytes()).hexdigest(),'layer':'current-exact-role-persona-fallback-capture'})
 (seed/'CourierRoleCurrentFactory.cs').write_text('using System;using System.Collections.Generic;using TaleWorlds.CampaignSystem;using TaleWorlds.CampaignSystem.Party;using AnimusForge.Refactor.Adapters;namespace AnimusForge;public partial class ShoutBehavior {'+factory_fields+role_wrapper+'}',encoding='utf-8')
 available_all='\n'.join(file.read_text(encoding='utf-8-sig') for file in seed.glob('*.cs'))
 available='\n'.join(exact(available_all[m.start():],m.group()) for m in re.finditer(r'internal sealed partial class SceneAgentIdentityPromptCaptureAdapter\s*\{',available_all))
 candidates=[]
 for match in re.finditer(r'(?m)^\s*internal static [^\n{};=]+ ([A-Za-z_][A-Za-z0-9_]*)\(',agent_text):
  candidates.append((match.group(1),exact(agent_text[match.start():],match.group().strip())))
 selected=[];needed={'BuildHeroStableRoleContextForExternal','BuildSceneSystemTopPromptIntroForSingle','SplitSceneNpcRoleIntroSections','BuildSceneAgentSelfActionFactForPrompt'}
 while True:
  count=len(selected)
  for name,body in candidates:
   if name not in needed or body in selected:continue
   if re.search(r'(?:internal|private|public) (?:static )?[^\n{};=]+ '+re.escape(name)+r'\(',available):continue
   selected.append(body);needed.update(re.findall(r'\b(\w+)\s*\(',body))
  if len(selected)==count:break
 (seed/'CourierRoleCurrentCapture.cs').write_text('using System;using System.Collections.Generic;using System.Linq;using System.Text;using System.Reflection;using System.Text.RegularExpressions;using TaleWorlds.Engine;using TaleWorlds.CampaignSystem;using TaleWorlds.CampaignSystem.Party;using TaleWorlds.MountAndBlade;using AnimusForge.Refactor.Runtime;namespace AnimusForge.Refactor.Adapters;internal sealed partial class SceneAgentIdentityPromptCaptureAdapter {'+exact(agent_text,'internal sealed class RoleIntroCapturePorts')+exact(agent_text,'internal sealed class ActionReadPorts')+'\n'.join(selected)+'}',encoding='utf-8')
 weekly_query_path=ROOT/'src/AF.GameAdapter.Bannerlord/Weekly/WeekZeroOpeningSummaryGenerationController.cs';weekly_query=weekly_query_path.read_text(encoding='utf-8-sig')
 (seed/'CourierWeeklyLatestCurrent.cs').write_text('using System;using System.Collections.Generic;using TaleWorlds.Library;using EventRecordEntry=AnimusForge.MyBehavior.EventRecordEntry;using WeeklyPromptReportSnapshot=AnimusForge.MyBehavior.WeeklyPromptReportSnapshot;namespace AnimusForge;internal sealed class WeekZeroOpeningSummaryGenerationController {private CourierWeeklyFixtureRecordBinding _records;internal WeekZeroOpeningSummaryGenerationController(List<EventRecordEntry> records){_records=new(){Records=records};}'+exact(weekly_query,'internal EventRecordEntry FindLatestWeeklyReportRecord(')+'}internal sealed class CourierWeeklyFixtureRecordBinding {internal List<EventRecordEntry> Records;}internal static class CourierWeeklyLatestQuery {internal static EventRecordEntry Find(List<EventRecordEntry> records,string kind,string id)=>new WeekZeroOpeningSummaryGenerationController(records).FindLatestWeeklyReportRecord(kind,id);}',encoding='utf-8')
 records.append({'path':weekly_query_path.relative_to(ROOT).as_posix(),'rawSha256':hashlib.sha256(weekly_query_path.read_bytes()).hexdigest(),'layer':'current-exact-weekly-latest-query-with-same-controlled-record-source'})
 hooks=(HERE/'LivenessHooks.cs.txt').read_text(encoding='utf-8-sig').split('// @@COURIER_CURRENT_LEAVES@@')[1]
 engine=hooks.split('// @@COURIER_ENGINE_LEAVES@@');hooks=engine[0]
 engine_text='using TaleWorlds.CampaignSystem.Roster;'+engine[1]
 if 'public readonly List<ItemRosterElement> Entries=new();' in seed_source_text:
  for signature in ['internal struct ItemRosterElement', 'internal class ItemRoster']:engine_text=engine_text.replace(exact(engine_text,signature),'')
  engine_text=engine_text.replace('AnimusForge.ItemRoster','TaleWorlds.CampaignSystem.Roster.ItemRoster')
 # These engine shapes now come from the current Native seed, not a second C copy.
 if 'public ItemObject Item;' in seed_source_text:engine_text=engine_text.replace('public partial struct EquipmentElement {public ItemObject Item;}','public partial struct EquipmentElement {}')
 if 'public bool IsSettlement,IsMobile;' in seed_source_text:
  engine_text=engine_text.replace('public bool IsMobile,IsSettlement;public MobileParty MobileParty;public Settlement Settlement;public Hero LeaderHero;public IFaction MapFaction;','')
 if 'public string Name,StringId;' in seed_source_text:engine_text=engine_text.replace('public string StringId="party",Name="Party";','').replace('public Clan ActualClan;','')
 if 'public bool IsPrisoner;' in seed_source_text or 'IsPrisoner,' in seed_source_text:
  engine_text=engine_text.replace('public bool IsPrisoner,IsPregnant;','public bool IsPregnant;').replace('public PartyBase PartyBelongedToAsPrisoner;','')
 if 'public bool IsTown,IsCastle,IsVillage;' in seed_source_text or 'public bool IsCastle,IsTown,IsVillage;' in seed_source_text:
  engine_text=engine_text.replace('public partial class Settlement {public bool IsCastle,IsTown,IsVillage;public Clan OwnerClan;public IFaction MapFaction;}','public partial class Settlement {}')
 if 'TryCallAuxiliaryRuleCodesForExternal(' in seed_source_text:
  method=exact(engine_text,'internal static bool TryCallAuxiliaryRuleCodesForExternal(');engine_text=engine_text.replace(method,'')
 (seed/'CourierEngineLeaves.cs').write_text(engine_text,encoding='utf-8')

 cases=(HERE/'LivenessCases.cs.txt').read_text(encoding='utf-8-sig').split('// @@COURIER_CURRENT_CASES@@')[1]
 (seed/'CourierCurrentLeaves.cs').write_text(hooks,encoding='utf-8');(seed/'CourierCurrentCases.cs').write_text(cases,encoding='utf-8')
 project=seed/'CurrentConsumer.csproj';tree=ET.parse(project);item=ET.SubElement(tree.getroot(),'ItemGroup')
 for file in sorted(seed.glob('Courier*.cs')):ET.SubElement(item,'Compile',{'Include':str(file)})
 ET.SubElement(item,'Compile',{'Include':str(ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierVisibleLetterSanitizer.cs')})
 ET.SubElement(item,'Compile',{'Include':str(ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierInboundCompletionReceipt.cs')})
 ET.SubElement(item,'Compile',{'Include':str(ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierInboundCompletionCommitCoordinator.cs')})
 ET.SubElement(item,'Compile',{'Include':str(ROOT/'src/modules/AF.Module.Llm/Transport/LegacyShoutNetworkGateway.cs')})
 ET.SubElement(item,'Compile',{'Include':str(ROOT/'src/AF.GameAdapter.Bannerlord/UI/Common/AnimusForgeTextInputSanitizer.cs')})
 ET.SubElement(item,'Compile',{'Include':str(ROOT/'src/modules/AF.Module.Conversation/Internal/RuntimeConfigSnapshotStore.cs')})
 recovery_path=ROOT/'src/modules/AF.Module.Memory/Summary/MemoryRecoveryStateOwner.cs'
 ET.SubElement(item,'Compile',{'Include':str(recovery_path)})
 records.append({'path':recovery_path.relative_to(ROOT).as_posix(),'rawSha256':hashlib.sha256(recovery_path.read_bytes()).hexdigest(),'layer':'full-current-recovery-acceptance-and-publication-authority'})
 for memory_source in ['src/modules/AF.Module.Memory/Recovery/MemoryRecoverySeedRules.cs','src/modules/AF.Module.Memory/Recovery/InteractionMemoryAuxiliaryCompletionCoordinator.cs']:
  path=ROOT/memory_source;assert path.is_file();ET.SubElement(item,'Compile',{'Include':str(path)});records.append({'path':memory_source,'rawSha256':hashlib.sha256(path.read_bytes()).hexdigest(),'layer':'whole-current-memory-policy/auxiliary-coordinator'})
 shown_path=ROOT/'src/modules/AF.Module.Economy/Presentation/ShownResourceRecordOwner.cs';ET.SubElement(item,'Compile',{'Include':str(shown_path)});records.append({'path':shown_path.relative_to(ROOT).as_posix(),'rawSha256':hashlib.sha256(shown_path.read_bytes()).hexdigest(),'layer':'whole-current-shown-resource-state-authority'})
 uncompressed_path=ROOT/'src/modules/AF.Module.Prompt/Composition/UncompressedMemoryMessageAssemblyOwner.cs';ET.SubElement(item,'Compile',{'Include':str(uncompressed_path)});records.append({'path':uncompressed_path.relative_to(ROOT).as_posix(),'rawSha256':hashlib.sha256(uncompressed_path.read_bytes()).hexdigest(),'layer':'whole-current-uncompressed-memory-assembly-authority'})
 identity_schema=seed/'CourierIdentitySchema.cs';schema=identity_schema.read_text(encoding='utf-8');schema=schema[:schema.index('namespace AnimusForge {internal static class PersonaIntroTextRules')];identity_schema.write_text(schema,encoding='utf-8')
 for role_path in ['src/AF.GameAdapter.Bannerlord/Composition/PersonaIntroLivePorts.cs','src/AF.GameAdapter.Bannerlord/Composition/PersonaEquipmentPromptCaptureAdapter.cs','src/modules/AF.Module.Prompt/Composition/PersonaIntroMessageComposer.cs','src/modules/AF.Module.Prompt/Composition/PersonaIntroTextRules.cs']:
  path=ROOT/role_path
  seed_copy=seed/path.name
  if not (seed_copy.is_file() and seed_copy.read_text(encoding='utf-8-sig')==path.read_text(encoding='utf-8-sig')):ET.SubElement(item,'Compile',{'Include':str(path)})
  records.append({'path':role_path,'rawSha256':hashlib.sha256(path.read_bytes()).hexdigest(),'layer':'whole-current-persona-capture/snapshot/composition-authority'})
 (seed/'courier-seed-exact-reuse.json').write_text(json.dumps(reused_seed_declarations,indent=2),encoding='utf-8')
 tree.write(project,encoding='utf-8',xml_declaration=True)
 for entry in tree.getroot().iter('Compile'):
  path=Path(entry.get('Include'));path=path if path.is_absolute() else seed/path
  if path.is_file() and path.resolve().is_relative_to((ROOT/'src').resolve()):
   relative=path.resolve().relative_to(ROOT).as_posix()
   if not any(x['path']==relative for x in records):records.append({'path':relative,'rawSha256':hashlib.sha256(path.read_bytes()).hexdigest(),'layer':'current-whole-linked-production-source'})
 for extraction in json.loads((seed/'extractions.json').read_text(encoding='utf-8')):
  if not any(x['path']==extraction['path'] for x in records):records.append({'path':extraction['path'],'rawSha256':extraction['rawSha256'],'layer':'current-exact-Native/My-seed-declarations'})
 dotnet=resolve_dotnet(ROOT);env=minimal_test_environment(dotnet,seed)
 run=subprocess.run([str(dotnet),'run','--project',str(project),'-c','Release'],cwd=seed,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180)
 log=run.stdout+run.stderr;(seed/'courier-current.log').write_text(log,encoding='utf-8')
 unique_errors=list(dict.fromkeys(re.sub(r'^.*?: error ', '',line).split(' [')[0] for line in log.splitlines() if ': error ' in line))
 (seed/'finite-diagnostics.json').write_text(json.dumps({'status':'COMPILE_FAILED_NOT_PROOF' if unique_errors else ('EXECUTION_FAILED_NOT_PROOF' if run.returncode else 'CURRENT_CONSUMER_PASS'),'uniqueCount':len(unique_errors),'errors':unique_errors},indent=2),encoding='utf-8')
 after=[dict(x,rawSha256=hashlib.sha256((ROOT/x['path']).read_bytes()).hexdigest()) for x in records]
 assert records==after,'current Courier inputs changed during run'
 test_after=[{'path':path.relative_to(ROOT).as_posix(),'rawSha256':hashlib.sha256(path.read_bytes()).hexdigest()} for path in test_paths]
 assert test_inputs==test_after,'current Courier test inputs changed during run'
 binaries=[{'path':str(path.relative_to(seed)),'rawSha256':hashlib.sha256(path.read_bytes()).hexdigest()} for path in sorted((seed/'bin').rglob('CurrentConsumer.dll'))]
 (seed/'courier-current-inputs.json').write_text(json.dumps({'sourceClass':'current-full-Courier-algorithms','protectedSessionTransportReadOnly':True,'inputs':records,'after':after,'exitCode':run.returncode,'baselineNativeExitCode':baseline,'apiLine':a.api_line,'testInputs':test_inputs,'testAfter':test_after,'generatedBinaries':binaries},indent=2),encoding='utf-8')
 print('Current Courier exit='+str(run.returncode)+' uniqueErrors='+str(len(unique_errors))+' log='+str(seed/'courier-current.log'));print('\n'.join(unique_errors));raise SystemExit(run.returncode)

courier=source('CourierDeliveryBehavior.cs') if a.old else ex.courier_source(None);partial=source('src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PromptPreparation.cs')
phase=ex.declaration((ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DetachedPostprocess.cs').read_text(encoding='utf-8-sig'),'private async Task<T> RunCourierOwnerPhaseAsync<T>(').replace('Task.Delay(30000)','Task.Delay(180)')
base=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig').split('internal static class Program {')[0]
message_markers=['private static List<object> BuildCourierReplyMessages(','private static List<object> BuildInboundNpcLetterMessages(',
 'private static object CreateCourierChatMessage(','private static void AppendCourierRawUserSection(',
 'private static void AppendCourierUserSection(','private static void AppendCourierPersistentMemoryRoleMessages(',
 'private static bool TryConvertCourierMemoryMessageToChatMessage(',
 'private static string BuildCourierMemoryMetadataPrefix(','private static string StripCourierPromptScopeLabel(',
 'private static string StripCourierSpeakerPrefix(']
message_source=ex.courier_source(None)
import main_assembly_projection as main_projection
main_projection.projected_messages()
message_builders=main_projection.production_builders()
for name in ('BuildCourierReplyMessages','BuildInboundNpcLetterMessages'):
 message_builders=message_builders.replace('private static List<object> '+name+'(', 'private static List<object> '+name+'Production(',1)
base=base.replace('@@MESSAGE_BUILDERS@@',message_builders).replace('@@FINALIZE_REPLY@@','')
base=base.replace('  private CourierPromptRun TestRun;\n  internal void ReserveTestRun()=>TestRun=BeginCourierPromptRun(Session,1);','')
base=base.replace(ex.declaration(base,'internal async Task<string> Start('),'')
base=base.replace('internal static long Generation=1;','internal static long Generation=1; internal static long CaptureGeneration()=>Generation;')
base=base.replace('ReplyGenerationStarted=true,PostprocessConsumed,DeliveryApplied;internal string ReplyText="",ReplyPostprocessedText="";','ReplyGenerationStarted=true,PostprocessConsumed,DeliveryApplied=true,ReplyWaitPopupShown=true;internal string Stage="GeneratingReply",ReplyText="",ReplyPostprocessedText="",RecipientWaitReason="";')
base=base.replace('static ManualResetEventSlim Entered=new(),Release=new(true);','static ManualResetEventSlim Entered=new(),Release=new(true);')
# The held provider snapshots its release event before a replacement Start can install the new test request.
base=base.replace('Probe.Entered.Set();\n   if(!Probe.Release.Wait(5000))','var release=Probe.Release;Probe.Entered.Set();\n   if(!release.Wait(5000))')
base=base.replace('if(Probe.ThrowRouting)throw new PreprocessFormatException();','if(Probe.ThrowRouting)throw new PreprocessFormatException();')
reqs='\n'.join(ex.declaration(courier,'private sealed class '+name) for name in ['CourierReplyGenerationRequest','InboundLetterGenerationRequest'])
campaign=(ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.CampaignLifetime.cs').read_text(encoding='utf-8-sig')
lifetime_field=campaign[campaign.index('    private readonly Dictionary<CourierSession, ConversationRequestLifetime>'):campaign.index('    private ConversationRequestLifetime BeginCourierRequestLifetime(')]
lifetime_methods=lifetime_field+'\n'+'\n'.join(ex.declaration(campaign,sig) for sig in ['private ConversationRequestLifetime BeginCourierRequestLifetime(', 'private void RetireCourierRequestLifetime(', 'private void RetireCourierRequestLifetimes('])
history=(ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.HistoryPreparation.cs').read_text(encoding='utf-8-sig');historyDecl='\n'.join(ex.declaration(history,sig) for sig in ['private sealed class CourierPreparedHistory','private bool IsCourierHistoryOwnerCurrent('])
base=base.replace('@@LIFETIME@@',lifetime_methods).replace('@@OWNER_PHASE@@',phase).replace('@@REQUESTS@@',reqs).replace('@@HISTORY@@',historyDecl).replace('@@BASELINE@@','')
# Sync baseline comparison belongs to run.py; the liveness suite uses actual Start -> caller instead.
base=base.replace(ex.declaration(base,'internal string Sync('),'')
methods='\n'.join(ex.declaration(courier,sig) for sig in ['private void StartCourierReplyGeneration(','private void StartInboundLetterGeneration(','private void BeginCourierReplyGenerationOnMainThread(','private void BeginInboundLetterGenerationOnMainThread(','private async Task PrepareAndGenerateCourierReplyOffMainThreadAsync(','private async Task PrepareAndGenerateInboundLetterOffMainThreadAsync(','private void FailCourierReplyGenerationOnMainThread(','private void FailInboundLetterGenerationOnMainThread(','private void ProcessInboundToPlayerSession('])
# Observability only: retain the actual background task handle, without replacing its delegate.
methods=methods.replace('_ = Task.Run(() => PrepareAndGenerate','Liveness.Background = Task.Run(() => PrepareAndGenerate')
process=ex.declaration(courier,'private void ProcessSessionCore(' if 'private void ProcessSessionCore(' in courier else 'private void ProcessSession(')
replytick=ex.declaration(process,'if (stage == CourierStage.GeneratingReply)')
deliver=ex.declaration(courier,'private void DeliverInboundLetterToPlayer(');cut=deliver.index('\n\t\tstring letter = (session.LetterText')
inboundprefix=deliver[:cut]+'\n\t\tLiveness.Deliveries++;\n\t}\n'
if a.mutate=='drop-failure':partial=partial.replace('CompleteCourierPromptSourceChanged(promptRun, input);',';')
if a.mutate=='ignore-run':partial=partial.replace('&& _courierPromptRuns.TryGetValue(run.Session, out CourierPromptRun current) && ReferenceEquals(current, run)','')
if a.mutate=='keep-stale-tags':
 partial=partial.replace('input.Session.ReplyText = string.Empty;','').replace('input.Session.ReplyPostprocessedText = string.Empty;','').replace('input.Session.PostprocessConsumed = true;','')
if a.mutate=='old-fallback':partial=partial.replace('input.Session.InboundFallbackLetter, "inbound_prompt_source_changed"','input.FallbackLetter, "inbound_prompt_source_changed"')
commit=ex.declaration(courier,'private void CommitGeneratedReplyActionsAtRecipientCore(' if a.old else 'private bool CommitGeneratedReplyActionsAtRecipientCore(');commit=commit[:commit.index('\n\t\tif (recipient == null')]+'\n\t\tif (text.Contains("[ACTION:")) Liveness.StaleTagEffects++;\n'+('' if a.old else '\t\treturn true;\n')+'\t}\n'
commit+='\n\tprivate void CommitGeneratedReplyAtRecipient(CourierSession session, Hero recipient, bool persistHistory = true) => CommitGeneratedReplyActionsAtRecipientCore(session, recipient, persistHistory);\n'
transport=(ROOT/'src/modules/AF.Module.Llm/Transport/LlmNonStreamingTransport.cs').read_text(encoding='utf-8-sig')
field=transport[transport.index('    private static readonly AsyncLocal<CancellationToken> OwnerCancellation'):];field=field[:field.index(';')+1]
transport_lifetime=field+'\n'+'\n'.join(ex.declaration(transport,sig) for sig in ['internal static IDisposable PushOwnerCancellation(', 'private sealed class OwnerCancellationScope', 'internal static CancellationTokenSource CreateTimeout('])
hooks=(HERE/'LivenessHooks.cs.txt').read_text(encoding='utf-8-sig').split('// @@COURIER_CURRENT_LEAVES@@')[0].replace('@@METHODS@@',methods).replace('@@REPLY_TICK@@',replytick).replace('@@INBOUND_PREFIX@@',inboundprefix).replace('@@COMMIT_GUARD@@',commit).replace('@@TRANSPORT_LIFETIME@@',transport_lifetime)
if a.output_name and a.run_root:p.error('Use either --output-name or --run-root')
out=new_run_root(ROOT,'courier-prompt-liveness',a.run_root or (HERE/'.generated'/a.output_name if a.output_name else None))
(out/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
(out/'Prompt.cs').write_text(partial,encoding='utf-8');(out/'Schedule.cs').write_text(historical_source('src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PromptSchedule.cs'),encoding='utf-8');(out/'Host.cs').write_text('#define LIVENESS\n'+base,encoding='utf-8');(out/'Hooks.cs').write_text(hooks,encoding='utf-8')
(out/'Program.cs').write_text((HERE/'LivenessCases.cs.txt').read_text(encoding='utf-8-sig').split('// @@COURIER_CURRENT_CASES@@')[0],encoding='utf-8')
files=[out/'Prompt.cs',out/'Host.cs',out/'Hooks.cs',out/'Program.cs',ROOT/'src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs',ROOT/'src/modules/AF.Module.Prompt/Composition/PromptExtrasComposer.cs',ROOT/'src/modules/AF.Module.Prompt/Composition/ConversationRoleClassificationOwner.cs',ROOT/'src/modules/AF.Module.Prompt/Composition/MainPromptMessageAssemblyOwner.cs',ROOT/'src/AF.Contracts/Internal/InteractionContracts.cs',ROOT/'src/AF.Contracts/Internal/LlmContracts.cs',ROOT/'src/modules/AF.Module.Conversation/Internal/ConversationRequestLifetime.cs',ROOT/'src/modules/AF.Module.Conversation/Internal/InteractionRequestLease.cs']
if not a.old:files.append(out/'Schedule.cs')
project=util.project(out,'CourierPromptLiveness',files,executable=True)
dotnet=os.environ.get('AF_DOTNET') or str(ROOT/'local/dotnet/8.0.425/dotnet.exe')
code,log=util.run_dotnet(dotnet,['run','--project',str(project),'-c','Release'],out);(out/'run.log').write_text(log,encoding='utf-8');print(log,end='');raise SystemExit(code)

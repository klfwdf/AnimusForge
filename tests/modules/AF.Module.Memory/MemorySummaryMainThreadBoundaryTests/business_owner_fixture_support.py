"""Link the actual single Memory state owner and its host projections into replay fixtures.
Only engine facts and test fault/counter hooks remain fixture seams.
"""
import re, hashlib
from pathlib import Path
FILES=['Summary/MemoryBusinessStateOwner.IdentityStores.cs','Summary/MemoryRecoveryStateOwner.cs','Recovery/InteractionMemoryRecoveryLedger.cs','Records/DialogueHistoryLedger.cs','Summary/MemoryBusinessStateOwner.cs','Summary/MemoryBusinessStateOwner.Identity.cs','Summary/MemoryBusinessStateOwner.Queues.cs','Summary/MemorySummaryPlanningOwner.cs','Summary/MemorySealingOwner.cs','Summary/MemorySourceFingerprintRules.cs','Summary/MemorySourceFingerprintWriter.cs','Summary/CooperativeMemoryQueueSort.cs','Summary/MemoryMaintenanceWorkBudget.cs','Records/NpcActionLedger.cs']
FIELDS=['_dailyMemoryDrafts','_compressedMemoryBlocks','_memoryOverviewStates','_npcMajorActionSummaries','_pendingWeeklyMemoryMaterialTriggers','_memorySummaryQueue','_memoryOverviewQueue','_npcMajorActionSummaryQueue','_dirtyMemoryOverviewIds','_pendingMemoryOverviewCandidateScanIds','_pendingMemoryOverviewCandidateScanIdSet','_npcMajorActions','_memoryOverviewStateStorage','_npcMajorActionSummaryStorage']
MAP={'_dailyMemoryDraftSealOwnerKeys':'OwnerKeys','_dailyMemoryDraftSealOwnerIndex':'OwnerIndex','_dailyMemoryDraftSealDraftIndex':'DraftIndex','_dailyMemoryDraftSealTargetDay':'TargetDay','_dailyMemoryDraftSealQueuedMajor':'QueuedMajor','_dailyMemoryDraftSealQueued':'Queued','_dailyMemoryDrafts':'_state.Drafts','_memorySummaryQueue':'_state.DailyQueue','_npcMajorActionSummaryQueue':'_state.MajorQueue','_campaignMemoryMaintenanceBudget':'_port.SharedBudget()','ResetDailyMemoryDraftSealSliceState':'Reset'}
RULES=['NormalizeMemoryHeroId','SanitizeDailyMemoryDraftLine']
def owner_anchor(s):
 for a,b in sorted(MAP.items(),key=lambda x:-len(x[0])):s=s.replace(a,b)
 for a in RULES:s=re.sub(r'(?<![\w.])'+a+r'\(', 'MemoryRecordRules.'+a+'(',s)
 return s

def daily_maintenance_replay_members(source, ex):
 # The manifest hashes these exact declarations, not the surrounding controller.
 members=[re.search(r'internal (?:bool|long) '+name+r';',source).group() for name in ['SummaryStartPending','SummaryStartGeneration']]
 members.append(re.search(r'internal const double DailyMaintenanceDefaultFrameBudgetMs = [^;]+;',source).group())
 members.append(ex.declaration(source,'internal static double GetDailyMaintenanceFrameBudgetMs('))
 return members

def include(root,files,manifest,ex):
 source=(root/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs').read_text(encoding='utf-8-sig')
 projections=[]
 if not any('class DialogueDay' in v for v in files.values()):
  projections.append(ex.declaration(source,'internal class DialogueDay'))
 for field in FIELDS:
  m=re.search(r'^\s*private [^\n]+ '+field+r' \{ get => _memoryBusinessState[^\n]+',source,re.M)
  if not m:continue
  prop=m.group().strip();found=False
  for name in list(files):
   if name not in ['Fixture.cs','Program.cs','Product.cs','Sealing.cs','Business.cs','Terminal.cs']:continue
   pattern=r'(?m)^\s*(?:private |readonly )?(?:List<[^;\n]+>|Dictionary<[^;\n]+>|HashSet<[^;\n]+>|Queue<[^;\n]+>) '+field+r'\s*=.*?;'
   files[name],n=re.subn(pattern,'\n '+prop,files[name]);found|=n>0
  if not found:projections.append(prop)
 if not any(re.search(r'private const int DailyMaintenanceMaxJobsPerTick',v) for v in files.values()):
  projections.append(re.search(r'private const int DailyMaintenanceMaxJobsPerTick = [^;]+;',source).group())
 if not any(re.search(r'\b(?:bool|void) CancelUnavailableHeroCompressionWorkById\(',v) for v in files.values()):
  projections.append(ex.declaration(source,'private bool CancelUnavailableHeroCompressionWorkById('))
 factory=ex.declaration(source,'private MemoryBusinessStateOwner MemoryQueueState').replace('CurrentDay = () => (int)CampaignTime.Now.ToDays','CurrentDay = GetCurrentGameDayIndexSafe')
 owner='private readonly MemoryBusinessStateOwner _memoryBusinessState = new MemoryBusinessStateOwner();'
 files['StateProjections.cs']='using System;using System.Collections.Generic;using TaleWorlds.CampaignSystem;namespace AnimusForge {public partial class MyBehavior { '+owner+'\n'+factory+'\n'+'\n'.join(projections)+' }}'
 # Link exact current data declarations needed by the same real state owner.
 # No recall/router behavior is substituted by these source-extracted type leaves.
 typed = [
  ('src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemorySummaryInput.cs', 'internal sealed class MemorySummarySourceView', 'MyBehavior'),
  ('src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.HistoryPromptSnapshot.cs', 'internal sealed class HistoryPromptSnapshot', 'MyBehavior'),
  ('src/modules/AF.Module.Memory/Recall/HistoryArchiveRecallOwner.cs', 'internal sealed class HistoryLineEntry', None),
 ]
 for relative, signature, containing in typed:
  actual=(root/relative).read_text(encoding='utf-8-sig')
  body=ex.declaration(actual,signature)
  if not any(signature in value for value in files.values()):
   wrapped=('public partial class MyBehavior { '+body+' }') if containing else body
   files[signature.rsplit(' ',1)[-1]+'.cs']='using System;using System.Collections.Generic;namespace AnimusForge { '+wrapped+' }'
  manifest.append(dict(file=relative,signature=signature,sha256=hashlib.sha256(body.encode()).hexdigest(),source_extracted_type=True))
 relative='src/modules/AF.Module.Prompt/Composition/PromptBuiltInTopicRouter.cs'
 actual=(root/relative).read_text(encoding='utf-8-sig')
 declaration=re.search(r'internal delegate bool PromptTopicSemanticEvaluator\([^;]+;',actual).group()
 if not any(declaration in value for value in files.values()):
  files['PromptTopicSemanticEvaluator.cs']='namespace AnimusForge { '+declaration+' }'
 manifest.append(dict(file=relative,signature='PromptTopicSemanticEvaluator',sha256=hashlib.sha256(declaration.encode()).hexdigest(),source_extracted_type=True))
 trigger_path='src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.WeeklyTriggers.cs'
 trigger_source=(root/trigger_path).read_text(encoding='utf-8-sig')
 trigger_signatures=['internal void AddWeeklyTrigger(', 'internal void AttachPendingWeeklyTriggers(', 'internal void PrunePendingWeeklyTriggers(']
 if 'WeeklyActionOutcomePublicationOwner.cs' in files:
  trigger_signatures += ['internal bool AttachConfirmedWeeklyOutcome(', 'private static bool HasExactWeeklyActionOutcomeTrigger(']
 if not any('void AttachPendingWeeklyTriggers(' in value for value in files.values()):
  bodies=[ex.declaration(trigger_source, sig) for sig in trigger_signatures]
  files['MemoryBusinessStateOwner.DailyTriggers.cs']='using System;using System.Linq;using System.Collections.Generic;using AnimusForge.Refactor.Runtime;namespace AnimusForge {internal sealed partial class MemoryBusinessStateOwner {'+'\n'.join(bodies)+'}}'
  manifest.extend(dict(file=trigger_path,signature=sig,sha256=hashlib.sha256(body.encode()).hexdigest(),source_extracted_rule=True) for sig,body in zip(trigger_signatures,bodies))
 # Pure dependencies are extracted from their current sole authorities, never reimplemented.
 def pure_type(relative, type_name, signatures, namespace='AnimusForge', prefix='', suffix=''):
  if any('class '+type_name in value for value in files.values()):return
  source=(root/relative).read_text(encoding='utf-8-sig')
  bodies=[ex.declaration(source, signature) for signature in signatures]
  files[type_name+'.cs']='using System;using System.IO;using System.Text;using System.Linq;using System.Security.Cryptography;using System.Collections.Generic;using System.Text.RegularExpressions;using Newtonsoft.Json;using static AnimusForge.MemoryBusinessStateOwner;namespace '+namespace+' { internal static class '+type_name+' { '+prefix+'\n'+'\n'.join(bodies)+'\n'+suffix+' } }'
  manifest.extend(dict(file=relative,signature=sig,sha256=hashlib.sha256(body.encode()).hexdigest(),source_extracted_rule=True) for sig,body in zip(signatures,bodies))
 pure_type('src/AF.GameAdapter.Bannerlord/Prompt/MemorySummaryInputCaptureAdapter.cs','MemorySummaryInputCaptureAdapter',['internal static T CloneMemorySummarySource<T>(', 'internal static string ComputeMemorySummaryFingerprint('], namespace='AnimusForge.Refactor.Adapters')
 pure_type('src/AF.GameAdapter.Bannerlord/Prompt/MemoryRecallInputCaptureAdapter.cs','MemoryRecallInputCaptureAdapter',['internal static bool TryStripPlayerSpeechPrefix('],namespace='AnimusForge.Refactor.Adapters')
 pure_type('src/modules/AF.Module.Memory/Recall/HistoryArchiveRecallOwner.cs','HistoryArchiveRecallOwner',['internal static bool IsSystemFactLine('])
 pure_type('src/AF.GameAdapter.Bannerlord/UI/Errors/LlmRetryPrompt.cs','LlmRetryPrompt',['public static string BuildFailureDetail(', 'private static string NormalizeFullText('])
 codec_path='src/modules/AF.Module.Economy/Execution/Party/PartyTransferExecutionContext.cs'
 if not any('class PartyTransferTagCodec' in value for value in files.values()):
  body=ex.declaration((root/codec_path).read_text(encoding='utf-8-sig'),'internal static class PartyTransferTagCodec')
  files['PartyTransferTagCodec.cs']='using System;using System.Text.RegularExpressions;namespace AnimusForge {'+body+'}'
  manifest.append(dict(file=codec_path,signature='PartyTransferTagCodec',sha256=hashlib.sha256(body.encode()).hexdigest(),source_extracted_rule=True))
 pure_type('src/AF.GameAdapter.Bannerlord/Prompt/PersonaIdentityPromptCaptureAdapter.cs','PersonaIdentityPromptCaptureAdapter',['internal static string NormalizePlayerHistoryLineForPrompt('],namespace='AnimusForge.Refactor.Adapters',suffix='private static string BuildPlayerPublicDisplayNameForPrompt() => throw new InvalidOperationException("Live player identity is outside memory state replay"); internal static TaleWorlds.CampaignSystem.Hero ResolveCurrentPlayerIdentityObserverForPrompt() => throw new InvalidOperationException("Live observer is outside memory state replay"); internal static bool TryBuildPlayerPublicDisplayNameForPrompt(TaleWorlds.CampaignSystem.Hero observer, out string displayName, out bool complete) => throw new InvalidOperationException("Live display identity is outside memory state replay");')
 # Planning fixtures do not otherwise need a live Hero; this identity-only engine leaf has no behavior.
 if not any(re.search(r'\bclass Hero\b', value) for value in files.values()):
  files['ControlledHeroIdentity.cs']='namespace TaleWorlds.CampaignSystem {internal sealed class Hero {}}'
 pure_type('src/AF.GameAdapter.Bannerlord/Memory/MemoryHistoryCommitBannerlordAdapter.cs','MemoryHistoryCommitBannerlordAdapter',['internal static bool IsFirstMeetingNpcFactBody(', 'internal static bool IsFirstMeetingNpcFactLine(', 'internal static string BuildFirstMeetingNpcFactText()', 'internal static string BuildFirstMeetingNpcFactText(Hero observer)', 'internal static string NormalizeFirstMeetingNpcFactForPrompt('])
 if 'MemoryHistoryCommitBannerlordAdapter.cs' in files and 'using TaleWorlds.CampaignSystem;' not in files['MemoryHistoryCommitBannerlordAdapter.cs']:
  files['MemoryHistoryCommitBannerlordAdapter.cs']='using TaleWorlds.CampaignSystem;\n'+files['MemoryHistoryCommitBannerlordAdapter.cs']
 # The memory tests observe only these actual maintenance-owned state slots and budget rule.
 # This is not a substitute for the separately executed Daily controller orchestration replay.
 daily_path='src/AF.GameAdapter.Bannerlord/Weekly/CampaignDailyMaintenanceController.cs'
 daily=(root/daily_path).read_text(encoding='utf-8-sig')
 if not any('class CampaignDailyMaintenanceController' in value for value in files.values()):
  members=daily_maintenance_replay_members(daily,ex)
  files['DailyMaintenanceReplayState.cs']='using System;namespace AnimusForge {internal sealed class CampaignDailyMaintenanceController {'+'\n'.join(members)+'} public partial class MyBehavior {private readonly CampaignDailyMaintenanceController _dailyMaintenanceController=new();}}'
  manifest.append(dict(file=daily_path,sha256=hashlib.sha256('\n'.join(members).encode()).hexdigest(),source_extracted_state_and_budget=True,orchestration='NOT_IN_THIS_FIXTURE'))
 if not any('class DuelSettings' in value for value in files.values()):
  files['ControlledMemorySettings.cs']='namespace AnimusForge {internal sealed class DuelSettings {internal static DuelSettings GetSettings()=>null; internal int MemoryCandidateLimit,MemoryFinalInjectCount,MemoryPreprocessMode,MemorySummaryRequestsPerMinute,DailyMaintenanceFrameBudgetMs,RecentDialogueTurns;}}'
 if not any('class AIConfigHandler' in value for value in files.values()):
  fields=' '.join('internal static '+kind+' '+name+' => throw new System.InvalidOperationException("ONNX/config access outside memory state replay");' for kind,name in [('string','DuelInstruction'),('string','RewardInstruction'),('string','LoanInstruction'),('string','SurroundingsInstruction'),('System.Collections.Generic.List<string>','DuelTriggerKeywords'),('System.Collections.Generic.List<string>','RewardTriggerKeywords'),('System.Collections.Generic.List<string>','LoanTriggerKeywords'),('System.Collections.Generic.List<string>','SurroundingsTriggerKeywords')])
  files['ControlledSemanticConfig.cs']='namespace AnimusForge { internal static class AIConfigHandler {'+fields+' internal static string GetGuardrailRuleInstruction(string tag)=>throw new System.NotSupportedException(); internal static System.Collections.Generic.List<string> GetGuardrailRuleKeywords(string tag)=>throw new System.NotSupportedException(); internal static bool IsGuardrailSemanticHit(string a,string b,string tag,string instruction,System.Collections.Generic.List<string> keywords,out string matched,out float score,System.Collections.Generic.HashSet<string> excluded)=>throw new System.NotSupportedException();}}'
 # Missing logging sinks are controlled leaves; business state/guards are not replaced.
 for name in list(files):
  if 'class Logger' in files[name] and 'static void Obs(' not in files[name]:
   files[name]=re.sub(r'(class Logger\s*\{)',r'\1 public static void Obs(params object[] args){} public static void Metric(params object[] args){} public static void Metric(string name, bool ok){}',files[name],count=1)
  if 'class Logger' in files[name] and 'Metric(string name, bool ok)' not in files[name]:
   files[name]=re.sub(r'(class Logger\s*\{)',r'\1 public static void Metric(string name, bool ok){}',files[name],count=1)
 if not any('class PerfProbe' in value for value in files.values()):
  files['ControlledPerfProbe.cs']='namespace AnimusForge {internal static class PerfProbe {internal static System.IDisposable Scope(string name)=>null;}}'
 for name in ['Business.cs','Product.cs','Sealing.cs','Terminal.cs']:
  if name in files and 'using AnimusForge.Refactor.Adapters;' not in files[name]:
   files[name]='using AnimusForge.Refactor.Adapters;\n'+files[name]
 for contract in ['InteractionContracts.cs','LlmContracts.cs']:
  path=root/'src/AF.Contracts/Internal'/contract
  if contract not in files:files[contract]=path.read_text(encoding='utf-8-sig')
 codec=root/'src/modules/AF.Module.Llm/Protocol/JsonResponseTextCodec.cs'
 if codec.exists():files[codec.name]=codec.read_text(encoding='utf-8-sig')
 for rel in FILES:
  path='src/modules/AF.Module.Memory/'+rel;text=(root/path).read_text(encoding='utf-8-sig')
  if Path(path).name not in files and not (Path(path).stem in ['MemorySealingOwner','MemoryMaintenanceWorkBudget','CooperativeMemoryQueueSort','InteractionMemoryRecoveryLedger'] and any('class '+Path(path).stem in v for v in files.values())):files[Path(path).name]=text
  manifest.append(dict(file=path,sha256=hashlib.sha256(text.encode()).hexdigest(),whole_component=True))

def enable_expression_declarations(ex):
 original=ex.declaration
 def declaration(text,signature,optional=False):
  start=text.find(signature)
  if start<0:return original(text,signature,optional)
  opening=text.find('{',start);arrow=text.find('=>',start);endline=text.find('\n',start)
  if arrow>=0 and (opening<0 or arrow<opening):
   # These actual host expressions contain no statement-bodied lambdas.
   end=text.index(';',arrow)+1
   return text[start:end]
  return original(text,signature,optional)
 ex.declaration=declaration

def statement_body(body):
 if '{' not in body and '=>' in body:
  head,expr=body.rsplit('=>',1)
  return head+'{ '+('' if ' void ' in head else 'return ')+expr.strip()+' }'
 return body


def captured_input_source(root, manifest, ex):
 """Current capture + execution/parser members; unused apply/publication stays out.
 The original harness still supplies only gateway, clock and live engine/config facts.
 """
 def read(relative):
  text=(root/relative).read_text(encoding='utf-8-sig').replace('\r\n','\n')
  manifest.append(dict(file=relative,sha256=hashlib.sha256(text.encode()).hexdigest(),current_source=True))
  return text
 def block(text):
  match=re.search(r'^namespace ([\w.]+);',text,re.M)
  assert match, 'Expected current file-scoped namespace'
  return 'namespace '+match[1]+' {'+text[:match.start()]+text[match.end():]+'\n}'
 root_source=read('src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemorySummaryInput.cs')
 commit='new MemorySummaryCommitCapabilities { State = _memoryBusinessState, Queue = () => MemoryQueueState, Record = () => _campaignCharacterRecordCapture }'
 assert root_source.count(',\n        '+commit)==1
 # Captured suite stops at receipt production. Apply/publication is the terminal suite.
 root_source=root_source.replace(',\n        '+commit,'')
 capture=read('src/AF.GameAdapter.Bannerlord/Prompt/MemorySummaryInputCaptureAdapter.cs')
 for signature,label in [('internal MemorySummaryInput CaptureMemorySummaryInput(','Capture'),('internal bool IsMemorySummaryInputCurrent(','Check'),('internal static T CloneMemorySummarySource<T>(', 'Clone'),('internal static string ComputeMemorySummaryFingerprint(', 'Fingerprint')]:
  body=ex.declaration(capture,signature);opening=body.index('{')+1
  capture=capture.replace(body,body[:opening]+'\n Probe.Call("'+label+'");'+body[opening:],1)
 for name in ['BuildMemorySummarySystemPrompt','BuildMemorySummaryUserPrompt','BuildMajorActionSummarySystemPrompt','BuildMajorActionSummaryUserPrompt','BuildMemoryOverviewSummarySystemPrompt','BuildMemoryOverviewSummaryUserPrompt']:
  body=ex.declaration(capture,'internal static string '+name+'(');opening=body.index('{')+1
  capture=capture.replace(body,body[:opening]+'\n Probe.Call("'+name+'");'+body[opening:],1)
 app=read('src/AF.GameAdapter.Bannerlord/Memory/MemorySummaryApplicationAdapter.cs')
 # Keep every current execution/parser/queue receipt member, without cold admission
 # and commit members that this captured-only harness intentionally never invokes.
 signatures=['internal MemorySummaryApplicationAdapter(', 'internal async Task<MyBehavior.CapturedMemorySummaryResult> ExecuteAsync(', 'internal static bool TryParseMemoryOverviewResponse(', 'internal static bool TryParseMajorActionSummaryResponse(', 'internal static bool TryParseMemorySummaryResponse(', 'internal static string BuildDailyMemoryLineForPrompt(', 'internal static string BuildMajorActionSummarySourceLine(', 'internal static int GetMajorActionSummaryTargetChars(', 'internal static string BuildMemoryOverviewBlockSourceText(', 'internal async Task<MyBehavior.DailySummaryQueueResult> ExecuteDailySummaryQueueItemAsync(', 'internal async Task<MyBehavior.MemorySummaryExecutionResult> ExecuteMemorySummaryJobAsync(', 'internal async Task<MyBehavior.MajorActionSummaryExecutionResult> ExecuteMajorActionSummaryJobAsync(', 'internal async Task<MyBehavior.MemoryOverviewExecutionResult> ExecuteMemoryOverviewJobAsync(']
 fields=re.findall(r'^ private readonly [^\n]+;',app,re.M)
 fields=[field for field in fields if '_commit' not in field]
 members=[]
 for signature in signatures:
  body=ex.declaration(app,signature)
  manifest.append(dict(file='src/AF.GameAdapter.Bannerlord/Memory/MemorySummaryApplicationAdapter.cs',signature=signature,sha256=hashlib.sha256(body.encode()).hexdigest(),source_extracted_member=True))
  if signature.startswith('internal MemorySummaryApplicationAdapter('):
   assert body.count(', MemorySummaryCommitCapabilities commit = null')==1 and body.count(' _commit=commit;')==1
   body=body.replace(', MemorySummaryCommitCapabilities commit = null','').replace(' _commit=commit;','')
  members.append(body)
 app='namespace AnimusForge {using System;using System.Text;using System.Linq;using System.Collections.Generic;using System.Threading.Tasks;using AnimusForge.Refactor.Runtime;using TaleWorlds.CampaignSystem;using TaleWorlds.CampaignSystem.Settlements;using TaleWorlds.Library;internal sealed class MemorySummaryApplicationAdapter {'+'\n'.join(fields+members)+'}}'
 # Execution owns the only delay now. This is a controlled clock seam, not policy.
 assert app.count('milliseconds => Task.Delay(milliseconds)')==1
 app=app.replace('milliseconds => Task.Delay(milliseconds)','milliseconds => MyBehavior.Instance.FixtureDelayAsync(milliseconds)')
 return block(root_source)+'\n'+block(capture)+'\n'+app

def include_captured_leaves(root, files, manifest, ex):
 """Link current rendering/config rules. Only external engine lookups are controlled."""
 def leaf(relative,type_name,signatures,namespace='AnimusForge',prefix='',suffix='',usings=''):
  text=(root/relative).read_text(encoding='utf-8-sig').replace('\r\n','\n')
  bodies=[statement_body(ex.declaration(text,sig)) for sig in signatures]
  files[type_name+'.cs']='using System;using System.Linq;using System.Text;using System.Text.RegularExpressions;using System.Collections.Generic;using TaleWorlds.CampaignSystem;using TaleWorlds.CampaignSystem.Settlements;using TaleWorlds.Library;'+usings+' namespace '+namespace+' { internal static class '+type_name+' { '+prefix+'\n'+'\n'.join(bodies)+'\n'+suffix+'}}'
  manifest.extend(dict(file=relative,signature=sig,sha256=hashlib.sha256(ex.declaration(text,sig).encode()).hexdigest(),source_extracted_member=True) for sig in signatures)
 leaf('src/AF.GameAdapter.Bannerlord/Memory/MemoryEntityIdentityBannerlordAdapter.cs','MemoryEntityIdentityBannerlordAdapter',['internal static Hero FindHeroById(','internal static bool IsHeroNpcEligibleForCompressedMemory(','internal static bool IsMemoryEntityEligibleForCompressedMemory(','internal static string ResolveHeroName(','internal static string ResolveClanName(','internal static string ResolveKingdomName(','internal static string ResolveDisplayNameBySettlementEntry('],suffix='internal static int GetCurrentGameDayIndexSafe()=>MyBehavior.ReplayCurrentDay();')
 leaf('src/AF.GameAdapter.Bannerlord/Configuration/LlmRequestConfigurationCaptureAdapter.cs','LlmRequestConfigurationCaptureAdapter',['internal static int GetMemoryCompressionDenominatorFromSettings(','internal static int GetMemoryOverviewStartBlockCountFromSettings(','internal static int GetMemoryOverviewTargetCharsFromSettings('],namespace='AnimusForge.Refactor.Adapters')
 leaf('src/modules/AF.Module.Prompt/Composition/UncompressedMemoryMessageAssemblyOwner.cs','UncompressedMemoryMessageAssemblyOwner',['internal static bool IsUnknownMemorySceneLabel(','internal static string ResolveMemoryLineSceneForPrompt('])
 leaf('src/modules/AF.Module.Memory/Recall/MemoryRecallContextOwner.cs','MemoryRecallContextOwner',['internal static string FormatMemoryHourRange('])
 leaf('src/AF.GameAdapter.Bannerlord/Records/CampaignBattleRecordCaptureAdapter.cs','CampaignBattleRecordCaptureAdapter',['internal static string StripBattlePlayerMarker('])
 leaf('src/modules/AF.Module.Memory/Records/NpcActionRecordOwner.cs','NpcActionRecordOwner',['internal static string RenderText(','internal static string RewriteNpcActionSecondPersonPronouns(','internal static string BuildMetadata('])
 leaf('src/AF.GameAdapter.Bannerlord/Records/CampaignCharacterRecordCaptureAdapter.cs','CampaignCharacterRecordCaptureAdapter',['internal static string BuildNpcActionMetadataNarrativeSuffix('],usings='using static AnimusForge.MemoryEntityIdentityBannerlordAdapter;')
 leaf('src/AF.GameAdapter.Bannerlord/Prompt/MemoryRecallInputCaptureAdapter.cs','MemoryRecallInputCaptureAdapter',['internal static string ResolveCapturedMemoryLineSceneForPrompt(','internal static bool TryStripPlayerSpeechPrefix('],namespace='AnimusForge.Refactor.Adapters')
 leaf('src/AF.GameAdapter.Bannerlord/Prompt/PersonaIdentityPromptCaptureAdapter.cs','PersonaIdentityPromptCaptureAdapter',['internal static string NormalizePlayerHistoryLineForPrompt('],namespace='AnimusForge.Refactor.Adapters',suffix='internal static string BuildPlayerPublicDisplayNameForPrompt(Hero hero=null)=>MyBehavior.ReplayPublicName(hero); internal static Hero ResolveCurrentPlayerIdentityObserverForPrompt()=>Hero.MainHero; internal static bool TryBuildPlayerPublicDisplayNameForPrompt(Hero observer,out string name,out bool complete){name=MyBehavior.ReplayPublicName(observer);complete=true;return !string.IsNullOrWhiteSpace(name);}')
 files['ControlledSummaryScene.cs']='namespace AnimusForge.Refactor.Adapters {internal static class SceneLocationPromptCaptureAdapter {internal static string ResolveCurrentMemorySceneLabel()=>AnimusForge.MyBehavior.ReplayCurrentScene();}}'
 for name in ['Program.cs','Fixture.cs']:
  if name not in files:continue
  text=files[name]
  assert text.count('public sealed class Hero {')==1
  hero_facts=' public bool IsAlive=true,IsDisabled;' + ('' if 'static Hero MainHero' in text else ' public static Hero MainHero;')
  text=text.replace('public sealed class Hero {','public sealed class Hero {'+hero_facts,1)
  text=text.replace('public sealed class DuelSettings {','public sealed class DuelSettings { public int MemoryCandidateLimit,MemoryFinalInjectCount,MemoryPreprocessMode,MemorySummaryRequestsPerMinute,RecentDialogueTurns; public double DailyMaintenanceFrameBudgetMs;',1)
  text=text.replace('sealed class ApiCallResult {','internal sealed class ApiCallResult {',1)
  text=text.replace('Task FixtureDelayAsync(int milliseconds)', 'internal Task FixtureDelayAsync(int milliseconds)',1)
  files[name]=text
 files['ControlledSummaryFacts.cs']='using TaleWorlds.CampaignSystem;namespace AnimusForge {public partial class MyBehavior {internal static int ReplayCurrentDay()=>GetCurrentGameDayIndexSafe();internal static string ReplayCurrentScene()=>ResolveCurrentMemorySceneLabel();internal static string ReplayPublicName(Hero hero)=>BuildPlayerPublicDisplayNameForPrompt(hero);}}'
 # Result DTOs have not changed identity: only their visibility must match current
 # MyBehavior so the real application can return them through the actual thin facade.
 for name in ['Product.cs','Business.cs']:
  if name in files:
   files[name]=re.sub(r'private sealed class (MemorySummaryExecutionResult|MajorActionSummaryExecutionResult|MemoryOverviewExecutionResult|DailySummaryQueueResult)\b',r'internal sealed class \1',files[name])

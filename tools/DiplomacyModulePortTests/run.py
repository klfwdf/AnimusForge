"""DPL-100: real bridges/adapters against recording engine owners, plus exact caller inverse."""
from pathlib import Path
import argparse, importlib.util, subprocess, re
ROOT=Path(__file__).resolve().parents[2]
HERE=Path(__file__).resolve().parent
BASELINE='23f4d467'
def read(p): return (ROOT/p).read_text(encoding='utf-8-sig')
def old(p): return subprocess.check_output(['git','show',BASELINE+':'+p],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n').replace('\r','\n')
def load(name,p):
 spec=importlib.util.spec_from_file_location(name,ROOT/p);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m
util=load('port_util','tools/ModuleFrameworkApiTests/run.py')
declaration=load('port_decl','tools/ChannelCutoverBoundaryTests/run.py').declaration
SOURCES=['Refactor/Contracts/'+n+'.cs' for n in ['DiplomacyModulePorts','AfTributePowerContext','WorldDiplomacyPolicySignalSnapshot','WorldDiplomacyPresentationPort','WorldDiplomacyPresentationContracts','WorldDiplomacyTimelineQueryContracts','WorldDiplomacyTimelineDocumentQueryContracts','WorldDiplomacyDocumentReadCommandContracts']]
SOURCES += ['src/bridges/Diplomacy/'+n+'.cs' for n in ['DiplomacyConversationBridge','DiplomacyPolicyObservationBridge','DiplomacyModuleServices']]
SOURCES += ['src/modules/AF.Module.Diplomacy/Adapters/'+n+'.cs' for n in ['DiplomacyModule','DiplomacyConversationModuleAdapter','WorldDiplomacyModuleAdapter','DiplomacyIdentityResolver']]
SOURCES += ['src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyTimelineRevisionApplication.cs']
SOURCES += ['src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyTimelineApplication.cs']
SOURCES += ['src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyDiscussionApplication.cs','WorldDiplomacyDiscussionEligibilityRules.cs']
SOURCES += ['src/modules/AF.Module.Diplomacy/Application/DiplomacyConversationEligibilityApplication.cs']
SOURCES += ['src/modules/AF.Module.Diplomacy/Application/DiplomacyIndependentPeaceApplication.cs']
SOURCES += ['src/modules/AF.Module.Diplomacy/Application/DiplomacyTributePowerApplication.cs']
SOURCES += ['src/modules/AF.Module.Diplomacy/Application/DiplomacyPostprocessContextApplication.cs']
SOURCES += ['src/modules/AF.Module.Diplomacy/Application/DiplomacyPromptApplication.cs']
SOURCES += ['src/modules/AF.Module.Diplomacy/Application/DiplomacyOralTagApplication.cs']
SOURCES += ['src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyTickApplication.cs']
SOURCES += ['src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyCampaignApplication.cs']
SOURCES += ['Refactor/Adapters/'+n+'Adapter.cs' for n in ['WorldDiplomacyTimelineRevisionQuery','WorldDiplomacyTimelineDocumentQuery','WorldDiplomacyDocumentReadCommand']]
def boundaries():
 adapter=read('src/modules/AF.Module.Diplomacy/Adapters/DiplomacyConversationModuleAdapter.cs')
 for legacy in ['CanInjectDiplomacyRuleForExternal','CanUseDiplomacyActionPostprocessForExternal',
                'CanUseFullDiplomacyActionPostprocessForExternal','CanUseNpcSovereignDeclareWarPostprocessForExternal',
                'IsIndependentClanPeacePostprocessTag','CanUseIndependentClanPeaceForExternal']:
  assert 'DiplomacyBehavior.'+legacy not in adapter, 'Oral eligibility still delegates full use case to Behavior: '+legacy
 assert 'DiplomacyBehavior.CaptureEligibilitySnapshot' in adapter, 'Oral adapter lost its snapshot-only source'
 assert 'DiplomacyIndependentPeaceApplication.CanUse(' in adapter, 'Independent peace bypasses Application admission'
 assert 'DiplomacyBehavior.TryBuildTributePowerContext' not in adapter, 'Tribute calculator still delegates full use case to Behavior'
 assert 'DiplomacyTributePowerApplication.TryBuild(' in adapter, 'Tribute calculation bypasses Application'
 assert 'DiplomacyBehavior.BuildDiplomacyPostprocessContext' not in adapter, 'Prompt context still delegates full use case to Behavior'
 assert 'DiplomacyPostprocessContextApplication.Build(' in adapter, 'Prompt context bypasses Application'
 assert 'DiplomacyBehavior.ProcessDiplomacyTagsDispatch' not in adapter, 'Tag use case still delegates to Behavior'
 assert 'DiplomacyOralTagApplication.Process(' in adapter, 'Tag dispatch bypasses Application'
 oral=read('src/modules/AF.Module.Diplomacy/Direct/DiplomacyBehavior.Actions.cs')
 for signature in ['internal static bool CanInjectDiplomacyRuleForExternal(',
                   'internal static bool CanUseDiplomacyActionPostprocessForExternal(',
                   'internal static bool CanUseFullDiplomacyActionPostprocessForExternal(',
                   'internal static bool CanUseNpcSovereignDeclareWarPostprocessForExternal(',
                   'internal static bool IsIndependentClanPeacePostprocessTag(']:
  assert 'DiplomacyConversationEligibilityApplication.' in declaration(oral,signature), 'Old oral eligibility branch retained: '+signature
 assert 'DiplomacyIndependentPeaceApplication.CanUse(' in declaration(oral,'private static bool TryResolveIndependentClanPeaceContext('), 'Independent peace context retains old branch policy'
 assert 'DiplomacyTributePowerApplication.TryBuild(' in declaration(oral,'internal static bool TryBuildTributePowerContext('), 'Old tribute calculation retained in Behavior'
 context_owner=read('src/modules/AF.Module.Diplomacy/Direct/DiplomacyBehavior.cs')
 assert 'ProcessDiplomacyTagsDispatch' not in context_owner, 'Duplicate command entry retained on Behavior: ProcessDiplomacyTagsDispatch'
 assert 'private void ProcessDiplomacyTags(' not in context_owner and 'private string ProcessSingleDiplomacyTag(' not in context_owner, 'Tag Behavior retains old processor'
 assert 'DiplomacyOralTagApplication.Process(' in adapter and 'DiplomacyOralTagApplication.Process(' not in context_owner, 'Tag dispatch must have one module-boundary entry only'
 world_adapter=read('src/modules/AF.Module.Diplomacy/Adapters/WorldDiplomacyModuleAdapter.cs')
 assert 'WorldDiplomacyTickApplication.Run(' in world_adapter and 'WorldDiplomacyBehavior.Instance?.OnEngineTick()' not in world_adapter, 'Module tick delegates whole workflow to Behavior'
 world_owner=read('src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs')
 tick_forwarder=declaration(world_owner,'public void OnEngineTick(')
 assert 'WorldDiplomacyTickApplication.Run(' in tick_forwarder and 'ProcessCompletedJobs()' not in tick_forwarder, 'Old tick ordering retained in Behavior'
 context_forwarder=declaration(context_owner,'internal static string BuildDiplomacyPostprocessContext(')
 assert 'DiplomacyPostprocessContextApplication.Build(' in context_forwarder and not any(
  token in context_forwarder for token in ['StringBuilder','Kingdom.All','FactionManager.','[ACTION:DIPLOMACY:']), 'Old prompt context retained in Behavior'
 paths=['AIConfigHandler.cs','ShoutBehavior.cs','ShoutBehavior.NativeTurnCommit.cs','DiplomacyPeaceTermsService.cs','NpcTributeVassalageBehavior.cs','src/modules/AF.Module.Social/Proactive/ProactiveCandidateQualification.cs']
 paths += ['src/modules/AF.Module.Conversation/Channels/'+p for p in ['Scene/ShoutBehavior.ScenePostprocess.cs','Scene/ShoutBehavior.SceneConversationChains.cs','Courier/CourierDeliveryBehavior.DomainCommit.cs','Courier/CourierDeliveryBehavior.DeliveryLifetime.cs']]
 count=0
 for p in paths:
  current=read(p);prior=old(p); count+=current.count('DiplomacyConversationBridge.')
  for name in ['CanDiscussWorldDiplomacyForExternal','TryBuildProactiveDiscussionForExternal']:
   current=current.replace('DiplomacyConversationBridge.'+name,'WorldDiplomacyBehavior.'+name)
  current=current.replace('DiplomacyConversationBridge.','DiplomacyBehavior.')
  if p=='NpcTributeVassalageBehavior.cs':
   block=declaration(prior,'internal readonly struct AfTributePowerContext')
   assert declaration(read('Refactor/Contracts/AfTributePowerContext.cs'),'internal readonly struct AfTributePowerContext')==block
   prior=prior.replace(block+'\n\n','')
  assert current.strip()==prior.strip(), 'Caller guard/order/argument drift: '+p
 world='src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs'
 current_world=read(world)
 prior_world=old(world)
 prior_tick=declaration(prior_world,'public void OnEngineTick(')
 current_tick=declaration(current_world,'public void OnEngineTick(')
 assert 'WorldDiplomacyTickApplication.Run(ref source, _orchestration)' in current_tick and 'ProcessCompletedJobs()' not in current_tick, 'Tick predecessor still owns ordering'
 prior_world=prior_world.replace(prior_tick,current_tick)
 # Leaf marshal helpers introduced by callback-narrowing slices; strip only after a thin check.
 for signature in ('private (int, int) GetDeclarationCharacterRange(',):
  if signature not in current_world: continue
  leaf=declaration(current_world,signature)
  assert leaf.count('WorldDiplomacy')<=6 and 'if (' not in leaf and 'foreach' not in leaf and 'Application.' not in leaf,'leaf helper regrew orchestration or app chaining: '+signature
  current_world=current_world.replace('\t'+leaf+'\n','',1)
 # Every use-case owner moved to a compiled Application; each surviving predecessor must be a
 # single forwarder to its owner. Baseline substitution mirrors DiplomacyArchitectureTests.
 for signature,owner in [
  ('private void ProcessAnalyzedDocument(', 'WorldDiplomacyDocumentExecutionApplication.'),
  ('private void ProcessAnalyzedMultiActionDocument(', 'WorldDiplomacyDocumentExecutionApplication.'),
  ('private void FinalizePublishedDocumentAfterAnalysis(', 'WorldDiplomacyDocumentExecutionApplication.'),
  ('private bool TryIncludeResultSettlementTarget(', 'WorldDiplomacyDocumentExecutionApplication.'),
  ('private void StartDocumentPropagation(', 'WorldDiplomacyPublicationRoutingApplication.'),
  ('private void ReconcileAnalyzedPlayerDeclarationWithReachedCourts(', 'WorldDiplomacyPublicationRoutingApplication.'),
  ('private void TryScheduleMandatoryCourtResponse(', 'WorldDiplomacyCourtResponseApplication.'),
  ('private void TrySettleRelayOffer(', 'WorldDiplomacyOfferApplication.'),
  ('private void NotifyExternalDiplomacyResolvedInternal(', '_orchestration.NotifyExternalDiplomacyResolved'),
  ('private bool CanIssueWarThreat(', 'WorldDiplomacyWarAdmissionApplication.'),
  ('private bool CanDeclareWar(', 'WorldDiplomacyWarAdmissionApplication.'),
  ('private void ExecuteImmediateIntent(', 'WorldDiplomacyImmediateActionApplication.'),
  ('private void HandleDisabledState(', 'WorldDiplomacyRoundApplication.'),
  ('private void CommitEmbeddedRoundPlan(', 'WorldDiplomacyRoundApplication.'),
  ('private bool TryApplyUltimatumComplianceDomesticPenalty(', 'WorldDiplomacyThreatSettlementApplication.'),
  ('private bool TryApplyDiplomaticThreatPolicyConditionCancellation(', 'WorldDiplomacyThreatSettlementApplication.'),
  ('private bool TryApplyDiplomaticThreatIssuerRelationReward(', 'WorldDiplomacyThreatSettlementApplication.'),
  ('private bool ResolveDiplomaticThreatCompliance(', 'WorldDiplomacyThreatSettlementApplication.'),
  ('private void ApplyDiplomaticThreatReputationPenalty(', 'WorldDiplomacyThreatSettlementApplication.'),
  ('private void RetryDiplomaticThreatDomesticPenalties(', 'WorldDiplomacyThreatSettlementApplication.'),
  ('private void RetryDiplomaticThreatComplianceConsequences(', 'WorldDiplomacyThreatSettlementApplication.'),
  ('private void RetryDiplomaticThreatHistoryResults(', 'WorldDiplomacyThreatSettlementApplication.'),
  ('private int ApplyNationalPrestigeDelta(', 'WorldDiplomacyPrestigeApplication.'),
  ('private void SettleInternationalReputationForDocument(', 'WorldDiplomacyPrestigeApplication.'),
  ('private void RecoverUnsettledAiInternationalReputation(', 'WorldDiplomacyPrestigeApplication.'),
  ('private void ReconcileAllNationalPrestigeVassalRelations(', 'WorldDiplomacyPrestigeApplication.'),
  ('private void ReconcileNationalPrestigeVassalRelations(', 'WorldDiplomacyPrestigeApplication.'),
  ('private void ApplyZeroPrestigeBreachRelationPenalty(', 'WorldDiplomacyPrestigeApplication.'),
  ('private void AnchorInternationalReputationNaturalChangeDays(', 'WorldDiplomacyPrestigeApplication.'),
  ('private void ProcessInternationalReputationNaturalChange(', 'WorldDiplomacyPrestigeApplication.'),
  ('private void OnCampaignTick(', 'DiplomacyModuleServices.World.OnCampaignTick'),
  ('private void OnDailyTick(', 'DiplomacyModuleServices.World.OnDailyTick'),
  ('private void TryApplyInitialNewGamePeace(', 'WorldDiplomacyInitialPeaceApplication.'),
  ('private void EnsureCanonicalHistoryInitialized(', 'WorldDiplomacyHistoryCaptureApplication.|WorldDiplomacyRoundApplication.'),
  ('private void SyncCanonicalHistorySources(', 'WorldDiplomacyHistoryCaptureApplication.|WorldDiplomacyRoundApplication.'),
  ('private void CaptureCanonicalHistoryForJob(', 'WorldDiplomacyHistoryCaptureApplication.|WorldDiplomacyRoundApplication.'),
  ('private void RestoreSuspendedExchangeIfAny(', 'WorldDiplomacyHistoryCaptureApplication.|WorldDiplomacyRoundApplication.'),
  ('private void CompleteExchange(', 'WorldDiplomacyHistoryCaptureApplication.|WorldDiplomacyRoundApplication.'),
  ('private bool IsNonRootAiRelayNoActionAllowed(', 'WorldDiplomacyNoActionApplication.'),
  ('private bool CanUseResultSettlementTarget(', 'WorldDiplomacyNoActionApplication.'),
  ('private bool TryResolvePolicyConditionForThreat(', 'WorldDiplomacyThreatBindingApplication.'),
  ('private bool RegisterOrAdvanceDiplomaticThreat(', 'WorldDiplomacyThreatBindingApplication.'),
  ('private void ProcessDiplomaticThreatDocument(', 'WorldDiplomacyThreatBindingApplication.'),
  ('private void PublishPlayerAuthoredDocumentImmediately(', 'WorldDiplomacyDocumentPublicationApplication.'),
  ('private void RefreshPolicyDiplomacySignals(', 'WorldDiplomacyPolicyRoundApplication.'),
  ('private List<string> BuildPotentialDiplomaticActionIntents(', 'WorldDiplomacyActionSelectionApplication|WorldDiplomacyDocumentExecutionApplication.'),
  ('private List<string> BuildLegalDiplomaticActionIntents(', 'WorldDiplomacyActionSelectionApplication|WorldDiplomacyDocumentExecutionApplication.'),
  ('private List<string> BuildLegalDiplomaticDeclarationIntents(', 'WorldDiplomacyActionSelectionApplication|WorldDiplomacyDocumentExecutionApplication.'),
  ('private List<Kingdom> GetActionableDiplomaticTargets(', 'WorldDiplomacyActionSelectionApplication|WorldDiplomacyDocumentExecutionApplication.'),
  ('private List<Kingdom> GetRoundPlanActionableParticipants(', 'WorldDiplomacyActionSelectionApplication|WorldDiplomacyDocumentExecutionApplication.'),
  ('private List<Kingdom> GetResultSettlementActionableTargets(', 'WorldDiplomacyActionSelectionApplication|WorldDiplomacyDocumentExecutionApplication.'),
  ('private void RefreshResultSettlementActionSlots(', 'WorldDiplomacyActionSelectionApplication|WorldDiplomacyDocumentExecutionApplication.'),
  ('private bool EnsureGenerationJobHasKingdomStrategicProfile(', 'WorldDiplomacyJobPreparationApplication.'),
  ('private bool RefreshDiplomaticActionPresentationAndPrompt(', 'WorldDiplomacyJobPreparationApplication.'),
  ('private bool RefreshDiplomaticThreatPresentationAndPrompt(', 'WorldDiplomacyJobPreparationApplication.'),
  ('private bool TryRebuildPendingWorldDiplomacyJob(', 'WorldDiplomacyJobPreparationApplication.'),
  ('private void CommitAnalysis(', 'WorldDiplomacyAnalysisApplication.'),
  ('private void SuppressInvalidDocumentBeforePropagation(', 'WorldDiplomacyAnalysisApplication.'),
  ('private void PreservePublishedPlayerDocumentAfterRejectedMechanic(', 'WorldDiplomacyAnalysisApplication.'),
  ('private WorldDiplomacyPeaceTerms ParseAndValidatePeaceTerms(', 'WorldDiplomacyPeaceAdmissionApplication.'),
  ('private static bool AreOfferedPeaceTermsCurrentlyExecutable(', 'WorldDiplomacyPeaceAdmissionApplication.'),
  ('private bool IsCessionCurrentlyAllowed(', 'WorldDiplomacyPeaceAdmissionApplication.'),
  ('private List<Settlement> BuildCessionCandidates(', 'WorldDiplomacyPeaceAdmissionApplication.'),
  ('private void ReconcileActiveDiplomacyAfterLoad(', 'WorldDiplomacyRoundApplication.'),
  ('private void EnqueueGenerationJob(', 'WorldDiplomacyGenerationTaskApplication.'),
  ('private void EnqueueAnalysisJob(', 'WorldDiplomacyJobPreparationApplication.'),
  ('private void CommitFailedJob(', 'WorldDiplomacyFailureApplication.'),
  ('private void CommitGeneratedDocument(', 'WorldDiplomacyGeneratedCompletionApplication.'),
  ('private void AbandonRejectedGeneration(', 'WorldDiplomacyGenerationTaskApplication.'),
  ('private void BeginOrExtendRoundResultSettlement(', 'WorldDiplomacyRoundApplication.'),
  ('private void ScheduleNextResultSettlementTurn(', 'WorldDiplomacyTurnSchedulingApplication.'),
  ('private void HandleRoundDocumentProcessed(', 'WorldDiplomacyRoundProgressApplication.'),
  ('private void EnqueueRoundPlanJob(', 'WorldDiplomacyJobPreparationApplication.'),
  ('private void RetryDeferredCanonicalHistoryEntries(', '_orchestration.RetryDeferredCanonicalHistoryEntries'),
  ('private void CommitRoundPlan(', 'WorldDiplomacyRoundPlanApplication.'),
  ('private void ScheduleNextRelayHop(', 'WorldDiplomacyTurnSchedulingApplication.'),
  ('private void ProcessRelayArrivals(', 'WorldDiplomacyRoundProgressApplication.'),
  ('private void RecordDiplomacyWeeklyMaterial(', 'WorldDiplomacyHistoryCaptureApplication.'),
  ('private void ProcessRoundLifecycle(', 'WorldDiplomacyRoundApplication.'),
  ('private void CommitRoundCompression(', 'WorldDiplomacyRoundCompressionApplication.'),
  ('private void NormalizeStorage(', 'WorldDiplomacyStorageNormalizationApplication.')]:
  if signature not in prior_world or signature not in current_world: continue
  before=declaration(prior_world,signature);after=declaration(current_world,signature).replace('DiplomacyModuleServices.Policy.','WorldDiplomacyPolicyContext.')
  assert any(o in after for o in owner.split('|')), signature
  prior_world=prior_world.replace(before,after)
 # Whole-file parity is dead post-migration: orchestration owns the moved bodies.
 # What survives: retired privates must stay absent and no private method may hide
 # a second Application algorithm (checked below).
 retired_names=load('retired','tools/DiplomacyArchitectureTests/retired.py').RETIRED
 assert all(sig not in current_world for sig in retired_names),'retired predecessor returned: '+next(s for s in retired_names if s in current_world)
 policy='PolicySystem/Context/WorldDiplomacyPolicyContext.cs'
 before=old(policy);after=read(policy)
 ledger=declaration(before,'internal sealed class PublishedPolicyArtifactLedgerEntry')
 assert ledger==declaration(read('Refactor/Contracts/PublishedPolicyArtifactLedgerEntry.cs'),'internal sealed class PublishedPolicyArtifactLedgerEntry')
 before=before[:before.index('/// <summary>')]+before[before.index(ledger)+len(ledger):].lstrip('\n')
 snapshot=declaration(before,'internal sealed class WorldDiplomacyPolicySignalSnapshot')
 before=before.replace(snapshot,'').strip()
 start=before.index('result.Add(new WorldDiplomacyPolicySignalSnapshot')
 end=before.index('});',start)+3
 old_block=before[start:end]
 fields=re.findall(r'^\s*(\w+) = (.*?)(?:,)?$',old_block,re.M)
 new_block='result.Add(new WorldDiplomacyPolicySignalSnapshot(\n'+',\n'.join('\t\t\t\t\t\t'+value.rstrip(',') for _,value in fields)+'\n\t\t\t\t\t));'
 assert before.replace(old_block,new_block)==after.strip(),'Policy producer/cache/ordering drift'
 value_type=read('Refactor/Contracts/WorldDiplomacyPolicySignalSnapshot.cs')
 for name,_ in fields:
  assert name+' = '+name[0].lower()+name[1:]+';' in value_type,'Snapshot value lost: '+name
 for p in ['src/AF.GameAdapter.Bannerlord/Composition/ApplicationTickComposition.cs','src/AF.GameAdapter.Bannerlord/Composition/StartupPatchComposition.cs']:
  restored=read(p).replace('DiplomacyModuleServices.World.OnEngineTick()', 'WorldDiplomacyBehavior.Instance?.OnEngineTick()').replace('DiplomacyModuleServices.RegisterPatches(harmony)','WorldDiplomacyBehavior.RegisterHarmonyPatches(harmony)')
  assert restored==old(p),'Lifecycle order/guard drift: '+p
 for p in ['src/bridges/Diplomacy/DiplomacyConversationBridge.cs','src/bridges/Diplomacy/DiplomacyPolicyObservationBridge.cs']:
  assert not any(s in read(p) for s in ['foreach (','Regex','Campaign.Current','_af_world_diplomacy_v1','new Dictionary','lock (']), 'Bridge owns business/state: '+p
 # --- R1 closeout negative guards ---
 # Composition is lifecycle registration only; any extra wiring or a second registration fails.
 composition=read('src/modules/AF.Module.Diplomacy/Adapters/DiplomacyModuleComposition.cs')
 comp_body=composition[composition.index('internal static void Register('):]
 invoked=set(re.findall(r'([A-Za-z_][\w.]*)\s*\(',comp_body))
 assert invoked=={'Register','RegisterPatches','starter.AddBehavior','WorldDiplomacyBehavior','DiplomacyBehavior',
                  'WorldDiplomacyBehavior.RegisterHarmonyPatches'},'Composition gained non-lifecycle wiring: '+str(invoked)
 # Module port covers commands, queries, lifecycle, presentation and receipts; dropping an entry fails.
 port_decl=declaration(read('Refactor/Contracts/DiplomacyModulePorts.cs'),'internal interface IWorldDiplomacyModulePort')
 assert set(re.findall(r'\b([A-Za-z_]\w+)\s*\(',port_decl))=={'BuildMemory','CanDiscuss','TryBuildProactiveDiscussion','QueryTimelineRevision',
   'QueryTimelineDocuments','TryMarkDocumentRead','OnEngineTick','OnCampaignTick','OnDailyTick'},'ModulePort surface drift'
 assert set(re.findall(r'(\w+)\s*\{\s*get;',port_decl))=={'Presentation'},'ModulePort read-model surface drift'
 conv_decl=declaration(read('Refactor/Contracts/DiplomacyModulePorts.cs'),'internal interface IDiplomacyConversationPort')
 assert set(re.findall(r'\b([A-Za-z_]\w+)\s*\(',conv_decl))=={'BuildPrompt','CanInjectDiplomacyRule','CanUseDiplomacyActionPostprocess',
   'CanUseFullDiplomacyActionPostprocess','CanUseNpcSovereignDeclareWarPostprocess','CanUseIndependentClanPeace',
   'IsIndependentClanPeacePostprocessTag','BuildDiplomacyPostprocessContext','ProcessDiplomacyTags',
   'TryBuildTributePowerContext'},'Conversation port surface drift'
 # Module adapter may bind only narrow snapshot/leaves, never a whole use-case Behavior method.
 allowed_wdb={'TryCaptureMemory','Instance','TickSource','CampaignSource','TryGetTimelineRevisionSnapshot','TryGetTimelineState',
   'TryCaptureDiscussionCandidate','HasKnownDocumentForDiscussion','TryCaptureProactiveSpeaker','TryCaptureProactiveDocuments',
   'GetPlayerKingdomNameForProactive','FormatDateForProactive','ResolvePresentationPort'}
 for call in set(re.findall(r'WorldDiplomacyBehavior\.(\w+)',world_adapter)):
  assert call in allowed_wdb,'Module adapter gained a non-snapshot Behavior coupling: '+call
 assert 'WorldDiplomacyBehavior.Instance.' not in world_adapter
 # Application layer stays free of host/game types.
 for app_file in sorted((ROOT/'src/modules/AF.Module.Diplomacy/Application').glob('*.cs')):
  app_text=app_file.read_text(encoding='utf-8-sig')
  for bad in ['WorldDiplomacyBehavior','TaleWorlds.','MyBehavior','Campaign.Current','Task.Run(']:
   assert bad not in app_text,'Application layer coupled to host/game: '+app_file.name+' '+bad
 # Callback-hiding guard: no host method may sequence >=2 use-case owners or loop over owner calls.
 tok=re.compile(r'@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|//[^\n]*|/\*[\s\S]*?\*/|[{}]')
 def method_bodies(t):
  out=[]
  for m in re.finditer(r'(?m)^[ \t]*(?:private|internal|public)\s+(?:static\s+|readonly\s+|sealed\s+|override\s+|new\s+)*[\w<>\[\],.?()\[\]= ]+?\b(\w+)\s*\(',t):
   brace=t.find('{',m.end());depth=0
   if brace<0: continue
   for mm in tok.finditer(t,brace):
    v=mm.group()
    if v=='{':depth+=1
    elif v=='}':
     depth-=1
     if depth==0:out.append((m.group(1),t[m.start():mm.end()]));break
  return out
 for wf in sorted((ROOT/'src/modules/AF.Module.Diplomacy/World').glob('WorldDiplomacyBehavior*.cs')):
  for name,body in method_bodies(wf.read_text(encoding='utf-8-sig')):
   apps=set(re.findall(r'WorldDiplomacy\w+Application\.',body))
   flow=len(re.findall(r'\b(?:if|foreach|while|for)\s*\(',body))
   assert not (len(apps)>=2 and flow>=1),'host method re-hides multi-owner orchestration: '+wf.name+'::'+name
   assert len(apps)<3,'host method sequences multiple use-case owners: '+wf.name+'::'+name
 # Load-time sequencing is orchestration-owned: the Behavior may not keep a private
 # NormalizeStorage successor, and the orchestration member must call the Application.
 assert 'private void NormalizeStorage(' not in current_world,'load sequencer returned to the Behavior'
 orch=read('src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyOrchestration.cs')
 assert 'WorldDiplomacyStorageNormalizationApplication.' in declaration(orch,'public void NormalizeStorage('),'NormalizeStorage lost its Application owner'
 # Migration ordering lives in the normalization Application, not in host wrappers.
 norm_app=read('src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyStorageNormalizationApplication.cs')
 for owner in ('WorldDiplomacyCanonicalHistoryMigrationApplication.MigrateIfNeeded(',
   'WorldDiplomacyStorageMigration.MigrateAutonomousDecisionArchitectureIfNeeded(',
   'WorldDiplomacyStorageMigration.MigrateResultSettlementStateIfNeeded(',
   'WorldDiplomacyStorageMigration.MigrateDiplomacyPromptContractIfNeeded(',
   'WorldDiplomacyThreatStorageMigration.NormalizeDiplomaticThreats('):
  assert owner in norm_app,'migration ordering left the Application: '+owner
 norm_source=read('src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.StorageNormalizationSource.cs')
 assert not re.search(r'void Migrate\w+\(',norm_source),'normalization source regrew a migration-ordering op'
 norm_iface=declaration(norm_app,'internal interface IWorldDiplomacyStorageNormalizationSource')
 assert not re.search(r'void Migrate\w+\(',norm_iface) and 'NormalizeDiplomaticThreats' not in norm_iface,'normalization source exposes an ordered migration op'
 # Execution port stays a leaf surface: no writable storage, fixed width, leaf member bodies.
 exec_decl=declaration(read('src/modules/AF.Module.Diplomacy/Application/IWorldDiplomacyDocumentExecutionPort.cs'),
   'internal interface IWorldDiplomacyDocumentExecutionPort')
 assert 'WorldDiplomacyStorage' not in exec_decl and ' set;' not in exec_decl,'Execution port exposed writable storage state'
 assert len([l for l in exec_decl.splitlines() if l.strip() and not l.strip().startswith(('//','{','}')) and 'interface' not in l])==18,'Execution port width changed without review'
 assert 'IReadOnlyList<WorldDiplomacyThreat> Threats' in exec_decl,'read-only Threats snapshot pin lost'
 assert not re.search(r'(?<!ReadOnly)\bList<WorldDiplomacyThreat>',exec_decl),'mutable Threats list exposure reintroduced'
 port_impl=read('src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.DocumentExecutionPort.cs')
 for name,body in method_bodies(port_impl):
  assert body.count(';')<=6,'Execution port member regrew orchestration: '+name
 # Callback-hiding guards: the generation-admission use cases live on the
 # orchestration member now; no private Behavior successor may exist and the
 # orchestration bodies must not smuggle host control flow back in.
 fresh_world=read('src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs')
 flow=re.compile(r'\b(?:if|foreach|while|switch|for)\s*\(')
 for sig in ('private void EnqueueGenerationJob(','private void ProcessRelayArrivals(','private void TryScheduleMandatoryCourtResponse('):
  assert sig not in fresh_world,'predecessor adapter re-appeared: '+sig
 for sig in ('public void ProcessRelayArrivals(','public void TryScheduleMandatoryCourtResponse('):
  body=declaration(orch,sig)
  assert body is not None,'missing orchestration member '+sig
 prog_app=read('src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyRoundProgressApplication.cs')
 assert 'settlementSlot.SlotId' in prog_app,'settlement-slot dispatch left the RoundProgress Application'
 assert 'resolveKingdomId?.Invoke(round.InitiatorKingdomId)' in prog_app,'initiator fallback target selection left the RoundProgress Application'
 # The mandatory-reply orchestration member must not see the relay-transcript
 # decision; the Application owns it.
 assert 'RelayPlanned' not in declaration(orch,'public void TryScheduleMandatoryCourtResponse('),'mandatory orchestration member regrew relay reuse decision'
 assert 'round?.RelayPlanned == true' in read('src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyCourtResponseApplication.cs'),'mandatory reply must map RelayPlanned to transcript reuse in the Application'
 # App-side ownership pins: candidate composition and route filtering live in the Application.
 gen_app=read('src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyGenerationTaskApplication.cs')
 assert 'legalDeclarationIntents?.Invoke(' in gen_app and '.Distinct()' in gen_app,'generation app lost candidate composition ownership'
 assert 'Action<string, string, WorldDiplomacyDocument, string, string, int, int, string> enqueueRelayTurn' in prog_app,'relay enqueue delegate signature drifted'
 print(f'PASS exact inverse: {len(paths)} caller files / {count} routes; channel guards/ref/out/order unchanged; policy cadence and tick entry guards preserved')

def main():
 p=argparse.ArgumentParser();p.add_argument('--dotnet',default='dotnet');a=p.parse_args();boundaries()
 out=HERE/'.generated/current';out.mkdir(parents=True,exist_ok=True)
 (out/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>',encoding='utf-8')
 # Compile the existing immutable ledger DTO verbatim, never a hand-maintained mirror.
 entry=declaration(read('Refactor/Contracts/PublishedPolicyArtifactLedgerEntry.cs'),'internal sealed class PublishedPolicyArtifactLedgerEntry')
 (out/'LedgerEntry.cs').write_text('namespace AnimusForge;\n'+entry,encoding='utf-8')
 common=[HERE/'HostStubs.cs',HERE/'Program.cs',out/'LedgerEntry.cs']
 variants=[
  ('current',None),
  ('wrong_target',('src/bridges/Diplomacy/DiplomacyConversationBridge.cs','(hero ?? character?.HeroObject)?.StringId','character?.HeroObject?.StringId')),
  ('swap_tribute',('src/modules/AF.Module.Diplomacy/Adapters/DiplomacyConversationModuleAdapter.cs','ResolveKingdom(payerId), ResolveKingdom(receiverId)','ResolveKingdom(receiverId), ResolveKingdom(payerId)')),
  ('wrong_executor',('src/modules/AF.Module.Diplomacy/Adapters/DiplomacyConversationModuleAdapter.cs','new DiplomacyOralTagSource(ResolveHero(heroId))','new DiplomacyOralTagSource(null)')),
  ('drop_tick',('src/modules/AF.Module.Diplomacy/Adapters/WorldDiplomacyModuleAdapter.cs','new WorldDiplomacyBehavior.TickSource(WorldDiplomacyBehavior.Instance)','new WorldDiplomacyBehavior.TickSource(null)'))]
 for name,mutation in variants:
  folder=out/name;folder.mkdir(exist_ok=True);sources=[ROOT/s for s in SOURCES]
  if mutation:
   path,before,after=mutation; text=read(path);assert before in text
   target=folder/Path(path).name;target.write_text(text.replace(before,after),encoding='utf-8');sources[sources.index(ROOT/path)]=target
  proj=util.project(folder,'DiplomacyPortChecks',sources+common,executable=True)
  code,log=util.run_dotnet(a.dotnet,['run','--project',str(proj),'-c','Release'],out)
  (folder/'run.log').write_text(log,encoding='utf-8')
  if not mutation: print(log,end='');assert code==0,'Port execution failed'
  else: assert code!=0 and 'FAIL ' in log and 'error CS' not in log,log;print('PASS behavioral mutation rejected: '+name)
if __name__=='__main__':main()

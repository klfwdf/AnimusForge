"""Check orchestration ownership, deletion parity and executable diplomacy dependency boundaries."""
from pathlib import Path
import argparse,importlib.util,json,subprocess,os,re,sys
ROOT = next(p for p in Path(__file__).resolve().parents if (p/'AnimusForge.csproj').is_file())
HERE=Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT/'tests'))
from output_isolation import current_source_path, new_run_root, minimal_test_environment
BASELINE='19e9bb22'
def read(p):return current_source_path(ROOT, p).read_text(encoding='utf-8-sig')
def old(p):return subprocess.check_output(['git','show',BASELINE+':'+p],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
def load(name,p):
 s=importlib.util.spec_from_file_location(name,current_source_path(ROOT,p));m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m

ORCH='src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyOrchestration.cs'
APPDIR='src/modules/AF.Module.Diplomacy/Application/'

def method_of(source,name,vis=r'(?:public|internal)'):
 m=re.search(r'(?m)^(\s*)'+vis+r'\s+[\w<>,\.\[\]\? ]+\s+'+re.escape(name)+r'(?:<[\w, ]+>)?\s*\(',source)
 if not m:return None
 i=m.start();j=source.index('{',source.index('(',m.start()));depth=0
 for k in range(j,len(source)):
  if source[k]=='{':depth+=1
  elif source[k]=='}':
   depth-=1
   if depth==0:return source[i:k+1]
 return None

def any_method_of(source,name):
 return method_of(source,name,r'(?:public|internal|private|protected)')

# orchestration member -> expected Application owner call inside its body.
ORCH_OWNERS={
 'ProcessCompletedJobs':'WorldDiplomacyCompletionApplication.',
 'TryStartNextLlmJob':'WorldDiplomacyLlmDispatchApplication.',
 'ProcessCourtArrival':'WorldDiplomacyCourtResponseApplication.',
 'TryScheduleMandatoryCourtResponse':'WorldDiplomacyCourtResponseApplication.',
 'TryScheduleNormalRound':'WorldDiplomacyRoundApplication.',
 'TrySchedulePolicyTriggeredRound':'WorldDiplomacyPolicyRoundApplication.',
 'ProcessRelayArrivals':'WorldDiplomacyRoundProgressApplication.',
 'AdvanceRelay':'WorldDiplomacyRoundApplication.',
 'CompleteExchange':'WorldDiplomacyRoundApplication.',
 'HandleRoundDocumentProcessed':'WorldDiplomacyRoundProgressApplication.',
 'ScheduleNextResultSettlementTurn':'WorldDiplomacyTurnSchedulingApplication.',
 'BeginOrExtendRoundResultSettlement':'WorldDiplomacyRoundApplication.',
 'ProcessRoundLifecycle':'WorldDiplomacyRoundApplication.',
 'HandleDisabledState':'WorldDiplomacyRoundApplication.',
 'CommitEmbeddedRoundPlan':'WorldDiplomacyRoundApplication.',
 'CommitRoundPlan':'WorldDiplomacyRoundPlanApplication.',
 'CommitRoundCompression':'WorldDiplomacyRoundCompressionApplication.',
 'ReconcileActiveDiplomacyAfterLoad':'WorldDiplomacyRoundApplication.',
 'NormalizeStorage':'WorldDiplomacyStorageNormalizationApplication.',
 'EnqueueAnalysisJob':'WorldDiplomacyJobPreparationApplication.',
 'EnqueueRoundPlanJob':'WorldDiplomacyJobPreparationApplication.',
 'AbandonRejectedGeneration':'WorldDiplomacyGenerationTaskApplication.',
 'CommitFailedJob':'WorldDiplomacyFailureApplication.',
 'CommitGeneratedDocument':'WorldDiplomacyGeneratedCompletionApplication.',
 'ProcessAnalyzedDocument':'WorldDiplomacyDocumentExecutionApplication.',
 'FinalizePublishedDocumentAfterAnalysis':'WorldDiplomacyDocumentPublicationApplication.',
 'TryIncludeResultSettlementTarget':'WorldDiplomacyDocumentExecutionApplication.',
 'RefreshResultSettlementActionSlots':'WorldDiplomacyDocumentExecutionApplication.',
 'StartDocumentPropagation':'WorldDiplomacyPublicationRoutingApplication.',
 'TrySettleRelayOffer':'WorldDiplomacyOfferApplication.',
 'PublishPlayerAuthoredDocumentImmediately':'WorldDiplomacyDocumentPublicationApplication.',
 'RefreshPolicyDiplomacySignals':'WorldDiplomacyPolicyRoundApplication.',
 'NotifyExternalDiplomacyResolved':'WorldDiplomacyDocumentPublicationApplication.',
 'TryApplyInitialNewGamePeace':'WorldDiplomacyInitialPeaceApplication.',
 'ExecuteImmediateIntent':'WorldDiplomacyImmediateActionApplication.',
 'IsNonRootAiRelayNoActionAllowed':'WorldDiplomacyNoActionApplication.',
 'CanUseResultSettlementTarget':'WorldDiplomacyNoActionApplication.',
 'TryResolvePolicyConditionForThreat':'WorldDiplomacyThreatBindingApplication.',
 'RegisterOrAdvanceDiplomaticThreat':'WorldDiplomacyThreatBindingApplication.',
 'ProcessDiplomaticThreatDocument':'WorldDiplomacyThreatBindingApplication.',
 'TryApplyUltimatumComplianceDomesticPenalty':'WorldDiplomacyThreatSettlementApplication.',
 'TryApplyDiplomaticThreatPolicyConditionCancellation':'WorldDiplomacyThreatSettlementApplication.',
 'TryApplyDiplomaticThreatIssuerRelationReward':'WorldDiplomacyThreatSettlementApplication.',
 'ResolveDiplomaticThreatCompliance':'WorldDiplomacyThreatSettlementApplication.',
 'ApplyDiplomaticThreatReputationPenalty':'WorldDiplomacyThreatSettlementApplication.',
 'RetryDiplomaticThreatDomesticPenalties':'WorldDiplomacyThreatSettlementApplication.',
 'RetryDiplomaticThreatComplianceConsequences':'WorldDiplomacyThreatSettlementApplication.',
 'RetryDiplomaticThreatHistoryResults':'WorldDiplomacyThreatSettlementApplication.',
 'ApplyNationalPrestigeDelta':'WorldDiplomacyPrestigeApplication.',
 'SettleInternationalReputationForDocument':'WorldDiplomacyPrestigeApplication.',
 'RecoverUnsettledAiInternationalReputation':'WorldDiplomacyPrestigeApplication.',
 'ReconcileAllNationalPrestigeVassalRelations':'WorldDiplomacyPrestigeApplication.',
 'ReconcileNationalPrestigeVassalRelations':'WorldDiplomacyPrestigeApplication.',
 'ApplyZeroPrestigeBreachRelationPenalty':'WorldDiplomacyPrestigeApplication.',
 'AnchorInternationalReputationNaturalChangeDays':'WorldDiplomacyPrestigeApplication.',
 'ProcessInternationalReputationNaturalChange':'WorldDiplomacyPrestigeApplication.',
 'SyncCanonicalHistorySources':'WorldDiplomacyHistoryCaptureApplication.',
 'CaptureCanonicalHistoryForJob':'WorldDiplomacyHistoryCaptureApplication.',
 'RecordDiplomacyWeeklyMaterial':'WorldDiplomacyHistoryCaptureApplication.',
 'EnsureGenerationJobHasKingdomStrategicProfile':'WorldDiplomacyJobPreparationApplication.',
 'RefreshDiplomaticActionPresentationAndPrompt':'WorldDiplomacyJobPreparationApplication.',
 'RefreshDiplomaticThreatPresentationAndPrompt':'WorldDiplomacyJobPreparationApplication.',
 'CommitAnalysis':'WorldDiplomacyAnalysisApplication.',
 'SuppressInvalidDocumentBeforePropagation':'WorldDiplomacyAnalysisApplication.',
 'ParseAndValidatePeaceTerms':'WorldDiplomacyPeaceAdmissionApplication.',
 'AreOfferedPeaceTermsCurrentlyExecutable':'OfferedPeaceTermsCurrentlyExecutable(',
}

# Application file -> member that must live there (was a Behavior private before).
APP_OWNERS={
 'WorldDiplomacyDocumentExecutionApplication.cs':['ProcessAnalyzedDocument','TryIncludeResultSettlementTarget','RefreshResultSettlementActionSlots','FinalizePublishedDocumentAfterAnalysis'],
 'WorldDiplomacyPublicationRoutingApplication.cs':['Start','ReconcileReachedCourts'],
 'WorldDiplomacyActionSelectionApplication.cs':['GetActionableDiplomaticTargets','GetRoundPlanActionableParticipants','GetResultSettlementActionableTargets','BuildLegalDiplomaticDeclarationIntents'],
 'WorldDiplomacyAnalysisApplication.cs':['PreservePublishedPlayerDocumentAfterRejectedMechanic','CommitAnalysis','SuppressInvalidDocumentBeforePropagation'],
 'WorldDiplomacyPeaceAdmissionApplication.cs':['IsCessionCurrentlyAllowed','ParseAndValidatePeaceTerms','AreOfferedPeaceTermsCurrentlyExecutable'],
 'WorldDiplomacyJobPreparationApplication.cs':['RebuildPendingJob','EnsureGenerationJobHasKingdomStrategicProfile'],
 'WorldDiplomacyNoActionApplication.cs':['IsAllowed','CanUseSettlementTarget'],
 'WorldDiplomacyTickApplication.cs':['Run'],
 'WorldDiplomacyGenerationTaskApplication.cs':['PrepareGenerationJob','AbandonRejectedGeneration'],
}

# Behavior private methods that must be thin lifecycle/compat forwarders only.
FORWARDERS={
 'OnMapEventEnded':'WorldDiplomacyBattleApplication.Record',
 'CaptureNativeDiplomacyDecision':'WorldDiplomacyNativeDecisionApplication.Capture',
 'RemoveQueuedNativeDiplomacyDecisions':'WorldDiplomacyNativeDecisionApplication.Sanitize',
 'OnNewGameCreated':'DiplomacyModuleServices.World.OnLifecycle',
 'OnGameLoaded':'DiplomacyModuleServices.World.OnLifecycle',
 'OnSessionLaunched':'DiplomacyModuleServices.World.OnLifecycle',
 'OnDailyTick':'DiplomacyModuleServices.World.OnDailyTick',
 'OnCampaignTick':'DiplomacyModuleServices.World.OnCampaignTick',
 'CanIssueWarThreat':'WorldDiplomacyWarAdmissionApplication.CanIssueWarThreat',
 'CanDeclareWar':'WorldDiplomacyWarAdmissionApplication.CanDeclareWar',
 'RetryDeferredCanonicalHistoryEntries':'_orchestration.RetryDeferredCanonicalHistoryEntries',
 'NotifyExternalDiplomacyResolvedInternal':'_orchestration.NotifyExternalDiplomacyResolved',
 'BuildLegalDiplomaticActionIntents':'WorldDiplomacyActionSelectionApplication',
 'BuildCessionCandidates':'WorldDiplomacyPeaceAdmissionApplication',
}

# Names that must not exist as methods anywhere in the Behavior file.
ABSENT={'EnqueueMandatoryCourtReplyJob','CloseRound','EnqueueGenerationJob',
 'PreservePublishedPlayerDocumentAfterRejectedMechanic','TryRebuildPendingWorldDiplomacyJob',
 'ProcessAnalyzedMultiActionDocument','IsCessionCurrentlyAllowed',
 'GetActionableDiplomaticTargets','GetRoundPlanActionableParticipants','GetResultSettlementActionableTargets',
 'ReconcileAnalyzedPlayerDeclarationWithReachedCourts','StartDocumentPropagation',
 'TryIncludeResultSettlementTarget','RefreshResultSettlementActionSlots',
 'FinalizePublishedDocumentAfterAnalysis',
 # prompt-section policy lives in WorldDiplomacyPromptComposer; the host keeps leaf facts only
 'AppendDiplomaticThreatDynamicContext','AppendDiplomaticThreatAnalysisContext',
 'AppendDiplomaticAuthorDecisionContext','AppendDiplomaticTargetDecisionContext',
 'AppendRelayResponseSourceContext','AppendRulerCaptivityDecisionContext',
 'AppendOtherKingdomRelationshipContext','BuildCompactRoundPlanCandidateLine',
 'BuildCompactDiplomaticRelationshipLine','BuildWarDecisionContext','BuildRulerCaptivityTargetHint',
 # stale presentation/job-refresh decisions are application/domain owned
 'HasStaleDiplomaticThreatPresentation','HasStaleThreatPresentation','HasStaleActionPresentation',
 # legacy propagation coverage is an application/domain decision over snapshots
 'HasCompleteLegacyPropagationCoverage',
 # bilateral state precedence label is a domain rule over leaf facts
 'BuildBilateralState',
 # AI-party selection is a domain rule over host leaf predicates
 'GetEligibleAiKingdoms','EligibleAiPartyIds'}

# Predicates that must not reappear in any World/ adapter file.
WORLD_FORBIDDEN={'SelectPresentedThreatStageDocumentIds','SelectNoncompliedThreatStageDocumentIds',
 'HasThreatPresentationDrift','HasStaleThreatPresentation','HasStaleActionPresentation',
 'HasStaleDiplomaticActionPresentation','HasCompleteLegacyPropagationCoverage',
 'BuildBilateralState','EligibleAiPartyIds','EnsureRoundParticipant','IsPlayerAsync'}

# Whitelisted WorldDiplomacy*Application references inside World/ adapter files.
BEHAVIOR_APP_WHITELIST={
 'WorldDiplomacyBehavior.cs':{'WorldDiplomacyNativeDecisionApplication','WorldDiplomacyBattleApplication','WorldDiplomacyTickApplication','WorldDiplomacyWarAdmissionApplication',
  'WorldDiplomacyDocumentApplication','WorldDiplomacyPeaceAdmissionApplication','WorldDiplomacyActionSelectionApplication',
  'WorldDiplomacyNotificationApplication','WorldDiplomacyLlmMessageApplication'},
 'WorldDiplomacyBehavior.JobRuntime.cs':{'WorldDiplomacyLlmDispatchApplication','WorldDiplomacyCompletionApplication'},
 'WorldDiplomacyBehavior.LlmDispatchSource.cs':{'WorldDiplomacyLlmApplication','WorldDiplomacyLlmMessageApplication'},
 'WorldDiplomacyBehavior.Presentation.cs':{'WorldDiplomacyPlayerApplication'},
 'WorldDiplomacyBehavior.OrchestrationHost.cs':{'WorldDiplomacyGeographyApplication','WorldDiplomacyPolicyRoundApplication','WorldDiplomacyPropagationApplication',
  'WorldDiplomacyDocumentExecutionApplication','WorldDiplomacyPublicationRoutingApplication',
  'WorldDiplomacyTurnSchedulingApplication','WorldDiplomacyRoundProgressApplication',
  'WorldDiplomacyRoundApplication','WorldDiplomacyActionSelectionApplication'},
 'WorldDiplomacyBehavior.PrestigePort.cs':{'WorldDiplomacyPrestigeApplication'},
 'WorldDiplomacyBehavior.PublicationPort.cs':{'WorldDiplomacyGeographyApplication'},
}

def main():
 p=argparse.ArgumentParser();p.add_argument('--dotnet',default='dotnet');p.add_argument('--baseline-dll');p.add_argument('--candidate-dll');p.add_argument('--run-root',type=Path);p.add_argument('--prepare-only',action='store_true');a=p.parse_args()
 if a.prepare_only and a.run_root is None:p.error('--prepare-only requires a fresh --run-root')
 retired=load('retired','tools/DiplomacyArchitectureTests/retired.py')
 prior_raw=old(retired.HOST)
 current=read(retired.HOST)
 orch=read(ORCH)

 # 1. Orchestration members absent from the Behavior private surface and
 #    forwarding to the expected Application owner.
 for name,owner in ORCH_OWNERS.items():
  assert method_of(current,name,r'private') is None,'predecessor still owns private '+name
  body=method_of(orch,name)
  assert body is not None,'missing orchestration member '+name
  assert owner in body,name+' does not call its Application owner '+owner

 # 2. Application-owned internals absent from the Behavior private surface.
 for fname,members in APP_OWNERS.items():
  text=read(APPDIR+fname)
  for name in members:
   assert method_of(current,name,r'private') is None,'predecessor still owns private '+name
   assert method_of(text,name) is not None,fname+' lost member '+name

 # 3. Thin forwarders: private Behavior methods whose entire body forwards.
 for name,call in FORWARDERS.items():
  body=method_of(current,name,r'private')
  assert body is not None,'missing thin forwarder '+name
  assert call in body,name+' no longer forwards to '+call
  assert len([s for s in re.findall(r';',body)])<=2,name+' regrew orchestration'

 # 4. Absent-everywhere names in the Behavior file.
 for name in ABSENT:
  assert any_method_of(current,name) is None,'predecessor method still in host: '+name

 # 4b. Presentation/job staleness and round-state writes must not hide in any
 #     World adapter partial (callback ports, prompt worlds, event adapters).
 for path in sorted((ROOT/'src/modules/AF.Module.Diplomacy/World').glob('*.cs')):
  text=path.read_text(encoding='utf-8-sig')
  for bad in WORLD_FORBIDDEN:
   assert bad not in text,'forbidden predicate '+bad+' reappeared in '+path.name

 # External static entries may remain but only forward (no second algorithm).
 for entry in re.finditer(r'(?m)^\s*public static void (NotifyExternalDiplomacyResolved)\s*\([^)]*\)\s*\{',current):
  body=method_of(current,entry.group(1))
  assert body is not None and '_orchestration.' in body or 'NotifyExternalDiplomacyResolvedInternal' in body

 # 5. Orchestration must not reference the concrete Behavior type (host interface only).
 assert 'WorldDiplomacyBehavior' not in orch,'orchestration leaked the concrete host type'

 # 6. Behavior adapter files reference *Application types only via the whitelist.
 for path in sorted((ROOT/'src/modules/AF.Module.Diplomacy/World').glob('*.cs')):
  rel='src/modules/AF.Module.Diplomacy/World/'+path.name
  text=path.read_text(encoding='utf-8-sig')
  allowed=BEHAVIOR_APP_WHITELIST.get(path.name,set())
  for m in re.finditer(r'WorldDiplomacy\w+Application',text):
   app=m.group(0)
   assert app in allowed,rel+' references '+app+' outside the adapter whitelist'

 # R1/R3/R5/R8 closure: hooks route through the bridge; no old prompt, memory or execution algorithm.
 direct=read('src/modules/AF.Module.Diplomacy/Direct/DiplomacyBehavior.cs')
 for name in ['BuildDiplomacyInstructionContext','BuildDiplomacyRuntimeInstruction','AppendWarStatsBlock','BuildPlayerIndependentSettlementClanContext']:
  assert any_method_of(direct,name) is None,'oral prompt policy retained: '+name
 for name in ['ShouldInjectDiplomacyMemoryForInput','BuildDiplomacyMemoryContext']:
  assert any_method_of(current,name) is None,'memory policy retained: '+name
 assert 'DiplomacyConversationBridge.BuildDiplomacyPrompt' in direct
 assert 'DiplomacyConversationBridge.BuildDiplomacyMemory' in current
 executor=read(APPDIR+'WorldDiplomacyDocumentExecutionApplication.cs')
 assert 'ProcessAnalyzedMultiActionDocument(' not in executor
 assert executor.index('actions.Count > port.MaxDiplomaticActionsPerDocument) return;') < executor.index('new WorldDiplomacyDocumentExecutionCommand(document, actions)')
 assert 'new WorldDiplomacyDocumentExecutionCommand(document, actions)' in executor
 assert 'port.ResolveKingdomId(input.TargetKingdomId)' in executor
 assert 'WorldDiplomacyIntentVocabulary.NormalizeIntent(input.Intent)' in executor
 assert 'WorldDiplomacyOfferContractRules.CommitmentMatchesIntent(intent, input.Commitment)' in executor
 assert 'new WorldDiplomacyDocumentActionReceipt(input.ActionId, target,' in executor
 assert 'WorldDiplomacyDocumentApplication.CaptureActionResult(document, resultAction, receipt)' in executor
 assert 'WorldDiplomacyDocumentExecutionCommand(document, actions)' in executor
 assert 'document.Actions = frozenActions' in executor
 assert 'validationDocument, requiredPeaceOffer' in executor
 assert 'DocumentHasUnsafeMultiplePeaceAcceptances(validationDocument' in executor
 assert 'BeginAction(document, input.Materialize(), target)' in executor
 assert 'outcomeKnown &= orchestration.ExecuteImmediateIntent(' in executor
 assert 'outcomeKnown &= orchestration.TrySettleRelayOffer(document) != WorldDiplomacyOfferOutcome.Unknown' in executor
 assert 'WorldDiplomacyDocumentApplication.SealActions(document, allAddressed, sourceContextDocumentId, receipts)' in executor
 assert 'document.ChangedDiplomaticState = receipts.Any(x => x.Applied)' in read(APPDIR+'WorldDiplomacyDocumentApplication.cs')
 assert 'outcomeKnown = false;' in executor
 assert executor.count('orchestration.ExecuteImmediateIntent(')==1
 history=read('Refactor/Domain/WorldDiplomacyCanonicalHistoryRules.cs')
 for forbidden in ['TryScheduleTokenCompression(', 'CommitCompression(', 'EnqueueCompressionJob(', 'AppendCanonicalDocumentEvents(', 'SyncPublishedPolicyArtifacts(']:
  assert forbidden not in history,'Domain still owns workflow: '+forbidden

 # Domain providers must not invoke migrated effect/recovery workflows.
 lifecycle=read('Refactor/Domain/WorldDiplomacyRoundLifecycleRules.cs')
 for name in ['AddOrMergeResultSettlementSlot','AddWarResponseResultSettlementSlot','InitializeResultSettlementRouteSlots','UpsertOfferCooldown','SettleTradeAllianceOfferCooldownsForClosedRound','HasStaleDiplomaticActionPresentation']:
  assert any_method_of(lifecycle,name) is None,'Domain retains workflow '+name
 reputation=read('Refactor/Domain/WorldDiplomacyReputationRules.cs')
 assert 'reconcileVassalRelations' not in reputation
 assert any_method_of(reputation,'RecoverUnsettledAiInternationalReputation') is None
 prompt=read('Refactor/Domain/WorldDiplomacyPromptContractRules.cs')
 assert 'buildCanonicalHistoryBlock' not in prompt

 # 7. Baseline retained text required by Program.cs: raw baseline host snapshot.
 out=new_run_root(ROOT,'DiplomacyArchitectureTests',a.run_root) if a.run_root is not None else HERE/'.generated'
 if a.run_root is None:out.mkdir(exist_ok=True)
 (out/'prior-host.cs.txt').write_text(prior_raw,encoding='utf-8')
 # Evaluation only: no game startup, restore, Stage or deployment.
 result=subprocess.run([a.dotnet,'msbuild',str(ROOT/'AnimusForge.csproj'),'-getItem:Compile'],cwd=ROOT,capture_output=True,encoding='utf-8',check=True)
 items=json.loads(result.stdout)['Items']['Compile']
 paths=[Path(item['FullPath']) for item in items]
 payload={'root':str(ROOT),'paths':[str(path) for path in paths], 'retired':[s[s.rfind(' ')+1:].rstrip('(') for s in retired.RETIRED]}
 payload.update(baseline=a.baseline_dll or '',candidate=a.candidate_dll or '')
 manifest=out/'sources.json';manifest.write_text(json.dumps(payload),encoding='utf-8')
 if a.prepare_only:
  print(manifest)
  return
 if a.run_root is None:
  code=subprocess.call([a.dotnet,'run','--project',str(HERE/'DiplomacyArchitectureTests.csproj'),'-c','Release','--',str(manifest)],cwd=ROOT)
 else:
  # Reuse the same isolated SDK build/target evaluation as the original discovery runner.
  runner=load('isolated_discovery_runner','tests/run_all.py')
  build_root=out/'build'
  command=[a.dotnet,'run','--project',str(HERE/'DiplomacyArchitectureTests.csproj'),'-c','Release','-p:UseArtifactsOutput=true','-p:UseAppHost=false',f'-p:ArtifactsPath={build_root}',f'-p:OutDir={build_root / "bin" / "runtime"}/','--',str(manifest)]
  result=runner.execute(command,minimal_test_environment(Path(a.dotnet),out),build_root,300)
  (out/'execution.log').write_text(result.stdout+result.stderr,encoding='utf-8')
  print(result.stdout+result.stderr)
  code=result.returncode
 raise SystemExit(code)
if __name__=='__main__':main()

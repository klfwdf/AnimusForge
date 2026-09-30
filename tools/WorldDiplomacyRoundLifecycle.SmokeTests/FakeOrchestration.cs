using System;
using System.Collections.Generic;
using AnimusForge;
using Newtonsoft.Json.Linq;

// Shared orchestration double for Application replays. Members are virtual and
// return type defaults; a replay subclasses and overrides only the members its
// lane invokes. Calls records every consumed member for boundary assertions.
internal class FakeOrchestration : IWorldDiplomacyOrchestration
{
    internal static readonly FakeOrchestration Instance = new FakeOrchestration();
    internal readonly List<string> Calls = new();
    internal Func<WorldDiplomacyJob, bool> OnRefreshThreatPresentation, OnRefreshActionPresentation;
    internal Action<WorldDiplomacyJob, string, string, string, string, JObject> OnRejectGeneratedDraftBeforePublication;
    internal Action<WorldDiplomacyJob, string> OnCommitGeneratedDocument, OnCommitAnalysis, OnCommitCompression, OnCommitRoundPlan, OnCommitRoundCompression, OnCommitFailedJob;
    public virtual WorldDiplomacyRound ResolveRound(string roundId)
    {
        Calls.Add("ResolveRound");
        return null;
    }
    public virtual WorldDiplomacyDocument ResolveDocument(string documentId)
    {
        Calls.Add("ResolveDocument");
        return null;
    }
    public virtual void EnqueueJob(WorldDiplomacyJob job)
    {
        Calls.Add("EnqueueJob");
    }
    public virtual void EnqueueGeneration(string authorId, string targetId, WorldDiplomacyExchange exchange, bool isResponse, WorldDiplomacyDocument sourceDocument, int priority, bool externalResponseOnly, bool isReminder, string roundId, bool isRelayTurn, bool allowUntargeted, string previousKingdomId, int scheduledDay, string resultSettlementSlotId)
    {
        Calls.Add("EnqueueGeneration");
    }
    public virtual string EnqueueMandatoryReplyJob(string receiverId, string targetId, WorldDiplomacyDocument source, string roundId, bool isRelayTurn)
    {
        Calls.Add("EnqueueMandatoryReplyJob");
        return "";
    }
    public virtual void EnqueueAnalysisJob(WorldDiplomacyDocument document, int priority)
    {
        Calls.Add("EnqueueAnalysisJob");
    }
    public virtual void EnqueueRoundPlanJob(WorldDiplomacyRound round, WorldDiplomacyDocument root)
    {
        Calls.Add("EnqueueRoundPlanJob");
    }
    public virtual void EnqueueCompressionJob(long throughSequence, long tokenCount, int targetTokens)
    {
        Calls.Add("EnqueueCompressionJob");
    }
    public virtual void AbandonRejectedGeneration(WorldDiplomacyJob job, string authorId, string targetId, string reason)
    {
        Calls.Add("AbandonRejectedGeneration");
    }
    public virtual void RejectGeneratedDraftBeforePublication(WorldDiplomacyJob job, string rejectedRaw, string authorId, string targetId, string reason, Newtonsoft.Json.Linq.JObject parsedJson)
    {
        if (OnRejectGeneratedDraftBeforePublication != null) { OnRejectGeneratedDraftBeforePublication(job, rejectedRaw, authorId, targetId, reason, parsedJson); return; }
        Calls.Add("RejectGeneratedDraftBeforePublication");
    }
    public virtual void CommitAnalysis(WorldDiplomacyJob job, string raw)
    {
        if (OnCommitAnalysis != null) { OnCommitAnalysis(job, raw); return; }
        Calls.Add("CommitAnalysis");
    }
    public virtual void ProcessAnalyzedDocument(WorldDiplomacyDocument document, string intent, string commitment, bool requiresResponse, string tone, float confidence)
    {
        Calls.Add("ProcessAnalyzedDocument");
    }
    public virtual void CommitFailedJob(WorldDiplomacyJob job, string error)
    {
        if (OnCommitFailedJob != null) { OnCommitFailedJob(job, error); return; }
        Calls.Add("CommitFailedJob");
    }
    public virtual void CommitGeneratedDocument(WorldDiplomacyJob job, string raw)
    {
        if (OnCommitGeneratedDocument != null) { OnCommitGeneratedDocument(job, raw); return; }
        Calls.Add("CommitGeneratedDocument");
    }
    public virtual bool TryGetGeneratedIntentLegalityViolation(WorldDiplomacyJob job, Newtonsoft.Json.Linq.JObject json, string authorId, string fallbackTargetId, out string generatedTargetId, out string reason)
    {
        Calls.Add("TryGetGeneratedIntentLegalityViolation");
        generatedTargetId = "";
        reason = "";
        return false;
    }
    public virtual bool TryGetGeneratedSingleActionLegalityViolation(WorldDiplomacyJob job, Newtonsoft.Json.Linq.JObject json, string authorId, string fallbackTargetId, out string generatedTargetId, out string reason)
    {
        Calls.Add("TryGetGeneratedSingleActionLegalityViolation");
        generatedTargetId = "";
        reason = "";
        return false;
    }
    public virtual bool TryApplyGeneratedSemanticEnvelope(WorldDiplomacyDocument document, Newtonsoft.Json.Linq.JObject json, string authorId, string fallbackTargetId, bool allowUntargeted, bool relayTurn)
    {
        Calls.Add("TryApplyGeneratedSemanticEnvelope");
        return false;
    }
    public virtual void SuppressInvalidDocumentBeforePropagation(WorldDiplomacyDocument document, string reason)
    {
        Calls.Add("SuppressInvalidDocumentBeforePropagation");
    }
    public virtual bool TryRebuildPendingJob(WorldDiplomacyJob job)
    {
        Calls.Add("TryRebuildPendingJob");
        return false;
    }
    public virtual bool EnsureGenerationJobHasKingdomStrategicProfile(WorldDiplomacyJob job)
    {
        Calls.Add("EnsureGenerationJobHasKingdomStrategicProfile");
        return false;
    }
    public virtual bool RefreshDiplomaticActionPresentationAndPrompt(WorldDiplomacyJob job)
    {
        if (OnRefreshActionPresentation != null) return OnRefreshActionPresentation(job);
        Calls.Add("RefreshDiplomaticActionPresentationAndPrompt");
        return false;
    }
    public virtual bool RefreshDiplomaticThreatPresentationAndPrompt(WorldDiplomacyJob job)
    {
        if (OnRefreshThreatPresentation != null) return OnRefreshThreatPresentation(job);
        Calls.Add("RefreshDiplomaticThreatPresentationAndPrompt");
        return false;
    }
    public virtual void ProcessRelayArrivals()
    {
        Calls.Add("ProcessRelayArrivals");
    }
    public virtual void ProcessRoundLifecycle()
    {
        Calls.Add("ProcessRoundLifecycle");
    }
    public virtual void ProcessCompletedJobs()
    {
        Calls.Add("ProcessCompletedJobs");
    }
    public virtual void TryStartNextLlmJob()
    {
        Calls.Add("TryStartNextLlmJob");
    }
    public virtual void PollNotifications()
    {
        Calls.Add("PollNotifications");
    }
    public virtual void ProcessCourtArrival(string receiverId, WorldDiplomacyDocument document)
    {
        Calls.Add("ProcessCourtArrival");
    }
    public virtual void TryScheduleMandatoryCourtResponse(WorldDiplomacyRound round, WorldDiplomacyRoundParticipant participant, string receiverId, WorldDiplomacyDocument trigger)
    {
        Calls.Add("TryScheduleMandatoryCourtResponse");
    }
    public virtual void MarkPlayerCourtReachedByRelay(string receiverId, WorldDiplomacyDocument document)
    {
        Calls.Add("MarkPlayerCourtReachedByRelay");
    }
    public virtual void ReconcileReachedCourts(WorldDiplomacyDocument document)
    {
        Calls.Add("ReconcileReachedCourts");
    }
    public virtual WorldDiplomacyRound EnsureActiveRound(string initiatorId, string targetId, bool isPlayerInsertion)
    {
        Calls.Add("EnsureActiveRound");
        return null;
    }
    public virtual void CloseActiveRound(string reason)
    {
        Calls.Add("CloseActiveRound");
    }
    public virtual void AdvanceRelay(WorldDiplomacyRound round, bool scheduleImmediately)
    {
        Calls.Add("AdvanceRelay");
    }
    public virtual void CompleteExchange(string exchangeId, string reason)
    {
        Calls.Add("CompleteExchange");
    }
    public virtual void ScheduleNextResultSettlementTurn(WorldDiplomacyRound round)
    {
        Calls.Add("ScheduleNextResultSettlementTurn");
    }
    public virtual void ScheduleNextRelayHop(WorldDiplomacyRound round, bool scheduleImmediately)
    {
        Calls.Add("ScheduleNextRelayHop");
    }
    public virtual void BeginOrExtendRoundResultSettlement(WorldDiplomacyRound round, WorldDiplomacyDocument document, string closeReason, string roundStatus)
    {
        Calls.Add("BeginOrExtendRoundResultSettlement");
    }
    public virtual void HandleRoundDocumentProcessed(WorldDiplomacyDocument document)
    {
        Calls.Add("HandleRoundDocumentProcessed");
    }
    public virtual IReadOnlyList<WorldDiplomacyThreat> Threats()
    {
        Calls.Add("Threats");
        return (IReadOnlyList<WorldDiplomacyThreat>)new List<WorldDiplomacyThreat>();
    }
    public virtual void IntegratePlayerDeclaration(WorldDiplomacyRound round, WorldDiplomacyDocument document)
    {
        Calls.Add("IntegratePlayerDeclaration");
    }
    public virtual void CommitEmbeddedRoundPlan(WorldDiplomacyRound round, WorldDiplomacyDocument root)
    {
        Calls.Add("CommitEmbeddedRoundPlan");
    }
    public virtual void CommitRoundPlan(WorldDiplomacyJob job, string raw)
    {
        if (OnCommitRoundPlan != null) { OnCommitRoundPlan(job, raw); return; }
        Calls.Add("CommitRoundPlan");
    }
    public virtual void CommitRoundCompression(WorldDiplomacyJob job, string raw)
    {
        if (OnCommitRoundCompression != null) { OnCommitRoundCompression(job, raw); return; }
        Calls.Add("CommitRoundCompression");
    }
    public virtual void CommitLocalRoundSummary(WorldDiplomacyRound round, List<WorldDiplomacyDocument> documents)
    {
        Calls.Add("CommitLocalRoundSummary");
    }
    public virtual void UpgradeRoundSummaryToStructuredArchive(WorldDiplomacyRoundSummary summary)
    {
        Calls.Add("UpgradeRoundSummaryToStructuredArchive");
    }
    public virtual void RefreshResultSettlementActionSlots(WorldDiplomacyRound round)
    {
        Calls.Add("RefreshResultSettlementActionSlots");
    }
    public virtual bool TryIncludeResultSettlementTarget(WorldDiplomacyRound round, string kingdomId)
    {
        Calls.Add("TryIncludeResultSettlementTarget");
        return false;
    }
    public virtual bool CanUseResultSettlementTarget(WorldDiplomacyRound round, string authorId, string targetId)
    {
        Calls.Add("CanUseResultSettlementTarget");
        return false;
    }
    public virtual void ReconcileActiveDiplomacyAfterLoad()
    {
        Calls.Add("ReconcileActiveDiplomacyAfterLoad");
    }
    public virtual void HandleDisabledState()
    {
        Calls.Add("HandleDisabledState");
    }
    public virtual void RestoreSuspendedExchangeIfAny()
    {
        Calls.Add("RestoreSuspendedExchangeIfAny");
    }
    public virtual void TryScheduleNormalRound()
    {
        Calls.Add("TryScheduleNormalRound");
    }
    public virtual void TrySchedulePolicyTriggeredRound()
    {
        Calls.Add("TrySchedulePolicyTriggeredRound");
    }
    public virtual void RefreshPolicyDiplomacySignals()
    {
        Calls.Add("RefreshPolicyDiplomacySignals");
    }
    public virtual void RetryDeferredRoundProgress()
    {
        Calls.Add("RetryDeferredRoundProgress");
    }
    public virtual void ScheduleNextNormalRoundAfter(int baseDay)
    {
        Calls.Add("ScheduleNextNormalRoundAfter");
    }
    public virtual void RefreshRoundIntervalScheduleIfNeeded()
    {
        Calls.Add("RefreshRoundIntervalScheduleIfNeeded");
    }
    public virtual void CompletePolicySignal(WorldDiplomacyPolicySignal signal, string reason)
    {
        Calls.Add("CompletePolicySignal");
    }
    public virtual void TryApplyInitialNewGamePeace()
    {
        Calls.Add("TryApplyInitialNewGamePeace");
    }
    public virtual void SyncData(bool isSaving, bool isLoading,
        Func<WorldDiplomacyStorage> loadStorage, Func<string> loadError,
        Action<WorldDiplomacyStorage> saveStorage, Action<string> log, Action resetTransientRuntime)
    {
        Calls.Add("SyncData");
    }
    public virtual void NormalizeStorage(bool allowWorldValidation)
    {
        Calls.Add("NormalizeStorage");
    }
    public virtual void ReplaceStorage(WorldDiplomacyStorage storage)
    {
        Calls.Add("ReplaceStorage");
    }
    public virtual void ResetStorageForNewGame(bool initialPeacePending)
    {
        Calls.Add("ResetStorageForNewGame");
    }
    public virtual void EnsureScheduleInitialized()
    {
        Calls.Add("EnsureScheduleInitialized");
    }
    public virtual void ResetRuntimeState()
    {
        Calls.Add("ResetRuntimeState");
    }
    public virtual void HandleWarDeclared(string firstId, string secondId)
    {
        Calls.Add("HandleWarDeclared");
    }
    public virtual void HandlePeaceMade(string firstId, string secondId)
    {
        Calls.Add("HandlePeaceMade");
    }
    public virtual void HandleSettlementOwnerChanged(string settlementId, string settlementName, string oldKingdomId, string newKingdomId)
    {
        Calls.Add("HandleSettlementOwnerChanged");
    }
    public virtual void RecordBattleFact(WorldDiplomacyBattleFact fact)
    {
        Calls.Add("RecordBattleFact");
    }
    public virtual bool RecordNativeSignal(string sourceId, string targetId, string action, string reason)
    {
        Calls.Add("RecordNativeSignal");
        return false;
    }
    public virtual void EnsureActiveWarLedgers()
    {
        Calls.Add("EnsureActiveWarLedgers");
    }
    public virtual void TrimRecentBattleFacts()
    {
        Calls.Add("TrimRecentBattleFacts");
    }
    public virtual void TrimNativeSignals()
    {
        Calls.Add("TrimNativeSignals");
    }
    public virtual void DecayWarPressure()
    {
        Calls.Add("DecayWarPressure");
    }
    public virtual void RemoveJob(string jobId)
    {
        Calls.Add("RemoveJob");
    }
    public virtual void AddWarPressure(string sourceId, string targetId, int delta, string reason, string intent)
    {
        Calls.Add("AddWarPressure");
    }
    public virtual WarPressureEntry FindWarPressure(string sourceId, string targetId)
    {
        Calls.Add("FindWarPressure");
        return null;
    }
    public virtual void ClearLlmCacheAffinityKey()
    {
        Calls.Add("ClearLlmCacheAffinityKey");
    }
    public virtual void StartDocumentPropagation(WorldDiplomacyDocument document, string authorId)
    {
        Calls.Add("StartDocumentPropagation");
    }
    public virtual void RetryDeferredDocumentPropagation()
    {
        Calls.Add("RetryDeferredDocumentPropagation");
    }
    public virtual void ProcessPropagationArrivals()
    {
        Calls.Add("ProcessPropagationArrivals");
    }
    public virtual void RecalculatePendingPropagationIfNeeded()
    {
        Calls.Add("RecalculatePendingPropagationIfNeeded");
    }
    public virtual void RecordDiplomacyWeeklyMaterial(WorldDiplomacyDocument document)
    {
        Calls.Add("RecordDiplomacyWeeklyMaterial");
    }
    public virtual void PublishPlayerAuthoredDocumentImmediately(WorldDiplomacyDocument document)
    {
        Calls.Add("PublishPlayerAuthoredDocumentImmediately");
    }
    public virtual void NotifyExternalDiplomacyResolved(string action, string initiatorId, string targetId, string reason)
    {
        Calls.Add("NotifyExternalDiplomacyResolved");
    }
    public virtual void RecoverPlayerCourtReceiptsFromKnowledge()
    {
        Calls.Add("RecoverPlayerCourtReceiptsFromKnowledge");
    }
    public virtual List<string> NormalizeKingdomIdList(IEnumerable<string> values, string excludedId)
    {
        Calls.Add("NormalizeKingdomIdList");
        return new List<string>();
    }
    public virtual bool CanExternalDiplomacyFactJoinRound(WorldDiplomacyRound round, string initiatorId, string targetId)
    {
        Calls.Add("CanExternalDiplomacyFactJoinRound");
        return false;
    }
    public virtual void FinalizePublishedDocumentAfterAnalysis(WorldDiplomacyDocument document, string authorId, string targetId, string normalizedIntent, bool recordNoActionDecision)
    {
        Calls.Add("FinalizePublishedDocumentAfterAnalysis");
    }
    public virtual void PruneInvalidOffers(WorldDiplomacyRound round)
    {
        Calls.Add("PruneInvalidOffers");
    }
    public virtual void NormalizeOfferCooldownStorage()
    {
        Calls.Add("NormalizeOfferCooldownStorage");
    }
    public virtual void SettleTradeAllianceOfferCooldownsForClosedRound(WorldDiplomacyRound round)
    {
        Calls.Add("SettleTradeAllianceOfferCooldownsForClosedRound");
    }
    public virtual void ClearBilateralOfferCooldowns(string firstId, string secondId, WorldDiplomacyOfferDomain domain)
    {
        Calls.Add("ClearBilateralOfferCooldowns");
    }
    public virtual List<string> BuildLegalDiplomaticActionIntents(WorldDiplomacyRound round, string authorId, string targetId)
    {
        Calls.Add("BuildLegalDiplomaticActionIntents");
        return new List<string>();
    }
    public virtual List<string> BuildPotentialDiplomaticActionIntents(string firstId, string secondId)
    {
        Calls.Add("BuildPotentialDiplomaticActionIntents");
        return new List<string>();
    }
    public virtual bool AreOfferedPeaceTermsCurrentlyExecutable(WorldDiplomacyRoundOffer offer, WorldDiplomacyDocument source)
    {
        Calls.Add("AreOfferedPeaceTermsCurrentlyExecutable");
        return false;
    }
    public virtual List<string> BuildLegalDiplomaticDeclarationIntents(WorldDiplomacyRound round, string authorId, string targetId, bool isRelayTurn, string resultSettlementSlotId, bool isExternalResponseOnly, WorldDiplomacyDocument responseSource)
    {
        Calls.Add("BuildLegalDiplomaticDeclarationIntents");
        return new List<string>();
    }
    public virtual Dictionary<string, List<string>> BuildLegalDiplomaticDeclarationIntentMap(WorldDiplomacyRound round, string authorId, List<string> ids, bool isRelayTurn, string resultSettlementSlotId, bool isExternalResponseOnly, WorldDiplomacyDocument responseSource)
    {
        Calls.Add("BuildLegalDiplomaticDeclarationIntentMap");
        return new Dictionary<string, List<string>>();
    }
    public virtual List<string> GetResultSettlementActionableTargetIds(WorldDiplomacyRound round, string authorId)
    {
        Calls.Add("GetResultSettlementActionableTargetIds");
        return new List<string>();
    }
    public virtual List<string> GetActionableDiplomaticTargetIds(string authorId, WorldDiplomacyRound round)
    {
        Calls.Add("GetActionableDiplomaticTargetIds");
        return new List<string>();
    }
    public virtual List<string> GetRoundPlanActionableParticipantIds(string authorId, WorldDiplomacyRound round)
    {
        Calls.Add("GetRoundPlanActionableParticipantIds");
        return new List<string>();
    }
    public virtual List<string> GetAuthorizedGenerationTargetIds(WorldDiplomacyJob source, WorldDiplomacyRound round, string authorId)
    {
        Calls.Add("GetAuthorizedGenerationTargetIds");
        return new List<string>();
    }
    public virtual bool HasAnyLegalDiplomaticActionIntent(WorldDiplomacyRound round, string authorId, string targetId)
    {
        Calls.Add("HasAnyLegalDiplomaticActionIntent");
        return false;
    }
    public virtual bool HasCessionBoundMultiplePeaceAcceptanceOptions(WorldDiplomacyRound round, string authorId, IReadOnlyDictionary<string, List<string>> legalActionsByTarget)
    {
        Calls.Add("HasCessionBoundMultiplePeaceAcceptanceOptions");
        return false;
    }
    public virtual string BuildCurrentLegalDiplomaticOptions(WorldDiplomacyRound round, string authorId, IEnumerable<string> targetIds, bool isRelayTurn, string resultSettlementSlotId, bool isExternalResponseOnly, WorldDiplomacyDocument responseSource)
    {
        Calls.Add("BuildCurrentLegalDiplomaticOptions");
        return "";
    }
    public virtual bool IsNonRootAiRelayNoActionAllowed(WorldDiplomacyRound round, string resultSettlementSlotId, string authorId, string targetId, bool isRelayTurn, bool isExternalResponseOnly, WorldDiplomacyDocument responseSource)
    {
        Calls.Add("IsNonRootAiRelayNoActionAllowed");
        return false;
    }
    public virtual bool OfferedPeaceTermsCurrentlyExecutable(WorldDiplomacyRoundOffer offer, WorldDiplomacyDocument source, string proposerId, string targetId)
    {
        Calls.Add("OfferedPeaceTermsCurrentlyExecutable");
        return false;
    }
    internal Func<WorldDiplomacyJob, string> OnBuildGenerationLegalActionSignature;
    public virtual string BuildGenerationLegalActionSignature(WorldDiplomacyJob job)
    {
        if (OnBuildGenerationLegalActionSignature != null) return OnBuildGenerationLegalActionSignature(job);
        Calls.Add("BuildGenerationLegalActionSignature");
        return "";
    }
    public virtual string BuildRelayTurnGenerationPrompt(WorldDiplomacyRound round, string authorId, string targetId, WorldDiplomacyDocument prioritySource, bool priorityResponseOnly)
    {
        Calls.Add("BuildRelayTurnGenerationPrompt");
        return "";
    }
    public virtual string BuildGenerationPromptForJob(string authorId, string targetId, WorldDiplomacyExchange exchange, bool isResponse, WorldDiplomacyDocument source, bool isReminder, string roundId, bool allowUntargeted, List<string> planCandidates, bool externalResponseOnly)
    {
        Calls.Add("BuildGenerationPromptForJob");
        return "";
    }
    public virtual string BuildRoundPlanSystemPrompt(WorldDiplomacyRound round)
    {
        Calls.Add("BuildRoundPlanSystemPrompt");
        return "";
    }
    public virtual string BuildRoundPlanPrompt(WorldDiplomacyDocument root, List<string> candidateIds)
    {
        Calls.Add("BuildRoundPlanPrompt");
        return "";
    }
    public virtual string BuildAnalysisPrompt(WorldDiplomacyDocument document)
    {
        Calls.Add("BuildAnalysisPrompt");
        return "";
    }
    public virtual string BuildFallbackAnalysisJson(WorldDiplomacyJob job)
    {
        Calls.Add("BuildFallbackAnalysisJson");
        return "";
    }
    public virtual string BuildRelayConversationTurnPrompt(WorldDiplomacyRound round, string authorId, string previousId, WorldDiplomacyDocument prioritySource, bool priorityResponseOnly)
    {
        Calls.Add("BuildRelayConversationTurnPrompt");
        return "";
    }
    public virtual WorldDiplomacyDocument CreateDocument(string authorId, string targetId, string title, string body, string origin, bool isPlayerAuthored, bool isResponse, string exchangeId)
    {
        Calls.Add("CreateDocument");
        return null;
    }
    public virtual void AddDocument(WorldDiplomacyDocument document)
    {
        Calls.Add("AddDocument");
    }
    public virtual void EnsureCanonicalHistoryInitialized()
    {
        Calls.Add("EnsureCanonicalHistoryInitialized");
    }
    public virtual void InvalidateCanonicalHistoryRenderCache()
    {
        Calls.Add("InvalidateCanonicalHistoryRenderCache");
    }
    public virtual void SyncCanonicalHistorySources(bool force)
    {
        Calls.Add("SyncCanonicalHistorySources");
    }
    public virtual void CaptureCanonicalHistoryForJob(WorldDiplomacyJob job, bool syncSources, long throughSequence)
    {
        Calls.Add("CaptureCanonicalHistoryForJob");
    }
    public virtual void AppendCanonicalDocumentEvents(WorldDiplomacyDocument document)
    {
        Calls.Add("AppendCanonicalDocumentEvents");
    }
    public virtual bool AppendCanonicalHistoryEntry(string kind, string sourceKey, string sourceId, int day, string gameDate, string authorKingdomId, IEnumerable<string> targetKingdomIds, string intent, string commitment, string content, bool verified, string respondingToOfferDocumentId, string respondingToThreatDocumentId, IEnumerable<string> actionFacts)
    {
        Calls.Add("AppendCanonicalHistoryEntry");
        return false;
    }
    public virtual bool AppendCanonicalHistoryWeeklyArtifact(WorldDiplomacyWeeklyArtifact artifact)
    {
        Calls.Add("AppendCanonicalHistoryWeeklyArtifact");
        return false;
    }
    public virtual void SyncPublishedPolicyArtifacts(int maxBatches)
    {
        Calls.Add("SyncPublishedPolicyArtifacts");
    }
    public virtual void RebuildPublishedPolicySignaturesThrough(long throughSequence)
    {
        Calls.Add("RebuildPublishedPolicySignaturesThrough");
    }
    public virtual bool AppendPublishedPolicyArtifact(PublishedPolicyArtifactLedgerEntry policy)
    {
        Calls.Add("AppendPublishedPolicyArtifact");
        return false;
    }
    public virtual void BackfillCanonicalResponseLinksV2()
    {
        Calls.Add("BackfillCanonicalResponseLinksV2");
    }
    public virtual void TryScheduleTokenCompression()
    {
        Calls.Add("TryScheduleTokenCompression");
    }
    public virtual void CommitCompression(WorldDiplomacyJob job, string raw)
    {
        if (OnCommitCompression != null) { OnCommitCompression(job, raw); return; }
        Calls.Add("CommitCompression");
    }
    public virtual void RetryDeferredCanonicalHistoryEntries(int maxAttempts)
    {
        Calls.Add("RetryDeferredCanonicalHistoryEntries");
    }
    public virtual void TryAppendDiplomaticThreatHistoryResult(WorldDiplomacyThreat threat)
    {
        Calls.Add("TryAppendDiplomaticThreatHistoryResult");
    }
    public virtual void TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(WorldDiplomacyThreat threat)
    {
        Calls.Add("TryAppendDiplomaticThreatDomesticPenaltyHistoryResult");
    }
    public virtual void TryAppendDiplomaticThreatIssuerRewardHistoryResult(WorldDiplomacyThreat threat)
    {
        Calls.Add("TryAppendDiplomaticThreatIssuerRewardHistoryResult");
    }
    public virtual void TryAppendDiplomaticThreatNonComplianceHistoryResult(WorldDiplomacyThreat threat, WorldDiplomacyThreatNonComplianceEvent nonCompliance)
    {
        Calls.Add("TryAppendDiplomaticThreatNonComplianceHistoryResult");
    }
    public virtual void ScheduleDeferredCanonicalHistoryRetry(string documentId)
    {
        Calls.Add("ScheduleDeferredCanonicalHistoryRetry");
    }
    public virtual string BuildCanonicalHistoryBlock(long throughSequence)
    {
        Calls.Add("BuildCanonicalHistoryBlock");
        return "";
    }
    public virtual void RecordDiplomaticThreatTargetDecisions(WorldDiplomacyDocument document, string authorId, string issuerId, string intent)
    {
        Calls.Add("RecordDiplomaticThreatTargetDecisions");
    }
    public virtual void RecordDiplomaticThreatTargetDecisionsForActions(WorldDiplomacyDocument document, string authorId)
    {
        Calls.Add("RecordDiplomaticThreatTargetDecisionsForActions");
    }
    public virtual bool DeferUnresolvedRequiredThreatAction(WorldDiplomacyDocument document, string authorId, string targetId, string intent)
    {
        Calls.Add("DeferUnresolvedRequiredThreatAction");
        return false;
    }
    public virtual void ApplyDiplomaticPressureEffect(WorldDiplomacyDocument document)
    {
        Calls.Add("ApplyDiplomaticPressureEffect");
    }
    public virtual void ProcessDiplomaticThreatDocument(WorldDiplomacyDocument document, string authorId, string targetId, bool recordTargetDecisions)
    {
        Calls.Add("ProcessDiplomaticThreatDocument");
    }
    public virtual bool TryResolvePolicyConditionForThreat(WorldDiplomacyDocument document, string threatIssuerId, string threatTargetId, out WorldDiplomacyPolicySignal selected)
    {
        Calls.Add("TryResolvePolicyConditionForThreat");
        selected = null;
        return false;
    }
    public virtual bool RegisterOrAdvanceDiplomaticThreat(WorldDiplomacyDocument document, string issuerId, string targetId, string stage)
    {
        Calls.Add("RegisterOrAdvanceDiplomaticThreat");
        return false;
    }
    public virtual bool ResolveDiplomaticThreatCompliance(WorldDiplomacyDocument document, string compliantKingdomId, string issuerId)
    {
        Calls.Add("ResolveDiplomaticThreatCompliance");
        return false;
    }
    public virtual bool TryApplyUltimatumComplianceDomesticPenalty(WorldDiplomacyThreat threat, string compliantKingdomId, out int affectedClanCount)
    {
        Calls.Add("TryApplyUltimatumComplianceDomesticPenalty");
        affectedClanCount = default;
        return false;
    }
    public virtual bool TryApplyDiplomaticThreatPolicyConditionCancellation(WorldDiplomacyThreat threat)
    {
        Calls.Add("TryApplyDiplomaticThreatPolicyConditionCancellation");
        return false;
    }
    public virtual bool TryApplyDiplomaticThreatIssuerRelationReward(WorldDiplomacyThreat threat, string issuerKingdomId, out int affectedClanCount)
    {
        Calls.Add("TryApplyDiplomaticThreatIssuerRelationReward");
        affectedClanCount = default;
        return false;
    }
    public virtual void ApplyDiplomaticThreatReputationPenalty(WorldDiplomacyThreat threat, WorldDiplomacyDocument document)
    {
        Calls.Add("ApplyDiplomaticThreatReputationPenalty");
    }
    public virtual void RetryDiplomaticThreatDomesticPenalties()
    {
        Calls.Add("RetryDiplomaticThreatDomesticPenalties");
    }
    public virtual void RetryDiplomaticThreatComplianceConsequences()
    {
        Calls.Add("RetryDiplomaticThreatComplianceConsequences");
    }
    public virtual void RetryDiplomaticThreatHistoryResults()
    {
        Calls.Add("RetryDiplomaticThreatHistoryResults");
    }
    public virtual void TrySettleRelayOffer(WorldDiplomacyDocument document)
    {
        Calls.Add("TrySettleRelayOffer");
    }
    public virtual void ExecuteImmediateIntent(string authorId, string targetId, string intent, WorldDiplomacyDocument document)
    {
        Calls.Add("ExecuteImmediateIntent");
    }
    public virtual int ApplyNationalPrestigeDelta(string kingdomId, int delta, WorldDiplomacyDocument sourceDocument, string reason)
    {
        Calls.Add("ApplyNationalPrestigeDelta");
        return default;
    }
    public virtual void SettleInternationalReputationForDocument(WorldDiplomacyDocument document)
    {
        Calls.Add("SettleInternationalReputationForDocument");
    }
    public virtual void RecoverUnsettledAiInternationalReputation()
    {
        Calls.Add("RecoverUnsettledAiInternationalReputation");
    }
    public virtual void ReconcileAllNationalPrestigeVassalRelations()
    {
        Calls.Add("ReconcileAllNationalPrestigeVassalRelations");
    }
    public virtual void ReconcileNationalPrestigeVassalRelations(string kingdomId)
    {
        Calls.Add("ReconcileNationalPrestigeVassalRelations");
    }
    public virtual void ApplyZeroPrestigeBreachRelationPenalty(string kingdomId, int amount)
    {
        Calls.Add("ApplyZeroPrestigeBreachRelationPenalty");
    }
    public virtual void AnchorInternationalReputationNaturalChangeDays()
    {
        Calls.Add("AnchorInternationalReputationNaturalChangeDays");
    }
    public virtual void ProcessInternationalReputationNaturalChange()
    {
        Calls.Add("ProcessInternationalReputationNaturalChange");
    }
    public virtual bool TryGetDiplomaticStateViolation(string intent, string authorId, string targetId, out string reason)
    {
        Calls.Add("TryGetDiplomaticStateViolation");
        reason = "";
        return false;
    }
    public virtual bool TryGetDiplomaticThreatIntentViolation(string intent, string authorId, string targetId, string claimedThreatDocumentId, out string reason)
    {
        Calls.Add("TryGetDiplomaticThreatIntentViolation");
        reason = "";
        return false;
    }
    public virtual bool TryGetPlayerWorldStateIntentViolation(WorldDiplomacyDocument document, string intent, string commitment, string authorId, string targetId, out string reason)
    {
        Calls.Add("TryGetPlayerWorldStateIntentViolation");
        reason = "";
        return false;
    }
    public virtual WorldDiplomacyPeaceTerms ParseAndValidatePeaceTerms(Newtonsoft.Json.Linq.JObject json, string authorId, string targetId)
    {
        Calls.Add("ParseAndValidatePeaceTerms");
        return null;
    }
}
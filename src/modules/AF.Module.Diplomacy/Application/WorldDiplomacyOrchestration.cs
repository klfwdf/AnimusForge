using AnimusForge;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Persistence;
using Newtonsoft.Json.Linq;
using static AnimusForge.Refactor.Domain.WorldDiplomacyIntentVocabulary;
using static AnimusForge.Refactor.Domain.WorldDiplomacyEnvelopeJsonRules;

namespace AnimusForge;

// Transient, never-persisted world-diplomacy orchestration state. Owned by the
// Application-layer composer; cleared by the Behavior's runtime reset.
internal sealed class WorldDiplomacyRuntimeState
{
    internal bool DisabledStateApplied;
    internal string LastLlmCacheAffinityKey = "";
    internal bool NativeQueueSanitized;
    internal bool InitialPeaceApplicationAttempted;
    internal int LastSchedulerDay = -1;
    internal int AiDocumentsStartedDay = -1;
    internal int AiDocumentsStartedToday;
    internal bool CanonicalHistoryInitializedThisSession;
    internal int LastCanonicalSourceSyncHour = int.MinValue;
    internal long LastObservedWorldWeeklyHistoryRevision = -1L;
    internal string CanonicalHistoryRenderCacheKey = "";
    internal string CanonicalHistoryRenderCache = "";
    internal readonly HashSet<string> CanonicalHistorySourceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    internal readonly Queue<string> DeferredCanonicalHistoryDocumentIds = new Queue<string>();
    internal readonly HashSet<string> DeferredCanonicalHistoryDocumentIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    internal readonly Dictionary<string, int> DeferredCanonicalHistoryRetryAttempts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    internal readonly Dictionary<string, int> DeferredCanonicalHistoryRetryAfterHour = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    internal readonly Dictionary<WorldDiplomacyOfferCooldownKey, WorldDiplomacyOfferCooldown> OfferCooldownByKey =
        new Dictionary<WorldDiplomacyOfferCooldownKey, WorldDiplomacyOfferCooldown>();
}

// Leaf-only host surface for the diplomacy orchestration. Members may only
// expose identity resolution, immutable snapshots, clocks, logging,
// presentation effects, service reads and leaf ports - never another
// Application.
internal interface IWorldDiplomacyOrchestrationHost
{
    int CurrentDay();
    int CurrentHour();
    double CurrentCampaignHours();
    string NewId(string prefix);
    string FormatCampaignDate(int day);
    void Log(string message);
    int EstimateTokens(string text);
    bool WorldDiplomacyEnabled();
    bool LlmRequestRunning();
    void AdvanceWorldMessageTimelineRevision();

    // Settings scalars.
    int GenerationMaxTokens();
    int AnalysisMaxTokens();
    int MaxPendingJobs();
    int MaxStoredDocuments();
    int MaxAutomaticReplyDepth();
    IReadOnlyList<WorldDiplomacyThreat> Threats();
    int MaxAutomaticDocumentsPerRound();
    int MaxConsecutiveTechnicalGenerationFailuresPerRound();
    int MaxPriorityPlayerResponsesPerDocument();
    int MaxRelayParticipants();
    int RoundParticipantLimit();
    int OrdinaryRoundLimit();
    int NewEventIntervalDays();
    int RoundTargetDurationDays();
    int RoundHardDurationDays(int targetDurationDays);
    int CourtMaxDeliveryDays();
    int CivilianSpreadDays();
    int RelayPassDurationDays();
    int RelaySchemaVersion();
    int RelayTargetDurationDays();
    int MaxAiDocumentsStartedPerDay();
    int MaxPropagationArrivalsPerDay();
    int DiplomacyPromptContractVersion();
    int ResultSettlementStateSchemaVersion();
    int DecisionArchitectureVersion();
    int MaxPendingPolicySignals();
    int MaxProcessedPolicySignalKeys();
    int MaxStoredRoundSummaries();
    int MaxStoredAnnualSummaries();
    int MaxStoredCompressionSummaries();
    int MaxDiplomaticActionsPerDocument();
    int ThreatComplianceIssuerRewardMax();
    int PolicySignalRetentionDays();
    int TargetHistoryMemorySchemaVersion();
    long HistoryCompressionTriggerTokens();
    int HistoryCompressionTargetTokens();
    int CompressionJobPriority();
    int CompressionOutputTokenReserve();
    int CompressionRetryMaximumHours();
    int CompressionRetryInitialHours();
    int ConfiguredOutputTokenLimit();
    int TradeAllianceFailedProposalCooldownDays();
    int PolicyHistorySyncBatchSize();
    int PolicyHistoryForceSyncMaxBatches();
    (int minimum, int maximum) DeclarationCharacterRange();
    string CommonDiplomacyContract(WorldDiplomacyRound round);
    string CommonDiplomacySystemPrefix();

    // Identity/fact leaf queries resolved from live campaign state.
    string ResolvePartyId(string id);
    bool PartyResolved(string id);
    bool IsEliminatedParty(string id);
    bool HasIndependentAuthority(string id);
    bool IsPlayerParty(string id);
    bool IsPlayerAffiliatedParty(string id);
    bool PartiesAtWar(string firstId, string secondId);
    bool PartiesShareIdentity(string firstId, string secondId);
    bool CanAiAuthorParty(string id, out string reason);
    string PartyNameOrEmpty(string id);
    string PartyRulerId(string id);
    string PartyRulerName(string id);
    string ResolveRepresentativeId(string kingdomId);
    string ResolveOriginSettlementId(string authorId);
    float CourtDistance(string firstId, string secondId);
    bool IsRepresentativeForAddressedVassal(string receiverId, WorldDiplomacyDocument document);
    bool CampaignHasKingdoms();
    IReadOnlyList<string> AllKingdomIds();
    string ResolveEligibleDiplomacyKingdomId(string kingdomId);
    bool PartiesAllied(string firstId, string secondId);
    bool TradeAgreementExists(string firstId, string secondId);
    bool AllianceKnown();
    bool TradeKnown();
    bool IsAtWarByKingdomIds(string firstId, string secondId);
    string ResolveKingdomNameOrEmpty(string kingdomId);
    string ValidateOpenThreatWorldEligibility(WorldDiplomacyThreat threat);
    WorldDiplomacyPolicyRoundApplication.Parties ResolvePolicyParties(WorldDiplomacyPolicySignal signal);
    string ResolvePropagationReceiverId(string kingdomId, string settlementId);
    string ResolveSettlementId(string settlementId);
    int OfferCooldownLastFailedRoundDay(WorldDiplomacyOfferCooldownKey key);
    bool ExternalProposalTakenEffect(string intent, string initiatorId, string targetId);
    string NewThreatId();

    // Propagation/world snapshots (infrequent, bounded by rules).
    List<WorldDiplomacyPropagationApplication.CourtTarget> CaptureCourtTargets();
    WorldDiplomacyPropagationApplication.DistanceSnapshot CapturePropagationDistances(WorldDiplomacyDocument document);

    // Presentation / effect leafs and world snapshots.
    void ShowPlayerCourtDelivery(string receiverName);
    void RemoveQueuedNativeDiplomacyDecisions();
    List<(string firstId, string secondId)> ActiveWarKingdomPairs();
    void InvalidateWarSituationCache(string firstId, string secondId);
    bool InternalActionDepthActive();
    int DaysPerYear();
    int RecentBattleRetentionDays();
    int NativeSignalBaseValue(string action);

    // Module/service leafs.
    IReadOnlyList<WorldDiplomacyPolicySignalSnapshot> ForeignPolicySignals();
    string PublishedPolicyHistoryLedgerId();
    long PublishedPolicyHistoryRevision();
    long PublishedPolicyHistorySequence();
    IReadOnlyList<PublishedPolicyArtifactLedgerEntry> PublishedPolicyHistoryArtifacts(long cursor, int batchSize);
    bool TryAcknowledgePublishedPolicyHistoryThrough(long throughSequence);
    List<PublishedPolicyArtifactLedgerEntry> ReadPublishedPolicyArtifacts();
    long PublishedWorldWeeklyHistoryRevision();
    void RecordWorldDiplomacyWeeklyMaterialExternal(string stableKey, string title, string text,
        string authorKingdomId, string authorRulerId, string relatedKingdomId, bool isWorldLevel, int day, string gameDate);
    void RecordDiplomacyBulletinMaterial(WorldDiplomacyDocument document);

// Leaf port factories - bounded snapshots, no Application calls behind them.
    IWorldDiplomacyActionSelectionPort ActionSelection();
    IWorldDiplomacyPeaceAdmissionPort PeaceAdmission();
    IWorldDiplomacyPromptWorld PromptWorld();
    IWorldDiplomacyDraftRepairWorld DraftRepairWorld();
    IWorldDiplomacyDocumentExecutionPort DocumentExecution();
    IWorldDiplomacyPublicationPort Publication();
    IWorldDiplomacyJobPreparationPort JobPreparation();
    IWorldDiplomacyHistoryCapturePort HistoryCapture();
    IWorldDiplomacyInitialPeacePort InitialPeace();

    IWorldDiplomacyThreatSettlementPort ThreatSettlement();
    IWorldDiplomacyAnalysisPort AnalysisPort();
    IWorldDiplomacyOfferActionPort OfferAction();
    IWorldDiplomacyImmediateActionPort ImmediateAction();
    IWorldDiplomacyWarAdmissionPort WarAdmission(string firstId, string secondId);
    IWorldDiplomacyStorageNormalizationSource StorageNormalizationSource();
    IWorldDiplomacyCanonicalHistoryMigrationSource CanonicalHistoryMigrationSource();
    IWorldDiplomacyLlmDispatchSource LlmDispatchSource();
    IWorldDiplomacyCompletionSource CompletionSource();
    void PollNotifications();
    string ResolveSettlementPartyId(string settlementId);
    string PartyNameIncludingEliminated(string id);
    string PlayerKingdomId();
    string ResolveKingdomIdOrNull(string id);
    bool KingdomIsEliminated(string id);
    (string id, string name) KingdomValidationIdentity(string id);
    string SettlementValidationName(string settlementId);
    string RealmRulerDisplayName(string kingdomId);
    string RealmRulerTitle(string kingdomId);
    int GetRoundParticipantLimit();
    string CanAiAuthorDocumentBlockReason(string id);
    void LogDiplomaticThreatFallbackAnalysisPublished(WorldDiplomacyJob job);
    void Notify(string message);
}

// Application-facing orchestration surface. Applications that need a
// cross-Application transition receive this surface instead of a Behavior
// method group, so the only runtime call direction is
// Application -> WorldDiplomacyOrchestration -> Application.
internal interface IWorldDiplomacyOrchestration
{
    WorldDiplomacyRound ResolveRound(string roundId);
    WorldDiplomacyDocument ResolveDocument(string documentId);
    void EnqueueJob(WorldDiplomacyJob job);
    void EnqueueGeneration(string authorId, string targetId, WorldDiplomacyExchange exchange, bool isResponse,
        WorldDiplomacyDocument sourceDocument, int priority, bool externalResponseOnly = false, bool isReminder = false,
        string roundId = null, bool isRelayTurn = false, bool allowUntargeted = false, string previousKingdomId = null,
        int scheduledDay = -1, string resultSettlementSlotId = null);
    void EnqueueAnalysisJob(WorldDiplomacyDocument document, int priority);

    void EnqueueCompressionJob(long throughSequence, long tokenCount, int targetTokens);
    void AbandonRejectedGeneration(WorldDiplomacyJob job, string authorId, string targetId, string reason);
    void RejectGeneratedDraftBeforePublication(WorldDiplomacyJob job, string rejectedRaw,
        string authorId, string targetId, string reason, Newtonsoft.Json.Linq.JObject parsedJson);
    void CommitAnalysis(WorldDiplomacyJob job, string raw);
    void ProcessAnalyzedDocument(WorldDiplomacyDocument document, string intent, string commitment,
        bool requiresResponse, string tone, float confidence);
    void CommitFailedJob(WorldDiplomacyJob job, string error);
    void CommitGeneratedDocument(WorldDiplomacyJob job, string raw);
    bool TryGetGeneratedIntentLegalityViolation(WorldDiplomacyJob job, Newtonsoft.Json.Linq.JObject json,
        string authorId, string fallbackTargetId, out string generatedTargetId, out string reason);
    bool TryGetGeneratedSingleActionLegalityViolation(WorldDiplomacyJob job, Newtonsoft.Json.Linq.JObject json,
        string authorId, string fallbackTargetId, out string generatedTargetId, out string reason);
    bool TryApplyGeneratedSemanticEnvelope(WorldDiplomacyDocument document, Newtonsoft.Json.Linq.JObject json,
        string authorId, string fallbackTargetId, bool allowUntargeted, bool relayTurn);
    void SuppressInvalidDocumentBeforePropagation(WorldDiplomacyDocument document, string reason);
    bool TryRebuildPendingJob(WorldDiplomacyJob job);
    bool RefreshDiplomaticActionPresentationAndPrompt(WorldDiplomacyJob job);
    bool RefreshDiplomaticThreatPresentationAndPrompt(WorldDiplomacyJob job);



    void ProcessRoundLifecycle();
    void HandleRoundDocumentProcessed(WorldDiplomacyDocument document);
    void ReconcileActiveDiplomacyAfterLoad();
    void RetryDeferredRoundProgress();
    void ProcessCompletedJobs();
    void TryStartNextLlmJob();
    void PollNotifications();
    void ProcessCourtArrival(string receiverId, WorldDiplomacyDocument document);
    void ReconcileReachedCourts(WorldDiplomacyDocument document);
    WorldDiplomacyRound EnsureActiveRound(string initiatorId, string targetId, bool isPlayerInsertion);
    void CloseActiveRound(string reason);

    void CompleteExchange(string exchangeId, string reason);




    IReadOnlyList<WorldDiplomacyThreat> Threats();
    void IntegratePlayerDeclaration(WorldDiplomacyRound round, WorldDiplomacyDocument document);


    void CommitRoundCompression(WorldDiplomacyJob job, string raw);
    void CommitLocalRoundSummary(WorldDiplomacyRound round, List<WorldDiplomacyDocument> documents);
    void UpgradeRoundSummaryToStructuredArchive(WorldDiplomacyRoundSummary summary);



    void HandleDisabledState();
    void RestoreSuspendedExchangeIfAny();
    void TryScheduleNormalRound();
    void TrySchedulePolicyTriggeredRound();
    void RefreshPolicyDiplomacySignals();

    void ScheduleNextNormalRoundAfter(int baseDay);
    void RefreshRoundIntervalScheduleIfNeeded();
    void CompletePolicySignal(WorldDiplomacyPolicySignal signal, string reason);
    void TryApplyInitialNewGamePeace();
    void NormalizeStorage(bool allowWorldValidation);
    void SyncData(bool isSaving, bool isLoading,
        Func<WorldDiplomacyStorage> loadStorage, Func<string> loadError,
        Action<WorldDiplomacyStorage> saveStorage, Action<string> log, Action resetTransientRuntime);
    void ReplaceStorage(WorldDiplomacyStorage storage);
    void ResetStorageForNewGame(bool initialPeacePending);
    void EnsureScheduleInitialized();
    void ResetRuntimeState();
    void HandleWarDeclared(string firstId, string secondId);
    void HandlePeaceMade(string firstId, string secondId);
    void HandleSettlementOwnerChanged(string settlementId, string settlementName, string oldKingdomId, string newKingdomId);
    void RecordBattleFact(WorldDiplomacyBattleFact fact);
    bool RecordNativeSignal(string sourceId, string targetId, string action, string reason);
    void EnsureActiveWarLedgers();
    void TrimRecentBattleFacts();
    void TrimNativeSignals();
    void DecayWarPressure();
    void RemoveJob(string jobId);
    void AddWarPressure(string sourceId, string targetId, int delta, string reason, string intent);
    WarPressureEntry FindWarPressure(string sourceId, string targetId);
    void ClearLlmCacheAffinityKey();

    void StartDocumentPropagation(WorldDiplomacyDocument document, string authorId);
    void RetryDeferredDocumentPropagation();
    void ProcessPropagationArrivals();
    void RecalculatePendingPropagationIfNeeded();
    void RecordDiplomacyWeeklyMaterial(WorldDiplomacyDocument document);
    void PublishPlayerAuthoredDocumentImmediately(WorldDiplomacyDocument document);
    bool CanExternalDiplomacyFactJoinRound(WorldDiplomacyRound round, string initiatorId, string targetId);
    void NotifyExternalDiplomacyResolved(string action, string initiatorId, string targetId, string reason);
    void RecoverPlayerCourtReceiptsFromKnowledge();
    List<string> NormalizeKingdomIdList(IEnumerable<string> values, string excludedId);

    void FinalizePublishedDocumentAfterAnalysis(WorldDiplomacyDocument document, string authorId, string targetId,
        string normalizedIntent, bool recordNoActionDecision);

    void PruneInvalidOffers(WorldDiplomacyRound round);
    void NormalizeOfferCooldownStorage();
    void SettleTradeAllianceOfferCooldownsForClosedRound(WorldDiplomacyRound round);
    void ClearBilateralOfferCooldowns(string firstId, string secondId, WorldDiplomacyOfferDomain domain);
    List<string> BuildLegalDiplomaticActionIntents(WorldDiplomacyRound round, string authorId, string targetId);
    List<string> BuildPotentialDiplomaticActionIntents(string firstId, string secondId);
    bool AreOfferedPeaceTermsCurrentlyExecutable(WorldDiplomacyRoundOffer offer, WorldDiplomacyDocument source);
    List<string> BuildLegalDiplomaticDeclarationIntents(WorldDiplomacyRound round, string authorId, string targetId,
        bool isRelayTurn, string resultSettlementSlotId, bool isExternalResponseOnly, WorldDiplomacyDocument responseSource);
    Dictionary<string, List<string>> BuildLegalDiplomaticDeclarationIntentMap(WorldDiplomacyRound round, string authorId,
        List<string> ids, bool isRelayTurn, string resultSettlementSlotId, bool isExternalResponseOnly,
        WorldDiplomacyDocument responseSource);

    List<string> GetActionableDiplomaticTargetIds(string authorId, WorldDiplomacyRound round);

    List<string> GetAuthorizedGenerationTargetIds(WorldDiplomacyJob source, WorldDiplomacyRound round, string authorId);
    bool HasAnyLegalDiplomaticActionIntent(WorldDiplomacyRound round, string authorId, string targetId);
    bool HasCessionBoundMultiplePeaceAcceptanceOptions(WorldDiplomacyRound round, string authorId,
        IReadOnlyDictionary<string, List<string>> legalActionsByTarget);
    string BuildCurrentLegalDiplomaticOptions(WorldDiplomacyRound round, string authorId, IEnumerable<string> targetIds,
        bool isRelayTurn, string resultSettlementSlotId, bool isExternalResponseOnly, WorldDiplomacyDocument responseSource);
    bool OfferedPeaceTermsCurrentlyExecutable(WorldDiplomacyRoundOffer offer, WorldDiplomacyDocument source,
        string proposerId, string targetId);

    string BuildGenerationLegalActionSignature(WorldDiplomacyJob job);

    string BuildGenerationPromptForJob(string authorId, string targetId, WorldDiplomacyExchange exchange, bool isResponse,
        WorldDiplomacyDocument source, bool isReminder, string roundId, bool allowUntargeted, List<string> planCandidates,
        bool externalResponseOnly);


    string BuildAnalysisPrompt(WorldDiplomacyDocument document);
    string BuildFallbackAnalysisJson(WorldDiplomacyJob job);


    WorldDiplomacyDocument CreateDocument(string authorId, string targetId, string title, string body, string origin,
        bool isPlayerAuthored, bool isResponse, string exchangeId);
    void AddDocument(WorldDiplomacyDocument document);

    void EnsureCanonicalHistoryInitialized();
    void InvalidateCanonicalHistoryRenderCache();
    void SyncCanonicalHistorySources(bool force = false);
    void CaptureCanonicalHistoryForJob(WorldDiplomacyJob job, bool syncSources, long throughSequence = long.MaxValue);
    void AppendCanonicalDocumentEvents(WorldDiplomacyDocument document);
    bool AppendCanonicalHistoryEntry(string kind, string sourceKey, string sourceId, int day, string gameDate,
        string authorKingdomId, IEnumerable<string> targetKingdomIds, string intent, string commitment, string content,
        bool verified, string respondingToOfferDocumentId = null, string respondingToThreatDocumentId = null,
        IEnumerable<string> actionFacts = null);
    bool AppendCanonicalHistoryWeeklyArtifact(WorldDiplomacyWeeklyArtifact artifact);
    void SyncPublishedPolicyArtifacts(int maxBatches);
    void RebuildPublishedPolicySignaturesThrough(long throughSequence);
    bool AppendPublishedPolicyArtifact(PublishedPolicyArtifactLedgerEntry policy);
    void BackfillCanonicalResponseLinksV2();
    void TryScheduleTokenCompression();
    void CommitCompression(WorldDiplomacyJob job, string raw);
    void RetryDeferredCanonicalHistoryEntries(int maxAttempts = 16);
    void TryAppendDiplomaticThreatHistoryResult(WorldDiplomacyThreat threat);
    void TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(WorldDiplomacyThreat threat);
    void TryAppendDiplomaticThreatIssuerRewardHistoryResult(WorldDiplomacyThreat threat);
    void TryAppendDiplomaticThreatNonComplianceHistoryResult(WorldDiplomacyThreat threat,
        WorldDiplomacyThreatNonComplianceEvent nonCompliance);
    void ScheduleDeferredCanonicalHistoryRetry(string documentId);
    string BuildCanonicalHistoryBlock(long throughSequence = long.MaxValue);

    // Threat / prestige / offer / immediate-effect lane.
    int ApplyNationalPrestigeDelta(string kingdomId, int delta, WorldDiplomacyDocument sourceDocument, string reason);
    void ApplyZeroPrestigeBreachRelationPenalty(string kingdomId, int amount);
    void ProcessDiplomaticThreatDocument(WorldDiplomacyDocument document, string authorId, string targetId, bool recordTargetDecisions = true);
    bool ResolveDiplomaticThreatCompliance(WorldDiplomacyDocument document, string compliantKingdomId, string issuerId);
    bool TryApplyDiplomaticThreatPolicyConditionCancellation(WorldDiplomacyThreat threat);
    void RetryDiplomaticThreatComplianceConsequences();
    void RetryDiplomaticThreatHistoryResults();
    WorldDiplomacyOfferOutcome TrySettleRelayOffer(WorldDiplomacyDocument document);
    WorldDiplomacyImmediateActionReceipt ExecuteImmediateIntent(string authorId, string targetId, string intent, WorldDiplomacyDocument document);
    bool TryGetDiplomaticStateViolation(string intent, string authorId, string targetId, out string reason);
    bool TryGetDiplomaticThreatIntentViolation(string intent, string authorId, string targetId,
        string claimedThreatDocumentId, out string reason);
    bool TryGetPlayerWorldStateIntentViolation(WorldDiplomacyDocument document, string intent, string commitment,
        string authorId, string targetId, out string reason);
    WorldDiplomacyPeaceTerms ParseAndValidatePeaceTerms(Newtonsoft.Json.Linq.JObject json, string authorId, string targetId);
}

// Application-layer composition root for the world diplomacy lane. Every
// callback that previously bound a Behavior method reaching another
// Application is bound here instead.
internal sealed partial class WorldDiplomacyOrchestration : IWorldDiplomacyOrchestration, IWorldDiplomacyRoundClosure, AnimusForge.DiplomacyDialogue.IDiplomacyDialogueRoundPort
{
    private readonly IWorldDiplomacyOrchestrationHost _host;
    private readonly WorldDiplomacyRuntimeState _runtime;
    private readonly WorldDiplomacyStateStore _stateStore = new WorldDiplomacyStateStore();
    private bool _persistenceHealthy = true;
    internal bool IsPersistenceHealthy => _persistenceHealthy;

    internal WorldDiplomacyOrchestration(IWorldDiplomacyOrchestrationHost host, WorldDiplomacyRuntimeState runtime)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    internal WorldDiplomacyRuntimeState Runtime => _runtime;
    private WorldDiplomacyStorage Storage => _stateStore.Current;
    // Read projection for host adapters; canonical writes stay inside the store.
    internal WorldDiplomacyStorage CurrentStorage => _stateStore.Current;
    public void ReplaceStorage(WorldDiplomacyStorage storage) { _stateStore.Replace(storage); NormalizeConcurrentWork(); }
    internal bool MarkDocumentRead(string documentId) => _persistenceHealthy && WorldDiplomacyTimelineApplication.MarkRead(Storage, documentId);

    // ---------- leaf helpers shared by orchestration methods ----------

    public WorldDiplomacyRound ResolveRound(string roundId)
    {
        if (!_persistenceHealthy) return null;
        WorldDiplomacyStorage storage = Storage;
        return FindIndexedDialogueRound(roundId);
    }

    public WorldDiplomacyDocument ResolveDocument(string documentId)
    {
        if (!_persistenceHealthy) return null;
        return WorldDiplomacyRoundLifecycleRules.ResolveDocument(Storage?.Documents, documentId);
    }

    private string GetAuthorDiplomacyBlockReason(string kingdomId)
    {
        return _host.CanAiAuthorParty(kingdomId, out string reason) ? null : reason;
    }

    public void EnqueueJob(WorldDiplomacyJob job)
    {
        if (!_persistenceHealthy) return;
        if (job != null) job.MaterialCountryContractVersion = 1;
        DiplomacyEventRules.Bind(ResolveRound(job?.RoundId), job);
        WorldDiplomacyRoundLifecycleRules.EnqueueJob(Storage, job, _host.MaxPendingJobs());
    }

    public void NormalizeOfferCooldownStorage()
    {
        WorldDiplomacyOfferCooldownStorageNormalizer.Normalize(Storage);
        WorldDiplomacyRoundLifecycleRules.RebuildOfferCooldownIndex(Storage?.OfferCooldowns, _runtime.OfferCooldownByKey);
    }

    public void SettleTradeAllianceOfferCooldownsForClosedRound(WorldDiplomacyRound round)
    {
        WorldDiplomacyOfferCooldownApplication.SettleTradeAllianceOfferCooldownsForClosedRound(
            round, Storage?.OfferCooldowns, _runtime.OfferCooldownByKey, NormalizeOfferCooldownStorage, _host.Log);
    }

    public void ClearBilateralOfferCooldowns(string firstId, string secondId, WorldDiplomacyOfferDomain domain)
    {
        if (firstId == null || secondId == null
            || string.Equals(firstId, secondId, StringComparison.OrdinalIgnoreCase)) return;
        WorldDiplomacyRoundLifecycleRules.ClearBilateralOfferCooldowns(
            Storage?.OfferCooldowns, _runtime.OfferCooldownByKey, firstId, secondId, domain);
    }

    public void ScheduleNextNormalRoundAfter(int baseDay)
    {
        WorldDiplomacyStorage storage = Storage;
        if (storage == null) return;
        int interval = Math.Max(1, Math.Min(30, _host.NewEventIntervalDays()));
        int lastOpening = storage.LastOrdinaryRoundStartedDay ?? -1;
        storage.NextNormalRoundDay = WorldDiplomacyRoundLifecycleRules.ComputeNextRoundDay(lastOpening >= 0 ? lastOpening : baseDay, interval);
        storage.LastAppliedRoundIntervalDays = interval;
    }

    // Daily/settings boundary only; elapsed time is retained when the interval changes.
    public void RefreshRoundIntervalScheduleIfNeeded()
    {
        WorldDiplomacyLiveRoundRules.RefreshOpeningInterval(Storage, _host.NewEventIntervalDays());
    }

    public void CommitLocalRoundSummary(WorldDiplomacyRound round, List<WorldDiplomacyDocument> documents)
    {
        WorldDiplomacyRoundLifecycleRules.CommitLocalRoundSummary(Storage, round, documents,
            _host.CurrentDay, _host.FormatCampaignDate, _host.Log);
    }

    public void UpgradeRoundSummaryToStructuredArchive(WorldDiplomacyRoundSummary summary)
    {
        WorldDiplomacyRoundLifecycleRules.UpgradeRoundSummaryToStructuredArchive(Storage, summary,
            ResolveRound, _host.FormatCampaignDate);
    }

    // ---------- canonical history pipeline ----------

    public void EnsureCanonicalHistoryInitialized()
    {
        WorldDiplomacyHistoryCaptureApplication.EnsureInitialized(Storage,
            ref _runtime.CanonicalHistoryInitializedThisSession,
            _runtime.CanonicalHistorySourceKeys,
            _host.HistoryCompressionTriggerTokens(),
            _host.EstimateTokens, InvalidateCanonicalHistoryRenderCache, _host.Log);
    }

    public void InvalidateCanonicalHistoryRenderCache()
    {
        _runtime.CanonicalHistoryRenderCacheKey = "";
        _runtime.CanonicalHistoryRenderCache = "";
    }

    public void SyncCanonicalHistorySources(bool force = false)
    {
        WorldDiplomacyHistoryCaptureApplication.SyncSources(_host.HistoryCapture(), this, force,
            ref _runtime.LastCanonicalSourceSyncHour, ref _runtime.LastObservedWorldWeeklyHistoryRevision,
            _host.PolicyHistoryForceSyncMaxBatches());
    }

    public void CaptureCanonicalHistoryForJob(WorldDiplomacyJob job, bool syncSources, long throughSequence = long.MaxValue)
    {
        WorldDiplomacyHistoryCaptureApplication.Capture(Storage, job, syncSources, throughSequence,
            _host.HistoryCapture(), this, _host.EstimateTokens);
    }

    private void CaptureCanonicalHistoryForQueuedJob(WorldDiplomacyJob job)
    {
        CaptureCanonicalHistoryForJob(job, syncSources: false);
    }

    public void AppendCanonicalDocumentEvents(WorldDiplomacyDocument document)
    {
        WorldDiplomacyHistoryPublicationApplication.AppendCanonicalDocumentEvents(
            Storage, _runtime.CanonicalHistorySourceKeys, _host.HistoryCompressionTriggerTokens(),
            EnsureCanonicalHistoryInitialized, _host.NewId, _host.FormatCampaignDate, _host.EstimateTokens,
            InvalidateCanonicalHistoryRenderCache, document);
    }

    public bool AppendCanonicalHistoryWeeklyArtifact(WorldDiplomacyWeeklyArtifact artifact)
    {
        if (artifact == null) return false;
        WorldDiplomacyHistoryPublicationApplication.AppendPublishedWorldWeeklyArtifact(Storage,
            _runtime.CanonicalHistorySourceKeys, _host.HistoryCompressionTriggerTokens(),
            EnsureCanonicalHistoryInitialized, _host.NewId, _host.FormatCampaignDate, _host.EstimateTokens,
            InvalidateCanonicalHistoryRenderCache, artifact.SourceId, artifact.Title, artifact.Text,
            artifact.Day, artifact.Date);
        return true;
    }

    public void SyncPublishedPolicyArtifacts(int maxBatches)
    {
        EnsureCanonicalHistoryInitialized();
        WorldDiplomacyHistoryPublicationApplication.SyncPublishedPolicyArtifacts(
            Storage, _runtime.CanonicalHistorySourceKeys, _host.HistoryCompressionTriggerTokens(),
            EnsureCanonicalHistoryInitialized, _host.NewId, _host.FormatCampaignDate, _host.EstimateTokens,
            InvalidateCanonicalHistoryRenderCache, _host.CurrentDay, _host.Log,
            maxBatches, _host.PolicyHistorySyncBatchSize(),
            _host.PublishedPolicyHistoryLedgerId,
            _host.PublishedPolicyHistoryRevision,
            _host.PublishedPolicyHistorySequence,
            _host.PublishedPolicyHistoryArtifacts,
            _host.TryAcknowledgePublishedPolicyHistoryThrough);
    }

    public void RebuildPublishedPolicySignaturesThrough(long throughSequence)
    {
        WorldDiplomacyHistoryPublicationApplication.RebuildPublishedPolicySignaturesThrough(
            Storage?.CanonicalHistory, throughSequence, _host.PublishedPolicyHistoryArtifacts);
    }

    public bool AppendPublishedPolicyArtifact(PublishedPolicyArtifactLedgerEntry policy)
    {
        return WorldDiplomacyHistoryPublicationApplication.AppendPublishedPolicyArtifact(
            Storage, _runtime.CanonicalHistorySourceKeys, _host.HistoryCompressionTriggerTokens(),
            EnsureCanonicalHistoryInitialized, _host.NewId, _host.FormatCampaignDate, _host.EstimateTokens,
            InvalidateCanonicalHistoryRenderCache, _host.CurrentDay, policy);
    }

    public void BackfillCanonicalResponseLinksV2()
    {
        WorldDiplomacyStorageMigration.BackfillCanonicalResponseLinksV2(Storage,
            (document, targets) => AppendCanonicalHistoryEntry("declaration",
                "document:" + document.DocumentId + ":response_link_v2",
                document.DocumentId, document.Day, document.GameDate, document.AuthorKingdomId,
                targets, document.Intent, document.Commitment, document.Body,
                verified: true, respondingToOfferDocumentId: document.RespondingToOfferDocumentId));
    }

    public bool TryRebuildPendingJob(WorldDiplomacyJob job)
    {
        if (IsOralDeclarationJob(job)) return RebuildOralDeclarationJob(job);
        return WorldDiplomacyJobPreparationApplication.Rebuild(_host.JobPreparation(), this, job, _host.PromptWorld().LocalizePrompt);
    }

    public string BuildCanonicalHistoryBlock(long throughSequence = long.MaxValue)
    {
        EnsureCanonicalHistoryInitialized();
        WorldDiplomacyCanonicalHistoryState history = Storage?.CanonicalHistory;
        if (history == null) return "";
        long cutoff = throughSequence == long.MaxValue ? history.NextSequence - 1L : Math.Max(0L, throughSequence);
        string cacheKey = (history.Snapshot.ContentHash ?? "") + "|" + history.Snapshot.CoveredThroughSequence.ToString(CultureInfo.InvariantCulture)
            + "|" + cutoff.ToString(CultureInfo.InvariantCulture);
        if (string.Equals(_runtime.CanonicalHistoryRenderCacheKey, cacheKey, StringComparison.Ordinal)
            && !string.IsNullOrEmpty(_runtime.CanonicalHistoryRenderCache))
        {
            return _runtime.CanonicalHistoryRenderCache;
        }
        string rendered = WorldDiplomacyCanonicalHistoryRules.RenderCanonicalHistoryBlock(history, cutoff, _host.PromptWorld().LocalizePrompt);
        _runtime.CanonicalHistoryRenderCacheKey = cacheKey;
        _runtime.CanonicalHistoryRenderCache = rendered;
        return rendered;
    }

    private int _compressionConfigurationHour = -1;
    private WorldDiplomacyStorage _compressionConfigurationStorage;
    private void RefreshCompressionConfiguration()
    {
        if (_host is not IDiplomacyModelConfiguration configuration) return;
        int hour = _host.CurrentHour();
        if (ReferenceEquals(Storage, _compressionConfigurationStorage) && hour == _compressionConfigurationHour) return;
        _compressionConfigurationStorage = Storage; _compressionConfigurationHour = hour;
        string fingerprint = configuration.ConfigurationFingerprint() ?? "";
        if (string.IsNullOrEmpty(fingerprint) || fingerprint == Storage.CompressionConfigurationFingerprint) return;
        if (!string.IsNullOrEmpty(Storage.CompressionConfigurationFingerprint)) DiplomacyCompressionBudget.Resume(Storage, true);
        Storage.CompressionConfigurationFingerprint = fingerprint;
    }

    public void TryScheduleTokenCompression()
    {
        RefreshCompressionConfiguration();
        WorldDiplomacyHistoryCompressionApplication.TryScheduleTokenCompression(Storage,
            _host.WorldDiplomacyEnabled, EnsureCanonicalHistoryInitialized,
            () => SyncCanonicalHistorySources(), _host.CurrentHour,
            _host.HistoryCompressionTriggerTokens(), _host.HistoryCompressionTargetTokens(),
            EnqueueCompressionJob);
    }

    public void EnqueueCompressionJob(long throughSequence, long tokenCount, int targetTokens)
    {
        WorldDiplomacyHistoryCompressionApplication.EnqueueCompressionJob(Storage,
            throughSequence, tokenCount, targetTokens,
            EnsureCanonicalHistoryInitialized,
            _host.DeclarationCharacterRange, _host.CommonDiplomacySystemPrefix, _host.EstimateTokens,
            _host.HistoryCompressionTriggerTokens(), _host.CompressionJobPriority(),
            _host.CompressionOutputTokenReserve(), _host.CompressionRetryMaximumHours(), _host.MaxPendingJobs(),
            _host.CurrentHour, _host.CurrentDay,
            _host.ConfiguredOutputTokenLimit,
            _host.NewId,
            (job, seq) => CaptureCanonicalHistoryForJob(job, syncSources: false, throughSequence: seq),
            _host.Log, _host.PromptWorld().LocalizePrompt);
    }

    public void CommitCompression(WorldDiplomacyJob job, string raw)
    {
        WorldDiplomacyHistoryCompressionApplication.CommitCompression(Storage, job, raw,
            EnsureCanonicalHistoryInitialized, _host.EstimateTokens, _host.CurrentDay,
            _host.HistoryCompressionTargetTokens(), _host.HistoryCompressionTriggerTokens(),
            InvalidateCanonicalHistoryRenderCache, _host.Log);
    }

    public void RetryDeferredCanonicalHistoryEntries(int maxAttempts = 16)
    {
        WorldDiplomacyHistoryCaptureApplication.RetryDeferredCanonicalHistoryEntries(
            _runtime.DeferredCanonicalHistoryDocumentIds, _runtime.DeferredCanonicalHistoryDocumentIdSet,
            _runtime.DeferredCanonicalHistoryRetryAttempts, _runtime.DeferredCanonicalHistoryRetryAfterHour,
            Storage?.DiplomaticThreats, _host.CurrentHour(),
            ResolveDocument, AppendCanonicalDocumentEvents,
            TryAppendDiplomaticThreatHistoryResult, TryAppendDiplomaticThreatDomesticPenaltyHistoryResult,
            TryAppendDiplomaticThreatIssuerRewardHistoryResult, TryAppendDiplomaticThreatNonComplianceHistoryResult,
            _host.Log, maxAttempts);
    }

    public void TryAppendDiplomaticThreatHistoryResult(WorldDiplomacyThreat threat)
    {
        WorldDiplomacyHistoryPublicationApplication.TryAppendDiplomaticThreatHistoryResult(
            Storage, _runtime.CanonicalHistorySourceKeys, AppendCanonicalHistoryEntry, ResolveDocument,
            _host.FormatCampaignDate, _host.PartyNameOrEmpty, _host.Log, threat);
    }

    public void TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(WorldDiplomacyThreat threat)
    {
        WorldDiplomacyHistoryPublicationApplication.TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(
            Storage, _runtime.CanonicalHistorySourceKeys, AppendCanonicalHistoryEntry, ResolveDocument,
            _host.FormatCampaignDate, _host.PartyNameIncludingEliminated, _host.Log, threat);
    }

    public void TryAppendDiplomaticThreatIssuerRewardHistoryResult(WorldDiplomacyThreat threat)
    {
        WorldDiplomacyHistoryPublicationApplication.TryAppendDiplomaticThreatIssuerRewardHistoryResult(
            Storage, _runtime.CanonicalHistorySourceKeys, AppendCanonicalHistoryEntry, ResolveDocument,
            _host.FormatCampaignDate, _host.PartyNameOrEmpty, _host.Log, threat);
    }

    public void TryAppendDiplomaticThreatNonComplianceHistoryResult(WorldDiplomacyThreat threat,
        WorldDiplomacyThreatNonComplianceEvent nonCompliance)
    {
        WorldDiplomacyHistoryPublicationApplication.TryAppendDiplomaticThreatNonComplianceHistoryResult(
            Storage, _runtime.CanonicalHistorySourceKeys, AppendCanonicalHistoryEntry, ResolveDocument,
            _host.FormatCampaignDate, _host.PartyNameOrEmpty, _host.Log, threat, nonCompliance);
    }

    public bool AppendCanonicalHistoryEntry(
        string kind,
        string sourceKey,
        string sourceId,
        int day,
        string gameDate,
        string authorKingdomId,
        IEnumerable<string> targetKingdomIds,
        string intent,
        string commitment,
        string content,
        bool verified,
        string respondingToOfferDocumentId = null,
        string respondingToThreatDocumentId = null,
        IEnumerable<string> actionFacts = null)
    {
        return WorldDiplomacyHistoryPublicationApplication.AppendCanonicalHistoryEntry(
            Storage, _runtime.CanonicalHistorySourceKeys, _host.HistoryCompressionTriggerTokens(),
            EnsureCanonicalHistoryInitialized, _host.NewId, _host.FormatCampaignDate, _host.EstimateTokens,
            InvalidateCanonicalHistoryRenderCache,
            kind, sourceKey, sourceId, day, gameDate, authorKingdomId, targetKingdomIds,
            intent, commitment, content, verified, respondingToOfferDocumentId,
            respondingToThreatDocumentId, actionFacts);
    }

    public void ScheduleDeferredCanonicalHistoryRetry(string documentId)
    {
        WorldDiplomacyRoundLifecycleRules.ScheduleDeferredCanonicalHistoryRetry(
            _runtime.DeferredCanonicalHistoryRetryAttempts, _runtime.DeferredCanonicalHistoryRetryAfterHour,
            _runtime.DeferredCanonicalHistoryDocumentIdSet, _runtime.DeferredCanonicalHistoryDocumentIds,
            documentId, _host.CurrentHour());
    }

    public void RecordDiplomacyWeeklyMaterial(WorldDiplomacyDocument document)
    {
        if (document == null || !document.IsReadyForPublication) return;
        _host.RecordDiplomacyBulletinMaterial(document);
        WorldDiplomacyHistoryCaptureApplication.RecordDiplomacyWeeklyMaterial(
            document, Storage?.Documents, _host.RecordWorldDiplomacyWeeklyMaterialExternal);
    }

    // ---------- legal selection ----------

    public List<string> BuildLegalDiplomaticActionIntents(WorldDiplomacyRound round, string authorId, string targetId)
    {
        var intents = new WorldDiplomacyActionSelectionApplication(_host.ActionSelection())
            .BuildLegalDiplomaticActionIntents(round, authorId, targetId,
                WorldDiplomacyPlayerApplication.InvolvesPlayer(null, round, _host.IsPlayerAffiliatedParty));
        if (CanReleasePlayerSubject(authorId, targetId)) intents.Add("release_subject");
        return FilterEndingIntents(round, authorId, targetId, intents);
    }

    public List<string> BuildLegalDiplomaticDeclarationIntents(
        WorldDiplomacyRound round, string authorId, string targetId, bool isRelayTurn,
        string resultSettlementSlotId, bool isExternalResponseOnly, WorldDiplomacyDocument responseSource)
    {
        return FilterEndingIntents(round, authorId, targetId, new WorldDiplomacyActionSelectionApplication(_host.ActionSelection())
            .BuildLegalDiplomaticDeclarationIntents(round, authorId, targetId, isRelayTurn,
                resultSettlementSlotId, isExternalResponseOnly, responseSource,
                WorldDiplomacyPlayerApplication.InvolvesPlayer(responseSource, round, _host.IsPlayerAffiliatedParty)));
    }



    public bool HasAnyLegalDiplomaticActionIntent(WorldDiplomacyRound round, string authorId, string targetId)
    {
        return BuildLegalDiplomaticActionIntents(round, authorId, targetId).Count > 0;
    }

    public List<string> GetActionableDiplomaticTargetIds(string authorId, WorldDiplomacyRound round)
    {
        var targets = new WorldDiplomacyActionSelectionApplication(_host.ActionSelection())
            .GetActionableDiplomaticTargets(authorId, round);
        return DiplomacyEventRules.IsEvent(round) ? targets : targets.Where(x => !_host.IsPlayerAffiliatedParty(x)).ToList();
    }



    public Dictionary<string, List<string>> BuildLegalDiplomaticDeclarationIntentMap(
        WorldDiplomacyRound round, string authorId, List<string> ids, bool isRelayTurn,
        string resultSettlementSlotId, bool isExternalResponseOnly, WorldDiplomacyDocument responseSource)
    {
        Dictionary<string, List<string>> result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        if (authorId == null) return result;
        HashSet<string> requestedIds = new HashSet<string>((ids ?? Enumerable.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x)
                && !string.Equals(x, authorId, StringComparison.OrdinalIgnoreCase)), StringComparer.OrdinalIgnoreCase);
        if (requestedIds.Count == 0) return result;
        foreach (string id in requestedIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            if (!_host.PartyResolved(id)) continue;
            List<string> actions = WorldDiplomacyRoundLifecycleRules.NormalizeOrderedIdList(
                BuildLegalDiplomaticDeclarationIntents(round, authorId, id, isRelayTurn,
                    resultSettlementSlotId, isExternalResponseOnly, responseSource)
                .Select(WorldDiplomacyIntentVocabulary.NormalizeIntent));
            if (actions.Count > 0) result[id] = actions;
        }
        return result;
    }

    public List<string> GetAuthorizedGenerationTargetIds(WorldDiplomacyJob source, WorldDiplomacyRound round, string authorId)
    {
        WorldDiplomacyDocument responseSource = ResolveDocument(source?.SourceDocumentId);
        return WorldDiplomacyRoundLifecycleRules.GetAuthorizedGenerationTargetIds(
            source, round, authorId,
            id => _host.PartyResolved(id)
                && !_host.IsEliminatedParty(id)
                && _host.HasIndependentAuthority(id)
                && BuildLegalDiplomaticDeclarationIntents(
                    round, authorId, id,
                    source.IsRelayTurn,
                    source.ResultSettlementSlotId,
                    source.IsExternalResponseOnly,
                    responseSource).Count > 0);
    }

    public string BuildCurrentLegalDiplomaticOptions(
        WorldDiplomacyRound round, string authorId, IEnumerable<string> targetKingdomIds,
        bool isRelayTurn, string resultSettlementSlotId, bool isExternalResponseOnly, WorldDiplomacyDocument responseSource)
    {
        if (authorId == null) return DiplomacyMiscPromptTexts.Get("Orch_AvailableActionsNone", "Available actions: None.");
        List<string> lines = new List<string>();
        foreach (string id in WorldDiplomacyRoundLifecycleRules.NormalizeOrderedIdList((targetKingdomIds ?? round?.Participants?.Where(x => x.State == "active").Select(x => x.KingdomId) ?? Enumerable.Empty<string>())
            .Where(x => !string.Equals(x, authorId, StringComparison.OrdinalIgnoreCase))))
        {
            if (!_host.PartyResolved(id)) continue;
            List<string> actions = BuildLegalDiplomaticDeclarationIntents(
                round, authorId, id, isRelayTurn, resultSettlementSlotId, isExternalResponseOnly, responseSource);
            List<string> normalizedActions = WorldDiplomacyRoundLifecycleRules.NormalizeOrderedIdList(actions
                .Select(WorldDiplomacyIntentVocabulary.NormalizeIntent));
            if (normalizedActions.Count == 0) continue;
            lines.Add(id + "=" + string.Join("/", normalizedActions));
        }
        return lines.Count == 0
            ? DiplomacyMiscPromptTexts.Get("Orch_AvailableActionsNoneNoFiller", "Available actions: None; do not generate filler declarations.")
            : DiplomacyMiscPromptTexts.Get("Orch_AvailableActionsPrefix", "Available actions: ") + string.Join(", ", lines) + ".";
    }

    public bool HasCessionBoundMultiplePeaceAcceptanceOptions(
        WorldDiplomacyRound round, string authorId, IReadOnlyDictionary<string, List<string>> legalActionsByTarget)
    {
        if (round == null || authorId == null || legalActionsByTarget == null) return false;
        HashSet<string> acceptingTargets = new HashSet<string>(legalActionsByTarget
            .Where(x => x.Value?.Contains("accept_peace", StringComparer.OrdinalIgnoreCase) == true)
            .Select(x => x.Key), StringComparer.OrdinalIgnoreCase);
        if (acceptingTargets.Count <= 1) return false;
        Dictionary<string, WorldDiplomacyDocument> documentsById = WorldDiplomacyDocumentFactRules.BuildDocumentIndex(Storage?.Documents);
        foreach (WorldDiplomacyRoundOffer offer in round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
        {
            if (!WorldDiplomacyRoundLifecycleRules.IsOpenOfferToTarget(offer, authorId)
                || !acceptingTargets.Contains(offer.ProposerKingdomId ?? "")
                || !string.Equals(WorldDiplomacyIntentVocabulary.NormalizeIntent(offer.Intent), "propose_peace", StringComparison.OrdinalIgnoreCase)
                || !documentsById.TryGetValue(offer.SourceDocumentId ?? "", out WorldDiplomacyDocument source)) continue;
            if (WorldDiplomacyDocumentFactRules.PeaceTermsContainCession(WorldDiplomacyDocumentFactRules.ResolveOfferedPeaceTerms(source, offer.SourceActionId))) return true;
        }
        return false;
    }









    public List<string> BuildPotentialDiplomaticActionIntents(string firstId, string secondId)
    {
        return new WorldDiplomacyActionSelectionApplication(_host.ActionSelection())
            .BuildPotentialDiplomaticActionIntents(firstId, secondId);
    }

    public bool AreOfferedPeaceTermsCurrentlyExecutable(WorldDiplomacyRoundOffer offer, WorldDiplomacyDocument source)
    {
        return OfferedPeaceTermsCurrentlyExecutable(offer, source, offer?.ProposerKingdomId, offer?.TargetKingdomId);
    }

    public bool OfferedPeaceTermsCurrentlyExecutable(WorldDiplomacyRoundOffer offer,
        WorldDiplomacyDocument source, string proposerId, string targetId)
    {
        return WorldDiplomacyPeaceAdmissionApplication.AreOfferedPeaceTermsCurrentlyExecutable(
            _host.PeaceAdmission(), offer, source, proposerId, targetId);
    }

    public void PruneInvalidOffers(WorldDiplomacyRound round)
    {
        InvalidateDialogueIndex();
        Dictionary<string, WorldDiplomacyDocument> offerPruneDocumentsById = null;
        WorldDiplomacyRoundLifecycleRules.PruneInvalidOffers(round,
            _host.CampaignHasKingdoms,
            id => (_host.PartyResolved(id), _host.IsEliminatedParty(id), _host.HasIndependentAuthority(id)),
            _host.PartiesShareIdentity,
            _host.PartiesAtWar,
            _host.AllianceKnown,
            _host.PartiesAllied,
            _host.TradeKnown,
            _host.TradeAgreementExists,
            offer =>
            {
                offerPruneDocumentsById ??= WorldDiplomacyDocumentFactRules.BuildDocumentIndex(Storage?.Documents);
                offerPruneDocumentsById.TryGetValue(offer.SourceDocumentId ?? "", out WorldDiplomacyDocument source);
                return OfferedPeaceTermsCurrentlyExecutable(offer, source, offer.ProposerKingdomId, offer.TargetKingdomId);
            },
            _host.Log);
        if (_host.CampaignHasKingdoms() && round?.PendingOffers != null)
            foreach (var offer in round.PendingOffers.Where(x => x != null && x.Status == "open" && IsFormalTreatyIntent(x.Intent)))
                if (!ValidateFormalTreatyTerms(offer.Intent, ResolveDialogueTerms(ResolveDocument(offer.SourceDocumentId), offer.SourceActionId),
                    offer.ProposerKingdomId, offer.TargetKingdomId, out _)) offer.Status = "invalidated";
    }

    // ---------- court / relay / generation spine ----------

    public void ProcessCompletedJobs()
    {
        if (!_persistenceHealthy) return;
        RefreshPlayerAuthority();
        var source = _host.CompletionSource();
        WorldDiplomacyCompletionApplication.Run(ref source, this);
    }

    public void TryStartNextLlmJob()
    {
        if (!_persistenceHealthy) return;
        if (!_host.WorldDiplomacyEnabled()) return;
        ResumeDiplomacyEvents();
        long now = DateTime.UtcNow.Ticks;
        if (now < _nextDiplomacyDispatchUtcTicks) return;
        _nextDiplomacyDispatchUtcTicks = now + TimeSpan.TicksPerSecond;
        RefreshPlayerAuthority();
        int day = _host.CurrentDay();
        Storage.RequestBudget.AdvanceDay(day);
        ProcessDuePlayerSilence();
        MaintainAidRequests();
        MaintainSpeechReviews();
        MaintainEvents();
        if (_diplomacyDispatchDay != day) { _diplomacyDispatchDay = day; _diplomacyWorkNeedsReconcile = true; }
        if (_diplomacyWorkNeedsReconcile)
        {
            _diplomacyWorkNeedsReconcile = false;
            ReconcileEventQueue();
        }
        var source = _host.LlmDispatchSource();
        for (int i = RequestLeases.Count; i < MaxConcurrentDiplomacyRequests; i++)
            WorldDiplomacyLlmDispatchApplication.Run(ref source, this);
    }

    public void PollNotifications()
    {
        if (!_persistenceHealthy) return;
        _host.PollNotifications();
    }





    public void ProcessCourtArrival(string receiverId, WorldDiplomacyDocument document)
    {
        if (document == null) return;
        RecordDiplomaticDocumentPersonalMemory(_host.PartyRulerId(receiverId), document, false);
        _diplomacyWorkNeedsReconcile = true;
        // Historical/closed documents can still arrive at the player's court;
        // they cannot start a second legacy response or promote an observer.
        if (!ProcessPlayerEventCourtArrival(receiverId, document)
            && _host.IsPlayerAffiliatedParty(receiverId) && document.IsReadyForPublication)
            document.HasReachedPlayerCourt = true;
        _host.Log("court received document=" + document.DocumentId + " receiver=" + receiverId
            + " direct=" + WorldDiplomacyStructureRules.GetDocumentTargetIds(document).Contains(receiverId)
            + " day=" + _host.CurrentDay());
    }





    public void EnqueueGeneration(
        string authorId,
        string targetId,
        WorldDiplomacyExchange exchange,
        bool isResponse,
        WorldDiplomacyDocument sourceDocument,
        int priority,
        bool externalResponseOnly = false,
        bool isReminder = false,
        string roundId = null,
        bool isRelayTurn = false,
        bool allowUntargeted = false,
        string previousKingdomId = null,
        int scheduledDay = -1,
        string resultSettlementSlotId = null)
    {
        var round = ResolveRound(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(roundId, exchange?.ExchangeId, sourceDocument?.RoundId));
        QueueEventDeclaration(round, authorId, targetId, sourceDocument, priority, externalResponseOnly, isReminder);
    }

    public bool RefreshDiplomaticActionPresentationAndPrompt(WorldDiplomacyJob job)
    {
        if (IsOralDeclarationJob(job)) return RebuildOralDeclarationJob(job);
        return WorldDiplomacyJobPreparationApplication.RefreshDiplomaticActionPresentationAndPrompt(
            _host.JobPreparation(), this, job, _host.PromptWorld().LocalizePrompt);
    }

    public bool RefreshDiplomaticThreatPresentationAndPrompt(WorldDiplomacyJob job)
    {
        if (IsOralDeclarationJob(job)) return RebuildOralDeclarationJob(job);
        return WorldDiplomacyJobPreparationApplication.RefreshDiplomaticThreatPresentationAndPrompt(
            _host.JobPreparation(), this, job, _host.PromptWorld().LocalizePrompt);
    }

    public void EnqueueAnalysisJob(WorldDiplomacyDocument document, int priority)
    {
        if (!_persistenceHealthy) return;
        if (document?.IsPlayerAuthored == true) EnsurePlayerDocumentRound(document);
        WorldDiplomacyJobPreparationApplication.PrepareAnalysisJob(
            document, priority, Storage, _host.CurrentDay(), _host.AnalysisMaxTokens(),
            _host.NewId, ResolveRound, _host.CommonDiplomacyContract, BuildAnalysisPrompt, EnqueueJob, _host.PromptWorld().LocalizePrompt);
    }



    public void AbandonRejectedGeneration(WorldDiplomacyJob job, string authorId, string targetId, string reason)
    {
        if (job == null) return;
        // A failed draft is technical pending work, never a political outcome.
        SuspendDiplomacyJob(job, reason);
    }

    public void RejectGeneratedDraftBeforePublication(
        WorldDiplomacyJob job, string rejectedRaw, string authorId, string targetId, string reason,
        Newtonsoft.Json.Linq.JObject parsedJson)
    {
        if (IsOralDeclarationJob(job)) { DeferOralDeclarationJob(job, reason); return; }
        WorldDiplomacyDraftRepairApplication.RejectGeneratedDraftBeforePublication(
            _host.DraftRepairWorld(), this, job, rejectedRaw, authorId, targetId, reason, parsedJson);
    }

    public void SuppressInvalidDocumentBeforePropagation(WorldDiplomacyDocument document, string reason)
    {
        WorldDiplomacyAnalysisApplication.Suppress(_host.AnalysisPort(), this, document, reason);
    }

    public void CommitAnalysis(WorldDiplomacyJob job, string raw)
    {
        if (job != null && _host.IsPlayerAffiliatedParty(job.AuthorKingdomId)
            && DialogueHost?.IsPlayerRuler(_host.PartyRulerId(job.AuthorKingdomId)) != true)
        {
            var unauthorized = ResolveDocument(job.DocumentId);
            if (unauthorized != null) unauthorized.AnalysisStatus = "published_action_rejected";
            _pendingPlayerAnalysisIds.Remove(job.DocumentId);
            return;
        }
        WorldDiplomacyAnalysisApplication.Commit(_host.AnalysisPort(), this, job, raw);
        var document = ResolveDocument(job.DocumentId);
        if (document?.AnalysisStatus != "pending_analysis" || document.PlayerAnalysisCommitted) _pendingPlayerAnalysisIds.Remove(job.DocumentId);
        if (document?.AnalysisStatus is "success" or "published_action_rejected") QueuePlayerReplyRoutingCheck(document);
        RegisterPlayerResponseWork(document);
    }

    public void ProcessAnalyzedDocument(
        WorldDiplomacyDocument document, string intent, string commitment,
        bool requiresResponse, string tone, float confidence)
    {
        var oralTreaty = BoundOralTreaty(document);
        bool recoveringTreaty = oralTreaty?.TreatyExecutionState == "pending"
            && _oralAgreementExecutionDocumentId == document.DocumentId && IsCurrentDialoguePublicationWork(oralTreaty);
        if (!string.IsNullOrEmpty(document?.DialogueArrangementId) && document.IsReadyForPublication && !recoveringTreaty) return;
        if (oralTreaty != null && !string.IsNullOrEmpty(oralTreaty.TreatyExecutionState)
            && oralTreaty.TreatyExecutionState != "pending") return;
        if (!ValidateSilenceDocumentForExecution(document)) return;
        if (document?.IsPlayerAuthored == true)
        {
            if (CurrentPlayerAuthority() != document.AuthorKingdomId)
            { document.AnalysisStatus = "published_action_rejected"; return; }
            EnsurePlayerDocumentRound(document);
            BindPlayerDeclarationToSharedEvent(document);
        }
        if (WorldDiplomacyIntentVocabulary.IsFormalTreatyIntent(intent)
            && !ValidateFormalTreatyDeclaration(document, intent, document.TreatyTerms,
                document.AuthorKingdomId, document.TargetKingdomId, document.RespondingToOfferDocumentId,
                document.RespondingToOfferActionId, out string treatyReason))
        {
            document.MechanicalResult = DiplomacyMiscPromptTexts.Get("Orch_TreatyNotExecutedPrefix", "Treaty not executed: ") + treatyReason;
            SuppressInvalidDocumentBeforePropagation(document, treatyReason);
            return;
        }

        if (document?.IsPlayerAuthored != true && (!_host.HasIndependentAuthority(document.AuthorKingdomId)
            || (!string.IsNullOrEmpty(document.AuthorRulerId) && document.AuthorRulerId != _host.PartyRulerId(document.AuthorKingdomId))))
        { SuppressInvalidDocumentBeforePropagation(document, "event_author_authority_changed"); return; }
        if (!IsCurrentDialogueDocumentWork(document)) return;
        if (!_committingEventDocuments && EventHost != null && ResolveRound(document.RoundId) is WorldDiplomacyRound eventRound
            && eventRound.EventSystemVersion > 0 && !EventBoundaryComplete(eventRound, "started"))
        {
            document.Intent = intent; document.Commitment = commitment; document.RequiresResponse = requiresResponse;
            foreach (string target in WorldDiplomacyStructureRules.GetDocumentTargetIds(document))
                ObserveEventTarget(eventRound, target, requiresResponse);
            EnsureEventBoundary(eventRound, "started");
            // The accepted arrangement remains durable while the opening is prepared.
            // A presentation/model failure cannot cancel it or execute it in secret.
            document.EventExecutionPending = true; document.EventExecutionTone = tone; document.EventExecutionConfidence = confidence;
            return;
        }
        if (TryProcessOfferWithdrawal(document)) return;
        string priorExecutingEvent = ExecutingEventId;
        ExecutingEventId = document.RoundId;
        try
        {
            if (oralTreaty != null) oralTreaty.TreatyExecutionState = "started";
            WorldDiplomacyDocumentExecutionApplication.ProcessAnalyzedDocument(
                _host.DocumentExecution(), this, document, intent, commitment, requiresResponse, tone, confidence, out var receipts);
            if (oralTreaty != null) oralTreaty.TreatyExecutionState = receipts.Any(x => !x.OutcomeKnown) ? "uncertain"
                : document.ChangedDiplomaticState ? "applied" : "not_applied";
        }
        catch { if (oralTreaty != null) oralTreaty.TreatyExecutionState = "uncertain"; throw; }
        finally { ExecutingEventId = priorExecutingEvent; }
        HandleAidPublication(document);
        InvalidateDialogueIndex();
        RegisterPlayerResponseWork(document);
    }

    public void CommitFailedJob(WorldDiplomacyJob job, string error)
    {
        if (IsSpeechReviewJob(job) || DiplomacyEventRules.IsEvent(ResolveRound(job?.RoundId)) || IsPrivacyPipelineJob(job))
        { SuspendDiplomacyJob(job, error); return; }
        if (IsOralDeclarationJob(job)) { DeferOralDeclarationJob(job, error); return; }
        WorldDiplomacyFailureApplication.Commit(
            job,
            error,
            Storage,
            _host.CompressionRetryMaximumHours(),
            _host.CompressionRetryInitialHours(),
            _host.CurrentHour,
            AbandonRejectedGeneration,
            BuildFallbackAnalysisJson,
            CommitAnalysis,
            _host.LogDiplomaticThreatFallbackAnalysisPublished,
            j => WorldDiplomacyDocumentFactRules.BuildFallbackRoundCompressionJson(
                Storage?.Documents, j?.CompressionDocumentIds, _host.FormatCampaignDate),
            CommitRoundCompression,
            RemoveJob,
            _host.Log);
        if (job?.Kind == "analyze" && ResolveDocument(job.DocumentId)?.AnalysisStatus == "analysis_execution_failed")
            _host.Notify(DiplomacyUiLocalization.Text("AF_DIP_UI_Orch_ProcessingIncomplete", "The diplomatic declaration has been published, but diplomatic processing did not complete fully; check the document outcome and current situation."));
    }

    public void CommitGeneratedDocument(WorldDiplomacyJob job, string raw)
    {
        if (!string.IsNullOrEmpty(job?.DocumentId) && ResolveDocument(job.DocumentId)?.IsReadyForPublication == true) return;
        // Audit the program-owned consequence together with its prose.
        if (!BindSilenceConsequence(job, ref raw)) return;
        if (DeferDraftForPrivacyCheck(job, raw)) return;
        if (IsOralDeclarationJob(job)) { CommitOralDeclaration(job, raw); return; }
        if (DeferIncompletePlayerLifecycle(job, raw)) return;
        if (DeferNewPlayerContact(job, raw)) return;
        WorldDiplomacyGeneratedCompletionApplication.Commit(
            job,
            raw,
            Storage,
            ResolveRound,
            ResolveDocument,
            id => _host.ResolveKingdomIdOrNull(id),
            id => _host.CanAiAuthorDocumentBlockReason(id),
            (WorldDiplomacyJob j, Newtonsoft.Json.Linq.JObject json, string authorId, string fallbackId,
                out string resolvedId, out string reason) =>
                TryGetGeneratedIntentLegalityViolation(j, json, authorId, fallbackId, out resolvedId, out reason),
            ParseAndValidatePeaceTerms,
            (doc, json, a, t, allow, relay) =>
                TryApplyGeneratedSemanticEnvelope(doc, json, a, t, allow, relay),
            (a, t, title, body, origin, player, response, exchange) =>
                CreateDocument(a, t, title, body, origin, player, response, exchange),
            _host.FormatCampaignDate,
            PruneInvalidOffers,
            AbandonRejectedGeneration,
            RejectGeneratedDraftBeforePublication,
            document => { job.DocumentId = document.DocumentId; AddDocument(document); },
            (document, intent, commitment, response, tone, confidence) =>
            {
                document.SilenceSourceDocumentId = job.SilenceSourceDocumentId;
                document.PlayerContactId = ContactIdentity(job);
                document.AuditedAgendaQuote = job.AuditedAgendaQuote;
                document.AuditedAgendaAnswers = new(job.AuditedAgendaAnswers);
                document.SilenceConsequence = job.SilenceConsequence;
                document.AnsweredPlayerDocumentIds = new List<string>(job.PlayerResponseSourceIds ?? new List<string>());
                var envelope = WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(raw);
                if (!WorldDiplomacyAnalysisApplication.TryReadResponseRequests(envelope, document.Body, _host.ResolveKingdomIdOrNull, out var requests))
                { SuppressInvalidDocumentBeforePropagation(document, "response_request_quote_invalid"); return; }
                document.ResponseRequests = requests; document.ResponseRequestsClassified = envelope["response_requests"] is Newtonsoft.Json.Linq.JArray;
                document.PlayerLifecycleClassified = IsEventJob(job) && HasExplicitPlayerLifecycle(envelope);
                if (!CaptureAidDecisions(document, envelope))
                { SuppressInvalidDocumentBeforePropagation(document, "aid_decision_contract_invalid"); return; }
                var participant = ResolveRound(job.RoundId)?.Participants.FirstOrDefault(x => x.KingdomId == document.AuthorKingdomId);
                bool temporaryRefusal = envelope?["temporary_refusal"]?.Type == JTokenType.Boolean && envelope["temporary_refusal"].Value<bool>();
                if (temporaryRefusal) document.RoundParticipation = "withdraw";
                if (document.PlayerLifecycleClassified && (document.RoundParticipation == "withdraw" || document.RoundStatus is "resolved" or "deadlocked"))
                {
                    document.RequiresResponse = response = false;
                    foreach (var action in document.Actions ?? new()) action.RequiresResponse = false;
                }
                if (!string.IsNullOrEmpty(job.SilenceSourceDocumentId))
                {
                    document.RoundParticipation = "withdraw";
                    document.RequiresResponse = response = false;
                    foreach (var action in document.Actions ?? new()) action.RequiresResponse = false;
                    // Close only after the fixed consequence has actually settled.
                }
                ProcessAnalyzedDocument(document, intent, commitment, response, tone, confidence);
                FinishSilenceConsequence(job, document);
                if (temporaryRefusal && document.IsReadyForPublication)
                {
                    if (participant != null) participant.RefuseRepeatedUntilDay = _host.CurrentDay() + 3;
                    RecordDisputeRefusal(ResolveRound(job.RoundId), document);
                }
                CommitSovereignPositionCorrection(job, envelope, document);
                CommitPlayerResponseCoverage(job, document);
            },
            _host.Log);
    }

    public bool TryGetGeneratedIntentLegalityViolation(
        WorldDiplomacyJob job,
        Newtonsoft.Json.Linq.JObject json,
        string authorId,
        string fallbackTargetId,
        out string generatedTargetId,
        out string reason)
    {
        if (!ValidatePlayerResponseCoverage(job, json))
        { generatedTargetId = fallbackTargetId; reason = "unanswered_player_declaration"; return true; }
        generatedTargetId = "";
        if (authorId == null)
        {
            reason = "diplomatic_actions_envelope_invalid";
            return true;
        }
        var round = ResolveRound(job?.RoundId);
        if (!ValidateEndingRecoveryDraft(job, json, out reason)) return true;
        if (json?["actions"] is JArray dependencyActions && !DiplomacyConditionalExecutionRules.Validate(dependencyActions,
            id => DialogueDocumentKnown(authorId, id) ? ResolveDocument(id) : null, out reason)) return true;
        if (job.MaterialCountryContractVersion >= 1 && json["response_requests"] is not JArray)
        { reason = "response_request_classification_missing"; return true; }
        if (!CanInitiatePlayerContact() && IsNewPlayerContact(job, json))
        { reason = "new_player_contact_interval_pending"; return true; }
        if (!IsLiveRound(round) || !DiplomacyEventRules.IsEvent(round)
            || (!round.Participants.Any(x => x.KingdomId == authorId && (x.State == "active"
                    || !string.IsNullOrEmpty(x.PendingParticipationSourceId) && x.PendingParticipationSourceId == job.SourceDocumentId))
                && !round.PlayerResponses.Any(x => x.KingdomId == authorId && x.Status == "pending"
                    && (x.SourceDocumentId == job.SourceDocumentId || job.PlayerResponseSourceIds.Contains(x.SourceDocumentId)))
                && !(HasBoundEndingJob(round, job, authorId) && json["actions"] is JArray endingActions
                    && endingActions.All(x => WorldDiplomacyEnvelopeJsonRules.ReadString(x as JObject, "target_kingdom_id", "target") == job.TargetKingdomId)))
            || !_host.HasIndependentAuthority(authorId) || !_host.CanAiAuthorParty(authorId, out _))
        { reason = "author_is_not_current_event_participant"; return true; }
        return WorldDiplomacyGenerationValidationRules.TryGetGeneratedIntentLegalityViolation(
            job, json,
            (single, isSingleAction) =>
            {
                bool failed = TryGetGeneratedSingleActionLegalityViolationInContext(
                    job, single, authorId, isSingleAction ? fallbackTargetId : null,
                    out string actionTargetId, out string actionReason);
                return (failed, actionTargetId ?? "", actionReason);
            },
            ResolveDocument,
            out generatedTargetId, out reason);
    }

    public bool TryGetGeneratedSingleActionLegalityViolation(
        WorldDiplomacyJob job,
        Newtonsoft.Json.Linq.JObject json,
        string authorId,
        string fallbackTargetId,
        out string generatedTargetId,
        out string reason)
        => TryGetGeneratedSingleActionLegalityViolationInContext(job, json, authorId, fallbackTargetId,
            out generatedTargetId, out reason);

    private bool TryGetGeneratedSingleActionLegalityViolationInContext(
        WorldDiplomacyJob job, Newtonsoft.Json.Linq.JObject json, string authorId, string fallbackTargetId,
        out string generatedTargetId, out string reason)
    {
        generatedTargetId = "";
        return WorldDiplomacyGenerationValidationRules.TryGetGeneratedSingleActionLegalityViolation(
            job,
            json,
            authorId ?? "",
            fallbackTargetId,
            id => _host.ResolveKingdomIdOrNull(id),
            id => _host.KingdomIsEliminated(id),
            id => _host.ResolveKingdomIdOrNull(id) != null && _host.HasIndependentAuthority(id),
            (aId, targetId) => authorId != null && _host.PartiesAtWar(authorId, targetId),
            ResolveRound,
            ResolveDocument,
            (round, targetId, intent) =>
            {
                bool ok = TryDeriveGeneratedDiplomaticStructure(
                    job, round, json, authorId, targetId, intent, out string structureReason);
                return (!ok, structureReason);
            },
            (intent, targetId) =>
            {
                bool failed = TryGetDiplomaticStateViolation(intent, authorId, targetId, out string stateReason);
                return (failed, stateReason);
            },
            (intent, targetId, claimedThreatDocumentId) =>
            {
                bool failed = TryGetDiplomaticThreatIntentViolation(
                    intent, authorId, targetId, claimedThreatDocumentId, out string threatReason);
                return (failed, threatReason);
            },
            (json2, targetId) => ParseAndValidatePeaceTerms(json2, authorId, targetId),
            (intent, visibleText, targetId) =>
            {
                bool failed = TryGetPublicPeaceTermsDisclosureViolation(
                    intent, visibleText, json, authorId, targetId, out string disclosureReason);
                return (failed, disclosureReason);
            },
            visibleText =>
            {
                bool failed = TryGetRealmIdentityViolation(authorId, visibleText, out string realmReason);
                return (failed, realmReason);
            },
            _host.Log,
            out generatedTargetId,
            out reason);
    }

    private bool TryDeriveGeneratedDiplomaticStructure(
        WorldDiplomacyJob job,
        WorldDiplomacyRound round,
        Newtonsoft.Json.Linq.JObject json,
        string authorId,
        string targetId,
        string intent,
        out string reason)
    {
        if (NormalizeIntent(intent) == "withdraw_offer")
        {
            var offers = (round?.PendingOffers ?? new List<WorldDiplomacyRoundOffer>()).Where(x => x != null && x.Status == "open"
                && x.ProposerKingdomId == authorId && x.TargetKingdomId == targetId
                && (string.IsNullOrWhiteSpace(ReadString(json, "responding_to_offer_document_id"))
                    || (x.SourceDocumentId == ReadString(json, "responding_to_offer_document_id")
                        && x.SourceActionId == ReadString(json, "responding_to_offer_action_id")))).Take(2).ToList();
            reason = offers.Count == 1 ? "" : "withdrawal_without_unique_owned_offer";
            if (offers.Count != 1) return false;
            json["responding_to_offer_document_id"] = offers[0].SourceDocumentId;
            json["responding_to_offer_action_id"] = offers[0].SourceActionId;
            return true;
        }
        bool valid = WorldDiplomacyRoundLifecycleRules.TryDeriveGeneratedDiplomaticStructure(
            job, round, json, authorId, targetId, intent,
            Storage?.DiplomaticThreats, ResolveDocument, out reason);
        string resolvedOffer = ReadString(json, "responding_to_offer_document_id");
        if (valid && !string.IsNullOrEmpty(ResponseIntentToProposalIntent(intent))
            && !DialogueDocumentKnown(authorId, resolvedOffer))
        { reason = "offer_source_not_known_to_author"; return false; }
        if (valid && IsFormalTreatyIntent(intent) && NormalizeIntent(intent).StartsWith("accept_"))
        {
            var terms = ResolveDialogueTerms(ResolveDocument(ReadString(json, "responding_to_offer_document_id")), ReadString(json, "responding_to_offer_action_id"));
            if (terms == null) { reason = "formal_acceptance_missing_exact_source_terms"; return false; }
            if (json["treaty_terms"] is JObject submitted)
            {
                if (ReadString(submitted, "receiving_kingdom_id") != terms.ReceivingKingdomId
                    || ReadString(submitted, "joining_kingdom_id") != terms.JoiningKingdomId
                    || submitted["daily_tribute"] != null && submitted["daily_tribute"].ToString() != "0"
                    || submitted["duration_days"] != null && submitted["duration_days"].ToString() != "0"
                    || !string.IsNullOrEmpty(ReadString(submitted, "cession_settlement_id")))
                { reason = "formal_acceptance_changes_source_terms"; return false; }
            }
            json["treaty_terms"] = new JObject { ["receiving_kingdom_id"] = terms.ReceivingKingdomId, ["joining_kingdom_id"] = terms.JoiningKingdomId };
        }
        return valid;
    }

    private bool TryGetPublicPeaceTermsDisclosureViolation(
        string intent, string visibleText, Newtonsoft.Json.Linq.JObject json,
        string authorId, string targetId, out string reason)
    {
        return WorldDiplomacyGenerationValidationRules.TryGetPublicPeaceTermsDisclosureViolation(
            intent, visibleText, json, authorId, targetId,
            _host.KingdomValidationIdentity, _host.SettlementValidationName, out reason);
    }

    private bool TryGetRealmIdentityViolation(string authorId, string visibleText, out string reason)
    {
        return WorldDiplomacyGenerationValidationRules.TryGetRealmIdentityViolation(
            _host.RealmRulerTitle(authorId), _host.RealmRulerDisplayName(authorId), visibleText, out reason);
    }

    public bool TryApplyGeneratedSemanticEnvelope(
        WorldDiplomacyDocument document,
        Newtonsoft.Json.Linq.JObject json,
        string authorId,
        string fallbackTargetId,
        bool allowUntargeted,
        bool relayTurn)
    {
        if (authorId == null) return false;
        return WorldDiplomacyGenerationValidationRules.TryApplyGeneratedSemanticEnvelope(
            document,
            json,
            authorId,
            fallbackTargetId,
            allowUntargeted,
            relayTurn,
            (actionDocument, single, actionFallbackTargetId, actionAllowUntargeted, actionRelayTurn) =>
                TryApplyGeneratedSingleActionSemanticEnvelope(
                    actionDocument, single, authorId, actionFallbackTargetId,
                    actionAllowUntargeted, actionRelayTurn),
            NormalizeKingdomIdList);
    }

    private bool TryApplyGeneratedSingleActionSemanticEnvelope(
        WorldDiplomacyDocument document,
        Newtonsoft.Json.Linq.JObject json,
        string authorId,
        string fallbackTargetId,
        bool allowUntargeted,
        bool relayTurn)
    {        document.TreatyTerms = ParseFormalTreatyTerms(json,
            WorldDiplomacyEnvelopeJsonRules.ReadString(json, "author_intent.intent", "intent", "diplomatic_intent"), authorId,
            _host.ResolveKingdomIdOrNull(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "target_kingdom_id", "primary_target_kingdom_id", "target")) ?? fallbackTargetId);

        return WorldDiplomacyGenerationValidationRules.TryApplyGeneratedSingleActionSemanticEnvelope(
            document,
            json,
            authorId,
            fallbackTargetId,
            allowUntargeted,
            id => _host.ResolveKingdomIdOrNull(id),
            id =>
            {
                string resolved = _host.ResolveKingdomIdOrNull(id);
                return resolved == null ? "" : _host.PartyNameOrEmpty(resolved);
            },
            (termsJson, termAuthorId, termTargetId) =>
                ParseAndValidatePeaceTerms(termsJson, termAuthorId, termTargetId),
            NormalizeKingdomIdList);
    }

    // ---------- round spine ----------

    public WorldDiplomacyRound EnsureActiveRound(string initiatorId, string targetId, bool isPlayerInsertion)
    {
        if (isPlayerInsertion) return CreateIndependentDialogueRound(initiatorId, targetId, "player_manual_declaration");
        InvalidateDialogueIndex();
        var opened = WorldDiplomacyRoundApplication.EnsureOpen(Storage, () =>
        {
            string roundInitiatorId = _host.ResolveRepresentativeId(initiatorId);
            string roundTargetId = _host.ResolveRepresentativeId(targetId);
            int day = _host.CurrentDay();
            int duration = _host.RoundTargetDurationDays();
            return new WorldDiplomacyRoundApplication.RoundOpening(_host.RelaySchemaVersion(), _host.NewId("diplomacy_round"),
                roundInitiatorId, roundTargetId, day, duration,
                _host.RoundHardDurationDays(duration), _host.CourtMaxDeliveryDays(), isPlayerInsertion);
        });
        if (string.IsNullOrEmpty(opened?.SchedulingOwner)) InitializeEventOwner(opened, false);
        return opened;
    }



    public void CloseActiveRound(string reason) => CloseRound(reason, Storage.ActiveRound);

    private void CloseActiveRound(string reason, WorldDiplomacyRound round) => CloseRound(reason, round);

    public void CloseRound(string reason, WorldDiplomacyRound round)
    {
        if (!IsLiveRound(round)) return;
        if (IsEnding(round) && (round.PendingOffers.Any(x => x.Status == "open")
            || Storage.DiplomaticThreats.Any(x => x.StageRoundId == round.RoundId && x.Status == "open")
            || Storage.Documents.Any(x => x.RoundId == round.RoundId && x.EventExecutionPending)
            || !string.IsNullOrEmpty(round.EventConvergence.TechnicalFailure))) return;
        if (round.PlayerResponses.Any(x => x.RequestKind == "invitation" && x.Status == "pending"
            && string.IsNullOrEmpty(x.AnswerDocumentId))) return;
        if (Storage.AidRequests.Any(x => x.RoundId == round.RoundId && x.Status == "accepted")) return;
        if (Storage.DialogueArrangements.Any(x => x.RoundId == round.RoundId && AnimusForge.DiplomacyDialogue.DiplomacyCommitmentRules.IsPending(x))) return;
        if (DiplomacyEventRules.IsEvent(round) && reason != "closed_disabled"
            && reason != "player_declaration_attached_to_shared_event" && reason != "player_negotiation_finished"
            && reason != "player_agenda_no_opportunity"
            && !(round.EventPhase == "closing" && reason == "event_" + round.EventOutcome)
            && !(reason == "dialogue_commitment_cancelled" && ResolveDocument(round.RootDocumentId)?.IsReadyForPublication != true)) return;
        if (EventHost != null && EventBoundaryComplete(round, "started") && !EventBoundaryComplete(round, "ended"))
        {
            round.EventCloseReason = reason;
            EnsureEventBoundary(round, "ended");
            return;
        }
        PreservePendingPlayerAnalysisForClosingRound(round, reason);
        WorldDiplomacyRoundApplication.Close(Storage, reason, _host.CurrentDay,
            SettleTradeAllianceOfferCooldownsForClosedRound, ScheduleNextNormalRoundAfter,
            CommitLocalRoundSummary, TryScheduleTokenCompression, _host.Log, round);
        InvalidateDialogueIndex();
        _lastCaseDispatch.Remove(round.RoundId);
        RecordAidCaseClosed(round);
        CarryUnansweredPlayerResponses(round);
    }



    public void CompleteExchange(string exchangeId, string reason)
    {
        WorldDiplomacyRoundApplication.CompleteExchange(Storage, exchangeId, reason, _host.CurrentDay,
            ScheduleNextNormalRoundAfter);
    }









    public IReadOnlyList<WorldDiplomacyThreat> Threats() => _host.Threats();

    public void IntegratePlayerDeclaration(WorldDiplomacyRound round, WorldDiplomacyDocument document)
    {
        WorldDiplomacyRoundApplication.IntegratePlayerDeclaration(Storage, round, document, _host.CurrentDay,
            id => _host.ResolveRepresentativeId(id),
            id => _host.IsPlayerParty(id), _host.Log);
    }





    public void CommitRoundCompression(WorldDiplomacyJob job, string raw)
    {
        WorldDiplomacyRoundCompressionApplication.Commit(Storage, job, raw,
            _host.CurrentDay(), _host.FormatCampaignDate);
    }



    public void HandleDisabledState()
    {
        if (_runtime.DisabledStateApplied) return;
        foreach (var round in GetLiveRounds()) AccrueTechnicalPause(round);
        if (Storage.EventsPausedAtHour < 0) Storage.EventsPausedAtHour = _host.CurrentCampaignHours();
        foreach (var round in GetLiveRounds().ToList()) RefreshEventRequests(round, "disabled");
        foreach (var review in Storage.Jobs.Where(IsSpeechReviewJob))
        { review.JobId = _host.NewId("oral_review_resume"); review.IsRunning = false; review.RequestAttempt = 0; }
        _runtime.DisabledStateApplied = true;
        _runtime.NativeQueueSanitized = false;
    }

    private void ResumeDiplomacyEvents()
    {
        if (Storage.EventsPausedAtHour < 0) return;
        DiplomacyCompressionBudget.Resume(Storage, false);
        _silenceDeadlines.Clear(); _silenceTicketIds.Clear(); _waitingByDocument.Clear();
        foreach (var round in GetLiveRounds().ToList())
        {
            foreach (var wait in round.PlayerNegotiationWaits)
            {
                if (wait.NotifiedHour >= 0 && wait.RequiresAnswer && string.IsNullOrEmpty(wait.AnswerDocumentId))
                    wait.NotifiedHour += Math.Max(0, _host.CurrentCampaignHours() - Math.Max(Storage.EventsPausedAtHour, wait.NotifiedHour));
                IndexPlayerWait(round, wait);
            }
            if (round.TechnicalHoldStartedHour >= 0) round.TechnicalHoldStartedHour = _host.CurrentCampaignHours();
            RefreshEventRequests(round, "enabled_revalidate");
        }
        Storage.EventsPausedAtHour = -1;
        _runtime.DisabledStateApplied = false;
    }

    public void RestoreSuspendedExchangeIfAny()
    {
        WorldDiplomacyRoundApplication.RestoreExchange(Storage, _host.CurrentDay);
    }

    public void TryApplyInitialNewGamePeace()
    {
        IWorldDiplomacyInitialPeacePort port = _host.InitialPeace();
        WorldDiplomacyInitialPeaceApplication.Apply(Storage, ref _runtime.InitialPeaceApplicationAttempted,
            ref _runtime.NativeQueueSanitized, ref port);
    }

    public void RefreshPolicyDiplomacySignals()
    {
        WorldDiplomacyPolicyRoundApplication.RefreshSignals(Storage, _host.CurrentDay, _host.ForeignPolicySignals,
            _host.PolicySignalRetentionDays(), _host.MaxPendingPolicySignals());
    }



    public void TrySchedulePolicyTriggeredRound()
    {
        TryScheduleEventConcept(policyOnly: true);
    }

    private bool ConsumeDailyAiDocumentBudget()
    {
        return WorldDiplomacyRoundLifecycleRules.TryConsumeAiDocumentBudget(ref _runtime.AiDocumentsStartedDay,
            ref _runtime.AiDocumentsStartedToday, _host.CurrentDay(), _host.MaxAiDocumentsStartedPerDay());
    }

    public void CompletePolicySignal(WorldDiplomacyPolicySignal signal, string reason)
    {
        WorldDiplomacyRoundLifecycleRules.CompletePolicySignal(Storage, signal, _host.MaxProcessedPolicySignalKeys(),
            reason, _host.Log);
    }

    public void TryScheduleNormalRound()
    {
        TryScheduleEventConcept(policyOnly: false);
    }

    public void NormalizeStorage(bool allowWorldValidation)
    {
        if (!_persistenceHealthy) return;
        InvalidateDialogueIndex();
        WorldDiplomacyStorage storage = Storage;
        WorldDiplomacyStorageShapeNormalizer.EnsureInitialized(storage);
        PreserveLegacyJobsBeforeNormalization();
        WorldDiplomacyStorageNormalizationApplication.Normalize(ref storage, allowWorldValidation,
            _host.StorageNormalizationSource(), _host.CanonicalHistoryMigrationSource(), this);
        if (!ReferenceEquals(storage, Storage)) _stateStore.Replace(storage);
        NormalizeConcurrentWork();
    }

    // Single persistence entry: the application owner sequences save/load,
    // state replacement, transient reset and post-load normalization. The host
    // supplies only the leaf store-read/store-write/error/log/reset adapters.
    public void SyncData(bool isSaving, bool isLoading,
        Func<WorldDiplomacyStorage> loadStorage, Func<string> loadError,
        Action<WorldDiplomacyStorage> saveStorage, Action<string> log, Action resetTransientRuntime)
    {
        if (isSaving)
        {
            if (!_persistenceHealthy) { saveStorage?.Invoke(_stateStore.Current); return; }
            NormalizeStorage(allowWorldValidation: false);
            var liveBudget = Storage.RequestBudget;
            try { Storage.RequestBudget = liveBudget.Snapshot(); saveStorage?.Invoke(_stateStore.Current); }
            finally { Storage.RequestBudget = liveBudget; }
            return;
        }
        if (!isLoading) return;
        // A failed current load cannot expose the prior campaign's in-memory
        // domain. The read owner separately retains exact rejected evidence.
        _persistenceHealthy = false;
        _stateStore.Replace(new WorldDiplomacyStorage());
        WorldDiplomacyStorage loaded = loadStorage?.Invoke();
        string error = loadError?.Invoke();
        _persistenceHealthy = string.IsNullOrWhiteSpace(error);
        _stateStore.Replace(_persistenceHealthy ? loaded : new WorldDiplomacyStorage());
        if (!_persistenceHealthy) log?.Invoke("load rejected; diplomacy quarantined: " + error);
        resetTransientRuntime?.Invoke();
        if (!_persistenceHealthy) return;
        NormalizeStorage(allowWorldValidation: false);
    }

    // ---------- canonical state owner lane (storage + runtime writes) ----------

    public void ResetStorageForNewGame(bool initialPeacePending)
    {
        _persistenceHealthy = true;
        _stateStore.Replace(new WorldDiplomacyStorage
        {
            HistoryMemorySchemaVersion = _host.TargetHistoryMemorySchemaVersion(),
            PromptContractVersion = _host.DiplomacyPromptContractVersion(),
            DiplomaticThreatStateSchemaVersion =
                WorldDiplomacyThreatStorageMigration.DiplomaticThreatStateSchemaVersion,
            OfferCooldownStateSchemaVersion =
                WorldDiplomacyOfferCooldownStorageNormalizer.CurrentSchemaVersion,
            ResultSettlementStateSchemaVersion = _host.ResultSettlementStateSchemaVersion(),
            DiplomacyNotificationStateSchemaVersion =
                WorldDiplomacyNotificationStateMigration.CurrentSchemaVersion,
            CanonicalHistory = new WorldDiplomacyCanonicalHistoryState(),
            DecisionArchitectureVersion = _host.DecisionArchitectureVersion(),
            PropagationReliabilityVersion = 1,
            InitialPeacePending = initialPeacePending
        });
    }

    public void EnsureScheduleInitialized()
    {
        WorldDiplomacyStorage storage = Storage;
        if (storage == null) return;
        int day = _host.CurrentDay();
        WorldDiplomacyLiveRoundRules.InitializeOrdinaryAdmission(storage);
        if (storage.NextNormalRoundDay <= 0) storage.NextNormalRoundDay = day;
        if (storage.LastCompressedYear < 0)
            storage.LastCompressedYear = WorldDiplomacyRoundLifecycleRules.ComputeInitialCompressedYear(day, _host.DaysPerYear());
    }

    public void ResetRuntimeState()
    {
        RequestLeases.Reset();
        _nextRequestLane = 0; _requestTurn = 0; _lastCaseDispatch.Clear();
        _playerMaintenanceHour = -1; _playerAuthorityDay = -1; _playerRoundCursor = 0;
        _observedAuthorityKingdom = _observedAuthorityRuler = "";
        _playerDirtyRounds.Clear(); _playerDirtyRoundIds.Clear();
        _playerAnalysisBacklog.Clear(); _pendingPlayerAnalysisIds.Clear();
        _refusalsByKingdom.Clear();
        _silenceDeadlines.Clear(); _silenceTicketIds.Clear(); _waitingByDocument.Clear();
        InvalidateDialogueIndex();
        RestoreDialogueMemoryRetryQueue();
        _personalMemoryRetryDocuments.Clear(); _personalMemoryRetryDocumentSet.Clear();
        _nextDiplomacyDispatchUtcTicks = 0; _diplomacyWorkNeedsReconcile = true;
        foreach (var doc in Storage.Documents) if (doc?.PendingPersonalMemoryRulers?.Count > 0) EnqueuePersonalMemoryRetry(doc.DocumentId);
        _runtime.DisabledStateApplied = false;
        _runtime.NativeQueueSanitized = false;
        _runtime.LastSchedulerDay = -1;
        _runtime.AiDocumentsStartedDay = -1;
        _runtime.AiDocumentsStartedToday = 0;
        _runtime.LastLlmCacheAffinityKey = "";
        _runtime.InitialPeaceApplicationAttempted = false;
        _runtime.CanonicalHistorySourceKeys.Clear();
        _runtime.DeferredCanonicalHistoryDocumentIds.Clear();
        _runtime.DeferredCanonicalHistoryDocumentIdSet.Clear();
        _runtime.DeferredCanonicalHistoryRetryAttempts.Clear();
        _runtime.DeferredCanonicalHistoryRetryAfterHour.Clear();
        _runtime.CanonicalHistoryRenderCacheKey = "";
        _runtime.CanonicalHistoryRenderCache = "";
        _runtime.LastCanonicalSourceSyncHour = int.MinValue;
        _runtime.LastObservedWorldWeeklyHistoryRevision = -1L;
        _runtime.CanonicalHistoryInitializedThisSession = false;
        WorldDiplomacyStorage storage = Storage;
        if (storage == null) return;
        WorldDiplomacyRoundLifecycleRules.RebuildOfferCooldownIndex(storage.OfferCooldowns, _runtime.OfferCooldownByKey);
        foreach (WorldDiplomacyDocument document in storage.Documents ?? new List<WorldDiplomacyDocument>())
        {
            if (WorldDiplomacyStructureRules.NeedsCanonicalHistoryRetry(document))
                WorldDiplomacyRoundLifecycleRules.EnqueueDeferredCanonicalHistoryRetry(
                    _runtime.DeferredCanonicalHistoryDocumentIdSet,
                    _runtime.DeferredCanonicalHistoryDocumentIds, document.DocumentId);
        }
        foreach (WorldDiplomacyJob job in storage.Jobs ?? new List<WorldDiplomacyJob>())
        {
            if (job == null) continue;
            job.IsRunning = false;
            // Older saves may contain a repair whose source coverage changed
            // after its messages were frozen. Rebuild once before resending.
            if (job.SemanticRepairAttempts > 0)
            {
                job.LlmMessages?.Clear();
                job.SemanticRepairAttempts = 0;
            }
        }
    }

    public void HandleWarDeclared(string firstId, string secondId)
    {
        if (string.IsNullOrWhiteSpace(firstId) || string.IsNullOrWhiteSpace(secondId)
            || string.Equals(firstId, secondId, StringComparison.OrdinalIgnoreCase)) return;
        EnsureAidIndex();
        foreach (var item in _pendingAid.Where(x => (x.CalledKingdomId == firstId && x.EnemyKingdomId == secondId)
            || (x.CalledKingdomId == secondId && x.EnemyKingdomId == firstId))) FulfillAid(item);
        WorldDiplomacyWarPressureRules.EnsureWarLedger(Storage?.ActiveWarLedgers, firstId, secondId, _host.CurrentDay());
        WorldDiplomacyRoundLifecycleRules.ResolveDiplomaticThreatsAfterWarStarted(
            Storage?.DiplomaticThreats, firstId, secondId, _host.CurrentDay(), _host.InternalActionDepthActive());
    }

    public void HandlePeaceMade(string firstId, string secondId)
    {
        if (string.IsNullOrWhiteSpace(firstId) || string.IsNullOrWhiteSpace(secondId)) return;
        WorldDiplomacyWarPressureRules.RemoveWarLedger(Storage?.ActiveWarLedgers, firstId, secondId);
        WorldDiplomacyWarPressureRules.ClearWarPressure(Storage?.WarPressure, firstId, secondId, _host.CurrentDay());
        WorldDiplomacyWarPressureRules.ClearWarPressure(Storage?.WarPressure, secondId, firstId, _host.CurrentDay());
    }

    public void HandleSettlementOwnerChanged(string settlementId, string settlementName,
        string oldKingdomId, string newKingdomId)
    {
        if (string.IsNullOrWhiteSpace(settlementId) || string.IsNullOrWhiteSpace(oldKingdomId)
            || string.IsNullOrWhiteSpace(newKingdomId)
            || string.Equals(oldKingdomId, newKingdomId, StringComparison.OrdinalIgnoreCase)) return;
        WorldDiplomacyStorage storage = Storage;
        WorldDiplomacyWarLedger ledger = WorldDiplomacyWarPressureRules.ResolveWarLedger(
            storage?.ActiveWarLedgers, oldKingdomId, newKingdomId);
        if (ledger == null && _host.IsAtWarByKingdomIds(oldKingdomId, newKingdomId))
        {
            ledger = WorldDiplomacyWarPressureRules.EnsureWarLedger(
                storage?.ActiveWarLedgers, oldKingdomId, newKingdomId, _host.CurrentDay());
        }
        if (ledger == null) return;
        ledger.SettlementChanges ??= new List<WorldDiplomacySettlementChange>();
        WorldDiplomacySettlementChange change = ledger.SettlementChanges.FirstOrDefault(x => x != null
            && string.Equals(x.SettlementId, settlementId, StringComparison.OrdinalIgnoreCase));
        if (change == null)
        {
            change = new WorldDiplomacySettlementChange
            {
                SettlementId = settlementId ?? "",
                SettlementName = settlementName ?? settlementId ?? "",
                OriginalKingdomId = oldKingdomId ?? ""
            };
            ledger.SettlementChanges.Add(change);
        }
        change.CurrentKingdomId = newKingdomId ?? "";
        change.LastChangedDay = _host.CurrentDay();
        change.CaptureCount++;
        RegisterPlayerAgendaCandidate("settlement:" + settlementId, oldKingdomId, newKingdomId);
        _host.InvalidateWarSituationCache(oldKingdomId, newKingdomId);
    }

    public void RecordBattleFact(WorldDiplomacyBattleFact fact)
    {
        if (fact == null) return;
        WorldDiplomacyStorage storage = Storage;
        if (storage == null) return;
        storage.RecentBattles ??= new List<WorldDiplomacyBattleFact>();
        if (storage.RecentBattles.Any(x => x != null
            && string.Equals(x.BattleId, fact.BattleId, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }
        storage.RecentBattles.Add(fact);
        foreach (string attacker in fact.AttackerKingdomIds ?? new())
            foreach (string defender in fact.DefenderKingdomIds ?? new())
                RegisterPlayerAgendaCandidate("battle:" + fact.BattleId, attacker, defender);
        TrimRecentBattleFacts();
    }

    public bool RecordNativeSignal(string sourceId, string targetId, string action, string reason)
    {
        WorldDiplomacyStorage storage = Storage;
        if (storage == null || string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(targetId)) return false;
        int value = _host.NativeSignalBaseValue(action);
        storage.NativeSignals ??= new List<NativeDiplomacySignal>();
        storage.NativeSignals.Add(new NativeDiplomacySignal
        {
            SignalId = _host.NewId("native_signal"),
            SourceKingdomId = sourceId,
            TargetKingdomId = targetId,
            Action = action,
            Reason = reason,
            Day = _host.CurrentDay(),
            Value = value
        });
        TrimNativeSignals();
        RegisterPlayerAgendaCandidate("native:" + action + ":" + sourceId + ":" + targetId, sourceId, targetId);
        if (action == "declare_war")
        {
            AddWarPressure(sourceId, targetId, value, DiplomacyMiscPromptTexts.Get("Orch_NativeWarDecisionSignalPrefix", "Native war decision signal: ") + reason, "");
        }
        _host.Log("captured native diplomacy decision action=" + action + " source=" + sourceId
            + " target=" + targetId + " value=" + value.ToString(CultureInfo.InvariantCulture));
        return true;
    }

    public void EnsureActiveWarLedgers()
    {
        WorldDiplomacyStorage storage = Storage;
        if (storage?.ActiveWarLedgers == null) return;
        storage.ActiveWarLedgers.RemoveAll(x => x == null
            || !_host.IsAtWarByKingdomIds(x.FirstKingdomId, x.SecondKingdomId));
        foreach ((string firstId, string secondId) in _host.ActiveWarKingdomPairs())
        {
            WorldDiplomacyWarPressureRules.EnsureWarLedger(storage.ActiveWarLedgers, firstId, secondId, _host.CurrentDay());
        }
    }

    public void TrimRecentBattleFacts()
    {
        WorldDiplomacyStorage storage = Storage;
        if (storage == null) return;
        storage.RecentBattles = WorldDiplomacyRoundLifecycleRules.TrimRecentBattleFacts(
            storage.RecentBattles, _host.CurrentDay(), _host.RecentBattleRetentionDays());
    }

    public void TrimNativeSignals()
    {
        WorldDiplomacyStorage storage = Storage;
        if (storage == null) return;
        storage.NativeSignals = WorldDiplomacyRoundLifecycleRules.TrimNativeSignals(
            storage.NativeSignals, _host.CurrentDay() - _host.DaysPerYear() * 2);
    }

    public void DecayWarPressure() =>
        WorldDiplomacyWarPressureRules.DecayWarPressure(Storage?.WarPressure, _host.CurrentDay());

    public void RemoveJob(string jobId)
    {
        _diplomacyWorkNeedsReconcile = true;
        WorldDiplomacyStorage storage = Storage;
        var retiring = storage?.Jobs?.FirstOrDefault(x => WorldDiplomacyRoundLifecycleRules.HasJobId(x, jobId));
        if (retiring?.Kind == "player_reply_routing") _pendingPlayerReplyRoutingIds.Remove(retiring.DocumentId);
        storage?.Jobs?.RemoveAll(x => WorldDiplomacyRoundLifecycleRules.HasJobId(x, jobId));
    }

    public void AddWarPressure(string sourceId, string targetId, int delta, string reason, string intent) =>
        WorldDiplomacyWarPressureRules.AddWarPressure(Storage?.WarPressure, sourceId, targetId, delta, reason,
            _host.CurrentDay(), intent);

    public WarPressureEntry FindWarPressure(string sourceId, string targetId) =>
        (Storage?.WarPressure ?? new List<WarPressureEntry>()).FirstOrDefault(x => x != null
            && string.Equals(x.SourceKingdomId, sourceId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.TargetKingdomId, targetId, StringComparison.OrdinalIgnoreCase));

    public void ClearLlmCacheAffinityKey() => _runtime.LastLlmCacheAffinityKey = "";

    // ---------- publication / propagation ----------

    public void StartDocumentPropagation(WorldDiplomacyDocument document, string authorId)
    {
        bool wasPublished = document?.PropagationStarted == true;
        try
        {
            WorldDiplomacyPublicationRoutingApplication.Start(_host.Publication(), this, document, authorId);
        }
        finally
        {
            // Publication can succeed before geography capture fails. Retain
            // its real memory receipt while the existing retry repairs delivery.
            if (document?.PropagationStarted == true)
            {
                if (!wasPublished && ResolveRound(document.RoundId) is WorldDiplomacyRound round)
                    round.ConversationRevision++;
                RecordDiplomaticDocumentPersonalMemories(document);
                InvalidateDialogueIndex();
            }
        }
    }

    public void ReconcileReachedCourts(WorldDiplomacyDocument document)
    {
        WorldDiplomacyPublicationRoutingApplication.ReconcileReachedCourts(_host.Publication(), this, document);
    }

    public void PublishPlayerAuthoredDocumentImmediately(WorldDiplomacyDocument document)
    {
        if (!_persistenceHealthy) return;
        WorldDiplomacyDocumentPublicationApplication.PublishPlayerImmediately(document, _host.Publication(), this);
        RememberPendingPlayerAnalysis(document);
        RegisterPublishedPlayerAnswer(document);
    }

    public void RetryDeferredDocumentPropagation()
    {
        string authorId = null;
        WorldDiplomacyPropagationApplication.RetryDeferred(
            Storage,
            id => { authorId = _host.ResolvePartyId(id); return authorId != null; },
            document => StartDocumentPropagation(document, authorId),
            _host.Log);
    }

    public void ProcessPropagationArrivals()
    {
        WorldDiplomacyPropagationApplication.ProcessDue(Storage, _host.CurrentDay(), _host.MaxPropagationArrivalsPerDay(),
            ResolveDocument,
            (arrival, document, day) =>
            {
                string receiverId = _host.ResolvePropagationReceiverId(arrival.KingdomId, arrival.SettlementId);
                if (receiverId == null) return;
                WorldDiplomacyPropagationApplication.ReceiveCourt(Storage, document, receiverId, day,
                    () => _host.IsPlayerAffiliatedParty(receiverId), () =>
                    {
                        arrival.CourtEffectPending = true;
                        ProcessCourtArrival(receiverId, document);
                    }, arrival.CourtEffectPending);
            },
            _host.ResolveSettlementId, _host.Log);
    }

    public void RecalculatePendingPropagationIfNeeded()
    {
        WorldDiplomacyStorage storage = Storage;
        if (storage == null) return;
        int courtDays = _host.CourtMaxDeliveryDays();
        int civilianDays = _host.CivilianSpreadDays();
        if (storage.LastAppliedCourtDeliveryDays == courtDays
            && storage.LastAppliedCivilianSpreadDays == civilianDays)
        {
            return;
        }
        WorldDiplomacyPropagationApplication.RecalculatePending(
            storage, _host.CurrentDay(), civilianDays, courtDays, _host.CaptureCourtTargets(),
            _host.CapturePropagationDistances);
        _host.Log("pending propagation recalculated courtDays=" + courtDays.ToString(CultureInfo.InvariantCulture)
            + " civilianDays=" + civilianDays.ToString(CultureInfo.InvariantCulture)
            + " arrivals=" + storage.PropagationArrivals.Count.ToString(CultureInfo.InvariantCulture));
    }



    public bool CanExternalDiplomacyFactJoinRound(WorldDiplomacyRound round, string initiatorId, string targetId)
    {
        if (!IsLiveRound(round) || !DiplomacyEventRules.IsEvent(round)) return false;
        return round.Participants.Any(x => x.KingdomId == initiatorId && x.State == "active")
            && round.Participants.Any(x => x.KingdomId == targetId && x.State == "active")
            || round.PendingOffers.Any(x => WorldDiplomacyRoundLifecycleRules.IsOpenOfferBetweenPair(
                x.Status, x.ProposerKingdomId, x.TargetKingdomId, initiatorId, targetId));
    }

    public void NotifyExternalDiplomacyResolved(string action, string initiatorId, string targetId, string reason)
    {
        if (initiatorId == null || targetId == null
            || string.Equals(initiatorId, targetId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        WorldDiplomacyDocumentPublicationApplication.NotifyExternalDiplomacyResolved(
            action, initiatorId, targetId, reason,
            _host.IsPlayerParty(initiatorId), Storage, _host.CurrentDay(),
            intent => _host.ExternalProposalTakenEffect(intent, initiatorId, targetId),
            domain => ClearBilateralOfferCooldowns(initiatorId, targetId, domain),
            (title, factBody, origin, playerAuthored) => CreateDocument(initiatorId, targetId, title, factBody, origin, playerAuthored, false, ""),
            normalized => WorldDiplomacyTextRules.BuildExternalFactBody(normalized,
                _host.PartyNameOrEmpty(initiatorId), _host.PartyNameOrEmpty(targetId), reason),
            candidate => CanExternalDiplomacyFactJoinRound(candidate, initiatorId, targetId),
            AddDocument,
            document => StartDocumentPropagation(document, initiatorId),
            AppendCanonicalDocumentEvents,
            ScheduleDeferredCanonicalHistoryRetry,
            HandleRoundDocumentProcessed,
            _host.Log);
    }

    public void FinalizePublishedDocumentAfterAnalysis(WorldDiplomacyDocument document, string authorId, string targetId,
        string normalizedIntent, bool recordNoActionDecision)
    {
        if (!_persistenceHealthy) return;
        SettleThreatDeclaration(document);
        WorldDiplomacyDocumentPublicationApplication.FinalizePublishedDocumentAfterAnalysis(
            document,
            authorId,
            targetId,
            normalizedIntent,
            recordNoActionDecision,
            _host.Threats(),

            StartDocumentPropagation,
            RecordDiplomacyWeeklyMaterial,
            ReconcileReachedCourts,
            AppendCanonicalDocumentEvents,
            TryAppendDiplomaticThreatHistoryResult,
            TryAppendDiplomaticThreatDomesticPenaltyHistoryResult,
            TryAppendDiplomaticThreatIssuerRewardHistoryResult,
            TryAppendDiplomaticThreatNonComplianceHistoryResult,
            ScheduleDeferredCanonicalHistoryRetry,
            HandleRoundDocumentProcessed,
            _host.Log);
    }

    public void RecoverPlayerCourtReceiptsFromKnowledge()
    {
        WorldDiplomacyPropagationApplication.RecoverPlayerCourtReceipts(Storage, _host.PlayerKingdomId(), _host.Log);
    }

    public List<string> NormalizeKingdomIdList(IEnumerable<string> values, string excludedId)
    {
        return (values ?? Enumerable.Empty<string>())
            .Select(v => _host.ResolvePartyId(v))
            .Where(x => x != null && !string.Equals(x, excludedId, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // ---------- threat / prestige / offer / immediate-effect lane ----------


    public void ProcessDiplomaticThreatDocument(
        WorldDiplomacyDocument document, string authorId, string targetId, bool recordTargetDecisions = true)
    {
        if (_host is IWorldDiplomacyThreatMechanicsHost host)
            WorldDiplomacyThreatBindingApplication.Process(Storage, document, authorId, targetId, false, host.ThreatBinding(), this);
        else if (WorldDiplomacyIntentVocabulary.NormalizeIntent(document?.Intent) == "comply_ultimatum")
            ResolveDiplomaticThreatCompliance(document, authorId, targetId);
    }


    public bool ResolveDiplomaticThreatCompliance(WorldDiplomacyDocument document, string compliantKingdomId, string issuerId)
    {
        return WorldDiplomacyThreatSettlementApplication.ResolveDiplomaticThreatCompliance(Storage,
            _host.ThreatSettlement(), this, document, compliantKingdomId, issuerId);
    }


    public bool TryApplyDiplomaticThreatPolicyConditionCancellation(WorldDiplomacyThreat threat)
    {
        return WorldDiplomacyThreatSettlementApplication.TryApplyDiplomaticThreatPolicyConditionCancellation(
            Storage, _host.ThreatSettlement(), this, threat);
    }


    public void RetryDiplomaticThreatComplianceConsequences()
    {
        WorldDiplomacyThreatSettlementApplication.RetryDiplomaticThreatComplianceConsequences(Storage, _host.ThreatSettlement(), this);
    }

    public void RetryDiplomaticThreatHistoryResults()
    {
        WorldDiplomacyThreatSettlementApplication.RetryDiplomaticThreatHistoryResults(Storage, _host.ThreatSettlement(), this);
    }

    public WorldDiplomacyOfferOutcome TrySettleRelayOffer(WorldDiplomacyDocument document)
    {
        if (HasConfirmedOralPlayerAgreement(document)) return ExecuteConfirmedOralPlayerAgreement(document);
        return WorldDiplomacyOfferApplication.Settle(document, _host.OfferAction(), this);
    }

    public WorldDiplomacyImmediateActionReceipt ExecuteImmediateIntent(string authorId, string targetId, string intent, WorldDiplomacyDocument document)
    {
        // Agreed oral treaties are executed exactly once by the offer effect
        // owner below, including treaties classified as immediate by vocabulary.
        if (HasConfirmedOralPlayerAgreement(document) && WorldDiplomacyIntentVocabulary.IsFormalTreatyIntent(intent))
            return new WorldDiplomacyImmediateActionReceipt(false, "");
        if (WorldDiplomacyIntentVocabulary.NormalizeIntent(intent) == "release_subject")
            return ExecutePlayerSubjectRelease(authorId, targetId, document);
        return WorldDiplomacyImmediateActionApplication.Execute(_host.ImmediateAction(), authorId, targetId, intent, document);
    }


    public bool TryGetDiplomaticStateViolation(string intent, string authorId, string targetId, out string reason)
    {
        if (WorldDiplomacyIntentVocabulary.NormalizeIntent(intent) == "release_subject")
        { reason = CanReleasePlayerSubject(authorId, targetId) ? "" : "subject_not_directly_owned_by_player_ruler"; return reason != ""; }
        IWorldDiplomacyWarAdmissionPort warAdmission = _host.WarAdmission(authorId, targetId);
        return WorldDiplomacyGenerationValidationRules.TryGetDiplomaticStateViolation(
            intent, authorId, targetId, Storage?.DiplomaticThreats,
            _host.PartiesAtWar(authorId, targetId),
            _host.PartiesAllied(authorId, targetId),
            _host.TradeAgreementExists(authorId, targetId),
            _host.AllianceKnown(), _host.TradeKnown(),
            enforcing =>
            {
                bool canDeclareWar = WorldDiplomacyWarAdmissionApplication.CanDeclareWar(
                    ref warAdmission, out string warReason, enforcing);
                return (canDeclareWar, warReason);
            },
            key => _host.OfferCooldownLastFailedRoundDay(key),
            _host.TradeAllianceFailedProposalCooldownDays(), _host.CurrentDay(), out reason);
    }

    public bool TryGetDiplomaticThreatIntentViolation(
        string intent, string authorId, string targetId, string claimedThreatDocumentId, out string reason)
    {
        IWorldDiplomacyWarAdmissionPort warAdmission = _host.WarAdmission(authorId, targetId);
        return WorldDiplomacyGenerationValidationRules.TryGetDiplomaticThreatIntentViolation(
            intent,
            authorId != null, targetId != null,
            authorId != null && targetId != null && string.Equals(authorId, targetId, StringComparison.OrdinalIgnoreCase),
            authorId, targetId, claimedThreatDocumentId,
            Storage?.DiplomaticThreats,
            _host.PartiesAtWar(authorId, targetId),
            () =>
            {
                bool canIssueWarThreat = WorldDiplomacyWarAdmissionApplication.CanIssueWarThreat(
                    ref warAdmission, out string warReason);
                return (canIssueWarThreat, warReason);
            },
            out reason);
    }

    public bool TryGetPlayerWorldStateIntentViolation(
        WorldDiplomacyDocument document, string intent, string commitment,
        string authorId, string targetId, out string reason)
    {
        if (WorldDiplomacyIntentVocabulary.NormalizeIntent(intent) == "release_subject")
        {
            if (!WorldDiplomacyOfferContractRules.CommitmentMatchesIntent(intent, commitment))
            { reason = "subject_release_commitment_mismatch"; return true; }
            return !ValidatePlayerSubjectRelease(document, authorId, targetId, out reason);
        }
        if (document != null && !HasConfirmedOralPlayerAgreement(document)
            && !string.IsNullOrEmpty(WorldDiplomacyIntentVocabulary.ResponseIntentToProposalIntent(intent))
            && !DialogueDocumentKnown(authorId, document.RespondingToOfferDocumentId))
        {
            reason = "player_offer_response_source_not_known";
            return true;
        }
        bool partiesEligible = document != null && authorId != null && targetId != null
            && !_host.PartiesShareIdentity(authorId, targetId)
            && !_host.IsEliminatedParty(authorId) && !_host.IsEliminatedParty(targetId);
        if (WorldDiplomacyGenerationValidationRules.TryGetPlayerWorldStateIntentViolation(
            document, intent, commitment, authorId, targetId, partiesEligible,
            normalizedIntent =>
            {
                bool violation = TryGetDiplomaticStateViolation(normalizedIntent, authorId, targetId, out string stateReason);
                return (violation, stateReason);
            },
            (normalizedIntent, claimedThreatDocumentId) =>
            {
                bool violation = TryGetDiplomaticThreatIntentViolation(normalizedIntent, authorId, targetId,
                    claimedThreatDocumentId, out string threatReason);
                return (violation, threatReason);
            },
            ResolveRound, ResolveDocument, out reason, HasConfirmedOralPlayerAgreement(document))) return true;
        // The executor projects each action's own terms before preflight and again
        // before its effect. New proposals have no existing source offer to resolve.
        if (WorldDiplomacyIntentVocabulary.NormalizeIntent(intent) == "propose_peace"
            && !WorldDiplomacyPeaceAdmissionApplication.TryValidatePeaceTerms(
                _host.PeaceAdmission(), document.PeaceTerms, authorId, targetId, out string peaceReason))
        {
            reason = "peace_terms_not_executable_without_changes:" + peaceReason;
            return true;
        }
        return false;
    }

    public WorldDiplomacyPeaceTerms ParseAndValidatePeaceTerms(
        Newtonsoft.Json.Linq.JObject json, string authorId, string targetId)
    {
        return WorldDiplomacyPeaceAdmissionApplication.ParseAndValidatePeaceTerms(
            _host.PeaceAdmission(), json, authorId, targetId);
    }

    // ---------- documents ----------

    public WorldDiplomacyDocument CreateDocument(
        string authorId,
        string targetId,
        string title,
        string body,
        string origin,
        bool isPlayerAuthored,
        bool isResponse,
        string exchangeId)
    {
        int day = _host.CurrentDay();
        var created = WorldDiplomacyDocumentApplication.Create(new WorldDiplomacyDocumentApplication.CreationSnapshot
        {
            DocumentId = _host.NewId("diplomacy_document"),
            ExchangeId = exchangeId,
            AuthorKingdomId = authorId,
            AuthorKingdomName = _host.PartyNameOrEmpty(authorId),
            AuthorRulerId = _host.PartyRulerId(authorId),
            AuthorRulerName = _host.PartyRulerName(authorId),
            TargetKingdomId = targetId,
            TargetKingdomName = targetId == null ? "" : _host.PartyNameOrEmpty(targetId),
            HasTarget = targetId != null,
            Title = title,
            Body = body,
            Origin = origin,
            Day = day,
            GameDate = _host.FormatCampaignDate(day),
            CreatedUtcTicks = DateTime.UtcNow.Ticks,
            IsPlayerAuthored = isPlayerAuthored,
            IsResponse = isResponse
        });
        if (isPlayerAuthored) created.SubjectReleaseTokens = SubjectReleaseHost?.CapturePlayerSubjectReleaseTokens(authorId);
        return created;
    }

    public void AddDocument(WorldDiplomacyDocument document)
    {
        InvalidateDialogueIndex();
        WorldDiplomacyDocumentApplication.Add(Storage, document, _host.MaxStoredDocuments(),
            _host.AdvanceWorldMessageTimelineRevision);
    }

    // ---------- prompts ----------

    public string BuildGenerationLegalActionSignature(WorldDiplomacyJob job)
    {
        if (IsOralDeclarationJob(job)) return BuildOralDeclarationSignature(job);
        if (job == null || !WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "generate")) return "";
        string authorId = _host.ResolvePartyId(job.AuthorKingdomId);
        if (authorId == null) return "missing_author";
        WorldDiplomacyRound round = ResolveRound(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(job.RoundId, job.ExchangeId));
        PruneInvalidOffers(round);
        WorldDiplomacyDocument responseSource = ResolveDocument(job.SourceDocumentId);
        return WorldDiplomacyRoundLifecycleRules.BuildGenerationLegalActionSignature(
            job, round, authorId, responseSource, Storage?.DiplomaticThreats,
            id => !_host.PartyResolved(id) || _host.IsEliminatedParty(id),
            id => BuildLegalDiplomaticDeclarationIntents(
                round, authorId, id, job.IsRelayTurn,
                job.ResultSettlementSlotId, job.IsExternalResponseOnly, responseSource));
    }





    public string BuildGenerationPromptForJob(
        string authorId, string targetId, WorldDiplomacyExchange exchange, bool isResponse,
        WorldDiplomacyDocument source, bool isReminder, string roundId, bool allowUntargeted,
        List<string> planCandidates, bool externalResponseOnly)
    {
        return WorldDiplomacyPromptComposer.BuildGenerationPrompt(_host.PromptWorld(), this, authorId, targetId, exchange,
            isResponse, source, isReminder, roundId, allowUntargeted, planCandidates, externalResponseOnly);
    }





    public string BuildAnalysisPrompt(WorldDiplomacyDocument document)
    {
        return WorldDiplomacyPromptComposer.BuildAnalysisPrompt(_host.PromptWorld(), this, document) + BuildPlayerRoundRoutingContext(document)
            + "\n" + DiplomacyPromptTexts.Get("ResponseRequestContract", "")
            + "\n" + DiplomacyPromptTexts.Get("PlayerConditionAnalysis", "Identify each condition separately. Accept only unambiguous, unconditional assent by the current author ruler to the exact source document/action. Conditional assent, hypotheticals, changed terms and partial acceptance of reciprocal terms are counteroffers. Preserve any explicit exchange prerequisite: emit dependency_action_ids on the affected actions, referencing action_1 etc. within this document or SOURCE_DOCUMENT_ID#SOURCE_ACTION_ID in an original offer. For independent terms omit dependencies. A peace proposal's cession and tribute terms form one indivisible original action; never accept only its favorable half. Modified prices or targets require a new proposal source, never reuse consent to a prior version. Output no invented acceptance or executed outcome. Add answered_npc_document_ids containing only incoming public document IDs whose particular matters this declaration actually answers; empty for unrelated speech. The drafting shortcut source is advisory and does not establish relevance. Never mark other countries' independent unanswered matters as answered merely because they share an event.");
    }

    public string BuildFallbackAnalysisJson(WorldDiplomacyJob job)
    {
        return WorldDiplomacyPromptComposer.BuildFallbackAnalysisJson(ResolveDocument(job?.DocumentId), job?.TargetKingdomId);
    }
    public void ProcessRoundLifecycle() => MaintainEvents();
    public void HandleRoundDocumentProcessed(WorldDiplomacyDocument document) => HandleEventPublication(document);
    public void ReconcileActiveDiplomacyAfterLoad()
    {
        RecoverPlayerDocumentRoutingAndRetireClosedJobs();
        RecoverRoundSchedulingAfterLoad();
        RecoverMisroutedPlayerOpenings();
        RecoverUnpublishedPlayerOpenings();
    }
    public void RetryDeferredRoundProgress()
    {
        var live = new HashSet<string>(GetLiveRounds().Select(x => x.RoundId), StringComparer.Ordinal);
        foreach (var document in Storage.Documents.Where(x => x != null && x.IsReadyForPublication
            && !x.RoundProgressHandled && live.Contains(x.RoundId)).Take(8).ToList())
        {
            try { HandleEventPublication(document); }
            catch (Exception ex) { _host.Log("event.publication_retry_failed document=" + document.DocumentId + " error=" + ex.Message); }
        }
    }
}

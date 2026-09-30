using System;
using System.Collections.Generic;
using AnimusForge;
using Newtonsoft.Json.Linq;

// Default leaf-host double for Application replays. Every member is virtual:
// tests override only the leaves the lane under test actually touches, and the
// Calls log records which host capabilities the orchestration consumed.
internal class FakeOrchestrationHost : IWorldDiplomacyOrchestrationHost
{
    internal readonly List<string> Calls = new();
    public int CurrentDayValue = 12;
    public virtual int CurrentDay() { Calls.Add("CurrentDay"); return CurrentDayValue; }
    public virtual int CurrentHour()
    {
        Calls.Add("CurrentHour");
        return default;
    }
    public virtual string NewId(string prefix)
    {
        Calls.Add("NewId");
        return "";
    }
    public virtual string FormatCampaignDate(int day)
    {
        Calls.Add("FormatCampaignDate");
        return "";
    }
    public virtual void Log(string message)
    {
        Calls.Add("Log");
    }
    public virtual int EstimateTokens(string text)
    {
        Calls.Add("EstimateTokens");
        return default;
    }
    public virtual bool WorldDiplomacyEnabled()
    {
        Calls.Add("WorldDiplomacyEnabled");
        return false;
    }
    public virtual bool LlmRequestRunning()
    {
        Calls.Add("LlmRequestRunning");
        return false;
    }
    public virtual void AdvanceWorldMessageTimelineRevision()
    {
        Calls.Add("AdvanceWorldMessageTimelineRevision");
    }
    public virtual int GenerationMaxTokens()
    {
        Calls.Add("GenerationMaxTokens");
        return default;
    }
    public virtual int AnalysisMaxTokens()
    {
        Calls.Add("AnalysisMaxTokens");
        return default;
    }
    public virtual int MaxPendingJobs()
    {
        Calls.Add("MaxPendingJobs");
        return default;
    }
    public virtual int MaxStoredDocuments()
    {
        Calls.Add("MaxStoredDocuments");
        return default;
    }
    public virtual int MaxAutomaticReplyDepth()
    {
        Calls.Add("MaxAutomaticReplyDepth");
        return default;
    }
    public virtual IReadOnlyList<WorldDiplomacyThreat> Threats()
    {
        Calls.Add("Threats");
        return (IReadOnlyList<WorldDiplomacyThreat>)new List<WorldDiplomacyThreat>();
    }
    public virtual int MaxAutomaticDocumentsPerRound()
    {
        Calls.Add("MaxAutomaticDocumentsPerRound");
        return default;
    }
    public virtual int MaxConsecutiveTechnicalGenerationFailuresPerRound()
    {
        Calls.Add("MaxConsecutiveTechnicalGenerationFailuresPerRound");
        return default;
    }
    public virtual int MaxPriorityPlayerResponsesPerDocument()
    {
        Calls.Add("MaxPriorityPlayerResponsesPerDocument");
        return default;
    }
    public virtual int MaxRelayParticipants()
    {
        Calls.Add("MaxRelayParticipants");
        return default;
    }
    public virtual int RoundParticipantLimit()
    {
        Calls.Add("RoundParticipantLimit");
        return default;
    }
    public virtual int RoundIntervalDays()
    {
        Calls.Add("RoundIntervalDays");
        return default;
    }
    public virtual int RoundTargetDurationDays()
    {
        Calls.Add("RoundTargetDurationDays");
        return default;
    }
    public virtual int RoundHardDurationDays(int targetDurationDays)
    {
        Calls.Add("RoundHardDurationDays");
        return default;
    }
    public virtual int CourtMaxDeliveryDays()
    {
        Calls.Add("CourtMaxDeliveryDays");
        return default;
    }
    public virtual int CivilianSpreadDays()
    {
        Calls.Add("CivilianSpreadDays");
        return default;
    }
    public virtual int RelayPassDurationDays()
    {
        Calls.Add("RelayPassDurationDays");
        return default;
    }
    public virtual int RelaySchemaVersion()
    {
        Calls.Add("RelaySchemaVersion");
        return default;
    }
    public virtual int RelayTargetDurationDays()
    {
        Calls.Add("RelayTargetDurationDays");
        return default;
    }
    public virtual int MaxAiDocumentsStartedPerDay()
    {
        Calls.Add("MaxAiDocumentsStartedPerDay");
        return default;
    }
    public virtual int MaxPropagationArrivalsPerDay()
    {
        Calls.Add("MaxPropagationArrivalsPerDay");
        return default;
    }
    public virtual int DiplomacyPromptContractVersion()
    {
        Calls.Add("DiplomacyPromptContractVersion");
        return default;
    }
    public virtual int ResultSettlementStateSchemaVersion()
    {
        Calls.Add("ResultSettlementStateSchemaVersion");
        return default;
    }
    public virtual int DecisionArchitectureVersion()
    {
        Calls.Add("DecisionArchitectureVersion");
        return default;
    }
    public virtual int MaxPendingPolicySignals()
    {
        Calls.Add("MaxPendingPolicySignals");
        return default;
    }
    public virtual int MaxProcessedPolicySignalKeys()
    {
        Calls.Add("MaxProcessedPolicySignalKeys");
        return default;
    }
    public virtual int MaxStoredRoundSummaries()
    {
        Calls.Add("MaxStoredRoundSummaries");
        return default;
    }
    public virtual int MaxStoredAnnualSummaries()
    {
        Calls.Add("MaxStoredAnnualSummaries");
        return default;
    }
    public virtual int MaxStoredCompressionSummaries()
    {
        Calls.Add("MaxStoredCompressionSummaries");
        return default;
    }
    public virtual int MaxDiplomaticActionsPerDocument()
    {
        Calls.Add("MaxDiplomaticActionsPerDocument");
        return default;
    }
    public virtual int ThreatComplianceIssuerRewardMax()
    {
        Calls.Add("ThreatComplianceIssuerRewardMax");
        return default;
    }
    public virtual int PolicySignalRetentionDays()
    {
        Calls.Add("PolicySignalRetentionDays");
        return default;
    }
    public virtual int TargetHistoryMemorySchemaVersion()
    {
        Calls.Add("TargetHistoryMemorySchemaVersion");
        return default;
    }
    public virtual long HistoryCompressionTriggerTokens()
    {
        Calls.Add("HistoryCompressionTriggerTokens");
        return default;
    }
    public virtual int HistoryCompressionTargetTokens()
    {
        Calls.Add("HistoryCompressionTargetTokens");
        return default;
    }
    public virtual int CompressionJobPriority()
    {
        Calls.Add("CompressionJobPriority");
        return default;
    }
    public virtual int CompressionOutputTokenReserve()
    {
        Calls.Add("CompressionOutputTokenReserve");
        return default;
    }
    public virtual int CompressionRetryMaximumHours()
    {
        Calls.Add("CompressionRetryMaximumHours");
        return default;
    }
    public virtual int CompressionRetryInitialHours()
    {
        Calls.Add("CompressionRetryInitialHours");
        return default;
    }
    public virtual int ConfiguredOutputTokenLimit()
    {
        Calls.Add("ConfiguredOutputTokenLimit");
        return default;
    }
    public virtual int TradeAllianceFailedProposalCooldownDays()
    {
        Calls.Add("TradeAllianceFailedProposalCooldownDays");
        return default;
    }
    public virtual int PolicyHistorySyncBatchSize()
    {
        Calls.Add("PolicyHistorySyncBatchSize");
        return default;
    }
    public virtual int PolicyHistoryForceSyncMaxBatches()
    {
        Calls.Add("PolicyHistoryForceSyncMaxBatches");
        return default;
    }
    public virtual  (int minimum, int maximum) DeclarationCharacterRange()
    {
        Calls.Add("DeclarationCharacterRange");
        return (0, int.MaxValue);
    }
    public virtual string CommonDiplomacyContract(WorldDiplomacyRound round)
    {
        Calls.Add("CommonDiplomacyContract");
        return "";
    }
    public virtual string CommonDiplomacySystemPrefix()
    {
        Calls.Add("CommonDiplomacySystemPrefix");
        return "";
    }
    public virtual string ResolvePartyId(string id)
    {
        Calls.Add("ResolvePartyId");
        return "";
    }
    public virtual bool PartyResolved(string id)
    {
        Calls.Add("PartyResolved");
        return false;
    }
    public virtual bool IsEliminatedParty(string id)
    {
        Calls.Add("IsEliminatedParty");
        return false;
    }
    public virtual bool HasIndependentAuthority(string id)
    {
        Calls.Add("HasIndependentAuthority");
        return false;
    }
    public virtual bool IsPlayerParty(string id)
    {
        Calls.Add("IsPlayerParty");
        return false;
    }
    public virtual bool IsPlayerAffiliatedParty(string id)
    {
        Calls.Add("IsPlayerAffiliatedParty");
        return false;
    }
    public virtual bool PartiesAtWar(string firstId, string secondId)
    {
        Calls.Add("PartiesAtWar");
        return false;
    }
    public virtual bool PartiesShareIdentity(string firstId, string secondId)
    {
        Calls.Add("PartiesShareIdentity");
        return false;
    }
    public virtual bool CanAiAuthorParty(string id, out string reason)
    {
        Calls.Add("CanAiAuthorParty");
        reason = "";
        return false;
    }
    public virtual string PartyNameOrEmpty(string id)
    {
        Calls.Add("PartyNameOrEmpty");
        return "";
    }
    public virtual string PartyRulerId(string id)
    {
        Calls.Add("PartyRulerId");
        return "";
    }
    public virtual string PartyRulerName(string id)
    {
        Calls.Add("PartyRulerName");
        return "";
    }
    public virtual string ResolveRepresentativeId(string kingdomId)
    {
        Calls.Add("ResolveRepresentativeId");
        return "";
    }
    public virtual string ResolveOriginSettlementId(string authorId)
    {
        Calls.Add("ResolveOriginSettlementId");
        return "";
    }
    public virtual float CourtDistance(string firstId, string secondId)
    {
        Calls.Add("CourtDistance");
        return default;
    }
    public virtual bool IsRepresentativeForAddressedVassal(string receiverId, WorldDiplomacyDocument document)
    {
        Calls.Add("IsRepresentativeForAddressedVassal");
        return false;
    }
    public virtual bool CampaignHasKingdoms()
    {
        Calls.Add("CampaignHasKingdoms");
        return false;
    }
    public virtual List<string> EligibleAiPartyIds()
    {
        Calls.Add("EligibleAiPartyIds");
        return new List<string>();
    }
    public virtual string ResolveEligibleDiplomacyKingdomId(string kingdomId)
    {
        Calls.Add("ResolveEligibleDiplomacyKingdomId");
        return "";
    }
    public virtual bool PartiesAllied(string firstId, string secondId)
    {
        Calls.Add("PartiesAllied");
        return false;
    }
    public virtual bool TradeAgreementExists(string firstId, string secondId)
    {
        Calls.Add("TradeAgreementExists");
        return false;
    }
    public virtual bool AllianceKnown()
    {
        Calls.Add("AllianceKnown");
        return false;
    }
    public virtual bool TradeKnown()
    {
        Calls.Add("TradeKnown");
        return false;
    }
    public virtual bool IsAtWarByKingdomIds(string firstId, string secondId)
    {
        Calls.Add("IsAtWarByKingdomIds");
        return false;
    }
    public virtual string ResolveKingdomNameOrEmpty(string kingdomId)
    {
        Calls.Add("ResolveKingdomNameOrEmpty");
        return "";
    }
    public virtual string ValidateOpenThreatWorldEligibility(WorldDiplomacyThreat threat)
    {
        Calls.Add("ValidateOpenThreatWorldEligibility");
        return "";
    }
    public virtual bool HasCompleteLegacyPropagationCoverage(WorldDiplomacyDocument document)
    {
        Calls.Add("HasCompleteLegacyPropagationCoverage");
        return false;
    }
    public virtual WorldDiplomacyPolicyRoundApplication.Parties ResolvePolicyParties(WorldDiplomacyPolicySignal signal)
    {
        Calls.Add("ResolvePolicyParties");
        return null;
    }
    public virtual string ResolvePropagationReceiverId(string kingdomId, string settlementId)
    {
        Calls.Add("ResolvePropagationReceiverId");
        return "";
    }
    public virtual int OfferCooldownLastFailedRoundDay(WorldDiplomacyOfferCooldownKey key)
    {
        Calls.Add("OfferCooldownLastFailedRoundDay");
        return default;
    }
    public virtual string BuildExternalFactBody(string action, string initiatorId, string targetId, string reason)
    {
        Calls.Add("BuildExternalFactBody");
        return "";
    }
    public virtual bool ExternalProposalTakenEffect(string intent, string initiatorId, string targetId)
    {
        Calls.Add("ExternalProposalTakenEffect");
        return false;
    }
    public virtual string NewThreatId()
    {
        Calls.Add("NewThreatId");
        return "";
    }
    public virtual List<WorldDiplomacyPropagationApplication.CourtTarget> CaptureCourtTargets()
    {
        Calls.Add("CaptureCourtTargets");
        return new List<WorldDiplomacyPropagationApplication.CourtTarget>();
    }
    public virtual WorldDiplomacyPropagationApplication.DistanceSnapshot CapturePropagationDistances(WorldDiplomacyDocument document)
    {
        Calls.Add("CapturePropagationDistances");
        return null;
    }
    public virtual void ShowPlayerCourtDelivery(string receiverName)
    {
        Calls.Add("ShowPlayerCourtDelivery");
    }
    public virtual void RemoveQueuedNativeDiplomacyDecisions()
    {
        Calls.Add("RemoveQueuedNativeDiplomacyDecisions");
    }
    public virtual List<(string firstId, string secondId)> ActiveWarKingdomPairs()
    {
        Calls.Add("ActiveWarKingdomPairs");
        return new List<(string firstId, string secondId)>();
    }
    public virtual void InvalidateWarSituationCache(string firstId, string secondId)
    {
        Calls.Add("InvalidateWarSituationCache");
    }
    public virtual bool InternalActionDepthActive()
    {
        Calls.Add("InternalActionDepthActive");
        return false;
    }
    public virtual int DaysPerYear()
    {
        Calls.Add("DaysPerYear");
        return 84;
    }
    public virtual int RecentBattleRetentionDays()
    {
        Calls.Add("RecentBattleRetentionDays");
        return 14;
    }
    public virtual int NativeSignalBaseValue(string action)
    {
        Calls.Add("NativeSignalBaseValue");
        return action == "declare_war" ? 24 : 42;
    }
    public virtual IReadOnlyList<WorldDiplomacyPolicySignalSnapshot> ForeignPolicySignals()
    {
        Calls.Add("ForeignPolicySignals");
        return (IReadOnlyList<WorldDiplomacyPolicySignalSnapshot>)new List<WorldDiplomacyPolicySignalSnapshot>();
    }
    public virtual string PublishedPolicyHistoryLedgerId()
    {
        Calls.Add("PublishedPolicyHistoryLedgerId");
        return "";
    }
    public virtual long PublishedPolicyHistoryRevision()
    {
        Calls.Add("PublishedPolicyHistoryRevision");
        return default;
    }
    public virtual long PublishedPolicyHistorySequence()
    {
        Calls.Add("PublishedPolicyHistorySequence");
        return default;
    }
    public virtual IReadOnlyList<PublishedPolicyArtifactLedgerEntry> PublishedPolicyHistoryArtifacts(long cursor, int batchSize)
    {
        Calls.Add("PublishedPolicyHistoryArtifacts");
        return (IReadOnlyList<PublishedPolicyArtifactLedgerEntry>)new List<PublishedPolicyArtifactLedgerEntry>();
    }
    public virtual bool TryAcknowledgePublishedPolicyHistoryThrough(long throughSequence)
    {
        Calls.Add("TryAcknowledgePublishedPolicyHistoryThrough");
        return false;
    }
    public virtual List<PublishedPolicyArtifactLedgerEntry> ReadPublishedPolicyArtifacts()
    {
        Calls.Add("ReadPublishedPolicyArtifacts");
        return new List<PublishedPolicyArtifactLedgerEntry>();
    }
    public virtual long PublishedWorldWeeklyHistoryRevision()
    {
        Calls.Add("PublishedWorldWeeklyHistoryRevision");
        return default;
    }
    public virtual void RecordWorldDiplomacyWeeklyMaterialExternal(string stableKey, string title, string text, string authorKingdomId, string authorRulerId, string relatedKingdomId, bool isWorldLevel, int day, string gameDate)
    {
        Calls.Add("RecordWorldDiplomacyWeeklyMaterialExternal");
    }
    public virtual bool TryBuildKingdomStrategicProfilePrompt(string kingdomId, string marker, out string prompt)
    {
        Calls.Add("TryBuildKingdomStrategicProfilePrompt");
        prompt = "";
        return false;
    }
    public virtual void LogKingdomStrategicProfileInjection(WorldDiplomacyJob job, string profilePrompt)
    {
        Calls.Add("LogKingdomStrategicProfileInjection");
    }
    public virtual IWorldDiplomacyActionSelectionPort ActionSelection()
    {
        Calls.Add("ActionSelection");
        return null;
    }
    public virtual IWorldDiplomacyNoActionPort NoActionPort(string authorId, string targetId)
    {
        Calls.Add("NoActionPort");
        return null;
    }
    public virtual IWorldDiplomacyPeaceAdmissionPort PeaceAdmission()
    {
        Calls.Add("PeaceAdmission");
        return null;
    }
    public virtual IWorldDiplomacyPromptWorld PromptWorld()
    {
        Calls.Add("PromptWorld");
        return null;
    }
    public virtual IWorldDiplomacyDraftRepairWorld DraftRepairWorld()
    {
        Calls.Add("DraftRepairWorld");
        return null;
    }
    public virtual IWorldDiplomacyDocumentExecutionPort DocumentExecution()
    {
        Calls.Add("DocumentExecution");
        return null;
    }
    public virtual IWorldDiplomacyPublicationPort Publication()
    {
        Calls.Add("Publication");
        return null;
    }
    public virtual IWorldDiplomacyJobPreparationPort JobPreparation()
    {
        Calls.Add("JobPreparation");
        return null;
    }
    public virtual IWorldDiplomacyHistoryCapturePort HistoryCapture()
    {
        Calls.Add("HistoryCapture");
        return null;
    }
    public virtual IWorldDiplomacyInitialPeacePort InitialPeace()
    {
        Calls.Add("InitialPeace");
        return null;
    }
    public virtual IWorldDiplomacyPrestigePort Prestige()
    {
        Calls.Add("Prestige");
        return null;
    }
    public virtual IWorldDiplomacyThreatSettlementPort ThreatSettlement()
    {
        Calls.Add("ThreatSettlement");
        return null;
    }
    public virtual IWorldDiplomacyThreatBindingPort ThreatBinding()
    {
        Calls.Add("ThreatBinding");
        return null;
    }
    public virtual IWorldDiplomacyAnalysisPort AnalysisPort()
    {
        Calls.Add("AnalysisPort");
        return null;
    }
    public virtual IWorldDiplomacyOfferActionPort OfferAction()
    {
        Calls.Add("OfferAction");
        return null;
    }
    public virtual IWorldDiplomacyImmediateActionPort ImmediateAction()
    {
        Calls.Add("ImmediateAction");
        return null;
    }
    public virtual IWorldDiplomacyWarAdmissionPort WarAdmission(string firstId, string secondId)
    {
        Calls.Add("WarAdmission");
        return null;
    }
    public virtual IWorldDiplomacyStorageNormalizationSource StorageNormalizationSource()
    {
        Calls.Add("StorageNormalizationSource");
        return null;
    }
    public virtual IWorldDiplomacyCanonicalHistoryMigrationSource CanonicalHistoryMigrationSource()
    {
        Calls.Add("CanonicalHistoryMigrationSource");
        return null;
    }
    public virtual IWorldDiplomacyLlmDispatchSource LlmDispatchSource()
    {
        Calls.Add("LlmDispatchSource");
        return null;
    }
    public virtual IWorldDiplomacyCompletionSource CompletionSource()
    {
        Calls.Add("CompletionSource");
        return null;
    }
    public virtual void PollNotifications()
    {
        Calls.Add("PollNotifications");
    }
    public virtual string ResolveSettlementPartyId(string settlementId)
    {
        Calls.Add("ResolveSettlementPartyId");
        return "";
    }
    public virtual string PartyNameIncludingEliminated(string id)
    {
        Calls.Add("PartyNameIncludingEliminated");
        return "";
    }
    public virtual string PlayerKingdomId()
    {
        Calls.Add("PlayerKingdomId");
        return "";
    }
    public virtual string ResolveKingdomIdOrNull(string id)
    {
        Calls.Add("ResolveKingdomIdOrNull");
        return "";
    }
    public virtual bool KingdomIsEliminated(string id)
    {
        Calls.Add("KingdomIsEliminated");
        return false;
    }
    public virtual  (string id, string name) KingdomValidationIdentity(string id)
    {
        Calls.Add("KingdomValidationIdentity");
        return (id, id);
    }
    public virtual string SettlementValidationName(string settlementId)
    {
        Calls.Add("SettlementValidationName");
        return "";
    }
    public virtual string RealmRulerDisplayName(string kingdomId)
    {
        Calls.Add("RealmRulerDisplayName");
        return "";
    }
    public virtual int GetRoundParticipantLimit()
    {
        Calls.Add("GetRoundParticipantLimit");
        return default;
    }
    public virtual string CanAiAuthorDocumentBlockReason(string id)
    {
        Calls.Add("CanAiAuthorDocumentBlockReason");
        return "";
    }
    public virtual void LogDiplomaticThreatFallbackAnalysisPublished(WorldDiplomacyJob job)
    {
        Calls.Add("LogDiplomaticThreatFallbackAnalysisPublished");
    }
    public virtual void Notify(string message)
    {
        Calls.Add("Notify");
    }
}
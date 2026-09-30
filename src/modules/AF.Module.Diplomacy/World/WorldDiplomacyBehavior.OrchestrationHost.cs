using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Domain;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    // Leaf-only host adapter for WorldDiplomacyOrchestration. Every member is a
    // synchronous identity/snapshot/settings/effect capability; no member calls
    // back into an Application or into the orchestration itself.
    private sealed class OrchestrationHost : IWorldDiplomacyOrchestrationHost
    {
        private readonly WorldDiplomacyBehavior _owner;
        internal OrchestrationHost(WorldDiplomacyBehavior owner) => _owner = owner;

        public WorldDiplomacyStorage Storage => _owner._storage;
        public int CurrentDay() => WorldDiplomacyBehavior.CurrentDay();
        public int CurrentHour() => WorldDiplomacyBehavior.CurrentHour();
        public string NewId(string prefix) => WorldDiplomacyBehavior.NewId(prefix);
        public string FormatCampaignDate(int day) => WorldDiplomacyBehavior.FormatCampaignDate(day);
        public void Log(string message) => WorldDiplomacyBehavior.Log(message);
        public void Notify(string message) => TaleWorlds.Library.InformationManager.DisplayMessage(new TaleWorlds.Library.InformationMessage(message));
        public int EstimateTokens(string text) => Logger.EstimateTokens(text);
        public bool WorldDiplomacyEnabled() => WorldDiplomacyBehavior.IsWorldDiplomacyEnabled();
        public bool LlmRequestRunning() => _owner._llmRequestLease.IsRunning;
        public void AdvanceWorldMessageTimelineRevision() => _owner.AdvanceWorldMessageTimelineRevision();

        // Settings scalars.
        public int GenerationMaxTokens() => WorldDiplomacyBehavior.GenerationMaxTokens;
        public int AnalysisMaxTokens() => WorldDiplomacyBehavior.AnalysisMaxTokens;
        public int MaxPendingJobs() => WorldDiplomacyBehavior.MaxPendingJobs;
        public int MaxStoredDocuments() => WorldDiplomacyBehavior.MaxStoredDocuments;
        public int MaxAutomaticReplyDepth() => WorldDiplomacyBehavior.MaxAutomaticReplyDepth;
        public int MaxAutomaticDocumentsPerRound() => WorldDiplomacyBehavior.MaxAutomaticDocumentsPerRound;
        public int MaxConsecutiveTechnicalGenerationFailuresPerRound() => WorldDiplomacyBehavior.MaxConsecutiveTechnicalGenerationFailuresPerRound;
        public int MaxPriorityPlayerResponsesPerDocument() => WorldDiplomacyBehavior.MaxPriorityPlayerResponsesPerDocument;
        public int MaxRelayParticipants() => WorldDiplomacyBehavior.MaxRelayParticipants;
        public int RoundParticipantLimit() => WorldDiplomacyBehavior.GetRoundParticipantLimit();
        public int RoundIntervalDays() => WorldDiplomacyBehavior.GetRoundIntervalDays();
        public int RoundTargetDurationDays() => WorldDiplomacyBehavior.RelayTargetDurationDays;
        public int RoundHardDurationDays(int targetDurationDays) => WorldDiplomacyBehavior.GetRoundHardDurationDays(targetDurationDays);
        public int CourtMaxDeliveryDays() => WorldDiplomacyBehavior.GetCourtMaxDeliveryDays();
        public int CivilianSpreadDays() => WorldDiplomacyBehavior.GetCivilianSpreadDays();
        public int RelayPassDurationDays() => WorldDiplomacyBehavior.RelayPassDurationDays;
        public int RelaySchemaVersion() => WorldDiplomacyBehavior.RelaySchemaVersion;
        public int RelayTargetDurationDays() => WorldDiplomacyBehavior.RelayTargetDurationDays;
        public int MaxAiDocumentsStartedPerDay() => WorldDiplomacyBehavior.MaxAiDocumentsStartedPerDay;
        public int MaxPropagationArrivalsPerDay() => WorldDiplomacyBehavior.MaxPropagationArrivalsPerDay;
        public int DiplomacyPromptContractVersion() => WorldDiplomacyBehavior.DiplomacyPromptContractVersion;
        public int ResultSettlementStateSchemaVersion() => WorldDiplomacyBehavior.ResultSettlementStateSchemaVersion;
        public int DecisionArchitectureVersion() => WorldDiplomacyBehavior.DecisionArchitectureVersion;
        public int MaxPendingPolicySignals() => WorldDiplomacyBehavior.MaxPendingPolicySignals;
        public int MaxProcessedPolicySignalKeys() => WorldDiplomacyBehavior.MaxProcessedPolicySignalKeys;
        public int MaxStoredRoundSummaries() => WorldDiplomacyBehavior.MaxStoredRoundSummaries;
        public int MaxStoredAnnualSummaries() => WorldDiplomacyBehavior.MaxStoredAnnualSummaries;
        public int MaxStoredCompressionSummaries() => WorldDiplomacyBehavior.MaxStoredCompressionSummaries;
        public int MaxDiplomaticActionsPerDocument() => WorldDiplomacyBehavior.MaxDiplomaticActionsPerDocument;
        public int ThreatComplianceIssuerRewardMax() => DuelSettings.WorldDiplomacyThreatComplianceIssuerRelationRewardMax;
        public int PolicySignalRetentionDays() => WorldDiplomacyBehavior.PolicySignalRetentionDays;
        public int TargetHistoryMemorySchemaVersion() => WorldDiplomacyBehavior.HistoryMemorySchemaVersion;
        public long HistoryCompressionTriggerTokens() => WorldDiplomacyBehavior.GetHistoryCompressionTriggerTokens();
        public int HistoryCompressionTargetTokens() => WorldDiplomacyBehavior.GetHistoryCompressionTargetTokens();
        public int CompressionJobPriority() => WorldDiplomacyBehavior.CompressionJobPriority;
        public int CompressionOutputTokenReserve() => WorldDiplomacyBehavior.CompressionOutputTokenReserve;
        public int CompressionRetryMaximumHours() => WorldDiplomacyBehavior.CompressionRetryMaximumHours;
        public int CompressionRetryInitialHours() => WorldDiplomacyBehavior.CompressionRetryInitialHours;
        public int ConfiguredOutputTokenLimit() => WorldDiplomacyLlmClient.GetConfiguredOutputTokenLimit();
        public int TradeAllianceFailedProposalCooldownDays() => WorldDiplomacyBehavior.GetTradeAllianceFailedProposalCooldownDays();
        public int PolicyHistorySyncBatchSize() => WorldDiplomacyBehavior.PolicyHistorySyncBatchSize;
        public int PolicyHistoryForceSyncMaxBatches() => WorldDiplomacyBehavior.PolicyHistoryForceSyncMaxBatches;
        public (int minimum, int maximum) DeclarationCharacterRange()
        {
            WorldDiplomacyBehavior.GetDiplomaticDeclarationCharacterRange(out int min, out int max);
            return (min, max);
        }
        public string CommonDiplomacyContract(WorldDiplomacyRound round) => _owner.GetCommonDiplomacyContract(round);
        public string CommonDiplomacySystemPrefix() => WorldDiplomacyBehavior.BuildCommonDiplomacySystemPrefix();

        // Identity/fact leaf queries resolved from live campaign state.
        public string ResolvePartyId(string id) => WorldDiplomacyBehavior.ResolveKingdom(id)?.StringId;
        public bool PartyResolved(string id) => WorldDiplomacyBehavior.ResolveKingdom(id) != null;
        public bool IsEliminatedParty(string id) => WorldDiplomacyBehavior.ResolveKingdomIncludingEliminated(id)?.IsEliminated == true;
        public bool HasIndependentAuthority(string id) => WorldDiplomacyBehavior.HasIndependentWorldDiplomacyAuthority(WorldDiplomacyBehavior.ResolveKingdom(id));
        public bool IsPlayerParty(string id) => WorldDiplomacyBehavior.IsPlayerKingdom(WorldDiplomacyBehavior.ResolveKingdom(id));
        public bool IsPlayerAffiliatedParty(string id) => WorldDiplomacyBehavior.IsPlayerAffiliatedKingdom(WorldDiplomacyBehavior.ResolveKingdom(id));
        public bool PartiesAtWar(string firstId, string secondId) => FactionManager.IsAtWarAgainstFaction(WorldDiplomacyBehavior.ResolveKingdom(firstId), WorldDiplomacyBehavior.ResolveKingdom(secondId));
        public bool PartiesShareIdentity(string firstId, string secondId) => WorldDiplomacyBehavior.ResolveKingdom(firstId) == WorldDiplomacyBehavior.ResolveKingdom(secondId);
        public bool CanAiAuthorParty(string id, out string reason) => WorldDiplomacyBehavior.CanAiAuthorDiplomaticDocument(WorldDiplomacyBehavior.ResolveKingdom(id), out reason);
        public string PartyNameOrEmpty(string id) => WorldDiplomacyBehavior.KingdomName(WorldDiplomacyBehavior.ResolveKingdom(id));
        public string PartyRulerId(string id)
        {
            Kingdom kingdom = WorldDiplomacyBehavior.ResolveKingdom(id);
            return (kingdom?.Leader ?? kingdom?.RulingClan?.Leader)?.StringId;
        }
        public string PartyRulerName(string id) => WorldDiplomacyBehavior.RulerName(WorldDiplomacyBehavior.ResolveKingdom(id));
        public string ResolveRepresentativeId(string kingdomId) => WorldDiplomacyBehavior.ResolveWorldDiplomacyRepresentative(WorldDiplomacyBehavior.ResolveKingdom(kingdomId))?.StringId;
        public string ResolveOriginSettlementId(string authorId) => _owner.ResolveCourtSettlement(WorldDiplomacyBehavior.ResolveKingdom(authorId))?.StringId;
        public float CourtDistance(string firstId, string secondId)
        {
            Settlement a = _owner.ResolveCourtSettlement(WorldDiplomacyBehavior.ResolveKingdom(firstId));
            Settlement b = _owner.ResolveCourtSettlement(WorldDiplomacyBehavior.ResolveKingdom(secondId));
            return a == null || b == null ? 0f : a.GatePosition.Distance(b.GatePosition);
        }
        public bool IsRepresentativeForAddressedVassal(string receiverId, WorldDiplomacyDocument document)
            => _owner.IsDiplomaticRepresentativeForAddressedVassal(WorldDiplomacyBehavior.ResolveKingdom(receiverId), document);
        public bool CampaignHasKingdoms() => Campaign.Current != null && Kingdom.All.Any();
        public List<string> EligibleAiPartyIds() => _owner.GetEligibleAiKingdoms().Select(x => x?.StringId).Where(x => x != null).ToList();
        public string ResolveEligibleDiplomacyKingdomId(string kingdomId) => _owner.ResolveEligibleDiplomacyKingdomId(kingdomId);
        public bool PartiesAllied(string firstId, string secondId) =>
            Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>()
                ?.IsAllyWithKingdom(WorldDiplomacyBehavior.ResolveKingdom(firstId), WorldDiplomacyBehavior.ResolveKingdom(secondId)) == true;
        public bool TradeAgreementExists(string firstId, string secondId)
        {
            ITradeAgreementsCampaignBehavior trade = Campaign.Current?.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
            return trade != null && BannerlordApiCompat.HasTradeAgreement(
                trade, WorldDiplomacyBehavior.ResolveKingdom(firstId), WorldDiplomacyBehavior.ResolveKingdom(secondId));
        }
        public bool AllianceKnown() => Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>() != null;
        public bool TradeKnown() => Campaign.Current?.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>() != null;
        public bool IsAtWarByKingdomIds(string firstId, string secondId) => _owner.IsAtWarByKingdomIds(firstId, secondId);
        public string ResolveKingdomNameOrEmpty(string kingdomId) => WorldDiplomacyBehavior.ResolveKingdomNameOrEmpty(kingdomId);
        public string ValidateOpenThreatWorldEligibility(WorldDiplomacyThreat threat) =>
            _owner.ValidateOpenThreatWorldEligibility(threat,
                Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>());
        public bool HasCompleteLegacyPropagationCoverage(WorldDiplomacyDocument document) => _owner.HasCompleteLegacyPropagationCoverage(document);
        public WorldDiplomacyPolicyRoundApplication.Parties ResolvePolicyParties(WorldDiplomacyPolicySignal signal)
        {
            Kingdom issuer = WorldDiplomacyBehavior.ResolveKingdom(signal?.IssuerKingdomId);
            Kingdom affected = WorldDiplomacyBehavior.ResolveKingdom(signal?.TargetKingdomId);
            bool valid = issuer != null && affected != null && issuer != affected && !issuer.IsEliminated && !affected.IsEliminated;
            Kingdom issuerRepresentative = valid ? WorldDiplomacyBehavior.ResolveWorldDiplomacyRepresentative(issuer) : null;
            Kingdom affectedRepresentative = valid ? WorldDiplomacyBehavior.ResolveWorldDiplomacyRepresentative(affected) : null;
            return new WorldDiplomacyPolicyRoundApplication.Parties(valid, issuerRepresentative?.StringId,
                affectedRepresentative?.StringId,
                affectedRepresentative != null && WorldDiplomacyBehavior.IsPlayerKingdom(affectedRepresentative));
        }
        public void AttachPolicySignalToRound(WorldDiplomacyRound round, WorldDiplomacyPolicySignal signal) =>
            _owner.AttachPolicySignalToRound(round, signal,
                WorldDiplomacyBehavior.ResolveKingdom(signal?.IssuerKingdomId),
                WorldDiplomacyBehavior.ResolveKingdom(signal?.TargetKingdomId));
        public string ResolvePropagationReceiverId(string kingdomId, string settlementId) =>
            (WorldDiplomacyBehavior.ResolveKingdom(kingdomId)
                ?? WorldDiplomacyBehavior.ResolveSettlementById(settlementId)?.OwnerClan?.Kingdom)?.StringId;
        public int OfferCooldownLastFailedRoundDay(WorldDiplomacyOfferCooldownKey key) => _owner.GetOfferCooldownLastFailedRoundDay(key);
        public string BuildExternalFactBody(string action, string initiatorId, string targetId, string reason) =>
            WorldDiplomacyBehavior.BuildExternalFactBody(action, WorldDiplomacyBehavior.ResolveKingdom(initiatorId), WorldDiplomacyBehavior.ResolveKingdom(targetId), reason);
        public bool ExternalProposalTakenEffect(string intent, string initiatorId, string targetId) =>
            new OfferActionPort(_owner).HasTakenEffect(intent, initiatorId, targetId);
        public string NewThreatId() => WorldDiplomacyBehavior.NewId("diplomacy_threat");

        // Propagation/world snapshots (infrequent, bounded by rules).
        public List<WorldDiplomacyPropagationApplication.CourtTarget> CaptureCourtTargets()
        {
            var targets = new List<WorldDiplomacyPropagationApplication.CourtTarget>();
            foreach (Kingdom kingdom in Kingdom.All.Where(x => x != null && !x.IsEliminated && !string.IsNullOrWhiteSpace(x.StringId)).OrderBy(x => x.StringId, StringComparer.OrdinalIgnoreCase))
            {
                targets.Add(new WorldDiplomacyPropagationApplication.CourtTarget
                {
                    KingdomId = kingdom.StringId,
                    SettlementId = _owner.ResolveCourtSettlement(kingdom)?.StringId ?? "",
                    IsPlayerAffiliated = WorldDiplomacyBehavior.IsPlayerAffiliatedKingdom(kingdom)
                });
            }
            return targets;
        }
        public WorldDiplomacyPropagationApplication.DistanceSnapshot CapturePropagationDistances(WorldDiplomacyDocument document)
        {
            List<Settlement> allSettlements = Settlement.All.Where(x => x != null).ToList();
            List<Settlement> settlements = allSettlements.Where(x => !x.IsHideout && !string.IsNullOrWhiteSpace(x.StringId)).ToList();
            List<Tuple<Kingdom, Settlement>> courts = Kingdom.All
                .Where(x => x != null && !x.IsEliminated && !string.IsNullOrWhiteSpace(x.StringId))
                .OrderBy(x => x.StringId, StringComparer.OrdinalIgnoreCase)
                .Select(x => Tuple.Create(x, _owner.ResolveCourtSettlement(x)))
                .ToList();
            Dictionary<string, Settlement> settlementsById = new Dictionary<string, Settlement>(StringComparer.OrdinalIgnoreCase);
            foreach (Settlement settlement in allSettlements)
            {
                if (!string.IsNullOrWhiteSpace(settlement.StringId) && !settlementsById.ContainsKey(settlement.StringId))
                    settlementsById.Add(settlement.StringId, settlement);
            }
            if (!settlementsById.TryGetValue(document?.OriginSettlementId ?? "", out Settlement origin)) return null;
            Dictionary<string, float> settlementDistances = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, Settlement> destination in settlementsById)
                settlementDistances.Add(destination.Key, origin.GatePosition.Distance(destination.Value.GatePosition));
            Dictionary<string, float> courtDistances = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            foreach (Tuple<Kingdom, Settlement> court in courts)
            {
                if (court.Item2 != null && !courtDistances.ContainsKey(court.Item1.StringId))
                    courtDistances.Add(court.Item1.StringId, origin.GatePosition.Distance(court.Item2.GatePosition));
            }
            return new WorldDiplomacyPropagationApplication.DistanceSnapshot
            {
                MaxCivilianDistance = settlements.Count == 0 ? 0f : settlements.Max(x => origin.GatePosition.Distance(x.GatePosition)),
                MaxCourtDistance = courts.Where(x => x.Item2 != null).Select(x => origin.GatePosition.Distance(x.Item2.GatePosition)).DefaultIfEmpty(0f).Max(),
                SettlementDistances = settlementDistances,
                CourtDistances = courtDistances
            };
        }

        // Presentation / effect leafs.
        public void ShowPlayerCourtDelivery(string receiverName) =>
            TaleWorlds.Library.InformationManager.DisplayMessage(
                new TaleWorlds.Library.InformationMessage("你的宣言已传播至" + (receiverName ?? "") + "。"));
        public void RemoveQueuedNativeDiplomacyDecisions() => _owner.RemoveQueuedNativeDiplomacyDecisions();
        public void EnsureActiveWarLedgersAndRemoveEndedWars() => _owner.EnsureActiveWarLedgersAndRemoveEndedWars();
        public void TrimRecentBattleFacts() => _owner.TrimRecentBattleFacts();
        public void TrimNativeSignals() => _owner.TrimNativeSignals();
        public void DecayWarPressure() => WorldDiplomacyWarPressureRules.DecayWarPressure(_owner._storage?.WarPressure, CurrentDay());
        public IReadOnlyList<WorldDiplomacyThreat> Threats() => Storage?.DiplomaticThreats;

        // Module/service leafs.
        public IReadOnlyList<WorldDiplomacyPolicySignalSnapshot> ForeignPolicySignals() => DiplomacyModuleServices.Policy.GetForeignPolicySignals();
        public string PublishedPolicyHistoryLedgerId() => DiplomacyModuleServices.Policy.GetPublishedPolicyHistoryLedgerId();
        public long PublishedPolicyHistoryRevision() => DiplomacyModuleServices.Policy.GetPublishedPolicyHistoryCurrentRevision();
        public long PublishedPolicyHistorySequence() => DiplomacyModuleServices.Policy.GetPublishedPolicyHistoryCurrentSequence();
        public IReadOnlyList<PublishedPolicyArtifactLedgerEntry> PublishedPolicyHistoryArtifacts(long cursor, int batchSize) =>
            DiplomacyModuleServices.Policy.GetPublishedPolicyHistoryArtifacts(cursor, batchSize);
        public bool TryAcknowledgePublishedPolicyHistoryThrough(long throughSequence) =>
            DiplomacyModuleServices.Policy.TryAcknowledgePublishedPolicyHistoryThrough(throughSequence);
        public List<PublishedPolicyArtifactLedgerEntry> ReadPublishedPolicyArtifacts() => WorldDiplomacyBehavior.ReadAllPublishedPolicyArtifactsForMigration();
        public long PublishedWorldWeeklyHistoryRevision() => MyBehavior.GetPublishedWorldWeeklyReportHistoryRevisionForExternal();
        public void RecordWorldDiplomacyWeeklyMaterialExternal(string stableKey, string title, string text,
            string authorKingdomId, string authorRulerId, string relatedKingdomId, bool isWorldLevel, int day, string gameDate) =>
            MyBehavior.RecordWorldDiplomacyWeeklyMaterialForExternal(stableKey, title, text,
                authorKingdomId, authorRulerId, relatedKingdomId, isWorldLevel, day, gameDate);
        public bool TryBuildKingdomStrategicProfilePrompt(string kingdomId, string marker, out string prompt) =>
            WorldDiplomacyBehavior.TryBuildKingdomStrategicProfilePrompt(WorldDiplomacyBehavior.ResolveKingdom(kingdomId), marker, out prompt);
        public void LogKingdomStrategicProfileInjection(WorldDiplomacyJob job, string profilePrompt) =>
            WorldDiplomacyBehavior.LogKingdomStrategicProfileInjection(job, profilePrompt);
        public void ClearLlmCacheAffinityKey() => _owner._runtime.LastLlmCacheAffinityKey = "";

        // Leaf port factories - bounded snapshots, no Application calls behind them.
        public IWorldDiplomacyActionSelectionPort ActionSelection() => new ActionSelectionPort(_owner, null);
        public IWorldDiplomacyNoActionPort NoActionPort(string authorId, string targetId) =>
            new NoActionPort(_owner, WorldDiplomacyBehavior.ResolveKingdom(authorId), WorldDiplomacyBehavior.ResolveKingdom(targetId));
        public IWorldDiplomacyPeaceAdmissionPort PeaceAdmission() => new PeaceAdmissionPort(_owner);
        public IWorldDiplomacyPromptWorld PromptWorld() => new PromptWorld(_owner);
        public IWorldDiplomacyDraftRepairWorld DraftRepairWorld() => new PromptWorld(_owner);
        public IWorldDiplomacyDocumentExecutionPort DocumentExecution() => new DocumentExecutionPort(_owner);
        public IWorldDiplomacyPublicationPort Publication() => new PublicationPort(_owner);
        public IWorldDiplomacyJobPreparationPort JobPreparation() => new JobPreparationPort(_owner);
        public IWorldDiplomacyHistoryCapturePort HistoryCapture() => new HistoryCapturePort(_owner);
        public IWorldDiplomacyInitialPeacePort InitialPeace() => new InitialPeacePort(_owner);
        public IWorldDiplomacyPrestigePort Prestige() => new PrestigePort();
        public IWorldDiplomacyThreatSettlementPort ThreatSettlement() => new ThreatSettlementPort(_owner);
        public IWorldDiplomacyThreatBindingPort ThreatBinding() => new ThreatBindingPort(_owner);
        public IWorldDiplomacyAnalysisPort AnalysisPort() => new AnalysisPort(_owner);
        public IWorldDiplomacyOfferActionPort OfferAction() => new OfferActionPort(_owner);
        public IWorldDiplomacyImmediateActionPort ImmediateAction() => new ImmediateActionPort(_owner);
        public IWorldDiplomacyWarAdmissionPort WarAdmission(string firstId, string secondId) =>
            new WarAdmissionPort(_owner, WorldDiplomacyBehavior.ResolveKingdom(firstId), WorldDiplomacyBehavior.ResolveKingdom(secondId));
        public IWorldDiplomacyStorageNormalizationSource StorageNormalizationSource() => new StorageNormalizationSource(_owner);
        public IWorldDiplomacyCanonicalHistoryMigrationSource CanonicalHistoryMigrationSource() => new CanonicalHistoryMigrationSource(_owner);
        public IWorldDiplomacyLlmDispatchSource LlmDispatchSource() => new LlmDispatchSource(_owner);
        public IWorldDiplomacyCompletionSource CompletionSource() => new CompletionSource(_owner);
        public void PollNotifications() => _owner._notifications.Poll(_owner._storage, DateTime.UtcNow, _owner.NotificationSink);
        public void ReplaceStorage(WorldDiplomacyStorage storage) => _owner._storage = storage;
        public string ResolveSettlementPartyId(string settlementId) => WorldDiplomacyBehavior.ResolveSettlementById(settlementId)?.OwnerClan?.Kingdom?.StringId;
        public string PartyNameIncludingEliminated(string id) => WorldDiplomacyBehavior.KingdomName(WorldDiplomacyBehavior.ResolveKingdomIncludingEliminated(id));
        public string PlayerKingdomId() => Clan.PlayerClan?.Kingdom?.StringId;
        public string ResolveKingdomIdOrNull(string id) => WorldDiplomacyBehavior.ResolveKingdom(id)?.StringId;
        public bool KingdomIsEliminated(string id) => WorldDiplomacyBehavior.ResolveKingdomIncludingEliminated(id)?.IsEliminated == true;
        public (string id, string name) KingdomValidationIdentity(string id)
        {
            Kingdom kingdom = WorldDiplomacyBehavior.ResolveKingdom(id);
            return kingdom == null ? (null, null) : (kingdom.StringId, WorldDiplomacyBehavior.KingdomName(kingdom));
        }
        public string SettlementValidationName(string settlementId) =>
            WorldDiplomacyBehavior.ResolveSettlementById(settlementId)?.Name?.ToString();
        public string RealmRulerDisplayName(string kingdomId) =>
            (WorldDiplomacyBehavior.ResolveKingdom(kingdomId) is Kingdom kingdom
                ? (kingdom.Leader ?? kingdom.RulingClan?.Leader)?.Name?.ToString()
                : null);
        public int GetRoundParticipantLimit() => WorldDiplomacyBehavior.GetRoundParticipantLimit();
        public string CanAiAuthorDocumentBlockReason(string id) =>
            WorldDiplomacyBehavior.CanAiAuthorDiplomaticDocument(WorldDiplomacyBehavior.ResolveKingdom(id), out string reason) ? null : reason;
        public void LogDiplomaticThreatFallbackAnalysisPublished(WorldDiplomacyJob job) => _owner.LogDiplomaticThreatFallbackAnalysisPublished(job);
        public void RemoveJob(string jobId) => _owner.RemoveJob(jobId);
        public void AddWarPressure(string sourceId, string targetId, int delta, string reason, string intent) =>
            _owner.AddWarPressure(sourceId, targetId, delta, reason, intent);
        public WarPressureEntry FindWarPressure(string sourceId, string targetId) => _owner.FindWarPressure(sourceId, targetId);
    }
}

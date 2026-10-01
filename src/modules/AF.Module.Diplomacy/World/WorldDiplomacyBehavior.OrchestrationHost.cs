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
        public int RoundTargetDurationDays() => WorldDiplomacyBehavior.GetRoundLengthDays();
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
            return a == null || b == null ? float.MaxValue : a.GatePosition.Distance(b.GatePosition);
        }
        public bool IsRepresentativeForAddressedVassal(string receiverId, WorldDiplomacyDocument document)
            => _owner.IsDiplomaticRepresentativeForAddressedVassal(WorldDiplomacyBehavior.ResolveKingdom(receiverId), document);
        public bool CampaignHasKingdoms() => Campaign.Current != null && Kingdom.All.Any();
        public IReadOnlyList<string> AllKingdomIds() => Kingdom.All
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.StringId))
            .Select(x => x.StringId)
            .ToList();
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
        public WorldDiplomacyPolicyRoundApplication.Parties ResolvePolicyParties(WorldDiplomacyPolicySignal signal)
        {
            Kingdom issuer = WorldDiplomacyBehavior.ResolveKingdom(signal?.IssuerKingdomId);
            Kingdom affected = WorldDiplomacyBehavior.ResolveKingdom(signal?.TargetKingdomId);
            bool valid = issuer != null && affected != null && issuer != affected && !issuer.IsEliminated && !affected.IsEliminated;
            Kingdom issuerRepresentative = valid ? WorldDiplomacyBehavior.ResolveWorldDiplomacyRepresentative(issuer) : null;
            Kingdom affectedRepresentative = valid ? WorldDiplomacyBehavior.ResolveWorldDiplomacyRepresentative(affected) : null;
            return new WorldDiplomacyPolicyRoundApplication.Parties(valid, issuerRepresentative?.StringId,
                affectedRepresentative?.StringId,
                affectedRepresentative != null && WorldDiplomacyBehavior.IsPlayerKingdom(affectedRepresentative),
                issuerRepresentative != null && WorldDiplomacyBehavior.IsPlayerKingdom(issuerRepresentative));
        }
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
            return WorldDiplomacyGeographyApplication.CourtTargets(new GeographyPort(_owner, null, null));
        }
        public WorldDiplomacyPropagationApplication.DistanceSnapshot CapturePropagationDistances(WorldDiplomacyDocument document)
        {
            return WorldDiplomacyGeographyApplication.Recalculation(new GeographyPort(_owner, null, document?.OriginSettlementId));
        }

        // Presentation / effect leafs and world snapshots.
        public void ShowPlayerCourtDelivery(string receiverName) =>
            TaleWorlds.Library.InformationManager.DisplayMessage(
                new TaleWorlds.Library.InformationMessage("你的宣言已传播至" + (receiverName ?? "") + "。"));
        public void RemoveQueuedNativeDiplomacyDecisions() => _owner.RemoveQueuedNativeDiplomacyDecisions();
        public List<(string firstId, string secondId)> ActiveWarKingdomPairs()
        {
            List<Kingdom> kingdoms = Kingdom.All
                .Where(x => x != null && !x.IsEliminated)
                .OrderBy(x => x.StringId, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var pairs = new List<(string firstId, string secondId)>();
            for (int i = 0; i < kingdoms.Count; i++)
            {
                for (int j = i + 1; j < kingdoms.Count; j++)
                {
                    if (FactionManager.IsAtWarAgainstFaction(kingdoms[i], kingdoms[j]))
                    {
                        pairs.Add((kingdoms[i].StringId, kingdoms[j].StringId));
                    }
                }
            }
            return pairs;
        }
        public void InvalidateWarSituationCache(string firstId, string secondId) =>
            _owner.InvalidateWarSituation(WorldDiplomacyBehavior.ResolveKingdom(firstId),
                WorldDiplomacyBehavior.ResolveKingdom(secondId));
        public bool InternalActionDepthActive() => WorldDiplomacyBehavior._internalDiplomaticActionDepth > 0;
        public int DaysPerYear() => WorldDiplomacyBehavior.DaysPerYear;
        public int RecentBattleRetentionDays() => WorldDiplomacyBehavior.RecentBattleRetentionDays;
        public int NativeSignalBaseValue(string action) =>
            WorldDiplomacyEventRules.NativeSignalBaseValue(action);
        public IReadOnlyList<WorldDiplomacyThreat> Threats() => _owner._storage?.DiplomaticThreats;

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
    }
}

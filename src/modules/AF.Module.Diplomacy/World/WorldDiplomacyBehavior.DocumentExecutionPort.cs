using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    internal sealed class DocumentExecutionPort : IWorldDiplomacyDocumentExecutionPort
    {
        private readonly WorldDiplomacyBehavior _owner;
        // Per-command identity cache: at most the author and bounded action targets.
        // Discarded synchronously with this port; never retained by background work.
        private readonly Dictionary<string, TaleWorlds.CampaignSystem.Kingdom> _parties =
            new Dictionary<string, TaleWorlds.CampaignSystem.Kingdom>(StringComparer.OrdinalIgnoreCase);
        private TaleWorlds.CampaignSystem.Kingdom ResolveParty(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            if (!_parties.TryGetValue(id, out var party))
            {
                party = WorldDiplomacyBehavior.ResolveKingdom(id);
                _parties.Add(id, party);
            }
            return party;
        }
        internal DocumentExecutionPort(WorldDiplomacyBehavior owner) => _owner = owner;
        public string ResolveKingdomId(string id) => ResolveParty(id)?.StringId;
        public bool IsEliminated(string id) => ResolveParty(id)?.IsEliminated == true;
        public bool HasIndependentWorldDiplomacyAuthority(string id) => WorldDiplomacyBehavior.HasIndependentWorldDiplomacyAuthority(ResolveParty(id));
        public bool CanAiAuthorDiplomaticDocument(string id, out string reason) => WorldDiplomacyBehavior.CanAiAuthorDiplomaticDocument(ResolveParty(id), out reason);
        public WorldDiplomacyRound ResolveRound(string id) => _owner.ResolveRound(id);
        public WorldDiplomacyDocument ResolveDocument(string id) => _owner.ResolveDocument(id);
        public void PruneInvalidOffers(WorldDiplomacyRound round) => _owner.PruneInvalidOffers(round);
        public bool IsNonRootAiRelayNoActionAllowed(WorldDiplomacyRound round, string slot, string author, string target, bool relay, bool external, WorldDiplomacyDocument source) => _owner.IsNonRootAiRelayNoActionAllowed(round, slot, ResolveParty(author), ResolveParty(target), relay, external, source);
        public List<string> BuildLegalDiplomaticActionIntents(WorldDiplomacyRound round, string author, string target) => _owner.BuildLegalDiplomaticActionIntents(round, ResolveParty(author), ResolveParty(target));
        public List<string> BuildLegalDiplomaticDeclarationIntents(WorldDiplomacyRound round, string author, string target, bool relay, string slot, bool external, WorldDiplomacyDocument source) => _owner.BuildLegalDiplomaticDeclarationIntents(round, ResolveParty(author), ResolveParty(target), relay, slot, external, source);
        public bool TryGetDiplomaticStateViolation(string intent, string author, string target, out string reason) => _owner.TryGetDiplomaticStateViolation(intent, ResolveParty(author), ResolveParty(target), out reason);
        public bool TryGetPlayerWorldStateIntentViolation(WorldDiplomacyDocument doc, string intent, string commitment, string author, string target, out string reason) => _owner.TryGetPlayerWorldStateIntentViolation(doc, intent, commitment, ResolveParty(author), ResolveParty(target), out reason);
        public WorldDiplomacyRoundOffer FindRequiredPeaceOfferResponse(WorldDiplomacyRound round, string author, string slot, bool external, string sourceId, bool requireAnyOpenPeaceOffer) => WorldDiplomacyBehavior.FindRequiredPeaceOfferResponse(round, ResolveParty(author), slot, external, sourceId, requireAnyOpenPeaceOffer);
        public bool IsAtWar(string author, string target) => TaleWorlds.CampaignSystem.FactionManager.IsAtWarAgainstFaction(ResolveParty(author), ResolveParty(target));
        public bool IsPlayerKingdom(string id) => WorldDiplomacyBehavior.IsPlayerKingdom(ResolveParty(id));
        public string NewId(string prefix) => WorldDiplomacyBehavior.NewId(prefix);
        public void SuppressInvalidDocumentBeforePropagation(WorldDiplomacyDocument doc, string reason) => _owner.SuppressInvalidDocumentBeforePropagation(doc, reason);
        public void ExecuteImmediateIntent(string author, string target, string intent, WorldDiplomacyDocument doc) => _owner.ExecuteImmediateIntent(ResolveParty(author), ResolveParty(target), intent, doc);
        public void ProcessDiplomaticThreatDocument(WorldDiplomacyDocument doc, string author, string target, bool recordTargetDecisions = true) => _owner.ProcessDiplomaticThreatDocument(doc, ResolveParty(author), ResolveParty(target), recordTargetDecisions);
        public void RecordDiplomaticThreatTargetDecisions(WorldDiplomacyDocument doc, string author, string target, string intent) => _owner.RecordDiplomaticThreatTargetDecisions(doc, ResolveParty(author), ResolveParty(target), intent);
        public void RecordDiplomaticThreatTargetDecisionsForActions(WorldDiplomacyDocument doc, string author) => _owner.RecordDiplomaticThreatTargetDecisionsForActions(doc, ResolveParty(author));
        public bool DeferUnresolvedRequiredThreatAction(WorldDiplomacyDocument doc, string author, string target, string intent) => _owner.DeferUnresolvedRequiredThreatAction(doc, ResolveParty(author), ResolveParty(target), intent);
        public void ApplyDiplomaticThreatReputationPenalty(WorldDiplomacyThreat threat, WorldDiplomacyDocument doc) => _owner.ApplyDiplomaticThreatReputationPenalty(threat, doc);
        public void TrySettleRelayOffer(WorldDiplomacyDocument doc) => _owner.TrySettleRelayOffer(doc);
        public void ApplyDiplomaticPressureEffect(WorldDiplomacyDocument doc) => _owner.ApplyDiplomaticPressureEffect(doc);
        public void SettleInternationalReputationForDocument(WorldDiplomacyDocument doc) => _owner.SettleInternationalReputationForDocument(doc);
        public void AppendCanonicalDocumentEvents(WorldDiplomacyDocument doc) => _owner.AppendCanonicalDocumentEvents(doc);
        public void HandleRoundDocumentProcessed(WorldDiplomacyDocument doc) => _owner.HandleRoundDocumentProcessed(doc);
        public void RecordDiplomacyWeeklyMaterial(WorldDiplomacyDocument doc) => _owner.RecordDiplomacyWeeklyMaterial(doc);
        public void ReconcileAnalyzedPlayerDeclarationWithReachedCourts(WorldDiplomacyDocument doc) => _owner.ReconcileAnalyzedPlayerDeclarationWithReachedCourts(doc);
        public void TryAppendDiplomaticThreatHistoryResult(WorldDiplomacyThreat threat) => _owner.TryAppendDiplomaticThreatHistoryResult(threat);
        public void TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(WorldDiplomacyThreat threat) => _owner.TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(threat);
        public void TryAppendDiplomaticThreatIssuerRewardHistoryResult(WorldDiplomacyThreat threat) => _owner.TryAppendDiplomaticThreatIssuerRewardHistoryResult(threat);
        public void TryAppendDiplomaticThreatNonComplianceHistoryResult(WorldDiplomacyThreat threat, WorldDiplomacyThreatNonComplianceEvent item) => _owner.TryAppendDiplomaticThreatNonComplianceHistoryResult(threat, item);
        public void ScheduleDeferredCanonicalHistoryRetry(string id) => _owner.ScheduleDeferredCanonicalHistoryRetry(id);
        public void StartDocumentPropagation(WorldDiplomacyDocument doc, string author) => _owner.StartDocumentPropagation(doc, ResolveParty(author));
        public WarPressureEntry FindWarPressure(string source, string target) => _owner.FindWarPressure(source, target);
        public void AddWarPressure(string source, string target, int delta, string reason, string intent) => _owner.AddWarPressure(source, target, delta, reason, intent);
        public List<string> NormalizeKingdomIdList(IEnumerable<string> values, string excludedId) => WorldDiplomacyBehavior.NormalizeKingdomIdList(values, excludedId);
        public void Log(string message) => WorldDiplomacyBehavior.Log(message);
        public void Notify(string message) => TaleWorlds.Library.InformationManager.DisplayMessage(new TaleWorlds.Library.InformationMessage(message));
        public int MaxDiplomaticActionsPerDocument => WorldDiplomacyBehavior.MaxDiplomaticActionsPerDocument;
        public int MaxRelayParticipants => WorldDiplomacyBehavior.MaxRelayParticipants;
        public int CurrentDay => WorldDiplomacyBehavior.CurrentDay();
        public List<WorldDiplomacyThreat> Threats => _owner._storage?.DiplomaticThreats;
    }
}

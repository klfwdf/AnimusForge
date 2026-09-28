using System.Collections.Generic;

namespace AnimusForge;

internal sealed class WorldDiplomacyConsequenceParty
{
    internal string Id { get; }
    internal bool IsEliminated { get; }
    internal bool HasRulingClan { get; }
    internal WorldDiplomacyConsequenceParty(string id, bool eliminated, bool hasRulingClan)
    { Id = id; IsEliminated = eliminated; HasRulingClan = hasRulingClan; }
}

internal sealed class WorldDiplomacyConsequenceSnapshot
{
    internal string RulingClanId { get; }
    internal IReadOnlyCollection<string> EligibleClanIds { get; }
    internal WorldDiplomacyConsequenceSnapshot(string rulingClanId, IReadOnlyCollection<string> eligibleClanIds)
    { RulingClanId = rulingClanId; EligibleClanIds = eligibleClanIds; }
}

internal sealed class WorldDiplomacyConsequenceClan
{
    internal bool IsEliminated { get; }
    internal string LeaderId { get; }
    internal WorldDiplomacyConsequenceClan(bool eliminated, string leaderId)
    { IsEliminated = eliminated; LeaderId = leaderId; }
}

// Main-thread snapshots and atomic effects. Canonical writes and retry order belong to Application.
internal interface IWorldDiplomacyThreatSettlementPort
{
    bool CampaignAvailable { get; }
    int IssuerRelationRewardMax { get; }
    int UltimatumComplianceRoyalRelationPenalty { get; }
    int UltimatumCompliancePrestigeChange { get; }
    int WarningCompliancePrestigeChange { get; }
    int UltimatumFollowThroughPrestigePenalty { get; }
    int WarningFollowThroughPrestigePenalty { get; }
    int ZeroPrestigeUltimatumBreachRelationPenalty { get; }
    int ZeroPrestigeWarningBreachRelationPenalty { get; }
    int GetThreatComplianceIssuerRelationReward();
    int CurrentDay();
    void Log(string message);
    string KingdomName(string kingdomId);
    WorldDiplomacyConsequenceParty ReadParty(string kingdomId);
    WorldDiplomacyConsequenceSnapshot CaptureConsequenceSnapshot(string kingdomId);
    void PrepareClans(HashSet<string> requiredClanIds);
    WorldDiplomacyConsequenceClan ReadClan(string clanId);
    int ReadRelation(string firstClanId, string secondClanId);
    void ChangeRelation(string firstClanId, string secondClanId, int amount);
    bool CancelPolicy(string policyId, string ownerId, string reason, out string policyName, out string result);
    int ApplyNationalPrestigeDelta(string kingdomId, int delta, WorldDiplomacyDocument document, string reason);
    void ApplyZeroPrestigeBreachRelationPenalty(string kingdomId, int amount);
    WorldDiplomacyDocument ResolveDocument(string id);
    void AppendCanonicalDocumentEvents(WorldDiplomacyDocument document);
    void ScheduleDeferredCanonicalHistoryRetry(string documentId);
    void TryAppendDiplomaticThreatHistoryResult(WorldDiplomacyThreat threat);
    void TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(WorldDiplomacyThreat threat);
    void TryAppendDiplomaticThreatIssuerRewardHistoryResult(WorldDiplomacyThreat threat);
    void TryAppendDiplomaticThreatNonComplianceHistoryResult(WorldDiplomacyThreat threat, WorldDiplomacyThreatNonComplianceEvent decision);
}


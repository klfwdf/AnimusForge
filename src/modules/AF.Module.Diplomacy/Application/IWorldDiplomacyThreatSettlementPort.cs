using System.Collections.Generic;
using AnimusForge.Refactor.Domain;

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
    internal IReadOnlyList<WorldDiplomacyClanSnapshot> Clans { get; }
    internal WorldDiplomacyConsequenceSnapshot(string rulingClanId, IReadOnlyList<WorldDiplomacyClanSnapshot> clans)
    { RulingClanId = rulingClanId; Clans = clans; }
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
    WorldDiplomacyDocument ResolveDocument(string id);
}

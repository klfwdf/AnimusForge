using System.Collections.Generic;

namespace AnimusForge;

// Policy owns its bounded cache/ledger; diplomacy owns consumption and acknowledgement cadence.
internal sealed class DiplomacyPolicyObservationBridge : IDiplomacyPolicyObservationPort
{
    public string BuildSnapshot(string kingdomId) => WorldDiplomacyPolicyContext.BuildSnapshot(kingdomId);
    public IReadOnlyList<WorldDiplomacyPolicySignalSnapshot> GetForeignPolicySignals() => WorldDiplomacyPolicyContext.GetForeignPolicySignals().AsReadOnly();
    public bool IsForeignPolicySignalActive(string policyId, string ownerKingdomId, string affectedKingdomId) =>
        WorldDiplomacyPolicyContext.IsForeignPolicySignalActive(policyId, ownerKingdomId, affectedKingdomId);
    public string GetPublishedPolicyHistoryLedgerId() => WorldDiplomacyPolicyContext.GetPublishedPolicyHistoryLedgerId();
    public long GetPublishedPolicyHistoryCurrentSequence() => WorldDiplomacyPolicyContext.GetPublishedPolicyHistoryCurrentSequence();
    public long GetPublishedPolicyHistoryCurrentRevision() => WorldDiplomacyPolicyContext.GetPublishedPolicyHistoryCurrentRevision();
    public IReadOnlyList<PublishedPolicyArtifactLedgerEntry> GetPublishedPolicyHistoryArtifacts(long afterSequence, int maxCount) =>
        WorldDiplomacyPolicyContext.GetPublishedPolicyHistoryArtifacts(afterSequence, maxCount);
    public bool TryAcknowledgePublishedPolicyHistoryThrough(long throughSequence) => WorldDiplomacyPolicyContext.TryAcknowledgePublishedPolicyHistoryThrough(throughSequence);
    public void Clear() => WorldDiplomacyPolicyContext.Clear();
}

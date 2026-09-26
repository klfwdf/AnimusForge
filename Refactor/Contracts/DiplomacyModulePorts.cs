using System.Collections.Generic;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge;

// Same-DLL ports; synchronous and game-object-free. No second action or state owner.
internal interface IDiplomacyConversationPort
{
    bool CanInjectDiplomacyRule(string heroId);
    bool CanUseDiplomacyActionPostprocess(string heroId);
    bool CanUseFullDiplomacyActionPostprocess(string heroId);
    bool CanUseNpcSovereignDeclareWarPostprocess(string heroId);
    bool CanUseIndependentClanPeace(string heroId);
    bool IsIndependentClanPeacePostprocessTag(string tag);
    string BuildDiplomacyPostprocessContext(string heroId);
    void ProcessDiplomacyTags(string heroId, ref string text);
    bool TryBuildTributePowerContext(string payerId, string receiverId, out AfTributePowerContext context);
}

internal interface IWorldDiplomacyModulePort
{
    bool CanDiscuss(string heroId);
    bool TryBuildProactiveDiscussion(string heroId, out string key, out string fact, out float urgency);
    WorldDiplomacyTimelineRevisionResult QueryTimelineRevision();
    WorldDiplomacyTimelineDocumentsResult QueryTimelineDocuments(int maxCount);
    bool TryMarkDocumentRead(string documentId, out bool ownerAvailable);
    IWorldDiplomacyPresentationPort Presentation { get; }
    void OnEngineTick();
}

internal interface IDiplomacyPolicyObservationPort
{
    string BuildSnapshot(string kingdomId);
    IReadOnlyList<WorldDiplomacyPolicySignalSnapshot> GetForeignPolicySignals();
    bool IsForeignPolicySignalActive(string policyId, string ownerKingdomId, string affectedKingdomId);
    string GetPublishedPolicyHistoryLedgerId();
    long GetPublishedPolicyHistoryCurrentSequence();
    long GetPublishedPolicyHistoryCurrentRevision();
    IReadOnlyList<PublishedPolicyArtifactLedgerEntry> GetPublishedPolicyHistoryArtifacts(long afterSequence, int maxCount);
    bool TryAcknowledgePublishedPolicyHistoryThrough(long throughSequence);
    void Clear();
}

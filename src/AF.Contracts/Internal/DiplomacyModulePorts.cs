using System.Collections.Generic;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge;

// Same-DLL ports; synchronous and game-object-free. No second action or state owner.
internal interface IDiplomacyConversationPort
{
    bool ApplyVassalageRewardTags(string giverId, string receiverId, ref string text, List<string> giverFacts, List<string> receiverFacts);
    bool ApplyKingdomAnnexationRewardTags(string giverId, string receiverId, ref string text, List<string> giverFacts, List<string> receiverFacts);
    string BuildPrompt(string heroId, string extras);
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
    bool OpenComposeFromTerminal(System.Action onClose = null);
    bool ShowRoyalAnnouncementArchive(System.Action onClose = null);
    bool ComposePopupOpen { get; }
    string Standing(string kingdomId);
    void ApplyExternalPrestigeDelta(string kingdomId, int delta, string reason);
    string BuildMemory(string heroId, string kingdomOverride, string input, IReadOnlyList<string> ruleIds, bool proactive);
    // Request-owned knowledge IDs captured on the game thread; unavailable owner returns null.
    ISet<string> CaptureKnownDocumentIds(string heroId, string kingdomOverride);
    bool CanDiscuss(string heroId);
    bool TryBuildProactiveDiscussion(string heroId, out string key, out string fact, out float urgency);
    WorldDiplomacyTimelineRevisionResult QueryTimelineRevision();
    WorldDiplomacyTimelineDocumentsResult QueryTimelineDocuments(int maxCount);
    bool TryMarkDocumentRead(string documentId, out bool ownerAvailable);
    WorldDiplomacyDocumentReadResult MarkTimelineRead(string documentId);
    IWorldDiplomacyPresentationPort Presentation { get; }
    void OnLifecycle(WorldDiplomacyLifecycleEvent lifecycle);
    void OnEngineTick();
    void OnCampaignTick();
    void OnDailyTick();
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

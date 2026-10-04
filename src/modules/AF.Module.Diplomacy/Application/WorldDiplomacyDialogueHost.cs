using System;
using AnimusForge.DiplomacyDialogue;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge;

// Detached values and leaf effects only. The orchestration owns consent,
// revisions, publication and recovery; this port never republishes or retries.
internal interface IWorldDiplomacyDialogueHost
{
    bool RulerAlive(string rulerId);
    bool IsPlayerRuler(string rulerId);
    string RulerName(string rulerId);
    (bool available, bool cycle, bool existing) TreatyState(string receivingId, string joiningId);
    (bool success, bool changed, string reason) ExecuteTreaty(string intent, string receivingId, string joiningId);
    MemoryCommitResult CommitFact(string rulerId, string sourceId, string fact, int day,
        string locationId, int hour = -1, string npcName = null, string gameDate = "");
    string PersonalMemory(string rulerId, string topic, string counterpart);
}

internal sealed partial class WorldDiplomacyOrchestration
{
    private IWorldDiplomacyDialogueHost DialogueHost => _host as IWorldDiplomacyDialogueHost;
    private string ResolveDialogueParty(string id) => _host.ResolvePartyId(id);
    private bool DialogueRulerIsCurrent(string partyId, string rulerId) => !string.IsNullOrWhiteSpace(partyId)
        && !string.IsNullOrWhiteSpace(rulerId) && string.Equals(_host.PartyRulerId(partyId), rulerId, StringComparison.OrdinalIgnoreCase)
        && DialogueHost?.RulerAlive(rulerId) == true;
    private bool HasCurrentDiplomaticRulerMemorySnapshot(string partyId, string capturedRulerId) => DialogueRulerIsCurrent(partyId, capturedRulerId);
    private bool DialogueDocumentKnown(string partyId, string documentId) => ResolveDocument(documentId)?.IsReadyForPublication == true;
}

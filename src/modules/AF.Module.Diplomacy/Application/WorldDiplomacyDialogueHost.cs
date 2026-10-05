using System;
using AnimusForge.DiplomacyDialogue;
using AnimusForge.Refactor.Contracts;
using System.Linq;

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
    private bool HasCourtDocumentKnowledge(string partyId, string documentId) =>
        !string.IsNullOrWhiteSpace(partyId) && !string.IsNullOrWhiteSpace(documentId)
        && (Storage.KingdomKnowledge.Any(x => x != null
                && string.Equals(x.KingdomId, partyId, StringComparison.OrdinalIgnoreCase)
                && x.DocumentIds?.Contains(documentId, StringComparer.OrdinalIgnoreCase) == true)
            || Storage.NobleKnowledge.Any(x => x != null
                && string.Equals(x.KingdomId, partyId, StringComparison.OrdinalIgnoreCase)
                && x.DocumentIds?.Contains(documentId, StringComparer.OrdinalIgnoreCase) == true));
    private bool DialogueDocumentKnown(string partyId, string documentId)
    {
        var document = ResolveDocument(documentId);
        return document?.IsReadyForPublication == true && !string.IsNullOrWhiteSpace(partyId)
            && (string.Equals(document.AuthorKingdomId, partyId, StringComparison.OrdinalIgnoreCase)
                || HasCourtDocumentKnowledge(partyId, documentId));
    }
}

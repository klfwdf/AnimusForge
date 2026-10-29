using AnimusForge;
using System;
using System.Linq;
using AnimusForge.DiplomacyDialogue;

using AnimusForge.Refactor.Domain;
using static AnimusForge.Refactor.Domain.WorldDiplomacyIntentVocabulary;
using static AnimusForge.Refactor.Domain.WorldDiplomacyTextRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyRoundLifecycleRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyStructureRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyEnvelopeJsonRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyDocumentFactRules;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    private bool CanInvalidateDialogueArrangement(WorldDiplomacyDialogueArrangement item) => item != null
        && (item.Status == "accepted" || item.Status == "deferred")
        && ResolveDocument(item.DocumentId)?.IsReadyForPublication != true;

    private bool TryValidateDialogueRevision(DialogueCommitmentRequest request,
        out WorldDiplomacyDialogueArrangement previous, out string reason)
    {
        previous = null; reason = "";
        if (!string.IsNullOrEmpty(request.SupersedesArrangementId)
            || Storage.DialogueArrangements.Any(x => DiplomacyCommitmentRules.Conflicts(x, request)))
        { reason = DiplomacyMiscPromptTexts.Get("DialogueControl_CommitmentBinding", "An accepted commitment must be fulfilled; further conversation cannot cancel or postpone it."); return false; }
        if (request.SupersedesVersion != 0 || !string.IsNullOrEmpty(request.RevisionReason))
        { reason = "revision_source_missing"; return false; }
        return true;
    }

    private bool IsCurrentDialoguePublicationWork(WorldDiplomacyDialogueArrangement item) => item != null
        && (item.Status == "accepted" || (item.Status == "deferred" && !item.ExplicitlyDeferred))
        && string.IsNullOrEmpty(item.SupersededByArrangementId)
        && Storage.DialogueArrangements.Any(x => ReferenceEquals(x, item) && x.Version == item.Version);

    private bool IsCurrentDialogueDocumentWork(WorldDiplomacyDocument document)
    {
        if (string.IsNullOrEmpty(document?.DialogueArrangementId)) return true;
        var item = Storage.DialogueArrangements?.FirstOrDefault(x => x.ArrangementId == document.DialogueArrangementId);
        return item != null && item.Version == document.DialogueArrangementVersion && item.DocumentId == document.DocumentId
            && string.IsNullOrEmpty(item.SupersededByArrangementId)
            && (item.Status == "accepted" || (item.Status == "deferred" && !item.ExplicitlyDeferred)
                || item.Status == "published" || item.Status == "executed");
    }
}

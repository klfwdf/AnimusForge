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
        if (string.IsNullOrEmpty(request.SupersedesArrangementId))
        {
            if (request.SupersedesVersion != 0 || !string.IsNullOrEmpty(request.RevisionReason))
            { reason = "revision_source_missing"; return false; }
            if (request.Move == DialogueDiplomaticMove.NewMatter && Storage.DialogueArrangements.Any(x =>
                x.RulerId == request.RulerId && x.ActorKingdomId == request.ActorKingdomId && x.TargetKingdomId == request.TargetKingdomId
                && x.Action == request.Action && CanInvalidateDialogueArrangement(x) && !x.Terms.ToTerms().Equals(request.Terms)))
            { reason = "请明确要修改的旧约定ID及版本，不能同时保留未发布的旧条件"; return false; }
            return true;
        }
        previous = Storage.DialogueArrangements.FirstOrDefault(x => x.ArrangementId == request.SupersedesArrangementId);
        if (request.Move != DialogueDiplomaticMove.NewMatter || request.SupersedesVersion <= 0
            || request.SupersedesVersion == int.MaxValue || request.Version != request.SupersedesVersion + 1
            || string.IsNullOrWhiteSpace(request.RevisionReason) || request.RevisionReason.Length > 180
            || previous == null || previous.Version != request.SupersedesVersion
            || previous.RulerId != request.RulerId || previous.ActorKingdomId != request.ActorKingdomId
            || previous.TargetKingdomId != request.TargetKingdomId || previous.Action != request.Action)
        { reason = "revision_source_or_version_mismatch"; return false; }
        if (!CanInvalidateDialogueArrangement(previous))
        { reason = "revision_requires_unpublished_arrangement_withdraw_public_offer_first"; return false; }
        return true;
    }

    private void SupersedeDialogueArrangement(WorldDiplomacyDialogueArrangement previous,
        WorldDiplomacyDialogueArrangement replacement, string reason)
    {
        // The replacement has passed all authority/terms/source checks and owns
        // its new event before the old work is invalidated. Main-thread only.
        CancelUnpublishedCommitmentWork(previous.ArrangementId, previous.Version, "条款已明确修改：" + reason);
        previous.Status = "superseded"; previous.SupersededByArrangementId = replacement.ArrangementId;
        RecordDialogueArrangementFact(replacement, "revision", "我已明确同意修改此前外交约定" + previous.ArrangementId
            + "的第" + previous.Version + "版，旧条件不再用于发文。原因：" + reason);
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

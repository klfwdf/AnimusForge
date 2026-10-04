using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// One canonical document record and one writable storage list. The host supplies
// current Bannerlord identities and date values before entering this owner.
internal readonly struct WorldDiplomacyDocumentActionReceipt
{
    internal readonly string ActionId;
    internal readonly string TargetKingdomId;
    internal readonly bool EffectAttempted;
    internal readonly bool OutcomeKnown;
    internal readonly bool Applied;
    internal readonly string Message;

    internal WorldDiplomacyDocumentActionReceipt(string actionId, string targetKingdomId,
        bool effectAttempted, bool outcomeKnown, bool applied, string message)
    {
        ActionId = actionId ?? "";
        TargetKingdomId = targetKingdomId ?? "";
        EffectAttempted = effectAttempted;
        OutcomeKnown = outcomeKnown;
        Applied = applied;
        Message = message ?? "";
    }
}

internal static class WorldDiplomacyDocumentApplication
{
    internal sealed class CreationSnapshot
    {
        internal string DocumentId;
        internal string ExchangeId;
        internal string AuthorKingdomId;
        internal string AuthorKingdomName;
        internal string AuthorRulerId;
        internal string AuthorRulerName;
        internal string TargetKingdomId;
        internal string TargetKingdomName;
        internal bool HasTarget;
        internal string Title;
        internal string Body;
        internal string Origin;
        internal int Day;
        internal string GameDate;
        internal long CreatedUtcTicks;
        internal bool IsPlayerAuthored;
        internal bool IsResponse;
    }

    internal static WorldDiplomacyDocument Create(CreationSnapshot source)
    {
        return new WorldDiplomacyDocument
        {
            DocumentId = source.DocumentId,
            ExchangeId = source.ExchangeId ?? "",
            RoundId = source.ExchangeId ?? "",
            AuthorKingdomId = source.AuthorKingdomId ?? "",
            AuthorKingdomName = source.AuthorKingdomName,
            AuthorRulerId = source.AuthorRulerId ?? "",
            AuthorRulerName = source.AuthorRulerName,
            TargetKingdomId = source.TargetKingdomId ?? "",
            TargetKingdomName = source.HasTarget ? source.TargetKingdomName : "",
            Title = WorldDiplomacyTextRules.Limit(
                WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(source.Title, "外交宣言"), 100),
            Body = WorldDiplomacyTextRules.NormalizeBody(source.Body),
            Origin = source.Origin ?? "",
            Day = source.Day,
            GameDate = source.GameDate,
            CreatedUtcTicks = source.CreatedUtcTicks,
            IsPlayerAuthored = source.IsPlayerAuthored,
            IsResponse = source.IsResponse,
            IsRead = source.IsPlayerAuthored,
            AddressedKingdomIds = source.HasTarget
                ? new List<string> { source.TargetKingdomId }
                : new List<string>()
        };
    }

    internal static void Add(
        WorldDiplomacyStorage storage,
        WorldDiplomacyDocument document,
        int maximumStored,
        Action advanceTimelineRevision)
    {
        if (document == null || string.IsNullOrWhiteSpace(document.DocumentId)) return;
        storage.Documents.RemoveAll(x => x != null
            && WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(x.DocumentId, document.DocumentId));
        storage.Documents.Add(document);
        // Cold publication boundary: display retention must not destroy pending
        // player sources, open offers, or unfinished oral publication artifacts.
        var retainedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { document.DocumentId };
        foreach (var round in WorldDiplomacyLiveRoundRules.Live(storage))
        {
            retainedIds.Add(round.RootDocumentId ?? "");
            foreach (var response in round.PlayerResponses ?? new List<WorldDiplomacyPlayerResponse>())
                if (response != null && response.Status == "pending") retainedIds.Add(response.SourceDocumentId ?? "");
            foreach (var offer in round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
                if (offer != null && WorldDiplomacyRoundLifecycleRules.IsOpenLifecycleStatus(offer.Status)) retainedIds.Add(offer.SourceDocumentId ?? "");
        }
        foreach (var job in storage.Jobs ?? new List<WorldDiplomacyJob>())
        {
            if (job == null) continue;
            retainedIds.Add(job.DocumentId ?? ""); retainedIds.Add(job.SourceDocumentId ?? "");
            foreach (string id in job.PlayerResponseSourceIds ?? new List<string>()) retainedIds.Add(id);
        }
        foreach (var oral in storage.DialogueArrangements ?? new List<WorldDiplomacyDialogueArrangement>())
            if (oral != null && (oral.Status == "accepted" || oral.Status == "deferred"))
            { retainedIds.Add(oral.DocumentId ?? ""); retainedIds.Add(oral.SourceDocumentId ?? ""); }
        var protectedDocuments = storage.Documents.Where(x => x != null && (retainedIds.Contains(x.DocumentId)
            || WorldDiplomacyStructureRules.NeedsCanonicalHistoryRetry(x))).ToList();
        var protectedSet = new HashSet<WorldDiplomacyDocument>(protectedDocuments);
        var ordinary = WorldDiplomacyRoundLifecycleRules.OrderDocumentsByRecency(
                storage.Documents.Where(x => x != null && !protectedSet.Contains(x)))
            .Take(Math.Max(0, maximumStored - protectedDocuments.Count));
        storage.Documents = WorldDiplomacyRoundLifecycleRules.OrderDocumentsChronologically(protectedDocuments.Concat(ordinary)).ToList();
        advanceTimelineRevision();
    }

    internal static void BeginAction(WorldDiplomacyDocument document,
        WorldDiplomacyDocumentAction action, string targetKingdomId)
    {
        WorldDiplomacyDocumentFactRules.MirrorPrimaryActionToDocument(document, action);
        document.ProcessingActionId = action.ActionId ?? "";
        document.AddressedKingdomIds = new List<string> { targetKingdomId };
        document.ChangedDiplomaticState = false;
        document.MechanicalResult = "";
    }

    internal static void CaptureActionResult(WorldDiplomacyDocument document,
        WorldDiplomacyDocumentAction action, WorldDiplomacyDocumentActionReceipt receipt)
    {
        action.ChangedDiplomaticState = receipt.Applied;
        action.MechanicalResult = receipt.Message;
        action.PeaceTerms = document.PeaceTerms;
        action.TreatyTerms = document.TreatyTerms == null ? null
            : WorldDiplomacyDialogueTerms.From(document.TreatyTerms.ToTerms());
    }

    internal static void SealActions(WorldDiplomacyDocument document, List<string> addressedKingdomIds,
        string originalSourceDocumentId, IReadOnlyList<WorldDiplomacyDocumentActionReceipt> receipts)
    {
        List<WorldDiplomacyDocumentAction> actions = document.Actions;
        document.ProcessingActionId = "";
        document.AddressedKingdomIds = addressedKingdomIds;
        WorldDiplomacyDocumentFactRules.MirrorPrimaryActionToDocument(document, actions[0]);
        document.SourceDocumentId = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(
            actions[0].RespondingToOfferDocumentId,
            actions[0].RespondingToThreatDocumentId,
            originalSourceDocumentId);
        document.ChangedDiplomaticState = receipts.Any(x => x.Applied);
        document.MechanicalResult = WorldDiplomacyDocumentFactRules.BuildMultiActionMechanicalResult(actions);
        document.RequiresResponse = actions.Any(x => x.RequiresResponse);
    }
}

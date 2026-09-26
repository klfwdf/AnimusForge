using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// One canonical document record and one writable storage list. The host supplies
// current Bannerlord identities and date values before entering this owner.
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
        // Pending canonical history append keeps its publication artifact.
        storage.Documents = WorldDiplomacyRoundLifecycleRules.SelectRetainedDocuments(
            storage.Documents, WorldDiplomacyStructureRules.NeedsCanonicalHistoryRetry, maximumStored);
        advanceTimelineRevision();
    }
}

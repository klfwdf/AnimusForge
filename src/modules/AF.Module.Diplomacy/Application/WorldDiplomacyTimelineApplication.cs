using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Synchronous access to the one canonical campaign store. The source only
// locates that store; query and command decisions remain in Application.
internal interface IWorldDiplomacyTimelineStateSource
{
    bool TryGetState(out WorldDiplomacyStorage storage);
}

internal static class WorldDiplomacyTimelineApplication
{
    internal static WorldDiplomacyTimelineDocumentsResult QueryDocuments(
        IWorldDiplomacyTimelineStateSource source, int maxCount)
    {
        try
        {
            return source.TryGetState(out WorldDiplomacyStorage storage)
                ? WorldDiplomacyPresentationQueries.Timeline(storage, maxCount)
                : WorldDiplomacyTimelineDocumentsResult.Unavailable();
        }
        catch { return WorldDiplomacyTimelineDocumentsResult.Failed(); }
    }

    internal static WorldDiplomacyDocumentReadResult MarkTimelineRead(IWorldDiplomacyTimelineStateSource source, string documentId)
    {
        try
        {
            string cleanId = (documentId ?? string.Empty).Trim();
            const string prefix = "diplomacy:";
            if (cleanId.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)) cleanId = cleanId.Substring(prefix.Length);
            if (cleanId.Length == 0) return WorldDiplomacyDocumentReadResult.InvalidDocumentId();
            bool applied = MarkRead(source, cleanId, out bool available);
            if (!available) return WorldDiplomacyDocumentReadResult.Unavailable();
            return applied ? WorldDiplomacyDocumentReadResult.Applied() : WorldDiplomacyDocumentReadResult.NotFound();
        }
        catch { return WorldDiplomacyDocumentReadResult.Failed(); }
    }

    internal static bool MarkRead(
        IWorldDiplomacyTimelineStateSource source, string documentId, out bool ownerAvailable)
    {
        ownerAvailable = source.TryGetState(out WorldDiplomacyStorage storage);
        return ownerAvailable && MarkRead(storage, documentId);
    }

    internal static bool MarkRead(WorldDiplomacyStorage storage, string documentId)
    {
        WorldDiplomacyDocument document = WorldDiplomacyRoundLifecycleRules.ResolveDocument(storage?.Documents, documentId);
        if (document == null) return false;
        document.IsRead = true;
        return true;
    }
}

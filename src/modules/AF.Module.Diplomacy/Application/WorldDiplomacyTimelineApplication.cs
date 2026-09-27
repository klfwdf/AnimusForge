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
        return source.TryGetState(out WorldDiplomacyStorage storage)
            ? WorldDiplomacyPresentationQueries.Timeline(storage, maxCount)
            : WorldDiplomacyTimelineDocumentsResult.Unavailable();
    }

    internal static bool MarkRead(
        IWorldDiplomacyTimelineStateSource source, string documentId, out bool ownerAvailable)
    {
        ownerAvailable = source.TryGetState(out WorldDiplomacyStorage storage);
        return ownerAvailable && WorldDiplomacyPresentationQueries.MarkRead(
            WorldDiplomacyRoundLifecycleRules.ResolveDocument(storage?.Documents, documentId));
    }
}

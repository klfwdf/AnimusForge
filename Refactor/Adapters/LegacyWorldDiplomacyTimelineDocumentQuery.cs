using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Adapters;

internal sealed class LegacyWorldDiplomacyTimelineDocumentQuery : IWorldDiplomacyTimelineDocumentQuery
{
    public WorldDiplomacyTimelineDocumentsResult Query(int maxCount)
    {
        return WorldDiplomacyBehavior.QueryTimelineDocuments(maxCount);
    }
}

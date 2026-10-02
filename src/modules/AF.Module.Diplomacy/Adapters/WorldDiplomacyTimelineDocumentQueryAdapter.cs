using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Adapters;

internal sealed class WorldDiplomacyTimelineDocumentQueryAdapter : IWorldDiplomacyTimelineDocumentQuery
{
    public WorldDiplomacyTimelineDocumentsResult Query(int maxCount)
    {
        return DiplomacyModuleServices.World.QueryTimelineDocuments(maxCount);
    }
}

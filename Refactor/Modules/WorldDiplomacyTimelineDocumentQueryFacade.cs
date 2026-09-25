using System;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Modules;

/// <summary>
/// Bounded timeline-document query. It runs only when the timeline projection is built.
/// </summary>
public sealed class WorldDiplomacyTimelineDocumentQueryFacade
{
    public const int MaximumDocumentCount = 420;

    private readonly IWorldDiplomacyTimelineDocumentQuery _query;

    public WorldDiplomacyTimelineDocumentQueryFacade(IWorldDiplomacyTimelineDocumentQuery query)
    {
        _query = query ?? throw new ArgumentNullException(nameof(query));
    }

    public WorldDiplomacyTimelineDocumentsResult Query(int maxCount)
    {
        int boundedCount = Math.Max(1, Math.Min(MaximumDocumentCount, maxCount));
        try
        {
            return _query.Query(boundedCount);
        }
        catch
        {
            return WorldDiplomacyTimelineDocumentsResult.Failed();
        }
    }
}

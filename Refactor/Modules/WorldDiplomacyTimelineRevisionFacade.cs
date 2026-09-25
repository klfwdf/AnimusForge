using System;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Modules;

/// <summary>
/// Delegates the timeline revision read to the current diplomacy owner while keeping
/// UI callers independent from the legacy campaign behavior.
/// </summary>
public sealed class WorldDiplomacyTimelineRevisionFacade
{
    private readonly IWorldDiplomacyTimelineRevisionQuery _query;

    public WorldDiplomacyTimelineRevisionFacade(IWorldDiplomacyTimelineRevisionQuery query)
    {
        _query = query ?? throw new ArgumentNullException(nameof(query));
    }

    public WorldDiplomacyTimelineRevisionResult Query()
    {
        try
        {
            return _query.Query();
        }
        catch
        {
            return WorldDiplomacyTimelineRevisionResult.Failed();
        }
    }

    public long GetRevisionOrZero()
    {
        WorldDiplomacyTimelineRevisionResult result = Query();
        return result.IsAvailable ? result.Revision : 0L;
    }
}

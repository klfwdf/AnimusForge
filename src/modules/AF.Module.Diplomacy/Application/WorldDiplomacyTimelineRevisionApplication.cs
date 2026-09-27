using AnimusForge.Refactor.Contracts;

namespace AnimusForge;

internal interface IWorldDiplomacyTimelineRevisionSource
{
    bool TryRead(out long revision);
}

// The real timeline caller enters this query owner. The source only captures
// the current campaign's scalar revision, never the query status or policy.
internal static class WorldDiplomacyTimelineRevisionApplication
{
    internal static WorldDiplomacyTimelineRevisionResult Query(IWorldDiplomacyTimelineRevisionSource source)
    {
        try
        {
            return source.TryRead(out long revision)
                ? WorldDiplomacyTimelineRevisionResult.Available(revision)
                : WorldDiplomacyTimelineRevisionResult.Unavailable();
        }
        catch
        {
            return WorldDiplomacyTimelineRevisionResult.Failed();
        }
    }
}

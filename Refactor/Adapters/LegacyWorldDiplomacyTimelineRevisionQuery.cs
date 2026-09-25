using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Temporary adapter over the current campaign behavior. It owns no state and performs
/// no scan; the scalar read remains with the existing world-diplomacy owner.
/// </summary>
internal sealed class LegacyWorldDiplomacyTimelineRevisionQuery : IWorldDiplomacyTimelineRevisionQuery
{
    public WorldDiplomacyTimelineRevisionResult Query()
    {
        return WorldDiplomacyBehavior.QueryWorldMessageTimelineRevision();
    }
}

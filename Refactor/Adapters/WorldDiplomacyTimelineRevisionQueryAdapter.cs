using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Query-facade adapter over the typed diplomacy module port. It owns no state and performs
/// no scan; the scalar read remains with the existing world-diplomacy owner.
/// </summary>
internal sealed class WorldDiplomacyTimelineRevisionQueryAdapter : IWorldDiplomacyTimelineRevisionQuery
{
    public WorldDiplomacyTimelineRevisionResult Query()
    {
        return DiplomacyModuleServices.World.QueryTimelineRevision();
    }
}

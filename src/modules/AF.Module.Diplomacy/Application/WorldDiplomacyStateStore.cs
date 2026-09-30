namespace AnimusForge;

// Canonical writable world-diplomacy state owner. The application layer holds
// the single live instance; hosts read Current and hand loaded copies to
// Replace. Serialized identity, defaults, ordering and repair semantics stay in
// WorldDiplomacyStorage and the persistence adapter.
internal sealed class WorldDiplomacyStateStore
{
    private WorldDiplomacyStorage _current = new WorldDiplomacyStorage();

    internal WorldDiplomacyStorage Current => _current;

    internal void Replace(WorldDiplomacyStorage storage) =>
        _current = storage ?? new WorldDiplomacyStorage();
}

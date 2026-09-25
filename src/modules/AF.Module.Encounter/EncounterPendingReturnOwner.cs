using System;

namespace AnimusForge.Refactor.Modules;

// A return to the custom encounter menu belongs to exactly one encounter,
// party and save runtime generation. It cannot reopen a later encounter.
internal sealed class EncounterPendingReturnOwner<TEncounter, TParty>
    where TEncounter : class where TParty : class
{
    private TEncounter _encounter;
    private TParty _party;
    private long _generation;

    internal bool IsPending => _encounter != null && _party != null;

    internal bool Mark(TEncounter encounter, TParty party, long generation)
    {
        Clear();
        if (encounter == null || party == null) return false;
        _encounter = encounter;
        _party = party;
        _generation = generation;
        return true;
    }

    internal bool IsCurrent(TEncounter encounter, TParty party, long generation)
        => IsPending && ReferenceEquals(_encounter, encounter)
            && ReferenceEquals(_party, party) && _generation == generation;

    internal void Clear()
    {
        _encounter = null;
        _party = null;
        _generation = 0;
    }
}

using System;

namespace AnimusForge.Refactor.Modules;

// A live exercise may affect only its own MapEvent or exact dummy parties.
internal static class ExerciseMapEventIdentityOwner
{
    internal static bool IsCurrent<TEvent, TParty>(TEvent currentEvent, TEvent candidate,
        TParty opponent, TParty holding, Func<TEvent, TParty, bool> containsParty)
        where TEvent : class where TParty : class
    {
        if (candidate == null) return false;
        if (currentEvent != null && ReferenceEquals(currentEvent, candidate)) return true;
        return opponent != null && containsParty(candidate, opponent)
            || holding != null && containsParty(candidate, holding);
    }
}

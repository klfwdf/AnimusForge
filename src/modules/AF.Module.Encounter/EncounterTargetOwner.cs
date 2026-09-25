using System;

namespace AnimusForge.Refactor.Modules;

// Owns the selected encounter target across repeated menu checks. The host
// supplies current Bannerlord eligibility and leader fallback facts.
internal sealed class EncounterTargetOwner<TTarget, TEncounter> where TTarget : class
{
    internal TTarget Target { get; private set; }

    internal void Set(TTarget target) => Target = target;

    internal TTarget Ensure(TEncounter encounter,
        Func<TTarget, TEncounter, bool> eligible,
        Func<TEncounter, TTarget> fallback,
        out bool refreshed, out bool cleared)
    {
        refreshed = false;
        cleared = false;
        if (Target != null && eligible(Target, encounter)) return Target;

        TTarget previous = Target;
        Target = fallback(encounter);
        refreshed = Target != null && !ReferenceEquals(previous, Target);
        cleared = previous != null && Target == null;
        return Target;
    }
}

namespace AnimusForge.Refactor.Modules;

// Mission-local transition owner. The Mission adapter alone applies teams,
// damage, equipment and native fight callbacks after a successful transition.
internal sealed class SceneTauntConflictLifecycleOwner
{
    internal bool Active { get; private set; }
    internal bool Armed { get; private set; }
    internal bool ArmedOccurred { get; private set; }

    internal bool TryBeginUnarmed()
    {
        if (Active) return false;
        Active = true;
        Armed = false;
        ArmedOccurred = false;
        return true;
    }

    internal bool TryBeginArmedCarryover()
    {
        if (Active) return false;
        Active = true;
        Armed = true;
        ArmedOccurred = true;
        return true;
    }

    internal bool TryEscalate()
    {
        if (!Active || Armed) return false;
        Armed = true;
        ArmedOccurred = true;
        return true;
    }

    // SETS may own the armed Mission while Taunt still handles the safe
    // main-hero defeat decision. This does not initialize a Taunt conflict.
    internal void MarkExternalArmedConflict() => ArmedOccurred = true;

    internal void End(bool preserveArmedDefeatState)
    {
        Active = false;
        Armed = false;
        if (!preserveArmedDefeatState) ArmedOccurred = false;
    }
}
